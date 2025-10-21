using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for mandatory training requirements
/// </summary>
public interface IMandatoryTrainingRepository
{
    Task<IEnumerable<MandatoryTrainingRequirement>> GetActiveRequirementsForEmployeeAsync(
        EmploymentType employmentType,
        string? jobTitle,
        CancellationToken cancellationToken = default);
}
