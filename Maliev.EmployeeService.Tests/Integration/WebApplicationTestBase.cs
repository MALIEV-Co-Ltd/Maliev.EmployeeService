using Maliev.EmployeeService.Api;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Base class for integration tests that test HTTP endpoints
/// Uses WebApplicationFactory to start the full API stack
/// TODO: Violates testing policy requiring PostgreSQL - needs refactoring for proper database setup
/// </summary>
public abstract class WebApplicationTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory _factory;
    protected readonly HttpClient _client;

    protected WebApplicationTestBase(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // Set default authorization header for tests (optional - can be removed per test)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Helper method to create a test department
    /// </summary>
    protected async Task<Department> CreateTestDepartmentAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EmployeeServiceDbContext>();

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        context.Departments.Add(department);
        await context.SaveChangesAsync();

        return department;
    }

    /// <summary>
    /// Helper method to create a test employee
    /// </summary>
    protected async Task<Employee> CreateTestEmployeeAsync(
        Guid departmentId,
        string employeeNumber,
        string firstName = "Test",
        string lastName = "Employee",
        string email = "test@example.com")
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EmployeeServiceDbContext>();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = employeeNumber,
            LegalName = new Domain.ValueObjects.LegalName(firstName, lastName),
            ContactInformation = new Domain.ValueObjects.ContactInformation
            {
                WorkEmail = email,
                MobilePhone = "+66123456789"
            },
            DepartmentId = departmentId,
            EmploymentStatus = Domain.Enums.EmploymentStatus.Active,
            EmploymentType = Domain.Enums.EmploymentType.FullTime,
            StartDate = DateTime.UtcNow,
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        return employee;
    }
}

/// <summary>
/// Custom WebApplicationFactory for testing
/// Configures in-memory database and test authentication
/// TODO: Violates testing policy requiring PostgreSQL - needs refactoring
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Shared database name for all DbContext instances in this factory
    // This ensures test data persists across HTTP requests
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "RABBITMQ_ENABLED", "false" }, // Disable RabbitMQ for tests
                { "ASPNETCORE_ENVIRONMENT", "Testing" },
                { "ConnectionStrings:EmployeeServiceDb", "Host=localhost;Database=test" } // Dummy connection string
                // ENCRYPTION_KEY not provided - will use built-in development fallback
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Register encryption service (required by DbContext)
            services.AddSingleton<IEncryptionService>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                return new EncryptionService(configuration);
            });

            // Add in-memory database for testing
            // Program.cs skips DbContext registration in Testing environment, so we provide it here
            // Use shared database name so test data persists across HTTP requests
            services.AddDbContext<EmployeeServiceDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                // Suppress the ManyServiceProvidersCreatedWarning for integration tests
                // Multiple test classes create their own factory instances, which is expected
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning));
            });
        });

        builder.UseEnvironment("Testing");
    }
}
