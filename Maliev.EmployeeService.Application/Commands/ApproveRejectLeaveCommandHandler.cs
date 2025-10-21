using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Handler for ApproveRejectLeaveCommand
/// </summary>
public class ApproveRejectLeaveCommandHandler
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ILeaveBalanceRepository _leaveBalanceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveRejectLeaveCommandHandler(
        ILeaveRequestRepository leaveRequestRepository,
        ILeaveBalanceRepository leaveBalanceRepository,
        IUnitOfWork unitOfWork)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _leaveBalanceRepository = leaveBalanceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApproveRejectLeaveCommandResult> HandleAsync(
        ApproveRejectLeaveCommand command,
        CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(
            command.LeaveRequestId,
            cancellationToken);

        if (leaveRequest == null)
        {
            return new ApproveRejectLeaveCommandResult(false, "Leave request not found");
        }

        // Verify approver
        if (leaveRequest.ApproverId != command.ApproverId)
        {
            return new ApproveRejectLeaveCommandResult(false, "You are not authorized to approve this request");
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            return new ApproveRejectLeaveCommandResult(false, "Leave request is not in a valid state for approval");
        }

        // Update leave request based on decision
        if (command.DecisionDto.IsApproved)
        {
            leaveRequest.Status = LeaveRequestStatus.Approved;
            leaveRequest.ApprovalDate = DateTime.UtcNow;
            leaveRequest.ApprovalComments = command.DecisionDto.Comments;

            // Update leave balance: move from pending to used
            var leaveBalance = await _leaveBalanceRepository.GetByEmployeeLeaveTypeAndYearAsync(
                leaveRequest.EmployeeId,
                leaveRequest.LeaveType,
                leaveRequest.StartDate.Year,
                cancellationToken);

            if (leaveBalance != null)
            {
                leaveBalance.PendingDays -= leaveRequest.TotalDays;
                leaveBalance.UsedDays += leaveRequest.TotalDays;
                _leaveBalanceRepository.Update(leaveBalance);
            }
        }
        else
        {
            // Denied
            leaveRequest.Status = LeaveRequestStatus.Denied;
            leaveRequest.ApprovalDate = DateTime.UtcNow;
            leaveRequest.ApprovalComments = command.DecisionDto.Comments;

            // Update leave balance: reduce pending days
            var leaveBalance = await _leaveBalanceRepository.GetByEmployeeLeaveTypeAndYearAsync(
                leaveRequest.EmployeeId,
                leaveRequest.LeaveType,
                leaveRequest.StartDate.Year,
                cancellationToken);

            if (leaveBalance != null)
            {
                leaveBalance.PendingDays -= leaveRequest.TotalDays;
                _leaveBalanceRepository.Update(leaveBalance);
            }
        }

        _leaveRequestRepository.Update(leaveRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ApproveRejectLeaveCommandResult(true);
    }
}
