using FluentValidation;
using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Validators;

/// <summary>
/// Validator for RecordCompensationChangeDto
/// </summary>
public class RecordCompensationChangeValidator : AbstractValidator<RecordCompensationChangeDto>
{
    public RecordCompensationChangeValidator()
    {
        RuleFor(x => x.SalaryAmount)
            .GreaterThan(0)
            .WithMessage("Salary amount must be greater than zero");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be a 3-letter ISO code (e.g., USD, EUR, THB)");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty()
            .WithMessage("Effective date is required")
            .LessThanOrEqualTo(DateTime.UtcNow.AddYears(1))
            .WithMessage("Effective date cannot be more than 1 year in the future");

        RuleFor(x => x.ChangeReason)
            .NotEmpty()
            .WithMessage("Change reason is required")
            .MaximumLength(500)
            .WithMessage("Change reason cannot exceed 500 characters");

        RuleFor(x => x.BonusStructure)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.BonusStructure))
            .WithMessage("Bonus structure cannot exceed 1000 characters");

        RuleFor(x => x.CommissionStructure)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.CommissionStructure))
            .WithMessage("Commission structure cannot exceed 1000 characters");
    }
}
