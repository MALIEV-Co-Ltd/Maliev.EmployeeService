namespace Maliev.EmployeeService.Domain.Enums;

/// <summary>
/// Party responsible for completing onboarding/offboarding checklist items
/// </summary>
public enum ResponsibleParty
{
    /// <summary>
    /// HR department is responsible
    /// </summary>
    HR = 0,

    /// <summary>
    /// IT department is responsible
    /// </summary>
    IT = 1,

    /// <summary>
    /// Facilities department is responsible
    /// </summary>
    Facilities = 2,

    /// <summary>
    /// Employee's manager is responsible
    /// </summary>
    Manager = 3
}
