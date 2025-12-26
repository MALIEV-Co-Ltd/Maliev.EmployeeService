using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Services;
using Maliev.EmployeeService.Domain.Authorization;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Data.Interceptors;
using Maliev.EmployeeService.Infrastructure.Repositories;
using Maliev.EmployeeService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for document access authorization (T321)
/// Tests role-based access control for documents with different access levels
/// </summary>
public class DocumentAccessAuthorizationIntegrationTests : PostgreSqlIntegrationTestBase
{
    [Fact]
    public async Task PublicDocument_ShouldBeAccessibleByAllUsers()
    {
        // Arrange - Create repositories and authorization service
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<DocumentAuthorizationService>>();
        var mockIamClient = new Mock<IIamServiceClient>();
        var mockConfiguration = new Mock<IConfiguration>();

        // Default IAM to false, let specific tests override if needed
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var authorizationService = new DocumentAuthorizationService(employeeRepository, mockIamClient.Object, mockConfiguration.Object, mockLogger.Object);

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            DocumentType = DocumentType.Certificate,
            FileName = "public_cert.pdf",
            StoragePath = "documents/public_cert.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            UploadDate = DateTime.UtcNow,
            UploadedBy = employee.Id,
            VersionNumber = 1,
            AccessLevel = AccessLevel.Public,
            IsArchived = false
        };

        Context.Employees.Add(employee);
        Context.Documents.Add(document);
        await Context.SaveChangesAsync();

        var otherUser = Guid.NewGuid();

        // Act
        var canAccessAsEmployee = await authorizationService.CanViewDocumentAsync(
            otherUser, document);

        var canAccessAsManager = await authorizationService.CanViewDocumentAsync(
            otherUser, document);

        var canAccessAsHR = await authorizationService.CanViewDocumentAsync(
            otherUser, document);

