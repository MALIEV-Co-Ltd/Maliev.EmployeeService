using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Service interface for leave accrual calculations and processing
/// </summary>
public interface ILeaveAccrualService
{
    /// <summary>
    /// Calculate annual leave entitlement based on tenure
    /// Rules: 0-2 years=10 days, 2-5 years=15 days, 5+ years=20 days
    /// </summary>
    /// <param name="startDate">Employee start date</param>
    /// <param name="referenceDate">Date to calculate tenure from (default: today)</param>
    /// <returns>Annual leave entitlement in days</returns>
    decimal CalculateAnnualEntitlement(DateTime startDate, DateTime? referenceDate = null);

    /// <summary>
    /// Calculate monthly accrual based on annual entitlement
    /// Divides annual entitlement by 12 months
    /// </summary>
    /// <param name="annualEntitlement">Annual leave entitlement in days</param>
    /// <returns>Monthly accrual in days</returns>
    decimal CalculateMonthlyAccrual(decimal annualEntitlement);

    /// <summary>
    /// Process monthly leave accrual for all active employees
    /// Called by background job on the 1st of each month
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of employees processed</returns>
    Task<int> ProcessMonthlyAccrualAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Process annual leave accrual for a specific employee
    /// Creates or updates leave balance for the year
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="year">Year to process accrual for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ProcessEmployeeAnnualAccrualAsync(Guid employeeId, int year, CancellationToken cancellationToken = default);
}
