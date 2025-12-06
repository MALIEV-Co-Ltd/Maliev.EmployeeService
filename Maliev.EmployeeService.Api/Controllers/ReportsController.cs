using Asp.Versioning;
using Maliev.EmployeeService.Api.Authorization;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Controller for reporting and analytics endpoints
/// User Story 12 - Reporting and Analytics
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employees/v{version:apiVersion}/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly GetHeadcountReportQueryHandler _headcountHandler;
    private readonly GetTurnoverAnalysisQueryHandler _turnoverHandler;
    private readonly SearchEmployeesQueryHandler _searchHandler;
    private readonly GetDiversityMetricsQueryHandler _diversityHandler;
    private readonly GetCompensationAnalysisQueryHandler _compensationHandler;
    private readonly GetSpanOfControlReportQueryHandler _spanOfControlHandler;
    private readonly GetLeaveUtilizationReportQueryHandler _leaveUtilizationHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportsController"/> class
    /// </summary>
    public ReportsController(
        GetHeadcountReportQueryHandler headcountHandler,
        GetTurnoverAnalysisQueryHandler turnoverHandler,
        SearchEmployeesQueryHandler searchHandler,
        GetDiversityMetricsQueryHandler diversityHandler,
        GetCompensationAnalysisQueryHandler compensationHandler,
        GetSpanOfControlReportQueryHandler spanOfControlHandler,
        GetLeaveUtilizationReportQueryHandler leaveUtilizationHandler)
    {
        _headcountHandler = headcountHandler;
        _turnoverHandler = turnoverHandler;
        _searchHandler = searchHandler;
        _diversityHandler = diversityHandler;
        _compensationHandler = compensationHandler;
        _spanOfControlHandler = spanOfControlHandler;
        _leaveUtilizationHandler = leaveUtilizationHandler;
    }

    /// <summary>
    /// Get headcount report with breakdowns by department, employment type, and tenure
    /// </summary>
    /// <param name="asOfDate">Date for the report (defaults to current date)</param>
    /// <param name="departmentId">Optional department filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Headcount report</returns>
    [HttpGet("headcount")]
    [Authorize(Policy = Policies.RequireHROrManager)]
    [ProducesResponseType(typeof(HeadcountReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<HeadcountReportDto>> GetHeadcountReport(
        [FromQuery] DateTime? asOfDate,
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetHeadcountReportQuery
        {
            AsOfDate = asOfDate,
            DepartmentId = departmentId
        };

        var report = await _headcountHandler.HandleAsync(query, cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Get turnover analysis with voluntary/involuntary breakdown and trends
    /// </summary>
    /// <param name="startDate">Start date of analysis period</param>
    /// <param name="endDate">End date of analysis period</param>
    /// <param name="departmentId">Optional department filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Turnover analysis report</returns>
    [HttpGet("turnover")]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(typeof(TurnoverAnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TurnoverAnalysisDto>> GetTurnoverAnalysis(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken = default)
    {
        // Default to last 12 months if not specified
        var end = endDate ?? DateTime.UtcNow;
        var start = startDate ?? end.AddYears(-1);

        if (start > end)
        {
            return BadRequest("Start date must be before end date");
        }

        var query = new GetTurnoverAnalysisQuery
        {
            StartDate = start,
            EndDate = end,
            DepartmentId = departmentId
        };

        var report = await _turnoverHandler.HandleAsync(query, cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Search employees with multi-criteria filtering and pagination
    /// </summary>
    /// <param name="name">Search by name</param>
    /// <param name="employeeNumber">Search by employee number</param>
    /// <param name="email">Search by email</param>
    /// <param name="departmentId">Filter by department</param>
    /// <param name="title">Filter by job title</param>
    /// <param name="employmentStatus">Filter by employment status</param>
    /// <param name="managerId">Filter by manager</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Page size (max 200)</param>
    /// <param name="sortBy">Sort field (name, employeeNumber, hireDate, department)</param>
    /// <param name="sortDirection">Sort direction (asc, desc)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated search results</returns>
    [HttpGet("employees/search")]
    [Authorize(Policy = Policies.RequireHROrManager)]
    [ProducesResponseType(typeof(EmployeeSearchResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeSearchResultDto>> SearchEmployees(
        [FromQuery] string? name,
        [FromQuery] string? employeeNumber,
        [FromQuery] string? email,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? title,
        [FromQuery] string? employmentStatus,
        [FromQuery] Guid? managerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = "name",
        [FromQuery] string? sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var query = new SearchEmployeesQuery
        {
            Name = name,
            EmployeeNumber = employeeNumber,
            Email = email,
            DepartmentId = departmentId,
            Title = title,
            EmploymentStatus = employmentStatus,
            ManagerId = managerId,
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var results = await _searchHandler.HandleAsync(query, cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Get diversity metrics report with gender, nationality, and age band analysis
    /// </summary>
    /// <param name="asOfDate">Date for the report (defaults to current date)</param>
    /// <param name="departmentId">Optional department filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Diversity metrics report</returns>
    [HttpGet("diversity")]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(typeof(DiversityMetricsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DiversityMetricsDto>> GetDiversityMetrics(
        [FromQuery] DateTime? asOfDate,
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetDiversityMetricsQuery
        {
            AsOfDate = asOfDate,
            DepartmentId = departmentId
        };

        var report = await _diversityHandler.HandleAsync(query, cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Get compensation analysis with anonymized salary ranges and statistics
    /// Restricted to HR Specialist role or higher
    /// </summary>
    /// <param name="asOfDate">Date for the analysis (defaults to current date)</param>
    /// <param name="departmentId">Optional department filter</param>
    /// <param name="jobTitle">Optional job title filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compensation analysis report</returns>
    [HttpGet("compensation")]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(typeof(CompensationAnalysisDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompensationAnalysisDto>> GetCompensationAnalysis(
        [FromQuery] DateTime? asOfDate,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? jobTitle,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCompensationAnalysisQuery
        {
            AsOfDate = asOfDate,
            DepartmentId = departmentId,
            JobTitle = jobTitle
        };

        var report = await _compensationHandler.HandleAsync(query, cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Get span of control report identifying managers at or exceeding limits
    /// </summary>
    /// <param name="departmentId">Optional department filter</param>
    /// <param name="onlyAtRisk">Show only managers at risk (exceeding/approaching limits)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Span of control report</returns>
    [HttpGet("span-of-control")]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(typeof(SpanOfControlReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SpanOfControlReportDto>> GetSpanOfControlReport(
        [FromQuery] Guid? departmentId,
        [FromQuery] bool onlyAtRisk = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSpanOfControlReportQuery
        {
            DepartmentId = departmentId,
            OnlyAtRisk = onlyAtRisk
        };

        var report = await _spanOfControlHandler.HandleAsync(query, cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Get leave utilization report analyzing accrual, usage, and carryover patterns
    /// </summary>
    /// <param name="year">Year for the report (defaults to current year)</param>
    /// <param name="departmentId">Optional department filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Leave utilization report</returns>
    [HttpGet("leave-utilization")]
    [Authorize(Policy = Policies.RequireHROrManager)]
    [ProducesResponseType(typeof(LeaveUtilizationReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveUtilizationReportDto>> GetLeaveUtilizationReport(
        [FromQuery] int? year,
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetLeaveUtilizationReportQuery
        {
            Year = year,
            DepartmentId = departmentId
        };

        var report = await _leaveUtilizationHandler.HandleAsync(query, cancellationToken);
        return Ok(report);
    }
}
