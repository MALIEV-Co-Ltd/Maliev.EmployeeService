using FluentAssertions;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for department headcount limit warnings and enforcement (User Story 5)
/// </summary>
public class DepartmentHeadcountLimitTests : PostgreSqlIntegrationTestBase
{
    private DepartmentRepository _departmentRepository = null!;
    private EmployeeRepository _employeeRepository = null!;
    private UnitOfWork _unitOfWork = null!;
    private UpdateDepartmentCommandHandler _handler = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _departmentRepository = new DepartmentRepository(Context);
        _employeeRepository = new EmployeeRepository(Context);
        _unitOfWork = new UnitOfWork(Context);
        _handler = new UpdateDepartmentCommandHandler(_departmentRepository, _employeeRepository, _unitOfWork);
    }

    [Fact]
    public async Task UpdateDepartment_HeadcountLimitAt80Percent_ShouldWarn()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 10,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        Context.Departments.Add(department);

        // Add 8 employees (80% capacity)
        for (int i = 0; i < 8; i++)
        {
            Context.Employees.Add(CreateTestEmployee($"EMP{i:D3}", department.Id));
        }

        await Context.SaveChangesAsync();

        var command = new UpdateDepartmentCommand
        {
            DepartmentId = department.Id,
            Name = "Engineering",
            HeadcountLimit = 10,
            IsActive = true
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.Warnings.Should().HaveCount(1);
        result.Warnings[0].Should().Contain("80.0% capacity");
        result.Warnings[0].Should().Contain("8/10");
    }

    [Fact]
    public async Task UpdateDepartment_HeadcountLimitAt95Percent_ShouldWarn()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 20,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        Context.Departments.Add(department);

        // Add 19 employees (95% capacity)
        for (int i = 0; i < 19; i++)
        {
            Context.Employees.Add(CreateTestEmployee($"EMP{i:D3}", department.Id));
        }

        await Context.SaveChangesAsync();

        var command = new UpdateDepartmentCommand
        {
            DepartmentId = department.Id,
            Name = "Engineering",
            HeadcountLimit = 20,
            IsActive = true
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.Warnings.Should().HaveCount(1);
        result.Warnings[0].Should().Contain("95.0% capacity");
        result.Warnings[0].Should().Contain("19/20");
    }

    [Fact]
    public async Task UpdateDepartment_HeadcountLimitAt100Percent_ShouldWarn()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 15,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        Context.Departments.Add(department);

        // Add 15 employees (100% capacity)
        for (int i = 0; i < 15; i++)
        {
            Context.Employees.Add(CreateTestEmployee($"EMP{i:D3}", department.Id));
        }

        await Context.SaveChangesAsync();

        var command = new UpdateDepartmentCommand
        {
            DepartmentId = department.Id,
            Name = "Engineering",
            HeadcountLimit = 15,
            IsActive = true
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.Warnings.Should().HaveCount(1);
        result.Warnings[0].Should().Contain("100.0% capacity");
        result.Warnings[0].Should().Contain("15/15");
    }

    [Fact]
    public async Task UpdateDepartment_ReduceHeadcountLimitBelowCurrent_ShouldPrevent()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 20,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        Context.Departments.Add(department);

        // Add 15 employees
        for (int i = 0; i < 15; i++)
        {
            Context.Employees.Add(CreateTestEmployee($"EMP{i:D3}", department.Id));
        }

        await Context.SaveChangesAsync();

        var command = new UpdateDepartmentCommand
        {
            DepartmentId = department.Id,
            Name = "Engineering",
            HeadcountLimit = 10, // Trying to reduce below current 15
            IsActive = true
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cannot set headcount limit (10) below current headcount (15)");
    }

    [Fact]
    public async Task UpdateDepartment_HeadcountLimitBelow80Percent_NoWarning()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 20,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        Context.Departments.Add(department);

        // Add 10 employees (50% capacity)
        for (int i = 0; i < 10; i++)
        {
            Context.Employees.Add(CreateTestEmployee($"EMP{i:D3}", department.Id));
        }

        await Context.SaveChangesAsync();

        var command = new UpdateDepartmentCommand
        {
            DepartmentId = department.Id,
            Name = "Engineering",
            HeadcountLimit = 20,
            IsActive = true
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.Warnings.Should().BeEmpty();
    }

    /// <summary>
    /// Helper method to create a test employee
    /// </summary>
    private Employee CreateTestEmployee(string employeeNumber, Guid departmentId)
    {
        return new Employee
        {
            Id = Guid.NewGuid(),
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
            DepartmentId = departmentId,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
    }
}
