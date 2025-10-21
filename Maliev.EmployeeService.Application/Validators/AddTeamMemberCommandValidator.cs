using FluentValidation;
using Maliev.EmployeeService.Application.Commands;

namespace Maliev.EmployeeService.Application.Validators;

/// <summary>
/// Validator for AddTeamMemberCommand (User Story 5)
/// </summary>
public class AddTeamMemberCommandValidator : AbstractValidator<AddTeamMemberCommand>
{
    public AddTeamMemberCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("Team ID is required");

        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee ID is required");
    }
}
