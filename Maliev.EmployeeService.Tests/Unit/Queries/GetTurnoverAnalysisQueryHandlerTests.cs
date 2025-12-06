using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Queries;

/// <summary>
/// Unit tests for GetTurnoverAnalysisQueryHandler
/// Tests turnover analysis calculations, department breakdown, and monthly trends
/// </summary>
public class GetTurnoverAnalysisQueryHandlerTests
{
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly GetTurnoverAnalysisQueryHandler _handler;

    public GetTurnoverAnalysisQueryHandlerTests()
    {
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _handler = new GetTurnoverAnalysisQueryHandler(_mockEmployeeRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_WithTerminations_ShouldCalculateTurnoverRate()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var employees = new List<Employee>
        {
            // Active throughout period
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
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP004",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            // Terminated during period
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP005",
                EmploymentStatus = EmploymentStatus.Terminated,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                TerminationDate = new DateTime(2024, 6, 15),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP006",
                EmploymentStatus = EmploymentStatus.Terminated,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                TerminationDate = new DateTime(2024, 9, 30),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2023, 1, 1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new GetTurnoverAnalysisQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);

        // Headcount at start: 6, Headcount at end: 4, Average: 5
        Assert.Equal(5, result.AverageHeadcount);
        Assert.Equal(2, result.TotalTerminations);

        // Turnover rate = (2 / 5) * 100 = 40%
        Assert.Equal(40.00m, result.TurnoverRate);

        // Note: Voluntary/Involuntary not implemented in current schema
        Assert.Equal(0, result.VoluntaryTerminations);
        Assert.Equal(0, result.InvoluntaryTerminations);
    }

    [Fact]
    public async Task HandleAsync_WithDepartmentFilter_ShouldFilterByDepartment()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
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
            // Engineering - Active
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                DepartmentId = engineeringId,
                Department = engineeringDept,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            // Engineering - Terminated
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Terminated,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                TerminationDate = new DateTime(2024, 6, 15),
                DepartmentId = engineeringId,
                Department = engineeringDept,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            // Sales - Active (should be excluded)
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                DepartmentId = salesId,
                Department = salesDept,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            // Sales - Terminated (should be excluded)
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP004",
                EmploymentStatus = EmploymentStatus.Terminated,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                TerminationDate = new DateTime(2024, 8, 1),
                DepartmentId = salesId,
                Department = salesDept,
                CreatedDate = new DateTime(2023, 1, 1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new GetTurnoverAnalysisQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            DepartmentId = engineeringId
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(1, result.TotalTerminations); // Only Engineering termination
        Assert.Single(result.ByDepartment);
        Assert.Equal("Engineering", result.ByDepartment[0].DepartmentName);
    }

    [Fact]
    public async Task HandleAsync_WithMonthlyTrend_ShouldCalculatePerMonth()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 3, 31); // 3 months

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var employees = new List<Employee>
        {
            // Active employee
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
            // Terminated in January
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Terminated,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                TerminationDate = new DateTime(2024, 1, 15),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            // Terminated in February
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Terminated,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                TerminationDate = new DateTime(2024, 2, 20),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2023, 1, 1)
            }
            // No terminations in March
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new GetTurnoverAnalysisQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(3, result.MonthlyTrend.Count());

        // January 2024
        var january = result.MonthlyTrend.First(m => m.Month == 1);
        Assert.Equal(2024, january.Year);
        Assert.NotEmpty(january.MonthName);
        Assert.Equal(1, january.Terminations);
        Assert.True(january.AverageHeadcount > 0);

        // February 2024
        var february = result.MonthlyTrend.First(m => m.Month == 2);
        Assert.Equal(1, february.Terminations);

        // March 2024
        var march = result.MonthlyTrend.First(m => m.Month == 3);
        Assert.Equal(0, march.Terminations);
    }

    [Fact]
    public async Task HandleAsync_WithZeroHeadcount_ShouldReturnZeroTurnoverRate()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        var query = new GetTurnoverAnalysisQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(0, result.AverageHeadcount);
        Assert.Equal(0, result.TotalTerminations);
        Assert.Equal(0, result.TurnoverRate);
        Assert.Empty(result.ByDepartment);
        Assert.Equal(12, result.MonthlyTrend.Count()); // 12 months, all zero
    }

    [Fact]
    public async Task HandleAsync_WithNoTerminations_ShouldReturnZeroTurnoverRate()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

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
                StartDate = new DateTime(2023, 1, 1),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2023, 1, 1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new GetTurnoverAnalysisQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(2, result.AverageHeadcount);
        Assert.Equal(0, result.TotalTerminations);
        Assert.Equal(0, result.TurnoverRate);
    }

    [Fact]
    public async Task HandleAsync_WithDepartmentBreakdown_ShouldCalculatePerDepartment()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

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

        var employees = new List<Employee>
        {
            // Engineering - 2 active, 1 terminated (50% turnover)
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                DepartmentId = engineeringDept.Id,
                Department = engineeringDept,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                DepartmentId = engineeringDept.Id,
                Department = engineeringDept,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Terminated,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                TerminationDate = new DateTime(2024, 6, 15),
                DepartmentId = engineeringDept.Id,
                Department = engineeringDept,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            // Sales - 3 active, 0 terminated (0% turnover)
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP004",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                DepartmentId = salesDept.Id,
                Department = salesDept,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP005",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                DepartmentId = salesDept.Id,
                Department = salesDept,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP006",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                DepartmentId = salesDept.Id,
                Department = salesDept,
                CreatedDate = new DateTime(2023, 1, 1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new GetTurnoverAnalysisQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(2, result.ByDepartment.Count());

        // Ordered by turnover rate descending
        var engineeringResult = result.ByDepartment.First(d => d.DepartmentName == "Engineering");
        Assert.Equal(3, engineeringResult.Headcount); // 2 active + 1 terminated
        Assert.Equal(1, engineeringResult.Terminations);
        Assert.True(Math.Abs(engineeringResult.TurnoverRate - 33.33m) <= 0.01m); // 1/3 = 33.33%

        var salesResult = result.ByDepartment.First(d => d.DepartmentName == "Sales");
        Assert.Equal(3, salesResult.Headcount);
        Assert.Equal(0, salesResult.Terminations);
        Assert.Equal(0, salesResult.TurnoverRate);
    }

    [Fact]
    public async Task HandleAsync_ShouldExcludeTerminationsOutsidePeriod()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var employees = new List<Employee>
        {
            // Active
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
            // Terminated before period - EXCLUDED
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Terminated,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                TerminationDate = new DateTime(2023, 12, 31), // Before startDate
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            // Terminated during period - INCLUDED
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Terminated,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                TerminationDate = new DateTime(2024, 6, 15), // Within period
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2023, 1, 1)
            },
            // Terminated after period - EXCLUDED
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP004",
                EmploymentStatus = EmploymentStatus.Terminated,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2023, 1, 1),
                TerminationDate = new DateTime(2025, 1, 15), // After endDate
                DepartmentId = department.Id,
                Department = department,
                CreatedDate = new DateTime(2023, 1, 1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new GetTurnoverAnalysisQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(1, result.TotalTerminations); // Only EMP003
    }
}
