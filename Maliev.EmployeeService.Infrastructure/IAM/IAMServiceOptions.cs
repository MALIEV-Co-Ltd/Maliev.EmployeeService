namespace Maliev.EmployeeService.Infrastructure.IAM;

public class IAMServiceOptions
{
    public string? BaseUrl { get; set; }
    public string? ServiceAccountToken { get; set; }
    public int TimeoutInSeconds { get; set; } = 5;
}
