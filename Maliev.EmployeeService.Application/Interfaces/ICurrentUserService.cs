namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Service for accessing current authenticated user information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's principal ID (IAM)
    /// </summary>
    Guid? PrincipalId { get; }

    /// <summary>
    /// Gets the current user's employee ID (HR System)
    /// </summary>
    Task<Guid?> GetEmployeeIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current user's email
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Checks if the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }
}
