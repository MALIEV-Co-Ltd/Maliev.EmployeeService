using System.ComponentModel.DataAnnotations;

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for updating benefits enrollment
/// </summary>
public class UpdateBenefitsEnrollmentDto
{
    /// <summary>
    /// Health insurance plan name or code
    /// </summary>
    [StringLength(200)]
    public string? HealthInsurancePlan { get; set; }

    /// <summary>
    /// Retirement contribution details
    /// </summary>
    [StringLength(500)]
    public string? RetirementContribution { get; set; }

    /// <summary>
    /// Beneficiary information for life insurance and retirement accounts
    /// </summary>
    [StringLength(2000)]
    public string? BeneficiaryInformation { get; set; }

    /// <summary>
    /// Enrollment date
    /// </summary>
    [Required]
    public DateTime EnrollmentDate { get; set; }
}
