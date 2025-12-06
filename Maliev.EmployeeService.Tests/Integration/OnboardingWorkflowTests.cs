using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.IntegrationEvents;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Application.Services;
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
/// Integration tests for onboarding workflow
/// Verifies complete onboarding flow with event publishing
/// </summary>
public class OnboardingWorkflowTests : PostgreSqlIntegrationTestBase
{
    private Mock<IEventPublisher> _eventPublisherMock = null!;
    private StartOnboardingCommandHandler _startOnboardingHandler = null!;
    private GetOnboardingStatusQueryHandler _getStatusHandler = null!;
    private CompleteOnboardingItemCommandHandler _completeItemHandler = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Initialize mock
        _eventPublisherMock = new Mock<IEventPublisher>();

        // Initialize repositories
        var employeeRepository = new EmployeeRepository(Context);
        var onboardingRepository = new OnboardingRepository(Context);

        // Initialize template service (no dependencies)
        var templateService = new OnboardingTemplateService();

        // Initialize handlers
        var mockLogger1 = new Mock<ILogger<StartOnboardingCommandHandler>>();
        _startOnboardingHandler = new StartOnboardingCommandHandler(
            employeeRepository,
            onboardingRepository,
            templateService,
            _eventPublisherMock.Object,
            mockLogger1.Object);

        var mockLogger2 = new Mock<ILogger<GetOnboardingStatusQueryHandler>>();
        _getStatusHandler = new GetOnboardingStatusQueryHandler(
            onboardingRepository,
            mockLogger2.Object);

