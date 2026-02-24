using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get all documents for an employee
/// </summary>
public class GetEmployeeDocumentsQuery
{
    /// <summary>
    /// Employee ID
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Optional document type filter
    /// </summary>
    public DocumentType? DocumentType { get; set; }

    /// <summary>
    /// Include archived documents
    /// </summary>
    public bool IncludeArchived { get; set; } = false;
}
