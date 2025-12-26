using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Maliev.EmployeeService.Infrastructure.BackgroundServices;

/// <summary>
/// Static helper class for tracking background job metrics
/// Phase 15 - T427 Technical Health Metrics
/// </summary>
public static class BackgroundJobMetrics
{
    private static readonly Meter Meter = new("employees");

    private static readonly Histogram<double> ExecutionDuration = Meter.CreateHistogram<double>(
        "background_job_execution_duration_seconds",
        "seconds",
        "Background job execution duration");

    private static readonly Counter<long> SuccessTotal = Meter.CreateCounter<long>(
        "background_job_success_total",
        "1",
        "Total number of successful background job executions");

    private static readonly Counter<long> FailureTotal = Meter.CreateCounter<long>(
        "background_job_failure_total",
        "1",
        "Total number of failed background job executions");

    // Store for the observable gauge
    private static readonly ConcurrentDictionary<string, long> _lastExecutionTimestamps = new();

    static BackgroundJobMetrics()
    {
        Meter.CreateObservableGauge("background_job_last_execution_timestamp_seconds", () =>
            _lastExecutionTimestamps.Select(kvp => new Measurement<long>(kvp.Value,
                new KeyValuePair<string, object?>("job_name", kvp.Key))),
            description: "Unix timestamp of the last execution of a background job");

        // Initialize with default values if needed, though OTEL doesn't strictly require "init" labels like prometheus-net sometimes did for visibility
    }

    /// <summary>
    /// Record a successful background job execution
    /// </summary>
    /// <param name="jobName">Name of the background job</param>
    /// <param name="durationSeconds">Duration of execution in seconds</param>
    public static void RecordSuccess(string jobName, double durationSeconds)
    {
        ExecutionDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("job_name", jobName),
            new KeyValuePair<string, object?>("status", "success"));

        SuccessTotal.Add(1, new KeyValuePair<string, object?>("job_name", jobName));

        _lastExecutionTimestamps[jobName] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>
    /// Record a failed background job execution
    /// </summary>
    /// <param name="jobName">Name of the background job</param>
    /// <param name="durationSeconds">Duration of execution in seconds (if available)</param>
    public static void RecordFailure(string jobName, double? durationSeconds = null)
    {
        if (durationSeconds.HasValue)
        {
            ExecutionDuration.Record(durationSeconds.Value,
                new KeyValuePair<string, object?>("job_name", jobName),
                new KeyValuePair<string, object?>("status", "failure"));
        }

        FailureTotal.Add(1, new KeyValuePair<string, object?>("job_name", jobName));

        _lastExecutionTimestamps[jobName] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>
    /// Create a timing scope for measuring background job execution
    /// Usage: using (BackgroundJobMetrics.Time("JobName")) { ... }
    /// </summary>
    /// <param name="jobName">Name of the background job</param>
    /// <returns>Disposable timing scope</returns>
    public static IDisposable Time(string jobName)
    {
        return new TimingScope(jobName);
    }

    private class TimingScope : IDisposable
    {
        private readonly string _jobName;
        private readonly long _startTime;
        private bool _disposed;

        public TimingScope(string jobName)
        {
            _jobName = jobName;
            _startTime = Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            if (_disposed) return;

            var duration = Stopwatch.GetElapsedTime(_startTime).TotalSeconds;

            // Record success by default (call RecordFailure explicitly on exception)
            RecordSuccess(_jobName, duration);

            _disposed = true;
        }
    }
}
