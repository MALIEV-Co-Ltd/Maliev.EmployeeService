using System.Net;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for Prometheus metrics endpoint
/// Phase 15 - T430, T431, T432
/// Note: Some tests are skipped because they require infrastructure (PostgreSQL, background jobs)
/// that isn't available in the in-memory test environment.
/// </summary>
public class MetricsEndpointIntegrationTests : WebApplicationTestBase
{
    public MetricsEndpointIntegrationTests(EmployeeServiceTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnPrometheusFormat()
    {
        // Act
        var response = await _client.GetAsync("/employee/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Verify Prometheus text format
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);

        // Verify content is not empty
        Assert.False(string.IsNullOrWhiteSpace(content));

        // Verify basic Prometheus format structure
        Assert.Contains("# HELP", content);
        Assert.Contains("# TYPE", content);
    }

    [Fact]
    public async Task GetMetrics_ShouldContainBusinessMetrics()
    {
        // Act
        var response = await _client.GetAsync("/employee/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Verify business KPI gauge metrics are present (from BusinessMetricsService)
        // These are observable gauges that are always registered
        Assert.Contains("employee_active_count", content);
        Assert.Contains("employee_turnover_rate_monthly", content);
        Assert.Contains("department_headcount_by_name", content);
        Assert.Contains("employee_probation_completion_rate", content);
        Assert.Contains("leave_balance_utilization_rate", content);
        
        // Note: Histogram metrics (employee_onboarding_duration_days, leave_request_approval_time_hours)
        // only appear in OpenTelemetry output after data is recorded, so we don't assert them here
    }

    [Fact(Skip = "Technical metrics require real PostgreSQL and background jobs which aren't available in in-memory test environment")]
    public async Task GetMetrics_ShouldContainTechnicalMetrics()
    {
        // Act
        var response = await _client.GetAsync("/employee/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // These metrics require actual database queries and background job executions
        Assert.Contains("db_query_duration_seconds", content);
        Assert.Contains("db_queries_total", content);
        Assert.Contains("background_job_execution_duration_seconds", content);
        Assert.Contains("background_job_success_total", content);
        Assert.Contains("background_job_failure_total", content);
        Assert.Contains("background_job_last_execution_timestamp_seconds", content);
    }

    [Fact]
    public async Task GetMetrics_ShouldContainHttpMetrics()
    {
        // Arrange - Make a test request to generate HTTP metrics
        await _client.GetAsync("/employee/liveness");

        // Act
        var response = await _client.GetAsync("/employee/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Verify HTTP metrics from OpenTelemetry ASP.NET Core instrumentation are present
        Assert.Contains("http", content); // HTTP metrics should be present in some form
    }

    [Fact]
    public async Task GetMetrics_ShouldContainProcessMetrics()
    {
        // Act
        var response = await _client.GetAsync("/employee/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Verify process-level metrics are present (from OpenTelemetry runtime instrumentation)
        Assert.Contains("process_", content); // Process metrics should be present
    }

    [Fact(Skip = "Label format assertions require actual infrastructure metrics which aren't available in in-memory test environment")]
    public async Task GetMetrics_ShouldHaveCorrectMetricLabels()
    {
        // Act
        var response = await _client.GetAsync("/employee/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Verify business metric labels
        Assert.Contains("department=", content);
        Assert.Contains("employment_type=", content);
        Assert.Contains("leave_type=", content);

        // Verify technical metric labels
        Assert.Contains("command_type=", content);
        Assert.Contains("operation=", content);
        Assert.Contains("status=", content);
        Assert.Contains("job_name=", content);
    }

    [Fact]
    public async Task GetMetrics_ShouldNotExposePII()
    {
        // Arrange - Create test employee with PII data
        var department = await CreateTestDepartmentAsync("Engineering");
        var employeeNumber = "E001";
        var employee = await CreateTestEmployeeAsync(
            department.Id,
            employeeNumber: employeeNumber,
            firstName: "John",
            lastName: "Doe",
            email: "john.doe@example.com"
        );

        // Wait a moment for metrics to update
        await Task.Delay(500);

        // Act
        var response = await _client.GetAsync("/employee/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Verify no PII is exposed in metrics
        Assert.DoesNotContain("John", content); // First name should not be exposed
        Assert.DoesNotContain("Doe", content); // Last name should not be exposed
        Assert.DoesNotContain("john.doe@example.com", content); // Email should not be exposed
        Assert.DoesNotContain(employee.Id.ToString(), content); // Employee ID should not be exposed
        Assert.DoesNotContain(employeeNumber, content); // Employee number should not be exposed

        // Verify only aggregate/anonymized data (no employee IDs, names, or emails in labels)
        Assert.DoesNotMatch(@"employee_id=""[a-f0-9-]+""", content);
        Assert.DoesNotMatch(@"employee_name=""[A-Za-z]+""", content);
        Assert.DoesNotMatch(@"email=""[^""]+@[^""]+""", content);
    }

    [Fact]
    public async Task GetMetrics_ShouldBeAccessibleWithoutAuthentication()
    {
        // Arrange - Remove authentication header
        _client.DefaultRequestHeaders.Remove("Authorization");

        // Act
        var response = await _client.GetAsync("/employee/metrics");

        // Assert - Metrics endpoint should be accessible without authentication for Prometheus scraping
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnConsistentFormat()
    {
        // Act - Call metrics endpoint multiple times
        var response1 = await _client.GetAsync("/employee/metrics");
        var response2 = await _client.GetAsync("/employee/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        // Both responses should have the same metric names (values may differ)
        var metricNames1 = ExtractMetricNames(content1);
        var metricNames2 = ExtractMetricNames(content2);

        // Metric names should be consistent across calls
        Assert.Equal(metricNames1, metricNames2);
    }

    [Fact]
    public async Task GetMetrics_ShouldIncludeMetricHelp()
    {
        // Act
        var response = await _client.GetAsync("/employee/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Verify HELP lines exist for key business metrics (available in test environment)
        Assert.Contains("# HELP employee_active_count", content);
        Assert.Contains("# HELP employee_turnover_rate_monthly", content);
    }

    [Fact]
    public async Task GetMetrics_ShouldIncludeMetricTypes()
    {
        // Act
        var response = await _client.GetAsync("/employee/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Verify TYPE lines exist for business metrics (available in test environment)
        Assert.Contains("# TYPE employee_active_count gauge", content);
        Assert.Contains("# TYPE employee_turnover_rate_monthly gauge", content);
    }

    /// <summary>
    /// Extract metric names from Prometheus format output
    /// </summary>
    private static HashSet<string> ExtractMetricNames(string prometheusOutput)
    {
        var metricNames = new HashSet<string>();

        foreach (var line in prometheusOutput.Split('\n'))
        {
            if (line.StartsWith("# HELP "))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    metricNames.Add(parts[2]);
                }
            }
        }

        return metricNames;
    }
}
