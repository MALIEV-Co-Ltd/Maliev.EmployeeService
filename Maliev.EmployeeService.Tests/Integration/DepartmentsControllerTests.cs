using System.Net;
using System.Net.Http.Json;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Domain.Authorization;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

[Collection("IntegrationTests")]
public class DepartmentsControllerTests : WebApplicationTestBase
{
    public DepartmentsControllerTests(EmployeeServiceTestFactory factory) : base(factory)
    {
    }

    private record CreateDepartmentResponse(Guid Id, string Message);

    [Fact]
    public async Task CreateAndGetDepartment_ShouldReturnSuccess()
    {
        // Arrange
        AuthenticateAs(Guid.NewGuid(), permissions: new[] { EmployeePermissions.DepartmentsManage });

        var createDto = new CreateDepartmentDto
        {
            Name = "New Dept",
            Description = "Test Dept",
            CostCenter = "CC001",
            HeadcountLimit = 50
        };

        // Act - Create
        var createResponse = await _client.PostAsJsonAsync("/employee/v1/departments", createDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateDepartmentResponse>();
        Guid deptId = createResult!.Id;

        // Act - Get
        var getResponse = await _client.GetAsync($"/employee/v1/departments/{deptId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var dept = await getResponse.Content.ReadFromJsonAsync<DepartmentDetailDto>();
        Assert.NotNull(dept);
        Assert.Equal("New Dept", dept.Name);
    }

    [Fact]
    public async Task GetAllDepartments_ShouldReturnSuccess()
    {
        // Arrange
        await CreateTestDepartmentAsync("Dept 1");
        await CreateTestDepartmentAsync("Dept 2");

        // Act
        var response = await _client.GetAsync("/employee/v1/departments");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var depts = await response.Content.ReadFromJsonAsync<List<DepartmentDto>>();
        Assert.NotNull(depts);
        Assert.True(depts.Count >= 2);
    }
}
