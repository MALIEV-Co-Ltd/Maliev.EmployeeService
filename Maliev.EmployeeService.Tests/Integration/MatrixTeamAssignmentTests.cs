using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for matrix organization team assignments (User Story 5)
/// </summary>
[Collection("IntegrationTests")]
public class MatrixTeamAssignmentTests : PostgreSqlIntegrationTestBase
{
    private TeamRepository _teamRepository = null!;
    private EmployeeRepository _employeeRepository = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _teamRepository = new TeamRepository(Context);
        _employeeRepository = new EmployeeRepository(Context);
    }

    [Fact]
    public async Task Employee_CanBelongToMultipleTeams_MatrixOrganization()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            LegalName = new LegalName { FirstName = "John", LastName = "Doe" },
            ContactInformation = new ContactInformation { WorkEmail = "john.doe@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            DepartmentId = department.Id,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var productTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Product Team",
            TeamType = "Product",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var engineeringTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Engineering Team",
            TeamType = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        // Employee belongs to primary department (Engineering)
        // But also participates in Product team (matrix organization)
        var primaryAssignment = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            TeamId = engineeringTeam.Id,
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow
        };

        var secondaryAssignment = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            TeamId = productTeam.Id,
            IsPrimary = false,
            CreatedDate = DateTime.UtcNow
        };

        Context.Departments.Add(department);
        Context.Employees.Add(employee);
        Context.Teams.AddRange(productTeam, engineeringTeam);
        Context.EmployeeTeamAssignments.AddRange(primaryAssignment, secondaryAssignment);
        await Context.SaveChangesAsync();

        // Act
        var employeeTeams = await Context.EmployeeTeamAssignments
            .Where(eta => eta.EmployeeId == employee.Id)
            .Include(eta => eta.Team)
            .ToListAsync();

        // Assert
        Assert.Equal(2, employeeTeams.Count()); // Employee should belong to multiple teams
        Assert.Contains(employeeTeams, eta => eta.IsPrimary && eta.Team!.Name == "Engineering Team");
        Assert.Contains(employeeTeams, eta => !eta.IsPrimary && eta.Team!.Name == "Product Team");
    }

    [Fact]
    public async Task GetEmployeeTeams_ShouldReturnAllAssignments()
    {
        // Arrange
        var employee = CreateTestEmployee("EMP001");

        var teams = new[]
        {
            new Team { Id = Guid.NewGuid(), Name = "Team A", TeamType = "Engineering", IsActive = true, CreatedDate = DateTime.UtcNow },
            new Team { Id = Guid.NewGuid(), Name = "Team B", TeamType = "Product", IsActive = true, CreatedDate = DateTime.UtcNow },
            new Team { Id = Guid.NewGuid(), Name = "Team C", TeamType = "DevOps", IsActive = true, CreatedDate = DateTime.UtcNow }
        };

        Context.Employees.Add(employee);
        Context.Teams.AddRange(teams);

        // Assign employee to all 3 teams
        foreach (var team in teams)
        {
            Context.EmployeeTeamAssignments.Add(new EmployeeTeamAssignment
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                TeamId = team.Id,
                IsPrimary = team.Name == "Team A", // First team is primary
                CreatedDate = DateTime.UtcNow
            });
        }

        await Context.SaveChangesAsync();

        // Act
        var employeeTeams = await _teamRepository.GetTeamsByEmployeeAsync(employee.Id);

        // Assert
        Assert.Equal(3, employeeTeams.Count());
    }

    [Fact]
    public async Task TeamMembers_CanHavePrimaryAndSecondaryDesignations()
    {
        // Arrange
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Cross-Functional Team",
            TeamType = "Mixed",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var primaryMember = CreateTestEmployee("EMP001");
        var secondaryMember1 = CreateTestEmployee("EMP002");
        var secondaryMember2 = CreateTestEmployee("EMP003");

        Context.Teams.Add(team);
        Context.Employees.AddRange(primaryMember, secondaryMember1, secondaryMember2);

        Context.EmployeeTeamAssignments.AddRange(
            new EmployeeTeamAssignment
            {
                Id = Guid.NewGuid(),
                EmployeeId = primaryMember.Id,
                TeamId = team.Id,
                IsPrimary = true,
                CreatedDate = DateTime.UtcNow
            },
            new EmployeeTeamAssignment
            {
                Id = Guid.NewGuid(),
                EmployeeId = secondaryMember1.Id,
                TeamId = team.Id,
                IsPrimary = false,
                CreatedDate = DateTime.UtcNow
            },
            new EmployeeTeamAssignment
            {
                Id = Guid.NewGuid(),
                EmployeeId = secondaryMember2.Id,
                TeamId = team.Id,
                IsPrimary = false,
                CreatedDate = DateTime.UtcNow
            }
        );

        await Context.SaveChangesAsync();

        // Act
        var teamWithMembers = await _teamRepository.GetWithMembersAsync(team.Id);

        // Assert
        Assert.NotNull(teamWithMembers);
        Assert.Equal(3, teamWithMembers!.TeamMembers.Count);
        Assert.Single(teamWithMembers.TeamMembers, tm => tm.IsPrimary);
        Assert.Equal(2, teamWithMembers.TeamMembers.Count(tm => !tm.IsPrimary));
    }

    [Fact]
    public async Task MatrixOrganization_EmployeeReportsToManagerInDepartment_AndParticipatesInCrossFunctionalTeams()
    {
        // Arrange - Create organizational structure
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering Department",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var manager = CreateTestEmployee("MGR001");
        manager.DepartmentId = department.Id;

        var employee = CreateTestEmployee("EMP001");
        employee.DepartmentId = department.Id;
        employee.ManagerId = manager.Id; // Reports to manager in department

        var productTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Product Innovation Team",
            TeamType = "Product",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var devOpsTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Infrastructure Team",
            TeamType = "DevOps",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        Context.Departments.Add(department);
        Context.Employees.AddRange(manager, employee);
        Context.Teams.AddRange(productTeam, devOpsTeam);

        // Employee participates in cross-functional teams
        Context.EmployeeTeamAssignments.AddRange(
            new EmployeeTeamAssignment
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                TeamId = productTeam.Id,
                IsPrimary = false,
                CreatedDate = DateTime.UtcNow
            },
            new EmployeeTeamAssignment
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                TeamId = devOpsTeam.Id,
                IsPrimary = false,
                CreatedDate = DateTime.UtcNow
            }
        );

        await Context.SaveChangesAsync();

        // Act
        var employeeWithDetails = await _employeeRepository.GetWithDetailsAsync(employee.Id);
        var employeeTeams = await Context.EmployeeTeamAssignments
            .Where(eta => eta.EmployeeId == employee.Id)
            .Include(eta => eta.Team)
            .ToListAsync();

        // Assert - Verify matrix organization structure
        Assert.NotNull(employeeWithDetails);
        Assert.Equal(department.Id, employeeWithDetails!.DepartmentId);
        Assert.Equal(manager.Id, employeeWithDetails.ManagerId);
        Assert.Equal(2, employeeTeams.Count()); // Employee should participate in 2 cross-functional teams
        Assert.All(employeeTeams, eta => Assert.False(eta.IsPrimary)); // Cross-functional teams are secondary assignments
    }

    /// <summary>
    /// Helper method to create a test employee
    /// </summary>
    private Employee CreateTestEmployee(string employeeNumber)
    {
        return new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = employeeNumber,
            LegalName = new LegalName
            {
                FirstName = $"First{employeeNumber}",
                LastName = $"Last{employeeNumber}"
            },
            ContactInformation = new ContactInformation
            {
                WorkEmail = $"{employeeNumber}@company.com"
            },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
    }
}
