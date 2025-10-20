namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for updating benefits enrollment
/// </summary>
public class UpdateBenefitsEnrollmentDto
{
    /// <summary>
    /// Health insurance plan name or code
    /// </summary>
    public string? HealthInsurancePlan { get; set; }

    /// <summary>
    /// Retirement contribution details
    /// </summary>
    public string? RetirementContribution { get; set; }

    /// <summary>
    /// Beneficiary information for life insurance and retirement accounts
    /// </summary>
    public string? BeneficiaryInformation { get; set; }

    /// <summary>
    /// Enrollment date
    /// </summary>
    public DateTime EnrollmentDate { get; set; }
}
