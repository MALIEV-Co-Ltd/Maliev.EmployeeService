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
            PrincipalId = Guid.NewGuid(), // Generate unique PrincipalId to avoid constraint violations
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
    protected void AuthenticateAs(Guid employeeId, string[]? roles = null, string[]? permissions = null)
    {
        // Lookup the employee's PrincipalId
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
        var employee = context.Employees.Find(employeeId);

        if (employee == null)
        {
            // If employee not found, authenticate with the ID directly (for role-only tests)
            AuthenticateAsPrincipal(employeeId, roles, permissions);
            return;
        }

        var additionalClaims = new Dictionary<string, string>
        {
            { "employee_id", employeeId.ToString() }
        };

        // Use the employee's PrincipalId as the subject (for GetEmployeeIdAsync lookup)
        var token = _factory.CreateTestJwtToken(employee.PrincipalId.ToString(), roles, permissions, additionalClaims);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void AuthenticateAsPrincipal(Guid principalId, string[]? roles = null, string[]? permissions = null)
    {
        var additionalClaims = new Dictionary<string, string>();
        var token = _factory.CreateTestJwtToken(principalId.ToString(), roles, permissions, additionalClaims);
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
    }

    protected override void ConfigureEnvironmentVariables()
    {
        // Disable IAM registration in tests - uses the service's built-in degraded mode
    }

    protected override void ConfigureAdditionalServices(IServiceCollection services)
    {
        // Remove any existing IIamServiceClient registration from Program.cs
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Maliev.Aspire.ServiceDefaults.IAM.IIamServiceClient));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }

        // Register mock IIamServiceClient for tests where IAM integration is disabled
        var mockIamClient = new Mock<Maliev.Aspire.ServiceDefaults.IAM.IIamServiceClient>();

        // Mock GetUserPermissionsAsync to return empty permissions
        mockIamClient.Setup(x => x.GetUserPermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Mock CheckPermissionAsync to return true (allow all in tests)
        mockIamClient.Setup(x => x.CheckPermissionAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Mock CheckPermissionsAsync to return all true
        mockIamClient.Setup(x => x.CheckPermissionsAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<Maliev.Aspire.ServiceDefaults.IAM.PermissionCheckRequest>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((string principalId, IEnumerable<Maliev.Aspire.ServiceDefaults.IAM.PermissionCheckRequest> requests, CancellationToken ct) =>
                requests.ToDictionary(r => r.PermissionId, r => true));

        services.AddSingleton<Maliev.Aspire.ServiceDefaults.IAM.IIamServiceClient>(mockIamClient.Object);
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
