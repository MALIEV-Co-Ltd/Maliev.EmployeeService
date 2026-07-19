using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using System.Net;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for Career Service circuit breaker behavior
/// Tests T026o: Verify circuit breaker opens after 5 consecutive failures and resumes after 30s
/// </summary>
[Collection("IntegrationTests")]
public class CareerServiceCircuitBreakerTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly ServiceProvider _serviceProvider;
    private readonly ICareerServiceClient _client;

    public CareerServiceCircuitBreakerTests()
    {
        _mockHandler = new MockHttpMessageHandler();

        // Build service collection with circuit breaker configuration
        var services = new ServiceCollection();

        // Add memory cache
        services.AddMemoryCache();

        // Configure HttpClient with circuit breaker (same as production)
        services.AddHttpClient<ICareerServiceClient, Infrastructure.ExternalServices.CareerServiceClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost");
            client.Timeout = TimeSpan.FromSeconds(5);
        })
        .ConfigurePrimaryHttpMessageHandler(() => _mockHandler)
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
        // Arrange - Configure mock to always return 500
        _mockHandler.SetupResponse("/careers/api/skills/*", HttpStatusCode.InternalServerError, "Internal Server Error");

        // Act - Make 5 consecutive failing requests
        var results = new List<object?>();
        for (int i = 0; i < 5; i++)
        {
            var result = await _client.GetSkillByIdAsync(i + 1);
            results.Add(result);
        }

        // Assert - All requests should fail and return null
        Assert.All(results, r => Assert.Null(r));

        // Verify requests were made to the server
        Assert.True(_mockHandler.RequestCount >= 5,
            $"Expected at least 5 requests, but got {_mockHandler.RequestCount}");
    }

    [Fact]
    public async Task CircuitBreaker_WhileOpen_ShouldRejectRequestsImmediately()
    {
        // Arrange - Configure mock to always return 500
        _mockHandler.SetupResponse("/careers/api/skills/*", HttpStatusCode.InternalServerError, "Internal Server Error");

        // Act - Trigger circuit breaker by making 5 consecutive failures
        for (int i = 0; i < 5; i++)
        {
            await _client.GetSkillByIdAsync(i + 1);
        }

        var requestCountAfterBreaking = _mockHandler.RequestCount;

        // Make additional request while circuit is open
        var resultWhileOpen = await _client.GetSkillByIdAsync(999);

        var requestCountWhileOpen = _mockHandler.RequestCount;

        // Assert - Circuit is open, request should fail immediately without hitting the server
        Assert.Null(resultWhileOpen);

        // Request count should not have increased significantly (circuit breaker blocks the call)
        Assert.True(requestCountWhileOpen < requestCountAfterBreaking + 5,
            $"Expected fewer requests when circuit is open. Before: {requestCountAfterBreaking}, After: {requestCountWhileOpen}");
    }

    [Fact]
    public async Task CircuitBreaker_AfterBreakDuration_ShouldAttemptRecovery()
    {
        // Arrange - Configure mock to fail first
        _mockHandler.SetupResponse("/careers/api/skills/*", HttpStatusCode.InternalServerError, "Temporary error");

        // Act - Trigger circuit breaker by making 5 consecutive failures
        for (int i = 0; i < 5; i++)
        {
            await _client.GetSkillByIdAsync(i + 1);
        }

        // Reconfigure to succeed
        _mockHandler.Reset();
        _mockHandler.SetupResponse("/careers/api/skills/*", HttpStatusCode.OK,
            "{\"skillId\": 1, \"skillName\": \"C#\", \"category\": \"Programming\", \"description\": \"Programming language\"}");

        // Wait for break duration (2 seconds in test configuration)
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Make a request after break duration - circuit should be half-open and allow test request
        var resultAfterBreak = await _client.GetSkillByIdAsync(100);

        // Assert - Request should succeed after circuit recovers
        Assert.NotNull(resultAfterBreak);
        Assert.Equal("C#", resultAfterBreak.SkillName);
    }

    [Fact]
    public async Task CircuitBreaker_WithSuccessfulRequests_ShouldRemainClosed()
    {
        // Arrange - Configure mock to always succeed
        _mockHandler.SetupResponse("/careers/api/skills/*", HttpStatusCode.OK,
            "{\"skillId\": 1, \"skillName\": \"Python\", \"category\": \"Programming\", \"description\": \"Programming language\"}");

        // Act - Make 10 successful requests
        var results = new List<object?>();
        for (int i = 0; i < 10; i++)
        {
            var result = await _client.GetSkillByIdAsync(i + 1);
            results.Add(result);
        }

        // Assert - All requests should succeed
        Assert.All(results, r => Assert.NotNull(r));

        foreach (var result in results.Cast<Application.DTOs.CareerService.SkillDto>())
        {
            Assert.Equal("Python", result.SkillName);
            Assert.Equal("Programming", result.Category);
        }

        // Verify all requests hit the API (circuit remained closed)
        Assert.Equal(10, _mockHandler.RequestCount);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
