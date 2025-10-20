using FluentValidation;
using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Validators;

/// <summary>
/// Validator for CreateEmployeeDto
/// </summary>
public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeDto>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.EmployeeNumber)
            .NotEmpty().WithMessage("Employee number is required")
            .MaximumLength(50).WithMessage("Employee number cannot exceed 50 characters");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.MiddleName)
            .MaximumLength(100).WithMessage("Middle name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.MiddleName));

        RuleFor(x => x.WorkEmail)
            .NotEmpty().WithMessage("Work email is required")
            .EmailAddress().WithMessage("Work email must be a valid email address")
            .MaximumLength(255).WithMessage("Work email cannot exceed 255 characters");

        RuleFor(x => x.PersonalEmail)
            .EmailAddress().WithMessage("Personal email must be a valid email address")
            .MaximumLength(255).WithMessage("Personal email cannot exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.PersonalEmail));

        RuleFor(x => x.MobilePhone)
            .MaximumLength(20).WithMessage("Mobile phone cannot exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.MobilePhone));

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .LessThan(DateTime.UtcNow.AddYears(-16)).WithMessage("Employee must be at least 16 years old")
            .GreaterThan(DateTime.UtcNow.AddYears(-100)).WithMessage("Date of birth must be within the last 100 years");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required")
            .GreaterThanOrEqualTo(DateTime.UtcNow.AddYears(-1).Date)
                .WithMessage("Start date cannot be more than 1 year in the past");

        RuleFor(x => x.ProbationEndDate)
            .GreaterThan(x => x.StartDate).WithMessage("Probation end date must be after start date")
            .LessThanOrEqualTo(x => x.StartDate.AddDays(180))
                .WithMessage("Probation period cannot exceed 180 days")
            .When(x => x.ProbationEndDate.HasValue);

        RuleFor(x => x.JobTitle)
            .MaximumLength(200).WithMessage("Job title cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.JobTitle));

        RuleFor(x => x.WorkLocation)
            .MaximumLength(200).WithMessage("Work location cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.WorkLocation));

        RuleFor(x => x.Nationality)
            .MaximumLength(50).WithMessage("Nationality cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.Nationality));

        RuleFor(x => x.NationalId)
            .Length(13).WithMessage("Thai National ID must be exactly 13 digits")
            .Matches(@"^\d{13}$").WithMessage("Thai National ID must contain only digits")
            .When(x => !string.IsNullOrEmpty(x.NationalId));

        RuleFor(x => x.EmploymentType)
            .IsInEnum().WithMessage("Invalid employment type");
    }
}
