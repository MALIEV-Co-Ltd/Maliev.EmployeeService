using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// End-to-end tests for leave request workflow (T407)
/// Tests the complete leave request lifecycle: Submit → Approve/Reject → Balance Update
/// </summary>
public class LeaveRequestWorkflowE2ETests : PostgreSqlIntegrationTestBase
{
    [Fact]
    public async Task LeaveRequestWorkflow_SubmitApproveAndBalanceUpdate_ShouldCompleteSuccessfully()
    {
        // === PHASE 1: Setup - Create employee, manager, and initial leave balance ===
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var manager = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = $"MGR{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
            LegalName = new LegalName
            {
                FirstName = "Sarah",
                LastName = "Manager"
            },
            ContactInformation = new ContactInformation
            {
                WorkEmail = "sarah.manager@maliev.com"
            },
            EmploymentType = EmploymentType.FullTime,
            EmploymentStatus = EmploymentStatus.Active,
            JobTitle = "Engineering Manager",
            StartDate = DateTime.UtcNow.AddYears(-2).Date,
            DepartmentId = department.Id,
            CreatedDate = DateTime.UtcNow
        };

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = $"EMP{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
            LegalName = new LegalName
            {
                FirstName = "John",
                LastName = "Doe"
            },
            ContactInformation = new ContactInformation
            {
                WorkEmail = "john.doe@maliev.com"
            },
            EmploymentType = EmploymentType.FullTime,
            EmploymentStatus = EmploymentStatus.Active,
            JobTitle = "Software Engineer",
            StartDate = DateTime.UtcNow.Date,
            DepartmentId = department.Id,
            ManagerId = manager.Id,
            CreatedDate = DateTime.UtcNow
        };

        // Create initial leave balance for annual leave
        var currentYear = DateTime.UtcNow.Year;
        var leaveBalance = new LeaveBalance
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            LeaveType = LeaveType.AnnualLeave,
            Year = currentYear,
            TotalEntitlement = 15m, // 15 days annual leave
            UsedDays = 0m,
            PendingDays = 0m,
            CarryForwardDays = 0m
        };

        Context.Departments.Add(department);
        Context.Employees.Add(manager);
        Context.Employees.Add(employee);
        Context.LeaveBalances.Add(leaveBalance);
        await Context.SaveChangesAsync();

        // Verify initial setup
        var initialBalance = await Context.LeaveBalances.FirstOrDefaultAsync(lb => lb.Id == leaveBalance.Id);
        Assert.NotNull(initialBalance);
        Assert.Equal(15m, initialBalance!.AvailableDays);

        // === PHASE 2: Employee submits leave request ===
        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            LeaveType = LeaveType.AnnualLeave,
            StartDate = DateTime.UtcNow.AddDays(7).Date,
            EndDate = DateTime.UtcNow.AddDays(11).Date,
            TotalDays = 5m, // 5-day vacation
            Reason = "Family vacation",
            Status = LeaveRequestStatus.Pending
        };

        Context.LeaveRequests.Add(leaveRequest);

        // Update leave balance to reflect pending request
        leaveBalance.PendingDays = 5m;
        await Context.SaveChangesAsync();

        // Verify leave request created and balance shows pending
        var pendingRequest = await Context.LeaveRequests.FirstOrDefaultAsync(lr => lr.Id == leaveRequest.Id);
        Assert.NotNull(pendingRequest);
        Assert.Equal(LeaveRequestStatus.Pending, pendingRequest!.Status);
        Assert.True(pendingRequest.IsActive);

        var balanceWithPending = await Context.LeaveBalances.FirstOrDefaultAsync(lb => lb.Id == leaveBalance.Id);
        Assert.Equal(5m, balanceWithPending!.PendingDays);
        Assert.Equal(10m, balanceWithPending.AvailableDays); // 15 - 0 - 5 = 10

        // === PHASE 3: Manager approves leave request ===
        var leaveApproval = new LeaveApproval
        {
            Id = Guid.NewGuid(),
            LeaveRequestId = leaveRequest.Id,
            ApproverId = manager.Id,
            ApprovalLevel = 1,
            Decision = ApprovalDecision.Approved,
            DecisionDate = DateTime.UtcNow,
            Comments = "Approved - enjoy your vacation"
        };

        Context.LeaveApprovals.Add(leaveApproval);

        // Update leave request status
        leaveRequest.Status = LeaveRequestStatus.Approved;
        leaveRequest.ApproverId = manager.Id;
        leaveRequest.ApprovalDate = DateTime.UtcNow;
        leaveRequest.ApprovalComments = "Approved - enjoy your vacation";

        // Update leave balance: move from pending to used
        leaveBalance.PendingDays = 0m;
        leaveBalance.UsedDays = 5m;
        await Context.SaveChangesAsync();

        // Verify approval and balance update
        var approvedRequest = await Context.LeaveRequests.FirstOrDefaultAsync(lr => lr.Id == leaveRequest.Id);
        Assert.NotNull(approvedRequest);
        Assert.Equal(LeaveRequestStatus.Approved, approvedRequest!.Status);
        Assert.False(approvedRequest.IsActive);
        Assert.Equal(manager.Id, approvedRequest.ApproverId);

        var approvalRecord = await Context.LeaveApprovals.FirstOrDefaultAsync(la => la.LeaveRequestId == leaveRequest.Id);
        Assert.NotNull(approvalRecord);
        Assert.True(approvalRecord!.IsApproved);
        Assert.False(approvalRecord.IsPending);

        var finalBalance = await Context.LeaveBalances.FirstOrDefaultAsync(lb => lb.Id == leaveBalance.Id);
        Assert.Equal(5m, finalBalance!.UsedDays);
        Assert.Equal(0m, finalBalance.PendingDays);
        Assert.Equal(10m, finalBalance.AvailableDays); // 15 - 5 - 0 = 10
        Assert.Equal(10m, finalBalance.RemainingDays);

        // === PHASE 4: Verify complete workflow ===
        var allLeaveRequests = await Context.LeaveRequests
            .Where(lr => lr.EmployeeId == employee.Id)
            .ToListAsync();
        Assert.Single(allLeaveRequests);
        Assert.Equal(LeaveRequestStatus.Approved, allLeaveRequests.First().Status);

        var allApprovals = await Context.LeaveApprovals
            .Where(la => la.ApproverId == manager.Id)
            .ToListAsync();
        Assert.Single(allApprovals);
        Assert.Equal(ApprovalDecision.Approved, allApprovals.First().Decision);
    }

    [Fact]
    public async Task LeaveRequestWorkflow_SubmitAndReject_ShouldRestoreBalance()
    {
        // === PHASE 1: Setup ===
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Marketing",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var manager = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = $"MGR{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
            LegalName = new LegalName
            {
                FirstName = "Bob",
                LastName = "Manager"
            },
            ContactInformation = new ContactInformation
            {
                WorkEmail = "bob.manager@maliev.com"
            },
            EmploymentType = EmploymentType.FullTime,
            EmploymentStatus = EmploymentStatus.Active,
            JobTitle = "Marketing Manager",
            StartDate = DateTime.UtcNow.AddYears(-1).Date,
            DepartmentId = department.Id,
            CreatedDate = DateTime.UtcNow
        };

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = $"EMP{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
            LegalName = new LegalName
            {
                FirstName = "Alice",
                LastName = "Smith"
            },
            ContactInformation = new ContactInformation
            {
                WorkEmail = "alice.smith@maliev.com"
            },
            EmploymentType = EmploymentType.FullTime,
            EmploymentStatus = EmploymentStatus.Active,
            JobTitle = "Marketing Coordinator",
            StartDate = DateTime.UtcNow.Date,
            DepartmentId = department.Id,
            ManagerId = manager.Id,
            CreatedDate = DateTime.UtcNow
        };

        var currentYear = DateTime.UtcNow.Year;
        var leaveBalance = new LeaveBalance
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            LeaveType = LeaveType.SickLeave,
            Year = currentYear,
            TotalEntitlement = 10m,
            UsedDays = 2m,
            PendingDays = 0m,
            CarryForwardDays = 0m
        };

        Context.Departments.Add(department);
        Context.Employees.Add(manager);
        Context.Employees.Add(employee);
        Context.LeaveBalances.Add(leaveBalance);
        await Context.SaveChangesAsync();

        // === PHASE 2: Submit leave request ===
        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            LeaveType = LeaveType.SickLeave,
            StartDate = DateTime.UtcNow.AddDays(1).Date,
            EndDate = DateTime.UtcNow.AddDays(3).Date,
            TotalDays = 3m,
            Reason = "Medical appointment",
            Status = LeaveRequestStatus.Pending
        };

        Context.LeaveRequests.Add(leaveRequest);
        leaveBalance.PendingDays = 3m;
        await Context.SaveChangesAsync();

        // Verify pending state
        var balanceAfterSubmit = await Context.LeaveBalances.FirstOrDefaultAsync(lb => lb.Id == leaveBalance.Id);
        Assert.Equal(3m, balanceAfterSubmit!.PendingDays);
        Assert.Equal(5m, balanceAfterSubmit.AvailableDays); // 10 - 2 - 3 = 5

        // === PHASE 3: Manager rejects leave request ===
        var leaveApproval = new LeaveApproval
        {
            Id = Guid.NewGuid(),
            LeaveRequestId = leaveRequest.Id,
            ApproverId = manager.Id,
            ApprovalLevel = 1,
            Decision = ApprovalDecision.Rejected,
            DecisionDate = DateTime.UtcNow,
            Comments = "Insufficient notice - please submit earlier"
        };

        Context.LeaveApprovals.Add(leaveApproval);

        // Update leave request status to denied
        leaveRequest.Status = LeaveRequestStatus.Denied;
        leaveRequest.ApproverId = manager.Id;
        leaveRequest.ApprovalDate = DateTime.UtcNow;
        leaveRequest.ApprovalComments = "Insufficient notice - please submit earlier";

        // Restore balance: remove pending days
        leaveBalance.PendingDays = 0m;
        await Context.SaveChangesAsync();

        // === PHASE 4: Verify denial and balance restoration ===
        var deniedRequest = await Context.LeaveRequests.FirstOrDefaultAsync(lr => lr.Id == leaveRequest.Id);
        Assert.NotNull(deniedRequest);
        Assert.Equal(LeaveRequestStatus.Denied, deniedRequest!.Status);
        Assert.False(deniedRequest.IsActive);

        var denialRecord = await Context.LeaveApprovals.FirstOrDefaultAsync(la => la.LeaveRequestId == leaveRequest.Id);
        Assert.NotNull(denialRecord);
        Assert.True(denialRecord!.IsRejected);
        Assert.Equal(ApprovalDecision.Rejected, denialRecord.Decision);

        var restoredBalance = await Context.LeaveBalances.FirstOrDefaultAsync(lb => lb.Id == leaveBalance.Id);
        Assert.Equal(0m, restoredBalance!.PendingDays);
        Assert.Equal(2m, restoredBalance.UsedDays); // Unchanged
        Assert.Equal(8m, restoredBalance.AvailableDays); // 10 - 2 - 0 = 8
    }

    [Fact]
    public async Task LeaveRequestWorkflow_CancelPendingRequest_ShouldRestoreBalance()
    {
        // === PHASE 1: Setup ===
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Sales",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var manager = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = $"MGR{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
            LegalName = new LegalName
            {
                FirstName = "Manager",
                LastName = "User"
            },
            ContactInformation = new ContactInformation
            {
                WorkEmail = "manager@maliev.com"
            },
            EmploymentType = EmploymentType.FullTime,
            EmploymentStatus = EmploymentStatus.Active,
            JobTitle = "Sales Manager",
            StartDate = DateTime.UtcNow.AddYears(-1).Date,
            DepartmentId = department.Id,
            CreatedDate = DateTime.UtcNow
        };

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = $"EMP{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
            LegalName = new LegalName
            {
                FirstName = "Test",
                LastName = "Employee"
            },
            ContactInformation = new ContactInformation
            {
                WorkEmail = "test.employee@maliev.com"
            },
            EmploymentType = EmploymentType.FullTime,
            EmploymentStatus = EmploymentStatus.Active,
            JobTitle = "Sales Representative",
            StartDate = DateTime.UtcNow.Date,
            DepartmentId = department.Id,
            ManagerId = manager.Id,
            CreatedDate = DateTime.UtcNow
        };

        var currentYear = DateTime.UtcNow.Year;
        var leaveBalance = new LeaveBalance
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            LeaveType = LeaveType.AnnualLeave,
            Year = currentYear,
            TotalEntitlement = 20m,
            UsedDays = 5m,
            PendingDays = 0m,
            CarryForwardDays = 0m
        };

        Context.Departments.Add(department);
        Context.Employees.Add(manager);
        Context.Employees.Add(employee);
        Context.LeaveBalances.Add(leaveBalance);
        await Context.SaveChangesAsync();

        // === PHASE 2: Submit leave request ===
        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            LeaveType = LeaveType.AnnualLeave,
            StartDate = DateTime.UtcNow.AddDays(14).Date,
            EndDate = DateTime.UtcNow.AddDays(16).Date,
            TotalDays = 3m,
            Reason = "Personal travel",
            Status = LeaveRequestStatus.Pending
        };

        Context.LeaveRequests.Add(leaveRequest);
        leaveBalance.PendingDays = 3m;
        await Context.SaveChangesAsync();

        // Verify pending state
        Assert.True(leaveRequest.CanBeCancelled);
        var balanceBeforeCancel = await Context.LeaveBalances.FirstOrDefaultAsync(lb => lb.Id == leaveBalance.Id);
        Assert.Equal(3m, balanceBeforeCancel!.PendingDays);

        // === PHASE 3: Employee cancels the leave request ===
        leaveRequest.Status = LeaveRequestStatus.Cancelled;
        leaveBalance.PendingDays = 0m;
        await Context.SaveChangesAsync();

        // === PHASE 4: Verify cancellation and balance restoration ===
        var cancelledRequest = await Context.LeaveRequests.FirstOrDefaultAsync(lr => lr.Id == leaveRequest.Id);
        Assert.NotNull(cancelledRequest);
        Assert.Equal(LeaveRequestStatus.Cancelled, cancelledRequest!.Status);
        Assert.False(cancelledRequest.IsActive);
        Assert.False(cancelledRequest.CanBeCancelled);

        var restoredBalance = await Context.LeaveBalances.FirstOrDefaultAsync(lb => lb.Id == leaveBalance.Id);
        Assert.Equal(0m, restoredBalance!.PendingDays);
        Assert.Equal(5m, restoredBalance.UsedDays);
        Assert.Equal(15m, restoredBalance.AvailableDays); // 20 - 5 - 0 = 15
    }
}
