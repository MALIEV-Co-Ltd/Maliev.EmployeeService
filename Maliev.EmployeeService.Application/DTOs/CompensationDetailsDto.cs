namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for compensation details (decrypted for authorized users)
/// </summary>
public class CompensationDetailsDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal SalaryAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public string ChangeReason { get; set; } = string.Empty;
    public string? BonusStructure { get; set; }
    public string? CommissionStructure { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? CreatedBy { get; set; }
}
