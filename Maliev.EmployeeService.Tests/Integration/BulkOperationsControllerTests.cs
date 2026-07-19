using System.Net;
using System.Net.Http.Json;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Domain.Authorization;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

[Collection("IntegrationTests")]
public class BulkOperationsControllerTests : WebApplicationTestBase
{
    public BulkOperationsControllerTests(EmployeeServiceTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ExportEmployees_ShouldReturnCsvFile()
    {
        // Arrange
        var dept = await CreateTestDepartmentAsync("ExportDept");
        await CreateTestEmployeeAsync(dept.Id, "EXP-001");
        AuthenticateAs(Guid.NewGuid(), permissions: new[] { EmployeePermissions.ReportsGenerate });

        // Act
        var response = await _client.PostAsync("/employee/v1/bulk/employees/export", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var csvContent = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(csvContent);
        Assert.Contains("EXP-001", csvContent);
    }
}
