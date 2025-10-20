namespace Maliev.EmployeeService.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ connection settings loaded from Google Secret Manager
/// </summary>
public class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "maliev.employee.events";
    public bool Enabled { get; set; } = true;
}
