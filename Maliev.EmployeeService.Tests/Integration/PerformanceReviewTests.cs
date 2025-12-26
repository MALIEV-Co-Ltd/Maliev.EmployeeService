using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration test for performance review creation by manager for direct report
/// Tests T256 - Manager creates performance review for their direct report
/// </summary>
public class PerformanceReviewTests : PostgreSqlIntegrationTestBase
{
    [Fact]
    public async Task CreatePerformanceReview_AsManagerForDirectReport_ShouldSucceed()
    {
        // Arrange - Create manager and direct report relationship
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);

        var managerId = Guid.NewGuid();
        var manager = new Employee
        {
            Id = managerId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "MGR001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-3),
            CreatedDate = DateTime.UtcNow.AddYears(-3)
        };

        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            ManagerId = managerId, // Direct report to manager
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        Context.Employees.AddRange(manager, employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.GetEmployeeIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(managerId);
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(managerId);

        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();

        var handler = new CreatePerformanceReviewCommandHandler(
            performanceReviewRepository,
            employeeRepository,
            unitOfWork,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        var command = new CreatePerformanceReviewCommand(
            employeeId,
            managerId,
            ReviewCycle.Annual,
            DateTime.UtcNow.AddYears(-1),
            DateTime.UtcNow,
            "Employee self-assessment: Exceeded expectations in all areas");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.PerformanceReviewId.HasValue && result.PerformanceReviewId.Value != Guid.Empty);
        Assert.Null(result.ErrorMessage);

        // Verify in database
        var savedReview = await performanceReviewRepository.GetByIdAsync(result.PerformanceReviewId!.Value);
        Assert.NotNull(savedReview);
        Assert.Equal(employeeId, savedReview!.EmployeeId);
        Assert.Equal(managerId, savedReview.ReviewerId);
        Assert.Equal(ReviewCycle.Annual, savedReview.ReviewCycle);
        Assert.Equal("Draft", savedReview.Status);
        Assert.Contains("Exceeded expectations", savedReview.SelfAssessment);
        Assert.True(Math.Abs((savedReview.ReviewPeriodStart - DateTime.UtcNow.AddYears(-1)).TotalHours) <= 1);
        Assert.True(Math.Abs((savedReview.ReviewPeriodEnd - DateTime.UtcNow).TotalHours) <= 1);
    }

    [Fact]
    public async Task CreatePerformanceReview_QuarterlyReview_ShouldSucceed()
    {
        // Arrange
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);

