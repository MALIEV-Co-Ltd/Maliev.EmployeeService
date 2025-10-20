using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Application.Commands;

public class UpdateDepartmentCommandHandler
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork)
    {
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateDepartmentResult> HandleAsync(
        UpdateDepartmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = new UpdateDepartmentResult { Success = true };

        // Get existing department
        var department = await _departmentRepository.GetByIdAsync(command.DepartmentId, cancellationToken);
        if (department == null)
        {
            result.Success = false;
            result.ErrorMessage = "Department not found";
            return result;
        }

        // Check if trying to set inactive with active employees
        if (!command.IsActive && department.IsActive)
        {
            var activeEmployeeCount = await _employeeRepository.GetCountByDepartmentAsync(
                command.DepartmentId,
                cancellationToken);

            if (activeEmployeeCount > 0)
            {
                result.Success = false;
                result.ErrorMessage = $"Cannot deactivate department with {activeEmployeeCount} active employee(s). Please reassign employees first.";
                return result;
            }
        }

        // Check headcount limit warnings
        if (command.HeadcountLimit.HasValue)
        {
            var currentHeadcount = await _employeeRepository.GetCountByDepartmentAsync(
                command.DepartmentId,
                cancellationToken);

            if (command.HeadcountLimit.Value < currentHeadcount)
            {
                result.Success = false;
                result.ErrorMessage = $"Cannot set headcount limit ({command.HeadcountLimit.Value}) below current headcount ({currentHeadcount})";
                return result;
            }

            // Warning at 80% capacity
            var warningThreshold = command.HeadcountLimit.Value * 0.8;
            if (currentHeadcount >= warningThreshold)
            {
                var percentFull = (double)currentHeadcount / command.HeadcountLimit.Value * 100;
                result.Warnings.Add($"Department is at {percentFull:F1}% capacity ({currentHeadcount}/{command.HeadcountLimit.Value})");
            }
        }

        // Validate parent department exists
        if (command.ParentDepartmentId.HasValue)
        {
            var parentDepartment = await _departmentRepository.GetByIdAsync(
                command.ParentDepartmentId.Value,
                cancellationToken);

            if (parentDepartment == null)
            {
                result.Success = false;
                result.ErrorMessage = "Parent department not found";
                return result;
            }

            // Prevent circular references
            if (command.ParentDepartmentId.Value == command.DepartmentId)
            {
                result.Success = false;
                result.ErrorMessage = "Department cannot be its own parent";
                return result;
            }
        }

        // Validate department head exists
        if (command.DepartmentHeadId.HasValue)
        {
            var departmentHead = await _employeeRepository.GetByIdAsync(
                command.DepartmentHeadId.Value,
                cancellationToken);

            if (departmentHead == null)
            {
                result.Success = false;
                result.ErrorMessage = "Department head employee not found";
                return result;
            }
        }

        // Update department properties
        department.Name = command.Name;
        department.Description = command.Description;
        department.ParentDepartmentId = command.ParentDepartmentId;
        department.DepartmentHeadId = command.DepartmentHeadId;
        department.CostCenter = command.CostCenter;
        department.HeadcountLimit = command.HeadcountLimit;
        department.IsActive = command.IsActive;
        department.ModifiedDate = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }
}
