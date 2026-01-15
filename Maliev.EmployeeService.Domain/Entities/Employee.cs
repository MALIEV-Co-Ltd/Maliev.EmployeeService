using Maliev.EmployeeService.Domain.Attributes;
using Maliev.EmployeeService.Domain.Common;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Core Employee entity representing an employee in the organization.
/// </summary>
public class Employee : Entity
{
    /// <summary>
    /// Gets or sets the unique identity identifier for the employee, linked to the IAM service.
    /// </summary>
    public Guid PrincipalId { get; set; }

    /// <summary>
    /// Gets or sets the PBKDF2 hashed password for authentication.
    /// Only set for employees who use password-based login.
    /// NULL for Google Workspace SSO-only accounts.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Gets or sets the company-assigned employee number.
    /// </summary>
    public string EmployeeNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the legal name of the employee.
    /// </summary>
    public LegalName LegalName { get; set; } = new();

    /// <summary>
    /// Gets or sets the name the employee preferred to be called.
    /// </summary>
    public string? PreferredName { get; set; }

    /// <summary>
    /// Gets or sets the date of birth of the employee.
    /// </summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Gets or sets the nationality of the employee.
    /// </summary>
    public string? Nationality { get; set; }

    /// <summary>
    /// Gets or sets the contact information for the employee.
    /// </summary>
    public ContactInformation ContactInformation { get; set; } = new();

    /// <summary>
    /// Gets or sets the type of employment (e.g., FullTime, PartTime).
    /// </summary>
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;

    /// <summary>
    /// Gets or sets the current status of employment (e.g., Active, Terminated).
    /// </summary>
    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;

