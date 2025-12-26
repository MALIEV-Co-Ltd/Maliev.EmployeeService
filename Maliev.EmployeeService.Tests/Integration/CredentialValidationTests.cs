using System.Net;
using System.Net.Http.Json;
using Maliev.EmployeeService.Api.Models;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

public class CredentialValidationTests : WebApplicationTestBase
{
    public CredentialValidationTests(EmployeeServiceTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ValidateCredentials_ReturnsPrincipalId_WhenEmployeeExists()
    {
        // Arrange
        var department = await CreateTestDepartmentAsync("HR");
        var principalId = Guid.NewGuid();
        var email = "migrated@example.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.EmployeeDbContext>();
            var employee = await CreateTestEmployeeAsync(department.Id, "EMP-MIG-001", email: email);
            employee.PrincipalId = principalId;
            context.Employees.Update(employee);
            await context.SaveChangesAsync();
        }

        var request = new ValidateCredentialsRequest(email, "password123");

        // Act
        var response = await _client.PostAsJsonAsync("/employee/v1/auth/validate", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CredentialValidationResponse>();
        Assert.NotNull(result);
        Assert.True(result!.IsValid);
        Assert.Equal(principalId, result.PrincipalId);
    }

    [Fact]
    public async Task ValidateCredentials_ReturnsNotValid_WhenEmployeeDoesNotExist()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var request = new ValidateCredentialsRequest(email, "password123");

        // Act
        var response = await _client.PostAsJsonAsync("/employee/v1/auth/validate", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CredentialValidationResponse>();
        Assert.NotNull(result);
        Assert.False(result!.IsValid);
    }
}
