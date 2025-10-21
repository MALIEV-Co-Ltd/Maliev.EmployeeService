using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get current compensation details for an employee
/// Authorization: HR Specialist, Finance, System Admin only
/// </summary>
public record GetCompensationDetailsQuery(Guid EmployeeId);

/// <summary>
/// Handler for GetCompensationDetailsQuery with authorization checks
/// </summary>
public class GetCompensationDetailsQueryHandler
{
    private readonly ICompensationRepository _compensationRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCompensationDetailsQueryHandler(
        ICompensationRepository compensationRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService)
    {
        _compensationRepository = compensationRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CompensationDetailsDto?> HandleAsync(
        GetCompensationDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: Only HR Specialist, HR Generalist, System Administrator
        if (!_currentUserService.IsInRole("HRSpecialist") &&
            !_currentUserService.IsInRole("HRGeneralist") &&
            !_currentUserService.IsInRole("SystemAdministrator"))
        {
            throw new UnauthorizedAccessException(
                "Only HR personnel and System Administrators can access compensation details");
        }

        // Verify employee exists
        var employee = await _employeeRepository.GetByIdAsync(query.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return null;
        }

        // Get current compensation record
        var compensationRecord = await _compensationRepository.GetCurrentAsync(query.EmployeeId, cancellationToken);
        if (compensationRecord == null)
        {
            return null;
        }

        // Decrypt salary amount and map to DTO
        // Note: SalaryAmount is automatically decrypted by EncryptionInterceptor
        return new CompensationDetailsDto
        {
            Id = compensationRecord.Id,
            EmployeeId = compensationRecord.EmployeeId,
            SalaryAmount = decimal.Parse(compensationRecord.SalaryAmount), // Decrypted by interceptor
            Currency = compensationRecord.Currency,
            EffectiveDate = compensationRecord.EffectiveDate,
            ChangeReason = compensationRecord.ChangeReason,
            BonusStructure = compensationRecord.BonusStructure,
            CommissionStructure = compensationRecord.CommissionStructure,
            CreatedDate = compensationRecord.CreatedDate,
            CreatedBy = compensationRecord.CreatedBy
        };
    }
}
