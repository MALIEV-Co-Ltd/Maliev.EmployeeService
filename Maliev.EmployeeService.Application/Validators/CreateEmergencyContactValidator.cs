using FluentValidation;
using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Validators;

/// <summary>
/// Validator for CreateEmergencyContactDto
/// </summary>
public class CreateEmergencyContactValidator : AbstractValidator<CreateEmergencyContactDto>
{
    public CreateEmergencyContactValidator()
    {
        RuleFor(x => x.ContactName)
            .NotEmpty()
            .WithMessage("Contact name is required")
            .MaximumLength(200)
            .WithMessage("Contact name cannot exceed 200 characters");

        RuleFor(x => x.Relationship)
            .NotEmpty()
            .WithMessage("Relationship is required")
            .MaximumLength(100)
            .WithMessage("Relationship cannot exceed 100 characters");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Phone number is required")
            .Matches(@"^\+?[0-9\s\-()]+$")
            .WithMessage("Phone number must contain only numbers, spaces, +, -, or ()")
            .MaximumLength(20)
            .WithMessage("Phone number cannot exceed 20 characters");

        When(x => !string.IsNullOrEmpty(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Email must be a valid email address")
                .MaximumLength(255)
                .WithMessage("Email cannot exceed 255 characters");
        });

        RuleFor(x => x.PriorityOrder)
            .GreaterThan(0)
            .WithMessage("Priority order must be greater than 0")
            .LessThanOrEqualTo(10)
            .WithMessage("Priority order cannot exceed 10");
    }
}
