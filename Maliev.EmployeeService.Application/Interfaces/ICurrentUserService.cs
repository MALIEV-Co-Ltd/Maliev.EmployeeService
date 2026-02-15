namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Service for accessing current authenticated user information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's principal ID (IAM) as a GUID if available
    /// </summary>
    Guid? PrincipalId { get; }

    /// <summary>
    /// Gets the current user's principal identifier (string sub claim)
    /// </summary>
    string? PrincipalIdentifier { get; }

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

    /// <summary>
    /// Checks if the current user has the specified permission (via JWT claims)
    /// </summary>
    /// <param name="permission">The permission to check</param>
    /// <returns>True if the user has the permission, otherwise false</returns>
    bool HasPermission(string permission);
}
