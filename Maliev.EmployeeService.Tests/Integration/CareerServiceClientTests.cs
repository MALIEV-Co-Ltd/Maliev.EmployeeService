using Maliev.EmployeeService.Application.DTOs.CareerService;
using Maliev.EmployeeService.Infrastructure.ExternalServices;
using Maliev.EmployeeService.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for CareerServiceClient using MockHttpMessageHandler to simulate Career Service API
/// Tests T026n: Verify HTTP client behavior with mock API responses
/// </summary>
[Collection("IntegrationTests")]
public class CareerServiceClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly CareerServiceClient _client;
    private readonly IMemoryCache _memoryCache;

    public CareerServiceClientTests()
    {
        _mockHandler = new MockHttpMessageHandler();

        var httpClient = new HttpClient(_mockHandler)
        {
            BaseAddress = new Uri("http://localhost")
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

        _mockHandler.SetupResponse($"/careers/api/skills/{skillId}", HttpStatusCode.OK, expectedSkill);

        // Act
        var result = await _client.GetSkillByIdAsync(skillId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(skillId, result.SkillId);
        Assert.Equal("C# Programming", result.SkillName);
        Assert.Equal("Programming Languages", result.Category);
    }

    [Fact]
    public async Task GetSkillByIdAsync_WithNonExistentSkillId_ShouldReturnNull()
    {
        // Arrange
        var skillId = 999;

        _mockHandler.SetupResponse($"/careers/api/skills/{skillId}", HttpStatusCode.NotFound);

        // Act
        var result = await _client.GetSkillByIdAsync(skillId);

        // Assert
        Assert.Null(result);
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

        _mockHandler.SetupResponse($"/careers/api/skills/{skillId}", HttpStatusCode.OK, expectedSkill);

        // Act - First call should hit the API
        var result1 = await _client.GetSkillByIdAsync(skillId);

        // Act - Second call should hit the cache
        var result2 = await _client.GetSkillByIdAsync(skillId);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.SkillId, result2.SkillId);
        Assert.Equal(result1.SkillName, result2.SkillName);

        // Verify API was only called once (cache hit on second call)
        Assert.Equal(1, _mockHandler.RequestCount);
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

        _mockHandler.SetupResponse($"/careers/api/locations/{locationId}", HttpStatusCode.OK, expectedLocation);

        // Act
        var result = await _client.GetWorkLocationByIdAsync(locationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(locationId, result.LocationId);
        Assert.Equal("Bangkok Office", result.LocationName);
        Assert.Equal("Bangkok", result.City);
        Assert.Equal("Thailand", result.Country);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetWorkLocationByIdAsync_WithNonExistentLocationId_ShouldReturnNull()
    {
        // Arrange
        var locationId = 888;

        _mockHandler.SetupResponse($"/careers/api/locations/{locationId}", HttpStatusCode.NotFound);

        // Act
        var result = await _client.GetWorkLocationByIdAsync(locationId);

        // Assert
        Assert.Null(result);
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

        _mockHandler.SetupResponse($"/careers/api/locations/{locationId}", HttpStatusCode.OK, expectedLocation);

        // Act - First call should hit the API
        var result1 = await _client.GetWorkLocationByIdAsync(locationId);

        // Act - Second call should hit the cache
        var result2 = await _client.GetWorkLocationByIdAsync(locationId);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.LocationId, result2.LocationId);
        Assert.Equal(result1.LocationName, result2.LocationName);

        // Verify API was only called once (cache hit on second call)
        Assert.Equal(1, _mockHandler.RequestCount);
    }

    [Fact]
    public async Task GetSkillByIdAsync_WithServerError_ShouldReturnNull()
    {
        // Arrange
        var skillId = 555;

        _mockHandler.SetupResponse($"/careers/api/skills/{skillId}", HttpStatusCode.InternalServerError, "Internal Server Error");

        // Act
        var result = await _client.GetSkillByIdAsync(skillId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetWorkLocationByIdAsync_WithServerError_ShouldReturnNull()
    {
        // Arrange
        var locationId = 777;

        _mockHandler.SetupResponse($"/careers/api/locations/{locationId}", HttpStatusCode.InternalServerError, "Internal Server Error");

        // Act
        var result = await _client.GetWorkLocationByIdAsync(locationId);

        // Assert
        Assert.Null(result);
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
    }
}
