namespace Maliev.EmployeeService.Api.Models;

/// <summary>
/// Request for credential validation
/// </summary>
public class ValidateCredentialsRequest
{
    /// <summary>
    /// The username or email address.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The password.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
