using FluentAssertions;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for dotted-line manager relationships (User Story 5)
/// </summary>
public class DottedLineManagerTests : PostgreSqlIntegrationTestBase
{
    [Fact]
    public async Task Employee_CanHaveBothPrimaryAndDottedLineManager()
    {
        // Arrange
        var primaryManager = CreateTestEmployee("MGR001");
        var dottedLineManager = CreateTestEmployee("MGR002");
        var employee = CreateTestEmployee("EMP001");

        employee.ManagerId = primaryManager.Id;
        employee.DottedLineManagerId = dottedLineManager.Id;

        Context.Employees.AddRange(primaryManager, dottedLineManager, employee);
        await Context.SaveChangesAsync();

        // Act
        var loadedEmployee = await Context.Employees
            .Include(e => e.Manager)
            .Include(e => e.DottedLineManager)
            .FirstAsync(e => e.Id == employee.Id);

        // Assert
        loadedEmployee.ManagerId.Should().Be(primaryManager.Id);
        loadedEmployee.DottedLineManagerId.Should().Be(dottedLineManager.Id);
        loadedEmployee.Manager.Should().NotBeNull();
        loadedEmployee.DottedLineManager.Should().NotBeNull();
        loadedEmployee.Manager!.Id.Should().Be(primaryManager.Id);
        loadedEmployee.DottedLineManager!.Id.Should().Be(dottedLineManager.Id);
    }

    [Fact]
    public async Task Manager_CanHaveBothDirectAndDottedLineReports()
    {
        // Arrange
        var manager = CreateTestEmployee("MGR001");
        var directReport1 = CreateTestEmployee("EMP001");
        var directReport2 = CreateTestEmployee("EMP002");
        var dottedLineReport1 = CreateTestEmployee("EMP003");
        var dottedLineReport2 = CreateTestEmployee("EMP004");

        directReport1.ManagerId = manager.Id;
        directReport2.ManagerId = manager.Id;
        dottedLineReport1.DottedLineManagerId = manager.Id;
        dottedLineReport2.DottedLineManagerId = manager.Id;

        Context.Employees.AddRange(
            manager, directReport1, directReport2, dottedLineReport1, dottedLineReport2);
        await Context.SaveChangesAsync();

        // Act
        var loadedManager = await Context.Employees
            .Include(e => e.DirectReports)
            .Include(e => e.DottedLineReports)
            .FirstAsync(e => e.Id == manager.Id);

        // Assert
        loadedManager.DirectReports.Should().HaveCount(2);
        loadedManager.DottedLineReports.Should().HaveCount(2);
        loadedManager.DirectReports.Should().Contain(e => e.Id == directReport1.Id);
        loadedManager.DirectReports.Should().Contain(e => e.Id == directReport2.Id);
        loadedManager.DottedLineReports.Should().Contain(e => e.Id == dottedLineReport1.Id);
        loadedManager.DottedLineReports.Should().Contain(e => e.Id == dottedLineReport2.Id);
    }

    [Fact]
    public async Task Employee_WithNoManagers_ShouldHaveNullReferences()
    {
        // Arrange
        var employee = CreateTestEmployee("EMP001");
        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        // Act
        var loadedEmployee = await Context.Employees
            .Include(e => e.Manager)
            .Include(e => e.DottedLineManager)
            .FirstAsync(e => e.Id == employee.Id);

        // Assert
        loadedEmployee.ManagerId.Should().BeNull();
        loadedEmployee.DottedLineManagerId.Should().BeNull();
        loadedEmployee.Manager.Should().BeNull();
        loadedEmployee.DottedLineManager.Should().BeNull();
    }

    [Fact]
    public async Task MatrixReporting_EmployeeReportsToBothFunctionalAndProjectManager()
    {
        // Arrange - Typical matrix organization scenario
        // Functional manager (department head)
        var functionalManager = CreateTestEmployee("FMGR001");
        functionalManager.JobTitle = "Engineering Department Head";

        // Project manager
        var projectManager = CreateTestEmployee("PMGR001");
        projectManager.JobTitle = "Product Manager";

        // Employee reports to functional manager (primary) and project manager (dotted line)
        var employee = CreateTestEmployee("EMP001");
        employee.JobTitle = "Software Engineer";
        employee.ManagerId = functionalManager.Id; // Primary functional reporting
        employee.DottedLineManagerId = projectManager.Id; // Dotted line to project manager

        Context.Employees.AddRange(functionalManager, projectManager, employee);
        await Context.SaveChangesAsync();

        // Act
        var employeeWithManagers = await Context.Employees
            .Include(e => e.Manager)
            .Include(e => e.DottedLineManager)
            .FirstAsync(e => e.Id == employee.Id);

        var functionalManagerWithReports = await Context.Employees
            .Include(e => e.DirectReports)
            .FirstAsync(e => e.Id == functionalManager.Id);

        var projectManagerWithDottedLineReports = await Context.Employees
            .Include(e => e.DottedLineReports)
            .FirstAsync(e => e.Id == projectManager.Id);

        // Assert - Verify matrix reporting structure
        employeeWithManagers.Manager.Should().NotBeNull();
        employeeWithManagers.Manager!.JobTitle.Should().Be("Engineering Department Head");
        employeeWithManagers.DottedLineManager.Should().NotBeNull();
        employeeWithManagers.DottedLineManager!.JobTitle.Should().Be("Product Manager");

        functionalManagerWithReports.DirectReports.Should().ContainSingle();
        functionalManagerWithReports.DirectReports.First().Id.Should().Be(employee.Id);

        projectManagerWithDottedLineReports.DottedLineReports.Should().ContainSingle();
        projectManagerWithDottedLineReports.DottedLineReports.First().Id.Should().Be(employee.Id);
    }

