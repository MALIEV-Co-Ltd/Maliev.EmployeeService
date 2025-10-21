using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to update goal progress and status
/// </summary>
public record UpdateGoalProgressCommand(
    Guid GoalId,
    Guid EmployeeId,
    GoalStatus CompletionStatus,
    string? ProgressUpdate = null);

/// <summary>
/// Response for goal progress update
/// </summary>
public record UpdateGoalProgressCommandResult(
    bool Success,
    string? ErrorMessage);

/// <summary>
/// Handler for UpdateGoalProgressCommand
/// </summary>
public class UpdateGoalProgressCommandHandler
{
    private readonly IGoalRepository _goalRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateGoalProgressCommandHandler(
        IGoalRepository goalRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _goalRepository = goalRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<UpdateGoalProgressCommandResult> HandleAsync(
        UpdateGoalProgressCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate goal exists
        var goal = await _goalRepository.GetByIdAsync(command.GoalId, cancellationToken);
        if (goal == null)
        {
            return new UpdateGoalProgressCommandResult(
                false,
                $"Goal with ID '{command.GoalId}' not found");
        }

        // Validate employee owns the goal
        if (goal.EmployeeId != command.EmployeeId)
        {
            return new UpdateGoalProgressCommandResult(
                false,
                "You can only update your own goals");
        }

        // Validate status transition
        if (goal.CompletionStatus == GoalStatus.Cancelled)
        {
            return new UpdateGoalProgressCommandResult(
                false,
                "Cannot update a cancelled goal");
        }

        // Validate not trying to move from Completed back to InProgress or NotStarted
        if (goal.CompletionStatus == GoalStatus.Completed &&
            command.CompletionStatus != GoalStatus.Completed)
        {
            return new UpdateGoalProgressCommandResult(
                false,
                "Cannot reopen a completed goal. Create a new goal instead.");
        }

        // Update goal status and progress
        goal.CompletionStatus = command.CompletionStatus;

        // Set completed date when marking as completed
        if (command.CompletionStatus == GoalStatus.Completed && !goal.CompletedDate.HasValue)
        {
            goal.CompletedDate = DateTime.UtcNow;
        }

        // Append progress update if provided
        if (!string.IsNullOrWhiteSpace(command.ProgressUpdate))
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var newUpdate = $"[{timestamp}] {command.ProgressUpdate}";

            if (string.IsNullOrWhiteSpace(goal.ProgressUpdates))
            {
                goal.ProgressUpdates = newUpdate;
            }
            else
            {
                goal.ProgressUpdates += $"\n{newUpdate}";
            }
        }

        goal.ModifiedBy = _currentUserService.EmployeeId;
        goal.ModifiedDate = DateTime.UtcNow;

        _goalRepository.Update(goal);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateGoalProgressCommandResult(true, null);
    }
}
