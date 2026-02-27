using Maliev.EmployeeService.Application.Commands;
using Maliev.MessagingContracts.Contracts.Employee;
using Maliev.MessagingContracts;
using Maliev.EmployeeService.Application.Interfaces;
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

[Collection("IntegrationTests")]
public class IntegrationEventTests : IAsyncLifetime
{
    private ITestHarness? _harness;
    private ServiceProvider? _provider;
    private EmployeeDbContext? Context;
    private PostgreSqlContainer? _postgresContainer;
    private IEncryptionService? _encryptionService;

    [Obsolete]
    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder().WithImage("postgres:18-alpine")
            .WithDatabase("employee_test_db")
            .WithUsername("postgres")
            .WithPassword("testpassword")
            .WithCleanUp(true)
            .Build();

        await _postgresContainer.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ASPNETCORE_ENVIRONMENT", "Testing" }
            })
            .Build();

        _encryptionService = new EncryptionService(configuration);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ICurrentUserService>(new TestAdminUserService());
        services.AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.HttpContextAccessor>();
        services.AddSingleton<AuditLogInterceptor>();
        services.AddSingleton<DatabaseMetricsInterceptor>();
        services.AddSingleton<IEncryptionService>(_encryptionService);

        services.AddMassTransitTestHarness(cfg =>
        {
            cfg.UsingInMemory((Context, configurator) =>
            {
                configurator.ConfigureEndpoints(Context);
            });
        });

        services.AddDbContext<EmployeeDbContext>(options =>
            options.UseNpgsql(_postgresContainer.GetConnectionString())
                .ConfigureWarnings(warnings =>
                {
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning);
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
                }));

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IEventPublisher, IntegrationEventPublisher>();

        services.AddScoped<StartOnboardingCommandHandler>();
        services.AddScoped<StartOffboardingCommandHandler>();

        var mockIamClient = new Mock<Maliev.Aspire.ServiceDefaults.IAM.IIamServiceClient>();
        mockIamClient.Setup(x => x.GetUserPermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        mockIamClient.Setup(x => x.CheckPermissionAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        services.AddSingleton(mockIamClient.Object);

        services.AddLogging(builder => builder.AddConsole());

        _provider = services.BuildServiceProvider();
        _harness = _provider.GetRequiredService<ITestHarness>();
        Context = _provider.GetRequiredService<EmployeeDbContext>();

        await Context.Database.MigrateAsync();
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        if (_harness != null) await _harness.Stop();
        if (Context != null) await Context.DisposeAsync();
        if (_provider != null) await _provider.DisposeAsync();
        if (_postgresContainer != null) await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task StartOnboarding_PublishesEmployeeCreatedEvent()
    {
        var employee = await CreateTestEmployee("Software Engineer");
        var handler = _provider!.GetRequiredService<StartOnboardingCommandHandler>();
        var command = new StartOnboardingCommand { EmployeeId = employee.Id };

        await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(await _harness!.Published.Any<EmployeeCreatedEvent>());
        var publishedEvent = _harness.Published.Select<EmployeeCreatedEvent>().FirstOrDefault();
        Assert.NotNull(publishedEvent);
        Assert.Equal(employee.Id, publishedEvent!.Context.Message.Payload.EmployeeId);
    }

    [Fact]
    public async Task StartOffboarding_PublishesEmployeeTerminatedEvent()
    {
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

        await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(await _harness!.Published.Any<EmployeeTerminatedEvent>());
        var publishedEvent = _harness.Published.Select<EmployeeTerminatedEvent>().FirstOrDefault();
        Assert.NotNull(publishedEvent);
        Assert.Equal(employee.Id, publishedEvent!.Context.Message.Payload.EmployeeId);
        Assert.Equal(terminationDate, publishedEvent.Context.Message.Payload.TerminationDate);
    }

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
            LegalName = new LegalName { FirstName = "Test", LastName = "Employee" },
            ContactInformation = new ContactInformation { WorkEmail = "test.employee@company.com" },
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
        public Guid? PrincipalId => Guid.Parse("00000000-0000-0000-0000-000000000001");
        public string? PrincipalIdentifier => PrincipalId?.ToString();
        public Task<Guid?> GetEmployeeIdAsync(CancellationToken ct = default) => Task.FromResult<Guid?>(Guid.Empty);
        public string? Email => "test-admin@example.com";
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => true; // Admin has all permissions in tests
    }
}
