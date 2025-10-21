using System.Text;
using System.Text.Json;
using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Maliev.EmployeeService.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ implementation of event publisher for domain events
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;

    public RabbitMqEventPublisher(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (_settings.Enabled)
        {
            InitializeConnection();
        }
    }

    private void InitializeConnection()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // Declare exchange
            _channel.ExchangeDeclareAsync(
                exchange: _settings.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null).GetAwaiter().GetResult();

            _logger.LogInformation(
                "RabbitMQ connection established to {Host}:{Port}",
                _settings.HostName,
                _settings.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection");
            throw;
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event, string exchange, string routingKey, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        if (!_settings.Enabled)
        {
            _logger.LogWarning("RabbitMQ is disabled. Event not published: {EventType}", typeof(TEvent).Name);
            return;
        }

        if (_channel == null)
        {
            _logger.LogError("RabbitMQ channel is not initialized");
            throw new InvalidOperationException("RabbitMQ channel is not initialized");
        }

        try
        {
            var message = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogDebug(
                "Published event {EventType} to exchange {Exchange} with routing key {RoutingKey}",
                typeof(TEvent).Name,
                exchange,
                routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", typeof(TEvent).Name);
            throw;
        }
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var routingKey = typeof(TEvent).Name.ToLowerInvariant();
        return PublishAsync(@event, _settings.ExchangeName, routingKey, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _channel?.Dispose();
        _connection?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
