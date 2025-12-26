using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Queries;

/// <summary>
/// Unit tests for GetCompensationDetailsQueryHandler
/// Tests permission-based authorization for sensitive compensation data access
/// </summary>
public class GetCompensationDetailsQueryHandlerTests
{
    private readonly Mock<ICompensationRepository> _mockCompensationRepository;
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IIamServiceClient> _mockIamClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly GetCompensationDetailsQueryHandler _handler;

    public GetCompensationDetailsQueryHandlerTests()
    {
        _mockCompensationRepository = new Mock<ICompensationRepository>();
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockIamClient = new Mock<IIamServiceClient>();
        _mockConfiguration = new Mock<IConfiguration>();

        _handler = new GetCompensationDetailsQueryHandler(
            _mockCompensationRepository.Object,
            _mockEmployeeRepository.Object,
            _mockIamClient.Object,
            _mockConfiguration.Object,
            _mockCurrentUserService.Object);
            
        _mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());
    }

    [Fact]
    public async Task HandleAsync_WhenIAMGrantsAccess_ShouldAllowAccess()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var query = new GetCompensationDetailsQuery(employeeId);
        var principalId = _mockCurrentUserService.Object.PrincipalId!.Value;

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var compensationRecord = new CompensationRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            SalaryAmount = "85000.00",
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Annual review",
            CreatedDate = DateTime.UtcNow
        };

        _mockIamClient.Setup(x => x.CheckPermissionAsync(
            principalId.ToString(),
            EmployeePermissions.CompensationRead,
            $"employee/{employeeId}",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _mockCompensationRepository.Setup(x => x.GetCurrentAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(compensationRecord);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(employeeId, result!.EmployeeId);
        Assert.Equal(85000.00m, result.SalaryAmount);
    }

    [Fact]
    public async Task HandleAsync_WhenIAMDeniesAccess_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var query = new GetCompensationDetailsQuery(employeeId);

        _mockIamClient.Setup(x => x.CheckPermissionAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.HandleAsync(query));

        _mockEmployeeRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentEmployee_ShouldReturnNull()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var query = new GetCompensationDetailsQuery(employeeId);

        _mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_WithNoCompensationRecord_ShouldReturnNull()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var query = new GetCompensationDetailsQuery(employeeId);

        _mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee { Id = employeeId });
        _mockCompensationRepository.Setup(x => x.GetCurrentAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompensationRecord?)null);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Null(result);
    }
}
