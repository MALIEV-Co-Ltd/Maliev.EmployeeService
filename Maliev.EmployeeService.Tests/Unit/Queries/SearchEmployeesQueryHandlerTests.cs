using FluentAssertions;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Queries;

/// <summary>
/// Unit tests for SearchEmployeesQueryHandler
/// Tests multi-criteria search with filtering, sorting, and pagination
/// </summary>
public class SearchEmployeesQueryHandlerTests
{
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly SearchEmployeesQueryHandler _handler;

    public SearchEmployeesQueryHandlerTests()
    {
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _handler = new SearchEmployeesQueryHandler(_mockEmployeeRepository.Object);
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
        result.TotalCount.Should().Be(1);
        result.Results.Should().HaveCount(1);
        result.Results[0].EmployeeNumber.Should().Be("EMP001");
        result.Results[0].FullName.Should().Contain("John");
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
        result.TotalCount.Should().Be(2); // Both Doe employees
        result.Results.Should().HaveCount(2);
        result.Results.Should().OnlyContain(r => r.FullName.Contains("Doe"));
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
        result.TotalCount.Should().Be(1);
        result.Results[0].EmployeeNumber.Should().Be("EMP001");
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
        result.TotalCount.Should().Be(1);
        result.Results[0].Email.Should().Be("john.doe@maliev.com");
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
        result.TotalCount.Should().Be(1);
        result.Results[0].DepartmentId.Should().Be(engineeringDeptId);
        result.Results[0].DepartmentName.Should().Be("Engineering");
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
        result.TotalCount.Should().Be(1);
        result.Results[0].Title.Should().Contain("Engineer");
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
        result.TotalCount.Should().Be(1);
        result.Results[0].EmploymentStatus.Should().Be("Active");
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
        result.TotalCount.Should().Be(1);
        result.Results[0].ManagerId.Should().Be(managerId);
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
        result.TotalCount.Should().Be(100);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(25);
        result.TotalPages.Should().Be(4); // 100 / 25 = 4
        result.Results.Should().HaveCount(25);
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
        result.PageSize.Should().Be(200); // Max enforced
        result.Results.Should().HaveCount(200);
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
        result.Results[0].FullName.Should().Contain("Anderson"); // Alice Anderson
        result.Results[1].FullName.Should().Contain("Baker"); // Bob Baker
        result.Results[2].FullName.Should().Contain("Brown"); // Charlie Brown
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
        result.Results[0].HireDate.Should().Be(new DateTime(2024, 1, 1));
        result.Results[1].HireDate.Should().Be(new DateTime(2024, 3, 1));
        result.Results[2].HireDate.Should().Be(new DateTime(2024, 6, 1));
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
        result.TotalCount.Should().Be(1);
        result.Results[0].EmployeeNumber.Should().Be("EMP001");
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
        result.TotalCount.Should().Be(0);
        result.Results.Should().BeEmpty();
        result.TotalPages.Should().Be(0);
    }
}
