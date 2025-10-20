using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get complete compensation history for an employee
/// Authorization: HR Specialist, Finance, System Admin only
/// </summary>
public record GetCompensationHistoryQuery(Guid EmployeeId);

/// <summary>
/// Handler for GetCompensationHistoryQuery with authorization checks
/// </summary>
public class GetCompensationHistoryQueryHandler
{
    private readonly ICompensationRepository _compensationRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCompensationHistoryQueryHandler(
        ICompensationRepository compensationRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService)
    {
        _compensationRepository = compensationRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<CompensationDetailsDto>> HandleAsync(
        GetCompensationHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: Only HR Specialist, HR Generalist, System Administrator
        if (!_currentUserService.IsInRole("HRSpecialist") &&
            !_currentUserService.IsInRole("HRGeneralist") &&
            !_currentUserService.IsInRole("SystemAdministrator"))
        {
            throw new UnauthorizedAccessException(
                "Only HR personnel and System Administrators can access compensation history");
        }

        // Verify employee exists
        var employee = await _employeeRepository.GetByIdAsync(query.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return Enumerable.Empty<CompensationDetailsDto>();
        }

        // Get compensation history
        var compensationRecords = await _compensationRepository.GetHistoryAsync(query.EmployeeId, cancellationToken);

        // Map to DTOs with decrypted salary amounts
        // Note: SalaryAmount is automatically decrypted by EncryptionInterceptor
        return compensationRecords.Select(record => new CompensationDetailsDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            SalaryAmount = decimal.Parse(record.SalaryAmount), // Decrypted by interceptor
            Currency = record.Currency,
            EffectiveDate = record.EffectiveDate,
            ChangeReason = record.ChangeReason,
            BonusStructure = record.BonusStructure,
            CommissionStructure = record.CommissionStructure,
            CreatedDate = record.CreatedDate,
            CreatedBy = record.CreatedBy
        });
    }
}
