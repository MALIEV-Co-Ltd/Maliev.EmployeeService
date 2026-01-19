using System.Net;
using System.Net.Http.Json;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Domain.Authorization;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

[Collection("IntegrationTests")]
public class HRControllerTests : WebApplicationTestBase
{
    public HRControllerTests(EmployeeServiceTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateEmployee_ValidData_ReturnsCreated()
    {
        // Arrange
        var department = await CreateTestDepartmentAsync("HR");
        var request = new CreateEmployeeDto
        {
            EmployeeNumber = "HR-001",
            FirstName = "HR",
            LastName = "Specialist",
            WorkEmail = "hr.specialist@maliev.com",
            DepartmentId = department.Id,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow,
            DateOfBirth = DateTime.UtcNow.AddYears(-35),
            NationalId = "1234567890123",
            JobTitle = "Senior HR",
            Nationality = "Thai"
        };

        AuthenticateAs(Guid.NewGuid(), new[] { "roles.employee.hr-manager" }, new[] { EmployeePermissions.ProfilesCreate });

        // Act
        var response = await _client.PostAsJsonAsync("/employee/v1/hr/employees", request);

        // Assert
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"BadRequest result: {content}");
        }
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
