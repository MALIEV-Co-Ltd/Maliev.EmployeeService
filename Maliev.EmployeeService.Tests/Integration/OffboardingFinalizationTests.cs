using FluentAssertions;
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
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for offboarding finalization
/// Verifies paycheck blocking validation until all checklist items are complete
/// </summary>
public class OffboardingFinalizationTests : PostgreSqlIntegrationTestBase
{
    [Fact]
    public async Task OffboardingFinalization_WithIncompleteBlockingItems_CannotReleasePaycheck()
    {
        // Arrange
        var eventPublisherMock = new Mock<IEventPublisher>();
        var offboardingRepository = new OffboardingRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockLogger = new Mock<ILogger<StartOffboardingCommandHandler>>();

        var startOffboardingHandler = new StartOffboardingCommandHandler(
            employeeRepository,
            offboardingRepository,
            eventPublisherMock.Object,
            mockLogger.Object);

        var mockLogger2 = new Mock<ILogger<GetOffboardingStatusQueryHandler>>();
        var getStatusHandler = new GetOffboardingStatusQueryHandler(
            employeeRepository,
            offboardingRepository,
            mockLogger2.Object);

        var mockLogger3 = new Mock<ILogger<CompleteOffboardingItemCommandHandler>>();
        var completeItemHandler = new CompleteOffboardingItemCommandHandler(
            offboardingRepository,
            mockLogger3.Object);

        var employee = await CreateTestEmployee();
        var terminationDate = DateTime.UtcNow.Date.AddDays(14);

        eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeTerminatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var startCommand = new StartOffboardingCommand
        {
            EmployeeId = employee.Id,
            TerminationDate = terminationDate,
            TerminationReason = "Resignation",
            EligibleForRehire = true
        };

        await startOffboardingHandler.HandleAsync(startCommand, CancellationToken.None);

        // Act
        var statusQuery = new GetOffboardingStatusQuery { EmployeeId = employee.Id };
        var status = await getStatusHandler.HandleAsync(statusQuery, CancellationToken.None);

        // Assert
        status.CanReleaseFinalPaycheck.Should().BeFalse(
            "Cannot release paycheck when blocking items are incomplete");

        var blockingItems = status.ChecklistItems.Where(item => item.BlocksFinalPaycheck && !item.CompletionStatus).ToList();
        status.BlockingItemsCount.Should().Be(blockingItems.Count);
        status.BlockingItemsCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task OffboardingFinalization_CompleteAllBlockingItems_AllowsPaycheckRelease()
    {
        // Arrange
        var eventPublisherMock = new Mock<IEventPublisher>();
        var offboardingRepository = new OffboardingRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockLogger = new Mock<ILogger<StartOffboardingCommandHandler>>();

        var startOffboardingHandler = new StartOffboardingCommandHandler(
            employeeRepository,
            offboardingRepository,
            eventPublisherMock.Object,
            mockLogger.Object);

        var mockLogger2 = new Mock<ILogger<GetOffboardingStatusQueryHandler>>();
        var getStatusHandler = new GetOffboardingStatusQueryHandler(
            employeeRepository,
            offboardingRepository,
            mockLogger2.Object);

        var mockLogger3 = new Mock<ILogger<CompleteOffboardingItemCommandHandler>>();
        var completeItemHandler = new CompleteOffboardingItemCommandHandler(
            offboardingRepository,
            mockLogger3.Object);

        var employee = await CreateTestEmployee();
        var terminationDate = DateTime.UtcNow.Date.AddDays(14);

        eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeTerminatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var startCommand = new StartOffboardingCommand
        {
            EmployeeId = employee.Id,
            TerminationDate = terminationDate,
            TerminationReason = "Mutual Agreement",
            EligibleForRehire = true
        };

        await startOffboardingHandler.HandleAsync(startCommand, CancellationToken.None);

        // Get initial status
        var initialStatusQuery = new GetOffboardingStatusQuery { EmployeeId = employee.Id };
        var initialStatus = await getStatusHandler.HandleAsync(initialStatusQuery, CancellationToken.None);

        initialStatus.CanReleaseFinalPaycheck.Should().BeFalse();

        // Get all blocking items
        var blockingItems = initialStatus.ChecklistItems.Where(item => item.BlocksFinalPaycheck).ToList();

        // Act - Complete all blocking items
        foreach (var item in blockingItems)
        {
            await completeItemHandler.HandleAsync(
                new CompleteOffboardingItemCommand
                {
                    ItemId = item.Id,
                    CompletedByEmployeeId = employee.Id,  // Use actual employee ID to satisfy FK constraint
                    Notes = $"Completed: {item.ItemDescription}"
                },
                CancellationToken.None);
        }

        // Get updated status
        var finalStatusQuery = new GetOffboardingStatusQuery { EmployeeId = employee.Id };
        var finalStatus = await getStatusHandler.HandleAsync(finalStatusQuery, CancellationToken.None);

        // Assert
        finalStatus.CanReleaseFinalPaycheck.Should().BeTrue(
            "Should allow paycheck release when all blocking items are complete");

        finalStatus.BlockingItemsCount.Should().Be(0,
            "No blocking items should remain incomplete");

        // Verify all blocking items are marked as complete
        var allBlockingItemsComplete = finalStatus.ChecklistItems
            .Where(item => item.BlocksFinalPaycheck)
            .All(item => item.CompletionStatus);

        allBlockingItemsComplete.Should().BeTrue(
            "All blocking items should be marked as complete");
    }

    [Fact]
    public async Task OffboardingFinalization_PartiallyCompleteBlockingItems_CannotReleasePaycheck()
    {
        // Arrange
        var eventPublisherMock = new Mock<IEventPublisher>();
        var offboardingRepository = new OffboardingRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockLogger = new Mock<ILogger<StartOffboardingCommandHandler>>();

        var startOffboardingHandler = new StartOffboardingCommandHandler(
            employeeRepository,
            offboardingRepository,
            eventPublisherMock.Object,
            mockLogger.Object);

        var mockLogger2 = new Mock<ILogger<GetOffboardingStatusQueryHandler>>();
        var getStatusHandler = new GetOffboardingStatusQueryHandler(
            employeeRepository,
            offboardingRepository,
            mockLogger2.Object);

        var mockLogger3 = new Mock<ILogger<CompleteOffboardingItemCommandHandler>>();
        var completeItemHandler = new CompleteOffboardingItemCommandHandler(
            offboardingRepository,
            mockLogger3.Object);

        var employee = await CreateTestEmployee();
        var terminationDate = DateTime.UtcNow.Date.AddDays(14);

        eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeTerminatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var startCommand = new StartOffboardingCommand
        {
            EmployeeId = employee.Id,
            TerminationDate = terminationDate,
            TerminationReason = "Performance",
            EligibleForRehire = false
        };

        await startOffboardingHandler.HandleAsync(startCommand, CancellationToken.None);

        var statusQuery = new GetOffboardingStatusQuery { EmployeeId = employee.Id };
        var status = await getStatusHandler.HandleAsync(statusQuery, CancellationToken.None);

        var blockingItems = status.ChecklistItems.Where(item => item.BlocksFinalPaycheck).ToList();
        var itemsToComplete = blockingItems.Take(blockingItems.Count - 1).ToList(); // Complete all but one

        // Act - Complete most blocking items, but leave one incomplete
        foreach (var item in itemsToComplete)
        {
            await completeItemHandler.HandleAsync(
                new CompleteOffboardingItemCommand
                {
                    ItemId = item.Id,
                    CompletedByEmployeeId = employee.Id,  // Use actual employee ID to satisfy FK constraint
                    Notes = "Completed"
                },
                CancellationToken.None);
        }

        var finalStatusQuery = new GetOffboardingStatusQuery { EmployeeId = employee.Id };
        var finalStatus = await getStatusHandler.HandleAsync(finalStatusQuery, CancellationToken.None);

        // Assert
        finalStatus.CanReleaseFinalPaycheck.Should().BeFalse(
            "Cannot release paycheck when even one blocking item is incomplete");

        finalStatus.BlockingItemsCount.Should().BeGreaterThan(0,
            "At least one blocking item should remain incomplete");
    }

    [Fact]
    public async Task OffboardingFinalization_CompleteNonBlockingItems_DoesNotAffectPaycheckRelease()
    {
        // Arrange
        var eventPublisherMock = new Mock<IEventPublisher>();
        var offboardingRepository = new OffboardingRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockLogger = new Mock<ILogger<StartOffboardingCommandHandler>>();

        var startOffboardingHandler = new StartOffboardingCommandHandler(
            employeeRepository,
            offboardingRepository,
            eventPublisherMock.Object,
            mockLogger.Object);

        var mockLogger2 = new Mock<ILogger<GetOffboardingStatusQueryHandler>>();
        var getStatusHandler = new GetOffboardingStatusQueryHandler(
            employeeRepository,
            offboardingRepository,
            mockLogger2.Object);

        var mockLogger3 = new Mock<ILogger<CompleteOffboardingItemCommandHandler>>();
        var completeItemHandler = new CompleteOffboardingItemCommandHandler(
            offboardingRepository,
            mockLogger3.Object);

        var employee = await CreateTestEmployee();
        var terminationDate = DateTime.UtcNow.Date.AddDays(14);

        eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeTerminatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var startCommand = new StartOffboardingCommand
        {
            EmployeeId = employee.Id,
            TerminationDate = terminationDate,
            TerminationReason = "Contract End",
            EligibleForRehire = true
        };

        await startOffboardingHandler.HandleAsync(startCommand, CancellationToken.None);

        var statusQuery = new GetOffboardingStatusQuery { EmployeeId = employee.Id };
        var status = await getStatusHandler.HandleAsync(statusQuery, CancellationToken.None);

        var nonBlockingItems = status.ChecklistItems.Where(item => !item.BlocksFinalPaycheck).ToList();

        // Act - Complete all non-blocking items
        foreach (var item in nonBlockingItems)
        {
            await completeItemHandler.HandleAsync(
                new CompleteOffboardingItemCommand
                {
                    ItemId = item.Id,
                    CompletedByEmployeeId = employee.Id,  // Use actual employee ID to satisfy FK constraint
                    Notes = "Completed"
                },
                CancellationToken.None);
        }

        var finalStatusQuery = new GetOffboardingStatusQuery { EmployeeId = employee.Id };
        var finalStatus = await getStatusHandler.HandleAsync(finalStatusQuery, CancellationToken.None);

        // Assert
        finalStatus.CanReleaseFinalPaycheck.Should().BeFalse(
            "Completing only non-blocking items should not allow paycheck release");

        // Verify blocking items are still incomplete
        var incompleteBlockingItems = finalStatus.ChecklistItems
            .Where(item => item.BlocksFinalPaycheck && !item.CompletionStatus)
            .ToList();

        incompleteBlockingItems.Should().NotBeEmpty(
            "Blocking items should remain incomplete");
    }

    [Fact]
    public async Task OffboardingFinalization_GetPaycheckBlockingItems_ReturnsOnlyBlockingItems()
    {
        // Arrange
        var eventPublisherMock = new Mock<IEventPublisher>();
        var offboardingRepository = new OffboardingRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockLogger = new Mock<ILogger<StartOffboardingCommandHandler>>();

        var startOffboardingHandler = new StartOffboardingCommandHandler(
            employeeRepository,
            offboardingRepository,
            eventPublisherMock.Object,
            mockLogger.Object);

        var mockLogger2 = new Mock<ILogger<GetOffboardingStatusQueryHandler>>();
        var getStatusHandler = new GetOffboardingStatusQueryHandler(
            employeeRepository,
            offboardingRepository,
            mockLogger2.Object);

        var mockLogger3 = new Mock<ILogger<CompleteOffboardingItemCommandHandler>>();
        var completeItemHandler = new CompleteOffboardingItemCommandHandler(
            offboardingRepository,
            mockLogger3.Object);

        var employee = await CreateTestEmployee();
        var terminationDate = DateTime.UtcNow.Date.AddDays(14);

        eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeTerminatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var startCommand = new StartOffboardingCommand
        {
            EmployeeId = employee.Id,
            TerminationDate = terminationDate,
            TerminationReason = "Layoff",
            EligibleForRehire = true
        };

        await startOffboardingHandler.HandleAsync(startCommand, CancellationToken.None);

        // Act
        var blockingItems = await offboardingRepository.GetPaycheckBlockingItemsAsync(
            employee.Id,
            CancellationToken.None);

        // Assert
        blockingItems.Should().NotBeEmpty();
        blockingItems.Should().OnlyContain(item => item.BlocksFinalPaycheck == true,
            "Should only return items that block paycheck");

        // Verify specific blocking items
        var blockingItemsList = blockingItems.ToList();
        blockingItemsList.Should().Contain(item => item.ItemDescription.Contains("laptop"));
        blockingItemsList.Should().Contain(item => item.ItemDescription.Contains("access card"));
    }

    [Fact]
    public async Task OffboardingFinalization_TrackCompletionPercentage_UpdatesCorrectly()
    {
        // Arrange
        var eventPublisherMock = new Mock<IEventPublisher>();
        var offboardingRepository = new OffboardingRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockLogger = new Mock<ILogger<StartOffboardingCommandHandler>>();

        var startOffboardingHandler = new StartOffboardingCommandHandler(
            employeeRepository,
            offboardingRepository,
            eventPublisherMock.Object,
            mockLogger.Object);

        var mockLogger2 = new Mock<ILogger<GetOffboardingStatusQueryHandler>>();
        var getStatusHandler = new GetOffboardingStatusQueryHandler(
            employeeRepository,
            offboardingRepository,
            mockLogger2.Object);

        var mockLogger3 = new Mock<ILogger<CompleteOffboardingItemCommandHandler>>();
        var completeItemHandler = new CompleteOffboardingItemCommandHandler(
            offboardingRepository,
            mockLogger3.Object);

        var employee = await CreateTestEmployee();
        var terminationDate = DateTime.UtcNow.Date.AddDays(14);

        eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeTerminatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var startCommand = new StartOffboardingCommand
        {
            EmployeeId = employee.Id,
            TerminationDate = terminationDate,
            TerminationReason = "Retirement",
            EligibleForRehire = true
        };

        await startOffboardingHandler.HandleAsync(startCommand, CancellationToken.None);

        var initialStatusQuery = new GetOffboardingStatusQuery { EmployeeId = employee.Id };
        var initialStatus = await getStatusHandler.HandleAsync(initialStatusQuery, CancellationToken.None);

        var totalItems = initialStatus.TotalItems;
        var itemsToComplete = initialStatus.ChecklistItems.Take(totalItems / 2).ToList();

        // Act - Complete half of the items
        foreach (var item in itemsToComplete)
        {
            await completeItemHandler.HandleAsync(
                new CompleteOffboardingItemCommand
                {
                    ItemId = item.Id,
                    CompletedByEmployeeId = employee.Id,  // Use actual employee ID to satisfy FK constraint
                    Notes = "Completed"
                },
                CancellationToken.None);
        }

        var finalStatusQuery = new GetOffboardingStatusQuery { EmployeeId = employee.Id };
        var finalStatus = await getStatusHandler.HandleAsync(finalStatusQuery, CancellationToken.None);

        // Assert
        finalStatus.CompletedItems.Should().Be(itemsToComplete.Count);
        finalStatus.CompletionPercentage.Should().BeApproximately(50m, 10m,
            "Completion percentage should be around 50% when half the items are complete");
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
