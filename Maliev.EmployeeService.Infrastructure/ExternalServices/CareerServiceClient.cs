using System.Net.Http.Json;
using Maliev.EmployeeService.Application.DTOs.CareerService;
using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Infrastructure.ExternalServices;

/// <summary>
/// HTTP client implementation for Career Service communication with caching
/// </summary>
public class CareerServiceClient : ICareerServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CareerServiceClient> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);
    private const string SkillCacheKeyPrefix = "CareerService:Skill:";
    private const string LocationCacheKeyPrefix = "CareerService:Location:";

    public CareerServiceClient(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<CareerServiceClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<JobApplicationDto?> GetJobApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/careers/api/applications/{applicationId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get job application {ApplicationId} from Career Service. Status: {Status}",
                    applicationId,
                    response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<JobApplicationDto>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Career Service to get job application {ApplicationId}", applicationId);
            throw;
        }
    }

    public async Task<IEnumerable<JobApplicationDto>> GetJobApplicationsByPostingAsync(Guid jobPostingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/careers/api/postings/{jobPostingId}/applications", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get job applications for posting {JobPostingId} from Career Service. Status: {Status}",
                    jobPostingId,
                    response.StatusCode);
                return Enumerable.Empty<JobApplicationDto>();
            }

            return await response.Content.ReadFromJsonAsync<IEnumerable<JobApplicationDto>>(cancellationToken)
                ?? Enumerable.Empty<JobApplicationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Career Service to get job applications for posting {JobPostingId}", jobPostingId);
            throw;
        }
    }

    public async Task<bool> UpdateJobApplicationStatusAsync(Guid applicationId, string status, Guid? employeeId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                Status = status,
                EmployeeId = employeeId
            };

            var response = await _httpClient.PutAsJsonAsync(
                $"/careers/api/applications/{applicationId}/status",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to update job application {ApplicationId} status in Career Service. Status: {Status}",
                    applicationId,
                    response.StatusCode);
                return false;
            }

            _logger.LogInformation(
                "Updated job application {ApplicationId} status to {NewStatus}",
                applicationId,
                status);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Career Service to update job application {ApplicationId} status", applicationId);
            throw;
        }
    }

    public async Task<JobPostingDto?> GetJobPostingAsync(Guid jobPostingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/careers/api/postings/{jobPostingId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get job posting {JobPostingId} from Career Service. Status: {Status}",
                    jobPostingId,
                    response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<JobPostingDto>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Career Service to get job posting {JobPostingId}", jobPostingId);
            throw;
        }
    }

    public async Task NotifyEmployeeTerminatedAsync(Guid employeeId, Guid? jobPostingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                EmployeeId = employeeId,
                JobPostingId = jobPostingId
            };

            var response = await _httpClient.PostAsJsonAsync("/careers/api/notifications/employee-terminated", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to notify Career Service of employee {EmployeeId} termination. Status: {Status}",
                    employeeId,
                    response.StatusCode);
            }
            else
            {
                _logger.LogInformation(
                    "Notified Career Service of employee {EmployeeId} termination",
                    employeeId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying Career Service of employee {EmployeeId} termination", employeeId);
            // Don't throw - this is a notification, not critical
        }
    }

    public async Task<SkillDto?> GetSkillByIdAsync(int skillId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{SkillCacheKeyPrefix}{skillId}";

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out SkillDto? cachedSkill))
        {
            _logger.LogDebug("Skill {SkillId} retrieved from cache", skillId);
            return cachedSkill;
        }

        try
        {
            var response = await _httpClient.GetAsync($"/careers/api/skills/{skillId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get skill {SkillId} from Career Service. Status: {Status}",
                    skillId,
                    response.StatusCode);
                return null;
            }

            var skill = await response.Content.ReadFromJsonAsync<SkillDto>(cancellationToken);

            if (skill != null)
            {
                // Cache with sliding expiration
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(CacheDuration);

                _cache.Set(cacheKey, skill, cacheOptions);
                _logger.LogDebug("Skill {SkillId} cached for {Duration} hours", skillId, CacheDuration.TotalHours);
            }

            return skill;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Career Service to get skill {SkillId}", skillId);
            return null; // Graceful degradation - return null instead of throwing
        }
    }

    public async Task<WorkLocationDto?> GetWorkLocationByIdAsync(int locationId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{LocationCacheKeyPrefix}{locationId}";

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out WorkLocationDto? cachedLocation))
        {
            _logger.LogDebug("Work location {LocationId} retrieved from cache", locationId);
            return cachedLocation;
        }

        try
        {
            var response = await _httpClient.GetAsync($"/careers/api/locations/{locationId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get work location {LocationId} from Career Service. Status: {Status}",
                    locationId,
                    response.StatusCode);
                return null;
            }

            var location = await response.Content.ReadFromJsonAsync<WorkLocationDto>(cancellationToken);

            if (location != null)
            {
                // Cache with sliding expiration
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(CacheDuration);

                _cache.Set(cacheKey, location, cacheOptions);
                _logger.LogDebug("Work location {LocationId} cached for {Duration} hours", locationId, CacheDuration.TotalHours);
            }

            return location;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Career Service to get work location {LocationId}", locationId);
            return null; // Graceful degradation - return null instead of throwing
        }
    }
}
