using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Authorization;
using Maliev.EmployeeService.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to create a new department in the organizational hierarchy
/// </summary>
public record CreateDepartmentCommand(CreateDepartmentDto DepartmentData);

/// <summary>
/// Response containing the newly created department ID
/// </summary>
public record CreateDepartmentCommandResult(bool Success, Guid? DepartmentId, string? ErrorMessage);

/// <summary>
/// Handler for CreateDepartmentCommand
/// </summary>
public class CreateDepartmentCommandHandler
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;

    public CreateDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    public async Task<CreateDepartmentCommandResult> HandleAsync(
        CreateDepartmentCommand command,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have DepartmentsManage permission
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.DepartmentsManage, "employee/departments", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to manage departments");
        }

        var dto = command.DepartmentData;

        // Validate parent department exists if provided
        if (dto.ParentDepartmentId.HasValue)
        {
            var parentDepartment = await _departmentRepository.GetByIdAsync(dto.ParentDepartmentId.Value, cancellationToken);
            if (parentDepartment == null)
            {
                return new CreateDepartmentCommandResult(
                    false,
                    null,
                    $"Parent department with ID '{dto.ParentDepartmentId.Value}' not found");
            }

            if (!parentDepartment.IsActive)
            {
                return new CreateDepartmentCommandResult(
                    false,
                    null,
                    "Cannot create subdepartment under inactive parent department");
            }

            // Detect circular hierarchy (parent → child → parent)
            if (await WouldCreateCircularHierarchy(dto.ParentDepartmentId.Value, Guid.Empty, cancellationToken))
            {
                return new CreateDepartmentCommandResult(
                    false,
                    null,
                    "Cannot create department: would create circular hierarchy");
            }
        }

        // Validate department head exists if provided
        if (dto.DepartmentHeadId.HasValue)
        {
            var departmentHead = await _employeeRepository.GetByIdAsync(dto.DepartmentHeadId.Value, cancellationToken);
            if (departmentHead == null)
            {
                return new CreateDepartmentCommandResult(
                    false,
                    null,
                    $"Department head employee with ID '{dto.DepartmentHeadId.Value}' not found");
            }

            if (!departmentHead.IsActive)
            {
                return new CreateDepartmentCommandResult(
                    false,
                    null,
                    "Cannot assign inactive employee as department head");
            }
        }

        // Create the department
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            ParentDepartmentId = dto.ParentDepartmentId,
            DepartmentHeadId = dto.DepartmentHeadId,
            CostCenter = dto.CostCenter,
            HeadcountLimit = dto.HeadcountLimit,
            IsActive = true,
            CreatedBy = _currentUserService.PrincipalId,
            CreatedDate = DateTime.UtcNow
        };

        await _departmentRepository.AddAsync(department, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateDepartmentCommandResult(true, department.Id, null);
    }

    /// <summary>
    /// Checks if adding a parent department would create a circular hierarchy
    /// </summary>
    private async Task<bool> WouldCreateCircularHierarchy(
        Guid proposedParentId,
        Guid currentDepartmentId,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<Guid> { currentDepartmentId };
        var currentParentId = proposedParentId;

        while (currentParentId != Guid.Empty)
        {
            if (visited.Contains(currentParentId))
            {
                return true; // Circular reference detected
            }

            visited.Add(currentParentId);

            var parent = await _departmentRepository.GetByIdAsync(currentParentId, cancellationToken);
            if (parent == null || !parent.ParentDepartmentId.HasValue)
            {
                break;
            }

            currentParentId = parent.ParentDepartmentId.Value;
        }

        return false;
    }
}
