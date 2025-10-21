using FluentAssertions;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for training compliance report generation (T289)
/// Tests the complete workflow of generating compliance reports with real data
/// </summary>
public class TrainingComplianceReportTests : PostgreSqlIntegrationTestBase
{


    [Fact]
    public async Task GenerateComplianceReport_WithCompliantEmployees_Returns100PercentCompliance()
    {
        // Arrange
        var department = CreateDepartment("Engineering", "ENG");
        var employee1 = CreateEmployee("EMP001", "John", "Doe", department.Id, EmploymentStatus.Active);
        var employee2 = CreateEmployee("EMP002", "Jane", "Smith", department.Id, EmploymentStatus.Active);

        // Both employees have valid certifications
        var training1 = CreateTrainingRecord(employee1.Id, "Safety Training", DateTime.UtcNow.AddMonths(-6),
            DateTime.UtcNow.AddMonths(6), CertificationStatus.Valid);
        var training2 = CreateTrainingRecord(employee2.Id, "Code of Conduct", DateTime.UtcNow.AddMonths(-3),
            DateTime.UtcNow.AddYears(1), CertificationStatus.Valid);

        Context.Departments.Add(department);
        Context.Employees.AddRange(employee1, employee2);
        Context.TrainingRecords.AddRange(training1, training2);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);
        var employeeRepository = new Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository(Context);
        var departmentRepository = new Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository(Context);
        var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<GetTrainingComplianceReportQueryHandler>>();
        var handler = new GetTrainingComplianceReportQueryHandler(
            employeeRepository,
            trainingRepository,
            departmentRepository,
            mockLogger.Object);
        var query = new GetTrainingComplianceReportQuery();

        // Act
        var report = await handler.HandleAsync(query);

