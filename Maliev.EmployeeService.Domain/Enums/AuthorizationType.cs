namespace Maliev.EmployeeService.Domain.Enums;

/// <summary>
/// Type of work authorization
/// </summary>
public enum AuthorizationType
{
    /// <summary>
    /// Work permit issued by government authority
    /// </summary>
    WorkPermit = 1,

    /// <summary>
    /// Visa with work authorization
    /// </summary>
    Visa = 2,

    /// <summary>
    /// Citizenship providing unrestricted right to work
    /// </summary>
    Citizenship = 3
}
