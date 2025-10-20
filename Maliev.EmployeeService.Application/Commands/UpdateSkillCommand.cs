using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to update employee skill proficiency
/// </summary>
public class UpdateSkillCommand
{
    public Guid SkillId { get; set; }
    public int ProficiencyLevel { get; set; }
    public bool IsDevelopmentArea { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Handler for UpdateSkillCommand
/// </summary>
public class UpdateSkillCommandHandler
{
    private readonly ISkillRepository _skillRepository;
    private readonly ILogger<UpdateSkillCommandHandler> _logger;

    public UpdateSkillCommandHandler(
        ISkillRepository skillRepository,
        ILogger<UpdateSkillCommandHandler> logger)
    {
        _skillRepository = skillRepository;
        _logger = logger;
    }

    public async Task<Skill> HandleAsync(UpdateSkillCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating skill {SkillId}, proficiency level {ProficiencyLevel}",
            command.SkillId, command.ProficiencyLevel);

        var skill = await _skillRepository.GetByIdAsync(command.SkillId, cancellationToken);
        if (skill == null)
        {
            throw new InvalidOperationException($"Skill {command.SkillId} not found");
        }

        // Validate proficiency level (1-5 scale)
        if (command.ProficiencyLevel < 1 || command.ProficiencyLevel > 5)
        {
            throw new ArgumentException("Proficiency level must be between 1 and 5", nameof(command.ProficiencyLevel));
        }

        skill.ProficiencyLevel = command.ProficiencyLevel;
        skill.IsDevelopmentArea = command.IsDevelopmentArea;
        skill.Notes = command.Notes;
        skill.LastAssessedDate = DateTime.UtcNow;

        await _skillRepository.UpdateAsync(skill, cancellationToken);

        _logger.LogInformation("Skill {SkillId} updated for employee {EmployeeId}",
            skill.Id, skill.EmployeeId);

        return skill;
    }
}
