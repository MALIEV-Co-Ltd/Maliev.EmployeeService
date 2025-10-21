using FluentAssertions;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Queries;

/// <summary>
/// Unit tests for GetHeadcountReportQueryHandler
/// Tests headcount report generation with aggregations by department, tenure, and employment type
/// </summary>
public class GetHeadcountReportQueryHandlerTests
{
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly GetHeadcountReportQueryHandler _handler;

    public GetHeadcountReportQueryHandlerTests()
    {
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _handler = new GetHeadcountReportQueryHandler(_mockEmployeeRepository.Object);
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
        result.Should().NotBeNull();
        result.TotalHeadcount.Should().Be(5);
        result.AsOfDate.Should().Be(asOfDate);

        // Department breakdown
        result.ByDepartment.Should().HaveCount(2);

        var engineeringDept = result.ByDepartment.First(d => d.DepartmentName == "Engineering");
        engineeringDept.Headcount.Should().Be(3);
        engineeringDept.ManagerCount.Should().Be(1);
        engineeringDept.IndividualContributorCount.Should().Be(2);

        var salesDept = result.ByDepartment.First(d => d.DepartmentName == "Sales");
        salesDept.Headcount.Should().Be(2);
        salesDept.ManagerCount.Should().Be(1);
        salesDept.IndividualContributorCount.Should().Be(1);

        // Employment type breakdown
        result.ByEmploymentType.Should().ContainKey("FullTime");
        result.ByEmploymentType["FullTime"].Should().Be(3);
        result.ByEmploymentType.Should().ContainKey("Contractor");
        result.ByEmploymentType["Contractor"].Should().Be(1);
        result.ByEmploymentType.Should().ContainKey("PartTime");
        result.ByEmploymentType["PartTime"].Should().Be(1);

        // Tenure band breakdown
        result.ByTenureBand.Should().ContainKey("0-1 years");
        result.ByTenureBand["0-1 years"].Should().Be(1); // EMP003 (6 months)
        result.ByTenureBand.Should().ContainKey("1-2 years");
        result.ByTenureBand["1-2 years"].Should().Be(1); // EMP005 (1.5 years)
        result.ByTenureBand.Should().ContainKey("2-3 years");
        result.ByTenureBand["2-3 years"].Should().Be(1); // EMP002 (2 years)
        result.ByTenureBand.Should().ContainKey("5-10 years");
        result.ByTenureBand["5-10 years"].Should().Be(1); // EMP001 (7 years)
        result.ByTenureBand.Should().ContainKey("10+ years");
        result.ByTenureBand["10+ years"].Should().Be(1); // EMP004 (12 years)
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
        result.TotalHeadcount.Should().Be(2); // Only Engineering employees
        result.ByDepartment.Should().HaveCount(1);
        result.ByDepartment[0].DepartmentName.Should().Be("Engineering");
        result.ByDepartment[0].Headcount.Should().Be(2);
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
        result.TotalHeadcount.Should().Be(2); // Only EMP001 and EMP005
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
        result.ByTenureBand.Should().HaveCount(6);
        result.ByTenureBand["0-1 years"].Should().Be(1);
        result.ByTenureBand["1-2 years"].Should().Be(1);
        result.ByTenureBand["2-3 years"].Should().Be(1);
        result.ByTenureBand["3-5 years"].Should().Be(1);
        result.ByTenureBand["5-10 years"].Should().Be(1);
        result.ByTenureBand["10+ years"].Should().Be(1);
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
        result.TotalHeadcount.Should().Be(0);
        result.ByDepartment.Should().BeEmpty();
        result.ByEmploymentType.Should().BeEmpty();
        result.ByTenureBand.Should().BeEmpty();
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
        result.TotalHeadcount.Should().Be(1); // Only Active employee
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
        result.TotalHeadcount.Should().Be(2);
        result.ByDepartment.Should().HaveCount(1); // Only employees with departments
        result.ByDepartment[0].DepartmentName.Should().Be("Engineering");
        result.ByDepartment[0].Headcount.Should().Be(1);
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
        result.ByDepartment[0].DepartmentName.Should().Be("Engineering");
        result.ByDepartment[0].Headcount.Should().Be(3);
        result.ByDepartment[1].DepartmentName.Should().Be("Legal");
        result.ByDepartment[1].Headcount.Should().Be(1);
    }
}
