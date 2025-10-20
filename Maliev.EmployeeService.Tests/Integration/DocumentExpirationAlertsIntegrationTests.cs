using FluentAssertions;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Data.Interceptors;
using Maliev.EmployeeService.Infrastructure.Repositories;
using Maliev.EmployeeService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for document expiration alerts (T322)
/// Tests document expiration detection at various thresholds (90, 60, 30, 14 days)
/// </summary>
public class DocumentExpirationAlertsIntegrationTests : PostgreSqlIntegrationTestBase
{
    [Theory]
    [InlineData(90)]
    [InlineData(60)]
    [InlineData(30)]
    [InlineData(14)]
    public async Task GetExpiringDocumentsAsync_ShouldReturnDocumentsExpiringWithinThreshold(int daysThreshold)
    {
        // Arrange - Create document repository
        var documentRepository = new DocumentRepository(Context);

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

        // Document expiring exactly at threshold
        var documentAtThreshold = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            DocumentType = DocumentType.WorkPermit,
            FileName = $"workpermit_{daysThreshold}.pdf",
            StoragePath = $"documents/workpermit_{daysThreshold}.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            UploadDate = DateTime.UtcNow.AddDays(-365),
            UploadedBy = uploader.Id,
            VersionNumber = 1,
            AccessLevel = AccessLevel.HROnly,
            IsArchived = false,
            ExpirationDate = DateTime.UtcNow.AddDays(daysThreshold)
        };

