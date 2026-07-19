using System.Net;
using System.Net.Http.Json;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Domain.Authorization;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

[Collection("IntegrationTests")]
public class ReportsControllerTests : WebApplicationTestBase
{
    public ReportsControllerTests(EmployeeServiceTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetHeadcountReport_ShouldReturnSuccess()
    {
        // Arrange
        AuthenticateAs(Guid.NewGuid(), permissions: new[] { EmployeePermissions.ReportsView });

        // Act
        var response = await _client.GetAsync("/employee/v1/reports/headcount");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<HeadcountReportDto>();
        Assert.NotNull(report);
    }

    [Fact]
    public async Task GetTurnoverAnalysis_ShouldReturnSuccess()
    {
        // Arrange
        AuthenticateAs(Guid.NewGuid(), permissions: new[] { EmployeePermissions.ReportsView });

        // Act
        var response = await _client.GetAsync("/employee/v1/reports/turnover?months=6");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<TurnoverAnalysisDto>();
        Assert.NotNull(report);
    }

    [Fact]
    public async Task GetDiversityMetrics_ShouldReturnSuccess()
    {
        // Arrange
        AuthenticateAs(Guid.NewGuid(), permissions: new[] { EmployeePermissions.ReportsView });

        // Act
        var response = await _client.GetAsync("/employee/v1/reports/diversity");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSpanOfControlReport_ShouldReturnSuccess()
    {
        // Arrange
        AuthenticateAs(Guid.NewGuid(), permissions: new[] { EmployeePermissions.ReportsView });

        // Act
        var response = await _client.GetAsync("/employee/v1/reports/span-of-control");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOrgChart_ShouldReturnSuccess()
    {
        // Arrange
        var dept = await CreateTestDepartmentAsync("OrgChartDept");
        var manager = await CreateTestEmployeeAsync(dept.Id, "MGR-001");
        AuthenticateAs(Guid.NewGuid(), permissions: new[] { EmployeePermissions.ProfilesRead });

        // Act
        var response = await _client.GetAsync($"/employee/v1/reports/org-chart?managerId={manager.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var chart = await response.Content.ReadFromJsonAsync<OrgChartDto>();
        Assert.NotNull(chart);
        Assert.Equal(manager.Id, chart.EmployeeId);
    }
}
