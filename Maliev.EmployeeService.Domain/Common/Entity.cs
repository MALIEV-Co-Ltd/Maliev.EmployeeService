namespace Maliev.EmployeeService.Domain.Common;

/// <summary>
/// Base entity class with common audit properties
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Timestamp when the entity was created
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// User ID who created the entity
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Timestamp when the entity was last modified
    /// </summary>
    public DateTime? ModifiedDate { get; set; }

    /// <summary>
    /// User ID who last modified the entity
    /// </summary>
    public Guid? ModifiedBy { get; set; }
}
