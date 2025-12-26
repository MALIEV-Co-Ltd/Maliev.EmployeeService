using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Maliev.EmployeeService.Infrastructure.Messaging;

/// <summary>
/// Resilient wrapper for IntegrationEventPublisher with Polly circuit breaker and retry policies
/// Phase 16 - T396, T397: Circuit breaker and retry policy for RabbitMQ
/// </summary>
public class ResilientIntegrationEventPublisher : IEventPublisher
{
    private readonly IntegrationEventPublisher _innerPublisher;
    private readonly ILogger<ResilientIntegrationEventPublisher> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly Counter<long> _publishAttempts;
    private readonly Counter<long> _publishFailures;
    private readonly Counter<long> _circuitBreakerStateChanges;
    private readonly KeyValuePair<string, object?>[] _defaultTags;

    public ResilientIntegrationEventPublisher(
        IntegrationEventPublisher innerPublisher,
        ILogger<ResilientIntegrationEventPublisher> logger,
        IMeterFactory meterFactory,
        IConfiguration configuration)
    {
        _innerPublisher = innerPublisher;
        _logger = logger;

        // Create metrics
        var serviceName = configuration["Service:Name"] ?? "EmployeeService";
        var meter = meterFactory.Create($"{serviceName.ToLower()}-rabbitmq-meter");

        _defaultTags = new[]
        {
            new KeyValuePair<string, object?>("service_name", serviceName),
            new KeyValuePair<string, object?>("version", configuration["Service:Version"] ?? "1.0.0"),
            new KeyValuePair<string, object?>("region", configuration["Service:Region"] ?? "global"),
            new KeyValuePair<string, object?>("environment", configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production")
        };

        _publishAttempts = meter.CreateCounter<long>("rabbitmq_publish_attempts_total",
            description: "Total number of RabbitMQ publish attempts");
        _publishFailures = meter.CreateCounter<long>("rabbitmq_publish_failures_total",
            description: "Total number of RabbitMQ publish failures");
        _circuitBreakerStateChanges = meter.CreateCounter<long>("rabbitmq_circuit_breaker_state_changes_total",
            description: "Total number of circuit breaker state changes");

        // Build resilience pipeline with retry + circuit breaker
        _resiliencePipeline = BuildResiliencePipeline();
    }

    private ResiliencePipeline BuildResiliencePipeline()
    {
        // T397: Retry policy - 3 retries with exponential backoff (2s, 4s, 8s)
        var retryOptions = new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromSeconds(2),
            UseJitter = true, // Add jitter to prevent thundering herd
            OnRetry = args =>
            {
                _logger.LogWarning(
                    "Retry attempt {AttemptNumber} for RabbitMQ publish after {Delay}ms. Exception: {Exception}",
                    args.AttemptNumber,
                    args.RetryDelay.TotalMilliseconds,
                    args.Outcome.Exception?.Message);
                return ValueTask.CompletedTask;
            }
        };

        // T396: Circuit breaker - 5 failures, 30 second break
        var breakDuration = TimeSpan.FromSeconds(30);
        var circuitBreakerOptions = new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5, // Open circuit if 50% of requests fail
            SamplingDuration = TimeSpan.FromSeconds(30), // Sample window
            MinimumThroughput = 5, // Minimum 5 requests before opening
            BreakDuration = breakDuration, // Stay open for 30 seconds
            OnOpened = args =>
            {
                _logger.LogError(
                    "Circuit breaker OPENED for RabbitMQ publishing. Will remain open for {BreakDuration}s",
                    breakDuration.TotalSeconds);

                var tags = new TagList();
                foreach (var tag in _defaultTags) tags.Add(tag);
                tags.Add("state", "open");
                _circuitBreakerStateChanges.Add(1, tags);
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                _logger.LogInformation("Circuit breaker CLOSED for RabbitMQ publishing. Normal operation resumed.");

                var tags = new TagList();
                foreach (var tag in _defaultTags) tags.Add(tag);
                tags.Add("state", "closed");
                _circuitBreakerStateChanges.Add(1, tags);
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = args =>
            {
                _logger.LogInformation("Circuit breaker HALF-OPEN for RabbitMQ publishing. Testing connection...");

                var tags = new TagList();
                foreach (var tag in _defaultTags) tags.Add(tag);
                tags.Add("state", "half-open");
                _circuitBreakerStateChanges.Add(1, tags);
                return ValueTask.CompletedTask;
            }
        };

        // Build pipeline: Retry -> Circuit Breaker
        return new ResiliencePipelineBuilder()
            .AddRetry(retryOptions)
            .AddCircuitBreaker(circuitBreakerOptions)
            .Build();
    }

    /// <summary>
    /// Publishes an integration event with circuit breaker and retry protection
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, string exchange, string routingKey, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var tags = new TagList();
        foreach (var tag in _defaultTags) tags.Add(tag);
        tags.Add("event_type", typeof(TEvent).Name);
        _publishAttempts.Add(1, tags);

        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await _innerPublisher.PublishAsync(integrationEvent, exchange, routingKey, ct);
            }, cancellationToken);
        }
        catch (BrokenCircuitException ex)
        {
            var failTags = new TagList();
            foreach (var tag in _defaultTags) failTags.Add(tag);
            failTags.Add("event_type", typeof(TEvent).Name);
            failTags.Add("reason", "circuit_breaker_open");
            _publishFailures.Add(1, failTags);

            _logger.LogError(
                "Failed to publish {EventType}: Circuit breaker is OPEN. RabbitMQ is unavailable.",
                typeof(TEvent).Name);

            throw new InvalidOperationException(
                $"Cannot publish event {typeof(TEvent).Name}: RabbitMQ circuit breaker is open", ex);
        }
        catch (Exception ex)
        {
            var failTags = new TagList();
            foreach (var tag in _defaultTags) failTags.Add(tag);
            failTags.Add("event_type", typeof(TEvent).Name);
            failTags.Add("reason", "exception");
            _publishFailures.Add(1, failTags);

            _logger.LogError(
                ex,
                "Failed to publish {EventType} after retry attempts",
                typeof(TEvent).Name);

            throw;
        }
    }

    /// <summary>
    /// Publishes an integration event with default routing and circuit breaker protection
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var tags = new TagList();
        foreach (var tag in _defaultTags) tags.Add(tag);
        tags.Add("event_type", typeof(TEvent).Name);
        _publishAttempts.Add(1, tags);

        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await _innerPublisher.PublishAsync(integrationEvent, ct);
            }, cancellationToken);
        }
        catch (BrokenCircuitException ex)
        {
            var failTags = new TagList();
            foreach (var tag in _defaultTags) failTags.Add(tag);
            failTags.Add("event_type", typeof(TEvent).Name);
            failTags.Add("reason", "circuit_breaker_open");
            _publishFailures.Add(1, failTags);

            _logger.LogError(
                "Failed to publish {EventType}: Circuit breaker is OPEN. RabbitMQ is unavailable.",
                typeof(TEvent).Name);

            throw new InvalidOperationException(
                $"Cannot publish event {typeof(TEvent).Name}: RabbitMQ circuit breaker is open", ex);
        }
        catch (Exception ex)
        {
            var failTags = new TagList();
            foreach (var tag in _defaultTags) failTags.Add(tag);
            failTags.Add("event_type", typeof(TEvent).Name);
            failTags.Add("reason", "exception");
            _publishFailures.Add(1, failTags);

            _logger.LogError(
                ex,
                "Failed to publish {EventType} after retry attempts",
                typeof(TEvent).Name);

            throw;
        }
    }
}