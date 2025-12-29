namespace Maliev.EmployeeService.Domain.Authorization;

/// <summary>Represents a role registration request for the IAM service.</summary>
public record RoleRegistration
{
    /// <summary>The unique identifier for the role (GCP format: roles.{service}.{role-name}).</summary>
    public required string RoleId { get; init; }

    /// <summary>The display name of the role.</summary>
    public required string RoleName { get; init; }

    /// <summary>A description of the role's purpose.</summary>
    public required string Description { get; init; }

    /// <summary>The list of permissions assigned to this role.</summary>
    public required string[] Permissions { get; init; }
}

/// <summary>
/// Defines predefined roles for the Employee Service.
/// Roles follow the GCP format: roles.{service}.{role-name}
/// Maps from old enum-based roles: SystemAdministrator, HRGeneralist, HRSpecialist, Manager, Employee
/// </summary>
public static class EmployeePredefinedRoles
{
    /// <summary>System Administrator: Full system access (was: SystemAdministrator).</summary>
    public static readonly RoleRegistration SystemAdministrator = new()
    {
        RoleId = "roles.employee.system-administrator",
        RoleName = "System Administrator",
        Description = "Full system access to all employee operations",
        Permissions = EmployeePermissions.All
    };

    /// <summary>HR Generalist: Broad HR operations access (was: HRGeneralist).</summary>
    public static readonly RoleRegistration HRGeneralist = new()
    {
        RoleId = "roles.employee.hr-generalist",
        RoleName = "HR Generalist",
        Description = "Broad HR operations access",
        Permissions = new[]
        {
            EmployeePermissions.ProfilesCreate,
            EmployeePermissions.ProfilesRead,
            EmployeePermissions.ProfilesUpdate,
            EmployeePermissions.DepartmentsManage,
            EmployeePermissions.TeamsManage,
            EmployeePermissions.ReportsView,
            EmployeePermissions.ReportsGenerate
        }
    };

    /// <summary>HR Specialist: Specialized HR functions access (was: HRSpecialist).</summary>
    public static readonly RoleRegistration HRSpecialist = new()
    {
        RoleId = "roles.employee.hr-specialist",
        RoleName = "HR Specialist",
        Description = "Specialized HR functions access",
        Permissions = new[]
        {
            EmployeePermissions.ProfilesRead,
            EmployeePermissions.ProfilesUpdate,
            EmployeePermissions.ReportsView
        }
    };

    /// <summary>Manager: Access to direct reports' data (was: Manager).</summary>
    public static readonly RoleRegistration Manager = new()
    {
        RoleId = "roles.employee.manager",
        RoleName = "Manager",
        Description = "Access to direct reports' data",
        Permissions = new[]
        {
            EmployeePermissions.ProfilesRead,
            EmployeePermissions.ReportsView
        }
    };

    /// <summary>Employee: Access to own data (was: Employee).</summary>
    public static readonly RoleRegistration Employee = new()
    {
        RoleId = "roles.employee.employee",
        RoleName = "Employee",
        Description = "Access to own employee data",
        Permissions = new[]
        {
            EmployeePermissions.ProfilesRead,
            EmployeePermissions.ProfilesUpdate  // own profile only
        }
    };

    /// <summary>All predefined roles.</summary>
    public static readonly RoleRegistration[] All = new[]
    {
        SystemAdministrator, HRGeneralist, HRSpecialist, Manager, Employee
    };
}