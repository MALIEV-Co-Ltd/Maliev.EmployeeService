using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Maliev.EmployeeService.Infrastructure.IAM;

public class IAMClient : IIAMClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IAMClient> _logger;

    public IAMClient(HttpClient httpClient, ILogger<IAMClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CreatePrincipalResponse> CreatePrincipalAsync(
        CreatePrincipalRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/iam/v1/principals", request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreatePrincipalResponse>(cancellationToken: cancellationToken);

        return result ?? throw new InvalidOperationException("Failed to deserialize IAM response");
    }

    public async Task DeletePrincipalAsync(
        Guid principalId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Deleting IAM principal {PrincipalId} as compensation for failed transaction", principalId);

        try
        {
            var response = await _httpClient.DeleteAsync($"/iam/v1/principals/{principalId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deleted IAM principal {PrincipalId} as compensation", principalId);
            }
            else
            {
                _logger.LogError("Failed to delete IAM principal {PrincipalId} during compensation. Status: {StatusCode}. Manual cleanup may be required.",
                    principalId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting IAM principal {PrincipalId} during compensation. Manual cleanup may be required.", principalId);
            // Don't re-throw - compensation is best-effort
        }
    }
}
