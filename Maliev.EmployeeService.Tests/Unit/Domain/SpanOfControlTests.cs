using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Domain;

/// <summary>
/// Unit tests for span of control validation logic (User Story 5)
/// </summary>
public class SpanOfControlTests
{
    [Fact]
    public void GetMaxSpanOfControl_ForICManager_ShouldReturn15()
    {
        // Arrange
        var manager = CreateTestEmployee("MGR001");
        var directReport1 = CreateTestEmployee("EMP001");
        var directReport2 = CreateTestEmployee("EMP002");

        // Add direct reports (ICs - no reports themselves)
        manager.DirectReports = new List<Employee> { directReport1, directReport2 };

        // Act
        var maxSpan = manager.GetMaxSpanOfControl();

        // Assert
        Assert.Equal(15, maxSpan); // IC managers should have a limit of 15 direct reports
    }

    [Fact]
    public void GetMaxSpanOfControl_ForManagerOfManagers_ShouldReturn8()
    {
        // Arrange
        var seniorManager = CreateTestEmployee("SMGR001");
        var midManager1 = CreateTestEmployee("MGR001");
        var midManager2 = CreateTestEmployee("MGR002");
        var ic1 = CreateTestEmployee("EMP001");
        var ic2 = CreateTestEmployee("EMP002");

        // Mid-managers have their own reports
        midManager1.DirectReports = new List<Employee> { ic1 };
        midManager2.DirectReports = new List<Employee> { ic2 };

        // Senior manager manages the mid-managers
        seniorManager.DirectReports = new List<Employee> { midManager1, midManager2 };

        // Act
        var maxSpan = seniorManager.GetMaxSpanOfControl();

        // Assert
        Assert.Equal(8, maxSpan); // Manager-of-managers should have a limit of 8 direct reports
    }

    [Fact]
    public void GetMaxSpanOfControl_WithNoDirectReports_ShouldReturn15()
    {
        // Arrange
        var employee = CreateTestEmployee("EMP001");
        employee.DirectReports = new List<Employee>();

        // Act
        var maxSpan = employee.GetMaxSpanOfControl();

        // Assert
        Assert.Equal(15, maxSpan); // Employees with no direct reports should default to IC manager limit
    }

    [Fact]
    public void GetCurrentSpanOfControl_ShouldReturnDirectReportCount()
    {
        // Arrange
        var manager = CreateTestEmployee("MGR001");
        var dr1 = CreateTestEmployee("EMP001");
        var dr2 = CreateTestEmployee("EMP002");
        var dr3 = CreateTestEmployee("EMP003");

        manager.DirectReports = new List<Employee> { dr1, dr2, dr3 };

        // Act
        var currentSpan = manager.GetCurrentSpanOfControl();

        // Assert
        Assert.Equal(3, currentSpan); // Should return the actual count of direct reports
    }

