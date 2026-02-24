namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Service for publishing domain events to message broker (RabbitMQ)
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event to the specified exchange/topic
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, string exchange, string routingKey, CancellationToken cancellationToken = default)
        where TEvent : class;

    /// <summary>
    /// Publishes an event with default routing
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;
}
