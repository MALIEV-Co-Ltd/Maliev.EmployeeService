using Maliev.EmployeeService.Domain.Common;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Leave balance tracking for an employee by leave type and year
/// </summary>
public class LeaveBalance : Entity
{
    public Guid EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; }
    public int Year { get; set; }

    // Balance tracking
    public decimal TotalEntitlement { get; set; }
    public decimal UsedDays { get; set; }
    public decimal PendingDays { get; set; }
    public decimal CarryForwardDays { get; set; }

    // Expiration
    public DateTime? ExpiryDate { get; set; }

    // Navigation Properties
    public Employee? Employee { get; set; }

    // Computed Properties
    public decimal AvailableDays => TotalEntitlement + CarryForwardDays - UsedDays - PendingDays;
    public decimal RemainingDays => TotalEntitlement + CarryForwardDays - UsedDays;
    public bool HasExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;
}
