using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Maliev.EmployeeService.Api.HealthChecks;

/// <summary>
/// Health check that verifies database connectivity
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly EmployeeServiceDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseHealthCheck"/> class
    /// </summary>
    /// <param name="dbContext">The database context</param>
    public DatabaseHealthCheck(EmployeeServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Checks the health of the database connection
    /// </summary>
    /// <param name="context">The health check context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the health check result</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("Database connection successful")
                : HealthCheckResult.Unhealthy("Cannot connect to database");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }
}
