namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for leave balance information
/// </summary>
public class LeaveBalanceDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal TotalEntitlement { get; set; }
    public decimal UsedDays { get; set; }
    public decimal PendingDays { get; set; }
    public decimal CarryForwardDays { get; set; }
    public decimal AvailableDays { get; set; }
    public decimal RemainingDays { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool HasExpired { get; set; }
}
