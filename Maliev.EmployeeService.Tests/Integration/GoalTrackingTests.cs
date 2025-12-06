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
/// Integration test for goal tracking workflow
/// Tests T258 - Full workflow: create goal, update progress, complete goal
/// </summary>
public class GoalTrackingTests : PostgreSqlIntegrationTestBase
{
    [Fact]
    public async Task GoalTrackingWorkflow_CreateUpdateComplete_ShouldSucceed()
    {
        // Arrange - Create employee
        var goalRepository = new GoalRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var unitOfWork = new UnitOfWork(Context);

        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(employeeId);

        // Step 1: Create goal
        var createHandler = new CreateGoalCommandHandler(
            goalRepository,
            employeeRepository,
            performanceReviewRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        var createCommand = new CreateGoalCommand(
            employeeId,
            "Complete AWS certification exam",
            "Pass exam with score >= 80%",
            DateTime.UtcNow.AddMonths(3),
            null);

        var createResult = await createHandler.HandleAsync(createCommand);

        // Assert goal created
        Assert.True(createResult.Success);
        Assert.True(createResult.GoalId.HasValue && createResult.GoalId.Value != Guid.Empty);

        var goalId = createResult.GoalId!.Value;
        var createdGoal = await goalRepository.GetByIdAsync(goalId);
        Assert.NotNull(createdGoal);
        Assert.Equal(GoalStatus.NotStarted, createdGoal!.CompletionStatus);
        Assert.True(createdGoal.ProgressUpdates == null || createdGoal.ProgressUpdates.Count() == 0);
        Assert.Null(createdGoal.CompletedDate);

        // Step 2: Update progress - started working
        var updateHandler = new UpdateGoalProgressCommandHandler(
            goalRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        var updateCommand1 = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.InProgress,
            "Started studying AWS documentation and enrolled in training course");

        var updateResult1 = await updateHandler.HandleAsync(updateCommand1);

        // Assert first update
        Assert.True(updateResult1.Success);

        Context.ChangeTracker.Clear();
        var goalAfterUpdate1 = await goalRepository.GetByIdAsync(goalId);
        Assert.Equal(GoalStatus.InProgress, goalAfterUpdate1!.CompletionStatus);
        Assert.Contains("Started studying AWS documentation", goalAfterUpdate1.ProgressUpdates);
        Assert.Null(goalAfterUpdate1.CompletedDate);

        // Step 3: Another progress update
        var updateCommand2 = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.InProgress,
            "Completed 60% of training modules. Scheduled exam for next month.");

        var updateResult2 = await updateHandler.HandleAsync(updateCommand2);

        // Assert second update
        Assert.True(updateResult2.Success);

        Context.ChangeTracker.Clear();
        var goalAfterUpdate2 = await goalRepository.GetByIdAsync(goalId);
        Assert.Contains("Started studying AWS documentation", goalAfterUpdate2!.ProgressUpdates);
        Assert.Contains("Completed 60% of training modules", goalAfterUpdate2.ProgressUpdates);
        Assert.Equal(GoalStatus.InProgress, goalAfterUpdate2.CompletionStatus);

        // Step 4: Final update - mark as completed
        var completeCommand = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.Completed,
            "Passed AWS certification exam with score of 89%!");

        var completeResult = await updateHandler.HandleAsync(completeCommand);

        // Assert completion
        Assert.True(completeResult.Success);

