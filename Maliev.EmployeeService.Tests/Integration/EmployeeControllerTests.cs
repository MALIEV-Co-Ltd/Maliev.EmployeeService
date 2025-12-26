using System.Net;
using System.Net.Http.Json;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

public class EmployeeControllerTests : WebApplicationTestBase
{
    public EmployeeControllerTests(EmployeeServiceTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetByPrincipalId_ReturnsProfile_WhenEmployeeExists()
    {
        // Arrange
        var department = await CreateTestDepartmentAsync("Engineering");
        var principalId = Guid.NewGuid();
        
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.EmployeeDbContext>();
            var employee = await CreateTestEmployeeAsync(department.Id, "EMP-LOOKUP-001");
            employee.PrincipalId = principalId;
            context.Employees.Update(employee);
            await context.SaveChangesAsync();
        }

        AuthenticateAs(Guid.NewGuid(), new[] { "roles.employee.hr-generalist" });

        // Act
        var response = await _client.GetAsync($"/employee/v1/employees/by-principal/{principalId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var profile = await response.Content.ReadFromJsonAsync<EmployeeProfileDto>();
        Assert.NotNull(profile);
        Assert.Equal("EMP-LOOKUP-001", profile!.EmployeeNumber);
    }

    [Fact]
    public async Task GetByPrincipalId_ReturnsNotFound_WhenEmployeeDoesNotExist()
    {
        // Arrange
        var principalId = Guid.NewGuid();
        AuthenticateAs(Guid.NewGuid(), new[] { "roles.employee.hr-generalist" });

        // Act
        var response = await _client.GetAsync($"/employee/v1/employees/by-principal/{principalId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
