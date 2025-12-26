using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// End-to-end tests for complete employee lifecycle (T406)
/// Tests the full employee journey: Onboarding → Active → Offboarding
/// Combines OnboardingWorkflowTests and OffboardingWorkflowTests into comprehensive E2E scenarios
/// </summary>
public class EmployeeLifecycleE2ETests : PostgreSqlIntegrationTestBase
{
    [Fact]
    public async Task EmployeeLifecycle_FromOnboardingToOffboarding_ShouldCompleteSuccessfully()
    {
        // === PHASE 1: Employee Creation ===
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        // Create manager for performance reviews
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
            PreferredName = "Johnny",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            ContactInformation = new ContactInformation
            {
                WorkEmail = "john.doe@maliev.com",
                PersonalEmail = "john.personal@gmail.com"
            },
            EmploymentType = EmploymentType.FullTime,
            EmploymentStatus = EmploymentStatus.Active,
            JobTitle = "Software Engineer",
            StartDate = DateTime.UtcNow.Date,
            DepartmentId = department.Id,
            ManagerId = manager.Id,
            Department = department,
            CreatedDate = DateTime.UtcNow
        };

        Context.Departments.Add(department);
        Context.Employees.Add(manager);
        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        // Verify employee created
        var createdEmployee = await Context.Employees.FindAsync(employee.Id);
        Assert.NotNull(createdEmployee);
        Assert.Equal("John Doe", createdEmployee!.FullName);
        Assert.True(createdEmployee.IsActive);

