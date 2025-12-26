namespace Maliev.EmployeeService.Api.Models;

/// <summary>
/// Response for credential validation (US3)
/// </summary>
public record CredentialValidationResponse
{
    /// <summary>
    /// Whether the credentials are valid
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// The principal identity (IAM) or legacy fallback ID
    /// </summary>
    public Guid PrincipalId { get; init; } // Changed from UserId to PrincipalId

    /// <summary>
    /// The user's email address
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// The user's full name
    /// </summary>
    public string? Name { get; init; }
}

/// <summary>
/// Request for credential validation
/// </summary>
public record ValidateCredentialsRequest(string Email, string Password);
