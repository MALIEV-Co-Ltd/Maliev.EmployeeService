using FluentAssertions;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Commands;

/// <summary>
/// Unit tests for UploadDocumentCommandHandler focusing on encryption verification (T317)
/// Tests that sensitive fields (FileName, StoragePath) are correctly handled for encryption
/// </summary>
public class UploadDocumentCommandHandlerWithEncryptionTests
{
    private readonly Mock<IDocumentRepository> _mockDocumentRepository;
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<IUploadServiceClient> _mockUploadServiceClient;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<UploadDocumentCommandHandler>> _mockLogger;
    private readonly UploadDocumentCommandHandler _handler;

    public UploadDocumentCommandHandlerWithEncryptionTests()
    {
        _mockDocumentRepository = new Mock<IDocumentRepository>();
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockUploadServiceClient = new Mock<IUploadServiceClient>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();

        // Setup default employee mock
        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) => new Employee
            {
                Id = id,
                EmployeeNumber = $"EMP_{id.ToString().Substring(0, 8)}",
                EmploymentStatus = EmploymentStatus.Active,
                StartDate = DateTime.UtcNow.AddYears(-1),
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            });

        _handler = new UploadDocumentCommandHandler(
            _mockDocumentRepository.Object,
            _mockEmployeeRepository.Object,
            _mockUploadServiceClient.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldStoreOriginalFileNameForEncryption()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var fileName = "sensitive_document.pdf";

        var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var command = new UploadDocumentCommand
        {
            EmployeeId = employeeId,
            DocumentType = DocumentType.IDDocument,
            AccessLevel = AccessLevel.HROnly,
            FileName = fileName,
            FileStream = fileStream,
            ContentType = "application/pdf",
            UploadedBy = Guid.NewGuid()
        };

        var uploadResult = new UploadResult
        {
            StoragePath = "documents/2025/01/sensitive_document_abc123.pdf",
            FileSizeBytes = 5,
            ContentType = "application/pdf"
        };

        _mockUploadServiceClient.Setup(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        _mockDocumentRepository.Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.HandleAsync(command);

        // Assert - Verify that the original filename is stored (it will be encrypted by interceptor)
        _mockDocumentRepository.Verify(x => x.AddAsync(
            It.Is<Document>(d =>
                d.FileName == fileName), // Original filename should be stored as-is
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldStoreOriginalStoragePathForEncryption()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var storagePath = "documents/2025/01/classified_report_xyz789.pdf";

        var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var command = new UploadDocumentCommand
        {
            EmployeeId = employeeId,
            DocumentType = DocumentType.PerformanceReview,
            AccessLevel = AccessLevel.HRSpecialistOnly,
            FileName = "classified_report.pdf",
            FileStream = fileStream,
            ContentType = "application/pdf",
            UploadedBy = Guid.NewGuid()
        };

        var uploadResult = new UploadResult
        {
            StoragePath = storagePath,
            FileSizeBytes = 5,
            ContentType = "application/pdf"
        };

        _mockUploadServiceClient.Setup(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        _mockDocumentRepository.Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.HandleAsync(command);

        // Assert - Verify that the original storage path is stored (it will be encrypted by interceptor)
        _mockDocumentRepository.Verify(x => x.AddAsync(
            It.Is<Document>(d =>
                d.StoragePath == storagePath), // Original storage path should be stored as-is
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnOriginalUnencryptedFileNameInResult()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var originalFileName = "employee_contract.pdf";

        var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var command = new UploadDocumentCommand
        {
            EmployeeId = employeeId,
            DocumentType = DocumentType.EmploymentContract,
            AccessLevel = AccessLevel.Employee,
            FileName = originalFileName,
            FileStream = fileStream,
            ContentType = "application/pdf",
            UploadedBy = Guid.NewGuid()
        };

        var uploadResult = new UploadResult
        {
            StoragePath = "documents/2025/01/employee_contract_def456.pdf",
            FileSizeBytes = 5,
            ContentType = "application/pdf"
        };

        _mockUploadServiceClient.Setup(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        _mockDocumentRepository.Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert - Result DTO should contain the ORIGINAL unencrypted filename
        result.FileName.Should().Be(originalFileName);
        result.FileName.Should().NotContain("ENCRYPTED"); // Should not be encrypted in response
    }

    [Fact]
    public async Task HandleAsync_WithSpecialCharactersInFileName_ShouldPreserveForEncryption()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var fileNameWithSpecialChars = "Employee_ID#12345_Contract_@2025.pdf";

        var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var command = new UploadDocumentCommand
        {
            EmployeeId = employeeId,
            DocumentType = DocumentType.EmploymentContract,
            AccessLevel = AccessLevel.HROnly,
            FileName = fileNameWithSpecialChars,
            FileStream = fileStream,
            ContentType = "application/pdf",
            UploadedBy = Guid.NewGuid()
        };

        var uploadResult = new UploadResult
        {
            StoragePath = "documents/2025/01/contract_ghi789.pdf",
            FileSizeBytes = 5,
            ContentType = "application/pdf"
        };

        _mockUploadServiceClient.Setup(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        _mockDocumentRepository.Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert - Special characters in filename should be preserved
        _mockDocumentRepository.Verify(x => x.AddAsync(
            It.Is<Document>(d =>
                d.FileName == fileNameWithSpecialChars), // Exact match with special characters
            It.IsAny<CancellationToken>()), Times.Once);

        result.FileName.Should().Be(fileNameWithSpecialChars);
    }

    [Fact]
    public async Task HandleAsync_WithUnicodeFileName_ShouldPreserveForEncryption()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var unicodeFileName = "สัญญาจ้างงาน_Contract_文件.pdf"; // Thai and Chinese characters

        var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var command = new UploadDocumentCommand
        {
            EmployeeId = employeeId,
            DocumentType = DocumentType.EmploymentContract,
            AccessLevel = AccessLevel.HROnly,
            FileName = unicodeFileName,
            FileStream = fileStream,
            ContentType = "application/pdf",
            UploadedBy = Guid.NewGuid()
        };

        var uploadResult = new UploadResult
        {
            StoragePath = "documents/2025/01/contract_jkl012.pdf",
            FileSizeBytes = 5,
            ContentType = "application/pdf"
        };

        _mockUploadServiceClient.Setup(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        _mockDocumentRepository.Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert - Unicode characters should be preserved for encryption
        _mockDocumentRepository.Verify(x => x.AddAsync(
            It.Is<Document>(d =>
                d.FileName == unicodeFileName), // Unicode should be preserved exactly
            It.IsAny<CancellationToken>()), Times.Once);

        result.FileName.Should().Be(unicodeFileName);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotEncryptNonSensitiveFieldsLikeDocumentType()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var documentType = DocumentType.Certificate;

        var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var command = new UploadDocumentCommand
        {
            EmployeeId = employeeId,
            DocumentType = documentType,
            AccessLevel = AccessLevel.Public,
            FileName = "certification.pdf",
            FileStream = fileStream,
            ContentType = "application/pdf",
            UploadedBy = Guid.NewGuid()
        };

        var uploadResult = new UploadResult
        {
            StoragePath = "documents/2025/01/certification_mno345.pdf",
            FileSizeBytes = 5,
            ContentType = "application/pdf"
        };

        _mockUploadServiceClient.Setup(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        _mockDocumentRepository.Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert - Non-sensitive fields like DocumentType should remain as enums (not encrypted)
        _mockDocumentRepository.Verify(x => x.AddAsync(
            It.Is<Document>(d =>
                d.DocumentType == documentType && // Should be stored as enum value
                d.AccessLevel == AccessLevel.Public && // Should be stored as enum value
                d.EmployeeId == employeeId), // Should be stored as Guid
            It.IsAny<CancellationToken>()), Times.Once);

        result.DocumentType.Should().Be(documentType);
    }
}