        // === PHASE 2: Onboarding Process ===
        var onboardingTasks = new List<OnboardingChecklist>
        {
            new OnboardingChecklist
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                ItemDescription = "Complete I-9 Form",
                DueDate = DateTime.UtcNow.AddDays(3),
                CompletionStatus = false,
                ResponsibleParty = ResponsibleParty.HR
            },
            new OnboardingChecklist
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                ItemDescription = "Set up workstation and laptop",
                DueDate = DateTime.UtcNow.AddDays(1),
                CompletionStatus = false,
                ResponsibleParty = ResponsibleParty.IT
            },
            new OnboardingChecklist
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                ItemDescription = "Complete orientation training",
                DueDate = DateTime.UtcNow.AddDays(5),
                CompletionStatus = false,
                ResponsibleParty = ResponsibleParty.HR
            }
        };

        Context.OnboardingChecklists.AddRange(onboardingTasks);
        await Context.SaveChangesAsync();

        // Verify onboarding tasks created
        var pendingOnboarding = await Context.OnboardingChecklists
            .Where(t => t.EmployeeId == employee.Id)
            .ToListAsync();
        Assert.Equal(3, pendingOnboarding.Count());
        Assert.All(pendingOnboarding, t => Assert.False(t.CompletionStatus));

        // Complete onboarding tasks
        foreach (var task in onboardingTasks)
        {
            task.CompletionStatus = true;
            task.CompletedDate = DateTime.UtcNow;
        }
        await Context.SaveChangesAsync();

        var completedOnboarding = await Context.OnboardingChecklists
            .Where(t => t.EmployeeId == employee.Id && t.CompletionStatus)
            .CountAsync();
        Assert.Equal(3, completedOnboarding);

        // === PHASE 3: Active Employment Period ===
        // Add emergency contact
        var emergencyContact = new EmergencyContact
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            ContactName = "Jane Doe",
            Relationship = "Spouse",
            PhoneNumber = "+66887654321",
            Email = "jane.doe@personal.com",
            PriorityOrder = 1
        };
        Context.EmergencyContacts.Add(emergencyContact);

        // Add work authorization
        var workAuth = new WorkAuthorization
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            AuthorizationType = AuthorizationType.Citizenship,
            IssuingAuthority = "Thailand Department of Employment",
            DocumentNumber = "TH123456789",
            IssueDate = DateTime.UtcNow.AddYears(-5),
            ExpirationDate = null, // Citizens don't expire
            SponsorshipStatus = SponsorshipStatus.NotRequired,
            IsActive = true
        };
        Context.WorkAuthorizations.Add(workAuth);

        // Add performance review
        var performanceReview = new PerformanceReview
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            ReviewPeriodStart = DateTime.UtcNow.AddMonths(-6),
            ReviewPeriodEnd = DateTime.UtcNow,
            ReviewDate = DateTime.UtcNow,
            ReviewerId = manager.Id,
            Rating = PerformanceRating.MeetsExpectations,
            Feedback = "Strong technical skills, good team collaboration",
            ReviewCycle = ReviewCycle.SemiAnnual,
            Status = "Submitted"
        };
        Context.PerformanceReviews.Add(performanceReview);

        // Add training record
        var training = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            CourseName = "Security Awareness Training",
            TrainingType = TrainingType.Mandatory,
            Provider = "Internal IT Security",
            CompletionDate = DateTime.UtcNow.AddDays(-29),
            Status = CertificationStatus.Valid
        };
        Context.TrainingRecords.Add(training);

        await Context.SaveChangesAsync();

        // Verify active employment data (query each separately as navigation properties don't all exist)
        var activeEmployee = await Context.Employees.FirstOrDefaultAsync(e => e.Id == employee.Id);
        Assert.NotNull(activeEmployee);

        var emergencyContactsCount = await Context.EmergencyContacts.CountAsync(ec => ec.EmployeeId == employee.Id);
        var workAuthCount = await Context.WorkAuthorizations.CountAsync(wa => wa.EmployeeId == employee.Id);
        var performanceReviewCount = await Context.PerformanceReviews.CountAsync(pr => pr.EmployeeId == employee.Id);
        var trainingRecordCount = await Context.TrainingRecords.CountAsync(tr => tr.EmployeeId == employee.Id);

        Assert.Equal(1, emergencyContactsCount);
        Assert.Equal(1, workAuthCount);
        Assert.Equal(1, performanceReviewCount);
        Assert.Equal(1, trainingRecordCount);

        // === PHASE 4: Offboarding Initiation ===
        employee.EmploymentStatus = EmploymentStatus.Terminated;
        employee.TerminationDate = DateTime.UtcNow;
        await Context.SaveChangesAsync();

        // Create offboarding checklist
        var offboardingTasks = new List<OffboardingChecklist>
        {
            new OffboardingChecklist
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                ItemDescription = "Return company equipment",
                DueDate = DateTime.UtcNow.AddDays(2),
                CompletionStatus = false,
                ResponsibleParty = ResponsibleParty.IT,
                CreatedDate = DateTime.UtcNow
            },
            new OffboardingChecklist
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                ItemDescription = "Exit interview",
                DueDate = DateTime.UtcNow.AddDays(1),
                CompletionStatus = false,
                ResponsibleParty = ResponsibleParty.HR,
                CreatedDate = DateTime.UtcNow
            },
            new OffboardingChecklist
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                ItemDescription = "Revoke system access",
                DueDate = DateTime.UtcNow.AddDays(1),
                CompletionStatus = false,
                ResponsibleParty = ResponsibleParty.IT,
                CreatedDate = DateTime.UtcNow
            }
        };

        Context.OffboardingChecklists.AddRange(offboardingTasks);
        await Context.SaveChangesAsync();

        // Verify offboarding initiated
        var terminatedEmployee = await Context.Employees
            .FirstOrDefaultAsync(e => e.Id == employee.Id);
        Assert.NotNull(terminatedEmployee);
        Assert.Equal(EmploymentStatus.Terminated, terminatedEmployee!.EmploymentStatus);
        Assert.NotNull(terminatedEmployee.TerminationDate);

        var pendingOffboarding = await Context.OffboardingChecklists
            .Where(t => t.EmployeeId == employee.Id)
            .ToListAsync();
        Assert.Equal(3, pendingOffboarding.Count());

        // === PHASE 5: Complete Offboarding ===
        foreach (var task in offboardingTasks)
        {
            task.CompletionStatus = true;
            task.CompletedDate = DateTime.UtcNow;
        }
        await Context.SaveChangesAsync();

        // Conduct exit interview
        var exitInterview = new ExitInterview
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            InterviewDate = DateTime.UtcNow,
            ConductedBy = manager.Id,
            CreatedAt = DateTime.UtcNow
        };
        Context.ExitInterviews.Add(exitInterview);
        await Context.SaveChangesAsync();

        // === PHASE 6: Verification - Complete Lifecycle ===
        var finalEmployee = await Context.Employees.FirstOrDefaultAsync(e => e.Id == employee.Id);
        Assert.NotNull(finalEmployee);

        // Onboarding completed
        var completedOnboardingTasks = await Context.OnboardingChecklists
            .Where(t => t.EmployeeId == employee.Id)
            .ToListAsync();
        Assert.Equal(3, completedOnboardingTasks.Count());
        Assert.All(completedOnboardingTasks, t => Assert.True(t.CompletionStatus));
        Assert.All(completedOnboardingTasks, t => Assert.NotNull(t.CompletedDate));

        // Active employment period documented
        var finalEmergencyContactsCount = await Context.EmergencyContacts.CountAsync(ec => ec.EmployeeId == employee.Id);
        var finalWorkAuthCount = await Context.WorkAuthorizations.CountAsync(wa => wa.EmployeeId == employee.Id);
        var finalPerformanceReviewCount = await Context.PerformanceReviews.CountAsync(pr => pr.EmployeeId == employee.Id);
        var finalTrainingRecordCount = await Context.TrainingRecords.CountAsync(tr => tr.EmployeeId == employee.Id);

        Assert.Equal(1, finalEmergencyContactsCount);
        Assert.Equal(1, finalWorkAuthCount);
        Assert.Equal(1, finalPerformanceReviewCount);
        Assert.Equal(1, finalTrainingRecordCount);

        // Offboarding completed
        Assert.Equal(EmploymentStatus.Terminated, finalEmployee!.EmploymentStatus);
        Assert.NotNull(finalEmployee.TerminationDate);

        var completedOffboardingTasks = await Context.OffboardingChecklists
            .Where(t => t.EmployeeId == employee.Id)
            .ToListAsync();
        Assert.Equal(3, completedOffboardingTasks.Count());
        Assert.All(completedOffboardingTasks, t => Assert.True(t.CompletionStatus));
        Assert.All(completedOffboardingTasks, t => Assert.NotNull(t.CompletedDate));

        var exitInterviewRecord = await Context.ExitInterviews.FirstOrDefaultAsync(ei => ei.EmployeeId == employee.Id);
        Assert.NotNull(exitInterviewRecord);

        // Verify data integrity throughout lifecycle
        Assert.Equal(employee.Id, finalEmployee.Id);
        Assert.Equal(employee.StartDate, finalEmployee.StartDate);
        Assert.Equal(employee.TerminationDate, finalEmployee.TerminationDate);
        Assert.Equal("John Doe", finalEmployee.FullName);
    }

    [Fact]
    public async Task EmployeeLifecycle_OnboardingIncomplete_ShouldTrackPendingTasks()
    {
        // Arrange - Create employee with incomplete onboarding
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Marketing",
            IsActive = true,
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
                WorkEmail = "alice.smith@maliev.com",
                PersonalEmail = "alice@personal.com"
            },
            EmploymentType = EmploymentType.FullTime,
            EmploymentStatus = EmploymentStatus.Active,
            JobTitle = "Marketing Manager",
            StartDate = DateTime.UtcNow.Date,
            DepartmentId = department.Id,
            Department = department,
            CreatedDate = DateTime.UtcNow
        };

        var onboardingTasks = new List<OnboardingChecklist>
        {
            new OnboardingChecklist
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                ItemDescription = "Submit tax forms",
                DueDate = DateTime.UtcNow.AddDays(7),
                CompletionStatus = true,
                CompletedDate = DateTime.UtcNow,
                ResponsibleParty = ResponsibleParty.HR
            },
            new OnboardingChecklist
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                ItemDescription = "Complete benefits enrollment",
                DueDate = DateTime.UtcNow.AddDays(14),
                CompletionStatus = false, // Still pending
                ResponsibleParty = ResponsibleParty.HR
            }
        };

        Context.Departments.Add(department);
        Context.Employees.Add(employee);
        Context.OnboardingChecklists.AddRange(onboardingTasks);
        await Context.SaveChangesAsync();

        // Act - Query pending onboarding tasks
        var pendingTasks = await Context.OnboardingChecklists
            .Where(t => t.EmployeeId == employee.Id && !t.CompletionStatus)
            .ToListAsync();

        // Assert
        Assert.Single(pendingTasks);
        Assert.Equal("Complete benefits enrollment", pendingTasks.First().ItemDescription);

        var completedTasks = await Context.OnboardingChecklists
            .Where(t => t.EmployeeId == employee.Id && t.CompletionStatus)
            .CountAsync();
        Assert.Equal(1, completedTasks);
    }
}
