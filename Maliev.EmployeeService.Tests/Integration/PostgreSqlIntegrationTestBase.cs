using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Data.Interceptors;
using Maliev.EmployeeService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;
using System.Threading;
using Npgsql;

using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Tests.Integration;
// ... (start of class)



/// <summary>
/// Base class for integration tests that require PostgreSQL database
/// Uses Testcontainers to provision a real PostgreSQL instance for each test class
/// Database is automatically cleared between tests to ensure isolation
/// </summary>
public abstract class PostgreSqlIntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private static readonly SemaphoreSlim _cleanupLock = new SemaphoreSlim(1, 1);
    private static readonly Dictionary<Type, bool> _firstTestTracker = new Dictionary<Type, bool>();
    private readonly Type _testClassType;

    protected EmployeeDbContext Context { get; private set; } = null!;
    protected IEncryptionService EncryptionService { get; private set; } = null!;

    protected PostgreSqlIntegrationTestBase()
    {
        // Track test class type for per-class isolation
        _testClassType = GetType();

        // Create PostgreSQL container for testing
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
            .WithDatabase("employee_test_db")
            .WithUsername("postgres")
            .WithPassword("testpassword")
            .WithCleanUp(true)
            .Build();
    }

    /// <summary>
    /// Initialize PostgreSQL container and apply migrations
    /// Called before any test in the test class runs
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        try
        {
            // Start PostgreSQL container
            await _postgresContainer.StartAsync();

            // Setup encryption service
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ASPNETCORE_ENVIRONMENT", "Testing" }
                })
                .Build();

            EncryptionService = new EncryptionService(configuration);

            // Create DbContext with encryption service (used by value converters)
            var options = new DbContextOptionsBuilder<EmployeeDbContext>()
                .UseNpgsql(_postgresContainer.GetConnectionString())
                .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
                .EnableSensitiveDataLogging()
                .ConfigureWarnings(warnings =>
                {
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning);
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
                })
                .Options;

            Context = new EmployeeDbContext(
                options,
                EncryptionService,
                new AuditLogInterceptor(new DummyCurrentUserService(), new Microsoft.AspNetCore.Http.HttpContextAccessor()),
                new DatabaseMetricsInterceptor());


            var retries = 5;
            while (retries > 0)
            {
                try
                {
                    await Context.Database.MigrateAsync();
                    break;
                }
                catch (NpgsqlException)
                {
                    if (--retries == 0) throw;
                    await Task.Delay(5000);
                }
            }

            // Clear any existing data to ensure clean state for first test
            // This is important because some tests may not call InitializeTestAsync() manually
            await ClearDatabaseAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"INITIALIZE ASYNC FAILED: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Cleanup PostgreSQL container
    /// Called after all tests in the test class complete
    /// </summary>
    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    /// <summary>
    /// Clear all data from test database between tests
    /// Clears in proper order to respect foreign key constraints
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        // Clear change tracker first to avoid any cached entities interfering
        Context.ChangeTracker.Clear();

        // Delete in order respecting FK constraints (children first, then parents)
        await Context.DocumentVersions.ExecuteDeleteAsync();
        await Context.Documents.ExecuteDeleteAsync();
        await Context.EmergencyContacts.ExecuteDeleteAsync();
        await Context.LeaveApprovals.ExecuteDeleteAsync();
        await Context.LeaveRequests.ExecuteDeleteAsync();
        await Context.LeaveBalances.ExecuteDeleteAsync();
        await Context.LeavePolicies.ExecuteDeleteAsync();

        // Clear performance-related tables
        await Context.Set<Domain.Entities.Goal>().ExecuteDeleteAsync();
        await Context.Set<Domain.Entities.PerformanceReview>().ExecuteDeleteAsync();

        // Clear team assignments before teams and employees
        await Context.EmployeeTeamAssignments.ExecuteDeleteAsync();
        await Context.Teams.ExecuteDeleteAsync();

        // Employees last (many tables reference Employee)
        await Context.Employees.ExecuteDeleteAsync();
        await Context.Departments.ExecuteDeleteAsync();

        await Context.SaveChangesAsync();

        // Clear tracker again after deletions
        Context.ChangeTracker.Clear();
    }

    /// <summary>
    /// Initialize test with a clean database
    /// Call this at the start of each test method to ensure test isolation
    /// </summary>
    protected async Task InitializeTestAsync()
    {
        await _cleanupLock.WaitAsync();
        try
        {
            // Check if this is the first test for this test class
            if (!_firstTestTracker.ContainsKey(_testClassType))
            {
                // First test in this class - mark it and don't clear
                _firstTestTracker[_testClassType] = true;
            }
            else
            {
                // Subsequent test in this class - clear database for isolation
                await ClearDatabaseAsync();
            }
        }
        finally
        {
            _cleanupLock.Release();
        }
    }

    private class DummyCurrentUserService : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public string? UserId => "test-user-id";
        public Guid? EmployeeId => Guid.NewGuid();
        public string? Email => "test@example.com";
        public IEnumerable<string> Roles => new[] { "Employee" };
        public Role PrimaryRole => Role.Employee;
        public bool IsInRole(string role) => true;
        public IEnumerable<System.Security.Claims.Claim> Claims => Enumerable.Empty<System.Security.Claims.Claim>();
    }
}
