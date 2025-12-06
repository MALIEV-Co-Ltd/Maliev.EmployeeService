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
/// Unit tests for UploadDocumentCommandHandler (T316)
/// Tests document upload workflow, Upload Service integration, and metadata storage
/// </summary>
public class UploadDocumentCommandHandlerTests
{
    private readonly Mock<IDocumentRepository> _mockDocumentRepository;
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<IUploadServiceClient> _mockUploadServiceClient;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<UploadDocumentCommandHandler>> _mockLogger;
    private readonly UploadDocumentCommandHandler _handler;

    public UploadDocumentCommandHandlerTests()
    {
        _mockDocumentRepository = new Mock<IDocumentRepository>();
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockUploadServiceClient = new Mock<IUploadServiceClient>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();

        // Setup default uploader mock for any Guid (can be overridden in specific tests)
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
    public async Task HandleAsync_WithValidDocument_ShouldUploadAndCreateMetadata()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var uploadedBy = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var uploader = new Employee
        {
            Id = uploadedBy,
            EmployeeNumber = "HR001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var command = new UploadDocumentCommand
        {
            EmployeeId = employeeId,
            DocumentType = DocumentType.IDDocument,
            AccessLevel = AccessLevel.HROnly,
            FileName = "passport.pdf",
            FileStream = fileStream,
            ContentType = "application/pdf",
            Description = "Employee passport copy",
            ExpirationDate = DateTime.UtcNow.AddYears(5),
            UploadedBy = uploadedBy
        };

        var uploadResult = new UploadResult
        {
            StoragePath = "documents/2025/01/passport_abc123.pdf",
            FileSizeBytes = 5,
            ContentType = "application/pdf"
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(uploadedBy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploader);

        _mockUploadServiceClient.Setup(x => x.UploadAsync(
                "passport.pdf",
                fileStream,
                "application/pdf",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        _mockDocumentRepository.Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.DocumentId);
        Assert.Equal("passport.pdf", result.FileName);
        Assert.Equal(5, result.FileSizeBytes);
        Assert.Equal(1, result.VersionNumber);

        _mockUploadServiceClient.Verify(x => x.UploadAsync(
            "passport.pdf",
            fileStream,
            "application/pdf",
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDocumentRepository.Verify(x => x.AddAsync(
            It.Is<Document>(d =>
                d.EmployeeId == employeeId &&
                d.FileName == "passport.pdf" &&
                d.StoragePath == "documents/2025/01/passport_abc123.pdf" &&
                d.DocumentType == DocumentType.IDDocument &&
                d.AccessLevel == AccessLevel.HROnly &&
                d.FileSizeBytes == 5 &&
                d.ContentType == "application/pdf" &&
                d.Description == "Employee passport copy" &&
                d.UploadedBy == uploadedBy &&
                d.VersionNumber == 1 &&
                d.IsArchived == false),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentEmployee_ShouldThrowException()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadDocumentCommand
        {
            EmployeeId = employeeId,
            DocumentType = DocumentType.EmploymentContract,
            AccessLevel = AccessLevel.Employee,
            FileName = "contract.pdf",
            FileStream = fileStream,
            ContentType = "application/pdf",
            UploadedBy = Guid.NewGuid()
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.HandleAsync(command));

        _mockUploadServiceClient.Verify(x => x.UploadAsync(
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _mockDocumentRepository.Verify(x => x.AddAsync(
            It.IsAny<Document>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithOptionalFieldsNull_ShouldSucceed()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP002",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadDocumentCommand
        {
            EmployeeId = employeeId,
            DocumentType = DocumentType.Other,
            AccessLevel = AccessLevel.Public,
            FileName = "document.pdf",
            FileStream = fileStream,
            ContentType = "application/pdf",
            Description = null, // Optional
            ExpirationDate = null, // Optional
            UploadedBy = Guid.NewGuid()
        };

        var uploadResult = new UploadResult
        {
            StoragePath = "documents/2025/01/document_xyz789.pdf",
            FileSizeBytes = 3,
            ContentType = "application/pdf"
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

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

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.DocumentId);

        _mockDocumentRepository.Verify(x => x.AddAsync(
            It.Is<Document>(d =>
                d.Description == null &&
                d.ExpirationDate == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithDifferentAccessLevels_ShouldRespectAccessLevel()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP003",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadDocumentCommand
        {
            EmployeeId = employeeId,
            DocumentType = DocumentType.PerformanceReview,
            AccessLevel = AccessLevel.Manager,
            FileName = "performance_review.pdf",
            FileStream = fileStream,
            ContentType = "application/pdf",
            UploadedBy = Guid.NewGuid()
        };

        var uploadResult = new UploadResult
        {
            StoragePath = "documents/2025/01/performance_review_def456.pdf",
            FileSizeBytes = 3,
            ContentType = "application/pdf"
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

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

        // Assert
        Assert.NotNull(result);

        _mockDocumentRepository.Verify(x => x.AddAsync(
            It.Is<Document>(d =>
                d.AccessLevel == AccessLevel.Manager &&
                d.DocumentType == DocumentType.PerformanceReview),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetUploadDateToUtcNow()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP004",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadDocumentCommand
        {
            EmployeeId = employeeId,
            DocumentType = DocumentType.Certificate,
            AccessLevel = AccessLevel.Employee,
            FileName = "certificate.pdf",
            FileStream = fileStream,
            ContentType = "application/pdf",
            UploadedBy = Guid.NewGuid()
        };

        var uploadResult = new UploadResult
        {
            StoragePath = "documents/2025/01/certificate_ghi789.pdf",
            FileSizeBytes = 3,
            ContentType = "application/pdf"
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

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

        var beforeUpload = DateTime.UtcNow;

        // Act
        var result = await _handler.HandleAsync(command);

        var afterUpload = DateTime.UtcNow;

        // Assert
        Assert.NotNull(result);

        _mockDocumentRepository.Verify(x => x.AddAsync(
            It.Is<Document>(d =>
                d.UploadDate >= beforeUpload &&
                d.UploadDate <= afterUpload),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithExpirationDate_ShouldStoreExpirationDate()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP005",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var expirationDate = DateTime.UtcNow.AddYears(2);
        var command = new UploadDocumentCommand
        {
            EmployeeId = employeeId,
            DocumentType = DocumentType.Certificate,
            AccessLevel = AccessLevel.Public,
            FileName = "certification.pdf",
            FileStream = fileStream,
            ContentType = "application/pdf",
            ExpirationDate = expirationDate,
            UploadedBy = Guid.NewGuid()
        };

        var uploadResult = new UploadResult
        {
            StoragePath = "documents/2025/01/certification_jkl012.pdf",
            FileSizeBytes = 3,
            ContentType = "application/pdf"
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

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

        // Assert
        Assert.NotNull(result);

        _mockDocumentRepository.Verify(x => x.AddAsync(
            It.Is<Document>(d =>
                d.ExpirationDate.HasValue &&
                d.ExpirationDate.Value == expirationDate),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
