namespace Maliev.EmployeeService.Domain.IntegrationEvents;

public record EmployeeCreatedIntegrationEvent(
    Guid EmployeeId,
    string EmployeeNumber,
    string FullName,
    string Email,
    DateTime HireDate,
    Guid DepartmentId,
    Guid? ManagerId,
    string JobTitle);
