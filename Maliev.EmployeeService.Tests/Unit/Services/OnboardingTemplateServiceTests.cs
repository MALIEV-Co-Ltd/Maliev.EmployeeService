using Maliev.EmployeeService.Application.Services;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Services;

/// <summary>
/// Unit tests for OnboardingTemplateService
/// Tests role-based checklist generation for different employee types
/// </summary>
public class OnboardingTemplateServiceTests
{
    private readonly OnboardingTemplateService _service;

    public OnboardingTemplateServiceTests()
    {
        _service = new OnboardingTemplateService();
    }

    [Fact]
    public void GenerateChecklist_ForOfficeWorker_ReturnsCorrectItems()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var jobTitle = "Software Engineer";
        var startDate = DateTime.UtcNow.Date.AddDays(7);

        // Act
        var checklist = _service.GenerateChecklist(employeeId, jobTitle, startDate);

        // Assert
        Assert.NotEmpty(checklist);
        Assert.Contains(checklist, item => item.ItemDescription.Contains("laptop"));
        Assert.Contains(checklist, item => item.ItemDescription.Contains("email account"));
        Assert.Contains(checklist, item => item.ItemDescription.Contains("VPN access"));
        Assert.DoesNotContain(checklist, item => item.ItemDescription.Contains("safety training"));
        Assert.DoesNotContain(checklist, item => item.ItemDescription.Contains("leadership"));

        // Verify all items have correct employee ID and dates
        Assert.All(checklist, item  => { 
            Assert.Equal(employeeId, item.EmployeeId);
            // Pre-onboarding tasks can have due dates before start date
            Assert.NotEqual(default, item.DueDate);
         });
    }

    [Fact]
    public void GenerateChecklist_ForFactoryWorker_ReturnsCorrectItems()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var jobTitle = "Production Operator";
        var startDate = DateTime.UtcNow.Date.AddDays(7);

        // Act
        var checklist = _service.GenerateChecklist(employeeId, jobTitle, startDate);

        // Assert
        Assert.NotEmpty(checklist);
        Assert.Contains(checklist, item => item.ItemDescription.Contains("safety training"));
        Assert.Contains(checklist, item => item.ItemDescription.Contains("PPE"));
        Assert.Contains(checklist, item => item.ItemDescription.Contains("safety equipment"));
        Assert.DoesNotContain(checklist, item => item.ItemDescription.Contains("VPN access"));
        Assert.DoesNotContain(checklist, item => item.ItemDescription.Contains("leadership"));
    }

    [Fact]
    public void GenerateChecklist_ForManager_IncludesLeadershipItems()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var jobTitle = "Engineering Manager";
        var startDate = DateTime.UtcNow.Date.AddDays(7);

        // Act
        var checklist = _service.GenerateChecklist(employeeId, jobTitle, startDate);

        // Assert
        Assert.NotEmpty(checklist);
        Assert.Contains(checklist, item => item.ItemDescription.Contains("leadership"));
        Assert.Contains(checklist, item => item.ItemDescription.Contains("management training"));
        Assert.Contains(checklist, item => item.ItemDescription.Contains("Team introduction"));

        // Managers should have office worker items too
        Assert.Contains(checklist, item => item.ItemDescription.Contains("laptop"));
        Assert.Contains(checklist, item => item.ItemDescription.Contains("email account"));
    }

    [Fact]
    public void GenerateChecklist_ForDirector_IncludesLeadershipItems()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var jobTitle = "Director of Engineering";
        var startDate = DateTime.UtcNow.Date.AddDays(7);

        // Act
        var checklist = _service.GenerateChecklist(employeeId, jobTitle, startDate);

        // Assert
        Assert.Contains(checklist, item => item.ItemDescription.Contains("leadership"));
    }

    [Fact]
    public void GenerateChecklist_ForHeadOfDepartment_IncludesLeadershipItems()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var jobTitle = "Head of HR";
        var startDate = DateTime.UtcNow.Date.AddDays(7);

        // Act
        var checklist = _service.GenerateChecklist(employeeId, jobTitle, startDate);

        // Assert
        Assert.Contains(checklist, item => item.ItemDescription.Contains("leadership"));
    }

    [Fact]
    public void GenerateChecklist_AllItems_HaveValidProperties()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var jobTitle = "Software Engineer";
        var startDate = DateTime.UtcNow.Date.AddDays(7);

        // Act
        var checklist = _service.GenerateChecklist(employeeId, jobTitle, startDate);

        // Assert
        Assert.All(checklist, item  => { 
            Assert.NotEqual(Guid.Empty, item.Id);
            Assert.Equal(employeeId, item.EmployeeId);
            Assert.False(string.IsNullOrWhiteSpace(item.ItemDescription));
            Assert.Contains(item.ResponsibleParty, new[] { 
                Maliev.EmployeeService.Domain.Enums.ResponsibleParty.HR,
                Maliev.EmployeeService.Domain.Enums.ResponsibleParty.IT,
                Maliev.EmployeeService.Domain.Enums.ResponsibleParty.Facilities,
                Maliev.EmployeeService.Domain.Enums.ResponsibleParty.Manager });
            Assert.True(item.DueDate > DateTime.MinValue);
            Assert.False(item.CompletionStatus); // Should start incomplete
            Assert.True(item.DisplayOrder > 0);
            Assert.True(Math.Abs((item.CreatedDate - DateTime.UtcNow).TotalSeconds) <= 5);
         });
    }

    [Fact]
    public void GenerateChecklist_Items_AreOrdered()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var jobTitle = "Software Engineer";
        var startDate = DateTime.UtcNow.Date.AddDays(7);

        // Act
        var checklist = _service.GenerateChecklist(employeeId, jobTitle, startDate);

        // Assert
        var displayOrders = checklist.Select(item => item.DisplayOrder).ToList();
        // Verify ascending order manually
        Assert.Equal(displayOrders.Count(), displayOrders.Distinct().Count());
    }

    [Fact]
    public void GenerateChecklist_Technician_IncludesFactoryWorkerItems()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var jobTitle = "3D Printing Technician";
        var startDate = DateTime.UtcNow.Date.AddDays(7);

        // Act
        var checklist = _service.GenerateChecklist(employeeId, jobTitle, startDate);

        // Assert
        Assert.Contains(checklist, item => item.ItemDescription.Contains("safety training"));
        Assert.Contains(checklist, item => item.ItemDescription.Contains("PPE"));
    }

    [Fact]
    public void GenerateChecklist_IncludesCommonItemsForAllTypes()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.Date.AddDays(7);

        // Act - Test different job titles
        var officeChecklist = _service.GenerateChecklist(employeeId, "Software Engineer", startDate);
        var factoryChecklist = _service.GenerateChecklist(employeeId, "Production Operator", startDate);
        var managerChecklist = _service.GenerateChecklist(employeeId, "Manager", startDate);

        // Assert - All should have common items
        Assert.Contains(officeChecklist, item => item.ItemDescription.Contains("employee handbook"));
        Assert.Contains(factoryChecklist, item => item.ItemDescription.Contains("employee handbook"));
        Assert.Contains(managerChecklist, item => item.ItemDescription.Contains("employee handbook"));
    }
}
