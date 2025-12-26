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
        // Ensure clean database state
        await InitializeTestAsync();

        // Arrange
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockUploadServiceClient = new Mock<IUploadServiceClient>();
        var mockLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
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
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.DocumentId);
        Assert.Equal(fileName, result.FileName);

        // Verify document can be retrieved with correct values
        Context.ChangeTracker.Clear();
        var documentFromDb = await documentRepository.GetByIdAsync(result.DocumentId);

        Assert.NotNull(documentFromDb);
        Assert.Equal(fileName, documentFromDb!.FileName);
        Assert.Equal(storagePath, documentFromDb.StoragePath);
        Assert.Equal(DocumentType.EmploymentContract, documentFromDb.DocumentType);
        Assert.Equal(AccessLevel.HROnly, documentFromDb.AccessLevel);
    }

    [Fact]
    public async Task UploadDocument_ShouldStoreMetadataCorrectly()
    {
        // Ensure clean database state
        await InitializeTestAsync();

        // Arrange
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockUploadServiceClient = new Mock<IUploadServiceClient>();
        var mockLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP002",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
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

        Assert.NotNull(documentFromDb);
        Assert.Equal(employee.Id, documentFromDb!.EmployeeId);
        Assert.Equal(DocumentType.IDDocument, documentFromDb.DocumentType);
        Assert.Equal(AccessLevel.HRSpecialistOnly, documentFromDb.AccessLevel);
        Assert.Equal("passport.pdf", documentFromDb.FileName); // Decrypted by interceptor
        Assert.Equal(2048, documentFromDb.FileSizeBytes);
        Assert.Equal("application/pdf", documentFromDb.ContentType);
        Assert.Equal("Employee passport", documentFromDb.Description);
        Assert.True(Math.Abs((documentFromDb.ExpirationDate!.Value - expirationDate).TotalSeconds) <= 1);
        Assert.Equal(uploader.Id, documentFromDb.UploadedBy);
        Assert.Equal(1, documentFromDb.VersionNumber);
        Assert.False(documentFromDb.IsArchived);
    }

    [Fact]
    public async Task UploadDocument_ShouldCallUploadServiceClient()
    {
        // Ensure clean database state
        await InitializeTestAsync();

        // Arrange
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockUploadServiceClient = new Mock<IUploadServiceClient>();
        var mockLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP003",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
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
        // Ensure clean database state
        await InitializeTestAsync();

        // Arrange - Same filename for two different documents
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockUploadServiceClient = new Mock<IUploadServiceClient>();
        var mockLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP004",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
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

        Assert.NotNull(doc1FromDb);
        Assert.Equal(fileName, doc1FromDb!.FileName);
        Assert.Equal("documents/2025/01/contract_v1.pdf", doc1FromDb.StoragePath);
        Assert.Equal(DocumentType.EmploymentContract, doc1FromDb.DocumentType);
        Assert.Equal(1024, doc1FromDb.FileSizeBytes);

        Assert.NotNull(doc2FromDb);
        Assert.Equal(fileName, doc2FromDb!.FileName);
        Assert.Equal("documents/2025/01/contract_v2.pdf", doc2FromDb.StoragePath);
        Assert.Equal(DocumentType.OfferLetter, doc2FromDb.DocumentType);
        Assert.Equal(2048, doc2FromDb.FileSizeBytes);
    }

    [Fact]
    public async Task UploadDocument_ShouldSetUploadDateToUtcNow()
    {
        // Ensure clean database state
        await InitializeTestAsync();

        // Arrange
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockUploadServiceClient = new Mock<IUploadServiceClient>();
        var mockLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP005",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
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

        Assert.NotNull(documentFromDb);
        Assert.True(documentFromDb!.UploadDate >= beforeUpload);
        Assert.True(documentFromDb.UploadDate <= afterUpload);
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
        Assert.Equal(0, documentsCount);

        // Verify Upload Service was not called
        mockUploadServiceClient.Verify(x => x.UploadAsync(
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
