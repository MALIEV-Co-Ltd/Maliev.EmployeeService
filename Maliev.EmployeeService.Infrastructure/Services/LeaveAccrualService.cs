using Microsoft.Extensions.Logging;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Infrastructure.Services;

/// <summary>
/// Service for leave accrual calculations and processing
/// Implements tenure-based accrual rules
/// </summary>
public class LeaveAccrualService : ILeaveAccrualService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILeaveBalanceRepository _leaveBalanceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LeaveAccrualService> _logger;

    public LeaveAccrualService(
        IEmployeeRepository employeeRepository,
        ILeaveBalanceRepository leaveBalanceRepository,
        IUnitOfWork unitOfWork,
        ILogger<LeaveAccrualService> logger)
    {
        _employeeRepository = employeeRepository;
        _leaveBalanceRepository = leaveBalanceRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc/>
    public decimal CalculateAnnualEntitlement(DateTime startDate, DateTime? referenceDate = null)
    {
        var refDate = referenceDate ?? DateTime.UtcNow;
        var tenure = refDate - startDate;
        var yearsOfService = tenure.TotalDays / 365.25; // Account for leap years

        // Tenure-based accrual rules
        if (yearsOfService < 2)
        {
            return 10m; // 0-2 years: 10 days annually
        }
        else if (yearsOfService < 5)
        {
            return 15m; // 2-5 years: 15 days annually
        }
        else
        {
            return 20m; // 5+ years: 20 days annually
        }
    }

    /// <inheritdoc/>
    public decimal CalculateMonthlyAccrual(decimal annualEntitlement)
    {
        // Divide annual entitlement by 12 months
        // Round to 2 decimal places
        return Math.Round(annualEntitlement / 12m, 2);
    }

    /// <inheritdoc/>
    public async Task<int> ProcessMonthlyAccrualAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting monthly leave accrual processing");

        // Get all active employees
        var activeEmployees = await _employeeRepository.GetByStatusAsync(EmploymentStatus.Active, cancellationToken);
        var processedCount = 0;

        foreach (var employee in activeEmployees)
        {
            try
            {
                await ProcessEmployeeMonthlyAccrualAsync(employee, cancellationToken);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process monthly accrual for employee {EmployeeId}", employee.Id);
                // Continue processing other employees
            }
        }

        _logger.LogInformation("Completed monthly leave accrual processing. Processed {Count} employees", processedCount);
        return processedCount;
    }

    /// <inheritdoc/>
    public async Task ProcessEmployeeAnnualAccrualAsync(Guid employeeId, int year, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        if (employee == null)
        {
            _logger.LogWarning("Employee {EmployeeId} not found for annual accrual processing", employeeId);
            return;
        }

        // Calculate annual entitlement
        var annualEntitlement = CalculateAnnualEntitlement(employee.StartDate, new DateTime(year, 1, 1));

        // Get or create leave balance for AnnualLeave
        var leaveBalance = await _leaveBalanceRepository.GetByEmployeeLeaveTypeAndYearAsync(
            employeeId,
            LeaveType.AnnualLeave,
            year,
            cancellationToken);

        if (leaveBalance == null)
        {
            // Create new leave balance
            leaveBalance = new LeaveBalance
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                LeaveType = LeaveType.AnnualLeave,
                Year = year,
                TotalEntitlement = annualEntitlement,
                UsedDays = 0,
                PendingDays = 0,
                CarryForwardDays = 0,
                CreatedDate = DateTime.UtcNow
            };

            await _leaveBalanceRepository.AddAsync(leaveBalance, cancellationToken);
            _logger.LogInformation("Created annual leave balance for employee {EmployeeId}, year {Year}, entitlement {Entitlement}",
                employeeId, year, annualEntitlement);
        }
        else
        {
            // Update existing balance if entitlement changed (e.g., due to tenure milestone)
            if (leaveBalance.TotalEntitlement != annualEntitlement)
            {
                leaveBalance.TotalEntitlement = annualEntitlement;
                leaveBalance.ModifiedDate = DateTime.UtcNow;
                _leaveBalanceRepository.Update(leaveBalance);
                _logger.LogInformation("Updated annual leave balance for employee {EmployeeId}, year {Year}, new entitlement {Entitlement}",
                    employeeId, year, annualEntitlement);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Process monthly accrual for a specific employee
    /// </summary>
    private async Task ProcessEmployeeMonthlyAccrualAsync(Employee employee, CancellationToken cancellationToken)
    {
        var currentYear = DateTime.UtcNow.Year;
        var currentMonth = DateTime.UtcNow.Month;

        // Calculate monthly accrual
        var annualEntitlement = CalculateAnnualEntitlement(employee.StartDate);
        var monthlyAccrual = CalculateMonthlyAccrual(annualEntitlement);

        // Get or create leave balance for current year
        var leaveBalance = await _leaveBalanceRepository.GetByEmployeeLeaveTypeAndYearAsync(
            employee.Id,
            LeaveType.AnnualLeave,
            currentYear,
            cancellationToken);

        if (leaveBalance == null)
        {
            // Create new balance with first month's accrual
            leaveBalance = new LeaveBalance
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                LeaveType = LeaveType.AnnualLeave,
                Year = currentYear,
                TotalEntitlement = monthlyAccrual,
                UsedDays = 0,
                PendingDays = 0,
                CarryForwardDays = 0,
                CreatedDate = DateTime.UtcNow
            };

            await _leaveBalanceRepository.AddAsync(leaveBalance, cancellationToken);
        }
        else
        {
            // Add monthly accrual to existing balance
            leaveBalance.TotalEntitlement += monthlyAccrual;
            leaveBalance.ModifiedDate = DateTime.UtcNow;
            _leaveBalanceRepository.Update(leaveBalance);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Processed monthly accrual for employee {EmployeeId}: {MonthlyAccrual} days",
            employee.Id, monthlyAccrual);
    }
}
