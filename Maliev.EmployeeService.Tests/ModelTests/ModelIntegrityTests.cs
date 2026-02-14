using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Infrastructure.Data.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace Maliev.EmployeeService.Tests.ModelTests;

/// <summary>
/// Verifies that the EF Core model matches the current migrations.
/// This prevents "Pending model changes" exceptions at runtime.
/// </summary>
public class ModelIntegrityTests
{
    [Fact]
    public void Model_ShouldNotHavePendingChanges()
    {
        // Use a dummy connection string just to build the model for comparison
        var options = new DbContextOptionsBuilder<EmployeeDbContext>()
            .UseNpgsql("Host=localhost;Database=ModelCheck")
            .Options;

        // Mock dependencies required for constructor
        var encryptionServiceMock = new Mock<IEncryptionService>();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        var auditLogInterceptor = new AuditLogInterceptor(currentUserServiceMock.Object, httpContextAccessorMock.Object);
        var databaseMetricsInterceptor = new DatabaseMetricsInterceptor();

        using var context = new EmployeeDbContext(
            options,
            encryptionServiceMock.Object,
            auditLogInterceptor,
            databaseMetricsInterceptor);

        // This helper (available in EF Core 9.0+) checks if the current code
        // matches the last snapshot in the Migrations folder.
        var hasChanges = context.Database.HasPendingModelChanges();

        Assert.False(hasChanges,
            "The EF Core model for 'EmployeeDbContext' has changed but no migration has been added. " +
            "Run 'dotnet ef migrations add <Name> --project Maliev.EmployeeService.Infrastructure --startup-project Maliev.EmployeeService.Api' to fix this.");
    }
}
