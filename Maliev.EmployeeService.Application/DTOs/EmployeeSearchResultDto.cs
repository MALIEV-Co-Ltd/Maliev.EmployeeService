namespace Maliev.EmployeeService.Application.DTOs;

///<summary>
/// DTO for employee search results with pagination
/// User Story 12 - Reporting &amp; Analytics
/// </summary>
public class EmployeeSearchResultDto
{
    ///<summary>
    /// List of matching employees
    /// </summary>
    public List<EmployeeSearchItemDto> Results { get; set; } = new();

    ///<summary>
    /// Total number of matching records
    /// </summary>
    public int TotalCount { get; set; }

    ///<summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    ///<summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    ///<summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }
}

///<summary>
/// Individual employee search result item
/// </summary>
public class EmployeeSearchItemDto
{
    ///<summary>
    /// The ID of the employee.
    /// </summary>
    public Guid Id { get; set; }
    ///<summary>
    /// The employee's number.
    /// </summary>
    public string EmployeeNumber { get; set; } = string.Empty;
    ///<summary>
    /// The employee's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    ///<summary>
    /// The employee's preferred name.
    /// </summary>
    public string? PreferredName { get; set; }
    ///<summary>
    /// The employee's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;
    ///<summary>
    /// The employee's title.
    /// </summary>
    public string? Title { get; set; }
    ///<summary>
    /// The name of the employee's department.
    /// </summary>
    public string? DepartmentName { get; set; }
    ///<summary>
    /// The ID of the employee's department.
    /// </summary>
    public Guid? DepartmentId { get; set; }
    ///<summary>
    /// The name of the employee's manager.
    /// </summary>
    public string? ManagerName { get; set; }
    ///<summary>
    /// The ID of the employee's manager.
    /// </summary>
    public Guid? ManagerId { get; set; }
    ///<summary>
    /// The employee's employment status.
    /// </summary>
    public string EmploymentStatus { get; set; } = string.Empty;
    ///<summary>
    /// The employee's employment type.
    /// </summary>
    public string EmploymentType { get; set; } = string.Empty;
    ///<summary>
    /// The employee's hire date.
    /// </summary>
    public DateTime HireDate { get; set; }
    ///<summary>
    /// The employee's termination date.
    /// </summary>
    public DateTime? TerminationDate { get; set; }
}
