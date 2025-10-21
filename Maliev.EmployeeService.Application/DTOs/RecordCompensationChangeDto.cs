namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for recording a compensation change
/// </summary>
public class RecordCompensationChangeDto
{
    /// <summary>
    /// Base salary amount (will be encrypted)
    /// </summary>
    public decimal SalaryAmount { get; set; }

    /// <summary>
    /// Currency code (e.g., USD, EUR, THB)
    /// </summary>
    public string Currency { get; set; } = "THB";

    /// <summary>
    /// Date when this compensation becomes effective
    /// </summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Reason for compensation change (e.g., "Annual Review", "Promotion", "Market Adjustment")
    /// </summary>
    public string ChangeReason { get; set; } = string.Empty;

    /// <summary>
    /// Bonus structure details (optional)
    /// </summary>
    public string? BonusStructure { get; set; }

    /// <summary>
    /// Commission structure details (optional)
    /// </summary>
    public string? CommissionStructure { get; set; }
}
