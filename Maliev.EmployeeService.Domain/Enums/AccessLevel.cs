namespace Maliev.EmployeeService.Domain.Enums;

/// <summary>
/// Access level for documents determining who can view/download them
/// Implements role-based document access control
/// </summary>
public enum AccessLevel
{
    /// <summary>
    /// Document is accessible to everyone (public information)
    /// </summary>
    Public = 0,

    /// <summary>
    /// Document is accessible only to the employee it belongs to
    /// </summary>
    Employee = 1,

    /// <summary>
    /// Document is accessible to the employee and their manager
    /// </summary>
    Manager = 2,

    /// <summary>
    /// Document is accessible only to HR roles (HR Manager, HR Specialist, Admin)
    /// </summary>
    HROnly = 3,

    /// <summary>
    /// Document is accessible only to HR Specialist and Admin roles
    /// Highest restriction level for sensitive documents (e.g., disciplinary records)
    /// </summary>
    HRSpecialistOnly = 4
}
