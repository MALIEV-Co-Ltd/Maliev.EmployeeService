using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Handler for SubmitLeaveRequestCommand
/// </summary>
public class SubmitLeaveRequestCommandHandler
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ILeaveBalanceRepository _leaveBalanceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitLeaveRequestCommandHandler(
        ILeaveRequestRepository leaveRequestRepository,
        ILeaveBalanceRepository leaveBalanceRepository,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _leaveBalanceRepository = leaveBalanceRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SubmitLeaveRequestCommandResult> HandleAsync(
        SubmitLeaveRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        // Verify employee exists
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return new SubmitLeaveRequestCommandResult(false, null, "Employee not found");
        }

        // Parse leave type
        if (!Enum.TryParse<LeaveType>(command.SubmitDto.LeaveType, out var leaveType))
        {
            return new SubmitLeaveRequestCommandResult(false, null, "Invalid leave type");
        }

        // Calculate total days
        var totalDays = (decimal)(command.SubmitDto.EndDate - command.SubmitDto.StartDate).TotalDays + 1;

        // Check for overlapping requests
        var overlappingRequests = await _leaveRequestRepository.GetOverlappingRequestsAsync(
            command.EmployeeId,
            command.SubmitDto.StartDate,
            command.SubmitDto.EndDate,
            cancellationToken);

        if (overlappingRequests.Any())
        {
            return new SubmitLeaveRequestCommandResult(false, null, "You have an overlapping leave request");
        }

        // Check leave balance
        var currentYear = command.SubmitDto.StartDate.Year;
        var leaveBalance = await _leaveBalanceRepository.GetByEmployeeLeaveTypeAndYearAsync(
            command.EmployeeId,
            leaveType,
            currentYear,
            cancellationToken);

        if (leaveBalance == null)
        {
            return new SubmitLeaveRequestCommandResult(false, null, $"No leave balance found for {leaveType} in {currentYear}");
        }

        if (leaveBalance.AvailableDays < totalDays)
        {
            return new SubmitLeaveRequestCommandResult(
                false,
                null,
                $"Insufficient leave balance. Available: {leaveBalance.AvailableDays} days, Requested: {totalDays} days");
        }

        // Create leave request
        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = command.EmployeeId,
            LeaveType = leaveType,
            StartDate = command.SubmitDto.StartDate,
            EndDate = command.SubmitDto.EndDate,
            TotalDays = totalDays,
            Reason = command.SubmitDto.Reason,
            Status = LeaveRequestStatus.Pending,
            CreatedDate = DateTime.UtcNow,
            ApproverId = employee.ManagerId // Manager will approve/deny
        };

        await _leaveRequestRepository.AddAsync(leaveRequest, cancellationToken);

        // Update pending days in leave balance
        leaveBalance.PendingDays += totalDays;
        _leaveBalanceRepository.Update(leaveBalance);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SubmitLeaveRequestCommandResult(true, leaveRequest.Id);
    }
}
