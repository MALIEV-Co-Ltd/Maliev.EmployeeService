using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get training compliance report for HR analytics
/// </summary>
public class GetTrainingComplianceReportQuery
{
    public Guid? DepartmentId { get; set; }
    public TrainingType? TrainingType { get; set; }
    public bool OnlyOverdue { get; set; }
}

/// <summary>
/// DTO for training compliance report
/// </summary>
public class TrainingComplianceReportDto
{
    public int TotalEmployees { get; set; }
    public int EmployeesWithExpiredCertifications { get; set; }
    public int EmployeesWithExpiringCertifications { get; set; }
    public decimal ComplianceRate { get; set; }
    public List<EmployeeTrainingComplianceDto> EmployeeDetails { get; set; } = new();
}

/// <summary>
/// DTO for employee training compliance details
/// </summary>
public class EmployeeTrainingComplianceDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public int TotalTrainings { get; set; }
    public int ExpiredCertifications { get; set; }
    public int ExpiringCertifications { get; set; }
    public bool IsCompliant { get; set; }
}

/// <summary>
/// Handler for GetTrainingComplianceReportQuery
/// </summary>
public class GetTrainingComplianceReportQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ITrainingRepository _trainingRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ILogger<GetTrainingComplianceReportQueryHandler> _logger;

    public GetTrainingComplianceReportQueryHandler(
        IEmployeeRepository employeeRepository,
        ITrainingRepository trainingRepository,
        IDepartmentRepository departmentRepository,
        ILogger<GetTrainingComplianceReportQueryHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _trainingRepository = trainingRepository;
        _departmentRepository = departmentRepository;
        _logger = logger;
    }

    public async Task<TrainingComplianceReportDto> HandleAsync(GetTrainingComplianceReportQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating training compliance report");

        // Get active employees
        var employees = query.DepartmentId.HasValue
            ? await _employeeRepository.GetByDepartmentAsync(query.DepartmentId.Value, cancellationToken)
            : await _employeeRepository.GetByStatusAsync(EmploymentStatus.Active, cancellationToken);

        // Filter by active status if we filtered by department
        if (query.DepartmentId.HasValue)
        {
            employees = employees.Where(e => e.EmploymentStatus == EmploymentStatus.Active).ToList();
        }

        var employeeDetails = new List<EmployeeTrainingComplianceDto>();

        foreach (var employee in employees)
        {
            // Get training records for employee
            var trainings = await _trainingRepository.GetByEmployeeIdAsync(employee.Id, cancellationToken);

            // Filter by training type if specified
            if (query.TrainingType.HasValue)
            {
                trainings = trainings.Where(tr => tr.TrainingType == query.TrainingType.Value).ToList();
            }

            var expiredCount = trainings.Count(tr => tr.Status == CertificationStatus.Expired);
            var expiringCount = trainings.Count(tr => tr.Status == CertificationStatus.Expiring);
            var isCompliant = expiredCount == 0;

            if (query.OnlyOverdue && isCompliant)
            {
                continue; // Skip compliant employees if only showing overdue
            }

            // Get department name if available
            string departmentName = "Unknown";
            if (employee.DepartmentId.HasValue)
            {
                var department = await _departmentRepository.GetByIdAsync(employee.DepartmentId.Value, cancellationToken);
                departmentName = department?.Name ?? "Unknown";
            }

            employeeDetails.Add(new EmployeeTrainingComplianceDto
            {
                EmployeeId = employee.Id,
                EmployeeNumber = employee.EmployeeNumber,
                FullName = $"{employee.LegalName.FirstName} {employee.LegalName.LastName}",
                Department = departmentName,
                JobTitle = employee.JobTitle ?? "Unknown",
                TotalTrainings = trainings.Count(),
                ExpiredCertifications = expiredCount,
                ExpiringCertifications = expiringCount,
                IsCompliant = isCompliant
            });
        }

        var employeeCount = employees.Count();
        var report = new TrainingComplianceReportDto
        {
            TotalEmployees = employeeCount,
            EmployeesWithExpiredCertifications = employeeDetails.Count(e => e.ExpiredCertifications > 0),
            EmployeesWithExpiringCertifications = employeeDetails.Count(e => e.ExpiringCertifications > 0),
            ComplianceRate = employeeCount > 0
                ? Math.Round((decimal)employeeDetails.Count(e => e.IsCompliant) / employeeCount * 100, 2)
                : 100m,
            EmployeeDetails = employeeDetails.OrderBy(e => e.FullName).ToList()
        };

        _logger.LogInformation("Training compliance report generated: {TotalEmployees} employees, {ComplianceRate}% compliant",
            report.TotalEmployees, report.ComplianceRate);

        return report;
    }
}
