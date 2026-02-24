using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Maliev.EmployeeService.Infrastructure.ExternalServices;

/// <summary>
/// HTTP client for Upload Service API (/uploads/v1)
/// Handles file upload, download, and deletion
/// </summary>
public class UploadServiceClient : IUploadServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UploadServiceClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public UploadServiceClient(
        HttpClient httpClient,
        ILogger<UploadServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc/>
    public async Task<UploadResult> UploadAsync(
        string fileName,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Uploading file {FileName} to Upload Service", fileName);

            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync("/uploads/v1", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<UploadServiceResponse>(responseBody, _jsonOptions);

            if (result == null)
            {
                throw new InvalidOperationException("Upload Service returned null response");
            }

            _logger.LogInformation("File {FileName} uploaded successfully to {StoragePath}",
                fileName, result.StoragePath);

            return new UploadResult
            {
                StoragePath = result.StoragePath,
                FileSizeBytes = result.FileSizeBytes,
                ContentType = result.ContentType
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName} to Upload Service", fileName);
            throw new InvalidOperationException($"Failed to upload document to Upload Service: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error uploading file {FileName}", fileName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Stream> DownloadAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Downloading file from Upload Service: {StoragePath}", storagePath);

            var encodedPath = Uri.EscapeDataString(storagePath);
            var response = await _httpClient.GetAsync($"/uploads/v1/{encodedPath}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new FileNotFoundException($"Document not found at storage path: {storagePath}");
            }

            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            _logger.LogInformation("File downloaded successfully from {StoragePath}", storagePath);

            return stream;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to download file from Upload Service: {StoragePath}", storagePath);
            throw new InvalidOperationException($"Failed to download document from Upload Service: {ex.Message}", ex);
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error downloading file: {StoragePath}", storagePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting file from Upload Service: {StoragePath}", storagePath);

            var encodedPath = Uri.EscapeDataString(storagePath);
            var response = await _httpClient.DeleteAsync($"/uploads/v1/{encodedPath}", cancellationToken);

            // Treat 404 as success - file is already deleted
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File not found at {StoragePath}, skipping deletion", storagePath);
                return;
            }

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("File deleted successfully from {StoragePath}", storagePath);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to delete file from Upload Service: {StoragePath}", storagePath);
            throw new InvalidOperationException($"Failed to delete document from Upload Service: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting file: {StoragePath}", storagePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedPath = Uri.EscapeDataString(storagePath);
            var request = new HttpRequestMessage(HttpMethod.Head, $"/uploads/v1/{encodedPath}");
            var response = await _httpClient.SendAsync(request, cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to check file existence: {StoragePath}", storagePath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking file existence: {StoragePath}", storagePath);
            throw;
        }
    }

    /// <summary>
    /// Response model from Upload Service
    /// </summary>
    private class UploadServiceResponse
    {
        public string StoragePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }
}
