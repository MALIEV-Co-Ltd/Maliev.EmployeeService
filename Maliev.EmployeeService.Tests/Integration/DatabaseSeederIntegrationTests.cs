using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for DatabaseSeeder
/// </summary>
public class DatabaseSeederIntegrationTests : PostgreSqlIntegrationTestBase
{



    [Fact]
    public async Task SeedTeamsAsync_WithEmployees_ShouldCreateTeamsAndAssignments()
    {
        // Arrange - Create active employees first
        var loggerMock = new Mock<ILogger<DatabaseSeeder>>();
        var seeder = new DatabaseSeeder(Context, loggerMock.Object);
        var employees = new List<Employee>();
        for (int i = 1; i <= 15; i++)
        {
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = $"EMP{i:D3}",
                LegalName = new LegalName
                {
                    FirstName = $"First{i}",
                    LastName = $"Last{i}"
                },
                ContactInformation = new ContactInformation
                {
                    WorkEmail = $"emp{i}@company.com"
                },
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddMonths(-i),
                CreatedDate = DateTime.UtcNow
            };
            employees.Add(employee);
        }

        Context.Employees.AddRange(employees);
        await Context.SaveChangesAsync();

        // Act
        await seeder.SeedTeamsAsync();

        // Assert
        var teams = await Context.Teams.ToListAsync();
        var assignments = await Context.EmployeeTeamAssignments.ToListAsync();

        Assert.Equal(5, teams.Count()); // Seeder should create 5 teams
        Assert.Contains(teams, t => t.Name == "Engineering Team");
        Assert.Contains(teams, t => t.Name == "Product Team");
        Assert.Contains(teams, t => t.Name == "DevOps Team");
        Assert.Contains(teams, t => t.Name == "QA Team");
        Assert.Contains(teams, t => t.Name == "Design Team");

        Assert.NotEmpty(assignments); // Teams should have member assignments

        // Verify team leads are assigned
        var engineeringTeam = teams.First(t => t.Name == "Engineering Team");
        Assert.NotEqual(Guid.Empty, engineeringTeam.TeamLeadId);
        Assert.Equal(employees[0].Id, engineeringTeam.TeamLeadId);

        // Verify multiple team memberships (matrix organization)
        var employeeTeamCounts = assignments
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, TeamCount = g.Count() })
            .ToList();

        Assert.True(employeeTeamCounts.Any(e => e.TeamCount > 1),
            "At least one employee should belong to multiple teams (matrix organization)");

        // Verify primary assignments exist
        Assert.True(assignments.Any(a => a.IsPrimary == true),
            "Some team members should be marked as primary");
    }

    [Fact]
    public async Task SeedTeamsAsync_WhenTeamsExist_ShouldSkipSeeding()
    {
        // Arrange - Create one team
        var existingTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Existing Team",
            TeamType = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };
        Context.Teams.Add(existingTeam);
        await Context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<DatabaseSeeder>>();
        var seeder = new DatabaseSeeder(Context, loggerMock.Object);
        // Act
        await seeder.SeedTeamsAsync();

        // Assert
        var teams = await Context.Teams.ToListAsync();
        Assert.Single(teams); // Should not seed when teams already exist
        Assert.Equal("Existing Team", teams[0].Name);
    }

    [Fact]
    public async Task SeedTeamsAsync_WithNoEmployees_ShouldLogWarning()
    {
        var loggerMock = new Mock<ILogger<DatabaseSeeder>>();
        var seeder = new DatabaseSeeder(Context, loggerMock.Object);
        // Act
        await seeder.SeedTeamsAsync();

        // Assert
        var teams = await Context.Teams.ToListAsync();
        Assert.Empty(teams); // No teams should be created when no active employees exist
    }

    [Fact]
    public async Task SeedTeamsAsync_ShouldCreateTeamsWithCorrectTypes()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DatabaseSeeder>>();
        var seeder = new DatabaseSeeder(Context, loggerMock.Object);
        var employees = CreateTestEmployees(10);
        Context.Employees.AddRange(employees);
        await Context.SaveChangesAsync();

        // Act
        await seeder.SeedTeamsAsync();

        // Assert
        var teams = await Context.Teams.ToListAsync();

        Assert.Contains(teams, t => t.TeamType == "Engineering" && t.Name == "Engineering Team");
        Assert.Contains(teams, t => t.TeamType == "Product");
        Assert.Contains(teams, t => t.TeamType == "Engineering" && t.Name == "DevOps Team");
        Assert.Contains(teams, t => t.TeamType == "QA");
        Assert.Contains(teams, t => t.TeamType == "Design");

        Assert.All(teams, t => Assert.True(t.IsActive == true,
            "All seeded teams should be active"));
    }

    [Fact]
    public async Task SeedAllAsync_ShouldInvokeSeedTeamsAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DatabaseSeeder>>();
        var seeder = new DatabaseSeeder(Context, loggerMock.Object);
        var employees = CreateTestEmployees(10);
        Context.Employees.AddRange(employees);
        await Context.SaveChangesAsync();

        // Act
        await seeder.SeedAllAsync();

        // Assert
        var teams = await Context.Teams.ToListAsync();
        Assert.NotEmpty(teams); // SeedAllAsync should create teams
    }

    /// <summary>
    /// Helper method to create test employees
    /// </summary>
    private List<Employee> CreateTestEmployees(int count)
    {
        var employees = new List<Employee>();
        for (int i = 1; i <= count; i++)
        {
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                EmployeeNumber = $"EMP{i:D3}",
                LegalName = new LegalName
                {
                    FirstName = $"First{i}",
                    LastName = $"Last{i}"
                },
                ContactInformation = new ContactInformation
                {
                    WorkEmail = $"emp{i}@company.com"
                },
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddMonths(-i),
                CreatedDate = DateTime.UtcNow
            };
            employees.Add(employee);
        }
        return employees;
    }
}
