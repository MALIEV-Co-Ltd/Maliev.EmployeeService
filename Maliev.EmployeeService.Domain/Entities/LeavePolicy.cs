using Maliev.EmployeeService.Domain.Common;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Leave policy configuration for different leave types
/// Defines accrual rules, carryover limits, and blackout periods
/// </summary>
public class LeavePolicy : Entity
{
    public LeaveType LeaveType { get; set; }

    /// <summary>
    /// Annual accrual rate in days per year (e.g., 10, 15, 20)
    /// </summary>
    public decimal AccrualRate { get; set; }

    /// <summary>
    /// Maximum days that can be carried forward to next year
    /// </summary>
    public int? MaxCarryover { get; set; }

    /// <summary>
    /// Minimum notice period in days before leave start date
    /// </summary>
    public int MinimumNoticeDays { get; set; } = 30;

    /// <summary>
    /// Blackout periods when leave cannot be taken (e.g., Dec 25-31)
    /// Stored as JSON array: [{"Start":"12-25","End":"12-31"}]
    /// </summary>
    public string? BlackoutPeriodsJson { get; set; }

    /// <summary>
    /// Whether this leave type requires manager approval
    /// </summary>
    public bool RequiresApproval { get; set; } = true;

    /// <summary>
    /// Policy is active and should be applied
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Policy effective date
    /// </summary>
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
}
