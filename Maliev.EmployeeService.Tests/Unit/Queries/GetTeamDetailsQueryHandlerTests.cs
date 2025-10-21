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
/// Unit tests for GetTeamDetailsQueryHandler (User Story 5)
/// </summary>
public class GetTeamDetailsQueryHandlerTests
{
    private readonly Mock<ITeamRepository> _mockTeamRepository;
    private readonly GetTeamDetailsQueryHandler _handler;

    public GetTeamDetailsQueryHandlerTests()
    {
        _mockTeamRepository = new Mock<ITeamRepository>();
        _handler = new GetTeamDetailsQueryHandler(_mockTeamRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentTeam_ShouldReturnNull()
    {
        // Arrange
        var teamId = Guid.NewGuid();

        _mockTeamRepository.Setup(x => x.GetWithMembersAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        var query = new GetTeamDetailsQuery(teamId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().BeNull();
        _mockTeamRepository.Verify(x => x.GetWithMembersAsync(teamId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithTeamNoMembers_ShouldReturnTeamWithEmptyMembersList()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var team = new Team
        {
            Id = teamId,
            Name = "Engineering Team",
            Description = "Backend engineering team",
            TeamType = "Engineering",
            TeamLeadId = null,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            TeamMembers = new List<EmployeeTeamAssignment>()
        };

        _mockTeamRepository.Setup(x => x.GetWithMembersAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var query = new GetTeamDetailsQuery(teamId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(teamId);
        result.Name.Should().Be("Engineering Team");
        result.Description.Should().Be("Backend engineering team");
        result.TeamType.Should().Be("Engineering");
        result.TeamLeadId.Should().BeNull();
        result.TeamLeadName.Should().BeNull();
        result.IsActive.Should().BeTrue();
        result.Members.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithTeamAndTeamLead_ShouldIncludeTeamLeadName()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamLeadId = Guid.NewGuid();

        var teamLead = new Employee
        {
            Id = teamLeadId,
            EmployeeNumber = "EMP001",
            LegalName = new LegalName
            {
                FirstName = "John",
                LastName = "Doe"
            },
            ContactInformation = new ContactInformation
            {
                WorkEmail = "john.doe@company.com"
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
            TeamMembers = new List<EmployeeTeamAssignment>()
        };

        _mockTeamRepository.Setup(x => x.GetWithMembersAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var query = new GetTeamDetailsQuery(teamId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.TeamLeadId.Should().Be(teamLeadId);
        result.TeamLeadName.Should().Be("John Doe");
    }

    [Fact]
    public async Task HandleAsync_WithTeamMembers_ShouldMapAllMembersCorrectly()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var member1Id = Guid.NewGuid();
        var member2Id = Guid.NewGuid();
        var departmentId = Guid.NewGuid();

        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering Department",
            IsActive = true
        };

        var member1 = new Employee
        {
            Id = member1Id,
            EmployeeNumber = "EMP001",
            LegalName = new LegalName
            {
                FirstName = "Jane",
                LastName = "Smith"
            },
            JobTitle = "Senior Developer",
            Department = department,
            DepartmentId = departmentId,
            ContactInformation = new ContactInformation
            {
                WorkEmail = "jane.smith@company.com"
            },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow
        };

        var member2 = new Employee
        {
            Id = member2Id,
            EmployeeNumber = "EMP002",
            LegalName = new LegalName
            {
                FirstName = "Bob",
                LastName = "Johnson"
            },
            JobTitle = "Developer",
            Department = department,
            DepartmentId = departmentId,
            ContactInformation = new ContactInformation
            {
                WorkEmail = "bob.johnson@company.com"
            },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow
        };

        var assignment1 = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = member1Id,
            Employee = member1,
            TeamId = teamId,
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow
        };

        var assignment2 = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = member2Id,
            Employee = member2,
            TeamId = teamId,
            IsPrimary = false,
            CreatedDate = DateTime.UtcNow
        };

        var team = new Team
        {
            Id = teamId,
            Name = "Engineering Team",
            Description = "Backend engineering team",
            TeamType = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            TeamMembers = new List<EmployeeTeamAssignment> { assignment1, assignment2 }
        };

        _mockTeamRepository.Setup(x => x.GetWithMembersAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var query = new GetTeamDetailsQuery(teamId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.Members.Should().HaveCount(2);

        var member1Dto = result.Members.First(m => m.EmployeeId == member1Id);
        member1Dto.EmployeeNumber.Should().Be("EMP001");
        member1Dto.FullName.Should().Be("Jane Smith");
        member1Dto.JobTitle.Should().Be("Senior Developer");
        member1Dto.DepartmentName.Should().Be("Engineering Department");
        member1Dto.IsPrimary.Should().BeTrue();
        member1Dto.WorkEmail.Should().Be("jane.smith@company.com");

        var member2Dto = result.Members.First(m => m.EmployeeId == member2Id);
        member2Dto.EmployeeNumber.Should().Be("EMP002");
        member2Dto.FullName.Should().Be("Bob Johnson");
        member2Dto.JobTitle.Should().Be("Developer");
        member2Dto.DepartmentName.Should().Be("Engineering Department");
        member2Dto.IsPrimary.Should().BeFalse();
        member2Dto.WorkEmail.Should().Be("bob.johnson@company.com");
    }

    [Fact]
    public async Task HandleAsync_WithInactiveTeam_ShouldReturnInactiveStatus()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var team = new Team
        {
            Id = teamId,
            Name = "Archived Team",
            Description = "Old team",
            TeamType = "Project",
            IsActive = false,
            CreatedDate = DateTime.UtcNow.AddYears(-1),
            ModifiedDate = DateTime.UtcNow.AddMonths(-1),
            TeamMembers = new List<EmployeeTeamAssignment>()
        };

        _mockTeamRepository.Setup(x => x.GetWithMembersAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var query = new GetTeamDetailsQuery(teamId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
        result.ModifiedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_WithMemberWithoutDepartment_ShouldHandleNullDepartmentName()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var member = new Employee
        {
            Id = memberId,
            EmployeeNumber = "EMP001",
            LegalName = new LegalName
            {
                FirstName = "Jane",
                LastName = "Smith"
            },
            JobTitle = "Contractor",
            Department = null,
            ContactInformation = new ContactInformation
            {
                WorkEmail = "jane.smith@company.com"
            },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.Contractor,
            StartDate = DateTime.UtcNow
        };

        var assignment = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = memberId,
            Employee = member,
            TeamId = teamId,
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow
        };

        var team = new Team
        {
            Id = teamId,
            Name = "Contractor Team",
            TeamType = "Project",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            TeamMembers = new List<EmployeeTeamAssignment> { assignment }
        };

        _mockTeamRepository.Setup(x => x.GetWithMembersAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var query = new GetTeamDetailsQuery(teamId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.Members.Should().HaveCount(1);
        result.Members[0].DepartmentName.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithMultiplePrimaryMembers_ShouldIncludeAll()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var member1Id = Guid.NewGuid();
        var member2Id = Guid.NewGuid();

        var member1 = new Employee
        {
            Id = member1Id,
            EmployeeNumber = "EMP001",
            LegalName = new LegalName { FirstName = "Jane", LastName = "Smith" },
            ContactInformation = new ContactInformation { WorkEmail = "jane@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow
        };

        var member2 = new Employee
        {
            Id = member2Id,
            EmployeeNumber = "EMP002",
            LegalName = new LegalName { FirstName = "Bob", LastName = "Johnson" },
            ContactInformation = new ContactInformation { WorkEmail = "bob@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow
        };

        var assignment1 = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = member1Id,
            Employee = member1,
            TeamId = teamId,
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow
        };

        var assignment2 = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = member2Id,
            Employee = member2,
            TeamId = teamId,
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow
        };

        var team = new Team
        {
            Id = teamId,
            Name = "Cross-functional Team",
            TeamType = "Project",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            TeamMembers = new List<EmployeeTeamAssignment> { assignment1, assignment2 }
        };

        _mockTeamRepository.Setup(x => x.GetWithMembersAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var query = new GetTeamDetailsQuery(teamId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.Members.Should().HaveCount(2);
        result.Members.Should().AllSatisfy(m => m.IsPrimary.Should().BeTrue());
    }
}
