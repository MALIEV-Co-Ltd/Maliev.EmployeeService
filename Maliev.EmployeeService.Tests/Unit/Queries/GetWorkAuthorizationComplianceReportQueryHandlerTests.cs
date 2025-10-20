using FluentAssertions;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Queries;

/// <summary>
/// Unit tests for GetWorkAuthorizationComplianceReportQueryHandler
/// Tests compliance reporting for work authorization expiration and sponsorship status
/// </summary>
public class GetWorkAuthorizationComplianceReportQueryHandlerTests
{
    private readonly Mock<IWorkAuthorizationRepository> _mockWorkAuthorizationRepository;
    private readonly GetWorkAuthorizationComplianceReportQueryHandler _handler;

    public GetWorkAuthorizationComplianceReportQueryHandlerTests()
    {
        _mockWorkAuthorizationRepository = new Mock<IWorkAuthorizationRepository>();

        _handler = new GetWorkAuthorizationComplianceReportQueryHandler(
            _mockWorkAuthorizationRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_WithExpiringAndExpiredAuthorizations_ShouldReturnCompleteReport()
    {
        // Arrange
        var query = new GetWorkAuthorizationComplianceReportQuery
        {
            DaysUntilExpiration = 90
        };

        var expiringList = new List<WorkAuthorization>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = Guid.NewGuid(),
                AuthorizationType = AuthorizationType.WorkPermit,
                DocumentNumber = "WP123",
                ExpirationDate = DateTime.UtcNow.AddDays(30),
                IsActive = true,
                Employee = new Employee
                {
                    Id = Guid.NewGuid(),
                    EmployeeNumber = "EMP001",
                    LegalName = new LegalName("John", "Doe"),
                    EmploymentStatus = EmploymentStatus.Active,
                    StartDate = DateTime.UtcNow.AddYears(-1),
                    CreatedDate = DateTime.UtcNow.AddYears(-1),
                    Department = new Department
                    {
                        Id = Guid.NewGuid(),
                        Name = "Engineering",
                        Description = "Engineering Department",
                        HeadcountLimit = 100,
                        CreatedDate = DateTime.UtcNow
                    }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = Guid.NewGuid(),
                AuthorizationType = AuthorizationType.Visa,
                DocumentNumber = "VISA456",
                ExpirationDate = DateTime.UtcNow.AddDays(60),
                IsActive = true,
                Employee = new Employee
                {
                    Id = Guid.NewGuid(),
                    EmployeeNumber = "EMP002",
                    LegalName = new LegalName("Jane", "Smith"),
                    EmploymentStatus = EmploymentStatus.Active,
                    StartDate = DateTime.UtcNow.AddYears(-2),
                    CreatedDate = DateTime.UtcNow.AddYears(-2),
                    Department = new Department
                    {
                        Id = Guid.NewGuid(),
                        Name = "Marketing",
                        Description = "Marketing Department",
                        HeadcountLimit = 50,
                        CreatedDate = DateTime.UtcNow
                    }
                }
            }
        };

        var expiredList = new List<WorkAuthorization>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = Guid.NewGuid(),
                AuthorizationType = AuthorizationType.WorkPermit,
                DocumentNumber = "WP789",
                ExpirationDate = DateTime.UtcNow.AddDays(-10),
                IsActive = true,
                Employee = new Employee
                {
                    Id = Guid.NewGuid(),
                    EmployeeNumber = "EMP003",
                    LegalName = new LegalName("Bob", "Johnson"),
                    EmploymentStatus = EmploymentStatus.Active,
                    StartDate = DateTime.UtcNow.AddYears(-3),
                    CreatedDate = DateTime.UtcNow.AddYears(-3),
                    Department = new Department
                    {
                        Id = Guid.NewGuid(),
                        Name = "Sales",
                        Description = "Sales Department",
                        HeadcountLimit = 75,
                        CreatedDate = DateTime.UtcNow
                    }
                }
            }
        };

