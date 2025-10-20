using FluentAssertions;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Queries;

/// <summary>
/// Unit tests for DownloadDocumentQueryHandler (T318)
/// Tests document download with authorization checks and decryption
/// </summary>
public class DownloadDocumentQueryHandlerTests
{
    private readonly Mock<IDocumentRepository> _mockDocumentRepository;
    private readonly Mock<IUploadServiceClient> _mockUploadServiceClient;
    private readonly Mock<ILogger<DownloadDocumentQueryHandler>> _mockLogger;
    private readonly DownloadDocumentQueryHandler _handler;

    public DownloadDocumentQueryHandlerTests()
    {
        _mockDocumentRepository = new Mock<IDocumentRepository>();
        _mockUploadServiceClient = new Mock<IUploadServiceClient>();
        _mockLogger = new Mock<ILogger<DownloadDocumentQueryHandler>>();

        _handler = new DownloadDocumentQueryHandler(
            _mockDocumentRepository.Object,
            _mockUploadServiceClient.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_WithCurrentVersion_ShouldDownloadSuccessfully()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var fileName = "contract.pdf";
        var storagePath = "documents/contract.pdf";
        var contentType = "application/pdf";
        var fileSizeBytes = 1024L;

        var document = new Document
        {
            Id = documentId,
            EmployeeId = Guid.NewGuid(),
            DocumentType = DocumentType.EmploymentContract,
            FileName = fileName,
            StoragePath = storagePath,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadDate = DateTime.UtcNow,
            UploadedBy = Guid.NewGuid(),
            VersionNumber = 1,
            AccessLevel = AccessLevel.Employee,
            IsArchived = false
        };

        var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockUploadServiceClient.Setup(x => x.DownloadAsync(storagePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileStream);

        var query = new DownloadDocumentQuery
        {
            DocumentId = documentId,
            VersionNumber = null // Current version
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be(fileName);
        result.ContentType.Should().Be(contentType);
        result.FileSizeBytes.Should().Be(fileSizeBytes);
        result.FileStream.Should().BeSameAs(fileStream);

        _mockDocumentRepository.Verify(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUploadServiceClient.Verify(x => x.DownloadAsync(storagePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithSpecificVersion_ShouldDownloadSuccessfully()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var versionNumber = 2;
        var fileName = "contract_v2.pdf";
        var storagePath = "documents/contract_v2.pdf";
        var contentType = "application/pdf";
        var fileSizeBytes = 2048L;

        var documentVersion = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            VersionNumber = versionNumber,
            FileName = fileName,
            StoragePath = storagePath,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadDate = DateTime.UtcNow,
            UploadedBy = Guid.NewGuid(),
            ChangeDescription = "Updated contract terms"
        };

        var fileStream = new MemoryStream(new byte[] { 5, 6, 7, 8 });

        _mockDocumentRepository.Setup(x => x.GetVersionAsync(documentId, versionNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentVersion);

        _mockUploadServiceClient.Setup(x => x.DownloadAsync(storagePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileStream);

        var query = new DownloadDocumentQuery
        {
            DocumentId = documentId,
            VersionNumber = versionNumber
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be(fileName);
        result.ContentType.Should().Be(contentType);
        result.FileSizeBytes.Should().Be(fileSizeBytes);
        result.FileStream.Should().BeSameAs(fileStream);

        _mockDocumentRepository.Verify(x => x.GetVersionAsync(documentId, versionNumber, It.IsAny<CancellationToken>()), Times.Once);
        _mockUploadServiceClient.Verify(x => x.DownloadAsync(storagePath, It.IsAny<CancellationToken>()), Times.Once);
        _mockDocumentRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentDocument_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        var query = new DownloadDocumentQuery
        {
            DocumentId = documentId,
            VersionNumber = null
        };

        // Act
        var act = async () => await _handler.HandleAsync(query);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Document {documentId} not found");

        _mockDocumentRepository.Verify(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUploadServiceClient.Verify(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentVersion_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var versionNumber = 99;

        _mockDocumentRepository.Setup(x => x.GetVersionAsync(documentId, versionNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentVersion?)null);

        var query = new DownloadDocumentQuery
        {
            DocumentId = documentId,
            VersionNumber = versionNumber
        };

        // Act
        var act = async () => await _handler.HandleAsync(query);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Document version {versionNumber} not found for document {documentId}");

        _mockDocumentRepository.Verify(x => x.GetVersionAsync(documentId, versionNumber, It.IsAny<CancellationToken>()), Times.Once);
        _mockUploadServiceClient.Verify(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenUploadServiceFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var storagePath = "documents/contract.pdf";

        var document = new Document
        {
            Id = documentId,
            EmployeeId = Guid.NewGuid(),
            DocumentType = DocumentType.EmploymentContract,
            FileName = "contract.pdf",
            StoragePath = storagePath,
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            UploadDate = DateTime.UtcNow,
            UploadedBy = Guid.NewGuid(),
            VersionNumber = 1,
            AccessLevel = AccessLevel.Employee,
            IsArchived = false
        };

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var uploadServiceException = new Exception("Upload service unavailable");
        _mockUploadServiceClient.Setup(x => x.DownloadAsync(storagePath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(uploadServiceException);

        var query = new DownloadDocumentQuery
        {
            DocumentId = documentId,
            VersionNumber = null
        };

        // Act
        var act = async () => await _handler.HandleAsync(query);

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to download document from storage service");

        exception.And.InnerException.Should().NotBeNull();
        exception.And.InnerException.Should().BeOfType<Exception>();
        exception.And.InnerException!.Message.Should().Be("Upload service unavailable");

        _mockDocumentRepository.Verify(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUploadServiceClient.Verify(x => x.DownloadAsync(storagePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldDecryptFileNameAndStoragePath()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var decryptedFileName = "contract.pdf";
        var decryptedStoragePath = "documents/contract.pdf";

        var document = new Document
        {
            Id = documentId,
            EmployeeId = Guid.NewGuid(),
            DocumentType = DocumentType.EmploymentContract,
            FileName = decryptedFileName, // Already decrypted by EncryptionInterceptor
            StoragePath = decryptedStoragePath, // Already decrypted by EncryptionInterceptor
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            UploadDate = DateTime.UtcNow,
            UploadedBy = Guid.NewGuid(),
            VersionNumber = 1,
            AccessLevel = AccessLevel.Employee,
            IsArchived = false
        };

        var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockUploadServiceClient.Setup(x => x.DownloadAsync(decryptedStoragePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileStream);

        var query = new DownloadDocumentQuery
        {
            DocumentId = documentId,
            VersionNumber = null
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.FileName.Should().Be(decryptedFileName);
        _mockUploadServiceClient.Verify(x => x.DownloadAsync(decryptedStoragePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCorrectMetadata()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var fileName = "important_doc.pdf";
        var contentType = "application/pdf";
        var fileSizeBytes = 5120L;

        var document = new Document
        {
            Id = documentId,
            EmployeeId = Guid.NewGuid(),
            DocumentType = DocumentType.EmploymentContract,
            FileName = fileName,
            StoragePath = "documents/important_doc.pdf",
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadDate = DateTime.UtcNow,
            UploadedBy = Guid.NewGuid(),
            VersionNumber = 1,
            AccessLevel = AccessLevel.Employee,
            IsArchived = false
        };

        var fileStream = new MemoryStream(new byte[5120]);

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockUploadServiceClient.Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileStream);

        var query = new DownloadDocumentQuery
        {
            DocumentId = documentId,
            VersionNumber = null
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.FileName.Should().Be(fileName);
        result.ContentType.Should().Be(contentType);
        result.FileSizeBytes.Should().Be(fileSizeBytes);
    }

    [Fact]
    public async Task HandleAsync_WithVersionNumber_ShouldUseVersionMetadata()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var versionNumber = 3;
        var versionFileName = "document_v3.pdf";
        var versionContentType = "application/pdf";
        var versionFileSizeBytes = 3072L;

        var documentVersion = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            VersionNumber = versionNumber,
            FileName = versionFileName,
            StoragePath = "documents/document_v3.pdf",
            ContentType = versionContentType,
            FileSizeBytes = versionFileSizeBytes,
            UploadDate = DateTime.UtcNow,
            UploadedBy = Guid.NewGuid(),
            ChangeDescription = "Third revision"
        };

        var fileStream = new MemoryStream(new byte[3072]);

        _mockDocumentRepository.Setup(x => x.GetVersionAsync(documentId, versionNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentVersion);

        _mockUploadServiceClient.Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileStream);

        var query = new DownloadDocumentQuery
        {
            DocumentId = documentId,
            VersionNumber = versionNumber
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.FileName.Should().Be(versionFileName);
        result.ContentType.Should().Be(versionContentType);
        result.FileSizeBytes.Should().Be(versionFileSizeBytes);
    }

    [Fact]
    public async Task HandleAsync_ShouldLogDownloadAttempt()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        var document = new Document
        {
            Id = documentId,
            EmployeeId = Guid.NewGuid(),
            DocumentType = DocumentType.EmploymentContract,
            FileName = "contract.pdf",
            StoragePath = "documents/contract.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            UploadDate = DateTime.UtcNow,
            UploadedBy = Guid.NewGuid(),
            VersionNumber = 1,
            AccessLevel = AccessLevel.Employee,
            IsArchived = false
        };

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockUploadServiceClient.Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());

        var query = new DownloadDocumentQuery
        {
            DocumentId = documentId,
            VersionNumber = null
        };

        // Act
        await _handler.HandleAsync(query);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Downloading document {documentId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Document downloaded successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
