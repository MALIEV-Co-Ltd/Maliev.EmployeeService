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
/// Integration tests for document version control (T320)
/// Tests document versioning, history tracking, and version metadata using PostgreSQL
/// </summary>
public class DocumentVersionControlIntegrationTests : PostgreSqlIntegrationTestBase
{

    [Fact]
    public async Task UploadNewVersion_ShouldIncrementVersionNumber()
    {
        // Arrange - Create repositories and mocks
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockUploadServiceClient = new Mock<IUploadServiceClient>();
        var mockUploadLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();
        var mockVersionLogger = new Mock<ILogger<UploadDocumentVersionCommandHandler>>();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp001@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "HR001",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "HR", LastName = "User" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "hr001@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.AddRange(employee, uploader);
        await Context.SaveChangesAsync();

        // Create initial document via UploadDocumentCommandHandler (proper way)
        mockUploadServiceClient.Setup(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadResult
            {
                StoragePath = "documents/contract_v1.pdf",
                FileSizeBytes = 1024,
                ContentType = "application/pdf"
            });

        var uploadHandler = new UploadDocumentCommandHandler(
            documentRepository,
            employeeRepository,
            mockUploadServiceClient.Object,
            unitOfWork,
            mockUploadLogger.Object);

        var uploadCommand = new UploadDocumentCommand
        {
            EmployeeId = employee.Id,
            DocumentType = DocumentType.EmploymentContract,
            AccessLevel = AccessLevel.Employee,
            FileName = "contract_v1.pdf",
            FileStream = new MemoryStream(new byte[] { 1, 2, 3 }),
            ContentType = "application/pdf",
            UploadedBy = uploader.Id
        };

        var uploadResult = await uploadHandler.HandleAsync(uploadCommand);

        // Setup mock for version upload
        mockUploadServiceClient.Setup(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadResult
            {
                StoragePath = "documents/contract_v2.pdf",
                FileSizeBytes = 2048,
                ContentType = "application/pdf"
            });

        var versionHandler = new UploadDocumentVersionCommandHandler(
            documentRepository,
            employeeRepository,
            mockUploadServiceClient.Object,
            unitOfWork,
            mockVersionLogger.Object);

        var versionCommand = new UploadDocumentVersionCommand
        {
            DocumentId = uploadResult.DocumentId,
            FileName = "contract_v2.pdf",
            FileStream = new MemoryStream(new byte[] { 1, 2, 3, 4 }),
            ContentType = "application/pdf",
            ChangeDescription = "Updated contract",
            UploadedBy = uploader.Id
        };

        // Act
        var result = await versionHandler.HandleAsync(versionCommand);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.VersionNumber);

        Context.ChangeTracker.Clear();
        var updatedDocument = await documentRepository.GetByIdAsync(uploadResult.DocumentId);