    [Fact]
    public async Task UpdateDottedLineManager_ShouldMaintainPrimaryManagerRelationship()
    {
        // Arrange
        var primaryManager = CreateTestEmployee("MGR001");
        var oldDottedLineManager = CreateTestEmployee("MGR002");
        var newDottedLineManager = CreateTestEmployee("MGR003");
        var employee = CreateTestEmployee("EMP001");

        employee.ManagerId = primaryManager.Id;
        employee.DottedLineManagerId = oldDottedLineManager.Id;

        Context.Employees.AddRange(primaryManager, oldDottedLineManager, newDottedLineManager, employee);
        await Context.SaveChangesAsync();

        // Act - Update dotted line manager
        var employeeToUpdate = await Context.Employees.FirstAsync(e => e.Id == employee.Id);
        employeeToUpdate.DottedLineManagerId = newDottedLineManager.Id;
        await Context.SaveChangesAsync();

        // Reload to verify
        var updatedEmployee = await Context.Employees
            .Include(e => e.Manager)
            .Include(e => e.DottedLineManager)
            .FirstAsync(e => e.Id == employee.Id);

        // Assert
        updatedEmployee.ManagerId.Should().Be(primaryManager.Id, "Primary manager should remain unchanged");
        updatedEmployee.DottedLineManagerId.Should().Be(newDottedLineManager.Id, "Dotted line manager should be updated");
        updatedEmployee.Manager!.Id.Should().Be(primaryManager.Id);
        updatedEmployee.DottedLineManager!.Id.Should().Be(newDottedLineManager.Id);
    }

    [Fact]
    public async Task RemoveDottedLineManager_ShouldMaintainPrimaryManager()
    {
        // Arrange
        var primaryManager = CreateTestEmployee("MGR001");
        var dottedLineManager = CreateTestEmployee("MGR002");
        var employee = CreateTestEmployee("EMP001");

        employee.ManagerId = primaryManager.Id;
        employee.DottedLineManagerId = dottedLineManager.Id;

        Context.Employees.AddRange(primaryManager, dottedLineManager, employee);
        await Context.SaveChangesAsync();

        // Act - Remove dotted line manager
        var employeeToUpdate = await Context.Employees.FirstAsync(e => e.Id == employee.Id);
        employeeToUpdate.DottedLineManagerId = null;
        await Context.SaveChangesAsync();

        // Reload to verify
        var updatedEmployee = await Context.Employees
            .Include(e => e.Manager)
            .Include(e => e.DottedLineManager)
            .FirstAsync(e => e.Id == employee.Id);

        // Assert
        updatedEmployee.ManagerId.Should().Be(primaryManager.Id, "Primary manager should remain unchanged");
        updatedEmployee.DottedLineManagerId.Should().BeNull("Dotted line manager should be removed");
        updatedEmployee.Manager!.Id.Should().Be(primaryManager.Id);
        updatedEmployee.DottedLineManager.Should().BeNull();
    }

    [Fact]
    public async Task ComplexMatrixOrganization_MultipleLayersOfReporting()
    {
        // Arrange - Complex matrix with multiple levels
        var ceo = CreateTestEmployee("CEO001");
        var vpEngineering = CreateTestEmployee("VP001");
        var vpProduct = CreateTestEmployee("VP002");
        var engineeringManager = CreateTestEmployee("MGR001");
        var productManager = CreateTestEmployee("PMGR001");
        var engineer = CreateTestEmployee("EMP001");

        // Primary reporting chain: Engineer -> Engineering Manager -> VP Engineering -> CEO
        vpEngineering.ManagerId = ceo.Id;
        engineeringManager.ManagerId = vpEngineering.Id;
        engineer.ManagerId = engineeringManager.Id;

        // Dotted line: Engineer also reports to Product Manager for project work
        engineer.DottedLineManagerId = productManager.Id;

        // Product Manager reports to VP Product
        vpProduct.ManagerId = ceo.Id;
        productManager.ManagerId = vpProduct.Id;

        Context.Employees.AddRange(
            ceo, vpEngineering, vpProduct, engineeringManager, productManager, engineer);
        await Context.SaveChangesAsync();

        // Act
        var engineerWithManagers = await Context.Employees
            .Include(e => e.Manager)
            .Include(e => e.DottedLineManager)
            .FirstAsync(e => e.Id == engineer.Id);

        // Assert
        engineerWithManagers.Manager.Should().NotBeNull();
        engineerWithManagers.Manager!.Id.Should().Be(engineeringManager.Id);
        engineerWithManagers.DottedLineManager.Should().NotBeNull();
        engineerWithManagers.DottedLineManager!.Id.Should().Be(productManager.Id);
    }

    /// <summary>
    /// Helper method to create a test employee
    /// </summary>
    private Employee CreateTestEmployee(string employeeNumber)
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
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
    }
}
