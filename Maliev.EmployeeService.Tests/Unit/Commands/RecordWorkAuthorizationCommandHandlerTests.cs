using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Commands;

/// <summary>
/// Unit tests for RecordWorkAuthorizationCommandHandler
/// Tests work authorization recording, document validation, and employee verification
/// </summary>
public class RecordWorkAuthorizationCommandHandlerTests
{
    private readonly Mock<IWorkAuthorizationRepository> _mockWorkAuthorizationRepository;
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<IDocumentRepository> _mockDocumentRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<RecordWorkAuthorizationCommandHandler>> _mockLogger;
    private readonly RecordWorkAuthorizationCommandHandler _handler;

    public RecordWorkAuthorizationCommandHandlerTests()
    {
        _mockWorkAuthorizationRepository = new Mock<IWorkAuthorizationRepository>();
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockDocumentRepository = new Mock<IDocumentRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<RecordWorkAuthorizationCommandHandler>>();

        _handler = new RecordWorkAuthorizationCommandHandler(
            _mockWorkAuthorizationRepository.Object,
            _mockEmployeeRepository.Object,
            _mockDocumentRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidWorkPermit_ShouldCreateAuthorization()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var command = new RecordWorkAuthorizationCommand
        {
            EmployeeId = employeeId,
            AuthorizationType = AuthorizationType.WorkPermit,
            DocumentNumber = "WP123456",
            IssueDate = DateTime.UtcNow.AddDays(-30),
            ExpirationDate = DateTime.UtcNow.AddYears(2),
            IssuingAuthority = "Thai Ministry of Labour",
            SponsorshipStatus = SponsorshipStatus.Approved,
            Notes = "Work permit for software engineer position"
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockWorkAuthorizationRepository.Setup(x => x.AddAsync(It.IsAny<WorkAuthorization>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.NotEqual(Guid.Empty, result);

        _mockWorkAuthorizationRepository.Verify(x => x.AddAsync(
            It.Is<WorkAuthorization>(wa =>
                wa.EmployeeId == employeeId &&
                wa.AuthorizationType == AuthorizationType.WorkPermit &&
                wa.DocumentNumber == "WP123456" &&
                wa.IssuingAuthority == "Thai Ministry of Labour" &&
                wa.SponsorshipStatus == SponsorshipStatus.Approved &&
                wa.IsActive == true),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithCitizenship_ShouldNotRequireExpirationDate()
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

        var command = new RecordWorkAuthorizationCommand
        {
            EmployeeId = employeeId,
            AuthorizationType = AuthorizationType.Citizenship,
            DocumentNumber = "TH1234567890123",
            IssueDate = DateTime.UtcNow.AddYears(-10),
            ExpirationDate = null, // Citizenship doesn't expire
            IssuingAuthority = "Thai Ministry of Interior",
            SponsorshipStatus = SponsorshipStatus.NotRequired,
            Notes = "Thai citizen"
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockWorkAuthorizationRepository.Setup(x => x.AddAsync(It.IsAny<WorkAuthorization>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.NotEqual(Guid.Empty, result);

        _mockWorkAuthorizationRepository.Verify(x => x.AddAsync(
            It.Is<WorkAuthorization>(wa =>
                wa.AuthorizationType == AuthorizationType.Citizenship &&
                wa.ExpirationDate == null &&
                wa.SponsorshipStatus == SponsorshipStatus.NotRequired),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithDocumentReference_ShouldValidateDocument()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP003",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var document = new Document
        {
            Id = documentId,
            EmployeeId = employeeId,
            DocumentType = DocumentType.WorkPermit,
            FileName = "work_permit.pdf",
            UploadDate = DateTime.UtcNow.AddDays(-5),
            UploadedBy = Guid.NewGuid(),
            StoragePath = "/documents/work_permit.pdf",
            FileSizeBytes = 1024000,
            ContentType = "application/pdf",
            VersionNumber = 1
        };

        var command = new RecordWorkAuthorizationCommand
        {
            EmployeeId = employeeId,
            AuthorizationType = AuthorizationType.Visa,
            DocumentNumber = "VISA987654",
            IssueDate = DateTime.UtcNow.AddDays(-60),
            ExpirationDate = DateTime.UtcNow.AddYears(3),
            IssuingAuthority = "Embassy of Thailand",
            SponsorshipStatus = SponsorshipStatus.Approved,
            RightToWorkDocumentId = documentId
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockWorkAuthorizationRepository.Setup(x => x.AddAsync(It.IsAny<WorkAuthorization>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.NotEqual(Guid.Empty, result);

        _mockDocumentRepository.Verify(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()), Times.Once);

        _mockWorkAuthorizationRepository.Verify(x => x.AddAsync(
            It.Is<WorkAuthorization>(wa => wa.RightToWorkDocumentId == documentId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentEmployee_ShouldThrowException()
    {
        // Arrange
        var employeeId = Guid.NewGuid();

        var command = new RecordWorkAuthorizationCommand
        {
            EmployeeId = employeeId,
            AuthorizationType = AuthorizationType.WorkPermit,
            DocumentNumber = "WP123456",
            IssueDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddYears(1),
            IssuingAuthority = "Authority",
            SponsorshipStatus = SponsorshipStatus.Pending
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(command));

        _mockWorkAuthorizationRepository.Verify(x => x.AddAsync(It.IsAny<WorkAuthorization>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentDocument_ShouldThrowException()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP004",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var command = new RecordWorkAuthorizationCommand
        {
            EmployeeId = employeeId,
            AuthorizationType = AuthorizationType.WorkPermit,
            DocumentNumber = "WP123456",
            IssueDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddYears(1),
            IssuingAuthority = "Authority",
            SponsorshipStatus = SponsorshipStatus.Pending,
            RightToWorkDocumentId = documentId
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(command));

        _mockWorkAuthorizationRepository.Verify(x => x.AddAsync(It.IsAny<WorkAuthorization>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithDocumentBelongingToDifferentEmployee_ShouldThrowException()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var otherEmployeeId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP005",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var document = new Document
        {
            Id = documentId,
            EmployeeId = otherEmployeeId, // Different employee!
            DocumentType = DocumentType.WorkPermit,
            FileName = "work_permit.pdf",
            UploadDate = DateTime.UtcNow.AddDays(-5),
            UploadedBy = Guid.NewGuid(),
            StoragePath = "/documents/work_permit.pdf",
            FileSizeBytes = 1024000,
            ContentType = "application/pdf",
            VersionNumber = 1
        };

        var command = new RecordWorkAuthorizationCommand
        {
            EmployeeId = employeeId,
            AuthorizationType = AuthorizationType.WorkPermit,
            DocumentNumber = "WP123456",
            IssueDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddYears(1),
            IssuingAuthority = "Authority",
            SponsorshipStatus = SponsorshipStatus.Pending,
            RightToWorkDocumentId = documentId
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockDocumentRepository.Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(command));
        Assert.Contains("does not belong to the specified employee", exception.Message);

        _mockWorkAuthorizationRepository.Verify(x => x.AddAsync(It.IsAny<WorkAuthorization>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP006",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var command = new RecordWorkAuthorizationCommand
        {
            EmployeeId = employeeId,
            AuthorizationType = AuthorizationType.WorkPermit,
            DocumentNumber = "WP123456",
            IssueDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddYears(1),
            IssuingAuthority = "Authority",
            SponsorshipStatus = SponsorshipStatus.Approved
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockWorkAuthorizationRepository.Setup(x => x.AddAsync(It.IsAny<WorkAuthorization>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        _mockWorkAuthorizationRepository.Verify(x => x.AddAsync(
            It.Is<WorkAuthorization>(wa => wa.IsActive == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetCreatedDate()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP007",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var command = new RecordWorkAuthorizationCommand
        {
            EmployeeId = employeeId,
            AuthorizationType = AuthorizationType.Visa,
            DocumentNumber = "VISA12345",
            IssueDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddYears(2),
            IssuingAuthority = "Embassy",
            SponsorshipStatus = SponsorshipStatus.Approved
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockWorkAuthorizationRepository.Setup(x => x.AddAsync(It.IsAny<WorkAuthorization>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var before = DateTime.UtcNow;
        var result = await _handler.HandleAsync(command);
        var after = DateTime.UtcNow;

        // Assert
        _mockWorkAuthorizationRepository.Verify(x => x.AddAsync(
            It.Is<WorkAuthorization>(wa =>
                wa.CreatedDate >= before &&
                wa.CreatedDate <= after),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
