namespace Maliev.EmployeeService.Domain.IntegrationEvents;

public record EmployeeTerminatedIntegrationEvent(
    Guid EmployeeId,
    string EmployeeNumber,
    DateTime TerminationDate);
