using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Xunit;
using Maliev.Aspire.ServiceDefaults.IAM;
using Microsoft.Extensions.Configuration;
using Maliev.EmployeeService.Domain.Authorization;
using Moq;

namespace Maliev.EmployeeService.Tests.Unit.Queries;

/// <summary>
/// Unit tests for GetEmployeeDocumentsQueryHandler (T318)
/// Tests document retrieval, filtering, and expiration calculation
/// </summary>
public class GetEmployeeDocumentsQueryHandlerTests
{
    private readonly Mock<IDocumentRepository> _mockDocumentRepository;
    private readonly Mock<IIamServiceClient> _mockIamClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ILogger<GetEmployeeDocumentsQueryHandler>> _mockLogger;
    private readonly GetEmployeeDocumentsQueryHandler _handler;

    public GetEmployeeDocumentsQueryHandlerTests()
    {
        _mockDocumentRepository = new Mock<IDocumentRepository>();
        _mockIamClient = new Mock<IIamServiceClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockLogger = new Mock<ILogger<GetEmployeeDocumentsQueryHandler>>();

        _mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());
        _mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new GetEmployeeDocumentsQueryHandler(
            _mockDocumentRepository.Object,
            _mockIamClient.Object,
            _mockConfiguration.Object,
            _mockCurrentUserService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_WithNoDocumentTypeFilter_ShouldReturnAllDocuments()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();

        var documents = new List<Document>
        {
            new Document
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                DocumentType = DocumentType.IDDocument,
                FileName = "passport.pdf",
                UploadDate = DateTime.UtcNow.AddDays(-10),
                UploadedBy = uploaderId,
                VersionNumber = 1,
                FileSizeBytes = 1024,
                ContentType = "application/pdf",
                AccessLevel = AccessLevel.HROnly,
                IsArchived = false
            },
            new Document
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                DocumentType = DocumentType.EmploymentContract,
                FileName = "contract.pdf",
                UploadDate = DateTime.UtcNow.AddDays(-5),
                UploadedBy = uploaderId,
                VersionNumber = 1,
                FileSizeBytes = 2048,
                ContentType = "application/pdf",
                AccessLevel = AccessLevel.Employee,
                IsArchived = false
            }
        };

        var query = new GetEmployeeDocumentsQuery
        {
            EmployeeId = employeeId,
            DocumentType = null,
            IncludeArchived = false
        };

        _mockDocumentRepository.Setup(x => x.GetByEmployeeIdAsync(employeeId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, d => d.DocumentType == DocumentType.IDDocument);
        Assert.Contains(result, d => d.DocumentType == DocumentType.EmploymentContract);

        _mockDocumentRepository.Verify(x => x.GetByEmployeeIdAsync(
            employeeId,
            false,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDocumentRepository.Verify(x => x.GetByEmployeeIdAndTypeAsync(
            It.IsAny<Guid>(),
            It.IsAny<DocumentType>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithDocumentTypeFilter_ShouldReturnFilteredDocuments()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();

        var documents = new List<Document>
        {
            new Document
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                DocumentType = DocumentType.Certificate,
                FileName = "certification1.pdf",
                UploadDate = DateTime.UtcNow.AddDays(-20),
                UploadedBy = uploaderId,
                VersionNumber = 1,
                FileSizeBytes = 512,
                ContentType = "application/pdf",
                AccessLevel = AccessLevel.Public,
                IsArchived = false
            },
            new Document
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                DocumentType = DocumentType.Certificate,
                FileName = "certification2.pdf",
                UploadDate = DateTime.UtcNow.AddDays(-10),
                UploadedBy = uploaderId,
                VersionNumber = 1,
                FileSizeBytes = 768,
                ContentType = "application/pdf",
                AccessLevel = AccessLevel.Public,
                IsArchived = false
            }
        };

        var query = new GetEmployeeDocumentsQuery
        {
            EmployeeId = employeeId,
            DocumentType = DocumentType.Certificate,
            IncludeArchived = false
        };

        _mockDocumentRepository.Setup(x => x.GetByEmployeeIdAndTypeAsync(
                employeeId,
                DocumentType.Certificate,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, d  => Assert.True(d.DocumentType == DocumentType.Certificate));

        _mockDocumentRepository.Verify(x => x.GetByEmployeeIdAndTypeAsync(
            employeeId,
            DocumentType.Certificate,
            false,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDocumentRepository.Verify(x => x.GetByEmployeeIdAsync(
            It.IsAny<Guid>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithIncludeArchived_ShouldIncludeArchivedDocuments()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();

        var documents = new List<Document>
        {
            new Document
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                DocumentType = DocumentType.OfferLetter,
                FileName = "offer.pdf",
                UploadDate = DateTime.UtcNow.AddDays(-100),
                UploadedBy = uploaderId,
                VersionNumber = 1,
                FileSizeBytes = 1536,
                ContentType = "application/pdf",
                AccessLevel = AccessLevel.Manager,
                IsArchived = true // Archived document
            }
        };

        var query = new GetEmployeeDocumentsQuery
        {
            EmployeeId = employeeId,
            DocumentType = null,
            IncludeArchived = true
        };

        _mockDocumentRepository.Setup(x => x.GetByEmployeeIdAsync(employeeId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result.First().IsArchived);

        _mockDocumentRepository.Verify(x => x.GetByEmployeeIdAsync(
            employeeId,
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithExpiredDocument_ShouldCalculateIsExpiredCorrectly()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();

        var documents = new List<Document>
        {
            new Document
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                DocumentType = DocumentType.WorkPermit,
                FileName = "workpermit.pdf",
                UploadDate = DateTime.UtcNow.AddDays(-365),
                UploadedBy = uploaderId,
                VersionNumber = 1,
                FileSizeBytes = 2048,
                ContentType = "application/pdf",
                AccessLevel = AccessLevel.HROnly,
                IsArchived = false,
                ExpirationDate = DateTime.UtcNow.AddDays(-30) // Expired 30 days ago
            }
        };

        var query = new GetEmployeeDocumentsQuery
        {
            EmployeeId = employeeId,
            DocumentType = null,
            IncludeArchived = false
        };

        _mockDocumentRepository.Setup(x => x.GetByEmployeeIdAsync(employeeId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var doc = result.First();
        Assert.True(doc.IsExpired);
        Assert.Null(doc.DaysUntilExpiration); // Expired documents have null days until expiration
    }

    [Fact]
    public async Task HandleAsync_WithExpiringDocument_ShouldCalculateDaysUntilExpirationCorrectly()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();

        var expirationDate = DateTime.UtcNow.AddDays(45);

        var documents = new List<Document>
        {
            new Document
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                DocumentType = DocumentType.Certificate,
                FileName = "certification.pdf",
                UploadDate = DateTime.UtcNow.AddDays(-300),
                UploadedBy = uploaderId,
                VersionNumber = 1,
                FileSizeBytes = 1024,
                ContentType = "application/pdf",
                AccessLevel = AccessLevel.Public,
                IsArchived = false,
                ExpirationDate = expirationDate
            }
        };

        var query = new GetEmployeeDocumentsQuery
        {
            EmployeeId = employeeId,
            DocumentType = null,
            IncludeArchived = false
        };

        _mockDocumentRepository.Setup(x => x.GetByEmployeeIdAsync(employeeId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var doc = result.First();
        Assert.False(doc.IsExpired);
        Assert.True(doc.DaysUntilExpiration > 0);
        Assert.True(doc.DaysUntilExpiration <= 45);
        Assert.True(doc.DaysUntilExpiration >= 44); // Account for time passage during test
    }

    [Fact]
    public async Task HandleAsync_WithNoExpirationDate_ShouldHaveNullExpirationFields()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();

        var documents = new List<Document>
        {
            new Document
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                DocumentType = DocumentType.EmploymentContract,
                FileName = "contract.pdf",
                UploadDate = DateTime.UtcNow.AddDays(-10),
                UploadedBy = uploaderId,
                VersionNumber = 1,
                FileSizeBytes = 2048,
                ContentType = "application/pdf",
                AccessLevel = AccessLevel.Employee,
                IsArchived = false,
                ExpirationDate = null // No expiration
            }
        };

        var query = new GetEmployeeDocumentsQuery
        {
            EmployeeId = employeeId,
            DocumentType = null,
            IncludeArchived = false
        };

        _mockDocumentRepository.Setup(x => x.GetByEmployeeIdAsync(employeeId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var doc = result.First();
        Assert.Null(doc.ExpirationDate);
        Assert.False(doc.IsExpired); // No expiration date means not expired
        Assert.Null(doc.DaysUntilExpiration);
    }

    [Fact]
    public async Task HandleAsync_WithUploaderName_ShouldMapUploadedByName()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();

        var uploaderLegalName = new LegalName
        {
            FirstName = "John",
            LastName = "Doe"
        };

        var uploaderEmployee = new Employee
        {
            Id = uploaderId,
            EmployeeNumber = "HR001",
            LegalName = uploaderLegalName,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var documents = new List<Document>
        {
            new Document
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                DocumentType = DocumentType.IDDocument,
                FileName = "passport.pdf",
                UploadDate = DateTime.UtcNow.AddDays(-10),
                UploadedBy = uploaderId,
                UploadedByEmployee = uploaderEmployee, // Navigation property
                VersionNumber = 1,
                FileSizeBytes = 1024,
                ContentType = "application/pdf",
                AccessLevel = AccessLevel.HROnly,
                IsArchived = false
            }
        };

        var query = new GetEmployeeDocumentsQuery
        {
            EmployeeId = employeeId,
            DocumentType = null,
            IncludeArchived = false
        };

        _mockDocumentRepository.Setup(x => x.GetByEmployeeIdAsync(employeeId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("John Doe", result.First().UploadedByName);
    }

    [Fact]
    public async Task HandleAsync_WithNoUploaderInfo_ShouldReturnUnknownForUploadedByName()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();

        var documents = new List<Document>
        {
            new Document
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                DocumentType = DocumentType.Other,
                FileName = "document.pdf",
                UploadDate = DateTime.UtcNow.AddDays(-10),
                UploadedBy = uploaderId,
                UploadedByEmployee = null, // No navigation property loaded
                VersionNumber = 1,
                FileSizeBytes = 1024,
                ContentType = "application/pdf",
                AccessLevel = AccessLevel.Public,
                IsArchived = false
            }
        };

        var query = new GetEmployeeDocumentsQuery
        {
            EmployeeId = employeeId,
            DocumentType = null,
            IncludeArchived = false
        };

        _mockDocumentRepository.Setup(x => x.GetByEmployeeIdAsync(employeeId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Unknown", result.First().UploadedByName);
    }

    [Fact]
    public async Task HandleAsync_WithNoDocuments_ShouldReturnEmptyList()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var documents = new List<Document>();

        var query = new GetEmployeeDocumentsQuery
        {
            EmployeeId = employeeId,
            DocumentType = null,
            IncludeArchived = false
        };

        _mockDocumentRepository.Setup(x => x.GetByEmployeeIdAsync(employeeId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
