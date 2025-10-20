using Maliev.EmployeeService.Domain.Common;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Represents an employee's benefits enrollment
/// Tracks health insurance, retirement contributions, and beneficiary information
/// </summary>
public class BenefitsEnrollment : Entity
{
    /// <summary>
    /// Employee ID this benefits enrollment belongs to
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Health insurance plan name or code
    /// (e.g., "Premium PPO", "Basic HMO", "Family Coverage")
    /// </summary>
    public string? HealthInsurancePlan { get; set; }

    /// <summary>
    /// Retirement contribution details
    /// (e.g., "401k - 5% employee + 3% employer match", "Provident Fund - 15%")
    /// </summary>
    public string? RetirementContribution { get; set; }

    /// <summary>
    /// Beneficiary information for life insurance and retirement accounts
    /// May contain multiple beneficiaries with percentages
    /// </summary>
    public string? BeneficiaryInformation { get; set; }

    /// <summary>
    /// Date when employee enrolled in these benefits
    /// </summary>
    public DateTime EnrollmentDate { get; set; }

    // Navigation Properties
    /// <summary>
    /// Employee this benefits enrollment belongs to
    /// </summary>
    public Employee Employee { get; set; } = null!;
}
