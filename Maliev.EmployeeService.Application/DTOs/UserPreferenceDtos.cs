namespace Maliev.EmployeeService.Application.DTOs;

public class UpsertPreferenceRequest
{
    public string PreferenceData { get; set; } = "{}";
}

public class UserPreferenceResponse
{
    public Guid PrincipalId { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string PreferenceData { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; }
}
