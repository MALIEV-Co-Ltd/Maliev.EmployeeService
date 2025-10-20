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
        result.TeamMembers.Should().HaveCount(5); // First page of 5
        result.TotalCount.Should().Be(10);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(5);
        result.TotalPages.Should().Be(2);

        // Verify first employee
        var firstMember = result.TeamMembers.First();
        firstMember.EmployeeNumber.Should().Be("EMP001");
        firstMember.FullName.Should().Be("FirstName1 LastName1");
        firstMember.PreferredName.Should().Be("Preferred1");
        firstMember.JobTitle.Should().Be("Developer 1");
        firstMember.DepartmentName.Should().Be("Engineering");
        firstMember.EmploymentStatus.Should().Be(EmploymentStatus.Active);
        firstMember.DirectReportsCount.Should().Be(0);
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
        result.TeamMembers.Should().HaveCount(5); // Second page of 5
        result.PageNumber.Should().Be(2);
        result.TotalPages.Should().Be(2);

        // Verify it returns employees 6-10
        result.TeamMembers.First().EmployeeNumber.Should().Be("EMP006");
        result.TeamMembers.Last().EmployeeNumber.Should().Be("EMP010");
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
        result.TeamMembers.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
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
        result.TeamMembers.Should().HaveCount(1);
        result.TeamMembers.First().DirectReportsCount.Should().Be(3);
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
        result.TeamMembers.Should().HaveCount(2); // Last page has only 2 items
        result.TotalCount.Should().Be(7);
        result.TotalPages.Should().Be(2);
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
        result.TeamMembers.Should().HaveCount(20); // Default page size
        result.PageSize.Should().Be(20);
        result.TotalPages.Should().Be(2);
    }
}
