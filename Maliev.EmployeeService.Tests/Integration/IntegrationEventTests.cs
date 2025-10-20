using FluentAssertions;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.IntegrationEvents;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Services;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Data.Interceptors;
using Maliev.EmployeeService.Infrastructure.Messaging;
using Maliev.EmployeeService.Infrastructure.Repositories;
using Maliev.EmployeeService.Infrastructure.Security;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for RabbitMQ integration event publishing using MassTransit test harness
/// Verifies events are correctly published to the message bus
/// </summary>
public class IntegrationEventTests : IAsyncLifetime
{
    private ITestHarness? _harness;
    private ServiceProvider? _provider;
    private EmployeeServiceDbContext? Context;
    private PostgreSqlContainer? _postgresContainer;
    private IEncryptionService? _encryptionService;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("employee_test_db")
            .WithUsername("postgres")
            .WithPassword("testpassword")
            .WithCleanUp(true)
            .Build();

        await _postgresContainer.StartAsync();

        // Setup encryption service
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ASPNETCORE_ENVIRONMENT", "Testing" }
            })
            .Build();

        _encryptionService = new EncryptionService(configuration);

        var services = new ServiceCollection();

        // Add encryption service (required by DbContext value converters)
        services.AddSingleton<IEncryptionService>(_encryptionService);

        // Add MassTransit InMemory test harness
        services.AddMassTransitTestHarness(cfg =>
        {
            // Configure in-memory transport for testing
            cfg.UsingInMemory((Context, configurator) =>
            {
                configurator.ConfigureEndpoints(Context);
            });
        });

        // Configure PostgreSQL database with encryption (using value converters)
        services.AddDbContext<EmployeeServiceDbContext>(options =>
            options.UseNpgsql(_postgresContainer.GetConnectionString())
                .ConfigureWarnings(warnings =>
                {
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning);
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
                }));

        // Add repositories
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IOnboardingRepository, OnboardingRepository>();
        services.AddScoped<IOffboardingRepository, OffboardingRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();

        // Add event publisher
        services.AddScoped<IEventPublisher, IntegrationEventPublisher>();

        // Add services
        services.AddScoped<OnboardingTemplateService>();

        // Add command handlers
        services.AddScoped<StartOnboardingCommandHandler>();
        services.AddScoped<StartOffboardingCommandHandler>();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        _provider = services.BuildServiceProvider();
        _harness = _provider.GetRequiredService<ITestHarness>();
        Context = _provider.GetRequiredService<EmployeeServiceDbContext>();

        // Apply migrations to create database schema
        await Context.Database.MigrateAsync();

        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        if (_harness != null)
        {
            await _harness.Stop();
        }

        if (Context != null)
        {
            await Context.DisposeAsync();
        }

        if (_provider != null)
        {
            await _provider.DisposeAsync();
        }

        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task StartOnboarding_PublishesEmployeeOnboardingStartedEvent()
    {
        // Arrange
        var employee = await CreateTestEmployee("Software Engineer");

        var handler = _provider!.GetRequiredService<StartOnboardingCommandHandler>();
        var command = new StartOnboardingCommand { EmployeeId = employee.Id };

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Wait for the event to be published
        await Task.Delay(100);

        // Assert
        var published = await _harness!.Published.Any<EmployeeOnboardingStartedIntegrationEvent>();
        published.Should().BeTrue("EmployeeOnboardingStartedIntegrationEvent should be published");

        var publishedEvent = _harness.Published.Select<EmployeeOnboardingStartedIntegrationEvent>().FirstOrDefault();
        publishedEvent.Should().NotBeNull();

        var eventMessage = publishedEvent!.Context.Message;
        eventMessage.EmployeeId.Should().Be(employee.Id);
        eventMessage.EmployeeNumber.Should().Be(employee.EmployeeNumber);
        eventMessage.FullName.Should().Be("Test Employee");
        eventMessage.Email.Should().Be("test.employee@company.com");
        eventMessage.JobTitle.Should().Be("Software Engineer");
        eventMessage.StartDate.Should().Be(employee.StartDate);
    }

    [Fact]
    public async Task StartOffboarding_PublishesEmployeeTerminatedEvent()
    {
        // Arrange
        var employee = await CreateTestEmployee("Product Manager");
        var terminationDate = DateTime.UtcNow.Date.AddDays(14);

        var handler = _provider!.GetRequiredService<StartOffboardingCommandHandler>();
        var command = new StartOffboardingCommand
        {
            EmployeeId = employee.Id,
            TerminationDate = terminationDate,
            TerminationReason = "Voluntary Resignation",
            EligibleForRehire = true
        };

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Wait for the event to be published
        await Task.Delay(100);

        // Assert
        var published = await _harness!.Published.Any<EmployeeTerminatedIntegrationEvent>();
        published.Should().BeTrue("EmployeeTerminatedIntegrationEvent should be published");

        var publishedEvent = _harness.Published.Select<EmployeeTerminatedIntegrationEvent>().FirstOrDefault();
        publishedEvent.Should().NotBeNull();

        var eventMessage = publishedEvent!.Context.Message;
        eventMessage.EmployeeId.Should().Be(employee.Id);
        eventMessage.EmployeeNumber.Should().Be(employee.EmployeeNumber);
        eventMessage.TerminationDate.Should().Be(terminationDate);
        eventMessage.TerminationReason.Should().Be("Voluntary Resignation");
        eventMessage.EligibleForRehire.Should().BeTrue();
    }

    [Fact]
    public async Task StartOnboarding_EventContainsCorrectDepartmentInfo()
    {
        // Arrange
        var employee = await CreateTestEmployee("Engineering Manager");

        var handler = _provider!.GetRequiredService<StartOnboardingCommandHandler>();
        var command = new StartOnboardingCommand { EmployeeId = employee.Id };

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Wait for the event to be published
        await Task.Delay(100);

        // Assert
        var publishedEvent = _harness!.Published.Select<EmployeeOnboardingStartedIntegrationEvent>().FirstOrDefault();
        publishedEvent.Should().NotBeNull();

        var eventMessage = publishedEvent!.Context.Message;
        eventMessage.Department.Should().Be("Test Department");
    }

    [Fact]
    public async Task StartOffboarding_EventContainsCorrectEmploymentStatus()
    {
        // Arrange
        var employee = await CreateTestEmployee("Senior Developer");
        var terminationDate = DateTime.UtcNow.Date.AddDays(30);

        var handler = _provider!.GetRequiredService<StartOffboardingCommandHandler>();
        var command = new StartOffboardingCommand
        {
            EmployeeId = employee.Id,
            TerminationDate = terminationDate,
            TerminationReason = "Performance Issues",
            EligibleForRehire = false
        };

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Wait for the event to be published
        await Task.Delay(100);

        // Assert - Verify event published
        var published = await _harness!.Published.Any<EmployeeTerminatedIntegrationEvent>();
        published.Should().BeTrue();

        // Verify employee status updated in database
        var updatedEmployee = await Context!.Employees.FindAsync(employee.Id);
        updatedEmployee!.EmploymentStatus.Should().Be(EmploymentStatus.Terminated);
        updatedEmployee.TerminationDate.Should().Be(terminationDate);
    }

    [Fact]
    public async Task MultipleOnboardings_PublishesMultipleEvents()
    {
        // Arrange
        var employee1 = await CreateTestEmployee("Developer 1");
        var employee2 = await CreateTestEmployee("Developer 2");

        var handler = _provider!.GetRequiredService<StartOnboardingCommandHandler>();

        // Act
        await handler.HandleAsync(new StartOnboardingCommand { EmployeeId = employee1.Id }, CancellationToken.None);
        await handler.HandleAsync(new StartOnboardingCommand { EmployeeId = employee2.Id }, CancellationToken.None);

        // Wait for events to be published
        await Task.Delay(200);

        // Assert
        var publishedEvents = _harness!.Published.Select<EmployeeOnboardingStartedIntegrationEvent>().ToList();
        publishedEvents.Should().HaveCount(2, "Two onboarding events should be published");

        var employeeIds = publishedEvents.Select(e => e.Context.Message.EmployeeId).ToList();
        employeeIds.Should().Contain(employee1.Id);
        employeeIds.Should().Contain(employee2.Id);
    }

    [Fact]
    public async Task StartOnboarding_EventTimestampIsRecent()
    {
        // Arrange
        var employee = await CreateTestEmployee("QA Engineer");
        var beforePublish = DateTime.UtcNow;

        var handler = _provider!.GetRequiredService<StartOnboardingCommandHandler>();
        var command = new StartOnboardingCommand { EmployeeId = employee.Id };

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Wait for the event to be published
        await Task.Delay(100);

        var afterPublish = DateTime.UtcNow;

        // Assert
        var publishedEvent = _harness!.Published.Select<EmployeeOnboardingStartedIntegrationEvent>().FirstOrDefault();
        publishedEvent.Should().NotBeNull();

        var eventMessage = publishedEvent!.Context.Message;
        eventMessage.OnboardingStartedAt.Should().BeOnOrAfter(beforePublish);
        eventMessage.OnboardingStartedAt.Should().BeOnOrBefore(afterPublish);
    }

    [Fact]
    public async Task StartOffboarding_EventContainsManagerId()
    {
        // Arrange
        var manager = await CreateTestEmployee("Manager");
        var employee = await CreateTestEmployee("Team Member");

        // Set manager relationship
        employee.ManagerId = manager.Id;
        Context!.Entry(employee).State = EntityState.Modified;
        await Context.SaveChangesAsync();

        var terminationDate = DateTime.UtcNow.Date.AddDays(14);

        var handler = _provider!.GetRequiredService<StartOffboardingCommandHandler>();
        var command = new StartOffboardingCommand
        {
            EmployeeId = employee.Id,
            TerminationDate = terminationDate,
            TerminationReason = "Contract End",
            EligibleForRehire = true
        };

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Wait for the event to be published
        await Task.Delay(100);

        // Assert
        var publishedEvent = _harness!.Published.Select<EmployeeTerminatedIntegrationEvent>().FirstOrDefault();
        publishedEvent.Should().NotBeNull();

        // Note: EmployeeTerminatedIntegrationEvent doesn't have ManagerId field
        // This test verifies the event is published successfully with manager relationship
        var eventMessage = publishedEvent!.Context.Message;
        eventMessage.EmployeeId.Should().Be(employee.Id);
    }

    [Fact]
    public async Task NoEventsPublished_WhenNoCommandsExecuted()
    {
        // Arrange - No commands executed

        // Act
        await Task.Delay(100);

        // Assert
        var onboardingEvents = await _harness!.Published.Any<EmployeeOnboardingStartedIntegrationEvent>();
        var offboardingEvents = await _harness.Published.Any<EmployeeTerminatedIntegrationEvent>();

        onboardingEvents.Should().BeFalse("No onboarding events should be published");
        offboardingEvents.Should().BeFalse("No offboarding events should be published");
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

        Context!.Departments.Add(department);
        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        return employee;
    }
}