    /// <summary>
    /// Gets or sets the job title of the employee.
    /// </summary>
    public string? JobTitle { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the department the employee belongs to.
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the employee's direct manager.
    /// </summary>
    public Guid? ManagerId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the employee's secondary manager in a matrix organization.
    /// </summary>
    public Guid? DottedLineManagerId { get; set; }

    /// <summary>
    /// Gets or sets the physical work location of the employee.
    /// </summary>
    public string? WorkLocation { get; set; }

    /// <summary>
    /// Gets or sets the date when the employee started working at the company.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the date when the employee's probation period ends.
    /// </summary>
    public DateTime? ProbationEndDate { get; set; }

    /// <summary>
    /// Gets or sets the date when the employee was terminated, if applicable.
    /// </summary>
    public DateTime? TerminationDate { get; set; }

    /// <summary>
    /// Gets or sets the date when the employee record was anonymized for GDPR compliance.
    /// </summary>
    public DateTime? AnonymizedAt { get; set; }

    /// <summary>
    /// Gets or sets the national identification number of the employee.
    /// This field is encrypted at rest.
    /// </summary>
    [Encrypted]
    public string? NationalId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the job application from the Career Service.
    /// </summary>
    public Guid? JobApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the department the employee is assigned to.
    /// </summary>
    public Department? Department { get; set; }

    /// <summary>
    /// Gets or sets the employee's direct manager.
    /// </summary>
    public Employee? Manager { get; set; }

    /// <summary>
    /// Gets or sets the employee's dotted-line manager.
    /// </summary>
    public Employee? DottedLineManager { get; set; }

    /// <summary>
    /// Gets or sets the collection of employees who report directly to this employee.
    /// </summary>
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();

    /// <summary>
    /// Gets or sets the collection of employees who have a dotted-line reporting relationship to this employee.
    /// </summary>
    public ICollection<Employee> DottedLineReports { get; set; } = new List<Employee>();

    /// <summary>
    /// Gets or sets the collection of emergency contacts for the employee.
    /// </summary>
    public ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();

    /// <summary>
    /// Gets or sets the collection of team assignments for the employee.
    /// </summary>
    public ICollection<EmployeeTeamAssignment> TeamAssignments { get; set; } = new List<EmployeeTeamAssignment>();

    /// <summary>
    /// Gets the full name of the employee.
    /// </summary>
    public string FullName => LegalName?.FullName ?? string.Empty;

    /// <summary>
    /// Gets the name to be displayed for the employee, favoring preferred name.
    /// </summary>
    public string DisplayName => PreferredName ?? LegalName?.FirstName ?? string.Empty;

    /// <summary>
    /// Calculates the age of the employee in years as of the specified date.
    /// </summary>
    public int? GetAge(DateTime asOfDate) => DateOfBirth != default ? (int?)((asOfDate - DateOfBirth).TotalDays / 365.25) : null;

    /// <summary>
    /// Calculates the tenure of the employee in years as of the specified date.
    /// </summary>
    public int? GetTenureInYears(DateTime asOfDate) => StartDate != default ? (int?)((asOfDate - StartDate).TotalDays / 365.25) : null;

    /// <summary>
    /// Gets the current age of the employee in years.
    /// </summary>
    public int? Age => GetAge(DateTime.Today);

    /// <summary>
    /// Gets the number of years the employee has been with the company.
    /// </summary>
    public int? TenureInYears => GetTenureInYears(DateTime.Today);

    /// <summary>
    /// Gets a value indicating whether the employee is currently active.
    /// </summary>
    public bool IsActive => EmploymentStatus == EmploymentStatus.Active;

    /// <summary>
    /// Validates that assigning a manager would not create a circular reporting relationship.
    /// </summary>
    /// <param name="proposedManagerId">The identifier of the proposed manager.</param>
    /// <param name="getEmployeeById">A function to retrieve an employee by their identifier.</param>
    /// <returns>True if a circular relationship would be created; otherwise, false.</returns>
    public bool WouldCreateCircularRelationship(Guid proposedManagerId, Func<Guid, Employee?> getEmployeeById)
    {
        if (Id == proposedManagerId)
        {
            return true;
        }

        var currentManager = getEmployeeById(proposedManagerId);
        var visited = new HashSet<Guid> { Id };

        while (currentManager != null && currentManager.ManagerId.HasValue)
        {
            if (visited.Contains(currentManager.ManagerId.Value))
            {
                return true;
            }

            visited.Add(currentManager.ManagerId.Value);
            currentManager = getEmployeeById(currentManager.ManagerId.Value);
        }

        return false;
    }

    /// <summary>
    /// Gets the maximum allowed span of control (direct reports) for this manager.
    /// </summary>
    /// <returns>The maximum number of direct reports allowed.</returns>
    public int GetMaxSpanOfControl()
    {
        if (DirectReports == null || !DirectReports.Any())
        {
            return 15;
        }

        bool hasManagerReports = DirectReports.Any(dr => dr.DirectReports.Any());
        return hasManagerReports ? 8 : 15;
    }

    /// <summary>
    /// Gets the current span of control (number of direct reports).
    /// </summary>
    /// <returns>The number of direct reports.</returns>
    public int GetCurrentSpanOfControl()
    {
        return DirectReports?.Count ?? 0;
    }

    /// <summary>
    /// Validates if adding another direct report would exceed span of control limits.
    /// </summary>
    /// <param name="additionalReports">The number of additional reports to add.</param>
    /// <returns>A validation result containing status, error messages, and warnings.</returns>
    public SpanOfControlValidationResult ValidateSpanOfControl(int additionalReports = 0)
    {
        var result = new SpanOfControlValidationResult { IsValid = true };

        int maxSpan = GetMaxSpanOfControl();
        int currentSpan = GetCurrentSpanOfControl();
        int projectedSpan = currentSpan + additionalReports;

        result.MaxSpanOfControl = maxSpan;
        result.CurrentSpanOfControl = currentSpan;
        result.ProjectedSpanOfControl = projectedSpan;

        if (projectedSpan > maxSpan)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Span of control limit exceeded. Maximum: {maxSpan}, Projected: {projectedSpan}";
            return result;
        }

        double capacityPercentage = (double)projectedSpan / maxSpan * 100;
        if (capacityPercentage >= 80)
        {
            result.Warnings.Add($"Approaching span of control limit: {projectedSpan}/{maxSpan} ({capacityPercentage:F1}%)");
        }

        return result;
    }
}

/// <summary>
/// Result of span of control validation.
/// </summary>
public class SpanOfControlValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the span of control is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the error message if the validation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the list of warnings if the span of control is approaching limits.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets the maximum allowed span of control.
    /// </summary>
    public int MaxSpanOfControl { get; set; }

    /// <summary>
    /// Gets or sets the current span of control.
    /// </summary>
    public int CurrentSpanOfControl { get; set; }

    /// <summary>
    /// Gets or sets the projected span of control if additional reports were added.
    /// </summary>
    public int ProjectedSpanOfControl { get; set; }
}
