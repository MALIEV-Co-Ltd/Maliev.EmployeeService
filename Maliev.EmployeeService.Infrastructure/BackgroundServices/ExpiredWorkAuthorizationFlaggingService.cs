using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that flags employees with expired work authorizations
/// Runs daily at 1:00 AM to identify and log employees requiring urgent attention
/// User Story 11 - Work Authorization & Visa Tracking
/// </summary>
public class ExpiredWorkAuthorizationFlaggingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiredWorkAuthorizationFlaggingService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    public ExpiredWorkAuthorizationFlaggingService(
        IServiceProvider serviceProvider,
        ILogger<ExpiredWorkAuthorizationFlaggingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expired Work Authorization Flagging Service started");

        // Wait until 1:00 AM for first run
        await WaitUntil1AM(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FlagExpiredAuthorizationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flagging expired work authorizations");
            }

            // Wait 24 hours until next run
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task WaitUntil1AM(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var next1AM = now.Date.AddHours(1);

        if (now.Hour >= 1)
        {
            next1AM = next1AM.AddDays(1);
        }

        var delay = next1AM - now;

        if (delay.TotalMilliseconds > 0)
        {
            _logger.LogInformation("Waiting {Delay} until next 1:00 AM UTC run", delay);
            await Task.Delay(delay, cancellationToken);
        }
    }

    private async Task FlagExpiredAuthorizationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var workAuthRepository = scope.ServiceProvider.GetRequiredService<IWorkAuthorizationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ExpiredWorkAuthorizationFlaggingService>>();

        logger.LogInformation("Starting expired work authorization flagging at {Time}", DateTime.UtcNow);

        try
        {
            // Get all expired authorizations
            var expired = await workAuthRepository.GetExpiredAsync(cancellationToken);
            var expiredList = expired.ToList();

            if (expiredList.Any())
            {
                logger.LogWarning(
                    "Found {Count} expired work authorizations requiring attention",
                    expiredList.Count);

                var flaggedCount = 0;

                foreach (var auth in expiredList.Where(a => a.IsActive))
                {
                    try
                    {
                        // Flag as inactive (expired)
                        auth.IsActive = false;
                        workAuthRepository.Update(auth);
                        flaggedCount++;

                        logger.LogWarning(
                            "EXPIRED WORK AUTHORIZATION - Employee: {EmployeeName} ({EmployeeNumber}), " +
                            "Department: {Department}, Type: {AuthType}, Document: {DocNumber}, " +
                            "Expired: {ExpirationDate}, Days overdue: {DaysOverdue}",
                            auth.Employee?.LegalName?.FullName ?? "Unknown",
                            auth.Employee?.EmployeeNumber ?? "Unknown",
                            auth.Employee?.Department?.Name ?? "Unknown",
                            auth.AuthorizationType,
                            auth.DocumentNumber,
                            auth.ExpirationDate?.ToString("yyyy-MM-dd"),
                            auth.ExpirationDate.HasValue
                                ? Math.Abs(Math.Round((DateTime.UtcNow - auth.ExpirationDate.Value).TotalDays))
                                : 0);

                        // TODO: Send urgent notification to HR and compliance team
                        // This would integrate with a notification service
                        // Example: await _notificationService.SendExpiredAuthAlertAsync(auth);

                        // TODO: Create compliance violation record or audit log entry
                        // Example: await _complianceService.RecordViolationAsync(auth.EmployeeId, "EXPIRED_WORK_AUTH");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Error flagging expired authorization {AuthId} for employee {EmployeeId}",
                            auth.Id,
                            auth.EmployeeId);
                    }
                }

                if (flaggedCount > 0)
                {
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    logger.LogWarning(
                        "Flagged {Count} work authorizations as inactive (expired). HR action required!",
                        flaggedCount);
                }
            }
            else
            {
                logger.LogInformation("No expired work authorizations found - all employees are compliant");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in expired work authorization flagging process");
        }

        logger.LogInformation("Completed expired work authorization flagging check");
    }
}
