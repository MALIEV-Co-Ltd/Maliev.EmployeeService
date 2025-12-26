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
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="logger">Logger instance.</param>
    public EmployeeIAMRegistrationService(
        IHttpClientFactory httpClientFactory,
        ILogger<EmployeeIAMRegistrationService> logger)
        : base(httpClientFactory, logger, "employee")
    {
    }

    /// <summary>
    /// Gets all permissions for the Employee Service.
    /// </summary>
    /// <returns>Collection of permission registrations.</returns>
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return EmployeePermissions.All.Select(p => new PermissionRegistration
        {
            PermissionId = p,
            Description = GetPermissionDescription(p)
        });
    }

    /// <summary>
    /// Gets all predefined roles for the Employee Service.
    /// </summary>
    /// <returns>Collection of role registrations.</returns>
    protected override IEnumerable<Maliev.Aspire.ServiceDefaults.IAM.RoleRegistration> GetPredefinedRoles()
    {
        return EmployeePredefinedRoles.All.Select(r => new Maliev.Aspire.ServiceDefaults.IAM.RoleRegistration
        {
            RoleId = r.RoleId,
            Description = r.Description,
            PermissionIds = r.Permissions.ToList(),
            IsCustom = false
        });
    }

    private static string GetPermissionDescription(string permission)
    {
        return permission switch
        {
            EmployeePermissions.ProfilesCreate => "Create employee profiles",
            EmployeePermissions.ProfilesRead => "Read employee profiles",
            EmployeePermissions.ProfilesUpdate => "Update employee profiles",
            EmployeePermissions.ProfilesDelete => "Delete employee profiles",
            EmployeePermissions.CompensationRead => "Read compensation data",
            EmployeePermissions.CompensationUpdate => "Update compensation data",
            EmployeePermissions.DepartmentsManage => "Manage departments",
            EmployeePermissions.DocumentsCreate => "Create employee documents",
            EmployeePermissions.DocumentsRead => "Read employee documents",
            EmployeePermissions.DocumentsUpdate => "Update employee documents",
            EmployeePermissions.DocumentsDelete => "Delete employee documents",
            EmployeePermissions.LeaveCreate => "Create leave requests",
            EmployeePermissions.LeaveRead => "Read leave requests",
            EmployeePermissions.LeaveApprove => "Approve leave requests",
            EmployeePermissions.LeaveCancel => "Cancel leave requests",
            EmployeePermissions.PerformanceCreate => "Create performance reviews",
            EmployeePermissions.PerformanceRead => "Read performance reviews",
            EmployeePermissions.PerformanceUpdate => "Update performance reviews",
            EmployeePermissions.TrainingCreate => "Create training records",
            EmployeePermissions.TrainingRead => "Read training records",
            EmployeePermissions.TrainingAssign => "Assign training",
            EmployeePermissions.TeamsRead => "Read team information",
            EmployeePermissions.TeamsManage => "Manage teams",
            EmployeePermissions.EmployeeSearch => "Search employees",
            EmployeePermissions.OnboardingManage => "Manage employee onboarding and offboarding",
            EmployeePermissions.WorkAuthManage => "Manage work authorization documents",
            EmployeePermissions.ReportsView => "View reports",
            EmployeePermissions.ReportsGenerate => "Generate reports",
            EmployeePermissions.AdminBackgroundJobs => "Manage background jobs",
            EmployeePermissions.AdminSystemConfig => "Manage system configuration",
            EmployeePermissions.SystemAdmin => "Full system administrative access",
            _ => $"Permission: {permission}"
        };
    }
}