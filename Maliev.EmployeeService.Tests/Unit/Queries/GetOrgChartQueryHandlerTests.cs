using FluentAssertions;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Queries;

/// <summary>
/// Unit tests for GetOrgChartQueryHandler
/// </summary>
public class GetOrgChartQueryHandlerTests
{
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<ILogger<GetOrgChartQueryHandler>> _mockLogger;
    private readonly GetOrgChartQueryHandler _handler;

    public GetOrgChartQueryHandlerTests()
    {
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockLogger = new Mock<ILogger<GetOrgChartQueryHandler>>();
        _handler = new GetOrgChartQueryHandler(_mockEmployeeRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentManager_ShouldReturnNull()
    {
        // Arrange
        var managerId = Guid.NewGuid();

        _mockEmployeeRepository.Setup(x => x.GetWithDetailsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var query = new GetOrgChartQuery(managerId, MaxDepth: 3);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.OrgChart.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithManagerNoReports_ShouldReturnSingleNode()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var manager = new Employee
        {
            Id = managerId,
            EmployeeNumber = "MGR001",
            LegalName = new LegalName("John", "Manager"),
            PreferredName = "John",
            JobTitle = "Director",
            Department = new Department { Id = Guid.NewGuid(), Name = "Engineering" },
            EmploymentStatus = EmploymentStatus.Active,
            WorkLocation = "HQ"
        };

        _mockEmployeeRepository.Setup(x => x.GetWithDetailsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manager);

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        var query = new GetOrgChartQuery(managerId, MaxDepth: 3);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.OrgChart.Should().NotBeNull();
        result.OrgChart!.EmployeeId.Should().Be(managerId);
        result.OrgChart.FullName.Should().Be("John Manager");
        result.OrgChart.Level.Should().Be(0);
        result.OrgChart.DirectReports.Should().BeEmpty();
        result.OrgChart.DirectReportsCount.Should().Be(0);
        result.OrgChart.TotalReportsCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_WithTwoLevels_ShouldBuildHierarchy()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var directReport1Id = Guid.NewGuid();
        var directReport2Id = Guid.NewGuid();
        var dept = new Department { Id = Guid.NewGuid(), Name = "Engineering" };

        var manager = new Employee
        {
            Id = managerId,
            EmployeeNumber = "MGR001",
            LegalName = new LegalName("John", "Manager"),
            PreferredName = "John",
            JobTitle = "Director",
            Department = dept,
            EmploymentStatus = EmploymentStatus.Active,
            WorkLocation = "HQ"
        };

        var directReport1 = new Employee
        {
            Id = directReport1Id,
            EmployeeNumber = "EMP001",
            LegalName = new LegalName("Alice", "Developer"),
            PreferredName = "Alice",
            JobTitle = "Senior Dev",
            Department = dept,
            EmploymentStatus = EmploymentStatus.Active,
            WorkLocation = "HQ"
        };

        var directReport2 = new Employee
        {
            Id = directReport2Id,
            EmployeeNumber = "EMP002",
            LegalName = new LegalName("Bob", "Developer"),
            PreferredName = "Bob",
            JobTitle = "Developer",
            Department = dept,
            EmploymentStatus = EmploymentStatus.Active,
            WorkLocation = "Remote"
        };

        _mockEmployeeRepository.Setup(x => x.GetWithDetailsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manager);

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { directReport1, directReport2 });

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(directReport1Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(directReport2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        var query = new GetOrgChartQuery(managerId, MaxDepth: 3);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.OrgChart.Should().NotBeNull();
        result.OrgChart!.DirectReportsCount.Should().Be(2);
        result.OrgChart.TotalReportsCount.Should().Be(2);
        result.OrgChart.DirectReports.Should().HaveCount(2);

        var alice = result.OrgChart.DirectReports.First(r => r.FullName == "Alice Developer");
        alice.Level.Should().Be(1);
        alice.JobTitle.Should().Be("Senior Dev");

        var bob = result.OrgChart.DirectReports.First(r => r.FullName == "Bob Developer");
        bob.Level.Should().Be(1);
        bob.WorkLocation.Should().Be("Remote");
    }

    [Fact]
    public async Task HandleAsync_WithThreeLevels_ShouldBuildCompleteHierarchy()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var teamLeadId = Guid.NewGuid();
        var developerId = Guid.NewGuid();
        var dept = new Department { Id = Guid.NewGuid(), Name = "Engineering" };

        var manager = new Employee
        {
            Id = managerId,
            EmployeeNumber = "MGR001",
            LegalName = new LegalName("John", "Manager"),
            Department = dept,
            EmploymentStatus = EmploymentStatus.Active
        };

        var teamLead = new Employee
        {
            Id = teamLeadId,
            EmployeeNumber = "LEAD001",
            LegalName = new LegalName("Alice", "Lead"),
            Department = dept,
            EmploymentStatus = EmploymentStatus.Active
        };

        var developer = new Employee
        {
            Id = developerId,
            EmployeeNumber = "DEV001",
            LegalName = new LegalName("Bob", "Developer"),
            Department = dept,
            EmploymentStatus = EmploymentStatus.Active
        };

        _mockEmployeeRepository.Setup(x => x.GetWithDetailsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manager);

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { teamLead });

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(teamLeadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { developer });

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(developerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        var query = new GetOrgChartQuery(managerId, MaxDepth: 3);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.OrgChart.Should().NotBeNull();
        result.OrgChart!.Level.Should().Be(0);
        result.OrgChart.DirectReportsCount.Should().Be(1);
        result.OrgChart.TotalReportsCount.Should().Be(2); // 1 direct + 1 indirect

        var teamLeadNode = result.OrgChart.DirectReports.First();
        teamLeadNode.Level.Should().Be(1);
        teamLeadNode.FullName.Should().Be("Alice Lead");
        teamLeadNode.DirectReportsCount.Should().Be(1);
        teamLeadNode.TotalReportsCount.Should().Be(1);

        var developerNode = teamLeadNode.DirectReports.First();
        developerNode.Level.Should().Be(2);
        developerNode.FullName.Should().Be("Bob Developer");
        developerNode.DirectReportsCount.Should().Be(0);
        developerNode.TotalReportsCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_WithDepthLimit_ShouldStopAtMaxDepth()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var level1Id = Guid.NewGuid();
        var level2Id = Guid.NewGuid();
        var dept = new Department { Id = Guid.NewGuid(), Name = "Engineering" };

        var manager = new Employee
        {
            Id = managerId,
            EmployeeNumber = "MGR001",
            LegalName = new LegalName("Manager", "Top"),
            Department = dept,
            EmploymentStatus = EmploymentStatus.Active
        };

        var level1Employee = new Employee
        {
            Id = level1Id,
            EmployeeNumber = "L1-001",
            LegalName = new LegalName("Level", "One"),
            Department = dept,
            EmploymentStatus = EmploymentStatus.Active
        };

        var level2Employee = new Employee
        {
            Id = level2Id,
            EmployeeNumber = "L2-001",
            LegalName = new LegalName("Level", "Two"),
            Department = dept,
            EmploymentStatus = EmploymentStatus.Active
        };

        _mockEmployeeRepository.Setup(x => x.GetWithDetailsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manager);

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { level1Employee });

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(level1Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { level2Employee });

        // Query with MaxDepth = 1 (only manager and direct reports)
        var query = new GetOrgChartQuery(managerId, MaxDepth: 1);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.OrgChart.Should().NotBeNull();
        result.OrgChart!.Level.Should().Be(0);
        result.OrgChart.DirectReports.Should().HaveCount(1);

        var level1Node = result.OrgChart.DirectReports.First();
        level1Node.Level.Should().Be(1);
        level1Node.DirectReports.Should().BeEmpty(); // Should stop here due to depth limit
        level1Node.DirectReportsCount.Should().Be(1); // But still shows the count
    }

    [Fact]
    public async Task HandleAsync_WithMultipleBranches_ShouldCountAllDescendants()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var teamLead1Id = Guid.NewGuid();
        var teamLead2Id = Guid.NewGuid();
        var dev1Id = Guid.NewGuid();
        var dev2Id = Guid.NewGuid();
        var dev3Id = Guid.NewGuid();
        var dept = new Department { Id = Guid.NewGuid(), Name = "Engineering" };

        var manager = new Employee
        {
            Id = managerId,
            EmployeeNumber = "MGR001",
            LegalName = new LegalName("Manager", "Top"),
            Department = dept,
            EmploymentStatus = EmploymentStatus.Active
        };

        var teamLead1 = new Employee { Id = teamLead1Id, EmployeeNumber = "TL1", LegalName = new LegalName("Lead", "One"), Department = dept, EmploymentStatus = EmploymentStatus.Active };
        var teamLead2 = new Employee { Id = teamLead2Id, EmployeeNumber = "TL2", LegalName = new LegalName("Lead", "Two"), Department = dept, EmploymentStatus = EmploymentStatus.Active };
        var dev1 = new Employee { Id = dev1Id, EmployeeNumber = "DEV1", LegalName = new LegalName("Dev", "One"), Department = dept, EmploymentStatus = EmploymentStatus.Active };
        var dev2 = new Employee { Id = dev2Id, EmployeeNumber = "DEV2", LegalName = new LegalName("Dev", "Two"), Department = dept, EmploymentStatus = EmploymentStatus.Active };
        var dev3 = new Employee { Id = dev3Id, EmployeeNumber = "DEV3", LegalName = new LegalName("Dev", "Three"), Department = dept, EmploymentStatus = EmploymentStatus.Active };

        _mockEmployeeRepository.Setup(x => x.GetWithDetailsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manager);

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { teamLead1, teamLead2 });

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(teamLead1Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { dev1, dev2 });

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(teamLead2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { dev3 });

        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(dev1Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());
        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(dev2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());
        _mockEmployeeRepository.Setup(x => x.GetDirectReportsAsync(dev3Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        var query = new GetOrgChartQuery(managerId, MaxDepth: 3);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.OrgChart.Should().NotBeNull();
        result.OrgChart!.DirectReportsCount.Should().Be(2); // 2 team leads
        result.OrgChart.TotalReportsCount.Should().Be(5); // 2 team leads + 3 developers

        // Check first branch
        var tl1Node = result.OrgChart.DirectReports.First(r => r.EmployeeNumber == "TL1");
        tl1Node.DirectReportsCount.Should().Be(2);
        tl1Node.TotalReportsCount.Should().Be(2);

        // Check second branch
        var tl2Node = result.OrgChart.DirectReports.First(r => r.EmployeeNumber == "TL2");
        tl2Node.DirectReportsCount.Should().Be(1);
        tl2Node.TotalReportsCount.Should().Be(1);
    }
}
