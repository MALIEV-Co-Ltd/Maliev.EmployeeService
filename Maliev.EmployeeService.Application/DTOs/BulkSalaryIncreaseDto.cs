namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for bulk salary increase request
/// User Story 12 - Bulk Operations
/// </summary>
public class BulkSalaryIncreaseDto
{
    /// <summary>
    /// Department to apply increase (optional - if null, applies to all)
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Percentage increase (e.g., 5 for 5%)
    /// </summary>
    public decimal PercentageIncrease { get; set; }

    /// <summary>
    /// Reason for the increase
    /// </summary>
    public required string Reason { get; set; }

    /// <summary>
    /// Effective date of the increase
    /// </summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Preview mode - calculate changes without applying
    /// </summary>
    public bool PreviewOnly { get; set; } = true;
}

/// <summary>
/// Result of bulk salary increase operation
/// </summary>
public class BulkSalaryIncreaseResultDto
{
    /// <summary>
    /// Total employees affected
    /// </summary>
    public int TotalEmployees { get; set; }

    /// <summary>
    /// Total cost of increase (sum of all increases)
    /// </summary>
    public decimal TotalIncreaseCost { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "THB";

    /// <summary>
    /// Preview of individual changes
    /// </summary>
    public List<SalaryChangePreviewDto> Changes { get; set; } = new();

    /// <summary>
    /// Whether this was a preview or actual execution
    /// </summary>
    public bool IsPreview { get; set; }

    /// <summary>
    /// Job ID if executed (null for preview)
    /// </summary>
    public Guid? JobId { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Preview of individual salary change
/// </summary>
public class SalaryChangePreviewDto
{
    public Guid EmployeeId { get; set; }
    public required string EmployeeNumber { get; set; }
    public required string EmployeeName { get; set; }
    public required string Department { get; set; }
    public decimal CurrentSalary { get; set; }
    public decimal NewSalary { get; set; }
    public decimal IncreaseAmount { get; set; }
    public decimal PercentageIncrease { get; set; }
}
