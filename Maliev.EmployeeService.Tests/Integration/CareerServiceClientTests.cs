using FluentAssertions;
using Maliev.EmployeeService.Application.DTOs.CareerService;
using Maliev.EmployeeService.Infrastructure.ExternalServices;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for CareerServiceClient using WireMock to simulate Career Service API
/// Tests T026n: Verify HTTP client behavior with mock API responses
/// </summary>
public class CareerServiceClientTests : IDisposable
{
    private readonly WireMockServer _wireMockServer;
    private readonly CareerServiceClient _client;
    private readonly IMemoryCache _memoryCache;

    public CareerServiceClientTests()
    {
        // Start WireMock server on dynamic port
        _wireMockServer = WireMockServer.Start();

        // Create HttpClient pointing to WireMock server
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_wireMockServer.Urls[0])
        };

        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _client = new CareerServiceClient(
            httpClient,
            _memoryCache,
            NullLogger<CareerServiceClient>.Instance);
    }

    [Fact]
    public async Task GetSkillByIdAsync_WithValidSkillId_ShouldReturnSkill()
    {
        // Arrange
        var skillId = 123;
        var expectedSkill = new SkillDto
        {
            SkillId = skillId,
            SkillName = "C# Programming",
            Category = "Programming Languages",
            Description = "Object-oriented programming language"
        };

        _wireMockServer
            .Given(Request.Create()
                .WithPath($"/careers/api/skills/{skillId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(expectedSkill));

        // Act
        var result = await _client.GetSkillByIdAsync(skillId);

        // Assert
        result.Should().NotBeNull();
        result!.SkillId.Should().Be(skillId);
        result.SkillName.Should().Be("C# Programming");
        result.Category.Should().Be("Programming Languages");
    }

    [Fact]
    public async Task GetSkillByIdAsync_WithNonExistentSkillId_ShouldReturnNull()
    {
        // Arrange
        var skillId = 999;

        _wireMockServer
            .Given(Request.Create()
                .WithPath($"/careers/api/skills/{skillId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404));

        // Act
        var result = await _client.GetSkillByIdAsync(skillId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSkillByIdAsync_ShouldCacheResults()
    {
        // Arrange
        var skillId = 456;
        var expectedSkill = new SkillDto
        {
            SkillId = skillId,
            SkillName = "TypeScript",
            Category = "Programming Languages",
            Description = "JavaScript superset with static typing"
        };

        _wireMockServer
            .Given(Request.Create()
                .WithPath($"/careers/api/skills/{skillId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(expectedSkill));

        // Act - First call should hit the API
        var result1 = await _client.GetSkillByIdAsync(skillId);

        // Act - Second call should hit the cache
        var result2 = await _client.GetSkillByIdAsync(skillId);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1!.SkillId.Should().Be(result2!.SkillId);
        result1.SkillName.Should().Be(result2.SkillName);

        // Verify API was only called once (cache hit on second call)
        _wireMockServer.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetWorkLocationByIdAsync_WithValidLocationId_ShouldReturnLocation()
    {
        // Arrange
        var locationId = 789;
        var expectedLocation = new WorkLocationDto
        {
            LocationId = locationId,
            LocationName = "Bangkok Office",
            City = "Bangkok",
            Country = "Thailand",
            IsActive = true
        };

        _wireMockServer
            .Given(Request.Create()
                .WithPath($"/careers/api/locations/{locationId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(expectedLocation));

        // Act
        var result = await _client.GetWorkLocationByIdAsync(locationId);

        // Assert
        result.Should().NotBeNull();
        result!.LocationId.Should().Be(locationId);
        result.LocationName.Should().Be("Bangkok Office");
        result.City.Should().Be("Bangkok");
        result.Country.Should().Be("Thailand");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetWorkLocationByIdAsync_WithNonExistentLocationId_ShouldReturnNull()
    {
        // Arrange
        var locationId = 888;

        _wireMockServer
            .Given(Request.Create()
                .WithPath($"/careers/api/locations/{locationId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404));

        // Act
        var result = await _client.GetWorkLocationByIdAsync(locationId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWorkLocationByIdAsync_ShouldCacheResults()
    {
        // Arrange
        var locationId = 101;
        var expectedLocation = new WorkLocationDto
        {
            LocationId = locationId,
            LocationName = "Chiang Mai Office",
            City = "Chiang Mai",
            Country = "Thailand",
            IsActive = true
        };

        _wireMockServer
            .Given(Request.Create()
                .WithPath($"/careers/api/locations/{locationId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(expectedLocation));

        // Act - First call should hit the API
        var result1 = await _client.GetWorkLocationByIdAsync(locationId);

        // Act - Second call should hit the cache
        var result2 = await _client.GetWorkLocationByIdAsync(locationId);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1!.LocationId.Should().Be(result2!.LocationId);
        result1.LocationName.Should().Be(result2.LocationName);

        // Verify API was only called once (cache hit on second call)
        _wireMockServer.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSkillByIdAsync_WithServerError_ShouldReturnNull()
    {
        // Arrange
        var skillId = 555;

        _wireMockServer
            .Given(Request.Create()
                .WithPath($"/careers/api/skills/{skillId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act
        var result = await _client.GetSkillByIdAsync(skillId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWorkLocationByIdAsync_WithServerError_ShouldReturnNull()
    {
        // Arrange
        var locationId = 777;

        _wireMockServer
            .Given(Request.Create()
                .WithPath($"/careers/api/locations/{locationId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act
        var result = await _client.GetWorkLocationByIdAsync(locationId);

        // Assert
        result.Should().BeNull();
    }

    public void Dispose()
    {
        _wireMockServer?.Stop();
        _wireMockServer?.Dispose();
        _memoryCache?.Dispose();
    }
}
