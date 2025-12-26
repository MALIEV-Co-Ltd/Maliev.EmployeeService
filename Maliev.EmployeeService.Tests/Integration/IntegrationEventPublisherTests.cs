using Maliev.EmployeeService.Application.IntegrationEvents;
using Maliev.EmployeeService.Infrastructure.Messaging;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for event publishing using MassTransit test harness
/// Phase 16 - T399: Integration event consumer tests
/// </summary>
public class IntegrationEventPublisherTests
{
    [Fact]
    public async Task PublishAsync_WithEmployeeCreatedEvent_ShouldPublishSuccessfully()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                // Configure in-memory test transport
            })
            .AddLogging()
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            // Create scope to resolve scoped services
            await using var scope = provider.CreateAsyncScope();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IntegrationEventPublisher>>();
            var publisher = new IntegrationEventPublisher(publishEndpoint, logger);

            var integrationEvent = new EmployeeCreatedIntegrationEvent
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                FullName = "John Doe",
                Email = "john.doe@maliev.co.th",
                StartDate = DateTime.UtcNow,
                Department = "Engineering",
                JobTitle = "Software Engineer",
                EventTimestamp = DateTime.UtcNow
            };

            // Act
            await publisher.PublishAsync(integrationEvent);

            // Assert
            var published = await harness.Published.Any<EmployeeCreatedIntegrationEvent>();
            Assert.True(published, "event should be published to the bus");

            var message = harness.Published.Select<EmployeeCreatedIntegrationEvent>().FirstOrDefault();
            Assert.NotNull(message);
            Assert.Equal(integrationEvent.EmployeeId, message!.Context.Message.EmployeeId);
            Assert.Equal("john.doe@maliev.co.th", message.Context.Message.Email);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task PublishAsync_WithOnboardingStartedEvent_ShouldPublishSuccessfully()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness()
            .AddLogging()
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            // Create scope to resolve scoped services
            await using var scope = provider.CreateAsyncScope();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IntegrationEventPublisher>>();
            var publisher = new IntegrationEventPublisher(publishEndpoint, logger);

            var integrationEvent = new EmployeeOnboardingStartedIntegrationEvent
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                FullName = "Jane Smith",
                Email = "jane.smith@maliev.co.th",
                StartDate = DateTime.UtcNow.AddDays(7),
                Department = "Marketing",
                JobTitle = "Marketing Manager",
                OnboardingStartedAt = DateTime.UtcNow
            };

            // Act
            await publisher.PublishAsync(integrationEvent);

            // Assert
            var published = await harness.Published.Any<EmployeeOnboardingStartedIntegrationEvent>();
            Assert.True(published);

            var message = harness.Published.Select<EmployeeOnboardingStartedIntegrationEvent>().FirstOrDefault();
            Assert.NotNull(message);
            Assert.Equal("Jane Smith", message!.Context.Message.FullName);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task PublishAsync_WithResilientPublisher_ShouldHandleSuccessfulPublish()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness()
            .AddLogging()
            .AddSingleton<IMeterFactory, NoOpMeterFactory>()
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            // Create scope to resolve scoped services
            await using var scope = provider.CreateAsyncScope();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IntegrationEventPublisher>>();
            var resilientLogger = scope.ServiceProvider.GetRequiredService<ILogger<ResilientIntegrationEventPublisher>>();
            var meterFactory = scope.ServiceProvider.GetRequiredService<IMeterFactory>();
            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();

            var innerPublisher = new IntegrationEventPublisher(publishEndpoint, logger);
            var resilientPublisher = new ResilientIntegrationEventPublisher(innerPublisher, resilientLogger, meterFactory, configuration);

            var integrationEvent = new EmployeeTerminatedIntegrationEvent
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                FullName = "Bob Johnson",
                Email = "bob.johnson@maliev.co.th",
                TerminationDate = DateTime.UtcNow,
                TerminationReason = "Resignation",
                Department = "Finance",
                EligibleForRehire = true,
                EventTimestamp = DateTime.UtcNow
            };

            // Act
            await resilientPublisher.PublishAsync(integrationEvent);

            // Assert
            var published = await harness.Published.Any<EmployeeTerminatedIntegrationEvent>();
            Assert.True(published, "resilient publisher should publish events");

            var message = harness.Published.Select<EmployeeTerminatedIntegrationEvent>().FirstOrDefault();
            Assert.NotNull(message);
            Assert.Equal("Resignation", message!.Context.Message.TerminationReason);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task PublishAsync_WithMultipleEvents_ShouldPublishAllSuccessfully()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness()
            .AddLogging()
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            // Create scope to resolve scoped services
            await using var scope = provider.CreateAsyncScope();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IntegrationEventPublisher>>();
            var publisher = new IntegrationEventPublisher(publishEndpoint, logger);

            var employeeId1 = Guid.NewGuid();
            var employeeId2 = Guid.NewGuid();

            var event1 = new EmployeeCreatedIntegrationEvent
            {
                EmployeeId = employeeId1,
                EmployeeNumber = "EMP001",
                FullName = "Alice Brown",
                Email = "alice@maliev.co.th",
                StartDate = DateTime.UtcNow,
                Department = "HR",
                JobTitle = "HR Manager"
            };

            var event2 = new DepartmentTransferredIntegrationEvent
            {
                EmployeeId = employeeId2,
                EmployeeNumber = "EMP004",
                FullName = "Charlie Davis",
                Email = "charlie.davis@maliev.co.th",
                FromDepartment = "Sales",
                FromDepartmentId = Guid.NewGuid(),
                ToDepartment = "Marketing",
                ToDepartmentId = Guid.NewGuid(),
                EffectiveDate = DateTime.UtcNow,
                TransferReason = "Career Development",
                EventTimestamp = DateTime.UtcNow
            };

            // Act
            await publisher.PublishAsync(event1);
            await publisher.PublishAsync(event2);

            // Assert
            var publishedCount = harness.Published.Count();
            Assert.Equal(2, publishedCount);

            var createdEventPublished = await harness.Published.Any<EmployeeCreatedIntegrationEvent>();
            Assert.True(createdEventPublished);

            var transferEventPublished = await harness.Published.Any<DepartmentTransferredIntegrationEvent>();
            Assert.True(transferEventPublished);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task PublishAsync_WithExchangeAndRoutingKey_ShouldSetHeaders()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness()
            .AddLogging()
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            // Create scope to resolve scoped services
            await using var scope = provider.CreateAsyncScope();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IntegrationEventPublisher>>();
            var publisher = new IntegrationEventPublisher(publishEndpoint, logger);

            var integrationEvent = new AccessRevocationRequiredIntegrationEvent
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeNumber = "EMP005",
                FullName = "Eve Wilson",
                Email = "eve.wilson@maliev.co.th",
                TerminationDate = DateTime.UtcNow,
                Department = "IT",
                JobTitle = "System Administrator",
                TerminationReason = "Resignation",
                EventTimestamp = DateTime.UtcNow
            };

            // Act
            await publisher.PublishAsync(integrationEvent, "employee.events", "access.revocation.required");

            // Assert
            var published = await harness.Published.Any<AccessRevocationRequiredIntegrationEvent>();
            Assert.True(published);

            var message = harness.Published.Select<AccessRevocationRequiredIntegrationEvent>().FirstOrDefault();
            Assert.NotNull(message);

            // Verify headers are set
            Assert.True(message!.Context.Headers.TryGetHeader("Exchange", out var exchange));
            Assert.Equal("employee.events", exchange);

            Assert.True(message.Context.Headers.TryGetHeader("RoutingKey", out var routingKey));
            Assert.Equal("access.revocation.required", routingKey);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task PublishAsync_VerifyMessageTimestamps_ShouldHaveCorrectTimestamps()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness()
            .AddLogging()
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            // Create scope to resolve scoped services
            await using var scope = provider.CreateAsyncScope();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IntegrationEventPublisher>>();
            var publisher = new IntegrationEventPublisher(publishEndpoint, logger);

            var beforePublish = DateTime.UtcNow;

            var integrationEvent = new OnboardingReminderNeededIntegrationEvent
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeNumber = "EMP006",
                FullName = "Frank Miller",
                Email = "frank.miller@maliev.co.th",
                StartDate = DateTime.UtcNow.AddDays(7),
                Department = "Sales",
                JobTitle = "Sales Representative",
                CompletedItems = 3,
                TotalItems = 8,
                CompletionPercentage = 37.5m,
                DaysUntilStartDate = 7,
                EventTimestamp = DateTime.UtcNow
            };

            // Act
            await publisher.PublishAsync(integrationEvent);

            var afterPublish = DateTime.UtcNow;

            // Assert
            var published = await harness.Published.Any<OnboardingReminderNeededIntegrationEvent>();
            Assert.True(published);

            var message = harness.Published.Select<OnboardingReminderNeededIntegrationEvent>().FirstOrDefault();
            Assert.NotNull(message);

            // Verify event timestamp is within expected range
            Assert.True(message!.Context.Message.EventTimestamp >= beforePublish);
            Assert.True(message.Context.Message.EventTimestamp <= afterPublish);
        }
        finally
        {
            await harness.Stop();
        }
    }
}

/// <summary>
/// No-op meter factory for testing purposes
/// </summary>
internal class NoOpMeterFactory : IMeterFactory
{
    public Meter Create(MeterOptions options)
    {
        return new Meter(options.Name, options.Version);
    }

    public void Dispose()
    {
    }
}
