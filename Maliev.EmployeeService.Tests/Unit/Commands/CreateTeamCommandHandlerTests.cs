using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Moq;
using Xunit;
using Maliev.Aspire.ServiceDefaults.IAM;
using Microsoft.Extensions.Configuration;
using Maliev.EmployeeService.Domain.Authorization;

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
    private readonly Mock<IIamServiceClient> _mockIamClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly CreateTeamCommandHandler _handler;

    public CreateTeamCommandHandlerTests()
    {
        _mockTeamRepository = new Mock<ITeamRepository>();
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockIamClient = new Mock<IIamServiceClient>();
        _mockConfiguration = new Mock<IConfiguration>();

        var principalId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.PrincipalId).Returns(principalId);
        _mockCurrentUserService.Setup(x => x.PrincipalIdentifier).Returns(principalId.ToString());
        _mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new CreateTeamCommandHandler(
            _mockTeamRepository.Object,
            _mockEmployeeRepository.Object,
            _mockUnitOfWork.Object,
            _mockEventPublisher.Object,
            _mockIamClient.Object,
            _mockConfiguration.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidDataAndTeamLead_ShouldCreateTeam()
    {
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

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockTeamRepository.Verify(x => x.AddAsync(
            It.Is<Team>(t => t.TeamLeadId == teamLeadId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithoutTeamLead_ShouldCreateTeam()
    {
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

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockTeamRepository.Verify(x => x.AddAsync(
            It.Is<Team>(t => t.TeamLeadId == Guid.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
