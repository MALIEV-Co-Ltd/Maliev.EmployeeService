using FluentAssertions;
using Maliev.EmployeeService.Domain.Entities;
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
        result.Should().BeTrue();
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
        result.Should().BeFalse();
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
        result.Should().BeTrue();
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
        result.Should().BeTrue();
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
        result.Should().BeFalse();
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
        result.Should().BeTrue();
    }
}
