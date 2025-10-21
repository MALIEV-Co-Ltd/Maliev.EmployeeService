using FluentValidation;
using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Validators;

/// <summary>
/// Validator for SubmitLeaveRequestDto
/// </summary>
public class SubmitLeaveRequestValidator : AbstractValidator<SubmitLeaveRequestDto>
{
    public SubmitLeaveRequestValidator()
    {
        RuleFor(x => x.LeaveType)
            .NotEmpty()
            .WithMessage("Leave type is required");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("Start date is required")
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Start date cannot be in the past");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("End date is required")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be after or equal to start date");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required")
            .MaximumLength(1000)
            .WithMessage("Reason cannot exceed 1000 characters");
    }
}
