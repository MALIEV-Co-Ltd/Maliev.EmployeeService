namespace Maliev.EmployeeService.Domain.Authorization;

/// <summary>
/// Defines all permissions for the Employee Service.
/// Permissions follow the format: employee.{resource}.{action}
/// </summary>
public static class EmployeePermissions
{
    // Profile Operations
    /// <summary>Permission to create employee profiles.</summary>
    public const string ProfilesCreate = "employee.profiles.create";
    /// <summary>Permission to read employee profiles.</summary>
    public const string ProfilesRead = "employee.profiles.read";
    /// <summary>Permission to update employee profiles.</summary>
    public const string ProfilesUpdate = "employee.profiles.update";
    /// <summary>Permission to delete employee profiles.</summary>
    public const string ProfilesDelete = "employee.profiles.delete";

    // Department Operations
    /// <summary>Permission to manage departments.</summary>
    public const string DepartmentsManage = "employee.departments.manage";

    // Team Operations
    /// <summary>Permission to read team information.</summary>
    public const string TeamsRead = "employee.teams.read";
    /// <summary>Permission to manage teams.</summary>
    public const string TeamsManage = "employee.teams.manage";

    // Search Operations
    /// <summary>Permission to search employees.</summary>
    public const string EmployeeSearch = "employee.employees.search";

    // Reporting Operations
    /// <summary>Permission to view employee reports.</summary>
    public const string ReportsView = "employee.reports.view";
    /// <summary>Permission to generate employee reports.</summary>
    public const string ReportsGenerate = "employee.reports.generate";

    // Administrative Operations
    /// <summary>Permission to manage background jobs.</summary>
    public const string AdminBackgroundJobs = "employee.admin.backgroundjobs";
    /// <summary>Permission to manage system configuration.</summary>
    public const string AdminSystemConfig = "employee.admin.systemconfig";
    /// <summary>Full system administrator permission.</summary>
    public const string SystemAdmin = "employee.system.admin";

    /// <summary>
    /// Collection of all defined employee permissions with descriptions.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { ProfilesCreate, "Create employee profiles" },
        { ProfilesRead, "Read employee profiles" },
        { ProfilesUpdate, "Update employee profiles" },
        { ProfilesDelete, "Delete employee profiles" },
        { DepartmentsManage, "Manage departments" },
        { TeamsRead, "Read team information" },
        { TeamsManage, "Manage teams" },
        { EmployeeSearch, "Search employees" },
        { ReportsView, "View reports" },
        { ReportsGenerate, "Generate reports" },
        { AdminBackgroundJobs, "Manage background jobs" },
        { AdminSystemConfig, "Manage system configuration" },
        { SystemAdmin, "Full system administrative access" }
    };

    /// <summary>
    /// All permissions defined for the Employee Service.
    /// </summary>
    public static readonly string[] All = AllWithDescriptions.Keys.ToArray();
}
