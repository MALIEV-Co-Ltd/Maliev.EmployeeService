using MassTransit;
using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Infrastructure.Messaging;

/// <summary>
/// Service for publishing integration events using MassTransit/RabbitMQ
/// </summary>
public class IntegrationEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<IntegrationEventPublisher> _logger;

    public IntegrationEventPublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<IntegrationEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// Publishes an integration event to a specific exchange with routing key
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, string exchange, string routingKey, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        try
        {
            _logger.LogInformation(
                "Publishing integration event {EventType} to exchange {Exchange} with routing key {RoutingKey}",
                typeof(TEvent).Name,
                exchange,
                routingKey);

            // MassTransit uses topic-based routing by default
            // We publish with headers that can be used for routing
            await _publishEndpoint.Publish(integrationEvent, context =>
            {
                context.Headers.Set("Exchange", exchange);
                context.Headers.Set("RoutingKey", routingKey);
            }, cancellationToken);

            _logger.LogInformation(
                "Successfully published integration event {EventType}",
                typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error publishing integration event {EventType}: {ErrorMessage}",
                typeof(TEvent).Name,
                ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Publishes an integration event with default routing
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        try
        {
            _logger.LogInformation(
                "Publishing integration event {EventType} to message bus",
                typeof(TEvent).Name);

            await _publishEndpoint.Publish(integrationEvent, cancellationToken);

            _logger.LogInformation(
                "Successfully published integration event {EventType}",
                typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error publishing integration event {EventType}: {ErrorMessage}",
                typeof(TEvent).Name,
                ex.Message);
            throw;
        }
    }
}
