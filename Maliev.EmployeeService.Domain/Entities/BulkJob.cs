using Maliev.EmployeeService.Domain.Common;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Entity for tracking status of bulk/async operations
/// User Story 12 - Reporting, Analytics & Bulk Operations
/// </summary>
public class BulkJob : Entity
{
    /// <summary>
    /// Unique job identifier
    /// </summary>
    public Guid JobId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of bulk operation (Import, Export, BulkSalaryIncrease)
    /// </summary>
    public required string JobType { get; set; }

    /// <summary>
    /// Current status of the job
    /// </summary>
    public BulkJobStatus Status { get; set; } = BulkJobStatus.Pending;

    /// <summary>
    /// Total number of records to process
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Number of records processed successfully
    /// </summary>
    public int SuccessfulRecords { get; set; }

    /// <summary>
    /// Number of records that failed processing
    /// </summary>
    public int FailedRecords { get; set; }

    /// <summary>
    /// Detailed error messages (JSON array of error objects)
    /// </summary>
    public string? Errors { get; set; }

    /// <summary>
    /// Additional metadata about the job (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// When the job was initiated
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the job completed (successfully or with errors)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// User who initiated the job
    /// </summary>
    public required Guid InitiatedByUserId { get; set; }

    /// <summary>
    /// Result data (e.g., file path for export, summary for import)
    /// </summary>
    public string? ResultData { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public decimal ProgressPercentage => TotalRecords > 0
        ? Math.Round((decimal)(SuccessfulRecords + FailedRecords) / TotalRecords * 100, 1)
        : 0;
}

/// <summary>
/// Status values for bulk jobs
/// </summary>
public enum BulkJobStatus
{
    /// <summary>
    /// Job is queued but not yet started
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Job is currently being processed
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Job completed successfully (all records processed)
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job completed with some failures
    /// </summary>
    CompletedWithErrors = 3,

    /// <summary>
    /// Job failed completely
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Job was cancelled by user or system
    /// </summary>
    Cancelled = 5
}
