using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Api.Authorization;

/// <summary>
/// Authorization policy names for the Employee Service
/// </summary>
public static class Policies
{
    /// <summary>
    /// Policy requiring System Administrator role
    /// </summary>
    public const string RequireAdminRole = "RequireAdminRole";

    /// <summary>
    /// Policy requiring HR Generalist role
    /// </summary>
    public const string RequireHRRole = "RequireHRRole";

    /// <summary>
    /// Policy requiring Manager role
    /// </summary>
    public const string RequireManagerRole = "RequireManagerRole";

    /// <summary>
    /// Policy requiring Employee role
    /// </summary>
    public const string RequireEmployeeRole = "RequireEmployeeRole";

    /// <summary>
    /// Policy requiring either HR Generalist or Manager role
    /// </summary>
    public const string RequireHROrManager = "RequireHROrManager";

    /// <summary>
    /// Policy requiring either HR Generalist or System Administrator role
    /// </summary>
    public const string RequireHROrAdmin = "RequireHROrAdmin";
}

/// <summary>
/// Role names used throughout the Employee Service (string constants for JWT claims)
/// </summary>
public static class Roles
{
    /// <summary>
    /// System Administrator role
    /// </summary>
    public const string Admin = "SystemAdministrator";

    /// <summary>
    /// HR Generalist role
    /// </summary>
    public const string HR = "HRGeneralist";

    /// <summary>
    /// HR Specialist role
    /// </summary>
    public const string HRSpecialist = "HRSpecialist";

    /// <summary>
    /// Manager role
    /// </summary>
    public const string Manager = "Manager";

    /// <summary>
    /// Employee role
    /// </summary>
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
