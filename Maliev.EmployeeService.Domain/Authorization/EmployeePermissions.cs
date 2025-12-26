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

    // Compensation Operations
    /// <summary>Permission to read employee compensation data.</summary>
    public const string CompensationRead = "employee.compensation.read";
    /// <summary>Permission to update employee compensation data.</summary>
    public const string CompensationUpdate = "employee.compensation.update";

    // Department Operations
    /// <summary>Permission to manage departments.</summary>
    public const string DepartmentsManage = "employee.departments.manage";

    // Document Operations
    /// <summary>Permission to create employee documents.</summary>
    public const string DocumentsCreate = "employee.documents.create";
    /// <summary>Permission to read employee documents.</summary>
    public const string DocumentsRead = "employee.documents.read";
    /// <summary>Permission to update employee documents.</summary>
    public const string DocumentsUpdate = "employee.documents.update";
    /// <summary>Permission to delete employee documents.</summary>
    public const string DocumentsDelete = "employee.documents.delete";

    // Leave Operations
    /// <summary>Permission to create leave requests.</summary>
    public const string LeaveCreate = "employee.leave.create";
    /// <summary>Permission to read leave requests.</summary>
    public const string LeaveRead = "employee.leave.read";
    /// <summary>Permission to approve leave requests.</summary>
    public const string LeaveApprove = "employee.leave.approve";
    /// <summary>Permission to cancel leave requests.</summary>
    public const string LeaveCancel = "employee.leave.cancel";

    // Performance Operations
    /// <summary>Permission to create performance reviews.</summary>
    public const string PerformanceCreate = "employee.performance.create";
    /// <summary>Permission to read performance reviews.</summary>
    public const string PerformanceRead = "employee.performance.read";
    /// <summary>Permission to update performance reviews.</summary>
    public const string PerformanceUpdate = "employee.performance.update";

    // Training Operations
    /// <summary>Permission to create training records.</summary>
    public const string TrainingCreate = "employee.training.create";
    /// <summary>Permission to read training records.</summary>
    public const string TrainingRead = "employee.training.read";
    /// <summary>Permission to assign training to employees.</summary>
    public const string TrainingAssign = "employee.training.assign";

    // Team Operations
    /// <summary>Permission to read team information.</summary>
    public const string TeamsRead = "employee.teams.read";
    /// <summary>Permission to manage teams.</summary>
    public const string TeamsManage = "employee.teams.manage";

    // Search Operations
    /// <summary>Permission to search employees.</summary>
    public const string EmployeeSearch = "employee.employees.search";

    // Onboarding/Offboarding
    /// <summary>Permission to manage employee onboarding.</summary>
    public const string OnboardingManage = "employee.onboarding.manage";
    /// <summary>Permission to manage work authorization documents.</summary>
    public const string WorkAuthManage = "employee.workauth.manage";

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
    /// All permissions defined for the Employee Service.
    /// </summary>
    public static readonly string[] All = new[]
    {
        ProfilesCreate, ProfilesRead, ProfilesUpdate, ProfilesDelete,
        CompensationRead, CompensationUpdate,
        DepartmentsManage,
        DocumentsCreate, DocumentsRead, DocumentsUpdate, DocumentsDelete,
        LeaveCreate, LeaveRead, LeaveApprove, LeaveCancel,
        PerformanceCreate, PerformanceRead, PerformanceUpdate,
        TrainingCreate, TrainingRead, TrainingAssign,
        TeamsRead, TeamsManage,
        EmployeeSearch,
        OnboardingManage,
        WorkAuthManage,
        ReportsView, ReportsGenerate,
        AdminBackgroundJobs, AdminSystemConfig,
        SystemAdmin
    };
}
