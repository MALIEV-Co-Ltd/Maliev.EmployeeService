using System.Text;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for ExportEmployeesQuery
/// Generates CSV export of employee data with privacy controls
/// User Story 12 - Bulk Operations
/// </summary>
public class ExportEmployeesQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICompensationRepository _compensationRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public ExportEmployeesQueryHandler(
        IEmployeeRepository employeeRepository,
        ICompensationRepository compensationRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _compensationRepository = compensationRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Handles the query to export employees to CSV
    /// </summary>
    public async Task<ExportEmployeesResultDto> HandleAsync(
        ExportEmployeesQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have ReportsGenerate permission for export
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.ReportsGenerate, "employee/export", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to export employee data");
        }

        // Additional checks for sensitive data flags
        if (query.IncludePersonalData &&
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.ProfilesRead, "employee/export/personal", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to include personal data in exports");
        }

        if (query.IncludeSalaryData &&
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.CompensationRead, "employee/export/salary", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to include salary data in exports");
        }

        // Get all employees
        var allEmployees = await _employeeRepository.GetAllAsync(cancellationToken);
        var employees = allEmployees.ToList();

        // Apply filters
        if (!query.IncludeTerminated)
        {
            employees = employees
                .Where(e => e.EmploymentStatus == Domain.Enums.EmploymentStatus.Active)
                .ToList();
        }

        if (query.DepartmentId.HasValue)
        {
            employees = employees
                .Where(e => e.DepartmentId == query.DepartmentId.Value)
                .ToList();
        }

        if (query.EmploymentStatus.HasValue)
        {
            employees = employees
                .Where(e => e.EmploymentStatus == query.EmploymentStatus.Value)
                .ToList();
        }

        // Build CSV
        var csv = new StringBuilder();

        // Build header based on options
        var headers = new List<string>
        {
            "EmployeeNumber",
            "FirstName",
            "LastName",
            "PreferredName",
            "JobTitle",
            "Department",
            "EmploymentType",
            "EmploymentStatus",
            "StartDate",
            "TerminationDate",
            "ManagerEmployeeNumber"
        };

        if (query.IncludePersonalData)
        {
            headers.AddRange(new[]
            {
                "WorkEmail",
                "PersonalEmail",
                "MobilePhone",
                "DateOfBirth",
                "Nationality"
            });
        }

        if (query.IncludeSalaryData)
        {
            headers.AddRange(new[]
            {
                "CurrentSalary",
                "Currency",
                "SalaryEffectiveDate"
            });
        }

        csv.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

        // Build data rows
        foreach (var employee in employees)
        {
            var row = new List<string>
            {
                employee.EmployeeNumber,
                employee.LegalName?.FirstName ?? string.Empty,
                employee.LegalName?.LastName ?? string.Empty,
                employee.PreferredName ?? string.Empty,
                employee.JobTitle ?? string.Empty,
                employee.Department?.Name ?? string.Empty,
                employee.EmploymentType.ToString(),
                employee.EmploymentStatus.ToString(),
                employee.StartDate.ToString("yyyy-MM-dd"),
                employee.TerminationDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                employee.Manager?.EmployeeNumber ?? string.Empty
            };

            if (query.IncludePersonalData)
            {
                row.AddRange(new[]
                {
                    employee.ContactInformation?.WorkEmail ?? string.Empty,
                    employee.ContactInformation?.PersonalEmail ?? string.Empty,
                    employee.ContactInformation?.MobilePhone ?? string.Empty,
                    employee.DateOfBirth != default(DateTime) ? employee.DateOfBirth.ToString("yyyy-MM-dd") : string.Empty,
                    employee.Nationality ?? string.Empty
                });
            }

            if (query.IncludeSalaryData)
            {
                // Get current compensation
                var currentComp = await _compensationRepository.GetCurrentAsync(employee.Id, cancellationToken);
                row.AddRange(new[]
                {
                    currentComp?.SalaryAmount ?? string.Empty,
                    currentComp?.Currency ?? string.Empty,
                    currentComp?.EffectiveDate.ToString("yyyy-MM-dd") ?? string.Empty
                });
            }

            csv.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = $"employees_export_{timestamp}.csv";

        return new ExportEmployeesResultDto
        {
            CsvContent = csv.ToString(),
            TotalEmployees = employees.Count,
            FileName = fileName,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Escapes CSV field content (wraps in quotes if contains comma, quote, or newline)
    /// </summary>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            // Escape quotes by doubling them
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}
