namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for work authorization compliance report
/// </summary>
public class WorkAuthorizationComplianceReportDto
{
    /// <summary>
    /// Total active work authorizations
    /// </summary>
    public int TotalActive { get; set; }

    /// <summary>
    /// Work authorizations expiring within threshold
    /// </summary>
    public int ExpiringSoon { get; set; }

    /// <summary>
    /// Expired work authorizations
    /// </summary>
    public int Expired { get; set; }

    /// <summary>
    /// List of expiring authorizations with employee details
    /// </summary>
    public List<ExpiringAuthorizationDto> ExpiringAuthorizations { get; set; } = new();

    /// <summary>
    /// List of expired authorizations with employee details
    /// </summary>
    public List<ExpiringAuthorizationDto> ExpiredAuthorizations { get; set; } = new();

    /// <summary>
    /// Sponsorship status summary
    /// </summary>
    public Dictionary<string, int> SponsorshipStatusSummary { get; set; } = new();
}

/// <summary>
/// DTO for expiring/expired authorization details
/// </summary>
public class ExpiringAuthorizationDto
{
    public Guid AuthorizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string AuthorizationType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public int? DaysUntilExpiration { get; set; }
    public string Department { get; set; } = string.Empty;
}
