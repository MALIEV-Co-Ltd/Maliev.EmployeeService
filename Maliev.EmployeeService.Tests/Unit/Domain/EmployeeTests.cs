using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Domain;

public class EmployeeTests
{
    [Fact]
    public void WouldCreateCircularRelationship_WhenEmployeeIsSelfManager_ShouldReturnTrue()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee { Id = employeeId };

        // Act
        var result = employee.WouldCreateCircularRelationship(employeeId, id => null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void WouldCreateCircularRelationship_WithNoCircle_ShouldReturnFalse()
    {
        // Arrange
        var employee1Id = Guid.NewGuid();
        var employee2Id = Guid.NewGuid();
        var employee3Id = Guid.NewGuid();

        // employee1 -> employee2 -> employee3 (no circle)
        var employee1 = new Employee { Id = employee1Id, ManagerId = employee2Id };
        var employee2 = new Employee { Id = employee2Id, ManagerId = employee3Id };
        var employee3 = new Employee { Id = employee3Id, ManagerId = null };

        Employee? GetEmployeeById(Guid id)
        {
            if (id == employee1Id) return employee1;
            if (id == employee2Id) return employee2;
            if (id == employee3Id) return employee3;
            return null;
        }

        // Act - Try to assign employee2 as manager of employee1
        var result = employee1.WouldCreateCircularRelationship(employee2Id, GetEmployeeById);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void WouldCreateCircularRelationship_WithTwoLevelCircle_ShouldReturnTrue()
    {
        // Arrange
        var employee1Id = Guid.NewGuid();
        var employee2Id = Guid.NewGuid();

        // Current: employee1 -> employee2 -> null
        // Trying: employee2 -> employee1 (would create circle)
        var employee1 = new Employee { Id = employee1Id, ManagerId = null };
        var employee2 = new Employee { Id = employee2Id, ManagerId = employee1Id };

        Employee? GetEmployeeById(Guid id)
        {
            if (id == employee1Id) return employee1;
            if (id == employee2Id) return employee2;
            return null;
        }

        // Act - Try to assign employee2 as manager of employee1 (would create circle)
        var result = employee1.WouldCreateCircularRelationship(employee2Id, GetEmployeeById);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void WouldCreateCircularRelationship_WithThreeLevelCircle_ShouldReturnTrue()
    {
        // Arrange
        var employee1Id = Guid.NewGuid();
        var employee2Id = Guid.NewGuid();
        var employee3Id = Guid.NewGuid();

        // Current: employee1 -> employee2 -> employee3 -> null
        // Trying: employee3 -> employee1 (would create circle)
        var employee1 = new Employee { Id = employee1Id, ManagerId = employee2Id };
        var employee2 = new Employee { Id = employee2Id, ManagerId = employee3Id };
        var employee3 = new Employee { Id = employee3Id, ManagerId = null };

        Employee? GetEmployeeById(Guid id)
        {
            if (id == employee1Id) return employee1;
            if (id == employee2Id) return employee2;
            if (id == employee3Id) return employee3;
            return null;
        }

        // Act - Try to assign employee1 as manager of employee3 (would create circle: 3->1->2->3)
        var result = employee3.WouldCreateCircularRelationship(employee1Id, GetEmployeeById);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void WouldCreateCircularRelationship_WithNullManager_ShouldReturnFalse()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        var employee = new Employee { Id = employeeId, ManagerId = null };
        var manager = new Employee { Id = managerId, ManagerId = null };

        Employee? GetEmployeeById(Guid id)
        {
            if (id == managerId) return manager;
            return null;
        }

        // Act
        var result = employee.WouldCreateCircularRelationship(managerId, GetEmployeeById);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void WouldCreateCircularRelationship_WithComplexHierarchy_ShouldDetectCircle()
    {
        // Arrange - Complex 4-level hierarchy
        var emp1Id = Guid.NewGuid();
        var emp2Id = Guid.NewGuid();
        var emp3Id = Guid.NewGuid();
        var emp4Id = Guid.NewGuid();

        // Current: emp1 -> emp2 -> emp3 -> emp4 -> null
        var emp1 = new Employee { Id = emp1Id, ManagerId = emp2Id };
        var emp2 = new Employee { Id = emp2Id, ManagerId = emp3Id };
        var emp3 = new Employee { Id = emp3Id, ManagerId = emp4Id };
        var emp4 = new Employee { Id = emp4Id, ManagerId = null };

        Employee? GetEmployeeById(Guid id)
        {
            if (id == emp1Id) return emp1;
            if (id == emp2Id) return emp2;
            if (id == emp3Id) return emp3;
            if (id == emp4Id) return emp4;
            return null;
        }

        // Act - Try to assign emp2 as manager of emp4 (would create circle: 4->2->3->4)
        var result = emp4.WouldCreateCircularRelationship(emp2Id, GetEmployeeById);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetAge_WithValidDateOfBirth_ShouldReturnCorrectAge()
    {
        var employee = new Employee
        {
            DateOfBirth = new DateTime(1990, 6, 15)
        };

        var age = employee.GetAge(new DateTime(2024, 6, 15));

        Assert.Equal(34, age);
    }

    [Fact]
    public void GetAge_WithDateOfBirthDefault_ShouldReturnNull()
    {
        var employee = new Employee
        {
            DateOfBirth = default
        };

        var age = employee.GetAge(new DateTime(2024, 6, 15));

        Assert.Null(age);
    }

    [Fact]
    public void GetTenureInYears_WithValidStartDate_ShouldReturnCorrectTenure()
    {
        var employee = new Employee
        {
            StartDate = new DateTime(2020, 1, 1)
        };

        var tenure = employee.GetTenureInYears(new DateTime(2024, 1, 1));

        Assert.Equal(4, tenure);
    }

    [Fact]
    public void GetTenureInYears_WithStartDateDefault_ShouldReturnNull()
    {
        var employee = new Employee
        {
            StartDate = default
        };

        var tenure = employee.GetTenureInYears(new DateTime(2024, 1, 1));

        Assert.Null(tenure);
    }

    [Fact]
    public void FullName_WithLegalName_ShouldReturnFullName()
    {
        var employee = new Employee
        {
            LegalName = new LegalName("John", "Doe", "Michael")
        };

        Assert.Equal("John Michael Doe", employee.FullName);
    }

    [Fact]
    public void FullName_WithNullLegalName_ShouldReturnEmptyString()
    {
        var employee = new Employee
        {
            LegalName = null!
        };

        Assert.Equal(string.Empty, employee.FullName);
    }

    [Fact]
    public void DisplayName_WithPreferredName_ShouldReturnPreferredName()
    {
        var employee = new Employee
        {
            LegalName = new LegalName("John", "Doe"),
            PreferredName = "Johnny"
        };

        Assert.Equal("Johnny", employee.DisplayName);
    }

    [Fact]
    public void DisplayName_WithoutPreferredName_ShouldReturnFirstName()
    {
        var employee = new Employee
        {
            LegalName = new LegalName("John", "Doe"),
            PreferredName = null
        };

        Assert.Equal("John", employee.DisplayName);
    }

    [Fact]
    public void DisplayName_WithNullLegalName_ShouldReturnEmptyString()
    {
        var employee = new Employee
        {
            LegalName = null!
        };

        Assert.Equal(string.Empty, employee.DisplayName);
    }

    [Fact]
    public void IsActive_WithActiveStatus_ShouldReturnTrue()
    {
        var employee = new Employee
        {
            EmploymentStatus = EmploymentStatus.Active
        };

        Assert.True(employee.IsActive);
    }

    [Fact]
    public void IsActive_WithTerminatedStatus_ShouldReturnFalse()
    {
        var employee = new Employee
        {
            EmploymentStatus = EmploymentStatus.Terminated
        };

        Assert.False(employee.IsActive);
    }

    [Fact]
    public void GetMaxSpanOfControl_WithNoDirectReports_ShouldReturn15()
    {
        var employee = new Employee
        {
            DirectReports = new List<Employee>()
        };

        Assert.Equal(15, employee.GetMaxSpanOfControl());
    }

    [Fact]
    public void GetMaxSpanOfControl_WithManagerDirectReports_ShouldReturn8()
    {
        var employee = new Employee
        {
            DirectReports = new List<Employee>
            {
                new Employee { DirectReports = new List<Employee> { new Employee() } }
            }
        };

        Assert.Equal(8, employee.GetMaxSpanOfControl());
    }

    [Fact]
    public void GetMaxSpanOfControl_WithNullDirectReports_ShouldReturn15()
    {
        var employee = new Employee
        {
            DirectReports = null!
        };

        Assert.Equal(15, employee.GetMaxSpanOfControl());
    }

    [Fact]
    public void GetCurrentSpanOfControl_ShouldReturnCount()
    {
        var employee = new Employee
        {
            DirectReports = new List<Employee>
            {
                new Employee(),
                new Employee(),
                new Employee()
            }
        };

        Assert.Equal(3, employee.GetCurrentSpanOfControl());
    }

    [Fact]
    public void GetCurrentSpanOfControl_WithNullDirectReports_ShouldReturn0()
    {
        var employee = new Employee
        {
            DirectReports = null!
        };

        Assert.Equal(0, employee.GetCurrentSpanOfControl());
    }

    [Fact]
    public void ValidateSpanOfControl_WithinLimit_ShouldReturnValid()
    {
        var employee = new Employee
        {
            DirectReports = new List<Employee>
            {
                new Employee(),
                new Employee()
            }
        };

        var result = employee.ValidateSpanOfControl();

        Assert.True(result.IsValid);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void ValidateSpanOfControl_ExceedingLimit_ShouldReturnInvalid()
    {
        var employee = new Employee
        {
            DirectReports = Enumerable.Range(1, 16).Select(_ => new Employee()).ToList()
        };

        var result = employee.ValidateSpanOfControl();

        Assert.False(result.IsValid);
        Assert.Contains("Span of control limit exceeded", result.ErrorMessage);
    }

    [Fact]
    public void ValidateSpanOfControl_At80Percent_ShouldAddWarning()
    {
        var employee = new Employee
        {
            DirectReports = Enumerable.Range(1, 13).Select(_ => new Employee()).ToList()
        };

        var result = employee.ValidateSpanOfControl();

        Assert.True(result.IsValid);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains("Approaching span of control limit", result.Warnings[0]);
    }
}
