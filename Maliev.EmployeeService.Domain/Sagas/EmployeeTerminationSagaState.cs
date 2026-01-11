using MassTransit;

namespace Maliev.EmployeeService.Domain.Sagas;

/// <summary>
/// State object for the Employee Termination distributed transaction (Saga).
/// </summary>
public class EmployeeTerminationSagaState : SagaStateMachineInstance
{
    /// <summary>
    /// Gets or sets the correlation identifier for the saga instance.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the current state of the saga machine.
    /// </summary>
    public string CurrentState { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the employee being terminated.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Gets or sets the scheduled termination date.
    /// </summary>
    public DateTime TerminationDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the leave balance closure step is complete.
    /// </summary>
    public bool LeaveBalanceClosed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the compensation archive step is complete.
    /// </summary>
    public bool CompensationArchived { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the system access revocation step is complete.
    /// </summary>
    public bool AccessRevoked { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the saga was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the saga was last updated.
    /// </summary>
    public DateTime? ModifiedDate { get; set; }
}
