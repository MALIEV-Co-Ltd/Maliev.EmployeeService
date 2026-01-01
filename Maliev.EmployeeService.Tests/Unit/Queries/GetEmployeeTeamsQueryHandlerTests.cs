using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Xunit;
using Maliev.Aspire.ServiceDefaults.IAM;
using Microsoft.Extensions.Configuration;
using Maliev.EmployeeService.Domain.Authorization;
using Moq;

namespace Maliev.EmployeeService.Tests.Unit.Queries;

/// <summary>
/// Unit tests for GetEmployeeTeamsQueryHandler (User Story 5)
/// </summary>
public class GetEmployeeTeamsQueryHandlerTests
{
    private readonly Mock<ITeamRepository> _mockTeamRepository;
    private readonly Mock<IIamServiceClient> _mockIamClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly GetEmployeeTeamsQueryHandler _handler;

    public GetEmployeeTeamsQueryHandlerTests()
    {
        _mockTeamRepository = new Mock<ITeamRepository>();
        _mockIamClient = new Mock<IIamServiceClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());
        _mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new GetEmployeeTeamsQueryHandler(
            _mockTeamRepository.Object,
            _mockIamClient.Object,
            _mockConfiguration.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task HandleAsync_WithNoTeams_ShouldReturnEmptyList()
    {
        var employeeId = Guid.NewGuid();

        _mockTeamRepository.Setup(x => x.GetTeamsByEmployeeAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Team>());

        var query = new GetEmployeeTeamsQuery(employeeId);

        var result = await _handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_WithOneTeam_ShouldReturnSingleTeamDto()
    {
        var employeeId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var teamLeadId = Guid.NewGuid();

        var teamLead = new Employee
        {
            Id = teamLeadId,
            EmployeeNumber = "LEAD001",
            LegalName = new LegalName
            {
                FirstName = "John",
                LastName = "Manager"
            },
            ContactInformation = new ContactInformation
            {
                WorkEmail = "john.manager@company.com"
            },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow
        };

        var team = new Team
        {
            Id = teamId,
            Name = "Engineering Team",
            Description = "Backend engineering team",
            TeamType = "Engineering",
            TeamLeadId = teamLeadId,
            TeamLead = teamLead,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            TeamMembers = new List<EmployeeTeamAssignment>
            {
                new EmployeeTeamAssignment { EmployeeId = employeeId, TeamId = teamId, IsPrimary = true }
            }
        };

        _mockTeamRepository.Setup(x => x.GetTeamsByEmployeeAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Team> { team });

        var query = new GetEmployeeTeamsQuery(employeeId);

        var result = await _handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(teamLeadId, result[0].TeamLeadId);
        Assert.Equal("John Manager", result[0].TeamLeadName);
    }

    [Fact]
    public async Task HandleAsync_WithTeamWithoutTeamLead_ShouldHandleEmptyTeamLead()
    {
        var employeeId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Unassigned Team",
            TeamType = "Project",
            TeamLeadId = Guid.Empty,
            TeamLead = null,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            TeamMembers = new List<EmployeeTeamAssignment>
            {
                new EmployeeTeamAssignment { EmployeeId = employeeId, TeamId = teamId, IsPrimary = true }
            }
        };

        _mockTeamRepository.Setup(x => x.GetTeamsByEmployeeAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Team> { team });

        var query = new GetEmployeeTeamsQuery(employeeId);

        var result = await _handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result[0].TeamLeadId == Guid.Empty || result[0].TeamLeadId == null);
        Assert.Null(result[0].TeamLeadName);
    }
}