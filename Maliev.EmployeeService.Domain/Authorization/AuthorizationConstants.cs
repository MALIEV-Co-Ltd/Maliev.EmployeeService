namespace Maliev.EmployeeService.Domain.Authorization;

/// <summary>
/// Role names used throughout the Employee Service (GCP-style format for JWT claims)
/// Migrated from enum-based roles to GCP-style format: roles.{service}.{role-name}
/// </summary>
public static class Roles
{
    /// <summary>
    /// System Administrator role (GCP format, was: SystemAdministrator)
    /// </summary>
    public const string Admin = "roles.employee.system-administrator";

    /// <summary>
    /// HR Generalist role (GCP format, was: HRGeneralist)
    /// </summary>
    public const string HR = "roles.employee.hr-generalist";

    /// <summary>
    /// HR Specialist role (GCP format, was: HRSpecialist)
    /// </summary>
    public const string HRSpecialist = "roles.employee.hr-specialist";

    /// <summary>
    /// Manager role (GCP format, was: Manager)
    /// </summary>
    public const string Manager = "roles.employee.manager";

    /// <summary>
    /// Employee role (GCP format, was: Employee)
    /// </summary>
    public const string Employee = "roles.employee.employee";
}
