using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.EmployeeService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Infrastructure.Data;

/// <summary>
/// Database seeder for development and testing environments.
/// </summary>
public class DatabaseSeeder
{
    private readonly EmployeeDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        EmployeeDbContext context,
        IPasswordService passwordService,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _configuration = configuration;
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

            if (employees.Count < 1)
            {
                _logger.LogWarning("Not enough active employees found for team seeding (need at least 1, found {Count})", employees.Count);
                return;
            }

            var teams = new List<Team>
            {
                new Team
                {
                    Name = "Engineering Team",
                    Description = "Core engineering and development team",
                    TeamType = "Engineering",
                    TeamLeadId = employees[0 % employees.Count].Id,
                    IsActive = true
                },
                new Team
                {
                    Name = "Product Team",
                    Description = "Product management and strategy team",
                    TeamType = "Product",
                    TeamLeadId = employees[1 % employees.Count].Id,
                    IsActive = true
                },
                new Team
                {
                    Name = "DevOps Team",
                    Description = "Infrastructure and deployment team",
                    TeamType = "Engineering",
                    TeamLeadId = employees[2 % employees.Count].Id,
                    IsActive = true
                },
                new Team
                {
                    Name = "QA Team",
                    Description = "Quality assurance and testing team",
                    TeamType = "QA",
                    TeamLeadId = employees[3 % employees.Count].Id,
                    IsActive = true
                },
                new Team
                {
                    Name = "Design Team",
                    Description = "UX/UI design team",
                    TeamType = "Design",
                    TeamLeadId = employees[4 % employees.Count].Id,
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
    /// Seeds test user with password authentication for development testing.
    /// Test credentials are loaded from user secrets or environment variables.
    /// </summary>
    public async Task SeedTestUserAsync()
    {
        // Read from configuration (user secrets in dev, environment variables in production)
        var testEmail = _configuration["TestUser:Email"];
        var testPassword = _configuration["TestUser:Password"];
        var testPrincipalId = _configuration["TestUser:PrincipalId"];

        if (string.IsNullOrEmpty(testEmail) || string.IsNullOrEmpty(testPassword) || string.IsNullOrEmpty(testPrincipalId))
        {
            _logger.LogWarning("Test user credentials not configured in secrets, skipping seed");
            return;
        }

        try
        {
            if (await _context.Employees.AnyAsync(e => e.ContactInformation.WorkEmail == testEmail))
            {
                _logger.LogInformation("Test user {Email} already exists, skipping seed", testEmail);
                return;
            }

            _logger.LogInformation("Seeding test user {Email}...", testEmail);

            if (!Guid.TryParse(testPrincipalId, out var principalId))
            {
                _logger.LogWarning("Invalid TestUser:PrincipalId configured, skipping seed");
                return;
            }

            var testEmployee = new Employee
            {
                Id = Guid.NewGuid(),
                PrincipalId = principalId,
                EmployeeNumber = "EMP-TEST-001",
                LegalName = new LegalName
                {
                    FirstName = "Natthapol",
                    LastName = "Vanasrivilai",
                    MiddleName = null
                },
                PreferredName = "Natthapol",
                DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Nationality = "Thai",
                ContactInformation = new ContactInformation
                {
                    WorkEmail = testEmail,
                    PersonalEmail = "natthapol@example.com",
                    MobilePhone = "+66-81-234-5678"
                },
                EmploymentType = EmploymentType.FullTime,
                EmploymentStatus = EmploymentStatus.Active,
                JobTitle = "Software Engineer",
                WorkLocation = "Bangkok Office",
                StartDate = DateTime.UtcNow.AddYears(-2),
                ProbationEndDate = DateTime.UtcNow.AddYears(-2).AddMonths(3),
                PasswordHash = _passwordService.HashPassword(testPassword),
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            await _context.Employees.AddAsync(testEmployee);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded test user {Email} with PrincipalId {PrincipalId}",
                testEmail, principalId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding test user");
            throw;
        }
    }

    /// <summary>
    /// Seed all development data.
    /// </summary>
    public async Task SeedAllAsync()
    {
        await SeedTestUserAsync();
        await SeedTeamsAsync();
    }
}