        // Document expiring within threshold (a few days before)
        var documentWithinThreshold = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            DocumentType = DocumentType.Certificate,
            FileName = $"cert_within_{daysThreshold}.pdf",
            StoragePath = $"documents/cert_within_{daysThreshold}.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 2048,
            UploadDate = DateTime.UtcNow.AddDays(-200),
            UploadedBy = uploader.Id,
            VersionNumber = 1,
            AccessLevel = AccessLevel.Public,
            IsArchived = false,
            ExpirationDate = DateTime.UtcNow.AddDays(daysThreshold - 5)
        };

        Context.Employees.AddRange(employee, uploader);
        Context.Documents.AddRange(documentAtThreshold, documentWithinThreshold);
        await Context.SaveChangesAsync();

        // Act
        var result = await documentRepository.GetExpiringDocumentsAsync(daysThreshold);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCountGreaterThanOrEqualTo(2);
        resultList.Should().Contain(d => d.Id == documentAtThreshold.Id);
        resultList.Should().Contain(d => d.Id == documentWithinThreshold.Id);
    }

    [Fact]
    public async Task GetExpiringDocumentsAsync_ShouldNotReturnDocumentsExpiringBeyondThreshold()
    {
        // Arrange
        var documentRepository = new DocumentRepository(Context);

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

        // Document expiring 31 days from now
        var documentBeyondThreshold = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            DocumentType = DocumentType.IDDocument,
            FileName = "passport_future.pdf",
            StoragePath = "documents/passport_future.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            UploadDate = DateTime.UtcNow,
            UploadedBy = uploader.Id,
            VersionNumber = 1,
            AccessLevel = AccessLevel.HROnly,
            IsArchived = false,
            ExpirationDate = DateTime.UtcNow.AddDays(31)
        };

        Context.Employees.AddRange(employee, uploader);
        Context.Documents.Add(documentBeyondThreshold);
        await Context.SaveChangesAsync();

        // Act - Check for documents expiring within 30 days
        var result = await documentRepository.GetExpiringDocumentsAsync(30);

        // Assert
        var resultList = result.ToList();
        resultList.Should().NotContain(d => d.Id == documentBeyondThreshold.Id);
    }

    [Fact]
    public async Task GetExpiringDocumentsAsync_ShouldNotReturnArchivedDocuments()
    {
        // Arrange
        var documentRepository = new DocumentRepository(Context);

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

        // Archived document expiring within 30 days
        var archivedDocument = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            DocumentType = DocumentType.Other,
            FileName = "archived_expiring.pdf",
            StoragePath = "documents/archived_expiring.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            UploadDate = DateTime.UtcNow.AddYears(-2),
            UploadedBy = uploader.Id,
            VersionNumber = 1,
            AccessLevel = AccessLevel.Employee,
            IsArchived = true,
            ExpirationDate = DateTime.UtcNow.AddDays(15)
        };

        Context.Employees.AddRange(employee, uploader);
        Context.Documents.Add(archivedDocument);
        await Context.SaveChangesAsync();

        // Act
        var result = await documentRepository.GetExpiringDocumentsAsync(30);

        // Assert
        var resultList = result.ToList();
        resultList.Should().NotContain(d => d.Id == archivedDocument.Id);
    }

    [Fact]
    public async Task GetExpiringDocumentsAsync_ShouldNotReturnExpiredDocuments()
    {
        // Arrange
        var documentRepository = new DocumentRepository(Context);

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

        // Already expired document
        var expiredDocument = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            DocumentType = DocumentType.WorkPermit,
            FileName = "expired_permit.pdf",
            StoragePath = "documents/expired_permit.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            UploadDate = DateTime.UtcNow.AddYears(-2),
            UploadedBy = uploader.Id,
            VersionNumber = 1,
            AccessLevel = AccessLevel.HROnly,
            IsArchived = false,
            ExpirationDate = DateTime.UtcNow.AddDays(-10)
        };

        Context.Employees.AddRange(employee, uploader);
        Context.Documents.Add(expiredDocument);
        await Context.SaveChangesAsync();

        // Act
        var result = await documentRepository.GetExpiringDocumentsAsync(30);

        // Assert
        var resultList = result.ToList();
        resultList.Should().NotContain(d => d.Id == expiredDocument.Id);
    }

    [Fact]
    public async Task GetExpiringDocumentsAsync_ShouldNotReturnDocumentsWithoutExpirationDate()
    {
        // Arrange
        var documentRepository = new DocumentRepository(Context);

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

        // Document without expiration date
        var documentWithoutExpiration = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            DocumentType = DocumentType.EmploymentContract,
            FileName = "contract.pdf",
            StoragePath = "documents/contract.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 2048,
            UploadDate = DateTime.UtcNow,
            UploadedBy = uploader.Id,
            VersionNumber = 1,
            AccessLevel = AccessLevel.Employee,
            IsArchived = false,
            ExpirationDate = null
        };

        Context.Employees.AddRange(employee, uploader);
        Context.Documents.Add(documentWithoutExpiration);
        await Context.SaveChangesAsync();

        // Act
        var result = await documentRepository.GetExpiringDocumentsAsync(90);

        // Assert
        var resultList = result.ToList();
        resultList.Should().NotContain(d => d.Id == documentWithoutExpiration.Id);
    }

    [Fact]
    public async Task GetExpiredDocumentsAsync_ShouldReturnAllExpiredDocuments()
    {
        // Arrange
        var documentRepository = new DocumentRepository(Context);

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP006",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "HR006",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var expiredDocuments = new List<Document>
        {
            new Document
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                DocumentType = DocumentType.WorkPermit,
                FileName = "expired_permit1.pdf",
                StoragePath = "documents/expired_permit1.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 1024,
                UploadDate = DateTime.UtcNow.AddYears(-2),
                UploadedBy = uploader.Id,
                VersionNumber = 1,
                AccessLevel = AccessLevel.HROnly,
                IsArchived = false,
                ExpirationDate = DateTime.UtcNow.AddDays(-30)
            },
            new Document
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                DocumentType = DocumentType.Certificate,
                FileName = "expired_cert.pdf",
                StoragePath = "documents/expired_cert.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 2048,
                UploadDate = DateTime.UtcNow.AddYears(-1),
                UploadedBy = uploader.Id,
                VersionNumber = 1,
                AccessLevel = AccessLevel.Public,
                IsArchived = false,
                ExpirationDate = DateTime.UtcNow.AddDays(-5)
            }
        };

        Context.Employees.AddRange(employee, uploader);
        Context.Documents.AddRange(expiredDocuments);
        await Context.SaveChangesAsync();

        // Act
        var result = await documentRepository.GetExpiredDocumentsAsync();

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCountGreaterThanOrEqualTo(2);
        resultList.Should().Contain(d => d.Id == expiredDocuments[0].Id);
        resultList.Should().Contain(d => d.Id == expiredDocuments[1].Id);
        resultList.Should().OnlyContain(d => d.ExpirationDate.HasValue && d.ExpirationDate.Value < DateTime.UtcNow);
    }

    [Fact]
    public async Task GetExpiredDocumentsAsync_ShouldNotReturnArchivedExpiredDocuments()
    {
        // Arrange
        var documentRepository = new DocumentRepository(Context);

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP007",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "HR007",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        // Archived expired document
        var archivedExpiredDocument = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            DocumentType = DocumentType.Other,
            FileName = "archived_expired.pdf",
            StoragePath = "documents/archived_expired.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            UploadDate = DateTime.UtcNow.AddYears(-3),
            UploadedBy = uploader.Id,
            VersionNumber = 1,
            AccessLevel = AccessLevel.Employee,
            IsArchived = true,
            ExpirationDate = DateTime.UtcNow.AddDays(-100)
        };

        Context.Employees.AddRange(employee, uploader);
        Context.Documents.Add(archivedExpiredDocument);
        await Context.SaveChangesAsync();

        // Act
        var result = await documentRepository.GetExpiredDocumentsAsync();

        // Assert
        var resultList = result.ToList();
        resultList.Should().NotContain(d => d.Id == archivedExpiredDocument.Id);
    }

    [Fact]
    public async Task GetExpiredDocumentsAsync_ShouldNotReturnDocumentsExpiringInFuture()
    {
        // Arrange
        var documentRepository = new DocumentRepository(Context);

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP008",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "HR008",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        // Document expiring in the future
        var futureExpirationDocument = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            DocumentType = DocumentType.WorkPermit,
            FileName = "future_permit.pdf",
            StoragePath = "documents/future_permit.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            UploadDate = DateTime.UtcNow,
            UploadedBy = uploader.Id,
            VersionNumber = 1,
            AccessLevel = AccessLevel.HROnly,
            IsArchived = false,
            ExpirationDate = DateTime.UtcNow.AddDays(60)
        };

        Context.Employees.AddRange(employee, uploader);
        Context.Documents.Add(futureExpirationDocument);
        await Context.SaveChangesAsync();

        // Act
        var result = await documentRepository.GetExpiredDocumentsAsync();

        // Assert
        var resultList = result.ToList();
        resultList.Should().NotContain(d => d.Id == futureExpirationDocument.Id);
    }

    [Fact]
    public async Task GetExpiringDocumentsAsync_WithMultipleThresholds_ShouldReturnCorrectDocuments()
    {
        // Arrange
        var documentRepository = new DocumentRepository(Context);

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP009",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var uploader = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "HR009",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var doc90Id = Guid.NewGuid();
        var doc60Id = Guid.NewGuid();
        var doc30Id = Guid.NewGuid();
        var doc14Id = Guid.NewGuid();

        var documents = new List<Document>
        {
            new Document
            {
                Id = doc90Id,
                EmployeeId = employee.Id,
                DocumentType = DocumentType.WorkPermit,
                FileName = "expiring_90.pdf",
                StoragePath = "documents/expiring_90.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 1024,
                UploadDate = DateTime.UtcNow,
                UploadedBy = uploader.Id,
                VersionNumber = 1,
                AccessLevel = AccessLevel.HROnly,
                IsArchived = false,
                ExpirationDate = DateTime.UtcNow.AddDays(85)
            },
            new Document
            {
                Id = doc60Id,
                EmployeeId = employee.Id,
                DocumentType = DocumentType.Certificate,
                FileName = "expiring_60.pdf",
                StoragePath = "documents/expiring_60.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 2048,
                UploadDate = DateTime.UtcNow,
                UploadedBy = uploader.Id,
                VersionNumber = 1,
                AccessLevel = AccessLevel.Public,
                IsArchived = false,
                ExpirationDate = DateTime.UtcNow.AddDays(55)
            },
            new Document
            {
                Id = doc30Id,
                EmployeeId = employee.Id,
                DocumentType = DocumentType.IDDocument,
                FileName = "expiring_30.pdf",
                StoragePath = "documents/expiring_30.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 1536,
                UploadDate = DateTime.UtcNow,
                UploadedBy = uploader.Id,
                VersionNumber = 1,
                AccessLevel = AccessLevel.HROnly,
                IsArchived = false,
                ExpirationDate = DateTime.UtcNow.AddDays(25)
            },
            new Document
            {
                Id = doc14Id,
                EmployeeId = employee.Id,
                DocumentType = DocumentType.Other,
                FileName = "expiring_14.pdf",
                StoragePath = "documents/expiring_14.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 512,
                UploadDate = DateTime.UtcNow,
                UploadedBy = uploader.Id,
                VersionNumber = 1,
                AccessLevel = AccessLevel.Employee,
                IsArchived = false,
                ExpirationDate = DateTime.UtcNow.AddDays(10)
            }
        };

        Context.Employees.AddRange(employee, uploader);
        Context.Documents.AddRange(documents);
        await Context.SaveChangesAsync();

        // Act & Assert - Test each threshold using document IDs to avoid encryption comparison issues
        Context.ChangeTracker.Clear();
        var result90 = await documentRepository.GetExpiringDocumentsAsync(90);
        var list90 = result90.ToList();
        list90.Should().Contain(d => d.Id == doc90Id);
        list90.Should().Contain(d => d.Id == doc60Id);
        list90.Should().Contain(d => d.Id == doc30Id);
        list90.Should().Contain(d => d.Id == doc14Id);

        Context.ChangeTracker.Clear();
        var result60 = await documentRepository.GetExpiringDocumentsAsync(60);
        var list60 = result60.ToList();
        list60.Should().NotContain(d => d.Id == doc90Id);
        list60.Should().Contain(d => d.Id == doc60Id);
        list60.Should().Contain(d => d.Id == doc30Id);
        list60.Should().Contain(d => d.Id == doc14Id);

        Context.ChangeTracker.Clear();
        var result30 = await documentRepository.GetExpiringDocumentsAsync(30);
        var list30 = result30.ToList();
        list30.Should().NotContain(d => d.Id == doc90Id);
        list30.Should().NotContain(d => d.Id == doc60Id);
        list30.Should().Contain(d => d.Id == doc30Id);
        list30.Should().Contain(d => d.Id == doc14Id);

        Context.ChangeTracker.Clear();
        var result14 = await documentRepository.GetExpiringDocumentsAsync(14);
        var list14 = result14.ToList();
        list14.Should().NotContain(d => d.Id == doc90Id);
        list14.Should().NotContain(d => d.Id == doc60Id);
        list14.Should().NotContain(d => d.Id == doc30Id);
        list14.Should().Contain(d => d.Id == doc14Id);
    }
}
