using System.Net;
using System.Net.Http.Json;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Domain.Authorization;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

[Collection("IntegrationTests")]
public class AnalyticsControllerTests : WebApplicationTestBase
{
    public AnalyticsControllerTests(EmployeeServiceTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetSummary_ReturnsSuccess_WithCorrectData()
    {
        // Arrange
        var department = await CreateTestDepartmentAsync("Analytics Engineering");
        await CreateTestEmployeeAsync(department.Id, "ANA-001");
        await CreateTestEmployeeAsync(department.Id, "ANA-002");

        AuthenticateAs(Guid.NewGuid(), permissions: new[] { EmployeePermissions.ReportsView });

        // Act
        var response = await _client.GetAsync("/employee/v1/analytics/summary");

        // Assert
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<HrAnalyticsDto>();
        Assert.NotNull(summary);
        Assert.True(summary.TotalHeadcount >= 2);
        Assert.True(summary.ActiveEmployees >= 2);
        Assert.Contains(summary.DepartmentDistribution, d => d.Department == "Analytics Engineering");
    }
}
