namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for bulk job status information
/// User Story 12 - Bulk Operations
/// </summary>
public class BulkJobStatusDto
{
    /// <summary>
    /// Unique job identifier
    /// </summary>
    public required Guid JobId { get; set; }

    /// <summary>
    /// Type of bulk operation
    /// </summary>
    public required string JobType { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public required string Status { get; set; }

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
    /// Progress percentage (0-100)
    /// </summary>
    public decimal ProgressPercentage { get; set; }

    /// <summary>
    /// List of error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// When the job was initiated
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the job completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// User who initiated the job
    /// </summary>
    public Guid InitiatedByPrincipalId { get; set; }

    /// <summary>
    /// Result data (e.g., download link for exports)
    /// </summary>
    public string? ResultData { get; set; }
}
