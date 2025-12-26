using System.Security.Claims;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Security;

public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly CurrentUserService _service;

    public CurrentUserServiceTests()
    {
        _service = new CurrentUserService(
            _httpContextAccessorMock.Object,
            _serviceProviderMock.Object,
            _cacheServiceMock.Object,
            NullLogger<CurrentUserService>.Instance);
    }

    private void SetupUserWithClaims(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    [Fact]
    public void PrincipalId_ShouldReturnId_WhenSubClaimExists()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        SetupUserWithClaims(new Claim("sub", expectedId.ToString()));

        // Act
        var result = _service.PrincipalId;

        // Assert
        Assert.Equal(expectedId, result);
    }

    [Fact]
    public void PrincipalId_ShouldThrow_WhenSubClaimIsMalformed()
    {
        // Arrange
        SetupUserWithClaims(new Claim("sub", "not-a-guid"));

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() => _service.PrincipalId);
    }

    [Fact]
    public async Task GetEmployeeIdAsync_ShouldReturnFromCache_WhenAvailable()
    {
        // Arrange
        var principalId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        SetupUserWithClaims(new Claim("sub", principalId.ToString()));
        
        _cacheServiceMock.Setup(x => x.GetAsync<CurrentUserService.EmployeeIdCacheWrapper>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurrentUserService.EmployeeIdCacheWrapper(employeeId));

        // Act
        var result = await _service.GetEmployeeIdAsync();

        // Assert
        Assert.Equal(employeeId, result);
        _employeeRepositoryMock.Verify(x => x.GetByPrincipalIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetEmployeeIdAsync_ShouldFetchFromDbAndCache_WhenNotCached()
    {
        // Arrange
        var principalId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        SetupUserWithClaims(new Claim("sub", principalId.ToString()));
        
        _cacheServiceMock.Setup(x => x.GetAsync<CurrentUserService.EmployeeIdCacheWrapper>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrentUserService.EmployeeIdCacheWrapper?)null);

        var serviceScopeMock = new Mock<IServiceScope>();
        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        
        serviceScopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactoryMock.Object);
        serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IEmployeeRepository))).Returns(_employeeRepositoryMock.Object);

        _employeeRepositoryMock.Setup(x => x.GetByPrincipalIdAsync(principalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee { Id = employeeId });

        // Act
        var result = await _service.GetEmployeeIdAsync();

        // Assert
        Assert.Equal(employeeId, result);
        _cacheServiceMock.Verify(x => x.SetAsync(
            $"principal_mapping:{principalId}", 
            It.Is<CurrentUserService.EmployeeIdCacheWrapper>(w => w.Id == employeeId), 
            It.Is<TimeSpan?>(t => t == TimeSpan.FromHours(24)), 
            null, 
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
