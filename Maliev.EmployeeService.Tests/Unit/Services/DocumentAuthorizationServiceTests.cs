using FluentAssertions;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Services;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Services;

/// <summary>
/// Unit tests for DocumentAuthorizationService (T316)
/// Tests role-based authorization for document viewing, uploading, and deleting
/// </summary>
public class DocumentAuthorizationServiceTests
{
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<ILogger<DocumentAuthorizationService>> _mockLogger;
    private readonly DocumentAuthorizationService _service;

    public DocumentAuthorizationServiceTests()
    {
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockLogger = new Mock<ILogger<DocumentAuthorizationService>>();
        _service = new DocumentAuthorizationService(_mockEmployeeRepository.Object, _mockLogger.Object);
    }

    #region CanViewDocumentAsync Tests

    [Fact]
    public async Task CanViewDocumentAsync_WithSystemAdministrator_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            AccessLevel = AccessLevel.HRSpecialistOnly // Most restrictive
        };

        // Act
        var result = await _service.CanViewDocumentAsync(userId, document, Role.SystemAdministrator);

        // Assert
        result.Should().BeTrue();
        _mockEmployeeRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(Role.Employee)]
    [InlineData(Role.Manager)]
    [InlineData(Role.HRGeneralist)]
    [InlineData(Role.HRSpecialist)]
    [InlineData(Role.SystemAdministrator)]
    public async Task CanViewDocumentAsync_WithPublicDocument_ShouldAlwaysReturnTrue(Role userRole)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            AccessLevel = AccessLevel.Public
        };

        // Act
        var result = await _service.CanViewDocumentAsync(userId, document, userRole);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanViewDocumentAsync_WithEmployeeLevelDocument_OwnerShouldSeeIt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = userId, // Same as userId
            AccessLevel = AccessLevel.Employee
        };

        // Act
        var result = await _service.CanViewDocumentAsync(userId, document, Role.Employee);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanViewDocumentAsync_WithEmployeeLevelDocument_NonOwnerShouldNotSeeIt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = otherUserId, // Different from userId
            AccessLevel = AccessLevel.Employee
        };

        // Act
        var result = await _service.CanViewDocumentAsync(userId, document, Role.Employee);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanViewDocumentAsync_WithManagerLevelDocument_OwnerShouldSeeIt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = userId, // Same as userId
            AccessLevel = AccessLevel.Manager
        };

        // Act
        var result = await _service.CanViewDocumentAsync(userId, document, Role.Employee);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanViewDocumentAsync_WithManagerLevelDocument_DirectManagerShouldSeeIt()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            AccessLevel = AccessLevel.Manager
        };

        var employee = new Employee
        {
            Id = employeeId,
            ManagerId = managerId,
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Act
        var result = await _service.CanViewDocumentAsync(managerId, document, Role.Manager);

        // Assert
        result.Should().BeTrue();
        _mockEmployeeRepository.Verify(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CanViewDocumentAsync_WithManagerLevelDocument_NonManagerShouldNotSeeIt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId, // Different employee
            AccessLevel = AccessLevel.Manager
        };

        var employee = new Employee
        {
            Id = employeeId,
            ManagerId = Guid.NewGuid(), // Different manager
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Act
        var result = await _service.CanViewDocumentAsync(userId, document, Role.Employee);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(Role.HRGeneralist)]
    [InlineData(Role.HRSpecialist)]
    public async Task CanViewDocumentAsync_WithHROnlyDocument_HRRolesShouldSeeIt(Role hrRole)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            AccessLevel = AccessLevel.HROnly
        };

        // Act
        var result = await _service.CanViewDocumentAsync(userId, document, hrRole);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(Role.Employee)]
    [InlineData(Role.Manager)]
    public async Task CanViewDocumentAsync_WithHROnlyDocument_NonHRRolesShouldNotSeeIt(Role nonHrRole)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = userId, // Even if it's their own document
            AccessLevel = AccessLevel.HROnly
        };

        // Act
        var result = await _service.CanViewDocumentAsync(userId, document, nonHrRole);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanViewDocumentAsync_WithHRSpecialistOnlyDocument_HRSpecialistShouldSeeIt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            AccessLevel = AccessLevel.HRSpecialistOnly
        };

        // Act
        var result = await _service.CanViewDocumentAsync(userId, document, Role.HRSpecialist);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(Role.Employee)]
    [InlineData(Role.Manager)]
    [InlineData(Role.HRGeneralist)]
    public async Task CanViewDocumentAsync_WithHRSpecialistOnlyDocument_NonHRSpecialistShouldNotSeeIt(Role nonSpecialistRole)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            AccessLevel = AccessLevel.HRSpecialistOnly
        };

        // Act
        var result = await _service.CanViewDocumentAsync(userId, document, nonSpecialistRole);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CanUploadDocumentAsync Tests

    [Theory]
    [InlineData(Role.SystemAdministrator)]
    [InlineData(Role.HRGeneralist)]
    [InlineData(Role.HRSpecialist)]
    public async Task CanUploadDocumentAsync_WithAdminOrHRRole_ShouldAlwaysReturnTrue(Role privilegedRole)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        // Act
        var result = await _service.CanUploadDocumentAsync(userId, employeeId, privilegedRole);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanUploadDocumentAsync_WithEmployee_ForOwnDocuments_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.CanUploadDocumentAsync(userId, userId, Role.Employee);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanUploadDocumentAsync_WithEmployee_ForOtherEmployee_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherEmployeeId = Guid.NewGuid();

        // Act
        var result = await _service.CanUploadDocumentAsync(userId, otherEmployeeId, Role.Employee);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanUploadDocumentAsync_WithManager_ForDirectReport_ShouldReturnTrue()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var employee = new Employee
        {
            Id = employeeId,
            ManagerId = managerId,
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Act
        var result = await _service.CanUploadDocumentAsync(managerId, employeeId, Role.Manager);

        // Assert
        result.Should().BeTrue();
        _mockEmployeeRepository.Verify(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CanUploadDocumentAsync_WithManager_ForNonDirectReport_ShouldReturnFalse()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var employee = new Employee
        {
            Id = employeeId,
            ManagerId = Guid.NewGuid(), // Different manager
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Act
        var result = await _service.CanUploadDocumentAsync(managerId, employeeId, Role.Manager);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CanDeleteDocumentAsync Tests

    [Theory]
    [InlineData(Role.SystemAdministrator)]
    [InlineData(Role.HRGeneralist)]
    [InlineData(Role.HRSpecialist)]
    public async Task CanDeleteDocumentAsync_WithAdminOrHRRole_ShouldReturnTrue(Role privilegedRole)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            AccessLevel = AccessLevel.Public
        };

        // Act
        var result = await _service.CanDeleteDocumentAsync(userId, document, privilegedRole);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(Role.Employee)]
    [InlineData(Role.Manager)]
    public async Task CanDeleteDocumentAsync_WithNonPrivilegedRole_ShouldReturnFalse(Role regularRole)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = userId, // Even their own document
            AccessLevel = AccessLevel.Employee
        };

        // Act
        var result = await _service.CanDeleteDocumentAsync(userId, document, regularRole);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Validate Methods Tests

    [Fact]
    public async Task ValidateCanViewDocumentAsync_WithAuthorizedUser_ShouldNotThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            AccessLevel = AccessLevel.Public
        };

        // Act
        var act = async () => await _service.ValidateCanViewDocumentAsync(userId, document, Role.Employee);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateCanViewDocumentAsync_WithUnauthorizedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(), // Different employee
            AccessLevel = AccessLevel.Employee
        };

        // Act
        var act = async () => await _service.ValidateCanViewDocumentAsync(userId, document, Role.Employee);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage($"User {userId} is not authorized to view document {document.Id}");
    }

    [Fact]
    public async Task ValidateCanUploadDocumentAsync_WithAuthorizedUser_ShouldNotThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await _service.ValidateCanUploadDocumentAsync(userId, userId, Role.Employee);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateCanUploadDocumentAsync_WithUnauthorizedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherEmployeeId = Guid.NewGuid();

        // Act
        var act = async () => await _service.ValidateCanUploadDocumentAsync(userId, otherEmployeeId, Role.Employee);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage($"User {userId} is not authorized to upload documents for employee {otherEmployeeId}");
    }

    [Fact]
    public async Task ValidateCanDeleteDocumentAsync_WithAuthorizedUser_ShouldNotThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            AccessLevel = AccessLevel.Public
        };

        // Act
        var act = async () => await _service.ValidateCanDeleteDocumentAsync(userId, document, Role.HRSpecialist);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateCanDeleteDocumentAsync_WithUnauthorizedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = userId,
            AccessLevel = AccessLevel.Employee
        };

        // Act
        var act = async () => await _service.ValidateCanDeleteDocumentAsync(userId, document, Role.Employee);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage($"User {userId} is not authorized to delete document {document.Id}");
    }

    #endregion
}
