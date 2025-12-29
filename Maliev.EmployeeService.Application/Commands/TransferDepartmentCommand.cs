using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.IntegrationEvents;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to transfer an employee to a different department
/// </summary>
public record TransferDepartmentCommand(
    Guid EmployeeId,
    Guid NewDepartmentId,
    string? TransferReason,
    DateTime EffectiveDate);

/// <summary>
/// Response for department transfer
/// </summary>
public record TransferDepartmentCommandResult(bool Success, string? ErrorMessage);

/// <summary>
/// Handler for TransferDepartmentCommand
/// </summary>
public class TransferDepartmentCommandHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventPublisher _eventPublisher;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;

    public TransferDepartmentCommandHandler(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        IEventPublisher eventPublisher)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _iamClient = iamClient;
        _configuration = configuration;
        _eventPublisher = eventPublisher;
    }

    public async Task<TransferDepartmentCommandResult> HandleAsync(
        TransferDepartmentCommand command,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have DepartmentsManage permission
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.DepartmentsManage, "employee/departments", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to manage departments");
        }
        // Validate employee exists
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return new TransferDepartmentCommandResult(false, "Employee not found");
        }

        if (!employee.IsActive)
        {
            return new TransferDepartmentCommandResult(false, "Cannot transfer inactive employee");
        }

        // Validate new department exists
        var newDepartment = await _departmentRepository.GetByIdAsync(command.NewDepartmentId, cancellationToken);
        if (newDepartment == null)
        {
            return new TransferDepartmentCommandResult(false, "Target department not found");
        }

        if (!newDepartment.IsActive)
        {
            return new TransferDepartmentCommandResult(false, "Cannot transfer to inactive department");
        }

        // Check if already in this department
        if (employee.DepartmentId == command.NewDepartmentId)
        {
            return new TransferDepartmentCommandResult(false, "Employee is already in this department");
        }

        // Check headcount limit
        if (newDepartment.IsAtHeadcountLimit)
        {
            return new TransferDepartmentCommandResult(
                false,
                $"Department '{newDepartment.Name}' has reached its headcount limit of {newDepartment.HeadcountLimit}");
        }

        // Validate effective date
        if (command.EffectiveDate < DateTime.UtcNow.Date)
        {
            return new TransferDepartmentCommandResult(false, "Effective date cannot be in the past");
        }

        // Perform the transfer
        var oldDepartmentId = employee.DepartmentId;
        employee.DepartmentId = command.NewDepartmentId;
        employee.ModifiedBy = _currentUserService.PrincipalId;
        employee.ModifiedDate = DateTime.UtcNow;

        _employeeRepository.Update(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish event for access control and email distribution list updates (Phase 3 - T127)
        await _eventPublisher.PublishAsync(new DepartmentTransferredIntegrationEvent(
            employee.Id,
            oldDepartmentId ?? Guid.Empty,
            command.NewDepartmentId,
            command.EffectiveDate
        ), cancellationToken);

        return new TransferDepartmentCommandResult(true, null);
    }
}
