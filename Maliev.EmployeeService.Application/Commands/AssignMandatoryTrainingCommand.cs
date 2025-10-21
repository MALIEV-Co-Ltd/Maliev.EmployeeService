using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to assign mandatory training to an employee based on their role
/// </summary>
public class AssignMandatoryTrainingCommand
{
    public Guid EmployeeId { get; set; }
}

/// <summary>
/// Handler for AssignMandatoryTrainingCommand
/// Auto-assigns mandatory training based on employment type and job role
/// </summary>
public class AssignMandatoryTrainingCommandHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IMandatoryTrainingRepository _mandatoryTrainingRepository;
    private readonly ILogger<AssignMandatoryTrainingCommandHandler> _logger;

    public AssignMandatoryTrainingCommandHandler(
        IEmployeeRepository employeeRepository,
        IMandatoryTrainingRepository mandatoryTrainingRepository,
        ILogger<AssignMandatoryTrainingCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _mandatoryTrainingRepository = mandatoryTrainingRepository;
        _logger = logger;
    }

    public async Task<List<string>> HandleAsync(AssignMandatoryTrainingCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Assigning mandatory training for employee {EmployeeId}", command.EmployeeId);

        // Get employee with details
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee == null)
        {
            throw new InvalidOperationException($"Employee {command.EmployeeId} not found");
        }

        // Get mandatory training requirements
        var requirements = await _mandatoryTrainingRepository.GetActiveRequirementsForEmployeeAsync(
            employee.EmploymentType,
            employee.JobTitle,
            cancellationToken);

        var assignedCourses = new List<string>();

        foreach (var requirement in requirements)
        {
            var courses = requirement.GetRequiredCoursesList();
            assignedCourses.AddRange(courses);
        }

        _logger.LogInformation("Assigned {Count} mandatory training courses to employee {EmployeeId}: {Courses}",
            assignedCourses.Count, command.EmployeeId, string.Join(", ", assignedCourses));

        return assignedCourses;
    }
}
