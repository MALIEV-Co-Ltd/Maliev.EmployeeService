using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.IntegrationEvents;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Maliev.Aspire.ServiceDefaults.IAM;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for offboarding workflow
/// Verifies complete offboarding flow with asset return checklist tracking
/// </summary>
public class OffboardingWorkflowTests : PostgreSqlIntegrationTestBase
{
    private Mock<IEventPublisher> _eventPublisherMock = null!;
    private StartOffboardingCommandHandler _startOffboardingHandler = null!;
    private GetOffboardingStatusQueryHandler _getStatusHandler = null!;
    private CompleteOffboardingItemCommandHandler _completeItemHandler = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Initialize mock
        _eventPublisherMock = new Mock<IEventPublisher>();

        // Initialize repositories
        var employeeRepository = new EmployeeRepository(Context);
        var offboardingRepository = new OffboardingRepository(Context);

        // Initialize handlers
        var mockLogger1 = new Mock<ILogger<StartOffboardingCommandHandler>>();
        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());

        _startOffboardingHandler = new StartOffboardingCommandHandler(
            employeeRepository,
            offboardingRepository,
            _eventPublisherMock.Object,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object,
            mockLogger1.Object);

        var mockLogger2 = new Mock<ILogger<GetOffboardingStatusQueryHandler>>();
        _getStatusHandler = new GetOffboardingStatusQueryHandler(
            employeeRepository,
            offboardingRepository,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object,
            mockLogger2.Object);

        var mockLogger3 = new Mock<ILogger<CompleteOffboardingItemCommandHandler>>();
        _completeItemHandler = new CompleteOffboardingItemCommandHandler(
            offboardingRepository,
            mockLogger3.Object);
    }



    [Fact]
    public async Task StartOffboarding_ValidEmployee_CreatesChecklistAndPublishesEvent()
    {
        // Arrange
        var employee = await CreateTestEmployee();
        var terminationDate = DateTime.UtcNow.Date.AddDays(14);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeTerminatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new StartOffboardingCommand
        {
            EmployeeId = employee.Id,
            TerminationDate = terminationDate,
            TerminationReason = "Voluntary Resignation",
            EligibleForRehire = true
        };

        // Act
        await _startOffboardingHandler.HandleAsync(command, CancellationToken.None);

        // Assert
        var checklist = await Context.OffboardingChecklists
            .Where(c => c.EmployeeId == employee.Id)
            .ToListAsync();

        Assert.NotEmpty(checklist);

        // Verify asset return items
        Assert.Contains(checklist, item => item.ItemDescription.Contains("laptop"));
        Assert.Contains(checklist, item => item.ItemDescription.Contains("access card"));
        Assert.Contains(checklist, item => item.ItemDescription.Contains("keys"));

        // Verify integration event was published
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<EmployeeTerminatedIntegrationEvent>(evt =>
                    evt.EmployeeId == employee.Id &&
                    evt.TerminationDate == terminationDate &&
                    evt.TerminationReason == "Voluntary Resignation" &&
                    evt.EligibleForRehire == true),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify employee status updated
        var updatedEmployee = await Context.Employees.FindAsync(employee.Id);
        Assert.Equal(EmploymentStatus.Terminated, updatedEmployee!.EmploymentStatus);
        Assert.Equal(terminationDate, updatedEmployee.TerminationDate);
    }

    [Fact]
    public async Task OffboardingWorkflow_AssetReturnTracking_UpdatesCorrectly()
    {
        // Arrange
        var employee = await CreateTestEmployee();
        var terminationDate = DateTime.UtcNow.Date.AddDays(7);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeTerminatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var startCommand = new StartOffboardingCommand
        {
            EmployeeId = employee.Id,
            TerminationDate = terminationDate,
            TerminationReason = "Contract End",
            EligibleForRehire = false
        };

        await _startOffboardingHandler.HandleAsync(startCommand, CancellationToken.None);

        // Get initial status
        var initialStatusQuery = new GetOffboardingStatusQuery { EmployeeId = employee.Id };
        var initialStatus = await _getStatusHandler.HandleAsync(initialStatusQuery, CancellationToken.None);

        // Find asset return items
        var laptopItem = initialStatus.ChecklistItems
            .First(item => item.ItemDescription.Contains("laptop"));
        var accessCardItem = initialStatus.ChecklistItems
            .First(item => item.ItemDescription.Contains("access card"));

        // Act - Complete asset return items
        await _completeItemHandler.HandleAsync(
            new CompleteOffboardingItemCommand
            {
                ItemId = laptopItem.Id,
                CompletedByEmployeeId = employee.Id,  // Use actual employee ID to satisfy FK constraint
                Notes = "Laptop returned in good condition"
            },
            CancellationToken.None);

        await _completeItemHandler.HandleAsync(
            new CompleteOffboardingItemCommand
            {
                ItemId = accessCardItem.Id,
                CompletedByEmployeeId = employee.Id,  // Use actual employee ID to satisfy FK constraint
                Notes = "Access card returned"
            },
            CancellationToken.None);

        // Get updated status
        var updatedStatusQuery = new GetOffboardingStatusQuery { EmployeeId = employee.Id };
        var updatedStatus = await _getStatusHandler.HandleAsync(updatedStatusQuery, CancellationToken.None);

        // Assert
        Assert.Equal(2, updatedStatus.CompletedItems);
        Assert.Contains(updatedStatus.ChecklistItems, item => item.Id == laptopItem.Id &&
            item.CompletionStatus == true &&
            item.Notes == "Laptop returned in good condition");

        Assert.Contains(updatedStatus.ChecklistItems, item => item.Id == accessCardItem.Id &&
            item.CompletionStatus == true &&
            item.Notes == "Access card returned");
    }

    [Fact]
    public async Task OffboardingWorkflow_HasPaycheckBlockingItems()
    {
        // Arrange
        var employee = await CreateTestEmployee();
        var terminationDate = DateTime.UtcNow.Date.AddDays(14);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeTerminatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new StartOffboardingCommand
        {
            EmployeeId = employee.Id,
            TerminationDate = terminationDate,
            TerminationReason = "Performance Issues",
            EligibleForRehire = false
        };

        // Act
        await _startOffboardingHandler.HandleAsync(command, CancellationToken.None);

        var statusQuery = new GetOffboardingStatusQuery { EmployeeId = employee.Id };
        var status = await _getStatusHandler.HandleAsync(statusQuery, CancellationToken.None);

        // Assert
        var blockingItems = status.ChecklistItems.Where(item => item.BlocksFinalPaycheck).ToList();
        Assert.NotEmpty(blockingItems); // There should be items that block final paycheck

        // Verify specific blocking items exist
        Assert.Contains(blockingItems, item => item.ItemDescription.Contains("laptop"));
        Assert.Contains(blockingItems, item => item.ItemDescription.Contains("access card"));
        Assert.Contains(blockingItems, item => item.ItemDescription.Contains("expense reimbursements"));

        // Verify cannot release paycheck initially
        Assert.False(status.CanReleaseFinalPaycheck,
            "Cannot release paycheck when blocking items are incomplete");
    }

    [Fact]
    public async Task OffboardingWorkflow_WithOverdueItems_MarksThemCorrectly()
    {
        // Arrange
        var employee = await CreateTestEmployee();
        var terminationDate = DateTime.UtcNow.Date.AddDays(-5); // Past termination date

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeTerminatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new StartOffboardingCommand
        {
            EmployeeId = employee.Id,
            TerminationDate = terminationDate,
            TerminationReason = "Layoff",
            EligibleForRehire = true
        };

        // Act
        await _startOffboardingHandler.HandleAsync(command, CancellationToken.None);

        var statusQuery = new GetOffboardingStatusQuery { EmployeeId = employee.Id };
        var status = await _getStatusHandler.HandleAsync(statusQuery, CancellationToken.None);

        // Assert
        Assert.True(status.ChecklistItems.Any(item => item.IsOverdue == true),
            "Some items should be overdue since termination date is in the past");
    }

    [Fact]
    public async Task CompleteOffboardingItem_UpdatesCompletedDate()
    {
        // Arrange
        var employee = await CreateTestEmployee();
        var terminationDate = DateTime.UtcNow.Date.AddDays(14);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeTerminatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var startCommand = new StartOffboardingCommand
        {
            EmployeeId = employee.Id,
            TerminationDate = terminationDate,
            TerminationReason = "Mutual Agreement",
            EligibleForRehire = true
        };

        await _startOffboardingHandler.HandleAsync(startCommand, CancellationToken.None);

        var checklist = await Context.OffboardingChecklists
            .Where(c => c.EmployeeId == employee.Id)
            .ToListAsync();

        var itemToComplete = checklist.First();

        // Act
        var completeCommand = new CompleteOffboardingItemCommand
        {
            ItemId = itemToComplete.Id,
            CompletedByEmployeeId = employee.Id,  // Use actual employee ID to satisfy FK constraint
            Notes = "Completed as scheduled"
        };
        await _completeItemHandler.HandleAsync(completeCommand, CancellationToken.None);

        // Assert
        var completedItem = await Context.OffboardingChecklists.FindAsync(itemToComplete.Id);
        Assert.NotNull(completedItem);
        Assert.True(completedItem!.CompletionStatus);
        Assert.NotNull(completedItem.CompletedDate);
        Assert.True(Math.Abs((completedItem.CompletedDate!.Value - DateTime.UtcNow).TotalSeconds) <= 5);
        Assert.Equal("Completed as scheduled", completedItem.Notes);
    }

    [Fact]
    public async Task OffboardingWorkflow_ChecklistItemsHaveCorrectResponsibleParties()
    {
        // Arrange
        var employee = await CreateTestEmployee();
        var terminationDate = DateTime.UtcNow.Date.AddDays(14);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeTerminatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new StartOffboardingCommand
        {
            EmployeeId = employee.Id,
            TerminationDate = terminationDate,
            TerminationReason = "Retirement",
            EligibleForRehire = true
        };

        // Act
        await _startOffboardingHandler.HandleAsync(command, CancellationToken.None);

        var statusQuery = new GetOffboardingStatusQuery { EmployeeId = employee.Id };
        var status = await _getStatusHandler.HandleAsync(statusQuery, CancellationToken.None);

        // Assert - Verify different responsible parties are assigned
        Assert.Contains(status.ChecklistItems, item => item.ResponsibleParty == "HR");
        Assert.Contains(status.ChecklistItems, item => item.ResponsibleParty == "IT");
        Assert.Contains(status.ChecklistItems, item => item.ResponsibleParty == "Facilities");
        Assert.Contains(status.ChecklistItems, item => item.ResponsibleParty == "Manager");
    }

    /// <summary>
    /// Helper method to create a test employee
    /// </summary>
    private async Task<Employee> CreateTestEmployee()
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Test Department",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = $"EMP{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
            LegalName = new LegalName
            {
                FirstName = "Test",
                LastName = "Employee"
            },
            ContactInformation = new ContactInformation
            {
                WorkEmail = "test.employee@company.com"
            },
            JobTitle = "Test Engineer",
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow.Date.AddMonths(-6),
            DepartmentId = department.Id,
            Department = department,
            CreatedDate = DateTime.UtcNow
        };

        Context.Departments.Add(department);
        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        return employee;
    }
}
