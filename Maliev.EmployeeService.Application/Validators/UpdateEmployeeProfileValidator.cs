using FluentValidation;
using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Validators;

/// <summary>
/// Validator for UpdateEmployeeProfileDto
/// </summary>
public class UpdateEmployeeProfileValidator : AbstractValidator<UpdateEmployeeProfileDto>
{
    public UpdateEmployeeProfileValidator()
    {
        When(x => !string.IsNullOrEmpty(x.PersonalEmail), () =>
        {
            RuleFor(x => x.PersonalEmail)
                .EmailAddress()
                .WithMessage("Personal email must be a valid email address")
                .MaximumLength(255)
                .WithMessage("Personal email cannot exceed 255 characters");
        });

        When(x => !string.IsNullOrEmpty(x.MobilePhone), () =>
        {
            RuleFor(x => x.MobilePhone)
                .Matches(@"^\+?[0-9\s\-()]+$")
                .WithMessage("Mobile phone must contain only numbers, spaces, +, -, or ()")
                .MaximumLength(20)
                .WithMessage("Mobile phone cannot exceed 20 characters");
        });

        When(x => !string.IsNullOrEmpty(x.PreferredName), () =>
        {
            RuleFor(x => x.PreferredName)
                .MaximumLength(100)
                .WithMessage("Preferred name cannot exceed 100 characters");
        });
    }
}