        Assert.NotNull(updatedDocument);
        Assert.Equal(2, updatedDocument!.VersionNumber);
        Assert.Equal("contract_v2.pdf", updatedDocument.FileName);
        Assert.Equal(2048, updatedDocument.FileSizeBytes);
    }

    [Fact]
    public async Task UploadNewVersion_ShouldCreateVersionHistory()
    {
        // Arrange - Create repositories and mocks
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockUploadServiceClient = new Mock<IUploadServiceClient>();
        var mockUploadLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();
        var mockVersionLogger = new Mock<ILogger<UploadDocumentVersionCommandHandler>>();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP002",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee2" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp002@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "HR002",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "HR", LastName = "User2" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "hr002@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.AddRange(employee, uploader);
        await Context.SaveChangesAsync();

        // Create initial document via UploadDocumentCommandHandler
        mockUploadServiceClient.Setup(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadResult
            {
                StoragePath = "documents/passport_v1.pdf",
                FileSizeBytes = 1024,
                ContentType = "application/pdf"
            });

        var uploadHandler = new UploadDocumentCommandHandler(
            documentRepository,
            employeeRepository,
            mockUploadServiceClient.Object,
            unitOfWork,
            mockUploadLogger.Object);

        var uploadCommand = new UploadDocumentCommand
        {
            EmployeeId = employee.Id,
            DocumentType = DocumentType.IDDocument,
            AccessLevel = AccessLevel.HROnly,
            FileName = "passport_v1.pdf",
            FileStream = new MemoryStream(new byte[] { 1, 2, 3 }),
            ContentType = "application/pdf",
            UploadedBy = uploader.Id
        };

        var uploadResult = await uploadHandler.HandleAsync(uploadCommand);

        // Setup mock for version upload
        mockUploadServiceClient.Setup(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadResult
            {
                StoragePath = "documents/passport_v2.pdf",
                FileSizeBytes = 2048,
                ContentType = "application/pdf"
            });

        var versionHandler = new UploadDocumentVersionCommandHandler(
            documentRepository,
            employeeRepository,
            mockUploadServiceClient.Object,
            unitOfWork,
            mockVersionLogger.Object);

        var versionCommand = new UploadDocumentVersionCommand
        {
            DocumentId = uploadResult.DocumentId,
            FileName = "passport_v2.pdf",
            FileStream = new MemoryStream(new byte[] { 1, 2, 3, 4 }),
            ContentType = "application/pdf",
            ChangeDescription = "Updated passport scan",
            UploadedBy = uploader.Id
        };

        // Act
        await versionHandler.HandleAsync(versionCommand);

        // Assert - Verify version history was created
        Context.ChangeTracker.Clear();
        var versionHistory = await Context.DocumentVersions
            .Where(dv => dv.DocumentId == uploadResult.DocumentId)
            .OrderBy(dv => dv.VersionNumber)
            .ToListAsync();

        Assert.Single(versionHistory);
        Assert.Equal(1, versionHistory[0].VersionNumber);
        Assert.Equal("passport_v1.pdf", versionHistory[0].FileName);
        Assert.Equal("documents/passport_v1.pdf", versionHistory[0].StoragePath);
        Assert.Equal(1024, versionHistory[0].FileSizeBytes);
        Assert.Equal("Previous version archived", versionHistory[0].ChangeDescription);
        Assert.Equal(uploader.Id, versionHistory[0].UploadedBy);
    }

    [Fact]
    public async Task UploadMultipleVersions_ShouldMaintainCompleteHistory()
    {
        // Arrange - Create repositories and mocks
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockUploadServiceClient = new Mock<IUploadServiceClient>();
        var mockUploadLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();
        var mockVersionLogger = new Mock<ILogger<UploadDocumentVersionCommandHandler>>();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP003",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee3" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp003@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "HR003",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "HR", LastName = "User3" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "hr003@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.AddRange(employee, uploader);
        await Context.SaveChangesAsync();

        // Create initial document via UploadDocumentCommandHandler
        mockUploadServiceClient.Setup(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadResult
            {
                StoragePath = "documents/cert_v1.pdf",
                FileSizeBytes = 512,
                ContentType = "application/pdf"
            });

        var uploadHandler = new UploadDocumentCommandHandler(
            documentRepository,
            employeeRepository,
            mockUploadServiceClient.Object,
            unitOfWork,
            mockUploadLogger.Object);

        var uploadCommand = new UploadDocumentCommand
        {
            EmployeeId = employee.Id,
            DocumentType = DocumentType.Certificate,
            AccessLevel = AccessLevel.Public,
            FileName = "cert_v1.pdf",
            FileStream = new MemoryStream(new byte[] { 1, 2, 3 }),
            ContentType = "application/pdf",
            UploadedBy = uploader.Id
        };

        var uploadResult = await uploadHandler.HandleAsync(uploadCommand);

        var versionHandler = new UploadDocumentVersionCommandHandler(
            documentRepository,
            employeeRepository,
            mockUploadServiceClient.Object,
            unitOfWork,
            mockVersionLogger.Object);

        // Act - Upload 3 additional versions
        for (int i = 2; i <= 4; i++)
        {
            mockUploadServiceClient.Setup(x => x.UploadAsync(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UploadResult
                {
                    StoragePath = $"documents/cert_v{i}.pdf",
                    FileSizeBytes = 512 * i,
                    ContentType = "application/pdf"
                });

            var command = new UploadDocumentVersionCommand
            {
                DocumentId = uploadResult.DocumentId,
                FileName = $"cert_v{i}.pdf",
                FileStream = new MemoryStream(new byte[i * 100]),
                ContentType = "application/pdf",
                ChangeDescription = $"Version {i}",
                UploadedBy = uploader.Id
            };

            await versionHandler.HandleAsync(command);
            mockUploadServiceClient.Reset();
        }

        // Assert
        Context.ChangeTracker.Clear();
        var updatedDocument = await documentRepository.GetByIdAsync(uploadResult.DocumentId);
        var versionHistory = await Context.DocumentVersions
            .Where(dv => dv.DocumentId == uploadResult.DocumentId)
            .OrderBy(dv => dv.VersionNumber)
            .ToListAsync();

        Assert.Equal(4, updatedDocument!.VersionNumber);
        Assert.Equal("cert_v4.pdf", updatedDocument.FileName);

        Assert.Equal(3, versionHistory.Count()); // Versions 1, 2, and 3
        Assert.Equal(new[] { 1, 2, 3 }, versionHistory.Select(v => v.VersionNumber).ToArray());
    }

    [Fact]
    public async Task UploadNewVersion_WithNonExistentDocument_ShouldThrowException()
    {
        // Arrange - Create repositories and mocks
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockUploadServiceClient = new Mock<IUploadServiceClient>();
        var mockLogger = new Mock<ILogger<UploadDocumentVersionCommandHandler>>();

        // Arrange
        var nonExistentDocumentId = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();

        var uploader = new Employee
        {
            Id = uploaderId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "HR004",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "HR", LastName = "User4" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "hr004@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(uploader);
        await Context.SaveChangesAsync();

        var handler = new UploadDocumentVersionCommandHandler(
            documentRepository,
            employeeRepository,
            mockUploadServiceClient.Object,
            unitOfWork,
            mockLogger.Object);

        var command = new UploadDocumentVersionCommand
        {
            DocumentId = nonExistentDocumentId,
            FileName = "new_version.pdf",
            FileStream = new MemoryStream(new byte[] { 1, 2, 3 }),
            ContentType = "application/pdf",
            UploadedBy = uploaderId
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.HandleAsync(command));

        // Verify no version history was created
        var versionCount = await Context.DocumentVersions.CountAsync();
        Assert.Equal(0, versionCount);
    }

    [Fact]
    public async Task GetDocumentVersionHistory_ShouldReturnAllVersionsOrdered()
    {
        // Arrange - Create repositories and mocks
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockUploadServiceClient = new Mock<IUploadServiceClient>();
        var mockLogger = new Mock<ILogger<UploadDocumentVersionCommandHandler>>();

        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP005",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee5" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp005@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "HR005",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "HR", LastName = "User5" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "hr005@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.AddRange(employee, uploader);
        await Context.SaveChangesAsync();

        var documentId = Guid.NewGuid();

        // Create the parent document first
        var document = new Document
        {
            Id = documentId,
            EmployeeId = employee.Id,
            DocumentType = DocumentType.EmploymentContract,
            AccessLevel = AccessLevel.Employee,
            FileName = "doc_v2.pdf",
            StoragePath = "documents/doc_v2.pdf",
            FileSizeBytes = 1500,
            ContentType = "application/pdf",
            UploadDate = DateTime.UtcNow.AddDays(-15),
            UploadedBy = uploader.Id,
            VersionNumber = 2,
            IsArchived = false,
            CreatedDate = DateTime.UtcNow
        };

        Context.Documents.Add(document);
        await Context.SaveChangesAsync();

        // Create version history
        var versions = new List<DocumentVersion>
        {
            new DocumentVersion
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                VersionNumber = 1,
                FileName = "doc_v1.pdf",
                StoragePath = "documents/doc_v1.pdf",
                FileSizeBytes = 1000,
                ContentType = "application/pdf",
                UploadDate = DateTime.UtcNow.AddDays(-30),
                UploadedBy = uploader.Id,
                ChangeDescription = "Initial version"
            },
            new DocumentVersion
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                VersionNumber = 2,
                FileName = "doc_v2.pdf",
                StoragePath = "documents/doc_v2.pdf",
                FileSizeBytes = 1500,
                ContentType = "application/pdf",
                UploadDate = DateTime.UtcNow.AddDays(-15),
                UploadedBy = uploader.Id,
                ChangeDescription = "First update"
            }
        };

        Context.DocumentVersions.AddRange(versions);
        await Context.SaveChangesAsync();

        // Act
        var history = await Context.DocumentVersions
            .Where(dv => dv.DocumentId == documentId)
            .OrderByDescending(dv => dv.VersionNumber)
            .ToListAsync();

        // Assert
        Assert.Equal(2, history.Count());
        Assert.Equal(2, history[0].VersionNumber); // Most recent first
        Assert.Equal("First update", history[0].ChangeDescription);
        Assert.Equal(1, history[1].VersionNumber);
        Assert.Equal("Initial version", history[1].ChangeDescription);
    }
}
