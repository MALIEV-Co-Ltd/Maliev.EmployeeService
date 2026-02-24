namespace Maliev.EmployeeService.Domain.Enums;

/// <summary>
/// Certification validity status
/// </summary>
public enum CertificationStatus
{
    /// <summary>
    /// Certification is currently valid
    /// </summary>
    Valid = 1,

    /// <summary>
    /// Certification is expiring soon (within warning period)
    /// </summary>
    Expiring = 2,

    /// <summary>
    /// Certification has expired
    /// </summary>
    Expired = 3
}
