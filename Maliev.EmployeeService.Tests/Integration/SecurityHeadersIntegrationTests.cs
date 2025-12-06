using System.Net;
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());
    }

    [Fact]
    public async Task GetRequest_ShouldIncludeXFrameOptions()
    {
        // Act
        var response = await _client.GetAsync("/employees/liveness");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").First());
    }

    [Fact]
    public async Task GetRequest_ShouldIncludeXXssProtection()
    {
        // Act
        var response = await _client.GetAsync("/employees/liveness");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-XSS-Protection"));
        Assert.Equal("1; mode=block", response.Headers.GetValues("X-XSS-Protection").First());
    }

    [Fact]
    public async Task GetRequest_ShouldIncludeReferrerPolicy()
    {
        // Act
        var response = await _client.GetAsync("/employees/liveness");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").First());
    }

    [Fact]
    public async Task GetRequest_ShouldIncludeContentSecurityPolicy()
    {
        // Act
        var response = await _client.GetAsync("/employees/liveness");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Content-Security-Policy"));

        var csp = response.Headers.GetValues("Content-Security-Policy").First();
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.Contains("base-uri 'self'", csp);
    }

    [Fact]
    public async Task GetRequest_ShouldIncludePermissionsPolicy()
    {
        // Act
        var response = await _client.GetAsync("/employees/liveness");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Permissions-Policy"));

        var policy = response.Headers.GetValues("Permissions-Policy").First();
        Assert.Contains("camera=()", policy);
        Assert.Contains("microphone=()", policy);
        Assert.Contains("geolocation=()", policy);
    }

    [Fact]
    public async Task GetRequest_ShouldRemoveServerHeader()
    {
        // Act
        var response = await _client.GetAsync("/employees/liveness");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Server"));
        Assert.False(response.Headers.Contains("X-Powered-By"));
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
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify key security headers are present
            Assert.True(response.Headers.Contains("X-Content-Type-Options"));
            Assert.True(response.Headers.Contains("X-Frame-Options"));
            Assert.True(response.Headers.Contains("Content-Security-Policy"));
            Assert.False(response.Headers.Contains("Server"));
        }
    }

    [Fact]
    public async Task SecurityHeaders_ShouldBeConsistentAcrossRequests()
    {
        // Act - Make multiple requests
        var response1 = await _client.GetAsync("/employees/liveness");
        var response2 = await _client.GetAsync("/employees/liveness");

        // Assert - Headers should be identical
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var headers1 = response1.Headers.GetValues("X-Content-Type-Options").First();
        var headers2 = response2.Headers.GetValues("X-Content-Type-Options").First();
        Assert.Equal(headers2, headers1);

        var csp1 = response1.Headers.GetValues("Content-Security-Policy").First();
        var csp2 = response2.Headers.GetValues("Content-Security-Policy").First();
        Assert.Equal(csp2, csp1);
    }
}