        // Assert
        Assert.True(canAccessAsEmployee); // public documents should be accessible by all employees
        Assert.True(canAccessAsManager); // public documents should be accessible by managers
        Assert.True(canAccessAsHR); // public documents should be accessible by HR
    }

    [Fact]
    public async Task EmployeeDocument_ShouldOnlyBeAccessibleByOwner()
    {
        // Arrange - Create repositories and authorization service
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<DocumentAuthorizationService>>();
        var mockIamClient = new Mock<IIamServiceClient>();
        var mockConfiguration = new Mock<IConfiguration>();

        var authorizationService = new DocumentAuthorizationService(employeeRepository, mockIamClient.Object, mockConfiguration.Object, mockLogger.Object);

        // Arrange
        var documentOwner = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP002",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var otherEmployee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP003",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = documentOwner.Id,
            DocumentType = DocumentType.IDDocument,
            FileName = "passport.pdf",
            StoragePath = "documents/passport.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 2048,
            UploadDate = DateTime.UtcNow,
            UploadedBy = documentOwner.Id,
            VersionNumber = 1,
            AccessLevel = AccessLevel.Employee,
            IsArchived = false
        };

        Context.Employees.AddRange(documentOwner, otherEmployee);
        Context.Documents.Add(document);
        await Context.SaveChangesAsync();

        // Mock IAM: Owner has permission, Other doesn't
        mockIamClient.Setup(x => x.CheckPermissionAsync(documentOwner.Id.ToString(), EmployeePermissions.DocumentsRead, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockIamClient.Setup(x => x.CheckPermissionAsync(otherEmployee.Id.ToString(), EmployeePermissions.DocumentsRead, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var ownerCanAccess = await authorizationService.CanViewDocumentAsync(
            documentOwner.Id, document);

        var otherEmployeeCanAccess = await authorizationService.CanViewDocumentAsync(
            otherEmployee.Id, document);

        // Assert
        Assert.True(ownerCanAccess); // document owner should be able to access their own document
        Assert.False(otherEmployeeCanAccess); // other employees should not access employee-level documents
    }

    [Fact]
    public async Task ManagerDocument_ShouldBeAccessibleByOwnerAndManager()
    {
        // Arrange - Create repositories and authorization service
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<DocumentAuthorizationService>>();
        var mockIamClient = new Mock<IIamServiceClient>();
        var mockConfiguration = new Mock<IConfiguration>();

        var authorizationService = new DocumentAuthorizationService(employeeRepository, mockIamClient.Object, mockConfiguration.Object, mockLogger.Object);

        // Arrange
        var manager = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "MGR001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP004",
            ManagerId = manager.Id,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var otherEmployee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP005",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            DocumentType = DocumentType.PerformanceReview,
            FileName = "review.pdf",
            StoragePath = "documents/review.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 3072,
            UploadDate = DateTime.UtcNow,
            UploadedBy = manager.Id,
            VersionNumber = 1,
            AccessLevel = AccessLevel.Manager,
            IsArchived = false
        };

        Context.Employees.AddRange(manager, employee, otherEmployee);
        Context.Documents.Add(document);
        await Context.SaveChangesAsync();

        // Mock IAM: Owner and Manager have permission, Other doesn't
        mockIamClient.Setup(x => x.CheckPermissionAsync(employee.Id.ToString(), EmployeePermissions.DocumentsRead, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockIamClient.Setup(x => x.CheckPermissionAsync(manager.Id.ToString(), EmployeePermissions.DocumentsRead, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockIamClient.Setup(x => x.CheckPermissionAsync(otherEmployee.Id.ToString(), EmployeePermissions.DocumentsRead, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var employeeCanAccess = await authorizationService.CanViewDocumentAsync(
            employee.Id, document);

        var managerCanAccess = await authorizationService.CanViewDocumentAsync(
            manager.Id, document);

        var otherEmployeeCanAccess = await authorizationService.CanViewDocumentAsync(
            otherEmployee.Id, document);

        // Assert
        Assert.True(employeeCanAccess); // employee should access their own manager-level document
        Assert.True(managerCanAccess); // manager should access their direct report's document
        Assert.False(otherEmployeeCanAccess); // unrelated employee should not access manager-level document
    }

    [Fact]
    public async Task HROnlyDocument_ShouldOnlyBeAccessibleByHRStaff()
    {
        // Arrange - Create repositories and authorization service
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<DocumentAuthorizationService>>();
        var mockIamClient = new Mock<IIamServiceClient>();
        var mockConfiguration = new Mock<IConfiguration>();

        var authorizationService = new DocumentAuthorizationService(employeeRepository, mockIamClient.Object, mockConfiguration.Object, mockLogger.Object);

        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP006",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var hrGeneralist = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "HR001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var hrSpecialist = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "HR002",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            DocumentType = DocumentType.EmploymentContract,
            FileName = "contract.pdf",
            StoragePath = "documents/contract.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 4096,
            UploadDate = DateTime.UtcNow,
            UploadedBy = hrGeneralist.Id,
            VersionNumber = 1,
            AccessLevel = AccessLevel.HROnly,
            IsArchived = false
        };

        Context.Employees.AddRange(employee, hrGeneralist, hrSpecialist);
        Context.Documents.Add(document);
        await Context.SaveChangesAsync();

        // Mock IAM: HR have permission, Regular doesn't
        mockIamClient.Setup(x => x.CheckPermissionAsync(hrGeneralist.Id.ToString(), EmployeePermissions.DocumentsRead, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockIamClient.Setup(x => x.CheckPermissionAsync(hrSpecialist.Id.ToString(), EmployeePermissions.DocumentsRead, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockIamClient.Setup(x => x.CheckPermissionAsync(employee.Id.ToString(), EmployeePermissions.DocumentsRead, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var employeeCanAccess = await authorizationService.CanViewDocumentAsync(
            employee.Id, document);

        var hrGeneralistCanAccess = await authorizationService.CanViewDocumentAsync(
            hrGeneralist.Id, document);

        var hrSpecialistCanAccess = await authorizationService.CanViewDocumentAsync(
            hrSpecialist.Id, document);

        // Assert
        Assert.False(employeeCanAccess); // regular employees should not access HR-only documents
        Assert.True(hrGeneralistCanAccess); // HR generalists should access HR-only documents
        Assert.True(hrSpecialistCanAccess); // HR specialists should access HR-only documents
    }

    [Fact]
    public async Task HRSpecialistOnlyDocument_ShouldOnlyBeAccessibleByHRSpecialists()
    {
        // Arrange - Create repositories and authorization service
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<DocumentAuthorizationService>>();
        var mockIamClient = new Mock<IIamServiceClient>();
        var mockConfiguration = new Mock<IConfiguration>();

        var authorizationService = new DocumentAuthorizationService(employeeRepository, mockIamClient.Object, mockConfiguration.Object, mockLogger.Object);

        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP007",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var hrGeneralist = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "HR003",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var hrSpecialist = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "HR004",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            DocumentType = DocumentType.IDDocument,
            FileName = "ssn.pdf",
            StoragePath = "documents/ssn.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 512,
            UploadDate = DateTime.UtcNow,
            UploadedBy = hrSpecialist.Id,
            VersionNumber = 1,
            AccessLevel = AccessLevel.HRSpecialistOnly,
            IsArchived = false
        };

        Context.Employees.AddRange(employee, hrGeneralist, hrSpecialist);
        Context.Documents.Add(document);
        await Context.SaveChangesAsync();

        // Mock IAM: ONLY HR Specialist has permission
        mockIamClient.Setup(x => x.CheckPermissionAsync(hrSpecialist.Id.ToString(), EmployeePermissions.DocumentsRead, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockIamClient.Setup(x => x.CheckPermissionAsync(hrGeneralist.Id.ToString(), EmployeePermissions.DocumentsRead, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        mockIamClient.Setup(x => x.CheckPermissionAsync(employee.Id.ToString(), EmployeePermissions.DocumentsRead, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var employeeCanAccess = await authorizationService.CanViewDocumentAsync(
            employee.Id, document);

        var hrGeneralistCanAccess = await authorizationService.CanViewDocumentAsync(
            hrGeneralist.Id, document);

        var hrSpecialistCanAccess = await authorizationService.CanViewDocumentAsync(
            hrSpecialist.Id, document);

        // Assert
        Assert.False(employeeCanAccess); // regular employees should not access HR specialist-only documents
        Assert.False(hrGeneralistCanAccess); // HR generalists should not access HR specialist-only documents
        Assert.True(hrSpecialistCanAccess); // HR specialists should access HR specialist-only documents
    }

    [Fact]
    public async Task SystemAdministrator_ShouldAccessAllDocuments()
    {
        // Arrange - Create repositories and authorization service
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<DocumentAuthorizationService>>();
        var mockIamClient = new Mock<IIamServiceClient>();
        var mockConfiguration = new Mock<IConfiguration>();

        var authorizationService = new DocumentAuthorizationService(employeeRepository, mockIamClient.Object, mockConfiguration.Object, mockLogger.Object);

        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP008",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var admin = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "ADMIN001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var documents = new List<Document>
        {
            new Document
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                DocumentType = DocumentType.IDDocument,
                FileName = "private.pdf",
                StoragePath = "documents/private.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 1024,
                UploadDate = DateTime.UtcNow,
                UploadedBy = employee.Id,
                VersionNumber = 1,
                AccessLevel = AccessLevel.Employee,
                IsArchived = false
            },
            new Document
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                DocumentType = DocumentType.EmploymentContract,
                FileName = "contract.pdf",
                StoragePath = "documents/contract.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 2048,
                UploadDate = DateTime.UtcNow,
                UploadedBy = admin.Id,
                VersionNumber = 1,
                AccessLevel = AccessLevel.HRSpecialistOnly,
                IsArchived = false
            }
        };

        Context.Employees.AddRange(employee, admin);
        Context.Documents.AddRange(documents);
        await Context.SaveChangesAsync();

        // Mock IAM: Admin has permission for all documents
        mockIamClient.Setup(x => x.CheckPermissionAsync(admin.Id.ToString(), EmployeePermissions.DocumentsRead, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert - System administrator should access all documents
        foreach (var document in documents)
        {
            var canAccess = await authorizationService.CanViewDocumentAsync(
                admin.Id, document);

            Assert.True(canAccess, $"system administrator should access document with {document.AccessLevel} access level");
        }
    }

    [Fact]
    public async Task ArchivedDocument_ShouldStillRespectAccessControl()
    {
        // Arrange - Create repositories and authorization service
        var documentRepository = new DocumentRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<DocumentAuthorizationService>>();
        var mockIamClient = new Mock<IIamServiceClient>();
        var mockConfiguration = new Mock<IConfiguration>();

        var authorizationService = new DocumentAuthorizationService(employeeRepository, mockIamClient.Object, mockConfiguration.Object, mockLogger.Object);

        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP009",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var otherEmployee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP010",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var archivedDocument = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            DocumentType = DocumentType.Other,
            FileName = "old_doc.pdf",
            StoragePath = "documents/old_doc.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            UploadDate = DateTime.UtcNow.AddYears(-1),
            UploadedBy = employee.Id,
            VersionNumber = 1,
            AccessLevel = AccessLevel.Employee,
            IsArchived = true
        };

        Context.Employees.AddRange(employee, otherEmployee);
        Context.Documents.Add(archivedDocument);
        await Context.SaveChangesAsync();

        // Mock IAM: Owner has permission, Other doesn't
        mockIamClient.Setup(x => x.CheckPermissionAsync(employee.Id.ToString(), EmployeePermissions.DocumentsRead, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockIamClient.Setup(x => x.CheckPermissionAsync(otherEmployee.Id.ToString(), EmployeePermissions.DocumentsRead, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var ownerCanAccess = await authorizationService.CanViewDocumentAsync(
            employee.Id, archivedDocument);

        var otherEmployeeCanAccess = await authorizationService.CanViewDocumentAsync(
            otherEmployee.Id, archivedDocument);

        // Assert
        Assert.True(ownerCanAccess); // document owner should access their archived documents
        Assert.False(otherEmployeeCanAccess); // archived documents should still respect access control
    }
}
