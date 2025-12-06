using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Queries;

/// <summary>
/// Unit tests for GetTeamQueryHandler
/// </summary>
public class GetTeamQueryHandlerTests
{
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly GetTeamQueryHandler _handler;

    public GetTeamQueryHandlerTests()
    {
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _handler = new GetTeamQueryHandler(_mockEmployeeRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_WithDirectReports_ShouldReturnPaginatedTeam()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var department = new Department { Id = Guid.NewGuid(), Name = "Engineering" };

        // Create 10 direct reports
        var directReports = Enumerable.Range(1, 10).Select(i => new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = $"EMP{i:000}",
            LegalName = new LegalName($"FirstName{i}", $"LastName{i}"),
            PreferredName = $"Preferred{i}",
            JobTitle = $"Developer {i}",
            Department = department,
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            WorkLocation = "Office",
            ContactInformation = new ContactInformation { WorkEmail = $"employee{i}@maliev.com" },
            StartDate = DateTime.UtcNow.AddYears(-1)
        }).ToList();

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReports);

        // Setup for counting sub-reports (assume no sub-reports for simplicity)
        foreach (var employee in directReports)
        {
            _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(employee.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Employee>());
        }

        var query = new GetTeamQuery(managerId, PageNumber: 1, PageSize: 5);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(5, result.TeamMembers.Count()); // First page of 5
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(2, result.TotalPages);

        // Verify first employee
        var firstMember = result.TeamMembers.First();
        Assert.Equal("EMP001", firstMember.EmployeeNumber);
        Assert.Equal("FirstName1 LastName1", firstMember.FullName);
        Assert.Equal("Preferred1", firstMember.PreferredName);
        Assert.Equal("Developer 1", firstMember.JobTitle);
        Assert.Equal("Engineering", firstMember.DepartmentName);
        Assert.Equal(EmploymentStatus.Active, firstMember.EmploymentStatus);
        Assert.Equal(0, firstMember.DirectReportsCount);
    }

    [Fact]
    public async Task HandleAsync_WithSecondPage_ShouldReturnCorrectPage()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var directReports = Enumerable.Range(1, 10).Select(i => new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = $"EMP{i:000}",
            LegalName = new LegalName($"FirstName{i}", $"LastName{i}"),
            Department = new Department { Id = Guid.NewGuid(), Name = "Engineering" },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            ContactInformation = new ContactInformation()
        }).ToList();

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReports);

        foreach (var employee in directReports)
        {
            _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(employee.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Employee>());
        }

        var query = new GetTeamQuery(managerId, PageNumber: 2, PageSize: 5);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(5, result.TeamMembers.Count()); // Second page of 5
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(2, result.TotalPages);

        // Verify it returns employees 6-10
        Assert.Equal("EMP006", result.TeamMembers.First().EmployeeNumber);
        Assert.Equal("EMP010", result.TeamMembers.Last().EmployeeNumber);
    }

    [Fact]
    public async Task HandleAsync_WithNoDirectReports_ShouldReturnEmptyResult()
    {
        // Arrange
        var managerId = Guid.NewGuid();

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        var query = new GetTeamQuery(managerId, PageNumber: 1, PageSize: 20);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Empty(result.TeamMembers);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task HandleAsync_WithSubordinatesHavingReports_ShouldCountCorrectly()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var teamLeadId = Guid.NewGuid();

        var directReports = new List<Employee>
        {
            new Employee
            {
                Id = teamLeadId,
                EmployeeNumber = "EMP001",
                LegalName = new LegalName("John", "Doe"),
                Department = new Department { Id = Guid.NewGuid(), Name = "Engineering" },
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                ContactInformation = new ContactInformation()
            }
        };

        // Team lead has 3 sub-reports
        var subReports = Enumerable.Range(1, 3).Select(i => new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = $"SUB{i:000}",
            LegalName = new LegalName($"Sub{i}", "Report"),
            EmploymentStatus = EmploymentStatus.Active
        }).ToList();

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReports);

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(teamLeadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subReports);

        var query = new GetTeamQuery(managerId, PageNumber: 1, PageSize: 20);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Single(result.TeamMembers);
        Assert.Equal(3, result.TeamMembers.First().DirectReportsCount);
    }

    [Fact]
    public async Task HandleAsync_WithPartialLastPage_ShouldReturnCorrectCount()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var directReports = Enumerable.Range(1, 7).Select(i => new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = $"EMP{i:000}",
            LegalName = new LegalName($"FirstName{i}", $"LastName{i}"),
            Department = new Department { Id = Guid.NewGuid(), Name = "Engineering" },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            ContactInformation = new ContactInformation()
        }).ToList();

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReports);

        foreach (var employee in directReports)
        {
            _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(employee.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Employee>());
        }

        var query = new GetTeamQuery(managerId, PageNumber: 2, PageSize: 5);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(2, result.TeamMembers.Count()); // Last page has only 2 items
        Assert.Equal(7, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task HandleAsync_WithDefaultPagination_ShouldUse20ItemsPerPage()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var directReports = Enumerable.Range(1, 25).Select(i => new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = $"EMP{i:000}",
            LegalName = new LegalName($"FirstName{i}", $"LastName{i}"),
            Department = new Department { Id = Guid.NewGuid(), Name = "Engineering" },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            ContactInformation = new ContactInformation()
        }).ToList();

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReports);

        foreach (var employee in directReports)
        {
            _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(employee.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Employee>());
        }

        var query = new GetTeamQuery(managerId); // Uses defaults: Page 1, Size 20

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(20, result.TeamMembers.Count()); // Default page size
        Assert.Equal(20, result.PageSize);
        Assert.Equal(2, result.TotalPages);
    }
}
