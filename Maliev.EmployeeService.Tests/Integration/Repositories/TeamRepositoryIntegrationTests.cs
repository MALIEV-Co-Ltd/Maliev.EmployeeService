using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for TeamRepository using in-memory database (User Story 5)
/// </summary>
public class TeamRepositoryIntegrationTests : PostgreSqlIntegrationTestBase
{



    [Fact]
    public async Task GetWithMembersAsync_ShouldReturnTeamWithMembers()
    {
        // Arrange
        var repository = new TeamRepository(Context);
        var teamLead = CreateTestEmployee("EMP001", "John Doe");
        var member1 = CreateTestEmployee("EMP002", "Jane Smith");
        var member2 = CreateTestEmployee("EMP003", "Bob Johnson");

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Engineering Team",
            Description = "Backend engineering team",
            TeamType = "Engineering",
            TeamLeadId = teamLead.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var assignment1 = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = member1.Id,
            TeamId = team.Id,
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow
        };

        var assignment2 = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = member2.Id,
            TeamId = team.Id,
            IsPrimary = false,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.AddRange(teamLead, member1, member2);
        Context.Teams.Add(team);
        Context.EmployeeTeamAssignments.AddRange(assignment1, assignment2);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.GetWithMembersAsync(team.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Engineering Team", result!.Name);
        Assert.NotNull(result.TeamLead);
        Assert.Equal("EMP001", result.TeamLead!.EmployeeNumber);
        Assert.Equal(2, result.TeamMembers.Count());
        Assert.Contains(result.TeamMembers, tm => tm.EmployeeId == member1.Id && tm.IsPrimary);
        Assert.Contains(result.TeamMembers, tm => tm.EmployeeId == member2.Id && !tm.IsPrimary);
    }

    [Fact]
    public async Task GetWithTeamLeadAsync_ShouldReturnTeamWithTeamLead()
    {
        // Arrange
        var repository = new TeamRepository(Context);
        var teamLead = CreateTestEmployee("EMP001", "John Doe");

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Product Team",
            TeamType = "Product",
            TeamLeadId = teamLead.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(teamLead);
        Context.Teams.Add(team);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.GetWithTeamLeadAsync(team.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result!.TeamLead);
        Assert.Equal("John Doe", result.TeamLead!.FullName);
    }

