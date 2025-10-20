using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to update existing work authorization
/// </summary>
public class UpdateWorkAuthorizationCommand
{
    public Guid AuthorizationId { get; set; }
    public AuthorizationType AuthorizationType { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string IssuingAuthority { get; set; } = string.Empty;
    public SponsorshipStatus SponsorshipStatus { get; set; }
    public Guid? RightToWorkDocumentId { get; set; }
    public string? Notes { get; set; }
}
