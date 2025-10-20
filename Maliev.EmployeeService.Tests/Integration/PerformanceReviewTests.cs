using FluentAssertions;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

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
            EmployeeNumber = "MGR001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-3),
            CreatedDate = DateTime.UtcNow.AddYears(-3)
        };

        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            ManagerId = managerId, // Direct report to manager
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        Context.Employees.AddRange(manager, employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(managerId);

        var handler = new CreatePerformanceReviewCommandHandler(
            performanceReviewRepository,
            employeeRepository,
            unitOfWork,
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
        result.Success.Should().BeTrue();
        result.PerformanceReviewId.Should().NotBeEmpty();
        result.ErrorMessage.Should().BeNull();

        // Verify in database
        var savedReview = await performanceReviewRepository.GetByIdAsync(result.PerformanceReviewId!.Value);
        savedReview.Should().NotBeNull();
        savedReview!.EmployeeId.Should().Be(employeeId);
        savedReview.ReviewerId.Should().Be(managerId);
        savedReview.ReviewCycle.Should().Be(ReviewCycle.Annual);
        savedReview.Status.Should().Be("Draft");
        savedReview.SelfAssessment.Should().Contain("Exceeded expectations");
        savedReview.ReviewPeriodStart.Should().BeCloseTo(DateTime.UtcNow.AddYears(-1), TimeSpan.FromHours(1));
        savedReview.ReviewPeriodEnd.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(1));
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
            EmployeeNumber = "MGR002",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP002",
            ManagerId = managerId,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        Context.Employees.AddRange(manager, employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(managerId);

        var handler = new CreatePerformanceReviewCommandHandler(
            performanceReviewRepository,
            employeeRepository,
            unitOfWork,
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
        result.Success.Should().BeTrue();

        var savedReview = await performanceReviewRepository.GetByIdAsync(result.PerformanceReviewId!.Value);
        savedReview!.ReviewCycle.Should().Be(ReviewCycle.Quarterly);
        savedReview.ReviewPeriodStart.Should().BeCloseTo(DateTime.UtcNow.AddMonths(-3), TimeSpan.FromHours(1));
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
            EmployeeNumber = "MGR003",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP003",
            ManagerId = managerId,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        Context.Employees.AddRange(manager, employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(managerId);

        var handler = new CreatePerformanceReviewCommandHandler(
            performanceReviewRepository,
            employeeRepository,
            unitOfWork,
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
        result.Success.Should().BeTrue();

        var savedReview = await performanceReviewRepository.GetByIdAsync(result.PerformanceReviewId!.Value);
        savedReview!.ReviewCycle.Should().Be(ReviewCycle.SemiAnnual);
        savedReview.SelfAssessment.Should().Be("H1 self-assessment");
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
            EmployeeNumber = "MGR004",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-3),
            CreatedDate = DateTime.UtcNow.AddYears(-3)
        };

        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
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
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(managerId);

        var handler = new CreatePerformanceReviewCommandHandler(
            performanceReviewRepository,
            employeeRepository,
            unitOfWork,
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
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cannot create performance review for inactive employee");
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
            EmployeeNumber = "MGR005",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-3),
            CreatedDate = DateTime.UtcNow.AddYears(-3)
        };

        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP005",
            ManagerId = managerId,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        Context.Employees.AddRange(manager, employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(managerId);

        var handler = new CreatePerformanceReviewCommandHandler(
            performanceReviewRepository,
            employeeRepository,
            unitOfWork,
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
        result1.Success.Should().BeTrue();
        result2.Success.Should().BeTrue();
        result3.Success.Should().BeTrue();

        var allReviews = await performanceReviewRepository.GetByEmployeeIdAsync(employeeId);
        allReviews.Should().HaveCount(3);
        allReviews.All(r => r.EmployeeId == employeeId).Should().BeTrue();
        allReviews.All(r => r.ReviewCycle == ReviewCycle.Quarterly).Should().BeTrue();
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
            EmployeeNumber = "MGR006",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP006",
            ManagerId = managerId,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        Context.Employees.AddRange(manager, employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(managerId);

        var handler = new CreatePerformanceReviewCommandHandler(
            performanceReviewRepository,
            employeeRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        var command = new CreatePerformanceReviewCommand(
            employeeId, managerId, ReviewCycle.Annual,
            DateTime.UtcNow.AddYears(-1), DateTime.UtcNow, null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        var savedReview = await performanceReviewRepository.GetByIdAsync(result.PerformanceReviewId!.Value);
        savedReview!.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        savedReview.CreatedBy.Should().Be(managerId);
        savedReview.Status.Should().Be("Draft");
        savedReview.AcknowledgedDate.Should().BeNull();
    }
}