        var sponsorshipSummary = new Dictionary<string, int>
        {
            { "Approved", 10 },
            { "Pending", 3 },
            { "NotRequired", 50 }
        };

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetExpiringAsync(90, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiringList);

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetExpiredAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredList);

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetSponsorshipStatusSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsorshipSummary);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalActive.Should().Be(63); // 10 + 3 + 50
        result.ExpiringSoon.Should().Be(2);
        result.Expired.Should().Be(1);
        result.ExpiringAuthorizations.Should().HaveCount(2);
        result.ExpiredAuthorizations.Should().HaveCount(1);
        result.SponsorshipStatusSummary.Should().HaveCount(3);
        result.SponsorshipStatusSummary["Approved"].Should().Be(10);
    }

    [Fact]
    public async Task HandleAsync_ShouldPopulateExpiringAuthorizationDetails()
    {
        // Arrange
        var query = new GetWorkAuthorizationComplianceReportQuery
        {
            DaysUntilExpiration = 30
        };

        var authorizationId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var expirationDate = DateTime.UtcNow.AddDays(15);

        var expiringList = new List<WorkAuthorization>
        {
            new()
            {
                Id = authorizationId,
                EmployeeId = employeeId,
                AuthorizationType = AuthorizationType.Visa,
                DocumentNumber = "VISA123456",
                ExpirationDate = expirationDate,
                IsActive = true,
                Employee = new Employee
                {
                    Id = employeeId,
                    EmployeeNumber = "EMP100",
                    LegalName = new LegalName("Alice", "Wong"),
                    EmploymentStatus = EmploymentStatus.Active,
                    StartDate = DateTime.UtcNow.AddYears(-1),
                    CreatedDate = DateTime.UtcNow.AddYears(-1),
                    Department = new Department
                    {
                        Id = Guid.NewGuid(),
                        Name = "IT",
                        Description = "IT Department",
                        HeadcountLimit = 40,
                        CreatedDate = DateTime.UtcNow
                    }
                }
            }
        };

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetExpiringAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiringList);

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetExpiredAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkAuthorization>());

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetSponsorshipStatusSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.ExpiringAuthorizations.Should().HaveCount(1);
        var expiringAuth = result.ExpiringAuthorizations.First();
        expiringAuth.AuthorizationId.Should().Be(authorizationId);
        expiringAuth.EmployeeId.Should().Be(employeeId);
        expiringAuth.EmployeeNumber.Should().Be("EMP100");
        expiringAuth.EmployeeName.Should().Be("Alice Wong");
        expiringAuth.AuthorizationType.Should().Be("Visa");
        expiringAuth.DocumentNumber.Should().Be("VISA123456");
        expiringAuth.ExpirationDate.Should().Be(expirationDate);
        expiringAuth.DaysUntilExpiration.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(15);
        expiringAuth.Department.Should().Be("IT");
    }

    [Fact]
    public async Task HandleAsync_ShouldPopulateExpiredAuthorizationDetails()
    {
        // Arrange
        var query = new GetWorkAuthorizationComplianceReportQuery
        {
            DaysUntilExpiration = 60
        };

        var authorizationId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var expirationDate = DateTime.UtcNow.AddDays(-20);

        var expiredList = new List<WorkAuthorization>
        {
            new()
            {
                Id = authorizationId,
                EmployeeId = employeeId,
                AuthorizationType = AuthorizationType.WorkPermit,
                DocumentNumber = "WP999888",
                ExpirationDate = expirationDate,
                IsActive = true,
                Employee = new Employee
                {
                    Id = employeeId,
                    EmployeeNumber = "EMP200",
                    LegalName = new LegalName("Carlos", "Rodriguez"),
                    EmploymentStatus = EmploymentStatus.Active,
                    StartDate = DateTime.UtcNow.AddYears(-2),
                    CreatedDate = DateTime.UtcNow.AddYears(-2),
                    Department = new Department
                    {
                        Id = Guid.NewGuid(),
                        Name = "Operations",
                        Description = "Operations Department",
                        HeadcountLimit = 60,
                        CreatedDate = DateTime.UtcNow
                    }
                }
            }
        };

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetExpiringAsync(60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkAuthorization>());

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetExpiredAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredList);

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetSponsorshipStatusSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.ExpiredAuthorizations.Should().HaveCount(1);
        var expiredAuth = result.ExpiredAuthorizations.First();
        expiredAuth.AuthorizationId.Should().Be(authorizationId);
        expiredAuth.EmployeeId.Should().Be(employeeId);
        expiredAuth.EmployeeNumber.Should().Be("EMP200");
        expiredAuth.EmployeeName.Should().Be("Carlos Rodriguez");
        expiredAuth.AuthorizationType.Should().Be("WorkPermit");
        expiredAuth.DocumentNumber.Should().Be("WP999888");
        expiredAuth.ExpirationDate.Should().Be(expirationDate);
        expiredAuth.DaysUntilExpiration.Should().BeLessThan(0);
        expiredAuth.Department.Should().Be("Operations");
    }

    [Fact]
    public async Task HandleAsync_WithNoExpiringOrExpired_ShouldReturnEmptyLists()
    {
        // Arrange
        var query = new GetWorkAuthorizationComplianceReportQuery
        {
            DaysUntilExpiration = 90
        };

        var sponsorshipSummary = new Dictionary<string, int>
        {
            { "NotRequired", 100 }
        };

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetExpiringAsync(90, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkAuthorization>());

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetExpiredAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkAuthorization>());

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetSponsorshipStatusSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsorshipSummary);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.TotalActive.Should().Be(100);
        result.ExpiringSoon.Should().Be(0);
        result.Expired.Should().Be(0);
        result.ExpiringAuthorizations.Should().BeEmpty();
        result.ExpiredAuthorizations.Should().BeEmpty();
        result.SponsorshipStatusSummary["NotRequired"].Should().Be(100);
    }

    [Fact]
    public async Task HandleAsync_WithMissingEmployeeData_ShouldUseDefaults()
    {
        // Arrange
        var query = new GetWorkAuthorizationComplianceReportQuery
        {
            DaysUntilExpiration = 30
        };

        var expiringList = new List<WorkAuthorization>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = Guid.NewGuid(),
                AuthorizationType = AuthorizationType.Visa,
                DocumentNumber = "VISA000",
                ExpirationDate = DateTime.UtcNow.AddDays(20),
                IsActive = true,
                Employee = null // Employee data not loaded
            }
        };

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetExpiringAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiringList);

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetExpiredAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkAuthorization>());

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetSponsorshipStatusSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.ExpiringAuthorizations.Should().HaveCount(1);
        var expiringAuth = result.ExpiringAuthorizations.First();
        expiringAuth.EmployeeNumber.Should().Be("Unknown");
        expiringAuth.EmployeeName.Should().Be("Unknown");
        expiringAuth.Department.Should().Be("Unknown");
    }

    [Fact]
    public async Task HandleAsync_WithDefaultDaysUntilExpiration_ShouldUse90Days()
    {
        // Arrange
        var query = new GetWorkAuthorizationComplianceReportQuery(); // No explicit DaysUntilExpiration

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetExpiringAsync(90, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkAuthorization>());

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetExpiredAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkAuthorization>());

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetSponsorshipStatusSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());

        // Act
        await _handler.HandleAsync(query);

        // Assert
        _mockWorkAuthorizationRepository.Verify(
            x => x.GetExpiringAsync(90, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCalculateDaysUntilExpirationCorrectly()
    {
        // Arrange
        var query = new GetWorkAuthorizationComplianceReportQuery
        {
            DaysUntilExpiration = 60
        };

        var now = DateTime.UtcNow;
        var expirationDate = now.AddDays(45);

        var expiringList = new List<WorkAuthorization>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = Guid.NewGuid(),
                AuthorizationType = AuthorizationType.Visa,
                DocumentNumber = "VISA555",
                ExpirationDate = expirationDate,
                IsActive = true,
                Employee = new Employee
                {
                    Id = Guid.NewGuid(),
                    EmployeeNumber = "EMP500",
                    LegalName = new LegalName("Test", "User"),
                    EmploymentStatus = EmploymentStatus.Active,
                    StartDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow
                }
            }
        };

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetExpiringAsync(60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiringList);

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetExpiredAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkAuthorization>());

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetSponsorshipStatusSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        var expiringAuth = result.ExpiringAuthorizations.First();
        expiringAuth.DaysUntilExpiration.Should().NotBeNull();
        expiringAuth.DaysUntilExpiration.Should().BeGreaterThanOrEqualTo(44).And.BeLessThanOrEqualTo(46);
    }

    [Fact]
    public async Task HandleAsync_WithNullExpirationDate_ShouldHandleGracefully()
    {
        // Arrange
        var query = new GetWorkAuthorizationComplianceReportQuery
        {
            DaysUntilExpiration = 90
        };

        var expiringList = new List<WorkAuthorization>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = Guid.NewGuid(),
                AuthorizationType = AuthorizationType.Citizenship,
                DocumentNumber = "TH123",
                ExpirationDate = null, // Citizenship doesn't expire
                IsActive = true,
                Employee = new Employee
                {
                    Id = Guid.NewGuid(),
                    EmployeeNumber = "EMP600",
                    LegalName = new LegalName("Thai", "Citizen"),
                    EmploymentStatus = EmploymentStatus.Active,
                    StartDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow
                }
            }
        };

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetExpiringAsync(90, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiringList);

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetExpiredAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkAuthorization>());

        _mockWorkAuthorizationRepository
            .Setup(x => x.GetSponsorshipStatusSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        var expiringAuth = result.ExpiringAuthorizations.First();
        expiringAuth.ExpirationDate.Should().BeNull();
        expiringAuth.DaysUntilExpiration.Should().BeNull();
    }
}
