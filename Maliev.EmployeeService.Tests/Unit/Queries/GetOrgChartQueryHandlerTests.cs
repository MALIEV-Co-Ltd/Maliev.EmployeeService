using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Xunit;
using Maliev.Aspire.ServiceDefaults.IAM;
using Microsoft.Extensions.Configuration;
using Maliev.EmployeeService.Domain.Authorization;
using Moq;

namespace Maliev.EmployeeService.Tests.Unit.Queries;

/// <summary>
/// Unit tests for GetOrgChartQueryHandler
/// </summary>
public class GetOrgChartQueryHandlerTests
{
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<IIamServiceClient> _mockIamClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ILogger<GetOrgChartQueryHandler>> _mockLogger;
    private readonly GetOrgChartQueryHandler _handler;

    public GetOrgChartQueryHandlerTests()
    {
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockIamClient = new Mock<IIamServiceClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockLogger = new Mock<ILogger<GetOrgChartQueryHandler>>();

        var principalId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.PrincipalId).Returns(principalId);
        _mockCurrentUserService.Setup(x => x.PrincipalIdentifier).Returns(principalId.ToString());
        _mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new GetOrgChartQueryHandler(
            _mockEmployeeRepository.Object,
            _mockLogger.Object,
            _mockIamClient.Object,
            _mockConfiguration.Object,
            _mockCurrentUserService.Object);
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
        Assert.Null(result.OrgChart);
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
        Assert.NotNull(result.OrgChart);
        Assert.Equal(managerId, result.OrgChart!.EmployeeId);
        Assert.Equal("John Manager", result.OrgChart.FullName);
        Assert.Equal(0, result.OrgChart.Level);
        Assert.Empty(result.OrgChart.DirectReports);
        Assert.Equal(0, result.OrgChart.DirectReportsCount);
        Assert.Equal(0, result.OrgChart.TotalReportsCount);
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
        Assert.NotNull(result.OrgChart);
        Assert.Equal(2, result.OrgChart!.DirectReportsCount);
        Assert.Equal(2, result.OrgChart.TotalReportsCount);
        Assert.Equal(2, result.OrgChart.DirectReports.Count);

        var alice = result.OrgChart.DirectReports.First(r => r.FullName == "Alice Developer");
        Assert.Equal(1, alice.Level);
        Assert.Equal("Senior Dev", alice.JobTitle);

        var bob = result.OrgChart.DirectReports.First(r => r.FullName == "Bob Developer");
        Assert.Equal(1, bob.Level);
        Assert.Equal("Remote", bob.WorkLocation);
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
        Assert.NotNull(result.OrgChart);
        Assert.Equal(0, result.OrgChart!.Level);
        Assert.Equal(1, result.OrgChart.DirectReportsCount);
        Assert.Equal(2, result.OrgChart.TotalReportsCount); // 1 direct + 1 indirect

        var teamLeadNode = result.OrgChart.DirectReports.First();
        Assert.Equal(1, teamLeadNode.Level);
        Assert.Equal("Alice Lead", teamLeadNode.FullName);
        Assert.Equal(1, teamLeadNode.DirectReportsCount);
        Assert.Equal(1, teamLeadNode.TotalReportsCount);

        var developerNode = teamLeadNode.DirectReports.First();
        Assert.Equal(2, developerNode.Level);
        Assert.Equal("Bob Developer", developerNode.FullName);
        Assert.Equal(0, developerNode.DirectReportsCount);
        Assert.Equal(0, developerNode.TotalReportsCount);
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
        Assert.NotNull(result.OrgChart);
        Assert.Equal(0, result.OrgChart!.Level);
        Assert.Single(result.OrgChart.DirectReports);

        var level1Node = result.OrgChart.DirectReports.First();
        Assert.Equal(1, level1Node.Level);
        Assert.Empty(level1Node.DirectReports); // Should stop here due to depth limit
        Assert.Equal(1, level1Node.DirectReportsCount); // But still shows the count
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
        Assert.NotNull(result.OrgChart);
        Assert.Equal(2, result.OrgChart!.DirectReportsCount); // 2 team leads
        Assert.Equal(5, result.OrgChart.TotalReportsCount); // 2 team leads + 3 developers

        // Check first branch
        var tl1Node = result.OrgChart.DirectReports.First(r => r.EmployeeNumber == "TL1");
        Assert.Equal(2, tl1Node.DirectReportsCount);
        Assert.Equal(2, tl1Node.TotalReportsCount);

        // Check second branch
        var tl2Node = result.OrgChart.DirectReports.First(r => r.EmployeeNumber == "TL2");
        Assert.Equal(1, tl2Node.DirectReportsCount);
        Assert.Equal(1, tl2Node.TotalReportsCount);
    }
}
