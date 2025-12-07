namespace Maliev.EmployeeService.Application.DTOs;

///<summary>
/// DTO for leave utilization report
/// Analyzes leave accrual, usage, and carryover patterns
/// User Story 12 - Reporting &amp; Analytics
/// </summary>
public class LeaveUtilizationReportDto
{
    ///<summary>
    /// Year analyzed
    /// </summary>
    public int Year { get; set; }

    ///<summary>
    /// Date the report was generated
    /// </summary>
    public DateTime GeneratedDate { get; set; }

    ///<summary>
    /// Total number of employees analyzed
    /// </summary>
    public int TotalEmployees { get; set; }

    ///<summary>
    /// Overall leave utilization statistics
    /// </summary>
    public LeaveUtilizationSummaryDto OverallUtilization { get; set; } = new();

    ///<summary>
    /// Utilization breakdown by leave type
    /// </summary>
    public List<LeaveTypeUtilizationDto> ByLeaveType { get; set; } = new();

    ///<summary>
    /// Utilization breakdown by department
    /// </summary>
    public List<DepartmentLeaveUtilizationDto> ByDepartment { get; set; } = new();

    ///<summary>
    /// Employees at risk of losing accrued leave (high balances approaching expiration)
    /// </summary>
    public List<EmployeeLeaveRiskDto> AtRiskOfExpiration { get; set; } = new();
}

///<summary>
/// Overall leave utilization summary
/// </summary>
public class LeaveUtilizationSummaryDto
{
    ///<summary>
    /// Total days of leave entitlement across all employees
    /// </summary>
    public decimal TotalEntitlement { get; set; }

    ///<summary>
    /// Total days of leave used
    /// </summary>
    public decimal TotalUsed { get; set; }

    ///<summary>
    /// Total days pending approval
    /// </summary>
    public decimal TotalPending { get; set; }

    ///<summary>
    /// Total days carried forward from previous year
    /// </summary>
    public decimal TotalCarryForward { get; set; }

    ///<summary>
    /// Total days remaining (available + pending)
    /// </summary>
    public decimal TotalRemaining { get; set; }

    ///<summary>
    /// Overall utilization rate (used / entitlement * 100)
    /// </summary>
    public decimal UtilizationRate { get; set; }

    ///<summary>
    /// Average leave days used per employee
    /// </summary>
    public decimal AverageDaysUsedPerEmployee { get; set; }

    ///<summary>
    /// Average leave days remaining per employee
    /// </summary>
    public decimal AverageDaysRemainingPerEmployee { get; set; }
}

///<summary>
/// Leave utilization statistics for a specific leave type
/// </summary>
public class LeaveTypeUtilizationDto
{
    ///<summary>
    /// The type of leave.
    /// </summary>
    public string LeaveType { get; set; } = string.Empty;
    ///<summary>
    /// The number of employees using this leave type.
    /// </summary>
    public int EmployeeCount { get; set; }
    ///<summary>
    /// The total leave entitlement for this type.
    /// </summary>
    public decimal TotalEntitlement { get; set; }
    ///<summary>
    /// The total leave used for this type.
    /// </summary>
    public decimal TotalUsed { get; set; }
    ///<summary>
    /// The total pending leave for this type.
    /// </summary>
    public decimal TotalPending { get; set; }
    ///<summary>
    /// The total remaining leave for this type.
    /// </summary>
    public decimal TotalRemaining { get; set; }
    ///<summary>
    /// The utilization rate for this leave type.
    /// </summary>
    public decimal UtilizationRate { get; set; }
    ///<summary>
    /// The average leave days used per employee for this type.
    /// </summary>
    public decimal AverageDaysUsedPerEmployee { get; set; }
}

///<summary>
/// Leave utilization statistics for a department
/// </summary>
public class DepartmentLeaveUtilizationDto
{
    ///<summary>
    /// The ID of the department.
    /// </summary>
    public Guid DepartmentId { get; set; }
    ///<summary>
    /// The name of the department.
    /// </summary>
    public string DepartmentName { get; set; } = string.Empty;
    ///<summary>
    /// The number of employees in the department.
    /// </summary>
    public int EmployeeCount { get; set; }
    ///<summary>
    /// The total leave entitlement for the department.
    /// </summary>
    public decimal TotalEntitlement { get; set; }
    ///<summary>
    /// The total leave used by the department.
    /// </summary>
    public decimal TotalUsed { get; set; }
    ///<summary>
    /// The total remaining leave for the department.
    /// </summary>
    public decimal TotalRemaining { get; set; }
    ///<summary>
    /// The utilization rate for the department.
    /// </summary>
    public decimal UtilizationRate { get; set; }
    ///<summary>
    /// The average leave days used per employee in the department.
    /// </summary>
    public decimal AverageDaysUsedPerEmployee { get; set; }
}

///<summary>
/// Employee at risk of losing accrued leave
/// </summary>
public class EmployeeLeaveRiskDto
{
    ///<summary>
    /// The ID of the employee.
    /// </summary>
    public Guid EmployeeId { get; set; }
    ///<summary>
    /// The name of the employee.
    /// </summary>
    public string EmployeeName { get; set; } = string.Empty;
    ///<summary>
    /// The name of the department the employee belongs to.
    /// </summary>
    public string? DepartmentName { get; set; }
    ///<summary>
    /// The type of leave.
    /// </summary>
    public string LeaveType { get; set; } = string.Empty;
    ///<summary>
    /// The number of remaining leave days.
    /// </summary>
    public decimal RemainingDays { get; set; }
    ///<summary>
    /// The expiry date of the leave.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }
    ///<summary>
    /// The number of days until the leave expires.
    /// </summary>
    public int DaysUntilExpiry { get; set; }
}
