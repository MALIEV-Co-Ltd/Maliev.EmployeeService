using Maliev.EmployeeService.Application.Commands;
using Maliev.MessagingContracts.Contracts.Career;
using Maliev.MessagingContracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Maliev.EmployeeService.Infrastructure.Messaging;

/// <summary>
/// Background service that consumes CandidateAccepted events from Career Service
/// Automatically creates employee records and initiates onboarding workflow
/// Updated for RabbitMQ 7.0+ async APIs (T026d)
/// </summary>
public class CandidateAcceptedEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CandidateAcceptedEventConsumer> _logger;
    private readonly RabbitMqSettings _settings;
    private IConnection? _connection;
    private IChannel? _channel;

    private const string QueueName = "career-service.candidate-accepted";
    private const string DeadLetterQueueName = "career-service.candidate-accepted.dlq";
    private const string DeadLetterExchange = "career-service.dlx";
    private const int MaxRetryCount = 3;

    public CandidateAcceptedEventConsumer(
        IServiceProvider serviceProvider,
        ILogger<CandidateAcceptedEventConsumer> logger,
        IOptions<RabbitMqSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("RabbitMQ is disabled. CandidateAcceptedEventConsumer will not start");
            return;
        }

        _logger.LogInformation("CandidateAcceptedEventConsumer starting");

        try
        {
            await InitializeRabbitMQAsync(stoppingToken);

            if (_channel == null)
            {
                _logger.LogError("Failed to initialize RabbitMQ channel");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                await ProcessMessageAsync(ea, stoppingToken);
            };

            await _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("CandidateAcceptedEventConsumer started successfully. Listening on queue: {QueueName}", QueueName);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in CandidateAcceptedEventConsumer");
            throw;
        }
    }

    private async Task InitializeRabbitMQAsync(CancellationToken cancellationToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            // Declare dead-letter exchange
            await _channel.ExchangeDeclareAsync(
                exchange: DeadLetterExchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                arguments: null,
                noWait: false,
                cancellationToken: cancellationToken);

            // Declare dead-letter queue
            await _channel.QueueDeclareAsync(
                queue: DeadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                noWait: false,
                cancellationToken: cancellationToken);

            await _channel.QueueBindAsync(
                queue: DeadLetterQueueName,
                exchange: DeadLetterExchange,
                routingKey: QueueName,
                arguments: null,
                noWait: false,
                cancellationToken: cancellationToken);

            // Declare main queue with dead-letter configuration
            var queueArgs = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", DeadLetterExchange },
                { "x-dead-letter-routing-key", QueueName },
                { "x-message-ttl", 300000 } // 5 minutes TTL for retries
            };

            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs,
                noWait: false,
                cancellationToken: cancellationToken);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "RabbitMQ connection established. Queue: {QueueName}, DLQ: {DLQName}",
                QueueName,
                DeadLetterQueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection");
            throw;
        }
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);

        try
        {
            _logger.LogInformation(
                "Received CandidateAccepted event. DeliveryTag: {DeliveryTag}",
                ea.DeliveryTag);

            var candidateEvent = JsonSerializer.Deserialize<CandidateAcceptedEvent>(
                message,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (candidateEvent == null)
            {
                _logger.LogWarning("Failed to deserialize CandidateAccepted event. Message: {Message}", message);
                if (_channel != null)
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false, cancellationToken); // Send to DLQ
                return;
            }

            // Process the event (T026e)
            await HandleCandidateAcceptedAsync(candidateEvent, cancellationToken);

            // Acknowledge successful processing
            if (_channel != null)
                await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);

            _logger.LogInformation(
                "Successfully processed CandidateAccepted event for {ApplicantName}",
                candidateEvent.Payload.ApplicantName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing CandidateAccepted event. DeliveryTag: {DeliveryTag}, Message: {Message}",
                ea.DeliveryTag,
                message);

            // Check retry count
            var retryCount = GetRetryCount(ea.BasicProperties);

            if (retryCount < MaxRetryCount)
            {
                _logger.LogInformation(
                    "Requeueing message for retry. Attempt {RetryCount} of {MaxRetries}",
                    retryCount + 1,
                    MaxRetryCount);

                // Requeue for retry
                if (_channel != null)
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);
            }
            else
            {
                _logger.LogError(
                    "Max retry count reached for message. Sending to dead-letter queue");

                // Send to DLQ (no requeue) - T026l implemented
                if (_channel != null)
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Handles CandidateAccepted event by creating employee record (T026e)
    /// </summary>
    private async Task HandleCandidateAcceptedAsync(CandidateAcceptedEvent candidateEvent, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var payload = candidateEvent.Payload;

        // Generate employee number (unique based on timestamp and guid)
        var employeeNumber = $"EMP{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";

        // Create employee DTO from candidate event payload
        var createEmployeeDto = new Maliev.EmployeeService.Application.DTOs.CreateEmployeeDto
        {
            EmployeeNumber = employeeNumber,
            FirstName = GetFirstName(payload.ApplicantName),
            MiddleName = GetMiddleName(payload.ApplicantName),
            LastName = GetLastName(payload.ApplicantName),
            WorkEmail = payload.ApplicantEmail,
            PersonalEmail = payload.ApplicantEmail,
            MobilePhone = payload.ApplicantPhone,
            DateOfBirth = DateTime.UtcNow.AddYears(-25), // Default age, should be provided by Career Service
            StartDate = payload.StartDate.DateTime,
            EmploymentType = Domain.Enums.EmploymentType.FullTime,
            DepartmentId = payload.DepartmentId,
            ManagerId = payload.ManagerId,
            WorkLocationId = payload.WorkLocationId,
            JobApplicationId = payload.CandidateId
        };

        var createEmployeeCommand = new CreateEmployeeCommand(createEmployeeDto);
        var createEmployeeHandler = scope.ServiceProvider.GetRequiredService<CreateEmployeeCommandHandler>();

        var result = await createEmployeeHandler.HandleAsync(createEmployeeCommand, cancellationToken);

        if (!result.Success)
        {
            _logger.LogError(
                "Failed to create employee from CandidateAccepted event. Candidate: {CandidateId}, Error: {Error}",
                payload.CandidateId,
                result.ErrorMessage);

            throw new InvalidOperationException(
                $"Failed to create employee: {result.ErrorMessage}");
        }

        _logger.LogInformation(
            "Employee created from CandidateAccepted event. EmployeeId: {EmployeeId}, CandidateId: {CandidateId}",
            result.EmployeeId,
            payload.CandidateId);

        // LifecycleService will pick up the EmployeeCreatedEvent and initiate the onboarding workflow
    }

    private int GetRetryCount(IReadOnlyBasicProperties? properties)
    {
        if (properties?.Headers == null)
            return 0;

        if (properties.Headers.TryGetValue("x-retry-count", out var value))
        {
            if (value is byte[] bytes)
            {
                return BitConverter.ToInt32(bytes, 0);
            }
        }

        return 0;
    }

    private string GetFirstName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : fullName;
    }

    private string? GetMiddleName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 2 ? parts[1] : null;
    }

    private string GetLastName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[parts.Length - 1] : string.Empty;
    }

    public override void Dispose()
    {
        try
        {
            if (_channel != null)
            {
                _channel.CloseAsync().GetAwaiter().GetResult();
                _channel.Dispose();
            }

            if (_connection != null)
            {
                _connection.CloseAsync().GetAwaiter().GetResult();
                _connection.Dispose();
            }

            _logger.LogInformation("CandidateAcceptedEventConsumer disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CandidateAcceptedEventConsumer disposal");
        }
        finally
        {
            base.Dispose();
        }
    }
}
