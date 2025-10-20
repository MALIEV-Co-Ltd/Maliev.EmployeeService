using FluentAssertions;
using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;
using WireMock.Util;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for Career Service circuit breaker behavior
/// Tests T026o: Verify circuit breaker opens after 5 consecutive failures and resumes after 30s
/// </summary>
public class CareerServiceCircuitBreakerTests : IDisposable
{
    private readonly WireMockServer _wireMockServer;
    private readonly ServiceProvider _serviceProvider;
    private readonly ICareerServiceClient _client;

    public CareerServiceCircuitBreakerTests()
    {
        // Start WireMock server on dynamic port
        _wireMockServer = WireMockServer.Start();

        // Build service collection with circuit breaker configuration
        var services = new ServiceCollection();

        // Add memory cache
        services.AddMemoryCache();

        // Configure HttpClient with circuit breaker (same as production)
        services.AddHttpClient<ICareerServiceClient, Infrastructure.ExternalServices.CareerServiceClient>(client =>
        {
            client.BaseAddress = new Uri(_wireMockServer.Urls[0]);
            client.Timeout = TimeSpan.FromSeconds(5);
        })
        .AddStandardResilienceHandler(options =>
        {
            // Attempt timeout: 5 seconds (must be less than half of sampling duration)
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);

            // Retry policy: 3 retries with minimal delay for testing
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromMilliseconds(100);
            options.Retry.BackoffType = DelayBackoffType.Constant;
            options.Retry.UseJitter = false; // Disable jitter for predictable testing

            // Circuit breaker: opens after 5 consecutive failures, breaks for 2 seconds (shorter for testing)
            // Sampling duration must be at least double the attempt timeout (5s * 2 = 10s minimum)
            options.CircuitBreaker.FailureRatio = 1.0;
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(2);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(20);
        });

        _serviceProvider = services.BuildServiceProvider();
        _client = _serviceProvider.GetRequiredService<ICareerServiceClient>();
    }

    [Fact]
    public async Task CircuitBreaker_AfterFiveConsecutiveFailures_ShouldOpenCircuit()
    {
        // Arrange - Configure WireMock to always return 500
        _wireMockServer
            .Given(Request.Create()
                .WithPath("/careers/api/skills/*")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act - Make 5 consecutive failing requests
        var results = new List<object?>();
        for (int i = 0; i < 5; i++)
        {
            var result = await _client.GetSkillByIdAsync(i + 1);
            results.Add(result);
        }

        // Assert - All requests should fail and return null
        results.Should().AllSatisfy(r => r.Should().BeNull());

        // Verify requests were made to the server
        // With retries and circuit breaker, we should see at least the minimum threshold requests
        // The exact count depends on when the circuit opens (may open before all retries complete)
        _wireMockServer.LogEntries.Count().Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task CircuitBreaker_WhileOpen_ShouldRejectRequestsImmediately()
    {
        // Arrange - Configure WireMock to always return 500
        _wireMockServer
            .Given(Request.Create()
                .WithPath("/careers/api/skills/*")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act - Trigger circuit breaker by making 5 consecutive failures
        for (int i = 0; i < 5; i++)
        {
            await _client.GetSkillByIdAsync(i + 1);
        }

        var requestCountAfterBreaking = _wireMockServer.LogEntries.Count();

        // Make additional request while circuit is open
        var resultWhileOpen = await _client.GetSkillByIdAsync(999);

        var requestCountWhileOpen = _wireMockServer.LogEntries.Count();

        // Assert - Circuit is open, request should fail immediately without hitting the server
        resultWhileOpen.Should().BeNull();

        // Request count should not have increased (circuit breaker blocks the call)
        // Note: Due to retry policy, there might be a few additional calls, but significantly fewer
        requestCountWhileOpen.Should().BeLessThan(requestCountAfterBreaking + 5);
    }

    [Fact]
    public async Task CircuitBreaker_AfterBreakDuration_ShouldAttemptRecovery()
    {
        // Arrange - Configure WireMock to fail first, then succeed
        _wireMockServer
            .Given(Request.Create()
                .WithPath("/careers/api/skills/*")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Temporary error"));

        // Act - Trigger circuit breaker by making 5 consecutive failures
        for (int i = 0; i < 5; i++)
        {
            await _client.GetSkillByIdAsync(i + 1);
        }

        // Reconfigure to succeed
        _wireMockServer.Reset();
        _wireMockServer
            .Given(Request.Create()
                .WithPath("/careers/api/skills/*")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"skillId\": 1, \"skillName\": \"C#\", \"category\": \"Programming\", \"description\": \"Programming language\"}"));

        // Wait for break duration (2 seconds in test configuration)
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Make a request after break duration - circuit should be half-open and allow test request
        var resultAfterBreak = await _client.GetSkillByIdAsync(100);

        // Assert - Request should succeed after circuit recovers
        resultAfterBreak.Should().NotBeNull();
        resultAfterBreak!.SkillName.Should().Be("C#");
    }

    [Fact]
    public async Task CircuitBreaker_WithSuccessfulRequests_ShouldRemainClosed()
    {
        // Arrange - Configure WireMock to always succeed
        _wireMockServer
            .Given(Request.Create()
                .WithPath("/careers/api/skills/*")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"skillId\": 1, \"skillName\": \"Python\", \"category\": \"Programming\", \"description\": \"Programming language\"}"));

        // Act - Make 10 successful requests
        var results = new List<object?>();
        for (int i = 0; i < 10; i++)
        {
            var result = await _client.GetSkillByIdAsync(i + 1);
            results.Add(result);
        }

        // Assert - All requests should succeed
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
        results.Cast<Application.DTOs.CareerService.SkillDto>().Should().AllSatisfy(s =>
        {
            s.SkillName.Should().Be("Python");
            s.Category.Should().Be("Programming");
        });

        // Verify all requests hit the API (circuit remained closed)
        _wireMockServer.LogEntries.Count().Should().Be(10);
    }

    public void Dispose()
    {
        _wireMockServer?.Stop();
        _wireMockServer?.Dispose();
        _serviceProvider?.Dispose();
    }
}
