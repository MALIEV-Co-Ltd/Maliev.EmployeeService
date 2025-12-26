using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for Reports and Bulk Operations (User Story 12)
/// T375-T379: Headcount reports, turnover analysis, bulk salary increase, employee search
/// </summary>
public class ReportsAndBulkOperationsIntegrationTests : PostgreSqlIntegrationTestBase
{
    [Fact]
    public async Task HeadcountReport_WithMultipleDepartmentsAndTenures_ShouldGenerateCompleteReport()
    {
        // Arrange - Create departments
        var engineeringDept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var salesDept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Sales",
            HeadcountLimit = 30,
            CreatedDate = DateTime.UtcNow.AddYears(-3)
        };

        Context.Departments.AddRange(engineeringDept, salesDept);

        // Create employees with different tenures and types
        var employees = new List<Employee>
        {
            // Engineering - Senior (10 years)
            new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-10),
                DepartmentId = engineeringDept.Id,
                LegalName = new LegalName { FirstName = "Alice", LastName = "Anderson" },
                DirectReports = new List<Employee>(), // Will add later
                CreatedDate = DateTime.UtcNow.AddYears(-10)
            },
            // Engineering - Junior (6 months)
            new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddMonths(-6),
                DepartmentId = engineeringDept.Id,
                LegalName = new LegalName { FirstName = "Bob", LastName = "Baker" },
                CreatedDate = DateTime.UtcNow.AddMonths(-6)
            },
            // Sales - Mid (3 years)
            new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-3),
                DepartmentId = salesDept.Id,
                LegalName = new LegalName { FirstName = "Charlie", LastName = "Chen" },
                CreatedDate = DateTime.UtcNow.AddYears(-3)
            },
            // Sales - Contractor (1.5 years)
            new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP004",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.Contractor,
                StartDate = DateTime.UtcNow.AddMonths(-18),
                DepartmentId = salesDept.Id,
                LegalName = new LegalName { FirstName = "Diana", LastName = "Davis" },
                CreatedDate = DateTime.UtcNow.AddMonths(-18)
            }
        };

        // Make EMP001 a manager
        employees[1].ManagerId = employees[0].Id;
        employees[0].DirectReports = new List<Employee> { employees[1] };

        Context.Employees.AddRange(employees);
        await Context.SaveChangesAsync();

        // Act
        var employeeRepository = new EmployeeRepository(Context);
        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());

        var handler = new GetHeadcountReportQueryHandler(
            employeeRepository,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        var result = await handler.HandleAsync(new GetHeadcountReportQuery
        {
            AsOfDate = DateTime.UtcNow
        });

        // Assert
        Assert.Equal(4, result.TotalHeadcount);
        Assert.Equal(2, result.ByDepartment.Count());

        var engDept = result.ByDepartment.First(d => d.DepartmentName == "Engineering");
        Assert.Equal(2, engDept.Headcount);
        Assert.Equal(1, engDept.ManagerCount); // EMP001
        Assert.Equal(1, engDept.IndividualContributorCount); // EMP002

        var salesResult = result.ByDepartment.First(d => d.DepartmentName == "Sales");
        Assert.Equal(2, salesResult.Headcount);

        // Tenure bands
        Assert.True(result.ByTenureBand.ContainsKey("0-1 years"));
        Assert.Equal(1, result.ByTenureBand["0-1 years"]); // EMP002
        Assert.True(result.ByTenureBand.ContainsKey("1-2 years"));
        Assert.Equal(1, result.ByTenureBand["1-2 years"]); // EMP004
        Assert.True(result.ByTenureBand.ContainsKey("3-5 years"));
        Assert.Equal(1, result.ByTenureBand["3-5 years"]); // EMP003
        Assert.True(result.ByTenureBand.ContainsKey("10+ years"));
        Assert.Equal(1, result.ByTenureBand["10+ years"]); // EMP001

        // Employment types
        Assert.Equal(3, result.ByEmploymentType["FullTime"]);
        Assert.Equal(1, result.ByEmploymentType["Contractor"]);
    }

    [Fact]
    public async Task TurnoverAnalysis_WithTerminationsInPeriod_ShouldCalculateCorrectRates()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        Context.Departments.Add(department);

        var employees = new List<Employee>
        {
            // Active throughout period
            new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DepartmentId = department.Id,
                LegalName = new LegalName { FirstName = "Alice", LastName = "Active" },
                CreatedDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DepartmentId = department.Id,
                LegalName = new LegalName { FirstName = "Bob", LastName = "Stable" },
                CreatedDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DepartmentId = department.Id,
                LegalName = new LegalName { FirstName = "Charlie", LastName = "Steady" },
                CreatedDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            // Terminated in Q2
            new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP004",
                EmploymentStatus = EmploymentStatus.Terminated,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TerminationDate = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
                DepartmentId = department.Id,
                LegalName = new LegalName { FirstName = "Diana", LastName = "Departed" },
                CreatedDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        Context.Employees.AddRange(employees);
        await Context.SaveChangesAsync();

        // Act
        var employeeRepository = new EmployeeRepository(Context);
        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());

        var handler = new GetTurnoverAnalysisQueryHandler(
            employeeRepository,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        var result = await handler.HandleAsync(new GetTurnoverAnalysisQuery
        {
            StartDate = startDate,
            EndDate = endDate
        });

        // Assert
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);
        Assert.Equal(1, result.TotalTerminations);

        // Headcount at start: 4, Headcount at end: 3, Average: 3.5 ≈ 3 (integer division)
        Assert.True(result.AverageHeadcount >= 3);

        // Turnover rate should be positive
        Assert.True(result.TurnoverRate > 0);
        Assert.True(result.TurnoverRate <= 50); // 1/3 = 33.33%

        // Department breakdown
        Assert.Single(result.ByDepartment);
        Assert.Equal("Engineering", result.ByDepartment[0].DepartmentName);
        Assert.Equal(1, result.ByDepartment[0].Terminations);

        // Monthly trend should have 12 months
        Assert.Equal(12, result.MonthlyTrend.Count());

        // June should have 1 termination
        var juneData = result.MonthlyTrend.First(m => m.Month == 6);
        Assert.Equal(1, juneData.Terminations);
    }

    [Fact]
    public async Task BulkSalaryIncrease_ExecutionMode_ShouldPersistChangesAndCreateJob()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        Context.Departments.Add(department);

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-2),
                DepartmentId = department.Id,
                LegalName = new LegalName { FirstName = "Alice", LastName = "Anderson" },
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-1),
                DepartmentId = department.Id,
                LegalName = new LegalName { FirstName = "Bob", LastName = "Baker" },
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            }
        };

        Context.Employees.AddRange(employees);

        // Create initial compensation records
        var compensation1 = new CompensationRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employees[0].Id,
            SalaryAmount = "100000",
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow.AddYears(-2),
            ChangeReason = "Initial Salary",
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var compensation2 = new CompensationRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employees[1].Id,
            SalaryAmount = "80000",
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow.AddYears(-1),
            ChangeReason = "Initial Salary",
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        Context.Set<CompensationRecord>().AddRange(compensation1, compensation2);
        await Context.SaveChangesAsync();

        // Act
        var employeeRepository = new EmployeeRepository(Context);
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var bulkJobRepository = new BulkJobRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());

        var handler = new BulkSalaryIncreaseCommandHandler(
            employeeRepository,
            compensationRepository,
            bulkJobRepository,
            unitOfWork,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        var result = await handler.HandleAsync(new BulkSalaryIncreaseCommand
        {
            PercentageIncrease = 10, // 10% increase
            Reason = "Annual Review 2024",
            EffectiveDate = DateTime.UtcNow.AddDays(30),
            PreviewOnly = false, // Execution mode
            InitiatedByUserId = Guid.NewGuid()
        });

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsPreview);
        Assert.NotNull(result.JobId);
        Assert.Equal(2, result.TotalEmployees);

        // Verify new compensation records were created
        var newCompensation = await Context.Set<CompensationRecord>()
            .Where(c => c.ChangeReason == "Annual Review 2024")
            .ToListAsync();

        Assert.Equal(2, newCompensation.Count());
        Assert.Contains(newCompensation, c => c.SalaryAmount == "110000.00"); // 100000 * 1.1
        Assert.Contains(newCompensation, c => c.SalaryAmount == "88000.00"); // 80000 * 1.1

        // Verify bulk job was created
        var job = await Context.Set<BulkJob>()
            .FirstOrDefaultAsync(j => j.JobId == result.JobId!.Value);

        Assert.NotNull(job);
        Assert.Equal("BulkSalaryIncrease", job!.JobType);
        Assert.Equal(2, job.TotalRecords);
        Assert.Equal(2, job.SuccessfulRecords);
    }

    [Fact]
    public async Task EmployeeSearch_WithMultipleCriteria_ShouldReturnCorrectResults()
    {
        // Arrange
        var engineeringDept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var salesDept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Sales",
            HeadcountLimit = 30,
            CreatedDate = DateTime.UtcNow.AddYears(-3)
        };

        Context.Departments.AddRange(engineeringDept, salesDept);

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-3),
                DepartmentId = engineeringDept.Id,
                JobTitle = "Senior Software Engineer",
                LegalName = new LegalName { FirstName = "Alice", LastName = "Anderson" },
                ContactInformation = new ContactInformation { WorkEmail = "alice.anderson@maliev.com" },
                CreatedDate = DateTime.UtcNow.AddYears(-3)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-2),
                DepartmentId = engineeringDept.Id,
                JobTitle = "Software Engineer",
                LegalName = new LegalName { FirstName = "Bob", LastName = "Baker" },
                ContactInformation = new ContactInformation { WorkEmail = "bob.baker@maliev.com" },
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-1),
                DepartmentId = salesDept.Id,
                JobTitle = "Sales Manager",
                LegalName = new LegalName { FirstName = "Charlie", LastName = "Chen" },
                ContactInformation = new ContactInformation { WorkEmail = "charlie.chen@maliev.com" },
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            }
        };

        Context.Employees.AddRange(employees);
        await Context.SaveChangesAsync();

        // Act - Search by department and title
        var employeeRepository = new EmployeeRepository(Context);
        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());

        var handler = new SearchEmployeesQueryHandler(
            employeeRepository,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        var result = await handler.HandleAsync(new SearchEmployeesQuery
        {
            DepartmentId = engineeringDept.Id,
            Title = "Senior",
            Page = 1,
            PageSize = 50
        });

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Results);
        Assert.Equal("EMP001", result.Results[0].EmployeeNumber);
        Assert.Contains("Senior", result.Results[0].Title);
        Assert.Equal("Engineering", result.Results[0].DepartmentName);
    }

    [Fact]
    public async Task EmployeeSearch_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 100,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        Context.Departments.Add(department);

        // Create 25 employees
        var employees = Enumerable.Range(1, 25).Select(i => new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = $"EMP{i:000}",
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow.AddMonths(-i),
            DepartmentId = department.Id,
            LegalName = new LegalName { FirstName = $"Employee{i}", LastName = "Test" },
            CreatedDate = DateTime.UtcNow.AddMonths(-i)
        }).ToList();

        Context.Employees.AddRange(employees);
        await Context.SaveChangesAsync();

        // Act - Get page 2 with page size 10
        var employeeRepository = new EmployeeRepository(Context);
        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());

        var handler = new SearchEmployeesQueryHandler(
            employeeRepository,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        var result = await handler.HandleAsync(new SearchEmployeesQuery
        {
            Page = 2,
            PageSize = 10,
            SortBy = "employeenumber",
            SortDirection = "asc"
        });

        // Assert
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalPages); // 25 / 10 = 3 pages
        Assert.Equal(10, result.Results.Count());

        // Page 2 should contain EMP011 to EMP020
        Assert.Equal("EMP011", result.Results[0].EmployeeNumber);
        Assert.Equal("EMP020", result.Results[9].EmployeeNumber);
    }

    [Fact]
    public async Task HeadcountReport_WithHistoricalAsOfDate_ShouldExcludeFutureHires()
    {
        // Arrange
        var asOfDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        Context.Departments.Add(department);

        var employees = new List<Employee>
        {
            // Hired before AsOfDate - INCLUDED
            new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DepartmentId = department.Id,
                LegalName = new LegalName { FirstName = "Alice", LastName = "Active" },
                CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            // Hired after AsOfDate - EXCLUDED
            new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc), // After AsOfDate
                DepartmentId = department.Id,
                LegalName = new LegalName { FirstName = "Bob", LastName = "Future" },
                CreatedDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            // Terminated before AsOfDate - EXCLUDED
            new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Terminated,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TerminationDate = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc), // Before AsOfDate
                DepartmentId = department.Id,
                LegalName = new LegalName { FirstName = "Charlie", LastName = "Past" },
                CreatedDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        Context.Employees.AddRange(employees);
        await Context.SaveChangesAsync();

        // Act
        var employeeRepository = new EmployeeRepository(Context);
        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());

        var handler = new GetHeadcountReportQueryHandler(
            employeeRepository,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        var result = await handler.HandleAsync(new GetHeadcountReportQuery
        {
            AsOfDate = asOfDate
        });

        // Assert
        Assert.Equal(1, result.TotalHeadcount); // Only EMP001
    }
}
