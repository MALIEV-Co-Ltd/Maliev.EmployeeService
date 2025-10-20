using FluentAssertions;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Commands;

/// <summary>
/// Unit tests for RecordTrainingCompletionCommandHandler (T286)
/// Tests training record creation with certificate linking and status calculation
/// </summary>
public class RecordTrainingCompletionCommandHandlerTests
{
    private readonly Mock<ITrainingRepository> _trainingRepositoryMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<ILogger<RecordTrainingCompletionCommandHandler>> _loggerMock;
    private readonly RecordTrainingCompletionCommandHandler _handler;

    public RecordTrainingCompletionCommandHandlerTests()
    {
        _trainingRepositoryMock = new Mock<ITrainingRepository>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _loggerMock = new Mock<ILogger<RecordTrainingCompletionCommandHandler>>();

        _handler = new RecordTrainingCompletionCommandHandler(
            _trainingRepositoryMock.Object,
            _employeeRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidMandatoryTraining_CreatesRecordSuccessfully()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            LegalName = new LegalName("John", "Doe", null)
        };

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _trainingRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<TrainingRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrainingRecord tr, CancellationToken ct) =>
            {
                tr.UpdateStatus(); // Simulate repository behavior
                return tr;
            });

        var command = new RecordTrainingCompletionCommand
        {
            EmployeeId = employeeId,
            CourseName = "Safety Training",
            CompletionDate = DateTime.UtcNow.Date,
            ExpirationDate = DateTime.UtcNow.AddYears(1).Date,
            CertificateDocumentId = "CERT123",
            TrainingType = TrainingType.Mandatory,
            Provider = "Safety Corp"
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.CourseName.Should().Be("Safety Training");
        result.EmployeeId.Should().Be(employeeId);
        result.TrainingType.Should().Be(TrainingType.Mandatory);
        result.CertificateDocumentId.Should().Be("CERT123");
        result.Provider.Should().Be("Safety Corp");
        result.Status.Should().Be(CertificationStatus.Valid);

        _trainingRepositoryMock.Verify(
            x => x.CreateAsync(It.IsAny<TrainingRecord>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_VoluntaryTrainingWithoutExpiration_CreatesRecordSuccessfully()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP002",
            LegalName = new LegalName("Jane", "Smith", null)
        };

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _trainingRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<TrainingRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrainingRecord tr, CancellationToken ct) =>
            {
                tr.UpdateStatus(); // Simulate repository behavior
                return tr;
            });

        var command = new RecordTrainingCompletionCommand
        {
            EmployeeId = employeeId,
            CourseName = "Leadership Development",
            CompletionDate = DateTime.UtcNow.Date,
            ExpirationDate = null,
            CertificateDocumentId = null,
            TrainingType = TrainingType.Voluntary,
            Provider = "Training Institute"
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.CourseName.Should().Be("Leadership Development");
        result.TrainingType.Should().Be(TrainingType.Voluntary);
        result.ExpirationDate.Should().BeNull();
        result.CertificateDocumentId.Should().BeNull();
        result.Status.Should().Be(CertificationStatus.Valid);
    }

    [Fact]
    public async Task HandleAsync_TrainingWithCertificateLink_StoresCertificateId()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP003",
            LegalName = new LegalName("Bob", "Johnson", null)
        };

        var certificateId = "CERT-DOC-12345";

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        TrainingRecord? capturedRecord = null;
        _trainingRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<TrainingRecord>(), It.IsAny<CancellationToken>()))
            .Callback<TrainingRecord, CancellationToken>((tr, ct) => capturedRecord = tr)
            .ReturnsAsync((TrainingRecord tr, CancellationToken ct) => tr);

        var command = new RecordTrainingCompletionCommand
        {
            EmployeeId = employeeId,
            CourseName = "First Aid Certification",
            CompletionDate = DateTime.UtcNow.Date,
            ExpirationDate = DateTime.UtcNow.AddYears(2).Date,
            CertificateDocumentId = certificateId,
            TrainingType = TrainingType.Mandatory,
            Provider = "Red Cross"
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.CertificateDocumentId.Should().Be(certificateId);
        capturedRecord.Should().NotBeNull();
        capturedRecord!.CertificateDocumentId.Should().Be(certificateId);
    }

    [Fact]
    public async Task HandleAsync_ExpiringCertification_SetsExpiringStatus()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP004",
            LegalName = new LegalName("Alice", "Brown", null)
        };

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _trainingRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<TrainingRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrainingRecord tr, CancellationToken ct) =>
            {
                tr.UpdateStatus(); // Simulate repository behavior
                return tr;
            });

        var command = new RecordTrainingCompletionCommand
        {
            EmployeeId = employeeId,
            CourseName = "CPR Certification",
            CompletionDate = DateTime.UtcNow.AddYears(-1).Date,
            ExpirationDate = DateTime.UtcNow.AddDays(30).Date, // Expires in 30 days
            TrainingType = TrainingType.Mandatory,
            Provider = "Medical Training"
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(CertificationStatus.Expiring);
    }

    [Fact]
    public async Task HandleAsync_EmployeeNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var employeeId = Guid.NewGuid();

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new RecordTrainingCompletionCommand
        {
            EmployeeId = employeeId,
            CourseName = "Some Training",
            CompletionDate = DateTime.UtcNow.Date,
            TrainingType = TrainingType.Mandatory
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_CompletionDateProvided_SetsCorrectDates()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP005",
            LegalName = new LegalName("Charlie", "Davis", null)
        };

        var completionDate = new DateTime(2024, 6, 15);
        var expirationDate = new DateTime(2026, 6, 15);

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        TrainingRecord? capturedRecord = null;
        _trainingRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<TrainingRecord>(), It.IsAny<CancellationToken>()))
            .Callback<TrainingRecord, CancellationToken>((tr, ct) => capturedRecord = tr)
            .ReturnsAsync((TrainingRecord tr, CancellationToken ct) => tr);

        var command = new RecordTrainingCompletionCommand
        {
            EmployeeId = employeeId,
            CourseName = "Security Awareness",
            CompletionDate = completionDate,
            ExpirationDate = expirationDate,
            TrainingType = TrainingType.Mandatory
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        capturedRecord.Should().NotBeNull();
        capturedRecord!.CompletionDate.Should().Be(completionDate);
        capturedRecord.ExpirationDate.Should().Be(expirationDate);
    }

    [Fact]
    public async Task HandleAsync_MultipleProviders_StoresProviderInformation()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP006",
            LegalName = new LegalName("Diana", "Evans", null)
        };

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _trainingRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<TrainingRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrainingRecord tr, CancellationToken ct) => tr);

        var providers = new[] { "Coursera", "Udemy", "LinkedIn Learning", null };

        foreach (var provider in providers)
        {
            var command = new RecordTrainingCompletionCommand
            {
                EmployeeId = employeeId,
                CourseName = $"Course from {provider ?? "Unknown"}",
                CompletionDate = DateTime.UtcNow.Date,
                TrainingType = TrainingType.Voluntary,
                Provider = provider
            };

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Provider.Should().Be(provider);
        }
    }
}
