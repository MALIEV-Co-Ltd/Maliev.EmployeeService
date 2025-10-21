using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for SearchEmployeesQuery
/// Performs multi-criteria search with filtering, sorting, and pagination
/// User Story 12 - Reporting & Analytics
/// </summary>
public class SearchEmployeesQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;

    public SearchEmployeesQueryHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    /// <summary>
    /// Handles the query to search employees
    /// </summary>
    public async Task<EmployeeSearchResultDto> HandleAsync(
        SearchEmployeesQuery query,
        CancellationToken cancellationToken = default)
    {
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
        var results = employees.Select(e => new EmployeeSearchItemDto
        {
            Id = e.Id,
            EmployeeNumber = e.EmployeeNumber,
            FullName = e.LegalName?.FullName ?? "Unknown",
            PreferredName = e.PreferredName,
            Email = e.ContactInformation?.WorkEmail ?? string.Empty,
            Title = e.JobTitle,
            DepartmentName = e.Department?.Name,
            DepartmentId = e.DepartmentId,
            ManagerName = e.Manager?.LegalName?.FullName,
            ManagerId = e.ManagerId,
            EmploymentStatus = e.EmploymentStatus.ToString(),
            EmploymentType = e.EmploymentType.ToString(),
            HireDate = e.StartDate,
            TerminationDate = e.TerminationDate
        }).ToList();

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
