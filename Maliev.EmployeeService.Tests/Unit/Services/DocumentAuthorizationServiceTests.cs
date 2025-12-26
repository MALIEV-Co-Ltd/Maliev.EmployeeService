using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Services;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Services;

/// <summary>
/// Unit tests for DocumentAuthorizationService
/// Verifies that the service correctly delegates permission checks to IAM Client
/// </summary>
public class DocumentAuthorizationServiceTests
{
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<IIamServiceClient> _mockIamClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<DocumentAuthorizationService>> _mockLogger;
    private readonly DocumentAuthorizationService _service;

    public DocumentAuthorizationServiceTests()
    {
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockIamClient = new Mock<IIamServiceClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<DocumentAuthorizationService>>();

        _service = new DocumentAuthorizationService(
            _mockEmployeeRepository.Object,
            _mockIamClient.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    #region CanViewDocumentAsync Tests

    [Fact]
    public async Task CanViewDocumentAsync_WhenIAMGrantsAccess_ShouldReturnTrue()
    {
        // Arrange
        var principalId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            AccessLevel = AccessLevel.HROnly
        };

        _mockIamClient.Setup(x => x.CheckPermissionAsync(
            principalId.ToString(),
            EmployeePermissions.DocumentsRead,
            It.Is<string>(s => s.Contains(document.Id.ToString())),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CanViewDocumentAsync(principalId, document);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanViewDocumentAsync_WhenIAMDeniesAccessButDocumentIsPublic_ShouldReturnTrue()
    {
        // Arrange
        var principalId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            AccessLevel = AccessLevel.Public
        };

        _mockIamClient.Setup(x => x.CheckPermissionAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CanViewDocumentAsync(principalId, document);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanViewDocumentAsync_WhenIAMDeniesAccessAndDocumentIsNotPublic_ShouldReturnFalse()
    {
        // Arrange
        var principalId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            AccessLevel = AccessLevel.Employee
        };

        _mockIamClient.Setup(x => x.CheckPermissionAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CanViewDocumentAsync(principalId, document);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CanUploadDocumentAsync Tests

    [Fact]
    public async Task CanUploadDocumentAsync_DelegatesToIAM()
    {
        // Arrange
        var principalId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        _mockIamClient.Setup(x => x.CheckPermissionAsync(
            principalId.ToString(),
            EmployeePermissions.DocumentsCreate,
            It.Is<string>(s => s.Contains(employeeId.ToString())),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CanUploadDocumentAsync(principalId, employeeId);

        // Assert
        Assert.True(result);
        _mockIamClient.VerifyAll();
    }

    #endregion

    #region CanDeleteDocumentAsync Tests

    [Fact]
    public async Task CanDeleteDocumentAsync_DelegatesToIAM()
    {
        // Arrange
        var principalId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid()
        };

        _mockIamClient.Setup(x => x.CheckPermissionAsync(
            principalId.ToString(),
            EmployeePermissions.DocumentsDelete,
            It.Is<string>(s => s.Contains(document.Id.ToString())),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CanDeleteDocumentAsync(principalId, document);

        // Assert
        Assert.True(result);
        _mockIamClient.VerifyAll();
    }

    #endregion

    #region Validate Methods Tests

    [Fact]
    public async Task ValidateCanViewDocumentAsync_ThrowsWhenDenied()
    {
        // Arrange
        var principalId = Guid.NewGuid();
        var document = new Document { Id = Guid.NewGuid(), AccessLevel = AccessLevel.Employee };
        _mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.ValidateCanViewDocumentAsync(principalId, document));
    }

    [Fact]
    public async Task ValidateCanUploadDocumentAsync_ThrowsWhenDenied()
    {
        // Arrange
        var principalId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        _mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.ValidateCanUploadDocumentAsync(principalId, employeeId));
    }

    [Fact]
    public async Task ValidateCanDeleteDocumentAsync_ThrowsWhenDenied()
    {
        // Arrange
        var principalId = Guid.NewGuid();
        var document = new Document { Id = Guid.NewGuid() };
        _mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.ValidateCanDeleteDocumentAsync(principalId, document));
    }

    #endregion
}
