using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Service for accessing current authenticated user information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's employee ID
    /// </summary>
    Guid? EmployeeId { get; }

    /// <summary>
    /// Gets the current user's email
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the current user's roles
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Gets the current user's primary role as Role enum
    /// </summary>
    Role PrimaryRole { get; }

    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Checks if the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }
}
