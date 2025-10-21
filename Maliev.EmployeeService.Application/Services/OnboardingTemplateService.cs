using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Services;

/// <summary>
/// Service for generating onboarding checklist templates based on employee type
/// </summary>
public class OnboardingTemplateService
{
    /// <summary>
    /// Generates onboarding checklist items based on employee type and role
    /// </summary>
    public List<OnboardingChecklist> GenerateChecklist(Guid employeeId, string jobTitle, DateTime startDate)
    {
        var checklist = new List<OnboardingChecklist>();
        var isManager = jobTitle.Contains("Manager", StringComparison.OrdinalIgnoreCase) ||
                        jobTitle.Contains("Director", StringComparison.OrdinalIgnoreCase) ||
                        jobTitle.Contains("Head", StringComparison.OrdinalIgnoreCase);

        // Common items for all employees
        AddCommonItems(checklist, employeeId, startDate);

        // Manager-specific items
        if (isManager)
        {
            AddManagerItems(checklist, employeeId, startDate);
        }

        // Factory/production workers vs office workers
        if (jobTitle.Contains("Operator", StringComparison.OrdinalIgnoreCase) ||
            jobTitle.Contains("Technician", StringComparison.OrdinalIgnoreCase) ||
            jobTitle.Contains("Production", StringComparison.OrdinalIgnoreCase))
        {
            AddFactoryWorkerItems(checklist, employeeId, startDate);
        }
        else
        {
            AddOfficeWorkerItems(checklist, employeeId, startDate);
        }

        return checklist;
    }

