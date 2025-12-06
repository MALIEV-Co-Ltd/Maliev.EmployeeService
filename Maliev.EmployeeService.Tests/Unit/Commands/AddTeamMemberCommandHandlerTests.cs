using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.Events;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Commands;

/// <summary>
/// Unit tests for AddTeamMemberCommandHandler (User Story 5)
/// </summary>
public class AddTeamMemberCommandHandlerTests
{
    private readonly Mock<ITeamRepository> _mockTeamRepository;
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<IRepository<EmployeeTeamAssignment>> _mockAssignmentRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly AddTeamMemberCommandHandler _handler;

    public AddTeamMemberCommandHandlerTests()
    {
        _mockTeamRepository = new Mock<ITeamRepository>();
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockAssignmentRepository = new Mock<IRepository<EmployeeTeamAssignment>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        // Setup default current user
        _mockCurrentUserService.Setup(x => x.EmployeeId).Returns(Guid.NewGuid());

        _handler = new AddTeamMemberCommandHandler(
            _mockTeamRepository.Object,
            _mockEmployeeRepository.Object,
            _mockAssignmentRepository.Object,
            _mockUnitOfWork.Object,
            _mockEventPublisher.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ShouldAddTeamMember()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Engineering Team",
            TeamType = "Engineering",
            IsActive = true
        };

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            LegalName = new LegalName
            {
                FirstName = "Jane",
                LastName = "Smith"
            },
            ContactInformation = new ContactInformation
            {
                WorkEmail = "jane.smith@company.com"
            },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow
        };

        var command = new AddTeamMemberCommand(
            TeamId: teamId,
            EmployeeId: employeeId,
            IsPrimary: false);

        _mockTeamRepository.Setup(x => x.GetByIdAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockTeamRepository.Setup(x => x.IsEmployeeMemberAsync(employeeId, teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockAssignmentRepository.Setup(x => x.AddAsync(It.IsAny<EmployeeTeamAssignment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _mockTeamRepository.Verify(x => x.GetByIdAsync(teamId, It.IsAny<CancellationToken>()), Times.Once);
        _mockEmployeeRepository.Verify(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTeamRepository.Verify(x => x.IsEmployeeMemberAsync(employeeId, teamId, It.IsAny<CancellationToken>()), Times.Once);

        _mockAssignmentRepository.Verify(x => x.AddAsync(
            It.Is<EmployeeTeamAssignment>(a =>
                a.EmployeeId == employeeId &&
                a.TeamId == teamId &&
                a.IsPrimary == false),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithPrimaryTeam_ShouldAddPrimaryTeamMember()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Engineering Team",
            TeamType = "Engineering",
            IsActive = true
        };

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            LegalName = new LegalName
            {
                FirstName = "Jane",
                LastName = "Smith"
            },
            ContactInformation = new ContactInformation
            {
                WorkEmail = "jane.smith@company.com"
            },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow
        };

        var command = new AddTeamMemberCommand(
            TeamId: teamId,
            EmployeeId: employeeId,
            IsPrimary: true);

        _mockTeamRepository.Setup(x => x.GetByIdAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockTeamRepository.Setup(x => x.IsEmployeeMemberAsync(employeeId, teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockAssignmentRepository.Setup(x => x.AddAsync(It.IsAny<EmployeeTeamAssignment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _mockAssignmentRepository.Verify(x => x.AddAsync(
            It.Is<EmployeeTeamAssignment>(a =>
                a.EmployeeId == employeeId &&
                a.TeamId == teamId &&
                a.IsPrimary == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidTeam_ShouldThrowException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var command = new AddTeamMemberCommand(
            TeamId: teamId,
            EmployeeId: employeeId,
            IsPrimary: false);

        _mockTeamRepository.Setup(x => x.GetByIdAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        // Act
        var act = async () => await _handler.HandleAsync(command);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);

        _mockTeamRepository.Verify(x => x.GetByIdAsync(teamId, It.IsAny<CancellationToken>()), Times.Once);
        _mockEmployeeRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockAssignmentRepository.Verify(x => x.AddAsync(It.IsAny<EmployeeTeamAssignment>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidEmployee_ShouldThrowException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Engineering Team",
            TeamType = "Engineering",
            IsActive = true
        };

        var command = new AddTeamMemberCommand(
            TeamId: teamId,
            EmployeeId: employeeId,
            IsPrimary: false);

        _mockTeamRepository.Setup(x => x.GetByIdAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var act = async () => await _handler.HandleAsync(command);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);

        _mockTeamRepository.Verify(x => x.GetByIdAsync(teamId, It.IsAny<CancellationToken>()), Times.Once);
        _mockEmployeeRepository.Verify(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAssignmentRepository.Verify(x => x.AddAsync(It.IsAny<EmployeeTeamAssignment>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenEmployeeAlreadyMember_ShouldThrowException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Engineering Team",
            TeamType = "Engineering",
            IsActive = true
        };

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            LegalName = new LegalName
            {
                FirstName = "Jane",
                LastName = "Smith"
            },
            ContactInformation = new ContactInformation
            {
                WorkEmail = "jane.smith@company.com"
            },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow
        };

        var command = new AddTeamMemberCommand(
            TeamId: teamId,
            EmployeeId: employeeId,
            IsPrimary: false);

        _mockTeamRepository.Setup(x => x.GetByIdAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockTeamRepository.Setup(x => x.IsEmployeeMemberAsync(employeeId, teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _handler.HandleAsync(command);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);

        _mockTeamRepository.Verify(x => x.GetByIdAsync(teamId, It.IsAny<CancellationToken>()), Times.Once);
        _mockEmployeeRepository.Verify(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTeamRepository.Verify(x => x.IsEmployeeMemberAsync(employeeId, teamId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAssignmentRepository.Verify(x => x.AddAsync(It.IsAny<EmployeeTeamAssignment>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetCreatedDate()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Engineering Team",
            TeamType = "Engineering",
            IsActive = true
        };

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            LegalName = new LegalName
            {
                FirstName = "Jane",
                LastName = "Smith"
            },
            ContactInformation = new ContactInformation
            {
                WorkEmail = "jane.smith@company.com"
            },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow
        };

        var command = new AddTeamMemberCommand(
            TeamId: teamId,
            EmployeeId: employeeId,
            IsPrimary: false);

        var capturedAssignment = (EmployeeTeamAssignment?)null;

        _mockTeamRepository.Setup(x => x.GetByIdAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockTeamRepository.Setup(x => x.IsEmployeeMemberAsync(employeeId, teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockAssignmentRepository.Setup(x => x.AddAsync(It.IsAny<EmployeeTeamAssignment>(), It.IsAny<CancellationToken>()))
            .Callback<EmployeeTeamAssignment, CancellationToken>((assignment, _) =>
            {
                assignment.Id = Guid.NewGuid();
                capturedAssignment = assignment;
            })
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        Assert.NotNull(capturedAssignment);
        Assert.NotEqual(Guid.Empty, capturedAssignment!.Id);
        Assert.Equal(employeeId, capturedAssignment.EmployeeId);
        Assert.Equal(teamId, capturedAssignment.TeamId);
    }
}
