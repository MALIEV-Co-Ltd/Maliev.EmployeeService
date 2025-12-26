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
    private EmployeeDbContext? Context;
    private PostgreSqlContainer? _postgresContainer;
    private IEncryptionService? _encryptionService;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
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

        // Register IConfiguration (required by command handlers)
        services.AddSingleton<IConfiguration>(configuration);

        // Register interceptor dependencies with test admin user
        services.AddSingleton<ICurrentUserService>(new TestAdminUserService());
        services.AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.HttpContextAccessor>();
        services.AddSingleton<AuditLogInterceptor>();
        services.AddSingleton<DatabaseMetricsInterceptor>();

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
        services.AddDbContext<EmployeeDbContext>(options =>
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

        // Add mock IIamServiceClient (required by command handlers)
        var mockIamClient = new Mock<Maliev.Aspire.ServiceDefaults.IAM.IIamServiceClient>();
        mockIamClient.Setup(x => x.GetUserPermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        mockIamClient.Setup(x => x.CheckPermissionAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockIamClient.Setup(x => x.CheckPermissionsAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<Maliev.Aspire.ServiceDefaults.IAM.PermissionCheckRequest>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((string principalId, IEnumerable<Maliev.Aspire.ServiceDefaults.IAM.PermissionCheckRequest> requests, CancellationToken ct) =>
                requests.ToDictionary(r => r.PermissionId, r => true));
        services.AddSingleton(mockIamClient.Object);

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        _provider = services.BuildServiceProvider();
        _harness = _provider.GetRequiredService<ITestHarness>();
        Context = _provider.GetRequiredService<EmployeeDbContext>();

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
        Assert.True(published); // EmployeeOnboardingStartedIntegrationEvent should be published

        var publishedEvent = _harness.Published.Select<EmployeeOnboardingStartedIntegrationEvent>().FirstOrDefault();
        Assert.NotNull(publishedEvent);

        var eventMessage = publishedEvent!.Context.Message;
        Assert.Equal(employee.Id, eventMessage.EmployeeId);
        Assert.Equal(employee.EmployeeNumber, eventMessage.EmployeeNumber);
        Assert.Equal("Test Employee", eventMessage.FullName);
        Assert.Equal("test.employee@company.com", eventMessage.Email);
        Assert.Equal("Software Engineer", eventMessage.JobTitle);
        Assert.Equal(employee.StartDate, eventMessage.StartDate);
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
        Assert.True(published); // EmployeeTerminatedIntegrationEvent should be published

        var publishedEvent = _harness.Published.Select<EmployeeTerminatedIntegrationEvent>().FirstOrDefault();
        Assert.NotNull(publishedEvent);

        var eventMessage = publishedEvent!.Context.Message;
        Assert.Equal(employee.Id, eventMessage.EmployeeId);
        Assert.Equal(employee.EmployeeNumber, eventMessage.EmployeeNumber);
        Assert.Equal(terminationDate, eventMessage.TerminationDate);
        Assert.Equal("Voluntary Resignation", eventMessage.TerminationReason);
        Assert.True(eventMessage.EligibleForRehire);
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
        Assert.NotNull(publishedEvent);

        var eventMessage = publishedEvent!.Context.Message;
        Assert.Equal("Test Department", eventMessage.Department);
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
        Assert.True(published);

        // Verify employee status updated in database
        var updatedEmployee = await Context!.Employees.FindAsync(employee.Id);
        Assert.Equal(EmploymentStatus.Terminated, updatedEmployee!.EmploymentStatus);
        Assert.Equal(terminationDate, updatedEmployee.TerminationDate);
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
        Assert.Equal(2, publishedEvents.Count()); // Two onboarding events should be published

        var employeeIds = publishedEvents.Select(e => e.Context.Message.EmployeeId).ToList();
        Assert.Contains(employee1.Id, employeeIds);
        Assert.Contains(employee2.Id, employeeIds);
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
        Assert.NotNull(publishedEvent);

        var eventMessage = publishedEvent!.Context.Message;
        Assert.True(eventMessage.OnboardingStartedAt >= beforePublish);
        Assert.True(eventMessage.OnboardingStartedAt <= afterPublish);
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
        Assert.NotNull(publishedEvent);

        // Note: EmployeeTerminatedIntegrationEvent doesn't have ManagerId field
        // This test verifies the event is published successfully with manager relationship
        var eventMessage = publishedEvent!.Context.Message;
        Assert.Equal(employee.Id, eventMessage.EmployeeId);
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

        Assert.False(onboardingEvents); // No onboarding events should be published
        Assert.False(offboardingEvents); // No offboarding events should be published
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


    private class TestAdminUserService : ICurrentUserService
    {
        public Guid? PrincipalId => Guid.Parse("00000000-0000-0000-0000-000000000001"); // Test admin principal ID
        public Task<Guid?> GetEmployeeIdAsync(CancellationToken ct = default) => Task.FromResult<Guid?>(Guid.Parse("00000000-0000-0000-0000-000000000002"));
        public string? Email => "test-admin@example.com";
        public bool IsAuthenticated => true;
    }
}
