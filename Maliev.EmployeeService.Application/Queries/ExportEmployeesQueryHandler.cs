using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Maliev.EmployeeService.Application.Queries;

public class ExportEmployeesQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ExportEmployeesQueryHandler> _logger;

    public ExportEmployeesQueryHandler(
        IEmployeeRepository employeeRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService,
        ILogger<ExportEmployeesQueryHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ExportEmployeesResultDto> HandleAsync(
        ExportEmployeesQuery query,
        CancellationToken cancellationToken = default)
    {
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.ReportsGenerate, "employee/export", cancellationToken))
        {
            throw new UnauthorizedAccessException("No permission");
        }

        var allEmployees = await _employeeRepository.GetAllAsync(cancellationToken);
        var employees = allEmployees.ToList();

        if (!query.IncludeTerminated)
        {
            employees = employees
                .Where(e => e.EmploymentStatus != Domain.Enums.EmploymentStatus.Terminated)
                .ToList();
        }

        var csv = new StringBuilder();
        var headers = new List<string>
        {
            "EmployeeNumber", "FirstName", "LastName", "JobTitle", "Department"
        };

        csv.AppendLine(string.Join(",", headers));

        foreach (var employee in employees)
        {
            var row = new List<string>
            {
                employee.EmployeeNumber,
                employee.LegalName?.FirstName ?? "",
                employee.LegalName?.LastName ?? "",
                employee.JobTitle ?? "",
                employee.Department?.Name ?? ""
            };

            csv.AppendLine(string.Join(",", row));
        }

        return new ExportEmployeesResultDto
        {
            CsvContent = csv.ToString(),
            TotalEmployees = employees.Count,
            FileName = "employees_export.csv",
            GeneratedAt = DateTime.UtcNow
        };
    }
}
