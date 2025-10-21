using Maliev.EmployeeService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that periodically updates business metrics
/// Constitution Principle X - Business Metrics
/// Phase 15 - T427 Technical Health Metrics
/// </summary>
public class MetricsUpdateBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetricsUpdateBackgroundService> _logger;
    private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(5);

    public MetricsUpdateBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MetricsUpdateBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Metrics Update Background Service started. Update interval: {Interval} minutes",
            _updateInterval.TotalMinutes);

        // Wait 1 minute before first update to allow application to fully start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateMetricsAsync(stoppingToken);
                await Task.Delay(_updateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                _logger.LogInformation("Metrics Update Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating business metrics");
                // Wait before retrying
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task UpdateMetricsAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var metricsService = scope.ServiceProvider.GetRequiredService<BusinessMetricsService>();

            _logger.LogDebug("Updating business metrics");

            await metricsService.UpdateAllMetricsAsync(cancellationToken);
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;

            BackgroundJobMetrics.RecordSuccess("MetricsUpdate", duration);
            _logger.LogInformation("Business metrics updated successfully in {Duration:F2} seconds", duration);
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            BackgroundJobMetrics.RecordFailure("MetricsUpdate", duration);
            _logger.LogError(ex, "Error updating business metrics");
            throw;
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Metrics Update Background Service is stopping");
        return base.StopAsync(cancellationToken);
    }
}
