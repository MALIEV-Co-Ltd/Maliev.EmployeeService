namespace Maliev.EmployeeService.Application.Authorization;

/// <summary>
/// Provides access to predefined roles for the Employee Service.
/// </summary>
public static class EmployeePredefinedRoles
{
    public const string Admin = "roles.employee.admin";
    public const string HR = "roles.employee.hr";
    public const string Manager = "roles.employee.manager";
    public const string Viewer = "roles.employee.viewer";

    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (
            Admin,
            "Employee Administrator with full access",
            new[]
            {
                EmployeePermissions.EmployeeRead,
                EmployeePermissions.EmployeeWrite,
                EmployeePermissions.ProfileRead,
                EmployeePermissions.ProfileWrite,
                EmployeePermissions.DocumentCreate,
                EmployeePermissions.DocumentRead,
                EmployeePermissions.DocumentDelete,
            }
        ),
        (
            HR,
            "HR role with employee management access",
            new[]
            {
                EmployeePermissions.EmployeeRead,
                EmployeePermissions.EmployeeWrite,
                EmployeePermissions.ProfileRead,
                EmployeePermissions.ProfileWrite,
                EmployeePermissions.DocumentCreate,
                EmployeePermissions.DocumentRead,
                EmployeePermissions.DocumentDelete,
            }
        ),
        (
            Manager,
            "Manager role with employee and profile read access",
            new[]
            {
                EmployeePermissions.EmployeeRead,
                EmployeePermissions.ProfileRead,
                EmployeePermissions.ProfileWrite,
                EmployeePermissions.DocumentCreate,
                EmployeePermissions.DocumentRead,
            }
        ),
        (
            Viewer,
            "Employee Viewer with read-only access",
            new[]
            {
                EmployeePermissions.EmployeeRead,
                EmployeePermissions.ProfileRead,
                EmployeePermissions.DocumentRead,
            }
        ),
    };
}
