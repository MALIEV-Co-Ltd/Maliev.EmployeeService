using Prometheus;

namespace Maliev.EmployeeService.Infrastructure.BackgroundServices;

/// <summary>
/// Static helper class for tracking background job metrics
/// Phase 15 - T427 Technical Health Metrics
/// </summary>
public static class BackgroundJobMetrics
{
    private static readonly Histogram ExecutionDuration = Metrics.CreateHistogram(
        "background_job_execution_duration_seconds",
        "Background job execution duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "job_name", "status" },
            Buckets = new[] { 0.1, 0.5, 1.0, 5.0, 10.0, 30.0, 60.0, 120.0, 300.0 }
        });

    private static readonly Counter SuccessTotal = Metrics.CreateCounter(
        "background_job_success_total",
        "Total number of successful background job executions",
        new CounterConfiguration
        {
            LabelNames = new[] { "job_name" }
        });

    private static readonly Counter FailureTotal = Metrics.CreateCounter(
        "background_job_failure_total",
        "Total number of failed background job executions",
        new CounterConfiguration
        {
            LabelNames = new[] { "job_name" }
        });

    private static readonly Gauge LastExecutionTimestamp = Metrics.CreateGauge(
        "background_job_last_execution_timestamp_seconds",
        "Unix timestamp of the last execution of a background job",
        new GaugeConfiguration
        {
            LabelNames = new[] { "job_name" }
        });

    // Static constructor to initialize metrics with default values
    static BackgroundJobMetrics()
    {
        // Initialize with labeled variations to ensure metrics and labels appear in output
        ExecutionDuration.WithLabels("init", "success").Observe(0);
        SuccessTotal.WithLabels("init").Inc(0); // Inc(0) just creates the metric
        FailureTotal.WithLabels("init").Inc(0);
        LastExecutionTimestamp.WithLabels("init").Set(0);
    }

    /// <summary>
    /// Record a successful background job execution
    /// </summary>
    /// <param name="jobName">Name of the background job</param>
    /// <param name="durationSeconds">Duration of execution in seconds</param>
    public static void RecordSuccess(string jobName, double durationSeconds)
    {
        ExecutionDuration
            .WithLabels(jobName, "success")
            .Observe(durationSeconds);

        SuccessTotal
            .WithLabels(jobName)
            .Inc();

        LastExecutionTimestamp
            .WithLabels(jobName)
            .SetToCurrentTimeUtc();
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
            ExecutionDuration
                .WithLabels(jobName, "failure")
                .Observe(durationSeconds.Value);
        }

        FailureTotal
            .WithLabels(jobName)
            .Inc();

        LastExecutionTimestamp
            .WithLabels(jobName)
            .SetToCurrentTimeUtc();
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
        private readonly DateTime _startTime;
        private bool _disposed;

        public TimingScope(string jobName)
        {
            _jobName = jobName;
            _startTime = DateTime.UtcNow;
        }

        public void Dispose()
        {
            if (_disposed) return;

            var duration = (DateTime.UtcNow - _startTime).TotalSeconds;

            // Record success by default (call RecordFailure explicitly on exception)
            RecordSuccess(_jobName, duration);

            _disposed = true;
        }
    }
}
