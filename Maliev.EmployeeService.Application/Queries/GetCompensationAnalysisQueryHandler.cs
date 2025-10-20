using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetCompensationAnalysisQuery
/// Provides anonymized salary statistics and ranges
/// User Story 12 - Reporting & Analytics
/// </summary>
public class GetCompensationAnalysisQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICompensationRepository _compensationRepository;

    public GetCompensationAnalysisQueryHandler(
        IEmployeeRepository employeeRepository,
        ICompensationRepository compensationRepository)
    {
        _employeeRepository = employeeRepository;
        _compensationRepository = compensationRepository;
    }

    /// <summary>
    /// Handles the query to generate compensation analysis
    /// </summary>
    public async Task<CompensationAnalysisDto> HandleAsync(
        GetCompensationAnalysisQuery query,
        CancellationToken cancellationToken = default)
    {
        var asOfDate = query.AsOfDate ?? DateTime.UtcNow;

        // Get all active employees
        var allEmployees = await _employeeRepository.GetAllAsync(cancellationToken);
        var employeesQuery = allEmployees
            .Where(e => e.EmploymentStatus == EmploymentStatus.Active)
            .Where(e => e.StartDate <= asOfDate)
            .Where(e => !e.TerminationDate.HasValue || e.TerminationDate.Value > asOfDate);

        // Apply filters
        if (query.DepartmentId.HasValue)
        {
            employeesQuery = employeesQuery.Where(e => e.DepartmentId == query.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.JobTitle))
        {
            var titleLower = query.JobTitle.ToLower();
            employeesQuery = employeesQuery.Where(e =>
                e.JobTitle != null && e.JobTitle.ToLower().Contains(titleLower));
        }

        var employees = employeesQuery.ToList();

        // Get current compensation for all employees
        var compensationData = new List<(Guid EmployeeId, Guid? DepartmentId, string? DepartmentName, string? JobTitle, decimal Salary, string Currency)>();

        foreach (var employee in employees)
        {
            var currentComp = await _compensationRepository.GetCurrentAsync(employee.Id, cancellationToken);
            if (currentComp != null && decimal.TryParse(currentComp.SalaryAmount, out var salary))
            {
                compensationData.Add((
                    employee.Id,
                    employee.DepartmentId,
                    employee.Department?.Name,
                    employee.JobTitle,
                    salary,
                    currentComp.Currency
                ));
            }
        }

        var report = new CompensationAnalysisDto
        {
            AsOfDate = asOfDate,
            TotalEmployees = compensationData.Count
        };

        // Overall statistics
        if (compensationData.Any())
        {
            var allSalaries = compensationData.Select(c => c.Salary).ToList();
            var currency = compensationData.First().Currency; // Assume single currency for now

            report.OverallStatistics = CalculateStatistics(allSalaries, currency);
        }

        // By department
        var departmentGroups = compensationData
            .Where(c => c.DepartmentId.HasValue && !string.IsNullOrEmpty(c.DepartmentName))
            .GroupBy(c => new { c.DepartmentId, c.DepartmentName })
            .Select(g =>
            {
                var salaries = g.Select(c => c.Salary).ToList();
                var currency = g.First().Currency;

                return new DepartmentCompensationDto
                {
                    DepartmentId = g.Key.DepartmentId!.Value,
                    DepartmentName = g.Key.DepartmentName!,
                    EmployeeCount = g.Count(),
                    Statistics = CalculateStatistics(salaries, currency)
                };
            })
            .OrderByDescending(d => d.Statistics.AverageSalary)
            .ToList();

        report.ByDepartment = departmentGroups;

        // By job title
        var jobTitleGroups = compensationData
            .Where(c => !string.IsNullOrWhiteSpace(c.JobTitle))
            .GroupBy(c => c.JobTitle!)
            .Select(g =>
            {
                var salaries = g.Select(c => c.Salary).ToList();
                var currency = g.First().Currency;

                return new JobTitleCompensationDto
                {
                    JobTitle = g.Key,
                    EmployeeCount = g.Count(),
                    Statistics = CalculateStatistics(salaries, currency)
                };
            })
            .OrderByDescending(j => j.Statistics.AverageSalary)
            .Take(20) // Top 20 job titles
            .ToList();

        report.ByJobTitle = jobTitleGroups;

        return report;
    }

    private static SalaryStatisticsDto CalculateStatistics(List<decimal> salaries, string currency)
    {
        if (!salaries.Any())
        {
            return new SalaryStatisticsDto { Currency = currency };
        }

        var sortedSalaries = salaries.OrderBy(s => s).ToList();

        return new SalaryStatisticsDto
        {
            MinSalary = Math.Round(sortedSalaries.First(), 2),
            MaxSalary = Math.Round(sortedSalaries.Last(), 2),
            AverageSalary = Math.Round(salaries.Average(), 2),
            MedianSalary = Math.Round(GetPercentile(sortedSalaries, 50), 2),
            Percentile25 = Math.Round(GetPercentile(sortedSalaries, 25), 2),
            Percentile75 = Math.Round(GetPercentile(sortedSalaries, 75), 2),
            Currency = currency
        };
    }

    private static decimal GetPercentile(List<decimal> sortedValues, double percentile)
    {
        if (!sortedValues.Any()) return 0;
        if (sortedValues.Count == 1) return sortedValues[0];

        var index = (percentile / 100.0) * (sortedValues.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);

        if (lower == upper)
        {
            return sortedValues[lower];
        }

        var weight = index - lower;
        return sortedValues[lower] * (1 - (decimal)weight) + sortedValues[upper] * (decimal)weight;
    }
}
