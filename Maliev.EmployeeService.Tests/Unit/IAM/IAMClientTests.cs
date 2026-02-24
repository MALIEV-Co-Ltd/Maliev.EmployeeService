using System.Net;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Infrastructure.IAM;
using Maliev.EmployeeService.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.IAM;

public class IAMClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly IAMClient _client;

    public IAMClientTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(_mockHandler)
        {
            BaseAddress = new Uri("http://iam-service")
        };
        _client = new IAMClient(httpClient, NullLogger<IAMClient>.Instance);
    }

    public void Dispose()
    {
        _mockHandler.Reset();
    }

    [Fact]
    public async Task CreatePrincipalAsync_ShouldReturnResponse_WhenSuccessful()
    {
        // Arrange
        var request = new CreatePrincipalRequest
        {
            Email = "test@example.com",
            LinkedService = "EmployeeService",
            LinkedEntityId = Guid.NewGuid()
        };
        var expectedResponse = new CreatePrincipalResponse
        {
            PrincipalId = Guid.NewGuid()
        };

        _mockHandler.SetupResponse("/iam/v1/service-accounts/users", HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _client.CreatePrincipalAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.PrincipalId, result.PrincipalId);
    }

    [Fact]
    public async Task CreatePrincipalAsync_ShouldThrowException_WhenApiReturnsError()
    {
        // Arrange
        var request = new CreatePrincipalRequest { Email = "test@example.com" };
        _mockHandler.SetupResponse("/iam/v1/service-accounts/users", HttpStatusCode.InternalServerError);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _client.CreatePrincipalAsync(request));
    }
}
