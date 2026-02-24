using System.Net;
using System.Net.Http.Json;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Domain.Authorization;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

[Collection("IntegrationTests")]
public class ManagersControllerTests : WebApplicationTestBase
{
    public ManagersControllerTests(EmployeeServiceTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetDirectReports_ShouldReturnSuccess()
    {
        // Arrange
        var dept = await CreateTestDepartmentAsync("ManagerDept");
        var manager = await CreateTestEmployeeAsync(dept.Id, "MGR-004");
        AuthenticateAs(Guid.NewGuid(), permissions: new[] { EmployeePermissions.ProfilesRead });

        // Act
        var response = await _client.GetAsync($"/employee/v1/managers/{manager.Id}/direct-reports");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOrgChart_ShouldReturnSuccess()
    {
        // Arrange
        var dept = await CreateTestDepartmentAsync("ManagerOrgChartDept");
        var manager = await CreateTestEmployeeAsync(dept.Id, "MGR-005");
        AuthenticateAs(Guid.NewGuid(), permissions: new[] { EmployeePermissions.ProfilesRead });

        // Act
        var response = await _client.GetAsync($"/employee/v1/managers/{manager.Id}/org-chart?depth=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var chart = await response.Content.ReadFromJsonAsync<OrgChartDto>();
        Assert.NotNull(chart);
    }
}
