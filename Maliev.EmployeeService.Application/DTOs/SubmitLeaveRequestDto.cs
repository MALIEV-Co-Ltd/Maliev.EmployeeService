using System.ComponentModel.DataAnnotations;

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for submitting a new leave request
/// </summary>
public class SubmitLeaveRequestDto
{
    /// <summary>
    /// The type of leave being requested.
    /// </summary>
    [Required]
    public string LeaveType { get; set; } = string.Empty;

    /// <summary>
    /// The start date of the leave request.
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The end date of the leave request.
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// The reason for the leave request.
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
