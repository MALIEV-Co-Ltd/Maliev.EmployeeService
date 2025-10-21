using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for work authorization information
/// </summary>
public class WorkAuthorizationDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public AuthorizationType AuthorizationType { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string IssuingAuthority { get; set; } = string.Empty;
    public SponsorshipStatus SponsorshipStatus { get; set; }
    public Guid? RightToWorkDocumentId { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public int? DaysUntilExpiration { get; set; }
}
