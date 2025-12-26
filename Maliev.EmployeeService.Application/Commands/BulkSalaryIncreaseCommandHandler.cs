using System.Text.Json;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Handler for BulkSalaryIncreaseCommand
/// Applies salary increases with preview capability
/// User Story 12 - Bulk Operations
/// </summary>
public class BulkSalaryIncreaseCommandHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICompensationRepository _compensationRepository;
    private readonly IBulkJobRepository _bulkJobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public BulkSalaryIncreaseCommandHandler(
        IEmployeeRepository employeeRepository,
        ICompensationRepository compensationRepository,
        IBulkJobRepository bulkJobRepository,
        IUnitOfWork unitOfWork,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _compensationRepository = compensationRepository;
        _bulkJobRepository = bulkJobRepository;
        _unitOfWork = unitOfWork;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    public async Task<BulkSalaryIncreaseResultDto> HandleAsync(
        BulkSalaryIncreaseCommand command,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have CompensationUpdate permission
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) || 
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.CompensationUpdate, "employee/compensation", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to perform bulk salary increases");
        }
        // Validate percentage
        if (command.PercentageIncrease <= 0 || command.PercentageIncrease > 100)
        {
            throw new ArgumentException("Percentage increase must be between 0 and 100", nameof(command.PercentageIncrease));
        }

        // Get employees
        var allEmployees = await _employeeRepository.GetAllAsync(cancellationToken);
        var employees = allEmployees
            .Where(e => e.EmploymentStatus == EmploymentStatus.Active)
            .ToList();

        // Filter by department if specified
        if (command.DepartmentId.HasValue)
        {
            employees = employees
                .Where(e => e.DepartmentId == command.DepartmentId.Value)
                .ToList();
        }

        var changes = new List<SalaryChangePreviewDto>();
        var totalIncreaseCost = 0m;
        var currency = "THB";

        // Calculate changes for each employee
        foreach (var employee in employees)
        {
            var currentComp = await _compensationRepository.GetCurrentAsync(employee.Id, cancellationToken);
            if (currentComp == null)
            {
                // Skip employees with no compensation record
                continue;
            }

            // Parse current salary
            if (!decimal.TryParse(currentComp.SalaryAmount, out var currentSalary))
            {
                // Skip if salary can't be parsed
                continue;
            }

            currency = currentComp.Currency; // Use employee's currency

            // Calculate new salary
            var increaseAmount = Math.Round(currentSalary * (command.PercentageIncrease / 100), 2);
            var newSalary = currentSalary + increaseAmount;

            changes.Add(new SalaryChangePreviewDto
            {
                EmployeeId = employee.Id,
                EmployeeNumber = employee.EmployeeNumber,
                EmployeeName = employee.LegalName?.FullName ?? "Unknown",
                Department = employee.Department?.Name ?? "Unknown",
                CurrentSalary = currentSalary,
                NewSalary = newSalary,
                IncreaseAmount = increaseAmount,
                PercentageIncrease = command.PercentageIncrease
            });

            totalIncreaseCost += increaseAmount;
        }

        // If preview mode, just return the preview
        if (command.PreviewOnly)
        {
            return new BulkSalaryIncreaseResultDto
            {
                TotalEmployees = changes.Count,
                TotalIncreaseCost = totalIncreaseCost,
                Currency = currency,
                Changes = changes,
                IsPreview = true,
                Message = $"Preview: {changes.Count} employees would receive a {command.PercentageIncrease}% increase"
            };
        }

        // Execute the salary increase
        var job = new BulkJob
        {
            JobType = "BulkSalaryIncrease",
            Status = BulkJobStatus.Processing,
            TotalRecords = changes.Count,
            InitiatedByUserId = command.InitiatedByUserId,
            StartedAt = DateTime.UtcNow,
            Metadata = JsonSerializer.Serialize(new
            {
                command.DepartmentId,
                command.PercentageIncrease,
                command.Reason,
                command.EffectiveDate
            })
        };

        await _bulkJobRepository.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var errors = new List<string>();
        var successCount = 0;
        var failCount = 0;

        try
        {
            foreach (var change in changes)
            {
                try
                {
                    // Create new compensation record
                    var newCompRecord = new CompensationRecord
                    {
                        EmployeeId = change.EmployeeId,
                        SalaryAmount = change.NewSalary.ToString("F2"),
                        Currency = currency,
                        EffectiveDate = command.EffectiveDate,
                        ChangeReason = command.Reason
                    };

                    await _compensationRepository.AddAsync(newCompRecord, cancellationToken);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Employee {change.EmployeeNumber}: {ex.Message}");
                    failCount++;
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Update job status
            job.SuccessfulRecords = successCount;
            job.FailedRecords = failCount;
            job.Status = failCount == 0 ? BulkJobStatus.Completed : BulkJobStatus.CompletedWithErrors;
            job.Errors = errors.Any() ? JsonSerializer.Serialize(errors) : null;
            job.CompletedAt = DateTime.UtcNow;
            job.ResultData = JsonSerializer.Serialize(new
            {
                TotalEmployees = successCount,
                TotalIncreaseCost = totalIncreaseCost,
                Currency = currency
            });

            _bulkJobRepository.Update(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new BulkSalaryIncreaseResultDto
            {
                TotalEmployees = successCount,
                TotalIncreaseCost = totalIncreaseCost,
                Currency = currency,
                Changes = changes,
                IsPreview = false,
                JobId = job.JobId,
                Message = $"Salary increase applied to {successCount} employees. {failCount} errors."
            };
        }
        catch (Exception ex)
        {
            job.Status = BulkJobStatus.Failed;
            job.Errors = JsonSerializer.Serialize(new[] { $"Bulk salary increase failed: {ex.Message}" });
            job.CompletedAt = DateTime.UtcNow;
            _bulkJobRepository.Update(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}