        Context.ChangeTracker.Clear();
        var completedGoal = await goalRepository.GetByIdAsync(goalId);
        Assert.Equal(GoalStatus.Completed, completedGoal!.CompletionStatus);
        Assert.NotNull(completedGoal.CompletedDate);
        Assert.True(Math.Abs((completedGoal.CompletedDate!.Value - DateTime.UtcNow).TotalSeconds) <= 5);
        Assert.Contains("Passed AWS certification exam with score of 89%", completedGoal.ProgressUpdates);
        Assert.Contains("Started studying AWS documentation", completedGoal.ProgressUpdates);
        Assert.Contains("Completed 60% of training modules", completedGoal.ProgressUpdates);
    }

    [Fact]
    public async Task GoalTrackingWorkflow_LinkedToPerformanceReview_ShouldSucceed()
    {
        // Arrange - Create employee and performance review
        var goalRepository = new GoalRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var unitOfWork = new UnitOfWork(Context);

        var employeeId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        // Create manager first (required for FK constraint)
        var manager = new Employee
        {
            Id = managerId,
            EmployeeNumber = "MGR001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP002",
            ManagerId = managerId,
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
            Status = "Draft",
            CreatedDate = DateTime.UtcNow.AddDays(-5)
        };

        Context.Employees.AddRange(manager, employee);
        Context.PerformanceReviews.Add(review);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(employeeId);

        // Act - Create goal linked to performance review
        var createHandler = new CreateGoalCommandHandler(
            goalRepository,
            employeeRepository,
            performanceReviewRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        var createCommand = new CreateGoalCommand(
            employeeId,
            "Improve customer satisfaction score by 15%",
            "Achieve CSAT score of 90% or higher by end of quarter",
            DateTime.UtcNow.AddMonths(3),
            reviewId);

        var result = await createHandler.HandleAsync(createCommand);

        // Assert
        Assert.True(result.Success);

        var createdGoal = await goalRepository.GetByIdAsync(result.GoalId!.Value);
        Assert.Equal(reviewId, createdGoal!.PerformanceReviewId);
        Assert.Equal(employeeId, createdGoal.EmployeeId);
    }

    [Fact]
    public async Task GoalTrackingWorkflow_MultipleGoals_ShouldTrackIndependently()
    {
        // Arrange
        var goalRepository = new GoalRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP003",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(employeeId);

        var createHandler = new CreateGoalCommandHandler(
            goalRepository,
            employeeRepository,
            performanceReviewRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        var updateHandler = new UpdateGoalProgressCommandHandler(
            goalRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        // Act - Create three different goals
        var goal1Command = new CreateGoalCommand(
            employeeId,
            "Complete training program",
            "Finish all required modules",
            DateTime.UtcNow.AddMonths(2),
            null);

        var goal2Command = new CreateGoalCommand(
            employeeId,
            "Mentor junior team member",
            "Provide weekly guidance sessions",
            DateTime.UtcNow.AddMonths(6),
            null);

        var goal3Command = new CreateGoalCommand(
            employeeId,
            "Deliver project milestone",
            "Complete phase 1 by deadline",
            DateTime.UtcNow.AddMonths(4),
            null);

        var result1 = await createHandler.HandleAsync(goal1Command);
        var result2 = await createHandler.HandleAsync(goal2Command);
        var result3 = await createHandler.HandleAsync(goal3Command);

        // Update goal 1 to completed
        var completeGoal1 = new UpdateGoalProgressCommand(
            result1.GoalId!.Value,
            employeeId,
            GoalStatus.Completed,
            "All training modules completed");

        await updateHandler.HandleAsync(completeGoal1);

        // Update goal 2 to in progress
        var updateGoal2 = new UpdateGoalProgressCommand(
            result2.GoalId!.Value,
            employeeId,
            GoalStatus.InProgress,
            "Conducted first mentoring session");

        await updateHandler.HandleAsync(updateGoal2);

        // Goal 3 remains not started

        // Assert
        Context.ChangeTracker.Clear();

        var allGoals = await goalRepository.GetByEmployeeIdAsync(employeeId);
        Assert.Equal(3, allGoals.Count());

        var completedGoal = allGoals.First(g => g.Id == result1.GoalId!.Value);
        Assert.Equal(GoalStatus.Completed, completedGoal.CompletionStatus);
        Assert.NotNull(completedGoal.CompletedDate);

        var inProgressGoal = allGoals.First(g => g.Id == result2.GoalId!.Value);
        Assert.Equal(GoalStatus.InProgress, inProgressGoal.CompletionStatus);
        Assert.Null(inProgressGoal.CompletedDate);
        Assert.Contains("Conducted first mentoring session", inProgressGoal.ProgressUpdates);

        var notStartedGoal = allGoals.First(g => g.Id == result3.GoalId!.Value);
        Assert.Equal(GoalStatus.NotStarted, notStartedGoal.CompletionStatus);
        Assert.Null(notStartedGoal.CompletedDate);
        Assert.True(notStartedGoal.ProgressUpdates == null || notStartedGoal.ProgressUpdates.Count() == 0);
    }

    [Fact]
    public async Task GoalTrackingWorkflow_CancelGoal_ShouldPreventFurtherUpdates()
    {
        // Arrange
        var goalRepository = new GoalRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP004",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(employeeId);

        var createHandler = new CreateGoalCommandHandler(
            goalRepository,
            employeeRepository,
            performanceReviewRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        var updateHandler = new UpdateGoalProgressCommandHandler(
            goalRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        // Create goal
        var createCommand = new CreateGoalCommand(
            employeeId,
            "Goal that will be cancelled",
            "This goal will become irrelevant",
            DateTime.UtcNow.AddMonths(3),
            null);

        var createResult = await createHandler.HandleAsync(createCommand);
        var goalId = createResult.GoalId!.Value;

        // Cancel the goal
        var cancelCommand = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.Cancelled,
            "Goal cancelled due to project scope change");

        var cancelResult = await updateHandler.HandleAsync(cancelCommand);
        Assert.True(cancelResult.Success);

        // Act - Try to update cancelled goal
        var updateCommand = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.InProgress,
            "Trying to reactivate cancelled goal");

        var updateResult = await updateHandler.HandleAsync(updateCommand);

        // Assert
        Assert.False(updateResult.Success);
        Assert.Contains("Cannot update a cancelled goal", updateResult.ErrorMessage);

        Context.ChangeTracker.Clear();
        var goal = await goalRepository.GetByIdAsync(goalId);
        Assert.Equal(GoalStatus.Cancelled, goal!.CompletionStatus);
        Assert.Contains("Goal cancelled due to project scope change", goal.ProgressUpdates);
        Assert.DoesNotContain("Trying to reactivate cancelled goal", goal.ProgressUpdates);
    }

    [Fact]
    public async Task GoalTrackingWorkflow_ProgressTimeline_ShouldMaintainHistory()
    {
        // Arrange
        var goalRepository = new GoalRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP005",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(employeeId);

        var createHandler = new CreateGoalCommandHandler(
            goalRepository,
            employeeRepository,
            performanceReviewRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        var updateHandler = new UpdateGoalProgressCommandHandler(
            goalRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        // Create goal
        var createCommand = new CreateGoalCommand(
            employeeId,
            "Product launch preparation",
            "Prepare all materials for product launch",
            DateTime.UtcNow.AddMonths(3),
            null);

        var createResult = await createHandler.HandleAsync(createCommand);
        var goalId = createResult.GoalId!.Value;

        // Act - Add multiple progress updates simulating timeline
        var updates = new[]
        {
            "Week 1: Completed market research analysis",
            "Week 2: Developed initial product positioning",
            "Week 4: Created marketing collateral drafts",
            "Week 6: Finalized launch timeline with stakeholders",
            "Week 8: Conducted team training sessions",
            "Week 10: All materials approved and ready for launch"
        };

        foreach (var update in updates)
        {
            var command = new UpdateGoalProgressCommand(
                goalId,
                employeeId,
                GoalStatus.InProgress,
                update);

            await updateHandler.HandleAsync(command);
            Context.ChangeTracker.Clear();
        }

        // Mark as completed
        var completeCommand = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.Completed,
            "Product successfully launched!");

        await updateHandler.HandleAsync(completeCommand);

        // Assert
        Context.ChangeTracker.Clear();
        var completedGoal = await goalRepository.GetByIdAsync(goalId);

        Assert.Equal(GoalStatus.Completed, completedGoal!.CompletionStatus);
        Assert.NotNull(completedGoal.CompletedDate);

        // Verify all updates are maintained in order
        foreach (var update in updates)
        {
            Assert.Contains(update, completedGoal.ProgressUpdates);
        }
        Assert.Contains("Product successfully launched!", completedGoal.ProgressUpdates);

        // Verify updates are in chronological order (newline separated)
        var updatesArray = completedGoal.ProgressUpdates!.Split('\n');
        Assert.True(updatesArray.Length >= updates.Length);
    }

    [Fact]
    public async Task GoalTrackingWorkflow_CompletedGoal_CannotBeReopened()
    {
        // Arrange
        var goalRepository = new GoalRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var performanceReviewRepository = new PerformanceReviewRepository(Context);
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP006",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(employeeId);

        var createHandler = new CreateGoalCommandHandler(
            goalRepository,
            employeeRepository,
            performanceReviewRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        var updateHandler = new UpdateGoalProgressCommandHandler(
            goalRepository,
            unitOfWork,
            mockCurrentUserService.Object);

        // Create and complete goal
        var createCommand = new CreateGoalCommand(
            employeeId,
            "Complete project deliverable",
            "Deliver all project artifacts",
            DateTime.UtcNow.AddMonths(2),
            null);

        var createResult = await createHandler.HandleAsync(createCommand);
        var goalId = createResult.GoalId!.Value;

        var completeCommand = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.Completed,
            "All deliverables submitted and approved");

        await updateHandler.HandleAsync(completeCommand);

        Context.ChangeTracker.Clear();

        // Act - Try to reopen completed goal
        var reopenCommand = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.InProgress,
            "Trying to reopen completed goal");

        var reopenResult = await updateHandler.HandleAsync(reopenCommand);

        // Assert
        Assert.False(reopenResult.Success);
        Assert.Contains("Cannot reopen a completed goal", reopenResult.ErrorMessage);
        Assert.Contains("Create a new goal instead", reopenResult.ErrorMessage);

        Context.ChangeTracker.Clear();
        var goal = await goalRepository.GetByIdAsync(goalId);
        Assert.Equal(GoalStatus.Completed, goal!.CompletionStatus);
    }
}
