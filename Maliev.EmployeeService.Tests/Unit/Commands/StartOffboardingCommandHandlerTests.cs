using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.IntegrationEvents;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Commands;

/// <summary>
/// Unit tests for StartOffboardingCommandHandler
/// Verifies offboarding workflow initialization with termination date validation
/// </summary>
public class StartOffboardingCommandHandlerTests
{
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<IOffboardingRepository> _offboardingRepositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<StartOffboardingCommandHandler>> _loggerMock;
    private readonly StartOffboardingCommandHandler _handler;

    public StartOffboardingCommandHandlerTests()
    {
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _offboardingRepositoryMock = new Mock<IOffboardingRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<StartOffboardingCommandHandler>>();

        _handler = new StartOffboardingCommandHandler(
            _employeeRepositoryMock.Object,
            _offboardingRepositoryMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidEmployee_UpdatesStatusAndCreatesChecklist()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var terminationDate = DateTime.UtcNow.Date.AddDays(14);
        var terminationReason = "Voluntary Resignation";

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            LegalName = new LegalName { FirstName = "John", LastName = "Doe" },
            ContactInformation = new ContactInformation { WorkEmail = "john.doe@company.com" },
            JobTitle = "Software Engineer",
            EmploymentStatus = EmploymentStatus.Active,
            Department = new Department { Id = Guid.NewGuid(), Name = "Engineering" }
        };

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _offboardingRepositoryMock
            .Setup(x => x.CreateChecklistAsync(It.IsAny<IEnumerable<OffboardingChecklist>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeTerminatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new StartOffboardingCommand
        {
            EmployeeId = employeeId,
            TerminationDate = terminationDate,
            TerminationReason = terminationReason,
            EligibleForRehire = true
        };

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(employeeId, result);

        // Verify employee status updated
        Assert.Equal(terminationDate, employee.TerminationDate);
        Assert.Equal(EmploymentStatus.Terminated, employee.EmploymentStatus);
        Assert.True(Math.Abs((employee.ModifiedDate!.Value - DateTime.UtcNow).TotalSeconds) <= 5);

        // Verify checklist created
        _offboardingRepositoryMock.Verify(
            x => x.CreateChecklistAsync(
                It.Is<IEnumerable<OffboardingChecklist>>(checklist => checklist.Any()),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify integration event published
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<EmployeeTerminatedIntegrationEvent>(evt =>
                    evt.EmployeeId == employeeId &&
                    evt.EmployeeNumber == "EMP001" &&
                    evt.FullName == "John Doe" &&
                    evt.Email == "john.doe@company.com" &&
                    evt.TerminationDate == terminationDate &&
                    evt.TerminationReason == terminationReason &&
                    evt.Department == "Engineering" &&
                    evt.EligibleForRehire == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EmployeeNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var employeeId = Guid.NewGuid();

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new StartOffboardingCommand
        {
            EmployeeId = employeeId,
            TerminationDate = DateTime.UtcNow.Date.AddDays(14),
            TerminationReason = "Resignation"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(command, CancellationToken.None));

        Assert.Contains(employeeId.ToString(), exception.Message);

        // Verify no checklist or event created
        _offboardingRepositoryMock.Verify(
            x => x.CreateChecklistAsync(It.IsAny<IEnumerable<OffboardingChecklist>>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<EmployeeTerminatedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_CreatesChecklistWithAllRequiredItems()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var terminationDate = DateTime.UtcNow.Date.AddDays(30);

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP002",
            LegalName = new LegalName { FirstName = "Jane", LastName = "Smith" },
            ContactInformation = new ContactInformation { WorkEmail = "jane.smith@company.com" },
            JobTitle = "HR Manager",
            EmploymentStatus = EmploymentStatus.Active
        };

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        OffboardingChecklist[]? capturedChecklist = null;
        _offboardingRepositoryMock
            .Setup(x => x.CreateChecklistAsync(It.IsAny<IEnumerable<OffboardingChecklist>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<OffboardingChecklist>, CancellationToken>((checklist, ct) =>
            {
                capturedChecklist = checklist.ToArray();
            })
            .Returns(Task.CompletedTask);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeTerminatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new StartOffboardingCommand
        {
            EmployeeId = employeeId,
            TerminationDate = terminationDate,
            TerminationReason = "Mutual Agreement",
            EligibleForRehire = false
        };

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedChecklist);
        Assert.NotEmpty(capturedChecklist);

        // Verify essential checklist items
        Assert.Contains(capturedChecklist, item => item.ItemDescription.Contains("exit interview"));
        Assert.Contains(capturedChecklist, item => item.ItemDescription.Contains("laptop"));
        Assert.Contains(capturedChecklist, item => item.ItemDescription.Contains("access card"));
        Assert.Contains(capturedChecklist, item => item.ItemDescription.Contains("final paycheck"));

        // Verify all items have correct employee ID
        Assert.All(capturedChecklist, item  => { 
            Assert.Equal(employeeId, item.EmployeeId);
            Assert.False(item.CompletionStatus);
            Assert.True(item.DisplayOrder > 0);
         });

        // Verify blocking items exist
        var blockingItems = capturedChecklist.Where(item => item.BlocksFinalPaycheck).ToList();
        Assert.NotEmpty(blockingItems); // there should be items that block final paycheck
    }

    [Fact]
    public async Task HandleAsync_NotEligibleForRehire_PublishesCorrectEvent()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var terminationDate = DateTime.UtcNow.Date.AddDays(7);

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP003",
            LegalName = new LegalName { FirstName = "Bob", LastName = "Johnson" },
            ContactInformation = new ContactInformation { WorkEmail = "bob.johnson@company.com" },
            EmploymentStatus = EmploymentStatus.Active
        };

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _offboardingRepositoryMock
            .Setup(x => x.CreateChecklistAsync(It.IsAny<IEnumerable<OffboardingChecklist>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeTerminatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new StartOffboardingCommand
        {
            EmployeeId = employeeId,
            TerminationDate = terminationDate,
            TerminationReason = "Performance Issues",
            EligibleForRehire = false
        };

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<EmployeeTerminatedIntegrationEvent>(evt => evt.EligibleForRehire == false),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
