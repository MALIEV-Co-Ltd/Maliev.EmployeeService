using System.Net;
using System.Text.Json;

namespace Maliev.EmployeeService.Tests.Helpers;

/// <summary>
/// A mock HTTP message handler for testing HTTP clients without external dependencies.
/// Allows setting up expected responses for specific request paths.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode StatusCode, string? Content)> _responses = new();
    private readonly List<HttpRequestMessage> _requests = new();

    /// <summary>
    /// Gets the number of requests that were made.
    /// </summary>
    public int RequestCount => _requests.Count;

    /// <summary>
    /// Gets the list of all requests that were made.
    /// </summary>
    public IReadOnlyList<HttpRequestMessage> Requests => _requests.AsReadOnly();

    /// <summary>
    /// Sets up a response for a specific path pattern.
    /// </summary>
    /// <param name="pathPattern">The path pattern to match (use * for wildcard).</param>
    /// <param name="statusCode">The HTTP status code to return.</param>
    /// <param name="content">The response content (JSON string).</param>
    public void SetupResponse(string pathPattern, HttpStatusCode statusCode, string? content = null)
    {
        _responses[pathPattern] = (statusCode, content);
    }

    /// <summary>
    /// Sets up a response for a specific path pattern with a typed object.
    /// </summary>
    /// <typeparam name="T">The type of the response object.</typeparam>
    /// <param name="pathPattern">The path pattern to match (use * for wildcard).</param>
    /// <param name="statusCode">The HTTP status code to return.</param>
    /// <param name="response">The response object to serialize as JSON.</param>
    public void SetupResponse<T>(string pathPattern, HttpStatusCode statusCode, T response)
    {
        var content = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        _responses[pathPattern] = (statusCode, content);
    }

    /// <summary>
    /// Clears all setup responses.
    /// </summary>
    public void Reset()
    {
        _responses.Clear();
        _requests.Clear();
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _requests.Add(request);

        var path = request.RequestUri?.PathAndQuery ?? string.Empty;

        // Find matching response (exact match first, then wildcard)
        var matchedResponse = FindMatchingResponse(path);

        var response = new HttpResponseMessage(matchedResponse.StatusCode);
        if (!string.IsNullOrEmpty(matchedResponse.Content))
        {
            response.Content = new StringContent(matchedResponse.Content, System.Text.Encoding.UTF8, "application/json");
        }

        return Task.FromResult(response);
    }

    private (HttpStatusCode StatusCode, string? Content) FindMatchingResponse(string path)
    {
        // Try exact match first
        if (_responses.TryGetValue(path, out var exactMatch))
        {
            return exactMatch;
        }

        // Try wildcard matches
        foreach (var (pattern, response) in _responses)
        {
            if (MatchesPattern(path, pattern))
            {
                return response;
            }
        }

        // Default to 404 if no match found
        return (HttpStatusCode.NotFound, null);
    }

    private static bool MatchesPattern(string path, string pattern)
    {
        if (!pattern.Contains('*'))
        {
            return path.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        // Simple wildcard matching: pattern ending with * matches any path starting with the prefix
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern[..^1];
            return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        // Pattern with * in the middle (e.g., /api/skills/*)
        var parts = pattern.Split('*');
        if (parts.Length == 2)
        {
            return path.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase) &&
                   (string.IsNullOrEmpty(parts[1]) || path.EndsWith(parts[1], StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }
}