        var managerId = Guid.NewGuid();
        var manager = new Employee
        {
            Id = managerId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "MGR002",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP002",
            ManagerId = managerId,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        Context.Employees.AddRange(manager, employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.GetEmployeeIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(managerId);
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(managerId);

        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();

        var handler = new CreatePerformanceReviewCommandHandler(
            performanceReviewRepository,
            employeeRepository,
            unitOfWork,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        var command = new CreatePerformanceReviewCommand(
            employeeId,
            managerId,
            ReviewCycle.Quarterly,
            DateTime.UtcNow.AddMonths(-3),
            DateTime.UtcNow,
            null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);

        var savedReview = await performanceReviewRepository.GetByIdAsync(result.PerformanceReviewId!.Value);
        Assert.Equal(ReviewCycle.Quarterly, savedReview!.ReviewCycle);
        Assert.True(Math.Abs((savedReview.ReviewPeriodStart - DateTime.UtcNow.AddMonths(-3)).TotalHours) <= 1);
    }

    [Fact]
    public async Task CreatePerformanceReview_SemiAnnualReview_ShouldSucceed()
    {
        // Arrange
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);

        var managerId = Guid.NewGuid();
        var manager = new Employee
        {
            Id = managerId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "MGR003",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP003",
            ManagerId = managerId,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        Context.Employees.AddRange(manager, employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.GetEmployeeIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(managerId);
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(managerId);

        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();

        var handler = new CreatePerformanceReviewCommandHandler(
            performanceReviewRepository,
            employeeRepository,
            unitOfWork,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        var command = new CreatePerformanceReviewCommand(
            employeeId,
            managerId,
            ReviewCycle.SemiAnnual,
            DateTime.UtcNow.AddMonths(-6),
            DateTime.UtcNow,
            "H1 self-assessment");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);

        var savedReview = await performanceReviewRepository.GetByIdAsync(result.PerformanceReviewId!.Value);
        Assert.Equal(ReviewCycle.SemiAnnual, savedReview!.ReviewCycle);
        Assert.Equal("H1 self-assessment", savedReview.SelfAssessment);
    }

    [Fact]
    public async Task CreatePerformanceReview_ForInactiveEmployee_ShouldFail()
    {
        // Arrange
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);

        var managerId = Guid.NewGuid();
        var manager = new Employee
        {
            Id = managerId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "MGR004",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-3),
            CreatedDate = DateTime.UtcNow.AddYears(-3)
        };

        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP004",
            ManagerId = managerId,
            EmploymentStatus = EmploymentStatus.Terminated,
            StartDate = DateTime.UtcNow.AddYears(-2),
            TerminationDate = DateTime.UtcNow.AddMonths(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        Context.Employees.AddRange(manager, employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.GetEmployeeIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(managerId);
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(managerId);

        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();

        var handler = new CreatePerformanceReviewCommandHandler(
            performanceReviewRepository,
            employeeRepository,
            unitOfWork,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        var command = new CreatePerformanceReviewCommand(
            employeeId,
            managerId,
            ReviewCycle.Annual,
            DateTime.UtcNow.AddYears(-1),
            DateTime.UtcNow,
            null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Cannot create performance review for inactive employee", result.ErrorMessage);
    }

    [Fact]
    public async Task CreatePerformanceReview_MultipleReviewsForSameEmployee_ShouldSucceed()
    {
        // Arrange - Test that employee can have multiple reviews over time
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);

        var managerId = Guid.NewGuid();
        var manager = new Employee
        {
            Id = managerId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "MGR005",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-3),
            CreatedDate = DateTime.UtcNow.AddYears(-3)
        };

        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP005",
            ManagerId = managerId,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        Context.Employees.AddRange(manager, employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.GetEmployeeIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(managerId);
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(managerId);

        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();

        var handler = new CreatePerformanceReviewCommandHandler(
            performanceReviewRepository,
            employeeRepository,
            unitOfWork,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        // Act - Create three quarterly reviews
        var command1 = new CreatePerformanceReviewCommand(
            employeeId, managerId, ReviewCycle.Quarterly,
            DateTime.UtcNow.AddMonths(-9), DateTime.UtcNow.AddMonths(-6), "Q1 review");

        var command2 = new CreatePerformanceReviewCommand(
            employeeId, managerId, ReviewCycle.Quarterly,
            DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow.AddMonths(-3), "Q2 review");

        var command3 = new CreatePerformanceReviewCommand(
            employeeId, managerId, ReviewCycle.Quarterly,
            DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow, "Q3 review");

        var result1 = await handler.HandleAsync(command1);
        var result2 = await handler.HandleAsync(command2);
        var result3 = await handler.HandleAsync(command3);

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.True(result3.Success);

        var allReviews = await performanceReviewRepository.GetByEmployeeIdAsync(employeeId);
        Assert.Equal(3, allReviews.Count());
        Assert.All(allReviews, r => Assert.Equal(employeeId, r.EmployeeId));
        Assert.All(allReviews, r => Assert.Equal(ReviewCycle.Quarterly, r.ReviewCycle));
    }

    [Fact]
    public async Task CreatePerformanceReview_ShouldSetCreatedDateAndStatus()
    {
        // Arrange
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);

        var managerId = Guid.NewGuid();
        var manager = new Employee
        {
            Id = managerId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "MGR006",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP006",
            ManagerId = managerId,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        Context.Employees.AddRange(manager, employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.GetEmployeeIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(managerId);
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(managerId);

        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();

        var handler = new CreatePerformanceReviewCommandHandler(
            performanceReviewRepository,
            employeeRepository,
            unitOfWork,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        var command = new CreatePerformanceReviewCommand(
            employeeId, managerId, ReviewCycle.Annual,
            DateTime.UtcNow.AddYears(-1), DateTime.UtcNow, null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        var savedReview = await performanceReviewRepository.GetByIdAsync(result.PerformanceReviewId!.Value);
        Assert.True(Math.Abs((savedReview!.CreatedDate - DateTime.UtcNow).TotalSeconds) <= 5);
        Assert.Equal(managerId, savedReview.CreatedBy);
        Assert.Equal("Draft", savedReview.Status);
        Assert.Null(savedReview.AcknowledgedDate);
    }
}
