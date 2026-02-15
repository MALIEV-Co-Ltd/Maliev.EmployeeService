using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Xunit;
using Maliev.Aspire.ServiceDefaults.IAM;
using Microsoft.Extensions.Configuration;
using Maliev.EmployeeService.Domain.Authorization;
using Moq;

namespace Maliev.EmployeeService.Tests.Unit.Queries;

/// <summary>
/// Unit tests for GetHeadcountReportQueryHandler
/// Tests headcount report generation with aggregations by department, tenure, and employment type
/// </summary>
public class GetHeadcountReportQueryHandlerTests
{
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<IIamServiceClient> _mockIamClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly GetHeadcountReportQueryHandler _handler;

    public GetHeadcountReportQueryHandlerTests()
    {
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockIamClient = new Mock<IIamServiceClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        var principalId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.PrincipalId).Returns(principalId);
        _mockCurrentUserService.Setup(x => x.PrincipalIdentifier).Returns(principalId.ToString());
        _mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new GetHeadcountReportQueryHandler(
            _mockEmployeeRepository.Object,
            _mockIamClient.Object,
            _mockConfiguration.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task HandleAsync_WithMultipleEmployees_ShouldGenerateCompleteReport()
    {
        // Arrange
        var asOfDate = DateTime.UtcNow;
        var department1 = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var department2 = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Sales",
            HeadcountLimit = 30,
            CreatedDate = DateTime.UtcNow.AddYears(-3)
        };

        var employees = new List<Employee>
        {
            // Engineering - Manager with 7 years tenure
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-7),
                DepartmentId = department1.Id,
                Department = department1,
                DirectReports = new List<Employee> { new Employee() }, // Has direct reports = manager
                CreatedDate = DateTime.UtcNow.AddYears(-7)
            },
            // Engineering - IC with 2 years tenure
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-2),
                DepartmentId = department1.Id,
                Department = department1,
                DirectReports = new List<Employee>(), // No direct reports = IC
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            // Engineering - IC with 6 months tenure
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.Contractor,
                StartDate = asOfDate.AddMonths(-6),
                DepartmentId = department1.Id,
                Department = department1,
                DirectReports = new List<Employee>(),
                CreatedDate = DateTime.UtcNow.AddMonths(-6)
            },
            // Sales - Manager with 12 years tenure
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP004",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-12),
                DepartmentId = department2.Id,
                Department = department2,
                DirectReports = new List<Employee> { new Employee(), new Employee() },
                CreatedDate = DateTime.UtcNow.AddYears(-12)
            },
            // Sales - IC with 1.5 years tenure
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP005",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.PartTime,
                StartDate = asOfDate.AddMonths(-18),
                DepartmentId = department2.Id,
                Department = department2,
                DirectReports = new List<Employee>(), // Empty list = IC
                CreatedDate = DateTime.UtcNow.AddMonths(-18)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new GetHeadcountReportQuery
        {
            AsOfDate = asOfDate
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalHeadcount);
        Assert.Equal(asOfDate, result.AsOfDate);

        // Department breakdown
        Assert.Equal(2, result.ByDepartment.Count());

        var engineeringDept = result.ByDepartment.First(d => d.DepartmentName == "Engineering");
        Assert.Equal(3, engineeringDept.Headcount);
        Assert.Equal(1, engineeringDept.ManagerCount);
        Assert.Equal(2, engineeringDept.IndividualContributorCount);

        var salesDept = result.ByDepartment.First(d => d.DepartmentName == "Sales");
        Assert.Equal(2, salesDept.Headcount);
        Assert.Equal(1, salesDept.ManagerCount);
        Assert.Equal(1, salesDept.IndividualContributorCount);

        // Employment type breakdown
        Assert.True(result.ByEmploymentType.ContainsKey("FullTime"));
        Assert.Equal(3, result.ByEmploymentType["FullTime"]);
        Assert.True(result.ByEmploymentType.ContainsKey("Contractor"));
        Assert.Equal(1, result.ByEmploymentType["Contractor"]);
        Assert.True(result.ByEmploymentType.ContainsKey("PartTime"));
        Assert.Equal(1, result.ByEmploymentType["PartTime"]);

        // Tenure band breakdown
        Assert.True(result.ByTenureBand.ContainsKey("0-1 years"));
        Assert.Equal(1, result.ByTenureBand["0-1 years"]); // EMP003 (6 months)
        Assert.True(result.ByTenureBand.ContainsKey("1-2 years"));
        Assert.Equal(1, result.ByTenureBand["1-2 years"]); // EMP005 (1.5 years)
        Assert.True(result.ByTenureBand.ContainsKey("2-3 years"));
        Assert.Equal(1, result.ByTenureBand["2-3 years"]); // EMP002 (2 years)
        Assert.True(result.ByTenureBand.ContainsKey("5-10 years"));
        Assert.Equal(1, result.ByTenureBand["5-10 years"]); // EMP001 (7 years)
        Assert.True(result.ByTenureBand.ContainsKey("10+ years"));
        Assert.Equal(1, result.ByTenureBand["10+ years"]); // EMP004 (12 years)
    }

    [Fact]
    public async Task HandleAsync_WithDepartmentFilter_ShouldFilterByDepartment()
    {
        // Arrange
        var asOfDate = DateTime.UtcNow;
        var engineeringId = Guid.NewGuid();
        var salesId = Guid.NewGuid();

        var engineeringDept = new Department
        {
            Id = engineeringId,
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var salesDept = new Department
        {
            Id = salesId,
            Name = "Sales",
            HeadcountLimit = 30,
            CreatedDate = DateTime.UtcNow.AddYears(-3)
        };

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-3),
                DepartmentId = engineeringId,
                Department = engineeringDept,
                CreatedDate = DateTime.UtcNow.AddYears(-3)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-2),
                DepartmentId = engineeringId,
                Department = engineeringDept,
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-1),
                DepartmentId = salesId,
                Department = salesDept,
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new GetHeadcountReportQuery
        {
            DepartmentId = engineeringId,
            AsOfDate = asOfDate
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(2, result.TotalHeadcount); // Only Engineering employees
        Assert.Single(result.ByDepartment);
        Assert.Equal("Engineering", result.ByDepartment[0].DepartmentName);
        Assert.Equal(2, result.ByDepartment[0].Headcount);
    }

    [Fact]
    public async Task HandleAsync_WithAsOfDateFilter_ShouldExcludeEmployeesNotActiveOnDate()
    {
        // Arrange
        var asOfDate = new DateTime(2024, 6, 1);
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var employees = new List<Employee>
        {
            // Active on AsOfDate
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            // Started after AsOfDate - EXCLUDED
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2024, 7, 1), // After AsOfDate
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2024, 7, 1)
            },
            // Terminated before AsOfDate - EXCLUDED
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Active, // Status is Active but has TerminationDate
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                TerminationDate = new DateTime(2024, 3, 1), // Before AsOfDate
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            // Terminated on AsOfDate (exclusive) - EXCLUDED
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP004",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                TerminationDate = asOfDate, // On AsOfDate - excluded (must be > AsOfDate)
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            // Terminated after AsOfDate - INCLUDED
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP005",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                TerminationDate = new DateTime(2024, 8, 1), // After AsOfDate - still active
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2023, 1, 1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new GetHeadcountReportQuery
        {
            AsOfDate = asOfDate
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(2, result.TotalHeadcount); // Only EMP001 and EMP005
    }

    [Fact]
    public async Task HandleAsync_WithAllTenureBands_ShouldCorrectlyCategorizeTenure()
    {
        // Arrange
        var asOfDate = DateTime.UtcNow;
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 100,
            CreatedDate = DateTime.UtcNow.AddYears(-15)
        };

        var employees = new List<Employee>
        {
            // 0-1 years (6 months)
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddMonths(-6),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = DateTime.UtcNow
            },
            // 1-2 years (18 months)
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddMonths(-18),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = DateTime.UtcNow
            },
            // 2-3 years (2.5 years)
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-2).AddMonths(-6),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = DateTime.UtcNow
            },
            // 3-5 years (4 years)
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP004",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-4),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = DateTime.UtcNow
            },
            // 5-10 years (7 years)
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP005",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-7),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = DateTime.UtcNow
            },
            // 10+ years (15 years)
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP006",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-15),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = DateTime.UtcNow
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new GetHeadcountReportQuery
        {
            AsOfDate = asOfDate
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(6, result.ByTenureBand.Count());
        Assert.Equal(1, result.ByTenureBand["0-1 years"]);
        Assert.Equal(1, result.ByTenureBand["1-2 years"]);
        Assert.Equal(1, result.ByTenureBand["2-3 years"]);
        Assert.Equal(1, result.ByTenureBand["3-5 years"]);
        Assert.Equal(1, result.ByTenureBand["5-10 years"]);
        Assert.Equal(1, result.ByTenureBand["10+ years"]);
    }

    [Fact]
    public async Task HandleAsync_WithNoEmployees_ShouldReturnEmptyReport()
    {
        // Arrange
        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        var query = new GetHeadcountReportQuery
        {
            AsOfDate = DateTime.UtcNow
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(0, result.TotalHeadcount);
        Assert.Empty(result.ByDepartment);
        Assert.Empty(result.ByEmploymentType);
        Assert.Empty(result.ByTenureBand);
    }

    [Fact]
    public async Task HandleAsync_WithInactiveEmployees_ShouldExcludeInactive()
    {
        // Arrange
        var asOfDate = DateTime.UtcNow;
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-3),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = DateTime.UtcNow.AddYears(-3)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Terminated, // EXCLUDED
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-2),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.OnLeave, // EXCLUDED
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-1),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new GetHeadcountReportQuery
        {
            AsOfDate = asOfDate
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(1, result.TotalHeadcount); // Only Active employee
    }

    [Fact]
    public async Task HandleAsync_WithEmployeesWithoutDepartment_ShouldHandleGracefully()
    {
        // Arrange
        var asOfDate = DateTime.UtcNow;
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-2),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.Contractor,
                StartDate = asOfDate.AddYears(-1),
                DepartmentId = null, // No department
                Department = null,
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new GetHeadcountReportQuery
        {
            AsOfDate = asOfDate
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(2, result.TotalHeadcount);
        Assert.Single(result.ByDepartment); // Only employees with departments
        Assert.Equal("Engineering", result.ByDepartment[0].DepartmentName);
        Assert.Equal(1, result.ByDepartment[0].Headcount);
    }

    [Fact]
    public async Task HandleAsync_ShouldOrderDepartmentsByHeadcountDescending()
    {
        // Arrange
        var asOfDate = DateTime.UtcNow;
        var smallDept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Legal",
            HeadcountLimit = 5,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var largeDept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 100,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var employees = new List<Employee>
        {
            // Legal - 1 employee
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-2),
                DepartmentId = smallDept.Id,
                Department = smallDept,
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            // Engineering - 3 employees
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-3),
                DepartmentId = largeDept.Id,
                Department = largeDept,
                CreatedDate = DateTime.UtcNow.AddYears(-3)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-2),
                DepartmentId = largeDept.Id,
                Department = largeDept,
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP004",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = asOfDate.AddYears(-1),
                DepartmentId = largeDept.Id,
                Department = largeDept,
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new GetHeadcountReportQuery
        {
            AsOfDate = asOfDate
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal("Engineering", result.ByDepartment[0].DepartmentName);
        Assert.Equal(3, result.ByDepartment[0].Headcount);
        Assert.Equal("Legal", result.ByDepartment[1].DepartmentName);
        Assert.Equal(1, result.ByDepartment[1].Headcount);
    }
}
