namespace Maliev.EmployeeService.Domain.ValueObjects;

/// <summary>
/// Value object representing a person's legal name
/// </summary>
public class LegalName
{
    /// <summary>
    /// First name (given name)
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name (surname/family name)
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Middle name (optional)
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>
    /// Full name combining all components
    /// </summary>
    public string FullName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {MiddleName} {LastName}";

    /// <summary>
    /// Parameterless constructor for EF Core
    /// </summary>
    public LegalName()
    {
    }

    /// <summary>
    /// Creates a legal name with required first and last names
    /// </summary>
    public LegalName(string firstName, string lastName, string? middleName = null)
    {
        FirstName = firstName;
        LastName = lastName;
        MiddleName = middleName;
    }

    /// <summary>
    /// Validates that both first and last names are provided
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName);
    }
}
