using Maliev.EmployeeService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Infrastructure.Data;

/// <summary>
/// Database seeder for development and testing environments
/// </summary>
public class DatabaseSeeder
{
    private readonly EmployeeServiceDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        EmployeeServiceDbContext context,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seed sample teams for development and testing
    /// </summary>
    public async Task SeedTeamsAsync()
    {
        try
        {
            // Check if teams already exist
            if (await _context.Teams.AnyAsync())
            {
                _logger.LogInformation("Teams already exist, skipping seed");
                return;
            }

            _logger.LogInformation("Seeding sample teams...");

            // Get some employees to assign as team leads and members
            var employees = await _context.Employees
                .Where(e => e.EmploymentStatus == Domain.Enums.EmploymentStatus.Active)
                .OrderBy(e => e.EmployeeNumber) // Explicit ordering for deterministic results
                .Take(15)
                .ToListAsync();

            if (!employees.Any())
            {
                _logger.LogWarning("No active employees found for team seeding");
                return;
            }

            // Create sample teams
            var teams = new List<Team>
            {
                new Team
                {
                    Name = "Engineering Team",
                    Description = "Core engineering and development team",
                    TeamType = "Engineering",
                    TeamLeadId = employees.Count > 0 ? employees[0].Id : null,
                    IsActive = true
                },
                new Team
                {
                    Name = "Product Team",
                    Description = "Product management and strategy team",
                    TeamType = "Product",
                    TeamLeadId = employees.Count > 1 ? employees[1].Id : null,
                    IsActive = true
                },
                new Team
                {
                    Name = "DevOps Team",
                    Description = "Infrastructure and deployment team",
                    TeamType = "Engineering",
                    TeamLeadId = employees.Count > 2 ? employees[2].Id : null,
                    IsActive = true
                },
                new Team
                {
                    Name = "QA Team",
                    Description = "Quality assurance and testing team",
                    TeamType = "QA",
                    TeamLeadId = employees.Count > 3 ? employees[3].Id : null,
                    IsActive = true
                },
                new Team
                {
                    Name = "Design Team",
                    Description = "UX/UI design team",
                    TeamType = "Design",
                    TeamLeadId = employees.Count > 4 ? employees[4].Id : null,
                    IsActive = true
                }
            };

            await _context.Teams.AddRangeAsync(teams);
            await _context.SaveChangesAsync();

            // Assign team members
            var assignments = new List<EmployeeTeamAssignment>();

            // Engineering Team members (team[0])
            for (int i = 0; i < Math.Min(5, employees.Count); i++)
            {
                assignments.Add(new EmployeeTeamAssignment
                {
                    EmployeeId = employees[i].Id,
                    TeamId = teams[0].Id,
                    IsPrimary = i < 3 // First 3 are primary members
                });
            }

            // Product Team members (team[1])
            for (int i = 1; i < Math.Min(4, employees.Count); i++)
            {
                assignments.Add(new EmployeeTeamAssignment
                {
                    EmployeeId = employees[i].Id,
                    TeamId = teams[1].Id,
                    IsPrimary = i == 1 // Only team lead is primary
                });
            }

            // DevOps Team members (team[2])
            for (int i = 2; i < Math.Min(6, employees.Count); i++)
            {
                assignments.Add(new EmployeeTeamAssignment
                {
                    EmployeeId = employees[i].Id,
                    TeamId = teams[2].Id,
                    IsPrimary = i < 4
                });
            }

            // QA Team members (team[3])
            if (employees.Count >= 7)
            {
                for (int i = 3; i < Math.Min(7, employees.Count); i++)
                {
                    assignments.Add(new EmployeeTeamAssignment
                    {
                        EmployeeId = employees[i].Id,
                        TeamId = teams[3].Id,
                        IsPrimary = i < 5
                    });
                }
            }

            // Design Team members (team[4])
            if (employees.Count >= 8)
            {
                for (int i = 4; i < Math.Min(8, employees.Count); i++)
                {
                    assignments.Add(new EmployeeTeamAssignment
                    {
                        EmployeeId = employees[i].Id,
                        TeamId = teams[4].Id,
                        IsPrimary = i == 4
                    });
                }
            }

            await _context.EmployeeTeamAssignments.AddRangeAsync(assignments);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded {TeamCount} teams with {AssignmentCount} member assignments",
                teams.Count, assignments.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding teams");
            throw;
        }
    }

    /// <summary>
    /// Seed all development data
    /// </summary>
    public async Task SeedAllAsync()
    {
        await SeedTeamsAsync();
        // Add more seed methods as needed
    }
}
