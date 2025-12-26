using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Xunit;
using Maliev.Aspire.ServiceDefaults.IAM;
using Microsoft.Extensions.Configuration;
using Maliev.EmployeeService.Domain.Authorization;
using Moq;

namespace Maliev.EmployeeService.Tests.Unit.Queries;

/// <summary>
/// Unit tests for SearchEmployeesQueryHandler
/// Tests multi-criteria search with filtering, sorting, and pagination
/// </summary>
public class SearchEmployeesQueryHandlerTests
{
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<IIamServiceClient> _mockIamClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly SearchEmployeesQueryHandler _handler;

    public SearchEmployeesQueryHandlerTests()
    {
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockIamClient = new Mock<IIamServiceClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());
        _mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new SearchEmployeesQueryHandler(
            _mockEmployeeRepository.Object,
            _mockIamClient.Object,
            _mockConfiguration.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task HandleAsync_SearchByFirstName_ShouldReturnMatchingEmployees()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-2),
                LegalName = new LegalName { FirstName = "John", LastName = "Doe" },
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-1),
                LegalName = new LegalName { FirstName = "Jane", LastName = "Smith" },
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new SearchEmployeesQuery { Name = "John" };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Results);
        Assert.Equal("EMP001", result.Results[0].EmployeeNumber);
        Assert.Contains("John", result.Results[0].FullName);
    }

    [Fact]
    public async Task HandleAsync_SearchByLastName_ShouldReturnMatchingEmployees()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-2),
                LegalName = new LegalName { FirstName = "John", LastName = "Doe" },
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-1),
                LegalName = new LegalName { FirstName = "Jane", LastName = "Doe" },
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddMonths(-6),
                LegalName = new LegalName { FirstName = "Bob", LastName = "Smith" },
                CreatedDate = DateTime.UtcNow.AddMonths(-6)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new SearchEmployeesQuery { Name = "Doe" };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(2, result.TotalCount); // Both Doe employees
        Assert.Equal(2, result.Results.Count());
        Assert.All(result.Results, r => Assert.Contains("Doe", r.FullName));
    }

    [Fact]
    public async Task HandleAsync_SearchByEmployeeNumber_ShouldReturnExactMatch()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-2),
                LegalName = new LegalName { FirstName = "John", LastName = "Doe" },
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-1),
                LegalName = new LegalName { FirstName = "Jane", LastName = "Smith" },
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new SearchEmployeesQuery { EmployeeNumber = "EMP001" };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("EMP001", result.Results[0].EmployeeNumber);
    }

    [Fact]
    public async Task HandleAsync_SearchByEmail_ShouldReturnMatchingEmployee()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-2),
                LegalName = new LegalName { FirstName = "John", LastName = "Doe" },
                ContactInformation = new ContactInformation { WorkEmail = "john.doe@maliev.com" },
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-1),
                LegalName = new LegalName { FirstName = "Jane", LastName = "Smith" },
                ContactInformation = new ContactInformation { WorkEmail = "jane.smith@maliev.com" },
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new SearchEmployeesQuery { Email = "john.doe" };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("john.doe@maliev.com", result.Results[0].Email);
    }

    [Fact]
    public async Task HandleAsync_FilterByDepartment_ShouldReturnOnlyDepartmentEmployees()
    {
        // Arrange
        var engineeringDeptId = Guid.NewGuid();
        var salesDeptId = Guid.NewGuid();

        var engineeringDept = new Department
        {
            Id = engineeringDeptId,
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
                StartDate = DateTime.UtcNow.AddYears(-2),
                DepartmentId = engineeringDeptId,
                Department = engineeringDept,
                LegalName = new LegalName { FirstName = "John", LastName = "Doe" },
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-1),
                DepartmentId = salesDeptId,
                LegalName = new LegalName { FirstName = "Jane", LastName = "Smith" },
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new SearchEmployeesQuery { DepartmentId = engineeringDeptId };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(engineeringDeptId, result.Results[0].DepartmentId);
        Assert.Equal("Engineering", result.Results[0].DepartmentName);
    }

    [Fact]
    public async Task HandleAsync_FilterByTitle_ShouldReturnMatchingEmployees()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-2),
                JobTitle = "Senior Software Engineer",
                LegalName = new LegalName { FirstName = "John", LastName = "Doe" },
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-1),
                JobTitle = "Sales Manager",
                LegalName = new LegalName { FirstName = "Jane", LastName = "Smith" },
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new SearchEmployeesQuery { Title = "Engineer" };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Contains("Engineer", result.Results[0].Title);
    }

    [Fact]
    public async Task HandleAsync_FilterByEmploymentStatus_ShouldReturnActiveOnly()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-2),
                LegalName = new LegalName { FirstName = "John", LastName = "Doe" },
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Terminated,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-3),
                TerminationDate = DateTime.UtcNow.AddMonths(-1),
                LegalName = new LegalName { FirstName = "Jane", LastName = "Smith" },
                CreatedDate = DateTime.UtcNow.AddYears(-3)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new SearchEmployeesQuery { EmploymentStatus = "Active" };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Active", result.Results[0].EmploymentStatus);
    }

    [Fact]
    public async Task HandleAsync_FilterByManager_ShouldReturnDirectReports()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var managerName = new LegalName { FirstName = "Bob", LastName = "Manager" };

        var employees = new List<Employee>
        {
            // Direct report
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-2),
                ManagerId = managerId,
                LegalName = new LegalName { FirstName = "John", LastName = "Doe" },
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            // Different manager
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-1),
                ManagerId = Guid.NewGuid(), // Different manager
                LegalName = new LegalName { FirstName = "Jane", LastName = "Smith" },
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new SearchEmployeesQuery { ManagerId = managerId };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(managerId, result.Results[0].ManagerId);
    }

    [Fact]
    public async Task HandleAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var employees = Enumerable.Range(1, 100).Select(i => new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = $"EMP{i:000}",
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow.AddYears(-1),
            LegalName = new LegalName { FirstName = $"Employee{i}", LastName = "Test" },
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        }).ToList();

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new SearchEmployeesQuery
        {
            Page = 2,
            PageSize = 25
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(100, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(4, result.TotalPages); // 100 / 25 = 4
        Assert.Equal(25, result.Results.Count());
    }

    [Fact]
    public async Task HandleAsync_WithMaxPageSize_ShouldEnforceLimit()
    {
        // Arrange
        var employees = Enumerable.Range(1, 250).Select(i => new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = $"EMP{i:000}",
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow.AddYears(-1),
            LegalName = new LegalName { FirstName = $"Employee{i}", LastName = "Test" },
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        }).ToList();

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new SearchEmployeesQuery
        {
            PageSize = 300 // Over the limit
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(200, result.PageSize); // Max enforced
        Assert.Equal(200, result.Results.Count());
    }

    [Fact]
    public async Task HandleAsync_SortByName_ShouldSortAlphabetically()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-2),
                LegalName = new LegalName { FirstName = "Charlie", LastName = "Brown" },
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-1),
                LegalName = new LegalName { FirstName = "Alice", LastName = "Anderson" },
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddMonths(-6),
                LegalName = new LegalName { FirstName = "Bob", LastName = "Baker" },
                CreatedDate = DateTime.UtcNow.AddMonths(-6)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new SearchEmployeesQuery
        {
            SortBy = "name",
            SortDirection = "asc"
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Contains("Anderson", result.Results[0].FullName); // Alice Anderson
        Assert.Contains("Baker", result.Results[1].FullName); // Bob Baker
        Assert.Contains("Brown", result.Results[2].FullName); // Charlie Brown
    }

    [Fact]
    public async Task HandleAsync_SortByHireDate_ShouldSortChronologically()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2024, 6, 1),
                LegalName = new LegalName { FirstName = "Charlie", LastName = "Brown" },
                CreatedDate = new DateTime(2024, 6, 1)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2024, 1, 1),
                LegalName = new LegalName { FirstName = "Alice", LastName = "Anderson" },
                CreatedDate = new DateTime(2024, 1, 1)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP003",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = new DateTime(2024, 3, 1),
                LegalName = new LegalName { FirstName = "Bob", LastName = "Baker" },
                CreatedDate = new DateTime(2024, 3, 1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new SearchEmployeesQuery
        {
            SortBy = "hiredate",
            SortDirection = "asc"
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(new DateTime(2024, 1, 1), result.Results[0].HireDate);
        Assert.Equal(new DateTime(2024, 3, 1), result.Results[1].HireDate);
        Assert.Equal(new DateTime(2024, 6, 1), result.Results[2].HireDate);
    }

    [Fact]
    public async Task HandleAsync_WithMultipleFilters_ShouldCombineFilters()
    {
        // Arrange
        var engineeringDeptId = Guid.NewGuid();
        var engineeringDept = new Department
        {
            Id = engineeringDeptId,
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var employees = new List<Employee>
        {
            // Matches all criteria
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-2),
                DepartmentId = engineeringDeptId,
                Department = engineeringDept,
                JobTitle = "Senior Software Engineer",
                LegalName = new LegalName { FirstName = "John", LastName = "Doe" },
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            // Wrong department
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-1),
                DepartmentId = Guid.NewGuid(), // Different department
                JobTitle = "Software Engineer",
                LegalName = new LegalName { FirstName = "Jane", LastName = "Doe" },
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new SearchEmployeesQuery
        {
            DepartmentId = engineeringDeptId,
            Title = "Senior",
            Name = "John"
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("EMP001", result.Results[0].EmployeeNumber);
    }

    [Fact]
    public async Task HandleAsync_WithNoMatches_ShouldReturnEmptyResult()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-2),
                LegalName = new LegalName { FirstName = "John", LastName = "Doe" },
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var query = new SearchEmployeesQuery { Name = "NonExistentEmployee" };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Results);
        Assert.Equal(0, result.TotalPages);
    }
}
