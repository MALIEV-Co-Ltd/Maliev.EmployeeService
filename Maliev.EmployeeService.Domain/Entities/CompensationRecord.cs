using Maliev.EmployeeService.Domain.Attributes;
using Maliev.EmployeeService.Domain.Common;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Represents a compensation record with encrypted salary information
/// Tracks salary changes, bonus structures, and commission plans over time
/// </summary>
public class CompensationRecord : Entity
{
    /// <summary>
    /// Employee ID this compensation record belongs to
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Base salary amount (encrypted at rest for PII protection)
    /// </summary>
    [Encrypted]
    public string SalaryAmount { get; set; } = string.Empty;

    /// <summary>
    /// Currency code (e.g., USD, EUR, THB)
    /// ISO 4217 currency code
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
    /// Bonus structure details (e.g., "10% annual performance bonus")
    /// </summary>
    public string? BonusStructure { get; set; }

    /// <summary>
    /// Commission structure details (e.g., "5% on sales over $100K")
    /// </summary>
    public string? CommissionStructure { get; set; }

    // Navigation Properties
    /// <summary>
    /// Employee this compensation record belongs to
    /// </summary>
    public Employee Employee { get; set; } = null!;
}
