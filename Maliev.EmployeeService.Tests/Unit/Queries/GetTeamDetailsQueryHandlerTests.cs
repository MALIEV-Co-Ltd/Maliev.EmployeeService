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
/// Unit tests for GetTeamDetailsQueryHandler (User Story 5)
/// </summary>
public class GetTeamDetailsQueryHandlerTests
{
    private readonly Mock<ITeamRepository> _mockTeamRepository;
    private readonly Mock<IIamServiceClient> _mockIamClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly GetTeamDetailsQueryHandler _handler;

    public GetTeamDetailsQueryHandlerTests()
    {
        _mockTeamRepository = new Mock<ITeamRepository>();
        _mockIamClient = new Mock<IIamServiceClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        var principalId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.PrincipalId).Returns(principalId);
        _mockCurrentUserService.Setup(x => x.PrincipalIdentifier).Returns(principalId.ToString());
        _mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new GetTeamDetailsQueryHandler(
            _mockTeamRepository.Object,
            _mockIamClient.Object,
            _mockConfiguration.Object,
            _mockCurrentUserService.Object);
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
        Assert.Null(result);
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
            TeamLeadId = Guid.Empty,
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
        Assert.NotNull(result);
        Assert.Equal(teamId, result!.Id);
        Assert.Equal("Engineering Team", result.Name);
        Assert.Equal("Backend engineering team", result.Description);
        Assert.Equal("Engineering", result.TeamType);
        Assert.Equal(Guid.Empty, result.TeamLeadId);
        Assert.Null(result.TeamLeadName);
        Assert.True(result.IsActive);
        Assert.Empty(result.Members);
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
        Assert.NotNull(result);
        Assert.Equal(teamLeadId, result!.TeamLeadId);
        Assert.Equal("John Doe", result.TeamLeadName);
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
        Assert.NotNull(result);
        Assert.Equal(2, result!.Members.Count);

        var member1Dto = result.Members.First(m => m.EmployeeId == member1Id);
        Assert.Equal("EMP001", member1Dto.EmployeeNumber);
        Assert.Equal("Jane Smith", member1Dto.FullName);
        Assert.Equal("Senior Developer", member1Dto.JobTitle);
        Assert.Equal("Engineering Department", member1Dto.DepartmentName);
        Assert.True(member1Dto.IsPrimary);
        Assert.Equal("jane.smith@company.com", member1Dto.WorkEmail);

        var member2Dto = result.Members.First(m => m.EmployeeId == member2Id);
        Assert.Equal("EMP002", member2Dto.EmployeeNumber);
        Assert.Equal("Bob Johnson", member2Dto.FullName);
        Assert.Equal("Developer", member2Dto.JobTitle);
        Assert.Equal("Engineering Department", member2Dto.DepartmentName);
        Assert.False(member2Dto.IsPrimary);
        Assert.Equal("bob.johnson@company.com", member2Dto.WorkEmail);
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
        Assert.NotNull(result);
        Assert.False(result!.IsActive);
        Assert.NotNull(result.ModifiedDate);
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
        Assert.NotNull(result);
        Assert.Single(result!.Members);
        Assert.Null(result.Members[0].DepartmentName);
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
        Assert.NotNull(result);
        Assert.Equal(2, result!.Members.Count);
        Assert.All(result.Members, m => Assert.True(m.IsPrimary));
    }
}