    private void AddCommonItems(List<OnboardingChecklist> checklist, Guid employeeId, DateTime startDate)
    {
        var items = new[]
        {
            new { Description = "Complete employment contract signature", ResponsibleParty = ResponsibleParty.HR, DaysBeforeStart = 7, DisplayOrder = 1 },
            new { Description = "Submit identification documents and work permit", ResponsibleParty = ResponsibleParty.HR, DaysBeforeStart = 7, DisplayOrder = 2 },
            new { Description = "Complete tax withholding forms", ResponsibleParty = ResponsibleParty.HR, DaysBeforeStart = 5, DisplayOrder = 3 },
            new { Description = "Enroll in health insurance and benefits", ResponsibleParty = ResponsibleParty.HR, DaysBeforeStart = 5, DisplayOrder = 4 },
            new { Description = "Complete emergency contact information", ResponsibleParty = ResponsibleParty.HR, DaysBeforeStart = 3, DisplayOrder = 5 },
            new { Description = "Attend company orientation session", ResponsibleParty = ResponsibleParty.HR, DaysBeforeStart = 0, DisplayOrder = 6 },
            new { Description = "Receive employee handbook and acknowledge policies", ResponsibleParty = ResponsibleParty.HR, DaysBeforeStart = 0, DisplayOrder = 7 },
            new { Description = "Setup payroll direct deposit", ResponsibleParty = ResponsibleParty.HR, DaysBeforeStart = 3, DisplayOrder = 8 }
        };

        foreach (var item in items)
        {
            checklist.Add(new OnboardingChecklist
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                ItemDescription = item.Description,
                ResponsibleParty = item.ResponsibleParty,
                DueDate = startDate.AddDays(-item.DaysBeforeStart),
                CompletionStatus = false,
                DisplayOrder = item.DisplayOrder,
                CreatedDate = DateTime.UtcNow
            });
        }
    }

    private void AddOfficeWorkerItems(List<OnboardingChecklist> checklist, Guid employeeId, DateTime startDate)
    {
        var startingOrder = checklist.Count + 1;
        var items = new[]
        {
            new { Description = "Setup company email account", ResponsibleParty = ResponsibleParty.IT, DaysBeforeStart = 1, DisplayOrder = startingOrder },
            new { Description = "Provision laptop and accessories", ResponsibleParty = ResponsibleParty.IT, DaysBeforeStart = 1, DisplayOrder = startingOrder + 1 },
            new { Description = "Setup VPN access and remote connectivity", ResponsibleParty = ResponsibleParty.IT, DaysBeforeStart = 0, DisplayOrder = startingOrder + 2 },
            new { Description = "Grant access to company software and systems", ResponsibleParty = ResponsibleParty.IT, DaysBeforeStart = 0, DisplayOrder = startingOrder + 3 },
            new { Description = "Assign desk and workspace setup", ResponsibleParty = ResponsibleParty.Facilities, DaysBeforeStart = 1, DisplayOrder = startingOrder + 4 },
            new { Description = "Provide office supplies and equipment", ResponsibleParty = ResponsibleParty.Facilities, DaysBeforeStart = 0, DisplayOrder = startingOrder + 5 },
            new { Description = "Schedule department introduction meeting", ResponsibleParty = ResponsibleParty.Manager, DaysBeforeStart = 0, DisplayOrder = startingOrder + 6 }
        };

        foreach (var item in items)
        {
            checklist.Add(new OnboardingChecklist
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                ItemDescription = item.Description,
                ResponsibleParty = item.ResponsibleParty,
                DueDate = startDate.AddDays(-item.DaysBeforeStart),
                CompletionStatus = false,
                DisplayOrder = item.DisplayOrder,
                CreatedDate = DateTime.UtcNow
            });
        }
    }

    private void AddFactoryWorkerItems(List<OnboardingChecklist> checklist, Guid employeeId, DateTime startDate)
    {
        var startingOrder = checklist.Count + 1;
        var items = new[]
        {
            new { Description = "Complete PPE safety equipment fitting (helmet, boots, goggles)", ResponsibleParty = ResponsibleParty.Facilities, DaysBeforeStart = 1, DisplayOrder = startingOrder },
            new { Description = "Complete machine safety training certification", ResponsibleParty = ResponsibleParty.Manager, DaysBeforeStart = 0, DisplayOrder = startingOrder + 1 },
            new { Description = "Assign locker and provide uniform", ResponsibleParty = ResponsibleParty.Facilities, DaysBeforeStart = 1, DisplayOrder = startingOrder + 2 },
            new { Description = "Setup time clock and attendance system access", ResponsibleParty = ResponsibleParty.IT, DaysBeforeStart = 0, DisplayOrder = startingOrder + 3 },
            new { Description = "Complete hazardous materials handling training", ResponsibleParty = ResponsibleParty.Manager, DaysBeforeStart = 0, DisplayOrder = startingOrder + 4 },
            new { Description = "Review emergency evacuation procedures", ResponsibleParty = ResponsibleParty.Manager, DaysBeforeStart = 0, DisplayOrder = startingOrder + 5 },
            new { Description = "Assign work station and introduce to team", ResponsibleParty = ResponsibleParty.Manager, DaysBeforeStart = 0, DisplayOrder = startingOrder + 6 }
        };

        foreach (var item in items)
        {
            checklist.Add(new OnboardingChecklist
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                ItemDescription = item.Description,
                ResponsibleParty = item.ResponsibleParty,
                DueDate = startDate.AddDays(-item.DaysBeforeStart),
                CompletionStatus = false,
                DisplayOrder = item.DisplayOrder,
                CreatedDate = DateTime.UtcNow
            });
        }
    }

    private void AddManagerItems(List<OnboardingChecklist> checklist, Guid employeeId, DateTime startDate)
    {
        var startingOrder = checklist.Count + 1;
        var items = new[]
        {
            new { Description = "Complete management training and leadership orientation", ResponsibleParty = ResponsibleParty.HR, DaysBeforeStart = 0, DisplayOrder = startingOrder },
            new { Description = "Team introduction and review of reporting relationships", ResponsibleParty = ResponsibleParty.HR, DaysBeforeStart = 0, DisplayOrder = startingOrder + 1 },
            new { Description = "Setup access to HRIS and performance management systems", ResponsibleParty = ResponsibleParty.IT, DaysBeforeStart = 0, DisplayOrder = startingOrder + 2 },
            new { Description = "Review department budget and financial responsibilities", ResponsibleParty = ResponsibleParty.Manager, DaysBeforeStart = 3, DisplayOrder = startingOrder + 3 },
            new { Description = "Schedule one-on-one meetings with direct reports", ResponsibleParty = ResponsibleParty.Manager, DaysBeforeStart = 0, DisplayOrder = startingOrder + 4 }
        };

        foreach (var item in items)
        {
            checklist.Add(new OnboardingChecklist
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                ItemDescription = item.Description,
                ResponsibleParty = item.ResponsibleParty,
                DueDate = startDate.AddDays(-item.DaysBeforeStart),
                CompletionStatus = false,
                DisplayOrder = item.DisplayOrder,
                CreatedDate = DateTime.UtcNow
            });
        }
    }
}
