using FluentAssertions;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.EmployeeService.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for Work Authorization feature
/// Tests work authorization creation, expiration detection, and compliance reporting
/// Covers T343, T344, T345
/// </summary>
public class WorkAuthorizationIntegrationTests : PostgreSqlIntegrationTestBase
{
    [Fact]
    public async Task RecordWorkAuthorization_WithValidData_ShouldPersistToDatabase()
    {
        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            LegalName = new LegalName("John", "Doe"),
            ContactInformation = new ContactInformation { WorkEmail = "john.doe@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        var workAuthRepository = new WorkAuthorizationRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var documentRepository = new DocumentRepository(Context);
        var unitOfWork = new Infrastructure.Data.UnitOfWork(Context);

        var handler = new RecordWorkAuthorizationCommandHandler(
            workAuthRepository,
            employeeRepository,
            documentRepository,
            unitOfWork,
            NullLogger<RecordWorkAuthorizationCommandHandler>.Instance);

        var command = new RecordWorkAuthorizationCommand
        {
            EmployeeId = employee.Id,
            AuthorizationType = AuthorizationType.WorkPermit,
            DocumentNumber = "WP-TH-2024-12345",
            IssueDate = DateTime.UtcNow.AddMonths(-1),
            ExpirationDate = DateTime.UtcNow.AddYears(2),
            IssuingAuthority = "Thai Ministry of Labour",
            SponsorshipStatus = SponsorshipStatus.Approved,
            Notes = "Initial work permit for software engineer"
        };

        // Act
        var authorizationId = await handler.HandleAsync(command);

        // Assert
        authorizationId.Should().NotBeEmpty();

        // Clear context to ensure fresh read from database
        Context.ChangeTracker.Clear();

        var savedAuthorization = await workAuthRepository.GetByIdAsync(authorizationId);
        savedAuthorization.Should().NotBeNull();
        savedAuthorization!.EmployeeId.Should().Be(employee.Id);
        savedAuthorization.AuthorizationType.Should().Be(AuthorizationType.WorkPermit);
        savedAuthorization.DocumentNumber.Should().Be("WP-TH-2024-12345");
        savedAuthorization.IssuingAuthority.Should().Be("Thai Ministry of Labour");
        savedAuthorization.SponsorshipStatus.Should().Be(SponsorshipStatus.Approved);
        savedAuthorization.IsActive.Should().BeTrue();
        savedAuthorization.ExpirationDate.Should().BeCloseTo(DateTime.UtcNow.AddYears(2), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RecordWorkAuthorization_WithDocumentReference_ShouldLinkToDocument()
    {
        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP002",
            LegalName = new LegalName("Jane", "Smith"),
            ContactInformation = new ContactInformation { WorkEmail = "jane.smith@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddMonths(-6),
            CreatedDate = DateTime.UtcNow.AddMonths(-6)
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        // Create a work permit document
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            DocumentType = DocumentType.WorkPermit,
            FileName = "work_permit_scan.pdf",
            UploadDate = DateTime.UtcNow.AddDays(-7),
            UploadedBy = employee.Id,
            StoragePath = "/documents/work_permits/WP-TH-2024-12345.pdf",
            FileSizeBytes = 2048000,
            ContentType = "application/pdf",
            VersionNumber = 1,
            CreatedDate = DateTime.UtcNow.AddDays(-7)
        };

        Context.Documents.Add(document);
        await Context.SaveChangesAsync();

        var workAuthRepository = new WorkAuthorizationRepository(Context);
        var employeeRepository = new EmployeeRepository(Context);
        var documentRepository = new DocumentRepository(Context);
        var unitOfWork = new Infrastructure.Data.UnitOfWork(Context);

        var handler = new RecordWorkAuthorizationCommandHandler(
            workAuthRepository,
            employeeRepository,
            documentRepository,
            unitOfWork,
            NullLogger<RecordWorkAuthorizationCommandHandler>.Instance);

        var command = new RecordWorkAuthorizationCommand
        {
            EmployeeId = employee.Id,
            AuthorizationType = AuthorizationType.WorkPermit,
            DocumentNumber = "WP-TH-2024-12345",
            IssueDate = DateTime.UtcNow.AddMonths(-1),
            ExpirationDate = DateTime.UtcNow.AddMonths(18),
            IssuingAuthority = "Thai Ministry of Labour",
            SponsorshipStatus = SponsorshipStatus.Approved,
            RightToWorkDocumentId = document.Id,
            Notes = "Work permit with scanned document"
        };

        // Act
        var authorizationId = await handler.HandleAsync(command);

        // Assert
        Context.ChangeTracker.Clear();

        var savedAuthorization = await workAuthRepository.GetByIdAsync(authorizationId);
        savedAuthorization.Should().NotBeNull();
        savedAuthorization!.RightToWorkDocumentId.Should().Be(document.Id);
    }

    [Fact]
    public async Task GetExpiringAuthorizations_ShouldReturnAuthorizationsWithinThreshold()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Create employees with different authorization statuses
        var employees = new List<Employee>();
        var authorizations = new List<WorkAuthorization>();

        // Employee 1: Authorization expiring in 30 days (should appear)
        var emp1 = CreateEmployee("EMP101", "Expiring", "Soon");
        employees.Add(emp1);
        authorizations.Add(new WorkAuthorization
        {
            Id = Guid.NewGuid(),
            EmployeeId = emp1.Id,
            AuthorizationType = AuthorizationType.WorkPermit,
            DocumentNumber = "WP-EXP-30",
            IssueDate = now.AddYears(-2),
            ExpirationDate = now.AddDays(30),
            IssuingAuthority = "Authority 1",
            SponsorshipStatus = SponsorshipStatus.Approved,
            IsActive = true,
            CreatedDate = now.AddYears(-2)
        });

        // Employee 2: Authorization expiring in 60 days (should appear)
        var emp2 = CreateEmployee("EMP102", "Expiring", "Later");
        employees.Add(emp2);
        authorizations.Add(new WorkAuthorization
        {
            Id = Guid.NewGuid(),
            EmployeeId = emp2.Id,
            AuthorizationType = AuthorizationType.Visa,
            DocumentNumber = "VISA-EXP-60",
            IssueDate = now.AddYears(-3),
            ExpirationDate = now.AddDays(60),
            IssuingAuthority = "Authority 2",
            SponsorshipStatus = SponsorshipStatus.Approved,
            IsActive = true,
            CreatedDate = now.AddYears(-3)
        });

        // Employee 3: Authorization expiring in 100 days (should NOT appear with 90 day threshold)
        var emp3 = CreateEmployee("EMP103", "Not", "Expiring");
        employees.Add(emp3);
        authorizations.Add(new WorkAuthorization
        {
            Id = Guid.NewGuid(),
            EmployeeId = emp3.Id,
            AuthorizationType = AuthorizationType.WorkPermit,
            DocumentNumber = "WP-SAFE-100",
            IssueDate = now.AddYears(-1),
            ExpirationDate = now.AddDays(100),
            IssuingAuthority = "Authority 3",
            SponsorshipStatus = SponsorshipStatus.Approved,
            IsActive = true,
            CreatedDate = now.AddYears(-1)
        });

        // Employee 4: Citizenship (no expiration, should NOT appear)
        var emp4 = CreateEmployee("EMP104", "Thai", "Citizen");
        employees.Add(emp4);
        authorizations.Add(new WorkAuthorization
        {
            Id = Guid.NewGuid(),
            EmployeeId = emp4.Id,
            AuthorizationType = AuthorizationType.Citizenship,
            DocumentNumber = "TH-CIT-123456",
            IssueDate = now.AddYears(-10),
            ExpirationDate = null,
            IssuingAuthority = "Thai Ministry of Interior",
            SponsorshipStatus = SponsorshipStatus.NotRequired,
            IsActive = true,
            CreatedDate = now.AddYears(-10)
        });

        Context.Employees.AddRange(employees);
        Context.WorkAuthorizations.AddRange(authorizations);
        await Context.SaveChangesAsync();

        var workAuthRepository = new WorkAuthorizationRepository(Context);

        // Act
        var expiringWithin90Days = await workAuthRepository.GetExpiringAsync(90);

        // Assert
        expiringWithin90Days.Should().HaveCount(2);
        expiringWithin90Days.Should().Contain(a => a.DocumentNumber == "WP-EXP-30");
        expiringWithin90Days.Should().Contain(a => a.DocumentNumber == "VISA-EXP-60");
        expiringWithin90Days.Should().NotContain(a => a.DocumentNumber == "WP-SAFE-100");
        expiringWithin90Days.Should().NotContain(a => a.DocumentNumber == "TH-CIT-123456");
    }

    [Fact]
    public async Task GetExpiredAuthorizations_ShouldReturnOnlyExpiredAuthorizations()
    {
        // Arrange
        var now = DateTime.UtcNow;

        var employees = new List<Employee>();
        var authorizations = new List<WorkAuthorization>();

        // Employee 1: Expired 10 days ago (should appear)
        var emp1 = CreateEmployee("EMP201", "Expired", "Recently");
        employees.Add(emp1);
        authorizations.Add(new WorkAuthorization
        {
            Id = Guid.NewGuid(),
            EmployeeId = emp1.Id,
            AuthorizationType = AuthorizationType.WorkPermit,
            DocumentNumber = "WP-EXPIRED-10",
            IssueDate = now.AddYears(-2),
            ExpirationDate = now.AddDays(-10),
            IssuingAuthority = "Authority 1",
            SponsorshipStatus = SponsorshipStatus.Approved,
            IsActive = true,
            CreatedDate = now.AddYears(-2)
        });

        // Employee 2: Expired 90 days ago (should appear)
        var emp2 = CreateEmployee("EMP202", "Expired", "LongAgo");
        employees.Add(emp2);
        authorizations.Add(new WorkAuthorization
        {
            Id = Guid.NewGuid(),
            EmployeeId = emp2.Id,
            AuthorizationType = AuthorizationType.Visa,
            DocumentNumber = "VISA-EXPIRED-90",
            IssueDate = now.AddYears(-3),
            ExpirationDate = now.AddDays(-90),
            IssuingAuthority = "Authority 2",
            SponsorshipStatus = SponsorshipStatus.Approved,
            IsActive = true,
            CreatedDate = now.AddYears(-3)
        });

        // Employee 3: Valid authorization (should NOT appear)
        var emp3 = CreateEmployee("EMP203", "Valid", "Authorization");
        employees.Add(emp3);
        authorizations.Add(new WorkAuthorization
        {
            Id = Guid.NewGuid(),
            EmployeeId = emp3.Id,
            AuthorizationType = AuthorizationType.WorkPermit,
            DocumentNumber = "WP-VALID",
            IssueDate = now.AddMonths(-6),
            ExpirationDate = now.AddYears(1),
            IssuingAuthority = "Authority 3",
            SponsorshipStatus = SponsorshipStatus.Approved,
            IsActive = true,
            CreatedDate = now.AddMonths(-6)
        });

        Context.Employees.AddRange(employees);
        Context.WorkAuthorizations.AddRange(authorizations);
        await Context.SaveChangesAsync();

        var workAuthRepository = new WorkAuthorizationRepository(Context);

        // Act
        var expired = await workAuthRepository.GetExpiredAsync();

        // Assert
        expired.Should().HaveCount(2);
        expired.Should().Contain(a => a.DocumentNumber == "WP-EXPIRED-10");
        expired.Should().Contain(a => a.DocumentNumber == "VISA-EXPIRED-90");
        expired.Should().NotContain(a => a.DocumentNumber == "WP-VALID");
    }

    [Fact]
    public async Task GetWorkAuthorizationComplianceReport_ShouldGenerateCompleteReport()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Create department for employee organization
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            Description = "Engineering Department",
            HeadcountLimit = 100,
            IsActive = true,
            CreatedDate = now
        };

        Context.Departments.Add(department);
        await Context.SaveChangesAsync();

        var employees = new List<Employee>();
        var authorizations = new List<WorkAuthorization>();

        // Scenario 1: Expiring soon (45 days)
        var emp1 = CreateEmployee("EMP301", "Expiring", "Soon", department.Id);
        employees.Add(emp1);
        authorizations.Add(new WorkAuthorization
        {
            Id = Guid.NewGuid(),
            EmployeeId = emp1.Id,
            AuthorizationType = AuthorizationType.WorkPermit,
            DocumentNumber = "WP-EXP-45",
            IssueDate = now.AddYears(-2),
            ExpirationDate = now.AddDays(45),
            IssuingAuthority = "Ministry of Labour",
            SponsorshipStatus = SponsorshipStatus.Approved,
            IsActive = true,
            CreatedDate = now.AddYears(-2)
        });

        // Scenario 2: Already expired
        var emp2 = CreateEmployee("EMP302", "Already", "Expired", department.Id);
        employees.Add(emp2);
        authorizations.Add(new WorkAuthorization
        {
            Id = Guid.NewGuid(),
            EmployeeId = emp2.Id,
            AuthorizationType = AuthorizationType.Visa,
            DocumentNumber = "VISA-EXPIRED",
            IssueDate = now.AddYears(-3),
            ExpirationDate = now.AddDays(-15),
            IssuingAuthority = "Embassy",
            SponsorshipStatus = SponsorshipStatus.Approved,
            IsActive = true,
            CreatedDate = now.AddYears(-3)
        });

        // Scenario 3: Citizenship (not required)
        var emp3 = CreateEmployee("EMP303", "Thai", "Citizen", department.Id);
        employees.Add(emp3);
        authorizations.Add(new WorkAuthorization
        {
            Id = Guid.NewGuid(),
            EmployeeId = emp3.Id,
            AuthorizationType = AuthorizationType.Citizenship,
            DocumentNumber = "TH-CIT",
            IssueDate = now.AddYears(-10),
            ExpirationDate = null,
            IssuingAuthority = "Ministry of Interior",
            SponsorshipStatus = SponsorshipStatus.NotRequired,
            IsActive = true,
            CreatedDate = now.AddYears(-10)
        });

        // Scenario 4: Pending sponsorship
        var emp4 = CreateEmployee("EMP304", "Pending", "Sponsorship", department.Id);
        employees.Add(emp4);
        authorizations.Add(new WorkAuthorization
        {
            Id = Guid.NewGuid(),
            EmployeeId = emp4.Id,
            AuthorizationType = AuthorizationType.Visa,
            DocumentNumber = "VISA-PENDING",
            IssueDate = now.AddMonths(-1),
            ExpirationDate = now.AddMonths(6),
            IssuingAuthority = "Embassy",
            SponsorshipStatus = SponsorshipStatus.Pending,
            IsActive = true,
            CreatedDate = now.AddMonths(-1)
        });

        Context.Employees.AddRange(employees);
        Context.WorkAuthorizations.AddRange(authorizations);
        await Context.SaveChangesAsync();

        var workAuthRepository = new WorkAuthorizationRepository(Context);
        var handler = new GetWorkAuthorizationComplianceReportQueryHandler(workAuthRepository);

        var query = new GetWorkAuthorizationComplianceReportQuery
        {
            DaysUntilExpiration = 90
        };

        // Act
        var report = await handler.HandleAsync(query);

        // Assert
        report.Should().NotBeNull();
        report.TotalActive.Should().Be(4); // All 4 authorizations are active
        report.ExpiringSoon.Should().Be(1); // Only WP-EXP-45 expires within 90 days
        report.Expired.Should().Be(1); // Only VISA-EXPIRED is expired

        report.ExpiringAuthorizations.Should().HaveCount(1);
        var expiringAuth = report.ExpiringAuthorizations.First();
        expiringAuth.EmployeeNumber.Should().Be("EMP301");
        expiringAuth.EmployeeName.Should().Be("Expiring Soon");
        expiringAuth.DocumentNumber.Should().Be("WP-EXP-45");
        expiringAuth.Department.Should().Be("Engineering");
        expiringAuth.DaysUntilExpiration.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(45);

        report.ExpiredAuthorizations.Should().HaveCount(1);
        var expiredAuth = report.ExpiredAuthorizations.First();
        expiredAuth.EmployeeNumber.Should().Be("EMP302");
        expiredAuth.DocumentNumber.Should().Be("VISA-EXPIRED");
        expiredAuth.DaysUntilExpiration.Should().BeLessThan(0);

        report.SponsorshipStatusSummary.Should().NotBeEmpty();
        report.SponsorshipStatusSummary["Approved"].Should().Be(2);
        report.SponsorshipStatusSummary["NotRequired"].Should().Be(1);
        report.SponsorshipStatusSummary["Pending"].Should().Be(1);
    }

    private Employee CreateEmployee(string employeeNumber, string firstName, string lastName, Guid? departmentId = null)
    {
        return new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = employeeNumber,
            LegalName = new LegalName(firstName, lastName),
            ContactInformation = new ContactInformation
            {
                WorkEmail = $"{firstName.ToLower()}.{lastName.ToLower()}@company.com"
            },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddMonths(-6),
            DepartmentId = departmentId,
            CreatedDate = DateTime.UtcNow.AddMonths(-6)
        };
    }
}
