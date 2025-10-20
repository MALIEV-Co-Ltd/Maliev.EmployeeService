using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Api.Authorization;

/// <summary>
/// Authorization policy names for the Employee Service
/// </summary>
public static class Policies
{
    // Role-based policies
    public const string RequireAdminRole = "RequireAdminRole";
    public const string RequireHRRole = "RequireHRRole";
    public const string RequireManagerRole = "RequireManagerRole";
    public const string RequireEmployeeRole = "RequireEmployeeRole";

    // Combined policies
    public const string RequireHROrManager = "RequireHROrManager";
    public const string RequireHROrAdmin = "RequireHROrAdmin";
}

/// <summary>
/// Role names used throughout the Employee Service (string constants for JWT claims)
/// </summary>
public static class Roles
{
    public const string Admin = "SystemAdministrator";
    public const string HR = "HRGeneralist";
    public const string HRSpecialist = "HRSpecialist";
    public const string Manager = "Manager";
    public const string Employee = "Employee";

    /// <summary>
    /// Converts Role enum to string claim value
    /// </summary>
    public static string FromEnum(Role role) => role.ToString();

    /// <summary>
    /// Converts string claim value to Role enum
    /// </summary>
    public static Role ToEnum(string roleString) => Enum.Parse<Role>(roleString);
}
