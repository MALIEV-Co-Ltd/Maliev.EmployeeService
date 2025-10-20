using FluentValidation;
using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Validators;

/// <summary>
/// Validator for CancelLeaveRequestDto
/// </summary>
public class CancelLeaveRequestValidator : AbstractValidator<CancelLeaveRequestDto>
{
    public CancelLeaveRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Cancellation reason is required")
            .MaximumLength(1000)
            .WithMessage("Reason cannot exceed 1000 characters");
    }
}
