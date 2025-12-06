using System.ComponentModel.DataAnnotations;

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for recording a compensation change
/// </summary>
public class RecordCompensationChangeDto
{
    /// <summary>
    /// Base salary amount (will be encrypted)
    /// </summary>
    [Range(0.01, (double)decimal.MaxValue)]
    public decimal SalaryAmount { get; set; }

    /// <summary>
    /// Currency code (e.g., USD, EUR, THB)
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "THB";

    /// <summary>
    /// Date when this compensation becomes effective
    /// </summary>
    [Required]
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Reason for compensation change (e.g., "Annual Review", "Promotion", "Market Adjustment")
    /// </summary>
    [Required]
    [StringLength(500)]
    public string ChangeReason { get; set; } = string.Empty;

    /// <summary>
    /// Bonus structure details (optional)
    /// </summary>
    [StringLength(1000)]
    public string? BonusStructure { get; set; }

    /// <summary>
    /// Commission structure details (optional)
    /// </summary>
    [StringLength(1000)]
    public string? CommissionStructure { get; set; }
}
