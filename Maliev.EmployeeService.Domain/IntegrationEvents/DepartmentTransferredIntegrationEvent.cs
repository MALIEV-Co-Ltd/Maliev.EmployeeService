namespace Maliev.EmployeeService.Domain.IntegrationEvents;

public record DepartmentTransferredIntegrationEvent(
    Guid EmployeeId,
    Guid OldDepartmentId,
    Guid NewDepartmentId,
    DateTime EffectiveDate);
