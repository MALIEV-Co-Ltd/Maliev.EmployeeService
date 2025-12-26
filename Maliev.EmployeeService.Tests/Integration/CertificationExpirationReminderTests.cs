using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.EmployeeService.Infrastructure.BackgroundServices;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for certification expiration reminder background job (T290)
/// Tests the complete workflow of detecting expiring certifications and triggering reminders
/// </summary>
public class CertificationExpirationReminderTests : PostgreSqlIntegrationTestBase
{


    [Fact]
    public async Task GetExpiringCertifications_60DaysThreshold_ReturnsOnlyCertificationsExpiring60DaysOut()
    {
        // Arrange
        var employee = CreateEmployee("EMP001", "John", "Doe");

        // Create certifications with various expiration dates
        var expiring60Days = CreateTrainingRecord(employee.Id, "60 Day Cert", DateTime.UtcNow.AddDays(60));
        var expiring59Days = CreateTrainingRecord(employee.Id, "59 Day Cert", DateTime.UtcNow.AddDays(59));
        var expiring61Days = CreateTrainingRecord(employee.Id, "61 Day Cert", DateTime.UtcNow.AddDays(61));
        var expiring30Days = CreateTrainingRecord(employee.Id, "30 Day Cert", DateTime.UtcNow.AddDays(30));

        Context.Employees.Add(employee);
        Context.TrainingRecords.AddRange(expiring60Days, expiring59Days, expiring61Days, expiring30Days);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);

        // Act
        var expiringCerts = await trainingRepository.GetExpiringCertificationsAsync(60, CancellationToken.None);

