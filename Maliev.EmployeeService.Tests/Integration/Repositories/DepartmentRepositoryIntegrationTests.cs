using FluentAssertions;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for DepartmentRepository using in-memory database
/// </summary>
public class DepartmentRepositoryIntegrationTests : PostgreSqlIntegrationTestBase
{



    [Fact]
    public async Task GetAllWithSubDepartmentsAsync_ShouldReturnAllDepartments()
    {
        // Arrange
        var repository = new DepartmentRepository(Context);
        var parentDepartment = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var childDepartment = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Backend Team",
            ParentDepartmentId = parentDepartment.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        Context.Departments.AddRange(parentDepartment, childDepartment);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllWithSubDepartmentsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(d => d.Name == "Engineering");
        result.Should().Contain(d => d.Name == "Backend Team");
    }

    [Fact]
    public async Task GetByIdWithSubDepartmentsAsync_ShouldIncludeEmployees()
    {
        // Arrange
        var repository = new DepartmentRepository(Context);
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            DepartmentId = department.Id,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Departments.Add(department);
        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdWithSubDepartmentsAsync(department.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Engineering");
        result.Employees.Should().HaveCount(1);
        result.Employees.First().EmployeeNumber.Should().Be("EMP001");
    }

    [Fact]
    public async Task GetHierarchyAsync_ShouldReturnRootDepartmentsWithChildren()
    {
        // Arrange
        var repository = new DepartmentRepository(Context);
        var root = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Company",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var engineering = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            ParentDepartmentId = root.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var backend = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Backend Team",
            ParentDepartmentId = engineering.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        Context.Departments.AddRange(root, engineering, backend);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.GetHierarchyAsync();

        // Assert
        result.Should().HaveCount(3);
        var rootDept = result.First(d => d.Name == "Company");
        rootDept.IsRootDepartment.Should().BeTrue();
    }

    [Fact]
    public async Task GetEmployeeCountAsync_WithoutSubDepartments_ShouldReturnDirectCount()
    {
        // Arrange
        var repository = new DepartmentRepository(Context);
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var employees = Enumerable.Range(1, 5).Select(i => new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = $"EMP{i:000}",
            DepartmentId = department.Id,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        }).ToList();

        Context.Departments.Add(department);
        Context.Employees.AddRange(employees);
        await Context.SaveChangesAsync();

        // Act
        var count = await repository.GetEmployeeCountAsync(department.Id, includeSubDepartments: false);

        // Assert
        count.Should().Be(5);
    }

    [Fact]
    public async Task GetEmployeeCountAsync_WithSubDepartments_ShouldReturnTotalCount()
    {
        // Arrange
        var repository = new DepartmentRepository(Context);
        var parent = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var child = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Backend",
            ParentDepartmentId = parent.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var parentEmployees = Enumerable.Range(1, 3).Select(i => new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = $"EMP{i:000}",
            DepartmentId = parent.Id,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        }).ToList();

        var childEmployees = Enumerable.Range(4, 2).Select(i => new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = $"EMP{i:000}",
            DepartmentId = child.Id,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        }).ToList();

        Context.Departments.AddRange(parent, child);
        Context.Employees.AddRange(parentEmployees);
        Context.Employees.AddRange(childEmployees);
        await Context.SaveChangesAsync();

        // Act
        var count = await repository.GetEmployeeCountAsync(parent.Id, includeSubDepartments: true);

        // Assert
        count.Should().Be(5); // 3 parent + 2 child
    }

    [Fact]
    public async Task HasSubDepartmentsAsync_WhenHasChildren_ShouldReturnTrue()
    {
        // Arrange
        var repository = new DepartmentRepository(Context);
        var parent = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var child = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Backend",
            ParentDepartmentId = parent.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        Context.Departments.AddRange(parent, child);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.HasSubDepartmentsAsync(parent.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasSubDepartmentsAsync_WhenNoChildren_ShouldReturnFalse()
    {
        // Arrange
        var repository = new DepartmentRepository(Context);
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        Context.Departments.Add(department);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.HasSubDepartmentsAsync(department.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasEmployeesAsync_WhenHasEmployees_ShouldReturnTrue()
    {
        // Arrange
        var repository = new DepartmentRepository(Context);
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            DepartmentId = department.Id,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Departments.Add(department);
        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.HasEmployeesAsync(department.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasEmployeesAsync_WhenNoEmployees_ShouldReturnFalse()
    {
        // Arrange
        var repository = new DepartmentRepository(Context);
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        Context.Departments.Add(department);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.HasEmployeesAsync(department.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetDepartmentsNearHeadcountLimitAsync_ShouldReturnDepartmentsAbove80Percent()
    {
        // Arrange
        var repository = new DepartmentRepository(Context);
        var fullDepartment = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Full Department",
            HeadcountLimit = 10,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var nearFullDepartment = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Near Full Department",
            HeadcountLimit = 10,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var lowDepartment = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Low Department",
            HeadcountLimit = 10,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        // Full: 10/10 employees
        var fullEmployees = Enumerable.Range(1, 10).Select(i => new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = $"FULL{i:00}",
            DepartmentId = fullDepartment.Id,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        }).ToList();

        // Near full: 8/10 employees (80%)
        var nearFullEmployees = Enumerable.Range(1, 8).Select(i => new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = $"NEAR{i:00}",
            DepartmentId = nearFullDepartment.Id,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        }).ToList();

        // Low: 5/10 employees (50%)
        var lowEmployees = Enumerable.Range(1, 5).Select(i => new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = $"LOW{i:00}",
            DepartmentId = lowDepartment.Id,
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        }).ToList();

        Context.Departments.AddRange(fullDepartment, nearFullDepartment, lowDepartment);
        Context.Employees.AddRange(fullEmployees);
        Context.Employees.AddRange(nearFullEmployees);
        Context.Employees.AddRange(lowEmployees);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.GetDepartmentsNearHeadcountLimitAsync();

        // Assert
        result.Should().HaveCount(2); // Full and Near Full
        result.Should().Contain(d => d.Name == "Full Department");
        result.Should().Contain(d => d.Name == "Near Full Department");
        result.Should().NotContain(d => d.Name == "Low Department");
    }
}
