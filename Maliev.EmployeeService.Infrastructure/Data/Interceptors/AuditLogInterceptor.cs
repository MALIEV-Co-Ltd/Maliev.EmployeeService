using System.Text.Json;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Maliev.EmployeeService.Infrastructure.Data.Interceptors;

/// <summary>
/// EF Core interceptor that automatically creates audit log entries for all data changes
/// </summary>
public class AuditLogInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogInterceptor(
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor)
    {
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CreateAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CreateAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CreateAuditLogs(DbContext? context)
    {
        if (context == null) return;

        var auditEntries = new List<AuditLog>();
        var userId = _currentUserService.EmployeeId;
        var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var purpose = GetAuditPurpose();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Skip audit log entries themselves to avoid recursion
            if (entry.Entity is AuditLog || entry.State == EntityState.Unchanged || entry.State == EntityState.Detached)
                continue;

            var auditLog = new AuditLog
            {
                LogId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                EntityType = entry.Entity.GetType().Name,
                EntityId = GetEntityId(entry),
                Action = entry.State.ToString(),
                IpAddress = ipAddress,
                Purpose = purpose ?? DeterminePurposeFromEntity(entry)
            };

            // Capture old and new values
            if (entry.State == EntityState.Modified)
            {
                auditLog.OldValues = SerializeOriginalValues(entry);
                auditLog.NewValues = SerializeCurrentValues(entry);
            }
            else if (entry.State == EntityState.Added)
            {
                auditLog.NewValues = SerializeCurrentValues(entry);
            }
            else if (entry.State == EntityState.Deleted)
            {
                auditLog.OldValues = SerializeOriginalValues(entry);
            }

            auditEntries.Add(auditLog);
        }

        // Add audit logs to context
        if (auditEntries.Any())
        {
            context.Set<AuditLog>().AddRange(auditEntries);
        }
    }

    private static Guid GetEntityId(EntityEntry entry)
    {
        var keyProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        return keyProperty?.CurrentValue is Guid id ? id : Guid.Empty;
    }

    private static string SerializeCurrentValues(EntityEntry entry)
    {
        var values = entry.Properties
            .Where(p => !p.Metadata.IsPrimaryKey())
            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);

        return JsonSerializer.Serialize(values);
    }

    private static string SerializeOriginalValues(EntityEntry entry)
    {
        var values = entry.Properties
            .Where(p => !p.Metadata.IsPrimaryKey())
            .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);

        return JsonSerializer.Serialize(values);
    }

    /// <summary>
    /// Retrieves the audit purpose from multiple sources:
    /// 1. X-Audit-Purpose HTTP header
    /// 2. Request path/endpoint information
    /// 3. Query parameter 'auditPurpose'
    /// </summary>
    private string? GetAuditPurpose()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        // Priority 1: Check for explicit X-Audit-Purpose header
        if (httpContext.Request.Headers.TryGetValue("X-Audit-Purpose", out var headerValue))
        {
            return headerValue.ToString();
        }

        // Priority 2: Check query parameter
        if (httpContext.Request.Query.TryGetValue("auditPurpose", out var queryValue))
        {
            return queryValue.ToString();
        }

        // Priority 3: Derive from request path and method
        var path = httpContext.Request.Path.ToString();
        var method = httpContext.Request.Method;

        // Extract meaningful purpose from path patterns
        if (path.Contains("/compensation", StringComparison.OrdinalIgnoreCase))
        {
            return $"Compensation Data Access: {method} {path}";
        }
        else if (path.Contains("/performance", StringComparison.OrdinalIgnoreCase))
        {
            return $"Performance Review Access: {method} {path}";
        }
        else if (path.Contains("/benefits", StringComparison.OrdinalIgnoreCase))
        {
            return $"Benefits Information Access: {method} {path}";
        }

        // Fallback: Generic API call description
        return $"API Call: {method} {path}";
    }

    /// <summary>
    /// Determines audit purpose based on entity type and action for sensitive data
    /// </summary>
    private static string? DeterminePurposeFromEntity(EntityEntry entry)
    {
        var entityType = entry.Entity.GetType().Name;
        var action = entry.State.ToString();

        // Special handling for sensitive entities
        return entityType switch
        {
            "CompensationRecord" => $"Compensation {action} - Payroll/Benefits Administration",
            "SalaryHistory" => $"Salary {action} - Compensation Review",
            "BenefitsEnrollment" => $"Benefits {action} - Employee Benefits Management",
            "PerformanceReview" => $"Performance {action} - Employee Development",
            "DisciplinaryAction" => $"Disciplinary {action} - Employee Relations",
            "PersonalDocument" => $"Document {action} - HR Records Management",
            "EmergencyContact" => $"Emergency Contact {action} - Employee Safety",
            _ => $"{entityType} {action}"
        };
    }
}