        // Assert
        report.Should().NotBeNull();
        report.TotalEmployees.Should().Be(2);
        report.EmployeesWithExpiredCertifications.Should().Be(0);
        report.EmployeesWithExpiringCertifications.Should().Be(0);
        report.ComplianceRate.Should().Be(100m);
        report.EmployeeDetails.Should().HaveCount(2);
        report.EmployeeDetails.Should().OnlyContain(e => e.IsCompliant);
    }

    [Fact]
    public async Task GenerateComplianceReport_WithExpiredCertifications_CalculatesCorrectComplianceRate()
    {
        // Arrange
        var department = CreateDepartment("IT", "IT");
        var compliantEmployee = CreateEmployee("EMP001", "Alice", "Compliant", department.Id, EmploymentStatus.Active);
        var nonCompliantEmployee = CreateEmployee("EMP002", "Bob", "NonCompliant", department.Id, EmploymentStatus.Active);

        var validTraining = CreateTrainingRecord(compliantEmployee.Id, "Valid Training", DateTime.UtcNow.AddMonths(-6),
            DateTime.UtcNow.AddYears(1), CertificationStatus.Valid);

        var expiredTraining = CreateTrainingRecord(nonCompliantEmployee.Id, "Expired Training", DateTime.UtcNow.AddYears(-2),
            DateTime.UtcNow.AddMonths(-3), CertificationStatus.Expired);
        expiredTraining.UpdateStatus(); // Ensure status is Expired

        Context.Departments.Add(department);
        Context.Employees.AddRange(compliantEmployee, nonCompliantEmployee);
        Context.TrainingRecords.AddRange(validTraining, expiredTraining);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);
        var employeeRepository = new Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository(Context);
        var departmentRepository = new Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository(Context);
        var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<GetTrainingComplianceReportQueryHandler>>();
        var handler = new GetTrainingComplianceReportQueryHandler(
            employeeRepository,
            trainingRepository,
            departmentRepository,
            mockLogger.Object);
        var query = new GetTrainingComplianceReportQuery();

        // Act
        var report = await handler.HandleAsync(query);

        // Assert
        report.TotalEmployees.Should().Be(2);
        report.EmployeesWithExpiredCertifications.Should().Be(1);
        report.ComplianceRate.Should().Be(50m);

        var compliantDetail = report.EmployeeDetails.First(e => e.EmployeeNumber == "EMP001");
        compliantDetail.IsCompliant.Should().BeTrue();
        compliantDetail.ExpiredCertifications.Should().Be(0);

        var nonCompliantDetail = report.EmployeeDetails.First(e => e.EmployeeNumber == "EMP002");
        nonCompliantDetail.IsCompliant.Should().BeFalse();
        nonCompliantDetail.ExpiredCertifications.Should().Be(1);
    }

    [Fact]
    public async Task GenerateComplianceReport_WithExpiringCertifications_IncludesExpiringCount()
    {
        // Arrange
        var department = CreateDepartment("HR", "HR");
        var employee = CreateEmployee("EMP001", "Charlie", "Worker", department.Id, EmploymentStatus.Active);

        var expiringTraining = CreateTrainingRecord(employee.Id, "Expiring Soon", DateTime.UtcNow.AddYears(-1),
            DateTime.UtcNow.AddDays(30), CertificationStatus.Expiring);
        expiringTraining.UpdateStatus(); // Ensure status is Expiring

        Context.Departments.Add(department);
        Context.Employees.Add(employee);
        Context.TrainingRecords.Add(expiringTraining);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);
        var employeeRepository = new Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository(Context);
        var departmentRepository = new Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository(Context);
        var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<GetTrainingComplianceReportQueryHandler>>();
        var handler = new GetTrainingComplianceReportQueryHandler(
            employeeRepository,
            trainingRepository,
            departmentRepository,
            mockLogger.Object);
        var query = new GetTrainingComplianceReportQuery();

        // Act
        var report = await handler.HandleAsync(query);

        // Assert
        report.EmployeesWithExpiringCertifications.Should().Be(1);
        report.EmployeeDetails.First().ExpiringCertifications.Should().Be(1);
        report.EmployeeDetails.First().IsCompliant.Should().BeTrue(); // Still compliant, just expiring
    }

    [Fact]
    public async Task GenerateComplianceReport_FilterByDepartment_OnlyIncludesDepartmentEmployees()
    {
        // Arrange
        var department1 = CreateDepartment("Engineering", "ENG");
        var department2 = CreateDepartment("Sales", "SAL");

        var empInDept1 = CreateEmployee("EMP001", "Alice", "Engineer", department1.Id, EmploymentStatus.Active);
        var empInDept2 = CreateEmployee("EMP002", "Bob", "Salesperson", department2.Id, EmploymentStatus.Active);

        Context.Departments.AddRange(department1, department2);
        Context.Employees.AddRange(empInDept1, empInDept2);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);
        var employeeRepository = new Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository(Context);
        var departmentRepository = new Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository(Context);
        var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<GetTrainingComplianceReportQueryHandler>>();
        var handler = new GetTrainingComplianceReportQueryHandler(
            employeeRepository,
            trainingRepository,
            departmentRepository,
            mockLogger.Object);
        var query = new GetTrainingComplianceReportQuery { DepartmentId = department1.Id };

        // Act
        var report = await handler.HandleAsync(query);

        // Assert
        report.TotalEmployees.Should().Be(1);
        report.EmployeeDetails.Should().HaveCount(1);
        report.EmployeeDetails.First().EmployeeNumber.Should().Be("EMP001");
        report.EmployeeDetails.First().Department.Should().Be("Engineering");
    }

    [Fact]
    public async Task GenerateComplianceReport_FilterByTrainingType_OnlyIncludesSpecifiedType()
    {
        // Arrange
        var department = CreateDepartment("Operations", "OPS");
        var employee = CreateEmployee("EMP001", "Diana", "Operator", department.Id, EmploymentStatus.Active);

        var mandatoryTraining = CreateTrainingRecord(employee.Id, "Mandatory Safety", DateTime.UtcNow.AddMonths(-6),
            DateTime.UtcNow.AddMonths(6), CertificationStatus.Valid, TrainingType.Mandatory);

        var voluntaryTraining = CreateTrainingRecord(employee.Id, "Voluntary Skills", DateTime.UtcNow.AddMonths(-3),
            null, CertificationStatus.Valid, TrainingType.Voluntary);

        Context.Departments.Add(department);
        Context.Employees.Add(employee);
        Context.TrainingRecords.AddRange(mandatoryTraining, voluntaryTraining);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);
        var employeeRepository = new Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository(Context);
        var departmentRepository = new Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository(Context);
        var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<GetTrainingComplianceReportQueryHandler>>();
        var handler = new GetTrainingComplianceReportQueryHandler(
            employeeRepository,
            trainingRepository,
            departmentRepository,
            mockLogger.Object);
        var query = new GetTrainingComplianceReportQuery { TrainingType = TrainingType.Mandatory };

        // Act
        var report = await handler.HandleAsync(query);

        // Assert
        report.EmployeeDetails.First().TotalTrainings.Should().Be(1); // Only mandatory
    }

    [Fact]
    public async Task GenerateComplianceReport_OnlyOverdueFilter_ExcludesCompliantEmployees()
    {
        // Arrange
        var department = CreateDepartment("Finance", "FIN");
        var compliantEmp = CreateEmployee("EMP001", "Eva", "Compliant", department.Id, EmploymentStatus.Active);
        var nonCompliantEmp = CreateEmployee("EMP002", "Frank", "NonCompliant", department.Id, EmploymentStatus.Active);

        var validTraining = CreateTrainingRecord(compliantEmp.Id, "Valid", DateTime.UtcNow.AddMonths(-6),
            DateTime.UtcNow.AddYears(1), CertificationStatus.Valid);

        var expiredTraining = CreateTrainingRecord(nonCompliantEmp.Id, "Expired", DateTime.UtcNow.AddYears(-2),
            DateTime.UtcNow.AddDays(-30), CertificationStatus.Expired);
        expiredTraining.UpdateStatus();

        Context.Departments.Add(department);
        Context.Employees.AddRange(compliantEmp, nonCompliantEmp);
        Context.TrainingRecords.AddRange(validTraining, expiredTraining);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);
        var employeeRepository = new Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository(Context);
        var departmentRepository = new Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository(Context);
        var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<GetTrainingComplianceReportQueryHandler>>();
        var handler = new GetTrainingComplianceReportQueryHandler(
            employeeRepository,
            trainingRepository,
            departmentRepository,
            mockLogger.Object);
        var query = new GetTrainingComplianceReportQuery { OnlyOverdue = true };

        // Act
        var report = await handler.HandleAsync(query);

        // Assert
        report.EmployeeDetails.Should().HaveCount(1);
        report.EmployeeDetails.First().EmployeeNumber.Should().Be("EMP002");
        report.EmployeeDetails.First().IsCompliant.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateComplianceReport_WithInactiveEmployees_OnlyIncludesActiveEmployees()
    {
        // Arrange
        var department = CreateDepartment("Admin", "ADM");
        var activeEmp = CreateEmployee("EMP001", "George", "Active", department.Id, EmploymentStatus.Active);
        var inactiveEmp = CreateEmployee("EMP002", "Hannah", "Inactive", department.Id, EmploymentStatus.Terminated);

        Context.Departments.Add(department);
        Context.Employees.AddRange(activeEmp, inactiveEmp);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);
        var employeeRepository = new Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository(Context);
        var departmentRepository = new Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository(Context);
        var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<GetTrainingComplianceReportQueryHandler>>();
        var handler = new GetTrainingComplianceReportQueryHandler(
            employeeRepository,
            trainingRepository,
            departmentRepository,
            mockLogger.Object);
        var query = new GetTrainingComplianceReportQuery();

        // Act
        var report = await handler.HandleAsync(query);

        // Assert
        report.TotalEmployees.Should().Be(1);
        report.EmployeeDetails.Should().HaveCount(1);
        report.EmployeeDetails.First().EmployeeNumber.Should().Be("EMP001");
    }

    [Fact]
    public async Task GenerateComplianceReport_WithMultipleCertificationsPerEmployee_AggregatesCorrectly()
    {
        // Arrange
        var department = CreateDepartment("Security", "SEC");
        var employee = CreateEmployee("EMP001", "Ivan", "Security", department.Id, EmploymentStatus.Active);

        var validTraining = CreateTrainingRecord(employee.Id, "Valid Training", DateTime.UtcNow.AddMonths(-6),
            DateTime.UtcNow.AddYears(1), CertificationStatus.Valid);

        var expiringTraining = CreateTrainingRecord(employee.Id, "Expiring Training", DateTime.UtcNow.AddYears(-1),
            DateTime.UtcNow.AddDays(30), CertificationStatus.Expiring);
        expiringTraining.UpdateStatus();

        var expiredTraining = CreateTrainingRecord(employee.Id, "Expired Training", DateTime.UtcNow.AddYears(-3),
            DateTime.UtcNow.AddDays(-60), CertificationStatus.Expired);
        expiredTraining.UpdateStatus();

        Context.Departments.Add(department);
        Context.Employees.Add(employee);
        Context.TrainingRecords.AddRange(validTraining, expiringTraining, expiredTraining);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);
        var employeeRepository = new Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository(Context);
        var departmentRepository = new Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository(Context);
        var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<GetTrainingComplianceReportQueryHandler>>();
        var handler = new GetTrainingComplianceReportQueryHandler(
            employeeRepository,
            trainingRepository,
            departmentRepository,
            mockLogger.Object);
        var query = new GetTrainingComplianceReportQuery();

        // Act
        var report = await handler.HandleAsync(query);

        // Assert
        var employeeDetail = report.EmployeeDetails.First();
        employeeDetail.TotalTrainings.Should().Be(3);
        employeeDetail.ExpiredCertifications.Should().Be(1);
        employeeDetail.ExpiringCertifications.Should().Be(1);
        employeeDetail.IsCompliant.Should().BeFalse(); // Has expired certification
    }

    [Fact]
    public async Task GenerateComplianceReport_WithNoEmployees_ReturnsEmptyReport()
    {
        // Arrange
        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);
        var employeeRepository = new Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository(Context);
        var departmentRepository = new Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository(Context);
        var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<GetTrainingComplianceReportQueryHandler>>();
        var handler = new GetTrainingComplianceReportQueryHandler(
            employeeRepository,
            trainingRepository,
            departmentRepository,
            mockLogger.Object);
        var query = new GetTrainingComplianceReportQuery();

        // Act
        var report = await handler.HandleAsync(query);

        // Assert
        report.TotalEmployees.Should().Be(0);
        report.ComplianceRate.Should().Be(100m); // Default to 100% when no employees
        report.EmployeeDetails.Should().BeEmpty();
    }

    // Helper methods
    private Department CreateDepartment(string name, string code)
    {
        return new Department
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = $"{name} Department",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

    private Employee CreateEmployee(string employeeNumber, string firstName, string lastName, Guid departmentId, EmploymentStatus status)
    {
        return new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = employeeNumber,
            LegalName = new LegalName(firstName, lastName, null),
            ContactInformation = new ContactInformation($"{firstName.ToLower()}.{lastName.ToLower()}@example.com", "555-0100"),
            EmploymentType = EmploymentType.FullTime,
            EmploymentStatus = status,
            JobTitle = "Test Position",
            DepartmentId = departmentId,
            StartDate = DateTime.UtcNow.AddMonths(-6).Date,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

    private TrainingRecord CreateTrainingRecord(Guid employeeId, string courseName, DateTime completionDate,
        DateTime? expirationDate, CertificationStatus status, TrainingType trainingType = TrainingType.Mandatory)
    {
        return new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            CourseName = courseName,
            CompletionDate = completionDate,
            ExpirationDate = expirationDate,
            TrainingType = trainingType,
            Status = status,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

}
