namespace Maliev.EmployeeService.Domain.Authorization;

/// <summary>
/// Defines predefined roles for the Employee Service.
/// Roles follow the GCP format: roles.employee.{role-name}
/// Maps from old enum-based roles: SystemAdministrator, HRGeneralist, HRSpecialist, Manager, Employee
/// </summary>
public static class EmployeePredefinedRoles
{
    public const string SystemAdministrator = "roles.employee.system-administrator";
    public const string HRGeneralist = "roles.employee.hr-generalist";
    public const string HRSpecialist = "roles.employee.hr-specialist";
    public const string Manager = "roles.employee.manager";
    public const string Employee = "roles.employee.employee";

    /// <summary>
    /// Collection of all predefined roles for the Employee Service.
    /// </summary>
    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (SystemAdministrator, "Full system access to all employee operations", EmployeePermissions.All),

        (HRGeneralist, "Broad HR operations access", new[]
        {
            EmployeePermissions.ProfilesCreate,
            EmployeePermissions.ProfilesRead,
            EmployeePermissions.ProfilesUpdate,
            EmployeePermissions.DepartmentsManage,
            EmployeePermissions.TeamsManage,
            EmployeePermissions.ReportsView,
            EmployeePermissions.ReportsGenerate
        }),

        (HRSpecialist, "Specialized HR functions access", new[]
        {
            EmployeePermissions.ProfilesRead,
            EmployeePermissions.ProfilesUpdate,
            EmployeePermissions.ReportsView
        }),

        (Manager, "Access to direct reports' data", new[]
        {
            EmployeePermissions.ProfilesRead,
            EmployeePermissions.ReportsView
        }),

        (Employee, "Access to own employee data", new[]
        {
            EmployeePermissions.ProfilesRead,
            EmployeePermissions.ProfilesUpdate
        })
    };
}