        // Assert
        var certList = expiringCerts.ToList();
        Assert.NotEmpty(certList);
        // Should include certifications expiring within 60 days (60, 59, 30 days)
        Assert.Contains(certList, c => c.CourseName == "60 Day Cert");
        Assert.Contains(certList, c => c.CourseName == "59 Day Cert");
        Assert.Contains(certList, c => c.CourseName == "30 Day Cert");
        // Should NOT include certifications expiring beyond 60 days
        Assert.DoesNotContain(certList, c => c.CourseName == "61 Day Cert");
    }

    [Fact]
    public async Task GetExpiringCertifications_30DaysThreshold_ReturnsOnlyCertificationsExpiring30DaysOut()
    {
        // Arrange
        var employee = CreateEmployee("EMP002", "Jane", "Smith");

        var expiring30Days = CreateTrainingRecord(employee.Id, "30 Day Cert", DateTime.UtcNow.AddDays(30));
        var expiring29Days = CreateTrainingRecord(employee.Id, "29 Day Cert", DateTime.UtcNow.AddDays(29));
        var expiring31Days = CreateTrainingRecord(employee.Id, "31 Day Cert", DateTime.UtcNow.AddDays(31));
        var expiring14Days = CreateTrainingRecord(employee.Id, "14 Day Cert", DateTime.UtcNow.AddDays(14));

        Context.Employees.Add(employee);
        Context.TrainingRecords.AddRange(expiring30Days, expiring29Days, expiring31Days, expiring14Days);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);

        // Act
        var expiringCerts = await trainingRepository.GetExpiringCertificationsAsync(30, CancellationToken.None);

        // Assert
        var certList = expiringCerts.ToList();
        Assert.Contains(certList, c => c.CourseName == "30 Day Cert");
        Assert.Contains(certList, c => c.CourseName == "29 Day Cert");
        Assert.Contains(certList, c => c.CourseName == "14 Day Cert");
        Assert.DoesNotContain(certList, c => c.CourseName == "31 Day Cert");
    }

    [Fact]
    public async Task GetExpiringCertifications_14DaysThreshold_ReturnsOnlyUrgentExpirations()
    {
        // Arrange
        var employee = CreateEmployee("EMP003", "Bob", "Johnson");

        var expiring14Days = CreateTrainingRecord(employee.Id, "14 Day Cert", DateTime.UtcNow.AddDays(14));
        var expiring7Days = CreateTrainingRecord(employee.Id, "7 Day Cert", DateTime.UtcNow.AddDays(7));
        var expiring1Day = CreateTrainingRecord(employee.Id, "1 Day Cert", DateTime.UtcNow.AddDays(1));
        var expiring15Days = CreateTrainingRecord(employee.Id, "15 Day Cert", DateTime.UtcNow.AddDays(15));

        Context.Employees.Add(employee);
        Context.TrainingRecords.AddRange(expiring14Days, expiring7Days, expiring1Day, expiring15Days);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);

        // Act
        var expiringCerts = await trainingRepository.GetExpiringCertificationsAsync(14, CancellationToken.None);

        // Assert
        var certList = expiringCerts.ToList();
        Assert.Contains(certList, c => c.CourseName == "14 Day Cert");
        Assert.Contains(certList, c => c.CourseName == "7 Day Cert");
        Assert.Contains(certList, c => c.CourseName == "1 Day Cert");
        Assert.DoesNotContain(certList, c => c.CourseName == "15 Day Cert");
    }

    [Fact]
    public async Task GetExpiringCertifications_WithExpiredCertifications_ExcludesExpiredOnes()
    {
        // Arrange
        var employee = CreateEmployee("EMP004", "Alice", "Brown");

        var expiring30Days = CreateTrainingRecord(employee.Id, "Expiring", DateTime.UtcNow.AddDays(30));
        var alreadyExpired = CreateTrainingRecord(employee.Id, "Already Expired", DateTime.UtcNow.AddDays(-5));
        alreadyExpired.UpdateStatus(); // Mark as expired

        Context.Employees.Add(employee);
        Context.TrainingRecords.AddRange(expiring30Days, alreadyExpired);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);

        // Act
        var expiringCerts = await trainingRepository.GetExpiringCertificationsAsync(30, CancellationToken.None);

        // Assert
        var certList = expiringCerts.ToList();
        Assert.Contains(certList, c => c.CourseName == "Expiring");
        Assert.DoesNotContain(certList, c => c.CourseName == "Already Expired");
    }

    [Fact]
    public async Task GetExpiringCertifications_WithNonExpiringCertifications_ExcludesLongValidOnes()
    {
        // Arrange
        var employee = CreateEmployee("EMP005", "Charlie", "Davis");

        var expiring30Days = CreateTrainingRecord(employee.Id, "Soon Expiring", DateTime.UtcNow.AddDays(30));
        var longValid = CreateTrainingRecord(employee.Id, "Long Valid", DateTime.UtcNow.AddYears(2));

        Context.Employees.Add(employee);
        Context.TrainingRecords.AddRange(expiring30Days, longValid);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);

        // Act
        var expiringCerts = await trainingRepository.GetExpiringCertificationsAsync(60, CancellationToken.None);

        // Assert
        var certList = expiringCerts.ToList();
        Assert.Contains(certList, c => c.CourseName == "Soon Expiring");
        Assert.DoesNotContain(certList, c => c.CourseName == "Long Valid");
    }

    [Fact]
    public async Task GetExpiringCertifications_MultipleEmployees_ReturnsAllExpiringCertifications()
    {
        // Arrange
        var employee1 = CreateEmployee("EMP001", "Diana", "Evans");
        var employee2 = CreateEmployee("EMP002", "Frank", "Garcia");

        var cert1 = CreateTrainingRecord(employee1.Id, "Employee 1 Cert", DateTime.UtcNow.AddDays(30));
        var cert2 = CreateTrainingRecord(employee2.Id, "Employee 2 Cert", DateTime.UtcNow.AddDays(25));

        Context.Employees.AddRange(employee1, employee2);
        Context.TrainingRecords.AddRange(cert1, cert2);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);

        // Act
        var expiringCerts = await trainingRepository.GetExpiringCertificationsAsync(30, CancellationToken.None);

        // Assert
        var certList = expiringCerts.ToList();
        Assert.Equal(2, certList.Count());
        Assert.Contains(certList, c => c.CourseName == "Employee 1 Cert");
        Assert.Contains(certList, c => c.CourseName == "Employee 2 Cert");
    }

    [Fact]
    public async Task GetExpiringCertifications_WithEmployeeData_IncludesEmployeeInformation()
    {
        // Arrange
        var employee = CreateEmployee("EMP006", "George", "Harris");

        var cert = CreateTrainingRecord(employee.Id, "Test Cert", DateTime.UtcNow.AddDays(30));

        Context.Employees.Add(employee);
        Context.TrainingRecords.Add(cert);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);

        // Act
        var expiringCerts = await trainingRepository.GetExpiringCertificationsAsync(30, CancellationToken.None);

        // Assert
        var certList = expiringCerts.ToList();
        Assert.Single(certList);
        var foundCert = certList.First();
        Assert.NotNull(foundCert.Employee);
        Assert.Equal("EMP006", foundCert.Employee.EmployeeNumber);
    }

    [Fact]
    public async Task GetExpiringCertifications_OrderedByExpirationDate_ReturnsInCorrectOrder()
    {
        // Arrange
        var employee = CreateEmployee("EMP007", "Helen", "Ivanov");

        var cert1 = CreateTrainingRecord(employee.Id, "Expires in 30 days", DateTime.UtcNow.AddDays(30));
        var cert2 = CreateTrainingRecord(employee.Id, "Expires in 7 days", DateTime.UtcNow.AddDays(7));
        var cert3 = CreateTrainingRecord(employee.Id, "Expires in 14 days", DateTime.UtcNow.AddDays(14));

        Context.Employees.Add(employee);
        Context.TrainingRecords.AddRange(cert1, cert2, cert3);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);

        // Act
        var expiringCerts = await trainingRepository.GetExpiringCertificationsAsync(60, CancellationToken.None);

        // Assert
        var certList = expiringCerts.ToList();
        Assert.Equal(3, certList.Count());
        // Should be ordered by expiration date (earliest first)
        Assert.Equal("Expires in 7 days", certList[0].CourseName);
        Assert.Equal("Expires in 14 days", certList[1].CourseName);
        Assert.Equal("Expires in 30 days", certList[2].CourseName);
    }

    [Fact]
    public async Task GetExpiringCertifications_WithNoExpirationDate_ExcludesPermanentCertifications()
    {
        // Arrange
        var employee = CreateEmployee("EMP008", "Ivan", "Johnson");

        var expiring = CreateTrainingRecord(employee.Id, "Has Expiration", DateTime.UtcNow.AddDays(30));
        var permanent = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            CourseName = "Permanent Cert",
            CompletionDate = DateTime.UtcNow.AddMonths(-6),
            ExpirationDate = null, // No expiration
            TrainingType = TrainingType.Voluntary,
            Status = CertificationStatus.Valid,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        Context.Employees.Add(employee);
        Context.TrainingRecords.AddRange(expiring, permanent);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);

        // Act
        var expiringCerts = await trainingRepository.GetExpiringCertificationsAsync(60, CancellationToken.None);

        // Assert
        var certList = expiringCerts.ToList();
        Assert.Contains(certList, c => c.CourseName == "Has Expiration");
        Assert.DoesNotContain(certList, c => c.CourseName == "Permanent Cert");
    }

    [Fact]
    public async Task GetExpiringCertifications_BoundaryCase_ExactlyAtThreshold()
    {
        // Arrange
        var employee = CreateEmployee("EMP009", "Kate", "Lee");

        var exactlyAtThreshold = CreateTrainingRecord(employee.Id, "Exactly 30", DateTime.UtcNow.AddDays(30));
        var justBeyondThreshold = CreateTrainingRecord(employee.Id, "31 Days", DateTime.UtcNow.AddDays(31));

        Context.Employees.Add(employee);
        Context.TrainingRecords.AddRange(exactlyAtThreshold, justBeyondThreshold);
        await Context.SaveChangesAsync();

        var trainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository(Context);

        // Act
        var expiringCerts = await trainingRepository.GetExpiringCertificationsAsync(30, CancellationToken.None);

        // Assert
        var certList = expiringCerts.ToList();
        Assert.Contains(certList, c => c.CourseName == "Exactly 30");
        Assert.DoesNotContain(certList, c => c.CourseName == "31 Days");
    }

    // Helper methods
    private Employee CreateEmployee(string employeeNumber, string firstName, string lastName)
    {
        return new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = employeeNumber,
            LegalName = new LegalName(firstName, lastName, null),
            ContactInformation = new ContactInformation($"{firstName.ToLower()}.{lastName.ToLower()}@example.com", "555-0100"),
            EmploymentType = EmploymentType.FullTime,
            EmploymentStatus = EmploymentStatus.Active,
            JobTitle = "Test Position",
            StartDate = DateTime.UtcNow.AddMonths(-6).Date,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

    private TrainingRecord CreateTrainingRecord(Guid employeeId, string courseName, DateTime expirationDate)
    {
        var record = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            CourseName = courseName,
            CompletionDate = DateTime.UtcNow.AddYears(-1),
            ExpirationDate = expirationDate,
            TrainingType = TrainingType.Mandatory,
            Status = CertificationStatus.Valid,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        record.UpdateStatus(); // Calculate correct status based on expiration
        return record;
    }

}
