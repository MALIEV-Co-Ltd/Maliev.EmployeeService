using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetEmployeeProfileQuery
/// Phase 16 - T386: Implements distributed caching for frequently accessed employee profiles
/// </summary>
public class GetEmployeeProfileQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICacheService? _cacheService;
    private readonly ILogger<GetEmployeeProfileQueryHandler> _logger;

    // Cache configuration
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);
    private const string CacheKeyPrefix = "employee:profile:";

    public GetEmployeeProfileQueryHandler(
        IEmployeeRepository employeeRepository,
        ILogger<GetEmployeeProfileQueryHandler> logger,
        ICacheService? cacheService = null) // Optional to support environments without Redis
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<GetEmployeeProfileQueryResult> HandleAsync(
        GetEmployeeProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        var cacheKey = $"{CacheKeyPrefix}{query.EmployeeId}";

        if (_cacheService != null)
        {
            try
            {
                var cachedProfile = await _cacheService.GetAsync<EmployeeProfileDto>(cacheKey, cancellationToken);
                if (cachedProfile != null)
                {
                    _logger.LogDebug("Employee profile cache hit for {EmployeeId}", query.EmployeeId);
                    return new GetEmployeeProfileQueryResult(cachedProfile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving employee profile from cache for {EmployeeId}", query.EmployeeId);
                // Continue to database query
            }
        }

        var employee = await _employeeRepository.GetWithDetailsAsync(query.EmployeeId, cancellationToken);

        if (employee == null)
        {
            return new GetEmployeeProfileQueryResult(null);
        }

        // Map employee with emergency contacts
        var employeeWithContacts = await _employeeRepository.GetWithEmergencyContactsAsync(
            query.EmployeeId,
            cancellationToken);

        var profileDto = new EmployeeProfileDto
        {
            Id = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,

            // Legal Name
            FirstName = employee.LegalName.FirstName,
            LastName = employee.LegalName.LastName,
            MiddleName = employee.LegalName.MiddleName,
            FullName = employee.FullName,

            // Personal Information
            PreferredName = employee.PreferredName,
            DateOfBirth = employee.DateOfBirth,
            Age = employee.Age,
            Nationality = employee.Nationality,

            // Contact Information
            WorkEmail = employee.ContactInformation.WorkEmail,
            PersonalEmail = employee.ContactInformation.PersonalEmail,
            MobilePhone = employee.ContactInformation.MobilePhone,

            // Employment Information
            EmploymentType = employee.EmploymentType.ToString(),
            EmploymentStatus = employee.EmploymentStatus.ToString(),
            JobTitle = employee.JobTitle,
            WorkLocation = employee.WorkLocation,

            // Employment Dates
            StartDate = employee.StartDate,
            ProbationEndDate = employee.ProbationEndDate,
            TerminationDate = employee.TerminationDate,
            TenureInYears = employee.TenureInYears,

            // Department and Manager
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department?.Name,
            ManagerId = employee.ManagerId,
            ManagerName = employee.Manager?.FullName,

            // Emergency Contacts
            EmergencyContacts = employeeWithContacts?.EmergencyContacts
                .Select(ec => new EmergencyContactDto
                {
                    Id = ec.Id,
                    EmployeeId = ec.EmployeeId,
                    ContactName = ec.ContactName,
                    Relationship = ec.Relationship,
                    PhoneNumber = ec.PhoneNumber,
                    Email = ec.Email,
                    PriorityOrder = ec.PriorityOrder
                })
                .OrderBy(ec => ec.PriorityOrder)
                .ToList() ?? new List<EmergencyContactDto>()
        };

        // Cache the result
        if (_cacheService != null)
        {
            try
            {
                await _cacheService.SetAsync(
                    cacheKey,
                    profileDto,
                    absoluteExpiration: CacheExpiration,
                    cancellationToken: cancellationToken);

                _logger.LogDebug("Cached employee profile for {EmployeeId}", query.EmployeeId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error caching employee profile for {EmployeeId}", query.EmployeeId);
                // Don't fail the request if caching fails
            }
        }

        return new GetEmployeeProfileQueryResult(profileDto);
    }
}
