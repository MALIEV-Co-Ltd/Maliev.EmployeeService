using FluentAssertions;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for document upload with Upload Service mock (T319)
/// Tests complete upload workflow, encryption, and metadata storage using PostgreSQL
/// </summary>
public class DocumentUploadIntegrationTests : PostgreSqlIntegrationTestBase
{
    [Fact]
    public async Task UploadDocument_ShouldStoreAndRetrieveFileNameCorrectly()
    {
        // Arrange
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockUploadServiceClient = new Mock<IUploadServiceClient>();
        var mockLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "HR001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.AddRange(employee, uploader);
        await Context.SaveChangesAsync();

        var fileName = "sensitive_contract.pdf";
        var storagePath = "documents/2025/01/sensitive_contract_abc123.pdf";

        mockUploadServiceClient.Setup(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadResult
            {
                StoragePath = storagePath,
                FileSizeBytes = 1024,
                ContentType = "application/pdf"
            });

        var handler = new UploadDocumentCommandHandler(
            documentRepository,
            employeeRepository,
            mockUploadServiceClient.Object,
            unitOfWork,
            mockLogger.Object);

        var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var command = new UploadDocumentCommand
        {
            EmployeeId = employee.Id,
            DocumentType = DocumentType.EmploymentContract,
            AccessLevel = AccessLevel.HROnly,
            FileName = fileName,
            FileStream = fileStream,
            ContentType = "application/pdf",
            Description = "Employment contract",
            UploadedBy = uploader.Id
        };

        // Act
        var result = await handler.HandleAsync(command);

        // Assert - Verify data round-trips correctly through save and load
        result.Should().NotBeNull();
        result.DocumentId.Should().NotBeEmpty();
        result.FileName.Should().Be(fileName);

        // Verify document can be retrieved with correct values
        Context.ChangeTracker.Clear();
        var documentFromDb = await documentRepository.GetByIdAsync(result.DocumentId);

        documentFromDb.Should().NotBeNull();
        documentFromDb!.FileName.Should().Be(fileName);
        documentFromDb.StoragePath.Should().Be(storagePath);
        documentFromDb.DocumentType.Should().Be(DocumentType.EmploymentContract);
        documentFromDb.AccessLevel.Should().Be(AccessLevel.HROnly);
    }

