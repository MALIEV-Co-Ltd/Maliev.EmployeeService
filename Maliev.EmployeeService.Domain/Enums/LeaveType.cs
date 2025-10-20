namespace Maliev.EmployeeService.Domain.Enums;

/// <summary>
/// Types of leave available to employees
/// </summary>
public enum LeaveType
{
    /// <summary>
    /// Annual/vacation leave - Accrued based on tenure (10-20 days/year)
    /// </summary>
    AnnualLeave = 1,

    /// <summary>
    /// Sick leave for illness or medical appointments
    /// </summary>
    SickLeave = 2,

    /// <summary>
    /// Parental leave (maternity/paternity combined)
    /// </summary>
    ParentalLeave = 3,

    /// <summary>
    /// Unpaid leave
    /// </summary>
    UnpaidLeave = 4
}
