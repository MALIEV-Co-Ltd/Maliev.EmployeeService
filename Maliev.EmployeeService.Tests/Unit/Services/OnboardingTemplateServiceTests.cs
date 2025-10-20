using FluentAssertions;
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
        checklist.Should().NotBeEmpty();
        checklist.Should().Contain(item => item.ItemDescription.Contains("laptop"));
        checklist.Should().Contain(item => item.ItemDescription.Contains("email account"));
        checklist.Should().Contain(item => item.ItemDescription.Contains("VPN access"));
        checklist.Should().NotContain(item => item.ItemDescription.Contains("safety training"));
        checklist.Should().NotContain(item => item.ItemDescription.Contains("leadership"));

        // Verify all items have correct employee ID and dates
        checklist.Should().AllSatisfy(item =>
        {
            item.EmployeeId.Should().Be(employeeId);
            // Pre-onboarding tasks can have due dates before start date
            item.DueDate.Should().NotBe(default);
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
        checklist.Should().NotBeEmpty();
        checklist.Should().Contain(item => item.ItemDescription.Contains("safety training"));
        checklist.Should().Contain(item => item.ItemDescription.Contains("PPE"));
        checklist.Should().Contain(item => item.ItemDescription.Contains("safety equipment"));
        checklist.Should().NotContain(item => item.ItemDescription.Contains("VPN access"));
        checklist.Should().NotContain(item => item.ItemDescription.Contains("leadership"));
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
        checklist.Should().NotBeEmpty();
        checklist.Should().Contain(item => item.ItemDescription.Contains("leadership"));
        checklist.Should().Contain(item => item.ItemDescription.Contains("management training"));
        checklist.Should().Contain(item => item.ItemDescription.Contains("Team introduction"));

        // Managers should have office worker items too
        checklist.Should().Contain(item => item.ItemDescription.Contains("laptop"));
        checklist.Should().Contain(item => item.ItemDescription.Contains("email account"));
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
        checklist.Should().Contain(item => item.ItemDescription.Contains("leadership"));
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
        checklist.Should().Contain(item => item.ItemDescription.Contains("leadership"));
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
        checklist.Should().AllSatisfy(item =>
        {
            item.Id.Should().NotBeEmpty();
            item.EmployeeId.Should().Be(employeeId);
            item.ItemDescription.Should().NotBeNullOrWhiteSpace();
            item.ResponsibleParty.Should().BeOneOf(
                Maliev.EmployeeService.Domain.Enums.ResponsibleParty.HR,
                Maliev.EmployeeService.Domain.Enums.ResponsibleParty.IT,
                Maliev.EmployeeService.Domain.Enums.ResponsibleParty.Facilities,
                Maliev.EmployeeService.Domain.Enums.ResponsibleParty.Manager);
            item.DueDate.Should().BeAfter(DateTime.MinValue);
            item.CompletionStatus.Should().BeFalse(); // Should start incomplete
            item.DisplayOrder.Should().BeGreaterThan(0);
            item.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
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
        displayOrders.Should().BeInAscendingOrder();
        displayOrders.Should().OnlyHaveUniqueItems();
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
        checklist.Should().Contain(item => item.ItemDescription.Contains("safety training"));
        checklist.Should().Contain(item => item.ItemDescription.Contains("PPE"));
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
        officeChecklist.Should().Contain(item => item.ItemDescription.Contains("employee handbook"));
        factoryChecklist.Should().Contain(item => item.ItemDescription.Contains("employee handbook"));
        managerChecklist.Should().Contain(item => item.ItemDescription.Contains("employee handbook"));
    }
}
