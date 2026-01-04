using Maliev.EmployeeService.Application.Interfaces;
using Maliev.Aspire.ServiceDefaults.Caching;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Handler for UpdateEmployeeProfileCommand
/// Phase 16 - T386: Implements cache invalidation for employee profile updates
/// </summary>
public class UpdateEmployeeProfileCommandHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService? _cacheService;
    private readonly ILogger<UpdateEmployeeProfileCommandHandler> _logger;

    private const string EmployeeProfileCacheKeyPrefix = "employee:profile:";
    private const string OrgChartCacheKeyPrefix = "orgchart:";

    public UpdateEmployeeProfileCommandHandler(
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateEmployeeProfileCommandHandler> logger,
        ICacheService? cacheService = null)
    {
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<UpdateEmployeeProfileCommandResult> HandleAsync(
        UpdateEmployeeProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);

        if (employee == null)
        {
            return new UpdateEmployeeProfileCommandResult(false, "Employee not found");
        }

        // Update only allowed fields (employees can only update their contact info and preferred name)
        employee.ContactInformation.PersonalEmail = command.UpdateDto.PersonalEmail;
        employee.ContactInformation.MobilePhone = command.UpdateDto.MobilePhone;
        employee.PreferredName = command.UpdateDto.PreferredName;

        _employeeRepository.Update(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache for this employee's profile
        if (_cacheService != null)
        {
            try
            {
                var cacheKey = $"{EmployeeProfileCacheKeyPrefix}{command.EmployeeId}";
                await _cacheService.RemoveAsync(cacheKey, cancellationToken);
                _logger.LogDebug("Invalidated employee profile cache for {EmployeeId}", command.EmployeeId);

                // If preferred name changed, invalidate org charts that might contain this employee
                if (!string.IsNullOrEmpty(command.UpdateDto.PreferredName) &&
                    command.UpdateDto.PreferredName != employee.PreferredName)
                {
                    // Note: Full pattern-based invalidation would require Redis directly
                    // For now, log that org charts may be stale
                    _logger.LogInformation(
                        "Employee {EmployeeId} preferred name changed. Org charts may be stale until cache expires.",
                        command.EmployeeId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache for employee {EmployeeId}", command.EmployeeId);
                // Don't fail the request if cache invalidation fails
            }
        }

        return new UpdateEmployeeProfileCommandResult(true);
    }
}
