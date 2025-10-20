using System.Net;
using FluentAssertions;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for security headers middleware
/// Phase 16 - T384: Security Headers
/// </summary>
public class SecurityHeadersIntegrationTests : WebApplicationTestBase
{
    public SecurityHeadersIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetRequest_ShouldIncludeXContentTypeOptions()
    {
        // Act
        var response = await _client.GetAsync("/employees/liveness");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");
    }

    [Fact]
    public async Task GetRequest_ShouldIncludeXFrameOptions()
    {
        // Act
        var response = await _client.GetAsync("/employees/liveness");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");
    }

    [Fact]
    public async Task GetRequest_ShouldIncludeXXssProtection()
    {
        // Act
        var response = await _client.GetAsync("/employees/liveness");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-XSS-Protection");
        response.Headers.GetValues("X-XSS-Protection").First().Should().Be("1; mode=block");
    }

    [Fact]
    public async Task GetRequest_ShouldIncludeReferrerPolicy()
    {
        // Act
        var response = await _client.GetAsync("/employees/liveness");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("Referrer-Policy");
        response.Headers.GetValues("Referrer-Policy").First().Should().Be("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task GetRequest_ShouldIncludeContentSecurityPolicy()
    {
        // Act
        var response = await _client.GetAsync("/employees/liveness");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("Content-Security-Policy");

        var csp = response.Headers.GetValues("Content-Security-Policy").First();
        csp.Should().Contain("default-src 'self'");
        csp.Should().Contain("frame-ancestors 'none'");
        csp.Should().Contain("base-uri 'self'");
    }

    [Fact]
    public async Task GetRequest_ShouldIncludePermissionsPolicy()
    {
        // Act
        var response = await _client.GetAsync("/employees/liveness");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("Permissions-Policy");

        var policy = response.Headers.GetValues("Permissions-Policy").First();
        policy.Should().Contain("camera=()");
        policy.Should().Contain("microphone=()");
        policy.Should().Contain("geolocation=()");
    }

    [Fact]
    public async Task GetRequest_ShouldRemoveServerHeader()
    {
        // Act
        var response = await _client.GetAsync("/employees/liveness");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().NotContainKey("Server");
        response.Headers.Should().NotContainKey("X-Powered-By");
    }

    [Fact]
    public async Task AllEndpoints_ShouldHaveSecurityHeaders()
    {
        // Arrange - Test multiple endpoints (excluding readiness as it may fail if dependencies aren't available)
        var endpoints = new[]
        {
            "/employees/liveness",
            "/employees/metrics"
        };

        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify key security headers are present
            response.Headers.Should().ContainKey("X-Content-Type-Options");
            response.Headers.Should().ContainKey("X-Frame-Options");
            response.Headers.Should().ContainKey("Content-Security-Policy");
            response.Headers.Should().NotContainKey("Server");
        }
    }

    [Fact]
    public async Task SecurityHeaders_ShouldBeConsistentAcrossRequests()
    {
        // Act - Make multiple requests
        var response1 = await _client.GetAsync("/employees/liveness");
        var response2 = await _client.GetAsync("/employees/liveness");

        // Assert - Headers should be identical
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var headers1 = response1.Headers.GetValues("X-Content-Type-Options").First();
        var headers2 = response2.Headers.GetValues("X-Content-Type-Options").First();
        headers1.Should().Be(headers2);

        var csp1 = response1.Headers.GetValues("Content-Security-Policy").First();
        var csp2 = response2.Headers.GetValues("Content-Security-Policy").First();
        csp1.Should().Be(csp2);
    }
}
