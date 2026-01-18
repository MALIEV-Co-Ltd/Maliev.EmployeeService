using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Organizational reporting and analytics
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employee/v{version:apiVersion}/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly GetHeadcountReportQueryHandler _headcountHandler;
    private readonly GetTurnoverAnalysisQueryHandler _turnoverHandler;
    private readonly GetDiversityMetricsQueryHandler _diversityHandler;
    private readonly GetSpanOfControlReportQueryHandler _spanOfControlHandler;
    private readonly GetOrgChartQueryHandler _orgChartHandler;
    private readonly SearchEmployeesQueryHandler _searchHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportsController"/> class
    /// </summary>
    public ReportsController(
        GetHeadcountReportQueryHandler headcountHandler,
        GetTurnoverAnalysisQueryHandler turnoverHandler,
        GetDiversityMetricsQueryHandler diversityHandler,
        GetSpanOfControlReportQueryHandler spanOfControlHandler,
        GetOrgChartQueryHandler orgChartHandler,
        SearchEmployeesQueryHandler searchHandler)
    {
        _headcountHandler = headcountHandler;
        _turnoverHandler = turnoverHandler;
        _diversityHandler = diversityHandler;
        _spanOfControlHandler = spanOfControlHandler;
        _orgChartHandler = orgChartHandler;
        _searchHandler = searchHandler;
    }

    /// <summary>
    /// Search employees with multi-criteria filtering
    /// </summary>
    [HttpGet("employees/search")]
    [RequirePermission(EmployeePermissions.EmployeeSearch)]
    [ProducesResponseType(typeof(EmployeeSearchResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchEmployees([FromQuery] SearchEmployeesQuery query, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _searchHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get organizational headcount summary
    /// </summary>
    [HttpGet("headcount")]
    [RequirePermission(EmployeePermissions.ReportsView)]
    public async Task<IActionResult> GetHeadcountReport([FromQuery] Guid? departmentId, CancellationToken cancellationToken)
    {
        var query = new GetHeadcountReportQuery { DepartmentId = departmentId };
        var result = await _headcountHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get turnover analysis report
    /// </summary>
    [HttpGet("turnover")]
    [RequirePermission(EmployeePermissions.ReportsView)]
    public async Task<IActionResult> GetTurnoverAnalysis([FromQuery] int months = 12, CancellationToken cancellationToken = default)
    {
        var query = new GetTurnoverAnalysisQuery
        {
            StartDate = DateTime.UtcNow.AddMonths(-months),
            EndDate = DateTime.UtcNow,
            DepartmentId = null
        };
        var result = await _turnoverHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get diversity metrics report
    /// </summary>
    [HttpGet("diversity")]
    [RequirePermission(EmployeePermissions.ReportsView)]
    public async Task<IActionResult> GetDiversityMetrics([FromQuery] Guid? departmentId, CancellationToken cancellationToken = default)
    {
        var query = new GetDiversityMetricsQuery { DepartmentId = departmentId };
        var result = await _diversityHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get manager span of control report
    /// </summary>
    [HttpGet("span-of-control")]
    [RequirePermission(EmployeePermissions.ReportsView)]
    public async Task<IActionResult> GetSpanOfControlReport(CancellationToken cancellationToken)
    {
        var query = new GetSpanOfControlReportQuery();
        var result = await _spanOfControlHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get organizational chart
    /// </summary>
    [HttpGet("org-chart")]
    [RequirePermission(EmployeePermissions.ProfilesRead)]
    public async Task<IActionResult> GetOrgChart([FromQuery] Guid? managerId, [FromQuery] int maxDepth = 3, CancellationToken cancellationToken = default)
    {
        var query = new GetOrgChartQuery(managerId ?? Guid.Empty, maxDepth);
        var result = await _orgChartHandler.HandleAsync(query, cancellationToken);
        return Ok(result.OrgChart);
    }
}
