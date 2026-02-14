using System.Net;
using System.Net.Http.Json;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

[Collection("IntegrationTests")]
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

        AuthenticateAs(Guid.NewGuid(), new[] { "roles.employee.hr-generalist" }, new[] { EmployeePermissions.ProfilesRead });

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
        AuthenticateAs(Guid.NewGuid(), new[] { "roles.employee.hr-generalist" }, new[] { EmployeePermissions.ProfilesRead });

        // Act
        var response = await _client.GetAsync($"/employee/v1/employees/by-principal/{principalId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByEmail_ReturnsEmployee_WhenEmployeeExists()
    {
        // Arrange
        var department = await CreateTestDepartmentAsync("Sales");
        var email = "test.lookup@maliev.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.EmployeeDbContext>();
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP-EMAIL-001",
                LegalName = new Domain.ValueObjects.LegalName("Lookup", "Test"),
                ContactInformation = new Domain.ValueObjects.ContactInformation
                {
                    WorkEmail = email,
                    MobilePhone = "+66123456789"
                },
                DepartmentId = department.Id,
                EmploymentStatus = Domain.Enums.EmploymentStatus.Active,
                EmploymentType = Domain.Enums.EmploymentType.FullTime,
                StartDate = DateTime.UtcNow,
                DateOfBirth = DateTime.UtcNow.AddYears(-30),
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };
            context.Employees.Add(employee);
            await context.SaveChangesAsync();
        }

        AuthenticateAs(Guid.NewGuid(), new[] { "roles.employee.hr-generalist" }, new[] { EmployeePermissions.ProfilesRead });

        // Act
        var response = await _client.GetAsync($"/employee/v1/employees/by-email/{email}");

        // Assert
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // Try to list all employees to see what's there
            using var scope2 = _factory.Services.CreateScope();
            var context2 = scope2.ServiceProvider.GetRequiredService<Infrastructure.Data.EmployeeDbContext>();
            var all = await context2.Employees.ToListAsync();
            var emails = string.Join(", ", all.Select(e => e.ContactInformation.WorkEmail));
            throw new Exception($"NotFound for {email}. DB has emails: {emails}");
        }
        response.EnsureSuccessStatusCode();
        var employeeResult = await response.Content.ReadFromJsonAsync<EmployeeLookupDto>();
        Assert.NotNull(employeeResult);
        Assert.Equal(email, employeeResult!.Email);
    }

    [Fact]
    public async Task AutoProvision_ValidRequest_ReturnsCreatedEmployee()
    {
        // Arrange
        var email = "new.auto@maliev.com";
        var command = new AutoProvisionEmployeeCommand
        (
            Email: email,
            FirstName: "Auto",
            LastName: "Provisioned",
            PictureUrl: null
        );

        AuthenticateAs(Guid.NewGuid(), new[] { "roles.employee.hr-generalist" }, new[] { EmployeePermissions.ProfilesCreate });

        // Act
        var request = new { email = email, first_name = "Auto", last_name = "Provisioned" };
        var response = await _client.PostAsJsonAsync("/employee/v1/employees/auto-provision", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AutoProvisionEmployeeDto>();
        Assert.NotNull(result);
        Assert.Equal(email, result!.Email);
    }
}
