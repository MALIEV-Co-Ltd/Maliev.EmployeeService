using System.Net;
using FluentAssertions;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for Prometheus metrics endpoint
/// Phase 15 - T430, T431, T432
/// </summary>
public class MetricsEndpointIntegrationTests : WebApplicationTestBase
{
    public MetricsEndpointIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnPrometheusFormat()
    {
        // Act
        var response = await _client.GetAsync("/employees/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        // Verify Prometheus text format
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");

        // Verify content is not empty
        content.Should().NotBeNullOrWhiteSpace();

        // Verify basic Prometheus format structure
        content.Should().Contain("# HELP");
        content.Should().Contain("# TYPE");
    }

    [Fact]
    public async Task GetMetrics_ShouldContainBusinessMetrics()
    {
        // Act
        var response = await _client.GetAsync("/employees/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        // Verify all 7 business KPI metrics are present
        content.Should().Contain("employee_active_count", "Active employee count metric should be present");
        content.Should().Contain("employee_turnover_rate_monthly", "Turnover rate metric should be present");
        content.Should().Contain("department_headcount_by_name", "Department headcount metric should be present");
        content.Should().Contain("employee_probation_completion_rate", "Probation completion rate metric should be present");
        content.Should().Contain("leave_balance_utilization_rate", "Leave utilization rate metric should be present");
        content.Should().Contain("employee_onboarding_duration_days", "Onboarding duration metric should be present");
        content.Should().Contain("leave_request_approval_time_hours", "Leave approval time metric should be present");
    }

    [Fact]
    public async Task GetMetrics_ShouldContainTechnicalMetrics()
    {
        // Act
        var response = await _client.GetAsync("/employees/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        // Verify technical health metrics are present
        content.Should().Contain("db_query_duration_seconds", "Database query duration metric should be present");
        content.Should().Contain("db_queries_total", "Database query count metric should be present");
        content.Should().Contain("background_job_execution_duration_seconds", "Background job duration metric should be present");
        content.Should().Contain("background_job_success_total", "Background job success metric should be present");
        content.Should().Contain("background_job_failure_total", "Background job failure metric should be present");
        content.Should().Contain("background_job_last_execution_timestamp_seconds", "Background job timestamp metric should be present");
    }

    [Fact]
    public async Task GetMetrics_ShouldContainHttpMetrics()
    {
        // Arrange - Make a test request to generate HTTP metrics
        await _client.GetAsync("/employees/liveness");

        // Act
        var response = await _client.GetAsync("/employees/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        // Verify HTTP metrics from prometheus-net.AspNetCore are present
        content.Should().Contain("http_request", "HTTP request metrics should be present");
    }

    [Fact]
    public async Task GetMetrics_ShouldContainProcessMetrics()
    {
        // Act
        var response = await _client.GetAsync("/employees/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        // Verify process-level metrics are present
        content.Should().Contain("process_", "Process metrics should be present");
        content.Should().Contain("dotnet_", ".NET runtime metrics should be present");
    }

    [Fact]
    public async Task GetMetrics_ShouldHaveCorrectMetricLabels()
    {
        // Act
        var response = await _client.GetAsync("/employees/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        // Verify business metric labels
        content.Should().Contain("department=", "Department label should be present");
        content.Should().Contain("employment_type=", "Employment type label should be present");
        content.Should().Contain("leave_type=", "Leave type label should be present");

        // Verify technical metric labels
        content.Should().Contain("command_type=", "Command type label should be present");
        content.Should().Contain("operation=", "Operation label should be present");
        content.Should().Contain("status=", "Status label should be present");
        content.Should().Contain("job_name=", "Job name label should be present");
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
        var response = await _client.GetAsync("/employees/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        // Verify no PII is exposed in metrics
        content.Should().NotContain("John", "First name should not be exposed");
        content.Should().NotContain("Doe", "Last name should not be exposed");
        content.Should().NotContain("john.doe@example.com", "Email should not be exposed");
        content.Should().NotContain(employee.Id.ToString(), "Employee ID should not be exposed");
        content.Should().NotContain(employeeNumber, "Employee number should not be exposed");

        // Verify only aggregate/anonymized data
        content.Should().NotMatchRegex(@"employee_id=""[a-f0-9-]+""", "No employee IDs should be in labels");
        content.Should().NotMatchRegex(@"employee_name=""[A-Za-z]+""", "No employee names should be in labels");
        content.Should().NotMatchRegex(@"email=""[^""]+@[^""]+""", "No emails should be in labels");
    }

    [Fact]
    public async Task GetMetrics_ShouldBeAccessibleWithoutAuthentication()
    {
        // Arrange - Remove authentication header
        _client.DefaultRequestHeaders.Remove("Authorization");

        // Act
        var response = await _client.GetAsync("/employees/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "Metrics endpoint should be accessible without authentication for Prometheus scraping");
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnConsistentFormat()
    {
        // Act - Call metrics endpoint multiple times
        var response1 = await _client.GetAsync("/employees/metrics");
        var response2 = await _client.GetAsync("/employees/metrics");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        // Both responses should have the same metric names (values may differ)
        var metricNames1 = ExtractMetricNames(content1);
        var metricNames2 = ExtractMetricNames(content2);

        metricNames1.Should().BeEquivalentTo(metricNames2, "Metric names should be consistent across calls");
    }

    [Fact]
    public async Task GetMetrics_ShouldIncludeMetricHelp()
    {
        // Act
        var response = await _client.GetAsync("/employees/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        // Verify HELP lines exist for key metrics
        content.Should().Contain("# HELP employee_active_count");
        content.Should().Contain("# HELP employee_turnover_rate_monthly");
        content.Should().Contain("# HELP db_query_duration_seconds");
        content.Should().Contain("# HELP background_job_execution_duration_seconds");
    }

    [Fact]
    public async Task GetMetrics_ShouldIncludeMetricTypes()
    {
        // Act
        var response = await _client.GetAsync("/employees/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        // Verify TYPE lines exist and are correct
        content.Should().Contain("# TYPE employee_active_count gauge");
        content.Should().Contain("# TYPE employee_turnover_rate_monthly gauge");
        content.Should().Contain("# TYPE db_query_duration_seconds histogram");
        content.Should().Contain("# TYPE background_job_execution_duration_seconds histogram");
        content.Should().Contain("# TYPE background_job_success_total counter");
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
