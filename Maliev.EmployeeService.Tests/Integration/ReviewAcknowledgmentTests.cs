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
/// Integration test for employee acknowledging their performance review
/// Tests T257 - Employee acknowledges their own review
/// </summary>
public class ReviewAcknowledgmentTests : PostgreSqlIntegrationTestBase
{
    [Fact]
    public async Task AcknowledgePerformanceReview_AsEmployee_ShouldSucceed()
    {
        // Arrange - Create employee and their performance review
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var unitOfWork = new UnitOfWork(Context);

        var employeeId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        var manager = new Employee
        {
            Id = managerId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "MGR001",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Manager", LastName = "User" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "manager@company.com" },
            ManagerId = null,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var employee = new Employee
        {
            Id = employeeId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "test@company.com" },
            ManagerId = null,  // Manager not needed for this test
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var reviewId = Guid.NewGuid();
        var review = new PerformanceReview
        {
            Id = reviewId,
            EmployeeId = employeeId,
            ReviewerId = managerId,
            ReviewCycle = ReviewCycle.Annual,
            ReviewPeriodStart = DateTime.UtcNow.AddYears(-1),
            ReviewPeriodEnd = DateTime.UtcNow,
            Status = "Submitted",
            Rating = PerformanceRating.ExceedsExpectations,
            Feedback = "Great performance this year. Exceeded all key objectives.",
            ReviewDate = DateTime.UtcNow.AddDays(-2),
            CreatedDate = DateTime.UtcNow.AddDays(-5)
        };

        Context.Employees.Add(manager);
        Context.Employees.Add(employee);
        Context.PerformanceReviews.Add(review);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(employeeId);

        var handler = new AcknowledgePerformanceReviewCommandHandler(
            performanceReviewRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        var command = new AcknowledgePerformanceReviewCommand(reviewId, employeeId);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);

        // Verify in database
        var updatedReview = await performanceReviewRepository.GetByIdAsync(reviewId);
        Assert.NotNull(updatedReview);
        Assert.NotNull(updatedReview!.AcknowledgedDate);
        Assert.True(Math.Abs((updatedReview.AcknowledgedDate!.Value - DateTime.UtcNow).TotalSeconds) <= 5);
        Assert.Equal("Acknowledged", updatedReview.Status);
        Assert.Equal(employeeId, updatedReview.ModifiedBy);
        Assert.True(Math.Abs((updatedReview.ModifiedDate!.Value - DateTime.UtcNow).TotalSeconds) <= 5);
    }

    [Fact]
    public async Task AcknowledgePerformanceReview_ForNonExistentReview_ShouldFail()
    {
        // Arrange
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var employeeId = Guid.NewGuid();
        var nonExistentReviewId = Guid.NewGuid();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(employeeId);

        var handler = new AcknowledgePerformanceReviewCommandHandler(
            performanceReviewRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        var command = new AcknowledgePerformanceReviewCommand(nonExistentReviewId, employeeId);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task AcknowledgePerformanceReview_ForDifferentEmployee_ShouldFail()
    {
        // Arrange - Create review for Employee A, but Employee B tries to acknowledge it
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var unitOfWork = new UnitOfWork(Context);

        var employeeA = Guid.NewGuid();
        var employeeB = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        var manager = new Employee
        {
            Id = managerId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "MGR002",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Manager", LastName = "User" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "manager2@company.com" },
            ManagerId = null,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var employee = new Employee
        {
            Id = employeeA,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMPA",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "EmployeeA" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "testA@company.com" },
            ManagerId = null,  // Manager not needed for this test
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var reviewId = Guid.NewGuid();
        var review = new PerformanceReview
        {
            Id = reviewId,
            EmployeeId = employeeA,
            ReviewerId = managerId,
            ReviewCycle = ReviewCycle.Annual,
            ReviewPeriodStart = DateTime.UtcNow.AddYears(-1),
            ReviewPeriodEnd = DateTime.UtcNow,
            Status = "Submitted",
            Rating = PerformanceRating.MeetsExpectations,
            Feedback = "Good performance",
            ReviewDate = DateTime.UtcNow.AddDays(-1),
            CreatedDate = DateTime.UtcNow.AddDays(-3)
        };

        Context.Employees.Add(manager);
        Context.Employees.Add(employee);
        Context.PerformanceReviews.Add(review);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(employeeB);

        var handler = new AcknowledgePerformanceReviewCommandHandler(
            performanceReviewRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        var command = new AcknowledgePerformanceReviewCommand(reviewId, employeeB);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("You can only acknowledge your own performance reviews", result.ErrorMessage);

        // Verify review was not modified
        var unchangedReview = await performanceReviewRepository.GetByIdAsync(reviewId);
        Assert.Null(unchangedReview!.AcknowledgedDate);
        Assert.Equal("Submitted", unchangedReview.Status);
    }

    [Fact]
    public async Task AcknowledgePerformanceReview_AlreadyAcknowledged_ShouldFail()
    {
        // Arrange
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var employeeId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var originalAcknowledgedDate = DateTime.UtcNow.AddDays(-10);

        var manager = new Employee
        {
            Id = managerId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "MGR003",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Manager", LastName = "User" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "manager3@company.com" },
            ManagerId = null,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var employee = new Employee
        {
            Id = employeeId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP002",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee2" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "test2@company.com" },
            ManagerId = null,  // Manager not needed for this test
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var reviewId = Guid.NewGuid();
        var review = new PerformanceReview
        {
            Id = reviewId,
            EmployeeId = employeeId,
            ReviewerId = managerId,
            ReviewCycle = ReviewCycle.SemiAnnual,
            ReviewPeriodStart = DateTime.UtcNow.AddMonths(-6),
            ReviewPeriodEnd = DateTime.UtcNow.AddDays(-15),
            Status = "Acknowledged",
            Rating = PerformanceRating.ExceedsExpectations,
            Feedback = "Excellent work",
            ReviewDate = DateTime.UtcNow.AddDays(-14),
            AcknowledgedDate = originalAcknowledgedDate,
            CreatedDate = DateTime.UtcNow.AddDays(-20)
        };

        Context.Employees.Add(manager);
        Context.Employees.Add(employee);
        Context.PerformanceReviews.Add(review);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(employeeId);

        var handler = new AcknowledgePerformanceReviewCommandHandler(
            performanceReviewRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        var command = new AcknowledgePerformanceReviewCommand(reviewId, employeeId);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already acknowledged", result.ErrorMessage);

        // Verify acknowledgment date was not changed
        var unchangedReview = await performanceReviewRepository.GetByIdAsync(reviewId);
        Assert.True(Math.Abs((unchangedReview!.AcknowledgedDate!.Value - originalAcknowledgedDate).TotalMilliseconds) <= 1);
    }

    [Fact]
    public async Task AcknowledgePerformanceReview_DraftReview_ShouldFail()
    {
        // Arrange - Review is still in Draft status, not yet submitted by manager
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var unitOfWork = new UnitOfWork(Context);

        var employeeId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        var manager = new Employee
        {
            Id = managerId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "MGR004",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Manager", LastName = "User" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "manager4@company.com" },
            ManagerId = null,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var employee = new Employee
        {
            Id = employeeId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP003",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee3" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "test3@company.com" },
            ManagerId = null,  // Manager not needed for this test
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var reviewId = Guid.NewGuid();
        var review = new PerformanceReview
        {
            Id = reviewId,
            EmployeeId = employeeId,
            ReviewerId = managerId,
            ReviewCycle = ReviewCycle.Quarterly,
            ReviewPeriodStart = DateTime.UtcNow.AddMonths(-3),
            ReviewPeriodEnd = DateTime.UtcNow,
            Status = "Draft", // Still being worked on by manager
            CreatedDate = DateTime.UtcNow.AddDays(-2)
        };

        Context.Employees.Add(manager);
        Context.Employees.Add(employee);
        Context.PerformanceReviews.Add(review);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(employeeId);

        var handler = new AcknowledgePerformanceReviewCommandHandler(
            performanceReviewRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        var command = new AcknowledgePerformanceReviewCommand(reviewId, employeeId);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("draft performance review", result.ErrorMessage);
    }

    [Fact]
    public async Task AcknowledgePerformanceReview_MultipleEmployees_ShouldOnlyAcknowledgeOwn()
    {
        // Arrange - Two employees with reviews, each can only acknowledge their own
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var unitOfWork = new UnitOfWork(Context);

        var employee1Id = Guid.NewGuid();
        var employee2Id = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        var manager = new Employee
        {
            Id = managerId,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "MGR005",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Manager", LastName = "User" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "manager5@company.com" },
            ManagerId = null,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var employee1 = new Employee
        {
            Id = employee1Id,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee1" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "test1@company.com" },
            ManagerId = null,  // Manager not needed for this test
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var employee2 = new Employee
        {
            Id = employee2Id,
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP002",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee2" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "test2b@company.com" },
            ManagerId = null,  // Manager not needed for this test
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var review1Id = Guid.NewGuid();
        var review1 = new PerformanceReview
        {
            Id = review1Id,
            EmployeeId = employee1Id,
            ReviewerId = managerId,
            ReviewCycle = ReviewCycle.Annual,
            ReviewPeriodStart = DateTime.UtcNow.AddYears(-1),
            ReviewPeriodEnd = DateTime.UtcNow,
            Status = "Submitted",
            Rating = PerformanceRating.ExceedsExpectations,
            Feedback = "Great work",
            ReviewDate = DateTime.UtcNow.AddDays(-1),
            CreatedDate = DateTime.UtcNow.AddDays(-3)
        };

        var review2Id = Guid.NewGuid();
        var review2 = new PerformanceReview
        {
            Id = review2Id,
            EmployeeId = employee2Id,
            ReviewerId = managerId,
            ReviewCycle = ReviewCycle.Annual,
            ReviewPeriodStart = DateTime.UtcNow.AddYears(-1),
            ReviewPeriodEnd = DateTime.UtcNow,
            Status = "Submitted",
            Rating = PerformanceRating.MeetsExpectations,
            Feedback = "Good performance",
            ReviewDate = DateTime.UtcNow.AddDays(-1),
            CreatedDate = DateTime.UtcNow.AddDays(-3)
        };

        Context.Employees.AddRange(manager, employee1, employee2);
        Context.PerformanceReviews.AddRange(review1, review2);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(employee1Id);

        var handler = new AcknowledgePerformanceReviewCommandHandler(
            performanceReviewRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        // Act - Employee 1 acknowledges their review
        var command1 = new AcknowledgePerformanceReviewCommand(review1Id, employee1Id);
        var result1 = await handler.HandleAsync(command1);

        // Employee 1 tries to acknowledge Employee 2's review
        var command2 = new AcknowledgePerformanceReviewCommand(review2Id, employee1Id);
        var result2 = await handler.HandleAsync(command2);

        // Assert
        Assert.True(result1.Success);
        Assert.False(result2.Success);
        Assert.Contains("You can only acknowledge your own performance reviews", result2.ErrorMessage);

        // Verify only review1 was acknowledged
        var updatedReview1 = await performanceReviewRepository.GetByIdAsync(review1Id);
        var updatedReview2 = await performanceReviewRepository.GetByIdAsync(review2Id);

        Assert.NotNull(updatedReview1!.AcknowledgedDate);
        Assert.Equal("Acknowledged", updatedReview1.Status);

        Assert.Null(updatedReview2!.AcknowledgedDate);
        Assert.Equal("Submitted", updatedReview2.Status);
    }
}
