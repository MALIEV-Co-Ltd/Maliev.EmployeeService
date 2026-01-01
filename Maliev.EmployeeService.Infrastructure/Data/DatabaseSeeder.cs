using Maliev.EmployeeService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Infrastructure.Data;

/// <summary>
/// Database seeder for development and testing environments.
/// </summary>
public class DatabaseSeeder
{
    private readonly EmployeeDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        EmployeeDbContext context,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seed sample teams for development and testing.
    /// </summary>
    public async Task SeedTeamsAsync()
    {
        try
        {
            if (await _context.Teams.AnyAsync())
            {
                _logger.LogInformation("Teams already exist, skipping seed");
                return;
            }

            _logger.LogInformation("Seeding sample teams...");

            var employees = await _context.Employees
                .Where(e => e.EmploymentStatus == Domain.Enums.EmploymentStatus.Active)
                .OrderBy(e => e.EmployeeNumber)
                .Take(15)
                .ToListAsync();

            if (employees.Count < 5)
            {
                _logger.LogWarning("Not enough active employees found for team seeding (need at least 5, found {Count})", employees.Count);
                return;
            }

            var teams = new List<Team>
            {
                new Team
                {
                    Name = "Engineering Team",
                    Description = "Core engineering and development team",
                    TeamType = "Engineering",
                    TeamLeadId = employees[0].Id,
                    IsActive = true
                },
                new Team
                {
                    Name = "Product Team",
                    Description = "Product management and strategy team",
                    TeamType = "Product",
                    TeamLeadId = employees[1].Id,
                    IsActive = true
                },
                new Team
                {
                    Name = "DevOps Team",
                    Description = "Infrastructure and deployment team",
                    TeamType = "Engineering",
                    TeamLeadId = employees[2].Id,
                    IsActive = true
                },
                new Team
                {
                    Name = "QA Team",
                    Description = "Quality assurance and testing team",
                    TeamType = "QA",
                    TeamLeadId = employees[3].Id,
                    IsActive = true
                },
                new Team
                {
                    Name = "Design Team",
                    Description = "UX/UI design team",
                    TeamType = "Design",
                    TeamLeadId = employees[4].Id,
                    IsActive = true
                }
            };

            await _context.Teams.AddRangeAsync(teams);
            await _context.SaveChangesAsync();

            var assignments = new List<EmployeeTeamAssignment>();

            for (int i = 0; i < Math.Min(5, employees.Count); i++)
            {
                assignments.Add(new EmployeeTeamAssignment
                {
                    EmployeeId = employees[i].Id,
                    TeamId = teams[0].Id,
                    IsPrimary = i < 3
                });
            }

            for (int i = 1; i < Math.Min(4, employees.Count); i++)
            {
                assignments.Add(new EmployeeTeamAssignment
                {
                    EmployeeId = employees[i].Id,
                    TeamId = teams[1].Id,
                    IsPrimary = i == 1
                });
            }

            for (int i = 2; i < Math.Min(6, employees.Count); i++)
            {
                assignments.Add(new EmployeeTeamAssignment
                {
                    EmployeeId = employees[i].Id,
                    TeamId = teams[2].Id,
                    IsPrimary = i < 4
                });
            }

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
    /// Seed all development data.
    /// </summary>
    public async Task SeedAllAsync()
    {
        await SeedTeamsAsync();
    }
}
