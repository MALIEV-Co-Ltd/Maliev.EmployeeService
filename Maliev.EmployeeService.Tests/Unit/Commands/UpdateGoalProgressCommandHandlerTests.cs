using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Commands;

/// <summary>
/// Unit tests for UpdateGoalProgressCommandHandler
/// Tests goal status transitions, progress updates, and validation
/// </summary>
public class UpdateGoalProgressCommandHandlerTests
{
    private readonly Mock<IGoalRepository> _mockGoalRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly UpdateGoalProgressCommandHandler _handler;

    public UpdateGoalProgressCommandHandlerTests()
    {
        _mockGoalRepository = new Mock<IGoalRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _mockCurrentUserService.Setup(x => x.EmployeeId).Returns(Guid.NewGuid());

        _handler = new UpdateGoalProgressCommandHandler(
            _mockGoalRepository.Object,
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidProgressUpdate_ShouldUpdateGoal()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var goal = new Goal
        {
            Id = goalId,
            EmployeeId = employeeId,
            Description = "Complete project X",
            CompletionStatus = GoalStatus.InProgress,
            TargetDate = DateTime.UtcNow.AddMonths(3),
            CreatedDate = DateTime.UtcNow.AddMonths(-1)
        };

        var command = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.InProgress,
            "Made good progress this week - completed 60% of milestones");

        _mockGoalRepository.Setup(x => x.GetByIdAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);

        Assert.Contains("Made good progress this week", goal.ProgressUpdates);
        Assert.Contains("60% of milestones", goal.ProgressUpdates);

        _mockGoalRepository.Verify(x => x.Update(It.Is<Goal>(g =>
            g.Id == goalId &&
            g.CompletionStatus == GoalStatus.InProgress)),
            Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TransitionToCompleted_ShouldSetCompletedDate()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var goal = new Goal
        {
            Id = goalId,
            EmployeeId = employeeId,
            Description = "Complete certification",
            CompletionStatus = GoalStatus.InProgress,
            TargetDate = DateTime.UtcNow.AddMonths(1),
            CreatedDate = DateTime.UtcNow.AddMonths(-3)
        };

        var command = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.Completed,
            "Certification exam passed with 95% score");

        _mockGoalRepository.Setup(x => x.GetByIdAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(GoalStatus.Completed, goal.CompletionStatus);
        Assert.NotNull(goal.CompletedDate);
        Assert.True(Math.Abs((goal.CompletedDate!.Value - DateTime.UtcNow).TotalSeconds) <= 5);
        Assert.Contains("Certification exam passed", goal.ProgressUpdates);
    }

    [Fact]
    public async Task HandleAsync_TransitionFromNotStartedToInProgress_ShouldSucceed()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var goal = new Goal
        {
            Id = goalId,
            EmployeeId = employeeId,
            Description = "Launch new feature",
            CompletionStatus = GoalStatus.NotStarted,
            TargetDate = DateTime.UtcNow.AddMonths(6),
            CreatedDate = DateTime.UtcNow
        };

        var command = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.InProgress,
            "Started initial planning and requirements gathering");

        _mockGoalRepository.Setup(x => x.GetByIdAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(GoalStatus.InProgress, goal.CompletionStatus);
        Assert.Null(goal.CompletedDate);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentGoal_ShouldReturnError()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var goalId = Guid.NewGuid();

        var command = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.InProgress,
            "Progress update");

