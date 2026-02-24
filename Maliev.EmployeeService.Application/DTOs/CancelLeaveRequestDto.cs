using System.ComponentModel.DataAnnotations;

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for cancelling a leave request
/// </summary>
public class CancelLeaveRequestDto
{
    /// <summary>
    /// The reason for cancelling the leave request.
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
