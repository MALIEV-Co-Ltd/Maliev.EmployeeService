using Maliev.EmployeeService.Domain.Attributes;
using Maliev.EmployeeService.Domain.Common;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Core Employee entity representing an employee in the organization
/// </summary>
public class Employee : Entity
{
    public Guid PrincipalId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;

    // Basic Information - Using Value Objects
    public LegalName LegalName { get; set; } = new();
    public string? PreferredName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? Nationality { get; set; }

    // Contact Information - Using Value Object
    public ContactInformation ContactInformation { get; set; } = new();

    // Employment Information - Using Enums
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;
    public string? JobTitle { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }
    public Guid? DottedLineManagerId { get; set; } // Matrix reporting - secondary/dotted-line manager
    public string? WorkLocation { get; set; }

    // Employment Dates
    public DateTime StartDate { get; set; }
    public DateTime? ProbationEndDate { get; set; }
    public DateTime? TerminationDate { get; set; }

    // Sensitive Information (encrypted at rest)
    [Encrypted]
    public string? NationalId { get; set; }

    // Career Integration
    public Guid? JobApplicationId { get; set; } // Link to Career Service application

    // Navigation Properties
    public Department? Department { get; set; }
    public Employee? Manager { get; set; }
    public Employee? DottedLineManager { get; set; }
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();
    public ICollection<Employee> DottedLineReports { get; set; } = new List<Employee>();
    public ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();
    public ICollection<EmployeeTeamAssignment> TeamAssignments { get; set; } = new List<EmployeeTeamAssignment>();
    public ICollection<CompensationRecord> CompensationRecords { get; set; } = new List<CompensationRecord>();
    public ICollection<BenefitsEnrollment> BenefitsEnrollments { get; set; } = new List<BenefitsEnrollment>();
    public ICollection<PerformanceReview> PerformanceReviews { get; set; } = new List<PerformanceReview>();
    public ICollection<PerformanceReview> ConductedReviews { get; set; } = new List<PerformanceReview>();
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
    public ICollection<PerformanceImprovementPlan> PerformanceImprovementPlans { get; set; } = new List<PerformanceImprovementPlan>();
    public ICollection<PerformanceImprovementPlan> ManagedPIPs { get; set; } = new List<PerformanceImprovementPlan>();

    // Computed Properties
    public string FullName => LegalName?.FullName ?? string.Empty;
    public string DisplayName => PreferredName ?? LegalName?.FirstName ?? string.Empty;
    public int? Age => DateOfBirth != default ? (int?)((DateTime.Today - DateOfBirth).TotalDays / 365.25) : null;
    public int? TenureInYears => StartDate != default ? (int?)((DateTime.Today - StartDate).TotalDays / 365.25) : null;
    public bool IsActive => EmploymentStatus == EmploymentStatus.Active;

    // Business Logic - Manager Validation
    /// <summary>
    /// Validates that assigning a manager would not create a circular reporting relationship
    /// Prevents scenarios like: A reports to B, B reports to C, C reports to A
    /// </summary>
    public bool WouldCreateCircularRelationship(Guid proposedManagerId, Func<Guid, Employee?> getEmployeeById)
    {
        if (Id == proposedManagerId)
        {
            // Employee cannot be their own manager
            return true;
        }

        // Traverse up the proposed manager's reporting chain
        var currentManager = getEmployeeById(proposedManagerId);
        var visited = new HashSet<Guid> { Id }; // Start with current employee

        while (currentManager != null && currentManager.ManagerId.HasValue)
        {
            if (visited.Contains(currentManager.ManagerId.Value))
            {
                // Found a cycle in the proposed chain
                return true;
            }

            visited.Add(currentManager.ManagerId.Value);
            currentManager = getEmployeeById(currentManager.ManagerId.Value);
        }

        return false;
    }

    // Business Logic - Span of Control
    /// <summary>
    /// Gets the maximum allowed span of control (direct reports) for this manager
    /// - 15 for IC (individual contributor) managers
    /// - 8 for manager-of-managers
    /// </summary>
    public int GetMaxSpanOfControl()
    {
        if (DirectReports == null || !DirectReports.Any())
        {
            // Not a manager yet, use IC manager limit
            return 15;
        }

        // Check if any direct reports are themselves managers
        bool hasManagerReports = DirectReports.Any(dr => dr.DirectReports.Any());
        return hasManagerReports ? 8 : 15;
    }

    /// <summary>
    /// Gets the current span of control (number of direct reports)
    /// </summary>
    public int GetCurrentSpanOfControl()
    {
        return DirectReports?.Count ?? 0;
    }

    /// <summary>
    /// Validates if adding another direct report would exceed span of control limits
    /// Returns validation result with warnings at 80% capacity
    /// </summary>
    public SpanOfControlValidationResult ValidateSpanOfControl(int additionalReports = 0)
    {
        var result = new SpanOfControlValidationResult { IsValid = true };

        int maxSpan = GetMaxSpanOfControl();
        int currentSpan = GetCurrentSpanOfControl();
        int projectedSpan = currentSpan + additionalReports;

        result.MaxSpanOfControl = maxSpan;
        result.CurrentSpanOfControl = currentSpan;
        result.ProjectedSpanOfControl = projectedSpan;

        // Check if exceeded limit
        if (projectedSpan > maxSpan)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Span of control limit exceeded. Maximum: {maxSpan}, Projected: {projectedSpan}";
            return result;
        }

        // Warning at 80% capacity
        double capacityPercentage = (double)projectedSpan / maxSpan * 100;
        if (capacityPercentage >= 80)
        {
            result.Warnings.Add($"Approaching span of control limit: {projectedSpan}/{maxSpan} ({capacityPercentage:F1}%)");
        }

        return result;
    }
}

/// <summary>
/// Result of span of control validation
/// </summary>
public class SpanOfControlValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
    public int MaxSpanOfControl { get; set; }
    public int CurrentSpanOfControl { get; set; }
    public int ProjectedSpanOfControl { get; set; }
}
