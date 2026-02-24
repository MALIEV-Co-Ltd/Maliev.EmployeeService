namespace Maliev.EmployeeService.Domain.ValueObjects;

/// <summary>
/// Value object representing contact information
/// </summary>
public class ContactInformation
{
    /// <summary>
    /// Mobile phone number
    /// </summary>
    public string? MobilePhone { get; set; }

    /// <summary>
    /// Personal email address
    /// </summary>
    public string? PersonalEmail { get; set; }

    /// <summary>
    /// Work email address (usually company domain)
    /// </summary>
    public string WorkEmail { get; set; } = string.Empty;

    /// <summary>
    /// Parameterless constructor for EF Core
    /// </summary>
    public ContactInformation()
    {
    }

    /// <summary>
    /// Creates contact information with required work email
    /// </summary>
    public ContactInformation(string workEmail, string? mobilePhone = null, string? personalEmail = null)
    {
        WorkEmail = workEmail;
        MobilePhone = mobilePhone;
        PersonalEmail = personalEmail;
    }

    /// <summary>
    /// Validates that at least work email is provided
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(WorkEmail);
    }
}
