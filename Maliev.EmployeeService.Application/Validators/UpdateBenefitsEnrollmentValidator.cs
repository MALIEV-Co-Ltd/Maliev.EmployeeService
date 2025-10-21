using FluentValidation;
using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Validators;

/// <summary>
/// Validator for UpdateBenefitsEnrollmentDto
/// </summary>
public class UpdateBenefitsEnrollmentValidator : AbstractValidator<UpdateBenefitsEnrollmentDto>
{
    public UpdateBenefitsEnrollmentValidator()
    {
        RuleFor(x => x.EnrollmentDate)
            .NotEmpty()
            .WithMessage("Enrollment date is required")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Enrollment date cannot be in the future");

        RuleFor(x => x.HealthInsurancePlan)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.HealthInsurancePlan))
            .WithMessage("Health insurance plan cannot exceed 200 characters");

        RuleFor(x => x.RetirementContribution)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.RetirementContribution))
            .WithMessage("Retirement contribution cannot exceed 500 characters");

        RuleFor(x => x.BeneficiaryInformation)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.BeneficiaryInformation))
            .WithMessage("Beneficiary information cannot exceed 2000 characters");
    }
}
