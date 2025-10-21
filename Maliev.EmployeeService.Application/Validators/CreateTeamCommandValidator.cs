using FluentValidation;
using Maliev.EmployeeService.Application.Commands;

namespace Maliev.EmployeeService.Application.Validators;

/// <summary>
/// Validator for CreateTeamCommand (User Story 5)
/// </summary>
public class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Team name is required")
            .MaximumLength(200).WithMessage("Team name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.TeamType)
            .NotEmpty().WithMessage("Team type is required")
            .MaximumLength(100).WithMessage("Team type cannot exceed 100 characters");

        RuleFor(x => x.TeamLeadId)
            .NotEmpty().WithMessage("Team lead ID must be a valid GUID")
            .When(x => x.TeamLeadId.HasValue);
    }
}