        _mockGoalRepository.Setup(x => x.GetByIdAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Goal?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Goal with ID", result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage);

        _mockGoalRepository.Verify(x => x.Update(It.IsAny<Goal>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithWrongEmployee_ShouldReturnError()
    {
        // Arrange
        var actualEmployeeId = Guid.NewGuid();
        var differentEmployeeId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var goal = new Goal
        {
            Id = goalId,
            EmployeeId = actualEmployeeId,
            Description = "Complete training",
            CompletionStatus = GoalStatus.InProgress,
            TargetDate = DateTime.UtcNow.AddMonths(2),
            CreatedDate = DateTime.UtcNow.AddMonths(-1)
        };

        var command = new UpdateGoalProgressCommand(
            goalId,
            differentEmployeeId,
            GoalStatus.Completed,
            "Trying to update someone else's goal");

        _mockGoalRepository.Setup(x => x.GetByIdAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("You can only update your own goals", result.ErrorMessage);

        _mockGoalRepository.Verify(x => x.Update(It.IsAny<Goal>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UpdateCancelledGoal_ShouldReturnError()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var goal = new Goal
        {
            Id = goalId,
            EmployeeId = employeeId,
            Description = "Cancelled goal",
            CompletionStatus = GoalStatus.Cancelled,
            TargetDate = DateTime.UtcNow.AddMonths(1),
            CreatedDate = DateTime.UtcNow.AddMonths(-2)
        };

        var command = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.InProgress,
            "Trying to reactivate cancelled goal");

        _mockGoalRepository.Setup(x => x.GetByIdAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Cannot update a cancelled goal", result.ErrorMessage);

        _mockGoalRepository.Verify(x => x.Update(It.IsAny<Goal>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ReopenCompletedGoal_ShouldReturnError()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var goal = new Goal
        {
            Id = goalId,
            EmployeeId = employeeId,
            Description = "Completed goal",
            CompletionStatus = GoalStatus.Completed,
            CompletedDate = DateTime.UtcNow.AddDays(-10),
            TargetDate = DateTime.UtcNow.AddMonths(-1),
            CreatedDate = DateTime.UtcNow.AddMonths(-3)
        };

        var command = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.InProgress,
            "Trying to reopen completed goal");

        _mockGoalRepository.Setup(x => x.GetByIdAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Cannot reopen a completed goal", result.ErrorMessage);
        Assert.Contains("Create a new goal instead", result.ErrorMessage);

        _mockGoalRepository.Verify(x => x.Update(It.IsAny<Goal>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithMultipleProgressUpdates_ShouldAppendCorrectly()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var goal = new Goal
        {
            Id = goalId,
            EmployeeId = employeeId,
            Description = "Complete project",
            CompletionStatus = GoalStatus.InProgress,
            ProgressUpdates = "[2025-01-01 10:00:00] First update: Started project planning",
            TargetDate = DateTime.UtcNow.AddMonths(3),
            CreatedDate = DateTime.UtcNow.AddMonths(-1)
        };

        var command = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.InProgress,
            "Second update: Completed design phase");

        _mockGoalRepository.Setup(x => x.GetByIdAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("First update: Started project planning", goal.ProgressUpdates);
        Assert.Contains("Second update: Completed design phase", goal.ProgressUpdates);
        Assert.Contains("\n", goal.ProgressUpdates); // Should have newline separator
    }

    [Fact]
    public async Task HandleAsync_WithNullProgressUpdate_ShouldUpdateStatusOnly()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var goal = new Goal
        {
            Id = goalId,
            EmployeeId = employeeId,
            Description = "Complete training",
            CompletionStatus = GoalStatus.NotStarted,
            TargetDate = DateTime.UtcNow.AddMonths(2),
            CreatedDate = DateTime.UtcNow
        };

        var command = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.InProgress,
            null);

        _mockGoalRepository.Setup(x => x.GetByIdAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(GoalStatus.InProgress, goal.CompletionStatus);
        Assert.True(string.IsNullOrEmpty(goal.ProgressUpdates));
    }

    [Fact]
    public async Task HandleAsync_ShouldSetModifiedByCurrentUser()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var goal = new Goal
        {
            Id = goalId,
            EmployeeId = employeeId,
            Description = "Goal description",
            CompletionStatus = GoalStatus.InProgress,
            TargetDate = DateTime.UtcNow.AddMonths(3),
            CreatedDate = DateTime.UtcNow.AddMonths(-1)
        };

        _mockCurrentUserService.Setup(x => x.EmployeeId).Returns(currentUserId);

        var command = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.Completed,
            "Goal completed successfully");

        _mockGoalRepository.Setup(x => x.GetByIdAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(currentUserId, goal.ModifiedBy);
        Assert.True(Math.Abs((goal.ModifiedDate!.Value - DateTime.UtcNow).TotalSeconds) <= 5);
    }

    [Fact]
    public async Task HandleAsync_KeepCompletedStatus_ShouldNotResetCompletedDate()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var originalCompletedDate = DateTime.UtcNow.AddDays(-5);
        var goal = new Goal
        {
            Id = goalId,
            EmployeeId = employeeId,
            Description = "Already completed goal",
            CompletionStatus = GoalStatus.Completed,
            CompletedDate = originalCompletedDate,
            TargetDate = DateTime.UtcNow.AddMonths(-1),
            CreatedDate = DateTime.UtcNow.AddMonths(-3)
        };

        var command = new UpdateGoalProgressCommand(
            goalId,
            employeeId,
            GoalStatus.Completed,
            "Adding additional notes");

        _mockGoalRepository.Setup(x => x.GetByIdAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(originalCompletedDate, goal.CompletedDate); // Should NOT change
        Assert.Contains("Adding additional notes", goal.ProgressUpdates);
    }
}
