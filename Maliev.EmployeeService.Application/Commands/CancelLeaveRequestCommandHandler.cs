using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Handler for CancelLeaveRequestCommand
/// </summary>
public class CancelLeaveRequestCommandHandler
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ILeaveBalanceRepository _leaveBalanceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelLeaveRequestCommandHandler(
        ILeaveRequestRepository leaveRequestRepository,
        ILeaveBalanceRepository leaveBalanceRepository,
        IUnitOfWork unitOfWork)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _leaveBalanceRepository = leaveBalanceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CancelLeaveRequestCommandResult> HandleAsync(
        CancelLeaveRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(
            command.LeaveRequestId,
            cancellationToken);

        if (leaveRequest == null)
        {
            return new CancelLeaveRequestCommandResult(false, "Leave request not found");
        }

        // Verify ownership
        if (leaveRequest.EmployeeId != command.EmployeeId)
        {
            return new CancelLeaveRequestCommandResult(false, "You can only cancel your own leave requests");
        }

        // Check if can be cancelled
        if (!leaveRequest.CanBeCancelled)
        {
            return new CancelLeaveRequestCommandResult(false, $"Leave request cannot be cancelled (Status: {leaveRequest.Status})");
        }

        // Get leave balance
        var leaveBalance = await _leaveBalanceRepository.GetByEmployeeLeaveTypeAndYearAsync(
            leaveRequest.EmployeeId,
            leaveRequest.LeaveType,
            leaveRequest.StartDate.Year,
            cancellationToken);

        if (leaveBalance != null)
        {
            // If it was approved, reduce used days; if pending, reduce pending days
            if (leaveRequest.Status == LeaveRequestStatus.Approved)
            {
                leaveBalance.UsedDays -= leaveRequest.TotalDays;
            }
            else if (leaveRequest.Status == LeaveRequestStatus.Pending)
            {
                leaveBalance.PendingDays -= leaveRequest.TotalDays;
            }

            _leaveBalanceRepository.Update(leaveBalance);
        }

        // Update leave request status
        leaveRequest.Status = LeaveRequestStatus.Cancelled;
        leaveRequest.ModifiedDate = DateTime.UtcNow;

        _leaveRequestRepository.Update(leaveRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CancelLeaveRequestCommandResult(true);
    }
}
