namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for leave utilization report
/// Analyzes leave accrual, usage, and carryover patterns
/// User Story 12 - Reporting & Analytics
/// </summary>
public class LeaveUtilizationReportDto
{
    /// <summary>
    /// Year analyzed
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Date the report was generated
    /// </summary>
    public DateTime GeneratedDate { get; set; }

    /// <summary>
    /// Total number of employees analyzed
    /// </summary>
    public int TotalEmployees { get; set; }

    /// <summary>
    /// Overall leave utilization statistics
    /// </summary>
    public LeaveUtilizationSummaryDto OverallUtilization { get; set; } = new();

    /// <summary>
    /// Utilization breakdown by leave type
    /// </summary>
    public List<LeaveTypeUtilizationDto> ByLeaveType { get; set; } = new();

    /// <summary>
    /// Utilization breakdown by department
    /// </summary>
    public List<DepartmentLeaveUtilizationDto> ByDepartment { get; set; } = new();

    /// <summary>
    /// Employees at risk of losing accrued leave (high balances approaching expiration)
    /// </summary>
    public List<EmployeeLeaveRiskDto> AtRiskOfExpiration { get; set; } = new();
}

/// <summary>
/// Overall leave utilization summary
/// </summary>
public class LeaveUtilizationSummaryDto
{
    /// <summary>
    /// Total days of leave entitlement across all employees
    /// </summary>
    public decimal TotalEntitlement { get; set; }

    /// <summary>
    /// Total days of leave used
    /// </summary>
    public decimal TotalUsed { get; set; }

    /// <summary>
    /// Total days pending approval
    /// </summary>
    public decimal TotalPending { get; set; }

    /// <summary>
    /// Total days carried forward from previous year
    /// </summary>
    public decimal TotalCarryForward { get; set; }

    /// <summary>
    /// Total days remaining (available + pending)
    /// </summary>
    public decimal TotalRemaining { get; set; }

    /// <summary>
    /// Overall utilization rate (used / entitlement * 100)
    /// </summary>
    public decimal UtilizationRate { get; set; }

    /// <summary>
    /// Average leave days used per employee
    /// </summary>
    public decimal AverageDaysUsedPerEmployee { get; set; }

    /// <summary>
    /// Average leave days remaining per employee
    /// </summary>
    public decimal AverageDaysRemainingPerEmployee { get; set; }
}

/// <summary>
/// Leave utilization statistics for a specific leave type
/// </summary>
public class LeaveTypeUtilizationDto
{
    public string LeaveType { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal TotalEntitlement { get; set; }
    public decimal TotalUsed { get; set; }
    public decimal TotalPending { get; set; }
    public decimal TotalRemaining { get; set; }
    public decimal UtilizationRate { get; set; }
    public decimal AverageDaysUsedPerEmployee { get; set; }
}

/// <summary>
/// Leave utilization statistics for a department
/// </summary>
public class DepartmentLeaveUtilizationDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal TotalEntitlement { get; set; }
    public decimal TotalUsed { get; set; }
    public decimal TotalRemaining { get; set; }
    public decimal UtilizationRate { get; set; }
    public decimal AverageDaysUsedPerEmployee { get; set; }
}

/// <summary>
/// Employee at risk of losing accrued leave
/// </summary>
public class EmployeeLeaveRiskDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public decimal RemainingDays { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
}
