using System.Net;
using System.Net.Http.Json;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Api.Controllers;
using Maliev.EmployeeService.Domain.Authorization;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

[Collection("IntegrationTests")]
public class TeamsControllerTests : WebApplicationTestBase
{
    public TeamsControllerTests(EmployeeServiceTestFactory factory) : base(factory)
    {
    }

    private record CreateTeamResponse(Guid TeamId, string Message);

    [Fact]
    public async Task CreateAndGetTeam_ShouldReturnSuccess()
    {
        // Arrange
        var dept = await CreateTestDepartmentAsync("TeamsDept");
        var manager = await CreateTestEmployeeAsync(dept.Id, "MGR-002");
        AuthenticateAs(Guid.NewGuid(), permissions: new[] { EmployeePermissions.TeamsManage });

        var createCommand = new CreateTeamCommand(
            "New Team",
            "Test Team",
            "Project",
            manager.Id
        );

        // Act - Create
        var createResponse = await _client.PostAsJsonAsync("/employee/v1/teams", createCommand);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();
        Guid teamId = createResult!.TeamId;

        // Act - Get
        var getResponse = await _client.GetAsync($"/employee/v1/teams/{teamId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var team = await getResponse.Content.ReadFromJsonAsync<TeamDetailsDto>();
        Assert.NotNull(team);
        Assert.Equal("New Team", team.Name);
    }

    [Fact]
    public async Task AddAndRemoveTeamMember_ShouldReturnSuccess()
    {
        // Arrange
        var dept = await CreateTestDepartmentAsync("MemberDept");
        var manager = await CreateTestEmployeeAsync(dept.Id, "MGR-003");
        var member = await CreateTestEmployeeAsync(dept.Id, "EMP-001");
        AuthenticateAs(Guid.NewGuid(), permissions: new[] { EmployeePermissions.TeamsManage });

        var createCommand = new CreateTeamCommand(
            "Member Team",
            null,
            "Squad",
            manager.Id
        );
        var createResponse = await _client.PostAsJsonAsync("/employee/v1/teams", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();
        Guid teamId = createResult!.TeamId;

        // Act - Add
        var addRequest = new AddTeamMemberRequest(member.Id, true);
        var addResponse = await _client.PostAsJsonAsync($"/employee/v1/teams/{teamId}/members", addRequest);
        Assert.Equal(HttpStatusCode.NoContent, addResponse.StatusCode);

        // Act - Remove
        var removeResponse = await _client.DeleteAsync($"/employee/v1/teams/{teamId}/members/{member.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);
    }
}
