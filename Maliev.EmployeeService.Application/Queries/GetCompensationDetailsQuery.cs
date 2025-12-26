using Maliev.EmployeeService.Domain.Authorization;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.Aspire.ServiceDefaults.IAM;
using Microsoft.Extensions.Configuration;

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
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;

    public GetCompensationDetailsQueryHandler(
        ICompensationRepository compensationRepository,
        IEmployeeRepository employeeRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _compensationRepository = compensationRepository;
        _employeeRepository = employeeRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    public async Task<CompensationDetailsDto?> HandleAsync(
        GetCompensationDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have CompensationRead permission
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.CompensationRead, $"employee/{query.EmployeeId}", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to access compensation details for this employee");
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
