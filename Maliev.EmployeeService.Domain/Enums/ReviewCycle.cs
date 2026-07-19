namespace Maliev.EmployeeService.Domain.Enums;

/// <summary>
/// Represents the frequency of performance review cycles
/// </summary>
public enum ReviewCycle
{
    /// <summary>
    /// Review conducted every three months (4 times per year)
    /// </summary>
    Quarterly = 1,

    /// <summary>
    /// Review conducted every six months (2 times per year)
    /// </summary>
    SemiAnnual = 2,

    /// <summary>
    /// Review conducted annually (once per year)
    /// </summary>
    Annual = 3
}
