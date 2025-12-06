using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Domain;

public class DepartmentTests
{
    [Fact]
    public void IsRootDepartment_WhenNoParent_ShouldReturnTrue()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Root Department",
            ParentDepartmentId = null
        };

        // Act
        var result = department.IsRootDepartment;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRootDepartment_WhenHasParent_ShouldReturnFalse()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Sub Department",
            ParentDepartmentId = Guid.NewGuid()
        };

        // Act
        var result = department.IsRootDepartment;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasSubDepartments_WhenHasChildren_ShouldReturnTrue()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Parent Department",
            SubDepartments = new List<Department>
            {
                new Department { Id = Guid.NewGuid(), Name = "Child 1" },
                new Department { Id = Guid.NewGuid(), Name = "Child 2" }
            }
        };

        // Act
        var result = department.HasSubDepartments;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasSubDepartments_WhenNoChildren_ShouldReturnFalse()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Leaf Department",
            SubDepartments = new List<Department>()
        };

        // Act
        var result = department.HasSubDepartments;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CurrentHeadcount_ShouldCountActiveEmployeesOnly()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Department",
            Employees = new List<Employee>
            {
                new Employee { Id = Guid.NewGuid(), EmployeeNumber = "EMP001", EmploymentStatus = EmploymentStatus.Active },
                new Employee { Id = Guid.NewGuid(), EmployeeNumber = "EMP002", EmploymentStatus = EmploymentStatus.Active },
                new Employee { Id = Guid.NewGuid(), EmployeeNumber = "EMP003", EmploymentStatus = EmploymentStatus.Terminated }, // Inactive
                new Employee { Id = Guid.NewGuid(), EmployeeNumber = "EMP004", EmploymentStatus = EmploymentStatus.Active },
                new Employee { Id = Guid.NewGuid(), EmployeeNumber = "EMP005", EmploymentStatus = EmploymentStatus.Terminated }  // Inactive
            }
        };

        // Act
        var headcount = department.CurrentHeadcount;

        // Assert
        Assert.Equal(3, headcount); // Only 3 active employees
    }

    [Fact]
    public void CurrentHeadcount_WhenNoEmployees_ShouldReturnZero()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Empty Department",
            Employees = new List<Employee>()
        };

        // Act
        var headcount = department.CurrentHeadcount;

        // Assert
        Assert.Equal(0, headcount);
    }

    [Fact]
    public void IsApproachingHeadcountLimit_When80PercentFull_ShouldReturnTrue()
    {
        // Arrange - 40 employees out of 50 limit (80%)
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Department",
            HeadcountLimit = 50,
            Employees = Enumerable.Range(1, 40).Select(i => new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = $"EMP{i:000}",
                EmploymentStatus = EmploymentStatus.Active
            }).ToList()
        };

        // Act
        var result = department.IsApproachingHeadcountLimit;

        // Assert
        Assert.True(result);
        Assert.Equal(40, department.CurrentHeadcount);
    }

    [Fact]
    public void IsApproachingHeadcountLimit_When90PercentFull_ShouldReturnTrue()
    {
        // Arrange - 45 employees out of 50 limit (90%)
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Department",
            HeadcountLimit = 50,
            Employees = Enumerable.Range(1, 45).Select(i => new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = $"EMP{i:000}",
                EmploymentStatus = EmploymentStatus.Active
            }).ToList()
        };

        // Act
        var result = department.IsApproachingHeadcountLimit;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsApproachingHeadcountLimit_WhenBelow80Percent_ShouldReturnFalse()
    {
        // Arrange - 39 employees out of 50 limit (78%)
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Department",
            HeadcountLimit = 50,
            Employees = Enumerable.Range(1, 39).Select(i => new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = $"EMP{i:000}",
                EmploymentStatus = EmploymentStatus.Active
            }).ToList()
        };

        // Act
        var result = department.IsApproachingHeadcountLimit;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsApproachingHeadcountLimit_WhenNoLimit_ShouldReturnFalse()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Department",
            HeadcountLimit = null,
            Employees = Enumerable.Range(1, 100).Select(i => new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = $"EMP{i:000}",
                EmploymentStatus = EmploymentStatus.Active
            }).ToList()
        };

        // Act
        var result = department.IsApproachingHeadcountLimit;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAtHeadcountLimit_WhenAtExactLimit_ShouldReturnTrue()
    {
        // Arrange - Exactly at 50/50
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Department",
            HeadcountLimit = 50,
            Employees = Enumerable.Range(1, 50).Select(i => new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = $"EMP{i:000}",
                EmploymentStatus = EmploymentStatus.Active
            }).ToList()
        };

        // Act
        var result = department.IsAtHeadcountLimit;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAtHeadcountLimit_WhenOverLimit_ShouldReturnTrue()
    {
        // Arrange - Over capacity (51/50)
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Department",
            HeadcountLimit = 50,
            Employees = Enumerable.Range(1, 51).Select(i => new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = $"EMP{i:000}",
                EmploymentStatus = EmploymentStatus.Active
            }).ToList()
        };

        // Act
        var result = department.IsAtHeadcountLimit;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAtHeadcountLimit_WhenBelowLimit_ShouldReturnFalse()
    {
        // Arrange - Under capacity (49/50)
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Department",
            HeadcountLimit = 50,
            Employees = Enumerable.Range(1, 49).Select(i => new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = $"EMP{i:000}",
                EmploymentStatus = EmploymentStatus.Active
            }).ToList()
        };

        // Act
        var result = department.IsAtHeadcountLimit;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAtHeadcountLimit_WhenNoLimit_ShouldReturnFalse()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Department",
            HeadcountLimit = null,
            Employees = Enumerable.Range(1, 1000).Select(i => new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = $"EMP{i:000}",
                EmploymentStatus = EmploymentStatus.Active
            }).ToList()
        };

        // Act
        var result = department.IsAtHeadcountLimit;

        // Assert
        Assert.False(result);
    }
}
