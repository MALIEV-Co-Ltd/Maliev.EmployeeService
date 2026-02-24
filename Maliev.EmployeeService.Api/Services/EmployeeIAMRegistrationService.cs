using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;

namespace Maliev.EmployeeService.Api.Services;

/// <summary>
/// Background service that registers Employee Service permissions and roles with IAM.
/// Uses the standard IAMRegistrationService base class.
/// </summary>
public class EmployeeIAMRegistrationService : IAMRegistrationService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeIAMRegistrationService"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public EmployeeIAMRegistrationService(
        IConfiguration configuration,
        ILogger<EmployeeIAMRegistrationService> logger)
        : base(configuration, logger, "employee")
    {
    }

    /// <summary>
    /// Gets all permissions for the Employee Service.
    /// </summary>
    /// <returns>Collection of permission registrations.</returns>
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return EmployeePermissions.AllWithDescriptions.Select(p => new PermissionRegistration
        {
            PermissionId = p.Key,
            Description = p.Value
        });
    }

    /// <summary>
    /// Gets all predefined roles for the Employee Service.
    /// </summary>
    /// <returns>Collection of role registrations.</returns>
    protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
    {
        return EmployeePredefinedRoles.All.Select(r => new RoleRegistration
        {
            RoleId = r.RoleId,
            Description = r.Description,
            PermissionIds = r.Permissions.ToList(),
            IsCustom = false
        });
    }
}
