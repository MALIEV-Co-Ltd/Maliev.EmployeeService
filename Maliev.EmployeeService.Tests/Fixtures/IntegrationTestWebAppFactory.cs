using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Testcontainers.RabbitMq;

namespace Maliev.EmployeeService.Tests.Fixtures;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RedisContainer _redisContainer;
    private readonly RabbitMqContainer _rabbitmqContainer;
    private readonly RSA _testRsa;
    private bool _containersStarted;
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    public IntegrationTestWebAppFactory()
    {
        // Generate ephemeral RSA key for test JWT tokens
        _testRsa = RSA.Create(2048);

        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
            .WithDatabase("employee_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        _rabbitmqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:4.2.1-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        if (_containersStarted)
            return;

        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _redisContainer.StartAsync(),
            _rabbitmqContainer.StartAsync()
        );

        // Wait for Redis to be ready
        using (var connection = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString()))
        {
            var db = connection.GetDatabase();
            await db.PingAsync();
        }

        _containersStarted = true;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.DisposeAsync().AsTask(),
            _redisContainer.DisposeAsync().AsTask(),
            _rabbitmqContainer.DisposeAsync().AsTask()
        );
        _testRsa.Dispose();
        await base.DisposeAsync();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure containers are started before creating host
        if (!_containersStarted)
        {
            InitializeAsync().GetAwaiter().GetResult();
        }

        // Set connection strings as environment variables
        Environment.SetEnvironmentVariable("ConnectionStrings__EmployeeDbContext", _postgresContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__redis", _redisContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__rabbitmq", _rabbitmqContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Service:Name"] = "EmployeeService",
                ["Service:Version"] = "1.0.0-test"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            services.RemoveAll<DbContextOptions<EmployeeDbContext>>();
            services.RemoveAll<EmployeeDbContext>();

            // Add DbContext with Testcontainers connection string
            services.AddDbContext<EmployeeDbContext>(options =>
            {
                options.UseNpgsql(_postgresContainer.GetConnectionString());
            });

            // Add permission-based authorization infrastructure for tests
            services.AddHttpContextAccessor();
            
            services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider,
                                  Maliev.Aspire.ServiceDefaults.Authorization.PermissionAuthorizationPolicyProvider>();
            services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
                               Maliev.Aspire.ServiceDefaults.Authorization.PermissionAuthorizationHandler>();
            services.AddAuthorizationBuilder();

            // PostConfigure JWT Bearer options to use our test RSA key
            services.PostConfigureAll<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = TestIssuer,
                    ValidAudience = TestAudience,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(_testRsa),
                    ClockSkew = TimeSpan.Zero // No clock skew for tests
                };
            });
        });
    }

    /// <summary>
    /// Creates a test JWT token with specified claims for integration testing.
    /// </summary>
    /// <param name="userId">User ID claim</param>
    /// <param name="roles">User roles</param>
    /// <param name="additionalClaims">Additional claims to include</param>
    /// <returns>JWT token string</returns>
    public string CreateTestJwtToken(string userId = "test-user", string[]? roles = null, Dictionary<string, string>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add roles
        roles ??= new[] { "roles.employee.employee" };
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add additional claims
        if (additionalClaims != null)
        {
            foreach (var (key, value) in additionalClaims)
            {
                claims.Add(new Claim(key, value));
            }
        }

        var credentials = new SigningCredentials(
            new RsaSecurityKey(_testRsa),
            SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public HttpClient CreateAuthenticatedClientWithAllPermissions(string userId = "test-user")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, "roles.employee.admin")
        };

        // Add all employee permissions
        var allPermissions = new[]
        {
            "employee.profiles.create", "employee.profiles.read", "employee.profiles.update", "employee.profiles.delete",
            "employee.compensation.read", "employee.compensation.update",
            "employee.departments.manage",
            "employee.documents.create", "employee.documents.read", "employee.documents.update", "employee.documents.delete",
            "employee.leave.create", "employee.leave.read", "employee.leave.approve", "employee.leave.cancel",
            "employee.performance.create", "employee.performance.read", "employee.performance.update",
            "employee.training.create", "employee.training.read", "employee.training.assign",
            "employee.teams.read", "employee.teams.manage",
            "employee.employees.search",
            "employee.onboarding.manage", "employee.workauth.manage",
            "employee.reports.view", "employee.reports.generate",
            "employee.admin.backgroundjobs", "employee.admin.systemconfig",
            "employee.system.admin"
        };

        foreach (var permission in allPermissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var credentials = new SigningCredentials(
            new RsaSecurityKey(_testRsa),
            SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokenString}");
        return client;
    }
}