    [Fact]
    public async Task GetAllActiveAsync_ShouldReturnOnlyActiveTeams()
    {
        // Arrange
        var repository = new TeamRepository(Context);
        var teamLead = CreateTestEmployee("EMP001", "John Doe");

        var activeTeam1 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Active Team 1",
            TeamType = "Engineering",
            TeamLeadId = teamLead.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var activeTeam2 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Active Team 2",
            TeamType = "Product",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var inactiveTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Team",
            TeamType = "Engineering",
            IsActive = false,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(teamLead);
        Context.Teams.AddRange(activeTeam1, activeTeam2, inactiveTeam);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllActiveAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, t => t.Name == "Active Team 1");
        Assert.Contains(result, t => t.Name == "Active Team 2");
        Assert.DoesNotContain(result, t => t.Name == "Inactive Team");
    }

    [Fact]
    public async Task GetByTeamTypeAsync_ShouldReturnTeamsOfSpecificType()
    {
        // Arrange
        var repository = new TeamRepository(Context);
        var team1 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Engineering Team 1",
            TeamType = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var team2 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Engineering Team 2",
            TeamType = "Engineering",
            IsActive = true,
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

        Context.Teams.AddRange(team1, team2, productTeam);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.GetByTeamTypeAsync("Engineering");

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, t => Assert.Equal("Engineering", t.TeamType));
    }

    [Fact]
    public async Task GetByTeamLeadAsync_ShouldReturnTeamsLedByEmployee()
    {
        // Arrange
        var repository = new TeamRepository(Context);
        var teamLead = CreateTestEmployee("EMP001", "John Doe");
        var otherLead = CreateTestEmployee("EMP002", "Jane Smith");

        var team1 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team 1",
            TeamType = "Engineering",
            TeamLeadId = teamLead.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var team2 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team 2",
            TeamType = "Product",
            TeamLeadId = teamLead.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var otherTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Other Team",
            TeamType = "Engineering",
            TeamLeadId = otherLead.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.AddRange(teamLead, otherLead);
        Context.Teams.AddRange(team1, team2, otherTeam);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.GetByTeamLeadAsync(teamLead.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, t => Assert.Equal(teamLead.Id, t.TeamLeadId));
        Assert.Contains(result, t => t.Name == "Team 1");
        Assert.Contains(result, t => t.Name == "Team 2");
    }

    [Fact]
    public async Task GetTeamsByEmployeeAsync_ShouldReturnEmployeeTeams()
    {
        // Arrange
        var repository = new TeamRepository(Context);
        var employee = CreateTestEmployee("EMP001", "John Doe");

        var team1 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Engineering Team",
            TeamType = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var team2 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Product Team",
            TeamType = "Product",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var team3 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Other Team",
            TeamType = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var assignment1 = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            TeamId = team1.Id,
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow
        };

        var assignment2 = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            TeamId = team2.Id,
            IsPrimary = false,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(employee);
        Context.Teams.AddRange(team1, team2, team3);
        Context.EmployeeTeamAssignments.AddRange(assignment1, assignment2);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.GetTeamsByEmployeeAsync(employee.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, t => t.Name == "Engineering Team");
        Assert.Contains(result, t => t.Name == "Product Team");
        Assert.DoesNotContain(result, t => t.Name == "Other Team");
    }

    [Fact]
    public async Task IsEmployeeMemberAsync_WhenMember_ShouldReturnTrue()
    {
        // Arrange
        var repository = new TeamRepository(Context);
        var employee = CreateTestEmployee("EMP001", "John Doe");

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Engineering Team",
            TeamType = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var assignment = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            TeamId = team.Id,
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(employee);
        Context.Teams.Add(team);
        Context.EmployeeTeamAssignments.Add(assignment);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.IsEmployeeMemberAsync(employee.Id, team.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsEmployeeMemberAsync_WhenNotMember_ShouldReturnFalse()
    {
        // Arrange
        var repository = new TeamRepository(Context);
        var employee = CreateTestEmployee("EMP001", "John Doe");

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Engineering Team",
            TeamType = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(employee);
        Context.Teams.Add(team);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.IsEmployeeMemberAsync(employee.Id, team.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetPrimaryTeamAsync_ShouldReturnPrimaryTeam()
    {
        // Arrange
        var repository = new TeamRepository(Context);
        var employee = CreateTestEmployee("EMP001", "John Doe");

        var primaryTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Primary Team",
            TeamType = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var secondaryTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Secondary Team",
            TeamType = "Product",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var primaryAssignment = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            TeamId = primaryTeam.Id,
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow
        };

        var secondaryAssignment = new EmployeeTeamAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            TeamId = secondaryTeam.Id,
            IsPrimary = false,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(employee);
        Context.Teams.AddRange(primaryTeam, secondaryTeam);
        Context.EmployeeTeamAssignments.AddRange(primaryAssignment, secondaryAssignment);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.GetPrimaryTeamAsync(employee.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Primary Team", result!.Name);
    }

    [Fact]
    public async Task GetPrimaryTeamAsync_WhenNoPrimaryTeam_ShouldReturnNull()
    {
        // Arrange
        var repository = new TeamRepository(Context);
        var employee = CreateTestEmployee("EMP001", "John Doe");
        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.GetPrimaryTeamAsync(employee.Id);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Helper method to create a test employee with minimal required fields
    /// </summary>
    private Employee CreateTestEmployee(string employeeNumber, string fullName)
    {
        var names = fullName.Split(' ');
        return new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = employeeNumber,
            LegalName = new LegalName
            {
                FirstName = names[0],
                LastName = names.Length > 1 ? names[1] : "Unknown"
            },
            ContactInformation = new ContactInformation
            {
                WorkEmail = $"{employeeNumber.ToLower()}@company.com"
            },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
    }
}
