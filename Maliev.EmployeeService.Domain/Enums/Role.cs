namespace Maliev.EmployeeService.Domain.Enums;

/// <summary>
/// User roles for role-based access control (RBAC)
/// </summary>
public enum Role
{
    /// <summary>
    /// Standard employee with access to own data
    /// </summary>
    Employee = 1,

    /// <summary>
    /// Manager with access to direct reports' data
    /// </summary>
    Manager = 2,

    /// <summary>
    /// HR Generalist with broad HR operations access
    /// </summary>
    HRGeneralist = 3,

    /// <summary>
    /// HR Specialist with specialized HR functions access
    /// </summary>
    HRSpecialist = 4,

    /// <summary>
    /// System Administrator with full system access
    /// </summary>
    SystemAdministrator = 5
}
