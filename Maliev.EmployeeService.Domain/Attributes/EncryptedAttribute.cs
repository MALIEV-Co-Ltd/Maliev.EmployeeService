namespace Maliev.EmployeeService.Domain.Attributes;

/// <summary>
/// Attribute to mark string properties that should be encrypted at rest
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class EncryptedAttribute : Attribute
{
}
