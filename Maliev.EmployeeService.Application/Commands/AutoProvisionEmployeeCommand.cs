using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.EmployeeService.Application.Commands;

public record AutoProvisionEmployeeCommand(
    [property: JsonPropertyName("email")]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    string Email,

    [property: JsonPropertyName("first_name")]
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters")]
    string FirstName,

    [property: JsonPropertyName("last_name")]
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters")]
    string LastName,

    [property: JsonPropertyName("picture_url")]
    string? PictureUrl
);
