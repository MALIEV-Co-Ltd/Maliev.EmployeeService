using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Prometheus;

namespace Maliev.EmployeeService.Infrastructure.Data.Interceptors;

/// <summary>
/// EF Core interceptor for tracking database query metrics
/// Phase 15 - T426 Technical Health Metrics
/// </summary>
public class DatabaseMetricsInterceptor : DbCommandInterceptor
{
    private static readonly Histogram QueryDuration = Metrics.CreateHistogram(
        "db_query_duration_seconds",
        "Database query execution duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "command_type", "operation" },
            Buckets = new[] { 0.001, 0.005, 0.01, 0.05, 0.1, 0.5, 1.0, 5.0, 10.0 }
        });

    private static readonly Counter QueryTotal = Metrics.CreateCounter(
        "db_queries_total",
        "Total number of database queries executed",
        new CounterConfiguration
        {
            LabelNames = new[] { "command_type", "operation", "status" }
        });

    // Static constructor to initialize metrics with default values
    static DatabaseMetricsInterceptor()
    {
        // Initialize with labeled variations to ensure metrics and labels appear in output
        QueryDuration.WithLabels("Text", "select").Observe(0);
        QueryTotal.WithLabels("Text", "select", "success").Inc(0);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        eventData.Context?.ChangeTracker.Entries();
        return base.ReaderExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        eventData.Context?.ChangeTracker.Entries();
        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        RecordMetrics(command, eventData.Duration, "success");
        return base.ReaderExecuted(command, eventData, result);
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        RecordMetrics(command, eventData.Duration, "success");
        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override void CommandFailed(
        DbCommand command,
        CommandErrorEventData eventData)
    {
        RecordMetrics(command, eventData.Duration, "error");
        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        RecordMetrics(command, eventData.Duration, "error");
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        RecordMetrics(command, eventData.Duration, "success");
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        RecordMetrics(command, eventData.Duration, "success");
        return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        RecordMetrics(command, eventData.Duration, "success");
        return base.ScalarExecuted(command, eventData, result);
    }

    public override async ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        RecordMetrics(command, eventData.Duration, "success");
        return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void RecordMetrics(DbCommand command, TimeSpan duration, string status)
    {
        var commandType = command.CommandType.ToString();
        var operation = DetermineOperation(command.CommandText);
        var durationSeconds = duration.TotalSeconds;

        // Record duration histogram
        QueryDuration
            .WithLabels(commandType, operation)
            .Observe(durationSeconds);

        // Record query counter
        QueryTotal
            .WithLabels(commandType, operation, status)
            .Inc();
    }

    private static string DetermineOperation(string commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText))
            return "unknown";

        var trimmed = commandText.TrimStart().ToUpperInvariant();

        if (trimmed.StartsWith("SELECT"))
            return "select";
        if (trimmed.StartsWith("INSERT"))
            return "insert";
        if (trimmed.StartsWith("UPDATE"))
            return "update";
        if (trimmed.StartsWith("DELETE"))
            return "delete";
        if (trimmed.StartsWith("CREATE"))
            return "create";
        if (trimmed.StartsWith("ALTER"))
            return "alter";
        if (trimmed.StartsWith("DROP"))
            return "drop";

        return "other";
    }
}
