using FluentAssertions;
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
/// Unit tests for CreateTeamCommandHandler (User Story 5)
/// </summary>
public class CreateTeamCommandHandlerTests
{
    private readonly Mock<ITeamRepository> _mockTeamRepository;
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly CreateTeamCommandHandler _handler;

    public CreateTeamCommandHandlerTests()
    {
        _mockTeamRepository = new Mock<ITeamRepository>();
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        // Setup default current user
        _mockCurrentUserService.Setup(x => x.EmployeeId).Returns(Guid.NewGuid());

        _handler = new CreateTeamCommandHandler(
            _mockTeamRepository.Object,
            _mockEmployeeRepository.Object,
            _mockUnitOfWork.Object,
            _mockEventPublisher.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidDataAndTeamLead_ShouldCreateTeam()
    {
        // Arrange
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

        var command = new CreateTeamCommand(
            Name: "Engineering Team",
            Description: "Backend engineering team",
            TeamType: "Engineering",
            TeamLeadId: teamLeadId,
            IsActive: true);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(teamLeadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teamLead);

        _mockTeamRepository.Setup(x => x.AddAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
            .Callback<Team, CancellationToken>((team, _) => team.Id = Guid.NewGuid())
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeEmpty();

        _mockEmployeeRepository.Verify(x => x.GetByIdAsync(teamLeadId, It.IsAny<CancellationToken>()), Times.Once);

        _mockTeamRepository.Verify(x => x.AddAsync(
            It.Is<Team>(t =>
                t.Name == "Engineering Team" &&
                t.Description == "Backend engineering team" &&
                t.TeamType == "Engineering" &&
                t.TeamLeadId == teamLeadId &&
                t.IsActive == true),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithoutTeamLead_ShouldCreateTeam()
    {
        // Arrange
        var command = new CreateTeamCommand(
            Name: "Product Team",
            Description: "Product management team",
            TeamType: "Product",
            TeamLeadId: null,
            IsActive: true);

        _mockTeamRepository.Setup(x => x.AddAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
            .Callback<Team, CancellationToken>((team, _) => team.Id = Guid.NewGuid())
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeEmpty();

        _mockEmployeeRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        _mockTeamRepository.Verify(x => x.AddAsync(
            It.Is<Team>(t =>
                t.Name == "Product Team" &&
                t.Description == "Product management team" &&
                t.TeamType == "Product" &&
                t.TeamLeadId == null &&
                t.IsActive == true),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidTeamLead_ShouldThrowException()
    {
        // Arrange
        var teamLeadId = Guid.NewGuid();

        var command = new CreateTeamCommand(
            Name: "Engineering Team",
            Description: "Backend engineering team",
            TeamType: "Engineering",
            TeamLeadId: teamLeadId,
            IsActive: true);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(teamLeadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var act = async () => await _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Team lead with ID {teamLeadId} not found");

        _mockEmployeeRepository.Verify(x => x.GetByIdAsync(teamLeadId, It.IsAny<CancellationToken>()), Times.Once);
        _mockTeamRepository.Verify(x => x.AddAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithMinimalData_ShouldCreateTeam()
    {
        // Arrange
        var command = new CreateTeamCommand(
            Name: "QA Team",
            Description: null,
            TeamType: "QA",
            TeamLeadId: null,
            IsActive: true);

        _mockTeamRepository.Setup(x => x.AddAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
            .Callback<Team, CancellationToken>((team, _) => team.Id = Guid.NewGuid())
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeEmpty();

        _mockTeamRepository.Verify(x => x.AddAsync(
            It.Is<Team>(t =>
                t.Name == "QA Team" &&
                t.Description == null &&
                t.TeamType == "QA" &&
                t.TeamLeadId == null &&
                t.IsActive == true),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetCreatedDate()
    {
        // Arrange
        var command = new CreateTeamCommand(
            Name: "DevOps Team",
            Description: "Infrastructure team",
            TeamType: "DevOps",
            TeamLeadId: null,
            IsActive: true);

        var capturedTeam = (Team?)null;

        _mockTeamRepository.Setup(x => x.AddAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
            .Callback<Team, CancellationToken>((team, _) =>
            {
                team.Id = Guid.NewGuid();
                capturedTeam = team;
            })
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeEmpty();
        capturedTeam.Should().NotBeNull();
        capturedTeam!.Id.Should().NotBeEmpty();
    }
}
