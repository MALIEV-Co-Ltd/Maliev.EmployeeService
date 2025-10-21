using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to update an employee's benefits enrollment
/// </summary>
public record UpdateBenefitsEnrollmentCommand(
    Guid EmployeeId,
    UpdateBenefitsEnrollmentDto EnrollmentDto
);

/// <summary>
/// Result of updating benefits enrollment
/// </summary>
public record UpdateBenefitsEnrollmentCommandResult(
    bool Success,
    Guid? EnrollmentId = null,
    string? ErrorMessage = null
);

/// <summary>
/// Handler for UpdateBenefitsEnrollmentCommand
/// </summary>
public class UpdateBenefitsEnrollmentCommandHandler
{
    private readonly IBenefitsRepository _benefitsRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBenefitsEnrollmentCommandHandler(
        IBenefitsRepository benefitsRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _benefitsRepository = benefitsRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateBenefitsEnrollmentCommandResult> HandleAsync(
        UpdateBenefitsEnrollmentCommand command,
        CancellationToken cancellationToken = default)
    {
        // Verify employee exists
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return new UpdateBenefitsEnrollmentCommandResult(false, null, "Employee not found");
        }

        // Get existing enrollment or create new one
        var existingEnrollment = await _benefitsRepository.GetEnrollmentAsync(command.EmployeeId, cancellationToken);

        if (existingEnrollment != null)
        {
            // Update existing enrollment
            existingEnrollment.HealthInsurancePlan = command.EnrollmentDto.HealthInsurancePlan;
            existingEnrollment.RetirementContribution = command.EnrollmentDto.RetirementContribution;
            existingEnrollment.BeneficiaryInformation = command.EnrollmentDto.BeneficiaryInformation;
            existingEnrollment.EnrollmentDate = command.EnrollmentDto.EnrollmentDate;
            existingEnrollment.ModifiedBy = _currentUserService.EmployeeId;
            existingEnrollment.ModifiedDate = DateTime.UtcNow;

            await _benefitsRepository.UpdateEnrollmentAsync(existingEnrollment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateBenefitsEnrollmentCommandResult(true, existingEnrollment.Id);
        }
        else
        {
            // Create new enrollment
            var newEnrollment = new BenefitsEnrollment
            {
                Id = Guid.NewGuid(),
                EmployeeId = command.EmployeeId,
                HealthInsurancePlan = command.EnrollmentDto.HealthInsurancePlan,
                RetirementContribution = command.EnrollmentDto.RetirementContribution,
                BeneficiaryInformation = command.EnrollmentDto.BeneficiaryInformation,
                EnrollmentDate = command.EnrollmentDto.EnrollmentDate,
                CreatedBy = _currentUserService.EmployeeId,
                CreatedDate = DateTime.UtcNow
            };

            await _benefitsRepository.AddAsync(newEnrollment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateBenefitsEnrollmentCommandResult(true, newEnrollment.Id);
        }
    }
}
