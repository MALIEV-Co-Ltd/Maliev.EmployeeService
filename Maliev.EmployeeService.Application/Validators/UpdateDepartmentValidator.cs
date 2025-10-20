using FluentValidation;
using Maliev.EmployeeService.Application.Commands;

namespace Maliev.EmployeeService.Application.Validators;

public class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentValidator()
    {
        RuleFor(x => x.DepartmentId)
            .NotEmpty()
            .WithMessage("Department ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Department name is required")
            .MaximumLength(200)
            .WithMessage("Department name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.CostCenter)
            .MaximumLength(50)
            .WithMessage("Cost center cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.CostCenter));

        RuleFor(x => x.HeadcountLimit)
            .GreaterThan(0)
            .WithMessage("Headcount limit must be greater than 0")
            .When(x => x.HeadcountLimit.HasValue);
    }
}