    [Fact]
    public void ValidateSpanOfControl_WithinLimit_ShouldBeValid()
    {
        // Arrange
        var manager = CreateTestEmployee("MGR001");
        manager.DirectReports = new List<Employee>
        {
            CreateTestEmployee("EMP001"),
            CreateTestEmployee("EMP002"),
            CreateTestEmployee("EMP003")
        };

        // Act
        var result = manager.ValidateSpanOfControl(additionalReports: 2);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(15, result.MaxSpanOfControl);
        Assert.Equal(3, result.CurrentSpanOfControl);
        Assert.Equal(5, result.ProjectedSpanOfControl);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void ValidateSpanOfControl_ExceedsLimit_ShouldBeInvalid()
    {
        // Arrange
        var manager = CreateTestEmployee("MGR001");
        manager.DirectReports = new List<Employee>();
        for (int i = 0; i < 14; i++)
        {
            manager.DirectReports.Add(CreateTestEmployee($"EMP{i:D3}"));
        }

        // Act - trying to add 2 more would exceed limit of 15
        var result = manager.ValidateSpanOfControl(additionalReports: 2);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Span of control limit exceeded", result.ErrorMessage);
        Assert.Contains("Maximum: 15", result.ErrorMessage);
        Assert.Contains("Projected: 16", result.ErrorMessage);
        Assert.Equal(15, result.MaxSpanOfControl);
        Assert.Equal(14, result.CurrentSpanOfControl);
        Assert.Equal(16, result.ProjectedSpanOfControl);
    }

    [Fact]
    public void ValidateSpanOfControl_At80PercentCapacity_ShouldHaveWarning()
    {
        // Arrange
        var manager = CreateTestEmployee("MGR001");
        manager.DirectReports = new List<Employee>();
        // 12 out of 15 = 80% capacity
        for (int i = 0; i < 12; i++)
        {
            manager.DirectReports.Add(CreateTestEmployee($"EMP{i:D3}"));
        }

        // Act
        var result = manager.ValidateSpanOfControl();

        // Assert
        Assert.True(result.IsValid);
        Assert.Single(result.Warnings);
        Assert.Contains("Approaching span of control limit", result.Warnings[0]);
        Assert.Contains("12/15", result.Warnings[0]);
        Assert.Contains("80", result.Warnings[0]);
    }

    [Fact]
    public void ValidateSpanOfControl_ManagerOfManagers_At80PercentCapacity_ShouldHaveWarning()
    {
        // Arrange
        var seniorManager = CreateTestEmployee("SMGR001");
        var midManager1 = CreateTestEmployee("MGR001");
        midManager1.DirectReports = new List<Employee> { CreateTestEmployee("EMP001") };

        // Add 7 mid-managers (7/8 = 87.5% capacity for manager-of-managers)
        seniorManager.DirectReports = new List<Employee>();
        for (int i = 0; i < 7; i++)
        {
            var midMgr = CreateTestEmployee($"MGR{i:D3}");
            midMgr.DirectReports = new List<Employee> { CreateTestEmployee($"EMP{i:D3}") };
            seniorManager.DirectReports.Add(midMgr);
        }

        // Act
        var result = seniorManager.ValidateSpanOfControl();

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(8, result.MaxSpanOfControl); // Manager-of-managers limit
        Assert.Equal(7, result.CurrentSpanOfControl);
        Assert.Single(result.Warnings);
        Assert.Contains("7/8", result.Warnings[0]);
        Assert.Contains("87.5", result.Warnings[0]);
    }

    [Fact]
    public void ValidateSpanOfControl_ManagerOfManagers_ExceedsLimit_ShouldBeInvalid()
    {
        // Arrange
        var seniorManager = CreateTestEmployee("SMGR001");

        // Add 8 mid-managers (at limit)
        seniorManager.DirectReports = new List<Employee>();
        for (int i = 0; i < 8; i++)
        {
            var midMgr = CreateTestEmployee($"MGR{i:D3}");
            midMgr.DirectReports = new List<Employee> { CreateTestEmployee($"EMP{i:D3}") };
            seniorManager.DirectReports.Add(midMgr);
        }

        // Act - trying to add 1 more would exceed limit of 8
        var result = seniorManager.ValidateSpanOfControl(additionalReports: 1);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Span of control limit exceeded", result.ErrorMessage);
        Assert.Equal(8, result.MaxSpanOfControl);
        Assert.Equal(8, result.CurrentSpanOfControl);
        Assert.Equal(9, result.ProjectedSpanOfControl);
    }

    [Fact]
    public void ValidateSpanOfControl_ExactlyAtLimit_ShouldBeValid()
    {
        // Arrange
        var manager = CreateTestEmployee("MGR001");
        manager.DirectReports = new List<Employee>();
        for (int i = 0; i < 15; i++)
        {
            manager.DirectReports.Add(CreateTestEmployee($"EMP{i:D3}"));
        }

        // Act
        var result = manager.ValidateSpanOfControl();

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(15, result.CurrentSpanOfControl);
        Assert.Equal(15, result.ProjectedSpanOfControl);
        Assert.Single(result.Warnings); // Should warn at 100% capacity
    }

    /// <summary>
    /// Helper method to create a test employee
    /// </summary>
    private Employee CreateTestEmployee(string employeeNumber)
    {
        return new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = employeeNumber,
            LegalName = new LegalName
            {
                FirstName = $"First{employeeNumber}",
                LastName = $"Last{employeeNumber}"
            },
            ContactInformation = new ContactInformation
            {
                WorkEmail = $"{employeeNumber}@company.com"
            },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            DirectReports = new List<Employee>()
        };
    }
}
