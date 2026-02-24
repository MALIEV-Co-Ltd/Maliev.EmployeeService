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
[Collection("IntegrationTests")]
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
        Assert.Equal(primaryManager.Id, loadedEmployee.ManagerId);
        Assert.Equal(dottedLineManager.Id, loadedEmployee.DottedLineManagerId);
        Assert.NotNull(loadedEmployee.Manager);
        Assert.NotNull(loadedEmployee.DottedLineManager);
        Assert.Equal(primaryManager.Id, loadedEmployee.Manager!.Id);
        Assert.Equal(dottedLineManager.Id, loadedEmployee.DottedLineManager!.Id);
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
        Assert.Equal(2, loadedManager.DirectReports.Count());
        Assert.Equal(2, loadedManager.DottedLineReports.Count());
        Assert.Contains(loadedManager.DirectReports, e => e.Id == directReport1.Id);
        Assert.Contains(loadedManager.DirectReports, e => e.Id == directReport2.Id);
        Assert.Contains(loadedManager.DottedLineReports, e => e.Id == dottedLineReport1.Id);
        Assert.Contains(loadedManager.DottedLineReports, e => e.Id == dottedLineReport2.Id);
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
        Assert.Null(loadedEmployee.ManagerId);
        Assert.Null(loadedEmployee.DottedLineManagerId);
        Assert.Null(loadedEmployee.Manager);
        Assert.Null(loadedEmployee.DottedLineManager);
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
        Assert.NotNull(employeeWithManagers.Manager);
        Assert.Equal("Engineering Department Head", employeeWithManagers.Manager!.JobTitle);
        Assert.NotNull(employeeWithManagers.DottedLineManager);
        Assert.Equal("Product Manager", employeeWithManagers.DottedLineManager!.JobTitle);

        Assert.Single(functionalManagerWithReports.DirectReports);
        Assert.Equal(employee.Id, functionalManagerWithReports.DirectReports.First().Id);

        Assert.Single(projectManagerWithDottedLineReports.DottedLineReports);
        Assert.Equal(employee.Id, projectManagerWithDottedLineReports.DottedLineReports.First().Id);
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
        Assert.Equal(primaryManager.Id, updatedEmployee.ManagerId); // Primary manager should remain unchanged
        Assert.Equal(newDottedLineManager.Id, updatedEmployee.DottedLineManagerId); // Dotted line manager should be updated
        Assert.Equal(primaryManager.Id, updatedEmployee.Manager!.Id);
        Assert.Equal(newDottedLineManager.Id, updatedEmployee.DottedLineManager!.Id);
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
        Assert.Equal(primaryManager.Id, updatedEmployee.ManagerId); // Primary manager should remain unchanged
        Assert.Null(updatedEmployee.DottedLineManagerId); // Dotted line manager should be removed
        Assert.Equal(primaryManager.Id, updatedEmployee.Manager!.Id);
        Assert.Null(updatedEmployee.DottedLineManager);
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
        Assert.NotNull(engineerWithManagers.Manager);
        Assert.Equal(engineeringManager.Id, engineerWithManagers.Manager!.Id);
        Assert.NotNull(engineerWithManagers.DottedLineManager);
        Assert.Equal(productManager.Id, engineerWithManagers.DottedLineManager!.Id);
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
            CreatedDate = DateTime.UtcNow
        };
    }
}
