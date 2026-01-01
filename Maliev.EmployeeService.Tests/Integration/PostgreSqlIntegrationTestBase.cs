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

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Base class for integration tests that require PostgreSQL database.
/// Uses Testcontainers to provision a real PostgreSQL instance for each test class.
/// Database is automatically cleared between tests to ensure isolation.
/// </summary>
public abstract class PostgreSqlIntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private static readonly SemaphoreSlim _cleanupLock = new SemaphoreSlim(1, 1);
    private static readonly Dictionary<Type, bool> _firstTestTracker = new Dictionary<Type, bool>();
    private readonly Type _testClassType;

    /// <summary>
    /// Gets the database context for the Employee Service.
    /// </summary>
    protected EmployeeDbContext Context { get; private set; } = null!;

    /// <summary>
    /// Gets the encryption service for handling sensitive data.
    /// </summary>
    protected IEncryptionService EncryptionService { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlIntegrationTestBase"/> class.
    /// </summary>
    protected PostgreSqlIntegrationTestBase()
    {
        _testClassType = GetType();

        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
            .WithDatabase("employee_test_db")
            .WithUsername("postgres")
            .WithPassword("testpassword")
            .WithCleanUp(true)
            .Build();
    }

    /// <summary>
    /// Initialize PostgreSQL container and apply migrations.
    /// Called before any test in the test class runs.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task InitializeAsync()
    {
        try
        {
            await _postgresContainer.StartAsync();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ASPNETCORE_ENVIRONMENT", "Testing" }
                })
                .Build();

            EncryptionService = new EncryptionService(configuration);

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

            await ClearDatabaseAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"INITIALIZE ASYNC FAILED: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Cleanup PostgreSQL container.
    /// Called after all tests in the test class complete.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        if (Context != null) await Context.DisposeAsync();
        if (_postgresContainer != null) await _postgresContainer.DisposeAsync();
    }

    /// <summary>
    /// Clear all data from test database between tests.
    /// Clears in proper order to respect foreign key constraints.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task ClearDatabaseAsync()
    {
        Context.ChangeTracker.Clear();

        await Context.EmergencyContacts.ExecuteDeleteAsync();
        await Context.EmployeeTeamAssignments.ExecuteDeleteAsync();
        await Context.Teams.ExecuteDeleteAsync();
        await Context.Employees.ExecuteDeleteAsync();
        await Context.Departments.ExecuteDeleteAsync();

        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    /// <summary>
    /// Initialize test with a clean database.
    /// Call this at the start of each test method to ensure test isolation.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task InitializeTestAsync()
    {
        await _cleanupLock.WaitAsync();
        try
        {
            if (!_firstTestTracker.ContainsKey(_testClassType))
            {
                _firstTestTracker[_testClassType] = true;
            }
            else
            {
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
        public Guid? PrincipalId => null;
        public Task<Guid?> GetEmployeeIdAsync(CancellationToken ct = default) => Task.FromResult<Guid?>(null);
        public string? Email => null;
        public bool IsAuthenticated => false;
    }
}