    [Fact]
    public async Task UploadDocument_ShouldStoreMetadataCorrectly()
    {
        // Arrange
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockUploadServiceClient = new Mock<IUploadServiceClient>();
        var mockLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP002",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "HR002",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.AddRange(employee, uploader);
        await Context.SaveChangesAsync();

        var expirationDate = DateTime.UtcNow.AddYears(1);

        mockUploadServiceClient.Setup(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadResult
            {
                StoragePath = "documents/2025/01/passport_xyz789.pdf",
                FileSizeBytes = 2048,
                ContentType = "application/pdf"
            });

        var handler = new UploadDocumentCommandHandler(
            documentRepository,
            employeeRepository,
            mockUploadServiceClient.Object,
            unitOfWork,
            mockLogger.Object);

        var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadDocumentCommand
        {
            EmployeeId = employee.Id,
            DocumentType = DocumentType.IDDocument,
            AccessLevel = AccessLevel.HRSpecialistOnly,
            FileName = "passport.pdf",
            FileStream = fileStream,
            ContentType = "application/pdf",
            Description = "Employee passport",
            ExpirationDate = expirationDate,
            UploadedBy = uploader.Id
        };

        // Act
        var result = await handler.HandleAsync(command);

        // Assert - Verify metadata
        Context.ChangeTracker.Clear();
        var documentFromDb = await documentRepository.GetByIdAsync(result.DocumentId);

        documentFromDb.Should().NotBeNull();
        documentFromDb!.EmployeeId.Should().Be(employee.Id);
        documentFromDb.DocumentType.Should().Be(DocumentType.IDDocument);
        documentFromDb.AccessLevel.Should().Be(AccessLevel.HRSpecialistOnly);
        documentFromDb.FileName.Should().Be("passport.pdf"); // Decrypted by interceptor
        documentFromDb.FileSizeBytes.Should().Be(2048);
        documentFromDb.ContentType.Should().Be("application/pdf");
        documentFromDb.Description.Should().Be("Employee passport");
        documentFromDb.ExpirationDate.Should().BeCloseTo(expirationDate, TimeSpan.FromSeconds(1));
        documentFromDb.UploadedBy.Should().Be(uploader.Id);
        documentFromDb.VersionNumber.Should().Be(1);
        documentFromDb.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task UploadDocument_ShouldCallUploadServiceClient()
    {
        // Arrange
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockUploadServiceClient = new Mock<IUploadServiceClient>();
        var mockLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP003",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "HR003",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.AddRange(employee, uploader);
        await Context.SaveChangesAsync();

        var fileName = "certificate.pdf";
        var contentType = "application/pdf";
        var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        mockUploadServiceClient.Setup(x => x.UploadAsync(
                fileName,
                It.IsAny<Stream>(),
                contentType,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadResult
            {
                StoragePath = "documents/2025/01/certificate_def456.pdf",
                FileSizeBytes = 8,
                ContentType = contentType
            });

        var handler = new UploadDocumentCommandHandler(
            documentRepository,
            employeeRepository,
            mockUploadServiceClient.Object,
            unitOfWork,
            mockLogger.Object);

        var command = new UploadDocumentCommand
        {
            EmployeeId = employee.Id,
            DocumentType = DocumentType.Certificate,
            AccessLevel = AccessLevel.Public,
            FileName = fileName,
            FileStream = fileStream,
            ContentType = contentType,
            UploadedBy = uploader.Id
        };

        // Act
        await handler.HandleAsync(command);

        // Assert - Verify Upload Service client was called
        mockUploadServiceClient.Verify(x => x.UploadAsync(
            fileName,
            It.IsAny<Stream>(),
            contentType,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocument_WithMultipleDocuments_ShouldStoreBothCorrectly()
    {
        // Arrange - Same filename for two different documents
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockUploadServiceClient = new Mock<IUploadServiceClient>();
        var mockLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP004",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "HR004",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.AddRange(employee, uploader);
        await Context.SaveChangesAsync();

        var fileName = "contract.pdf";

        mockUploadServiceClient.SetupSequence(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadResult
            {
                StoragePath = "documents/2025/01/contract_v1.pdf",
                FileSizeBytes = 1024,
                ContentType = "application/pdf"
            })
            .ReturnsAsync(new UploadResult
            {
                StoragePath = "documents/2025/01/contract_v2.pdf",
                FileSizeBytes = 2048,
                ContentType = "application/pdf"
            });

        var handler = new UploadDocumentCommandHandler(
            documentRepository,
            employeeRepository,
            mockUploadServiceClient.Object,
            unitOfWork,
            mockLogger.Object);

        // Act - Upload two documents with same filename
        var command1 = new UploadDocumentCommand
        {
            EmployeeId = employee.Id,
            DocumentType = DocumentType.EmploymentContract,
            AccessLevel = AccessLevel.Employee,
            FileName = fileName,
            FileStream = new MemoryStream(new byte[] { 1, 2, 3 }),
            ContentType = "application/pdf",
            UploadedBy = uploader.Id
        };

        var command2 = new UploadDocumentCommand
        {
            EmployeeId = employee.Id,
            DocumentType = DocumentType.OfferLetter,
            AccessLevel = AccessLevel.Manager,
            FileName = fileName,
            FileStream = new MemoryStream(new byte[] { 4, 5, 6 }),
            ContentType = "application/pdf",
            UploadedBy = uploader.Id
        };

        var result1 = await handler.HandleAsync(command1);
        var result2 = await handler.HandleAsync(command2);

        // Assert - Both documents should be stored and retrievable with correct values
        Context.ChangeTracker.Clear();

        var doc1FromDb = await documentRepository.GetByIdAsync(result1.DocumentId);
        var doc2FromDb = await documentRepository.GetByIdAsync(result2.DocumentId);

        doc1FromDb.Should().NotBeNull();
        doc1FromDb!.FileName.Should().Be(fileName);
        doc1FromDb.StoragePath.Should().Be("documents/2025/01/contract_v1.pdf");
        doc1FromDb.DocumentType.Should().Be(DocumentType.EmploymentContract);
        doc1FromDb.FileSizeBytes.Should().Be(1024);

        doc2FromDb.Should().NotBeNull();
        doc2FromDb!.FileName.Should().Be(fileName);
        doc2FromDb.StoragePath.Should().Be("documents/2025/01/contract_v2.pdf");
        doc2FromDb.DocumentType.Should().Be(DocumentType.OfferLetter);
        doc2FromDb.FileSizeBytes.Should().Be(2048);
    }

    [Fact]
    public async Task UploadDocument_ShouldSetUploadDateToUtcNow()
    {
        // Arrange
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockUploadServiceClient = new Mock<IUploadServiceClient>();
        var mockLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP005",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "HR005",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.AddRange(employee, uploader);
        await Context.SaveChangesAsync();

        mockUploadServiceClient.Setup(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadResult
            {
                StoragePath = "documents/2025/01/document.pdf",
                FileSizeBytes = 512,
                ContentType = "application/pdf"
            });

        var handler = new UploadDocumentCommandHandler(
            documentRepository,
            employeeRepository,
            mockUploadServiceClient.Object,
            unitOfWork,
            mockLogger.Object);

        var beforeUpload = DateTime.UtcNow;

        var command = new UploadDocumentCommand
        {
            EmployeeId = employee.Id,
            DocumentType = DocumentType.Other,
            AccessLevel = AccessLevel.Public,
            FileName = "document.pdf",
            FileStream = new MemoryStream(new byte[] { 1, 2, 3 }),
            ContentType = "application/pdf",
            UploadedBy = uploader.Id
        };

        // Act
        var result = await handler.HandleAsync(command);

        var afterUpload = DateTime.UtcNow;

        // Assert
        Context.ChangeTracker.Clear();
        var documentFromDb = await documentRepository.GetByIdAsync(result.DocumentId);

        documentFromDb.Should().NotBeNull();
        documentFromDb!.UploadDate.Should().BeOnOrAfter(beforeUpload);
        documentFromDb.UploadDate.Should().BeOnOrBefore(afterUpload);
    }

    [Fact]
    public async Task UploadDocument_WithNonExistentEmployee_ShouldThrowException()
    {
        // Arrange
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockUploadServiceClient = new Mock<IUploadServiceClient>();
        var mockLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();

        var nonExistentEmployeeId = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();

        var uploader = new Employee
        {
            Id = uploaderId,
            EmployeeNumber = "HR006",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(uploader);
        await Context.SaveChangesAsync();

        var handler = new UploadDocumentCommandHandler(
            documentRepository,
            employeeRepository,
            mockUploadServiceClient.Object,
            unitOfWork,
            mockLogger.Object);

        var command = new UploadDocumentCommand
        {
            EmployeeId = nonExistentEmployeeId,
            DocumentType = DocumentType.EmploymentContract,
            AccessLevel = AccessLevel.Employee,
            FileName = "contract.pdf",
            FileStream = new MemoryStream(new byte[] { 1, 2, 3 }),
            ContentType = "application/pdf",
            UploadedBy = uploaderId
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.HandleAsync(command));

        // Verify no document was created
        var documentsCount = await Context.Documents.CountAsync();
        documentsCount.Should().Be(0);

        // Verify Upload Service was not called
        mockUploadServiceClient.Verify(x => x.UploadAsync(
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
