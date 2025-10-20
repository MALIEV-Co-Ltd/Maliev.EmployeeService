using FluentValidation;
using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Validators;

/// <summary>
/// Validator for ApproveRejectLeaveDto
/// </summary>
public class ApproveRejectLeaveValidator : AbstractValidator<ApproveRejectLeaveDto>
{
    public ApproveRejectLeaveValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Comments), () =>
        {
            RuleFor(x => x.Comments)
                .MaximumLength(1000)
                .WithMessage("Comments cannot exceed 1000 characters");
        });

        When(x => !x.IsApproved, () =>
        {
            RuleFor(x => x.Comments)
                .NotEmpty()
                .WithMessage("Comments are required when rejecting a leave request");
        });
    }
}