        var mockLogger3 = new Mock<ILogger<CompleteOnboardingItemCommandHandler>>();
        _completeItemHandler = new CompleteOnboardingItemCommandHandler(
            onboardingRepository,
            mockLogger3.Object);
    }



    [Fact]
    public async Task StartOnboarding_ValidEmployee_CreatesChecklistAndPublishesEvent()
    {
        // Arrange
        var employee = await CreateTestEmployee("Software Engineer");

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeOnboardingStartedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new StartOnboardingCommand { EmployeeId = employee.Id };

        // Act
        await _startOnboardingHandler.HandleAsync(command, CancellationToken.None);

        // Assert
        var checklist = await Context.OnboardingChecklists
            .Where(c => c.EmployeeId == employee.Id)
            .ToListAsync();

        Assert.NotEmpty(checklist);
        Assert.Contains(checklist, item => item.ItemDescription.Contains("laptop"));
        Assert.Contains(checklist, item => item.ItemDescription.Contains("email account"));

        // Verify integration event was published with correct data
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<EmployeeOnboardingStartedIntegrationEvent>(evt =>
                    evt.EmployeeId == employee.Id &&
                    evt.EmployeeNumber == employee.EmployeeNumber &&
                    evt.Email == employee.ContactInformation.WorkEmail &&
                    evt.JobTitle == "Software Engineer"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnboardingWorkflow_CompleteFlow_UpdatesStatusCorrectly()
    {
        // Arrange
        var employee = await CreateTestEmployee("Product Manager");

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeOnboardingStartedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Start onboarding
        var startCommand = new StartOnboardingCommand { EmployeeId = employee.Id };
        await _startOnboardingHandler.HandleAsync(startCommand, CancellationToken.None);

        // Get initial status
        var initialStatusQuery = new GetOnboardingStatusQuery { EmployeeId = employee.Id };
        var initialStatus = await _getStatusHandler.HandleAsync(initialStatusQuery, CancellationToken.None);

        Assert.True(initialStatus.TotalItems > 0);
        Assert.Equal(0, initialStatus.CompletedItems);
        Assert.Equal(0m, initialStatus.CompletionPercentage);

        // Complete first item
        var firstItem = initialStatus.ChecklistItems.First();
        var completeCommand = new CompleteOnboardingItemCommand
        {
            ItemId = firstItem.Id,
            CompletedByEmployeeId = employee.Id,  // Use actual employee ID to satisfy FK constraint
            Notes = "Completed successfully"
        };
        await _completeItemHandler.HandleAsync(completeCommand, CancellationToken.None);

        // Get updated status
        var updatedStatusQuery = new GetOnboardingStatusQuery { EmployeeId = employee.Id };
        var updatedStatus = await _getStatusHandler.HandleAsync(updatedStatusQuery, CancellationToken.None);

        // Assert
        Assert.Equal(1, updatedStatus.CompletedItems);
        Assert.True(updatedStatus.CompletionPercentage > 0m);
        Assert.Contains(updatedStatus.ChecklistItems, item => item.Id == firstItem.Id &&
            item.CompletionStatus == true &&
            item.Notes == "Completed successfully");
    }

    [Fact]
    public async Task StartOnboarding_ForManager_CreatesLeadershipItems()
    {
        // Arrange
        var employee = await CreateTestEmployee("Engineering Manager");

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeOnboardingStartedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new StartOnboardingCommand { EmployeeId = employee.Id };

        // Act
        await _startOnboardingHandler.HandleAsync(command, CancellationToken.None);

        // Assert
        var checklist = await Context.OnboardingChecklists
            .Where(c => c.EmployeeId == employee.Id)
            .ToListAsync();

        Assert.Contains(checklist, item => item.ItemDescription.Contains("leadership"));
        Assert.Contains(checklist, item => item.ItemDescription.Contains("management training"));
    }

    [Fact]
    public async Task StartOnboarding_ForFactoryWorker_CreatesSafetyItems()
    {
        // Arrange
        var employee = await CreateTestEmployee("Production Operator");

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeOnboardingStartedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new StartOnboardingCommand { EmployeeId = employee.Id };

        // Act
        await _startOnboardingHandler.HandleAsync(command, CancellationToken.None);

        // Assert
        var checklist = await Context.OnboardingChecklists
            .Where(c => c.EmployeeId == employee.Id)
            .ToListAsync();

        Assert.Contains(checklist, item => item.ItemDescription.Contains("safety training"));
        Assert.Contains(checklist, item => item.ItemDescription.Contains("safety equipment"));
    }

    [Fact]
    public async Task GetOnboardingStatus_WithOverdueItems_MarksThemCorrectly()
    {
        // Arrange
        var employee = await CreateTestEmployee("Developer");
        var startDate = DateTime.UtcNow.Date.AddDays(-7); // Start date in the past
        employee.StartDate = startDate;
        Context.Entry(employee).State = EntityState.Modified;
        await Context.SaveChangesAsync();

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeOnboardingStartedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Start onboarding with past start date
        var startCommand = new StartOnboardingCommand { EmployeeId = employee.Id };
        await _startOnboardingHandler.HandleAsync(startCommand, CancellationToken.None);

        // Act
        var statusQuery = new GetOnboardingStatusQuery { EmployeeId = employee.Id };
        var status = await _getStatusHandler.HandleAsync(statusQuery, CancellationToken.None);

        // Assert
        Assert.True(status.ChecklistItems.Any(item => item.IsOverdue == true),
            "Some items should be overdue since start date is in the past");
    }

    [Fact]
    public async Task CompleteOnboardingItem_UpdatesCompletedDate()
    {
        // Arrange
        var employee = await CreateTestEmployee("Analyst");

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeOnboardingStartedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var startCommand = new StartOnboardingCommand { EmployeeId = employee.Id };
        await _startOnboardingHandler.HandleAsync(startCommand, CancellationToken.None);

        var checklist = await Context.OnboardingChecklists
            .Where(c => c.EmployeeId == employee.Id)
            .ToListAsync();

        var itemToComplete = checklist.First();

        // Act
        var completeCommand = new CompleteOnboardingItemCommand
        {
            ItemId = itemToComplete.Id,
            CompletedByEmployeeId = employee.Id,  // Use actual employee ID to satisfy FK constraint
            Notes = "Test completion"
        };
        await _completeItemHandler.HandleAsync(completeCommand, CancellationToken.None);

        // Assert
        var completedItem = await Context.OnboardingChecklists.FindAsync(itemToComplete.Id);
        Assert.NotNull(completedItem);
        Assert.True(completedItem!.CompletionStatus);
        Assert.NotNull(completedItem.CompletedDate);
        Assert.True(Math.Abs((completedItem.CompletedDate!.Value - DateTime.UtcNow).TotalSeconds) <= 5);
        Assert.Equal("Test completion", completedItem.Notes);
    }

    /// <summary>
    /// Helper method to create a test employee
    /// </summary>
    private async Task<Employee> CreateTestEmployee(string jobTitle)
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
            JobTitle = jobTitle,
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow.Date.AddDays(7),
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
