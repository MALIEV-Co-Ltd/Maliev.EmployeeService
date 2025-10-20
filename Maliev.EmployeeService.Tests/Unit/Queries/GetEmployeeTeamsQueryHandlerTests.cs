using FluentAssertions;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Queries;

/// <summary>
/// Unit tests for GetEmployeeTeamsQueryHandler (User Story 5)
/// </summary>
public class GetEmployeeTeamsQueryHandlerTests
{
    private readonly Mock<ITeamRepository> _mockTeamRepository;
    private readonly GetEmployeeTeamsQueryHandler _handler;

    public GetEmployeeTeamsQueryHandlerTests()
    {
        _mockTeamRepository = new Mock<ITeamRepository>();
        _handler = new GetEmployeeTeamsQueryHandler(_mockTeamRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_WithNoTeams_ShouldReturnEmptyList()
    {
        // Arrange
        var employeeId = Guid.NewGuid();

        _mockTeamRepository.Setup(x => x.GetTeamsByEmployeeAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Team>());

        var query = new GetEmployeeTeamsQuery(employeeId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockTeamRepository.Verify(x => x.GetTeamsByEmployeeAsync(employeeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithOneTeam_ShouldReturnSingleTeamDto()
    {
        // Arrange
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

        var assignment = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TeamId = teamId,
            IsPrimary = true
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
            TeamMembers = new List<EmployeeTeamAssignment> { assignment }
        };

        _mockTeamRepository.Setup(x => x.GetTeamsByEmployeeAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Team> { team });

        var query = new GetEmployeeTeamsQuery(employeeId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        var teamDto = result[0];
        teamDto.Id.Should().Be(teamId);
        teamDto.Name.Should().Be("Engineering Team");
        teamDto.Description.Should().Be("Backend engineering team");
        teamDto.TeamType.Should().Be("Engineering");
        teamDto.TeamLeadId.Should().Be(teamLeadId);
        teamDto.TeamLeadName.Should().Be("John Manager");
        teamDto.IsActive.Should().BeTrue();
        teamDto.MemberCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_WithMultipleTeams_ShouldReturnAllTeams()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var team1Id = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var team3Id = Guid.NewGuid();

        var assignment1 = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TeamId = team1Id,
            IsPrimary = true
        };

        var assignment2 = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TeamId = team2Id,
            IsPrimary = false
        };

        var assignment3 = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TeamId = team3Id,
            IsPrimary = false
        };

        var team1 = new Team
        {
            Id = team1Id,
            Name = "Engineering Team",
            TeamType = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            TeamMembers = new List<EmployeeTeamAssignment> { assignment1 }
        };

        var team2 = new Team
        {
            Id = team2Id,
            Name = "Product Team",
            TeamType = "Product",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            TeamMembers = new List<EmployeeTeamAssignment> { assignment2 }
        };

        var team3 = new Team
        {
            Id = team3Id,
            Name = "DevOps Team",
            TeamType = "DevOps",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            TeamMembers = new List<EmployeeTeamAssignment> { assignment3 }
        };

        _mockTeamRepository.Setup(x => x.GetTeamsByEmployeeAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Team> { team1, team2, team3 });

        var query = new GetEmployeeTeamsQuery(employeeId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(t => t.Name == "Engineering Team");
        result.Should().Contain(t => t.Name == "Product Team");
        result.Should().Contain(t => t.Name == "DevOps Team");
    }

    [Fact]
    public async Task HandleAsync_WithTeamWithoutTeamLead_ShouldHandleNullTeamLead()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var assignment = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TeamId = teamId,
            IsPrimary = true
        };

        var team = new Team
        {
            Id = teamId,
            Name = "Unassigned Team",
            TeamType = "Project",
            TeamLeadId = null,
            TeamLead = null,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            TeamMembers = new List<EmployeeTeamAssignment> { assignment }
        };

        _mockTeamRepository.Setup(x => x.GetTeamsByEmployeeAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Team> { team });

        var query = new GetEmployeeTeamsQuery(employeeId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].TeamLeadId.Should().BeNull();
        result[0].TeamLeadName.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithInactiveTeam_ShouldIncludeInactiveStatus()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var assignment = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TeamId = teamId,
            IsPrimary = true
        };

        var team = new Team
        {
            Id = teamId,
            Name = "Archived Team",
            TeamType = "Project",
            IsActive = false,
            CreatedDate = DateTime.UtcNow.AddYears(-1),
            ModifiedDate = DateTime.UtcNow.AddMonths(-1),
            TeamMembers = new List<EmployeeTeamAssignment> { assignment }
        };

        _mockTeamRepository.Setup(x => x.GetTeamsByEmployeeAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Team> { team });

        var query = new GetEmployeeTeamsQuery(employeeId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].IsActive.Should().BeFalse();
        result[0].ModifiedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_WithMultipleMembers_ShouldCalculateMemberCountCorrectly()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var assignment1 = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TeamId = teamId,
            IsPrimary = true
        };

        var assignment2 = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            TeamId = teamId,
            IsPrimary = false
        };

        var assignment3 = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            TeamId = teamId,
            IsPrimary = false
        };

        var team = new Team
        {
            Id = teamId,
            Name = "Large Team",
            TeamType = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            TeamMembers = new List<EmployeeTeamAssignment> { assignment1, assignment2, assignment3 }
        };

        _mockTeamRepository.Setup(x => x.GetTeamsByEmployeeAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Team> { team });

        var query = new GetEmployeeTeamsQuery(employeeId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].MemberCount.Should().Be(3);
    }

    [Fact]
    public async Task HandleAsync_WithTeamsOfDifferentTypes_ShouldReturnAllTypes()
    {
        // Arrange
        var employeeId = Guid.NewGuid();

        var engineeringTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Backend Team",
            TeamType = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            TeamMembers = new List<EmployeeTeamAssignment>()
        };

        var productTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Product Team",
            TeamType = "Product",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            TeamMembers = new List<EmployeeTeamAssignment>()
        };

        var projectTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Special Project",
            TeamType = "Project",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            TeamMembers = new List<EmployeeTeamAssignment>()
        };

        _mockTeamRepository.Setup(x => x.GetTeamsByEmployeeAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Team> { engineeringTeam, productTeam, projectTeam });

        var query = new GetEmployeeTeamsQuery(employeeId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(t => t.TeamType == "Engineering");
        result.Should().Contain(t => t.TeamType == "Product");
        result.Should().Contain(t => t.TeamType == "Project");
    }

    [Fact]
    public async Task HandleAsync_ShouldPreserveDatesCorrectly()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var createdDate = DateTime.UtcNow.AddMonths(-3);
        var modifiedDate = DateTime.UtcNow.AddDays(-5);

        var team = new Team
        {
            Id = teamId,
            Name = "Time-Sensitive Team",
            TeamType = "Project",
            IsActive = true,
            CreatedDate = createdDate,
            ModifiedDate = modifiedDate,
            TeamMembers = new List<EmployeeTeamAssignment>()
        };

        _mockTeamRepository.Setup(x => x.GetTeamsByEmployeeAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Team> { team });

        var query = new GetEmployeeTeamsQuery(employeeId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].CreatedDate.Should().BeCloseTo(createdDate, TimeSpan.FromSeconds(1));
        result[0].ModifiedDate.Should().BeCloseTo(modifiedDate, TimeSpan.FromSeconds(1));
    }
}
