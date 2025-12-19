using System.Net.Http.Headers;
using System.Net.Http.Json;
using Maliev.EmployeeService.Api;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Security;
using Maliev.EmployeeService.Tests.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Base class for integration tests that test HTTP endpoints
/// Uses unified BaseIntegrationTestFactory for consistent infrastructure
/// </summary>
public abstract class WebApplicationTestBase : IClassFixture<EmployeeServiceTestFactory>, IAsyncLifetime
{
    protected readonly EmployeeServiceTestFactory _factory;
    protected readonly HttpClient _client;

    protected WebApplicationTestBase(EmployeeServiceTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // Set default authorization header with a valid test JWT token
        var token = factory.CreateTestJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Helper method to create a test department
    /// </summary>
    protected async Task<Department> CreateTestDepartmentAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();

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
        var context = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();

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
    protected void AuthenticateAs(Guid employeeId, string[]? roles = null)
    {
        var additionalClaims = new Dictionary<string, string>
        {
            { "employee_id", employeeId.ToString() }
        };
        var token = _factory.CreateTestJwtToken(employeeId.ToString(), roles, additionalClaims);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}

/// <summary>
/// EmployeeService integration test factory using unified base class
/// </summary>
public class EmployeeServiceTestFactory : BaseIntegrationTestFactory<Program, EmployeeDbContext>
{
    public EmployeeServiceTestFactory()
    {
        // Force initialization of metrics (same as Program.cs line 295)
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(
            typeof(Maliev.EmployeeService.Application.Services.BusinessMetricsService).TypeHandle);
    }

    /// <summary>
    /// Override CreateDbContext to provide required dependencies for EmployeeDbContext
    /// </summary>
    public override EmployeeDbContext CreateDbContext()
    {
        var connectionString = Environment.GetEnvironmentVariable($"ConnectionStrings__{DbConnectionStringName}")
            ?? throw new InvalidOperationException($"Connection string '{DbConnectionStringName}' not found");

        var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<EmployeeDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        // Create mock dependencies for testing
        var mockEncryptionService = new Mock<IEncryptionService>();
        mockEncryptionService.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        mockEncryptionService.Setup(x => x.Decrypt(It.IsAny<string>())).Returns<string>(s => s);

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var auditLogInterceptor = new Infrastructure.Data.Interceptors.AuditLogInterceptor(
            mockCurrentUserService.Object,
            mockHttpContextAccessor.Object);
        var databaseMetricsInterceptor = new Infrastructure.Data.Interceptors.DatabaseMetricsInterceptor();

        return new EmployeeDbContext(
            optionsBuilder.Options,
            mockEncryptionService.Object,
            auditLogInterceptor,
            databaseMetricsInterceptor);
    }
}
