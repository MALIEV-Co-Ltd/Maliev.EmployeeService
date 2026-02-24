using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Mapping;
using Maliev.EmployeeService.Domain.Authorization;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

///<summary>
/// Handler for SearchEmployeesQuery
/// Performs multi-criteria search with filtering, sorting, and pagination
/// User Story 12 - Reporting &amp; Analytics
/// </summary>
public class SearchEmployeesQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    ///<summary>
    /// Initializes a new instance of the <see cref="SearchEmployeesQueryHandler"/> class.
    /// </summary>
    /// <param name="employeeRepository">The employee repository.</param>
    /// <param name="iamClient">The IAM service client for authorization checks.</param>
    /// <param name="configuration">The configuration provider.</param>
    /// <param name="currentUserService">The service to access information about the current user.</param>
    public SearchEmployeesQueryHandler(
        IEmployeeRepository employeeRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    ///<summary>
    /// Handles the query to search employees
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A DTO containing the employee search results.</returns>
    public async Task<EmployeeSearchResultDto> HandleAsync(
        SearchEmployeesQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have EmployeeSearch permission
        var principalId = _currentUserService.PrincipalIdentifier;
        if (string.IsNullOrEmpty(principalId) ||
            (!await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.EmployeeSearch, "employee/search", cancellationToken) &&
             !_currentUserService.HasPermission(EmployeePermissions.EmployeeSearch)))
        {
            throw new UnauthorizedAccessException("You do not have permission to search employees");
        }
        // Get all employees
        var allEmployees = await _employeeRepository.GetAllAsync(cancellationToken);
        var employeesQuery = allEmployees.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var searchTerm = query.Name.ToLower();
            employeesQuery = employeesQuery.Where(e =>
                (e.LegalName != null && (
                    e.LegalName.FirstName.ToLower().Contains(searchTerm) ||
                    e.LegalName.LastName.ToLower().Contains(searchTerm) ||
                    e.LegalName.FullName.ToLower().Contains(searchTerm))) ||
                (e.PreferredName != null && e.PreferredName.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(query.EmployeeNumber))
        {
            employeesQuery = employeesQuery.Where(e =>
                e.EmployeeNumber.Contains(query.EmployeeNumber));
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var emailTerm = query.Email.ToLower();
            employeesQuery = employeesQuery.Where(e =>
                e.ContactInformation != null &&
                e.ContactInformation.WorkEmail.ToLower().Contains(emailTerm));
        }

        if (query.DepartmentId.HasValue)
        {
            employeesQuery = employeesQuery.Where(e =>
                e.DepartmentId == query.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            var titleTerm = query.Title.ToLower();
            employeesQuery = employeesQuery.Where(e =>
                e.JobTitle != null && e.JobTitle.ToLower().Contains(titleTerm));
        }

        if (!string.IsNullOrWhiteSpace(query.EmploymentStatus))
        {
            if (Enum.TryParse<EmploymentStatus>(query.EmploymentStatus, true, out var status))
            {
                employeesQuery = employeesQuery.Where(e => e.EmploymentStatus == status);
            }
        }

        if (query.ManagerId.HasValue)
        {
            employeesQuery = employeesQuery.Where(e =>
                e.ManagerId == query.ManagerId.Value);
        }

        // Apply sorting
        employeesQuery = ApplySorting(employeesQuery, query.SortBy, query.SortDirection);

        // Get total count before pagination
        var totalCount = employeesQuery.Count();

        // Apply pagination
        var pageSize = Math.Min(query.PageSize, 200); // Max 200 per page
        var skip = (query.Page - 1) * pageSize;

        var employees = employeesQuery
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        // Map to DTOs
        var results = employees.Select(e => e.ToEmployeeSearchItemDto()).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new EmployeeSearchResultDto
        {
            Results = results,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    ///<summary>
    /// Applies sorting to the employee query based on the specified sort field and direction.
    /// </summary>
    /// <param name="query">The IQueryable of employees.</param>
    /// <param name="sortBy">The field to sort by (e.g., "name", "employeeNumber", "hireDate", "department").</param>
    /// <param name="sortDirection">The sort direction ("asc" for ascending, "desc" for descending).</param>
    /// <returns>The sorted IQueryable of employees.</returns>
    private IQueryable<Domain.Entities.Employee> ApplySorting(
        IQueryable<Domain.Entities.Employee> query,
        string? sortBy,
        string? sortDirection)
    {
        var isDescending = sortDirection?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "employeenumber" => isDescending
                ? query.OrderByDescending(e => e.EmployeeNumber)
                : query.OrderBy(e => e.EmployeeNumber),

            "hiredate" => isDescending
                ? query.OrderByDescending(e => e.StartDate)
                : query.OrderBy(e => e.StartDate),

            "department" => isDescending
                ? query.OrderByDescending(e => e.Department!.Name)
                : query.OrderBy(e => e.Department!.Name),

            "name" or _ => isDescending
                ? query.OrderByDescending(e => e.LegalName!.LastName).ThenByDescending(e => e.LegalName!.FirstName)
                : query.OrderBy(e => e.LegalName!.LastName).ThenBy(e => e.LegalName!.FirstName)
        };
    }
}
