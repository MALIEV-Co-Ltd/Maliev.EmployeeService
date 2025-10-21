using FluentValidation;
using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Validators;

/// <summary>
/// Validator for CreateDepartmentDto
/// </summary>
public class CreateDepartmentValidator : AbstractValidator<CreateDepartmentDto>
{
    public CreateDepartmentValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Department name is required")
            .MaximumLength(200).WithMessage("Department name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.CostCenter)
            .MaximumLength(50).WithMessage("Cost center cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.CostCenter));

        RuleFor(x => x.HeadcountLimit)
            .GreaterThan(0).WithMessage("Headcount limit must be greater than 0")
            .LessThanOrEqualTo(1000).WithMessage("Headcount limit cannot exceed 1000")
            .When(x => x.HeadcountLimit.HasValue);
    }
}
