using Microsoft.EntityFrameworkCore;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Infrastructure.Data.Extensions;
using Maliev.EmployeeService.Infrastructure.Data.Interceptors;

namespace Maliev.EmployeeService.Infrastructure.Data;

/// <summary>
/// Database context for Employee Service with support for encrypted sensitive fields
/// </summary>
public class EmployeeDbContext : DbContext
{
    private readonly IEncryptionService _encryptionService;
    private readonly AuditLogInterceptor _auditLogInterceptor;
    private readonly DatabaseMetricsInterceptor _databaseMetricsInterceptor;

    public EmployeeDbContext(
        DbContextOptions<EmployeeDbContext> options,
        IEncryptionService encryptionService,
        AuditLogInterceptor auditLogInterceptor,
        DatabaseMetricsInterceptor databaseMetricsInterceptor)
        : base(options)
    {
        _encryptionService = encryptionService;
        _auditLogInterceptor = auditLogInterceptor;
        _databaseMetricsInterceptor = databaseMetricsInterceptor;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditLogInterceptor, _databaseMetricsInterceptor);
        base.OnConfiguring(optionsBuilder);
    }

    // Authentication & Authorization
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Core Entities
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<EmploymentHistory> EmploymentHistories => Set<EmploymentHistory>();

    // Organizational Structure (User Story 5 - Matrix Organizations)
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<EmployeeTeamAssignment> EmployeeTeamAssignments => Set<EmployeeTeamAssignment>();

    // Personal Information
    public DbSet<PersonalDocument> PersonalDocuments => Set<PersonalDocument>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();
    public DbSet<Dependent> Dependents => Set<Dependent>();

    // Leave Management
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveApproval> LeaveApprovals => Set<LeaveApproval>();
    public DbSet<LeavePolicy> LeavePolicies => Set<LeavePolicy>();

    // Performance & Development
    public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<PerformanceImprovementPlan> PerformanceImprovementPlans => Set<PerformanceImprovementPlan>();
    public DbSet<Training> Trainings => Set<Training>();
    public DbSet<Certification> Certifications => Set<Certification>();

    // Training & Certification Management (User Story 8)
    public DbSet<TrainingRecord> TrainingRecords => Set<TrainingRecord>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<MandatoryTrainingRequirement> MandatoryTrainingRequirements => Set<MandatoryTrainingRequirement>();

    // Compensation & Benefits
    public DbSet<SalaryHistory> SalaryHistories => Set<SalaryHistory>();
    public DbSet<Benefit> Benefits => Set<Benefit>();
    public DbSet<EmployeeBenefit> EmployeeBenefits => Set<EmployeeBenefit>();
    public DbSet<CompensationRecord> CompensationRecords => Set<CompensationRecord>();
    public DbSet<BenefitsEnrollment> BenefitsEnrollments => Set<BenefitsEnrollment>();

    // Attendance & Time Tracking
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();

    // Discipline & Incidents
    public DbSet<DisciplinaryAction> DisciplinaryActions => Set<DisciplinaryAction>();
    public DbSet<Incident> Incidents => Set<Incident>();

    // Onboarding & Offboarding (User Story 10)
    public DbSet<OnboardingChecklist> OnboardingChecklists => Set<OnboardingChecklist>();
    public DbSet<OffboardingChecklist> OffboardingChecklists => Set<OffboardingChecklist>();
    public DbSet<OffboardingTask> OffboardingTasks => Set<OffboardingTask>();
    public DbSet<ExitInterview> ExitInterviews => Set<ExitInterview>();

    // Document Management (User Story 9)
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    // Work Authorization & Visa Tracking (User Story 11)
    public DbSet<WorkAuthorization> WorkAuthorizations => Set<WorkAuthorization>();

    // Bulk Operations (User Story 12)
    public DbSet<BulkJob> BulkJobs => Set<BulkJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Note: No separate IEntityTypeConfiguration classes exist in this assembly.
        // All entity configurations are defined inline below.
        // The previous ApplyConfigurationsFromAssembly call was causing a startup hang
        // due to unnecessary assembly scanning when no configurations exist.

        // Configure schema
        modelBuilder.HasDefaultSchema("employee");

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.EmployeeId);

            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(50);

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure AuditLog entity (immutable)
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(50);

            // Make AuditLog read-only
            entity.ToTable(t => t.HasCheckConstraint("CK_AuditLog_Immutable", "1=1"));
        });

        // Configure Employee entity (User Story 1)
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeNumber).IsUnique();
            entity.HasIndex(e => e.EmploymentStatus);
            entity.HasIndex(e => e.DepartmentId);
            entity.HasIndex(e => e.ManagerId);
            entity.HasIndex(e => e.EmploymentType); // Phase 16 - T387: Performance optimization
            entity.HasIndex(e => e.StartDate); // Phase 16 - T387: For GetEmployeesByStartDateAsync
            entity.HasIndex(e => e.TerminationDate); // Phase 16 - T387: For GetEmployeesByTerminationDateAsync
            entity.HasIndex(e => new { e.DepartmentId, e.EmploymentStatus }); // Phase 16 - T387: Composite for GetCountByDepartmentAsync

            entity.Property(e => e.EmployeeNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PreferredName).HasMaxLength(100);
            entity.Property(e => e.Nationality).HasMaxLength(50);
            entity.Property(e => e.JobTitle).HasMaxLength(200);
            entity.Property(e => e.WorkLocation).HasMaxLength(200);

            // NationalId is encrypted using value converter
            // Entity property remains plaintext, database stores encrypted value
            // EF Core tracks plaintext value (avoiding randomized IV concurrency issues)
            if (_encryptionService != null)
            {
                entity.HasEncryption(e => e.NationalId, _encryptionService, maxLength: 255);
            }

            // Configure value object: LegalName (Owned Entity - stored in same table)
            entity.OwnsOne(e => e.LegalName, ln =>
            {
                ln.Property(n => n.FirstName).HasMaxLength(100).IsRequired();
                ln.Property(n => n.LastName).HasMaxLength(100).IsRequired();
                ln.Property(n => n.MiddleName).HasMaxLength(100);
            });

            // Configure value object: ContactInformation (Owned Entity - stored in same table)
            entity.OwnsOne(e => e.ContactInformation, ci =>
            {
                ci.Property(c => c.WorkEmail).HasMaxLength(255).IsRequired();
                ci.Property(c => c.PersonalEmail).HasMaxLength(255);
                ci.Property(c => c.MobilePhone).HasMaxLength(20);
            });

            // Configure enums as strings
            entity.Property(e => e.EmploymentType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.EmploymentStatus).HasConversion<string>().HasMaxLength(50);

            // Configure relationships
            entity.HasOne(e => e.Manager)
                .WithMany(m => m.DirectReports)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Dotted line manager relationship (User Story 5 - Matrix Organizations)
            entity.HasOne(e => e.DottedLineManager)
                .WithMany(m => m.DottedLineReports)
                .HasForeignKey(e => e.DottedLineManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.EmergencyContacts)
                .WithOne(ec => ec.Employee)
                .HasForeignKey(ec => ec.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Team assignments relationship (User Story 5 - Matrix Organizations)
            entity.HasMany(e => e.TeamAssignments)
                .WithOne(ta => ta.Employee)
                .HasForeignKey(ta => ta.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Compensation records relationship (User Story 6 - Compensation & Benefits)
            entity.HasMany(e => e.CompensationRecords)
                .WithOne(cr => cr.Employee)
                .HasForeignKey(cr => cr.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Benefits enrollments relationship (User Story 6 - Compensation & Benefits)
            entity.HasMany(e => e.BenefitsEnrollments)
                .WithOne(be => be.Employee)
                .HasForeignKey(be => be.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure EmergencyContact entity
        modelBuilder.Entity<EmergencyContact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => new { e.EmployeeId, e.PriorityOrder });

            entity.Property(e => e.ContactName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Relationship).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255);
        });

        // Configure Department entity (User Story 2 - HR Employee Lifecycle)
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.ParentDepartmentId);
            entity.HasIndex(e => e.DepartmentHeadId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.ParentDepartmentId, e.IsActive }); // Phase 16 - T387: Composite for HasSubDepartmentsAsync

            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CostCenter).HasMaxLength(50);

            // Self-referencing hierarchy relationship
            entity.HasOne(e => e.ParentDepartment)
                .WithMany(d => d.SubDepartments)
                .HasForeignKey(e => e.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Department head relationship
            entity.HasOne(e => e.DepartmentHead)
                .WithMany()
                .HasForeignKey(e => e.DepartmentHeadId)
                .OnDelete(DeleteBehavior.Restrict);

            // Employees in department relationship (inverse of Employee.Department)
            entity.HasMany(e => e.Employees)
                .WithOne(emp => emp.Department)
                .HasForeignKey(emp => emp.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Update Employee.Department relationship to use proper navigation
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure LeaveBalance entity (User Story 4 - Leave Management)
        modelBuilder.Entity<LeaveBalance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => new { e.EmployeeId, e.LeaveType, e.Year }).IsUnique();

            entity.Property(e => e.LeaveType).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.TotalEntitlement).HasPrecision(5, 2);
            entity.Property(e => e.UsedDays).HasPrecision(5, 2);
            entity.Property(e => e.PendingDays).HasPrecision(5, 2);
            entity.Property(e => e.CarryForwardDays).HasPrecision(5, 2);

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure LeaveRequest entity (User Story 4 - Simplified Single Approver)
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.StartDate, e.EndDate });
            entity.HasIndex(e => e.ApproverId);
            entity.HasIndex(e => new { e.ApproverId, e.Status }); // Phase 16 - T387: Composite for GetPendingForApproverAsync

            entity.Property(e => e.LeaveType).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.ApprovalComments).HasMaxLength(1000);
            entity.Property(e => e.TotalDays).HasPrecision(5, 2);

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Approver)
                .WithMany()
                .HasForeignKey(e => e.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure LeavePolicy entity (User Story 4 - Policy Configuration)
        modelBuilder.Entity<LeavePolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LeaveType);
            entity.HasIndex(e => new { e.LeaveType, e.IsActive, e.EffectiveDate });

            entity.Property(e => e.LeaveType).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.AccrualRate).HasPrecision(5, 2).IsRequired();
            entity.Property(e => e.BlackoutPeriodsJson).HasMaxLength(2000);
            entity.Property(e => e.MinimumNoticeDays).HasDefaultValue(30);
            entity.Property(e => e.RequiresApproval).HasDefaultValue(true);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        // Configure LeaveApproval entity (User Story 2)
        modelBuilder.Entity<LeaveApproval>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LeaveRequestId);
            entity.HasIndex(e => e.ApproverId);
            entity.HasIndex(e => new { e.LeaveRequestId, e.ApprovalLevel }).IsUnique();

            entity.Property(e => e.Decision).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.Comments).HasMaxLength(1000);

            entity.HasOne(e => e.Approver)
                .WithMany()
                .HasForeignKey(e => e.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Team entity (User Story 5 - Matrix Organizations)
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.TeamLeadId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.TeamType, e.IsActive });

            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.TeamType).HasMaxLength(100).IsRequired();

            // Team lead relationship
            entity.HasOne(e => e.TeamLead)
                .WithMany()
                .HasForeignKey(e => e.TeamLeadId)
                .OnDelete(DeleteBehavior.Restrict);

            // Team members relationship (many-to-many via EmployeeTeamAssignment)
            entity.HasMany(e => e.TeamMembers)
                .WithOne(ta => ta.Team)
                .HasForeignKey(ta => ta.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure EmployeeTeamAssignment entity (User Story 5 - Matrix Organizations)
        modelBuilder.Entity<EmployeeTeamAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.TeamId);
            entity.HasIndex(e => new { e.EmployeeId, e.TeamId }).IsUnique();
            entity.HasIndex(e => new { e.EmployeeId, e.IsPrimary });

            // Relationships configured in Employee and Team entities
        });

        // Configure CompensationRecord entity (User Story 6 - Compensation & Benefits)
        modelBuilder.Entity<CompensationRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.EffectiveDate);
            entity.HasIndex(e => new { e.EmployeeId, e.EffectiveDate });

            // SalaryAmount is encrypted using value converter
            // Entity property remains plaintext, database stores encrypted value
            // EF Core tracks plaintext value (avoiding randomized IV concurrency issues)
            entity.HasEncryption(e => e.SalaryAmount, _encryptionService, maxLength: 255)
                .IsRequired();

            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired().HasDefaultValue("THB");
            entity.Property(e => e.ChangeReason).HasMaxLength(500).IsRequired();
            entity.Property(e => e.BonusStructure).HasMaxLength(1000);
            entity.Property(e => e.CommissionStructure).HasMaxLength(1000);

            // Relationship configured in Employee entity
        });

        // Configure BenefitsEnrollment entity (User Story 6 - Compensation & Benefits)
        modelBuilder.Entity<BenefitsEnrollment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.EnrollmentDate);
            entity.HasIndex(e => new { e.EmployeeId, e.EnrollmentDate });

            entity.Property(e => e.HealthInsurancePlan).HasMaxLength(200);
            entity.Property(e => e.RetirementContribution).HasMaxLength(500);
            entity.Property(e => e.BeneficiaryInformation).HasMaxLength(2000);

            // Relationship configured in Employee entity
        });

        // Configure PerformanceReview entity (User Story 7 - Performance Management)
        modelBuilder.Entity<PerformanceReview>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.ReviewerId);
            entity.HasIndex(e => new { e.EmployeeId, e.ReviewPeriodStart, e.ReviewPeriodEnd });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ReviewCycle);

            entity.Property(e => e.ReviewCycle).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.Rating).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Draft");
            entity.Property(e => e.Feedback).HasMaxLength(4000);
            entity.Property(e => e.SelfAssessment).HasMaxLength(4000);

            // Employee being reviewed relationship
            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.PerformanceReviews)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Reviewer (manager) relationship
            entity.HasOne(e => e.Reviewer)
                .WithMany(emp => emp.ConductedReviews)
                .HasForeignKey(e => e.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Goals relationship
            entity.HasMany(e => e.Goals)
                .WithOne(g => g.PerformanceReview)
                .HasForeignKey(g => g.PerformanceReviewId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Goal entity (User Story 7 - Performance Management)
        modelBuilder.Entity<Goal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.PerformanceReviewId);
            entity.HasIndex(e => e.TargetDate);
            entity.HasIndex(e => e.CompletionStatus);
            entity.HasIndex(e => new { e.EmployeeId, e.TargetDate });

            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            entity.Property(e => e.SuccessCriteria).HasMaxLength(2000);
            entity.Property(e => e.ProgressUpdates).HasMaxLength(4000);
            entity.Property(e => e.CompletionStatus).HasConversion<string>().HasMaxLength(50).IsRequired();

            // Employee relationship
            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.Goals)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Optional PerformanceReview relationship (configured in PerformanceReview entity)
        });

        // Configure PerformanceImprovementPlan entity (User Story 7 - Performance Management)
        modelBuilder.Entity<PerformanceImprovementPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.ManagerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.EmployeeId, e.StartDate, e.EndDate });
            entity.HasIndex(e => new { e.Status, e.EndDate });

            entity.Property(e => e.IssuesDocumented).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Milestones).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Active");
            entity.Property(e => e.ProgressNotes).HasMaxLength(4000);
            entity.Property(e => e.Outcome).HasMaxLength(2000);

            // Employee on PIP relationship
            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.PerformanceImprovementPlans)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Manager overseeing PIP relationship
            entity.HasOne(e => e.Manager)
                .WithMany(emp => emp.ManagedPIPs)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure TrainingRecord entity (User Story 8 - Training & Certification Management)
        modelBuilder.Entity<TrainingRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.CompletionDate);
            entity.HasIndex(e => e.ExpirationDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.EmployeeId, e.CompletionDate });
            entity.HasIndex(e => new { e.TrainingType, e.Status });

            entity.Property(e => e.CourseName).HasMaxLength(300).IsRequired();
            entity.Property(e => e.CertificateDocumentId).HasMaxLength(100);
            entity.Property(e => e.Provider).HasMaxLength(200);
            entity.Property(e => e.TrainingType).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50).IsRequired();

            // Employee relationship
            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Skill entity (User Story 8 - Training & Certification Management)
        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => new { e.EmployeeId, e.SkillName }).IsUnique();
            entity.HasIndex(e => e.IsDevelopmentArea);
            entity.HasIndex(e => e.LastAssessedDate);

            entity.Property(e => e.SkillName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ProficiencyLevel).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(2000);

            // Employee relationship
            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure MandatoryTrainingRequirement entity (User Story 8 - Training & Certification Management)
        modelBuilder.Entity<MandatoryTrainingRequirement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmploymentType);
            entity.HasIndex(e => e.JobRole);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.EmploymentType, e.JobRole, e.IsActive });

            entity.Property(e => e.EmploymentType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.JobRole).HasMaxLength(200);
            entity.Property(e => e.RequiredCourses).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.DeadlineDaysFromStart).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Priority).HasDefaultValue(1);
        });

        // Configure OnboardingChecklist entity (User Story 10 - Onboarding/Offboarding)
        modelBuilder.Entity<OnboardingChecklist>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => new { e.EmployeeId, e.DisplayOrder });
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.ResponsibleParty);
            entity.HasIndex(e => e.CompletionStatus);

            entity.Property(e => e.ItemDescription).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ResponsibleParty).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(2000);

            // Employee relationship
            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Completed by employee relationship
            entity.HasOne(e => e.CompletedByEmployee)
                .WithMany()
                .HasForeignKey(e => e.CompletedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure OffboardingChecklist entity (User Story 10 - Onboarding/Offboarding)
        modelBuilder.Entity<OffboardingChecklist>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => new { e.EmployeeId, e.DisplayOrder });
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.ResponsibleParty);
            entity.HasIndex(e => e.CompletionStatus);
            entity.HasIndex(e => e.BlocksFinalPaycheck);

            entity.Property(e => e.ItemDescription).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ResponsibleParty).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(2000);

            // Employee relationship
            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Completed by employee relationship
            entity.HasOne(e => e.CompletedByEmployee)
                .WithMany()
                .HasForeignKey(e => e.CompletedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Document entity (User Story 9 - Document Management)
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.DocumentType);
            entity.HasIndex(e => e.UploadDate);
            entity.HasIndex(e => e.ExpirationDate);
            entity.HasIndex(e => e.AccessLevel);
            entity.HasIndex(e => new { e.EmployeeId, e.DocumentType });
            entity.HasIndex(e => new { e.EmployeeId, e.IsArchived });
            entity.HasIndex(e => new { e.DocumentType, e.AccessLevel });

            // FileName and StoragePath are stored as plaintext (metadata only, not sensitive)
            // Actual file content is encrypted in Google Cloud Storage
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.StoragePath).HasMaxLength(1000).IsRequired();

            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.ContentType).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DocumentType).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.AccessLevel).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.IsArchived).HasDefaultValue(false);

            // Employee relationship
            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Uploaded by employee relationship
            entity.HasOne(e => e.UploadedByEmployee)
                .WithMany()
                .HasForeignKey(e => e.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Document versions relationship
            entity.HasMany(e => e.Versions)
                .WithOne(v => v.Document)
                .HasForeignKey(v => v.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure DocumentVersion entity (User Story 9 - Document Management)
        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.UploadDate);
            entity.HasIndex(e => new { e.DocumentId, e.VersionNumber }).IsUnique();

            // FileName and StoragePath are stored as plaintext (metadata only, not sensitive)
            // Actual file content is encrypted in Google Cloud Storage
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.StoragePath).HasMaxLength(1000).IsRequired();

            entity.Property(e => e.ChangeDescription).HasMaxLength(2000);
            entity.Property(e => e.ContentType).HasMaxLength(200).IsRequired();

            // Document relationship (configured in Document entity)

            // Uploaded by employee relationship
            entity.HasOne(e => e.UploadedByEmployee)
                .WithMany()
                .HasForeignKey(e => e.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure WorkAuthorization entity (User Story 11 - Work Authorization & Visa Tracking)
        modelBuilder.Entity<WorkAuthorization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.AuthorizationType);
            entity.HasIndex(e => e.ExpirationDate);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.EmployeeId, e.AuthorizationType, e.IsActive });
            entity.HasIndex(e => new { e.ExpirationDate, e.IsActive });

            entity.Property(e => e.AuthorizationType).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.DocumentNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IssuingAuthority).HasMaxLength(300).IsRequired();
            entity.Property(e => e.SponsorshipStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            // Employee relationship
            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Right-to-work document relationship (optional)
            entity.HasOne(e => e.RightToWorkDocument)
                .WithMany()
                .HasForeignKey(e => e.RightToWorkDocumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure BulkJob entity (User Story 12 - Bulk Operations)
        modelBuilder.Entity<BulkJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.JobType);
            entity.HasIndex(e => e.InitiatedByUserId);
            entity.HasIndex(e => new { e.Status, e.StartedAt });
            entity.HasIndex(e => e.CompletedAt);

            entity.Property(e => e.JobType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.Errors).HasColumnType("text");
            entity.Property(e => e.Metadata).HasColumnType("text");
            entity.Property(e => e.ResultData).HasColumnType("text");

            // Computed property (ProgressPercentage) is not mapped to database
            entity.Ignore(e => e.ProgressPercentage);
        });

        // Configure naming convention for PostgreSQL (snake_case)
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Skip owned entity types (value objects) - they are configured inline with their owner
            if (entity.IsOwned())
                continue;

            // Convert table names to snake_case
            entity.SetTableName(ToSnakeCase(entity.GetTableName() ?? entity.DisplayName()));

            // Convert column names to snake_case
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            // Convert foreign key names to snake_case
            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName() ?? $"pk_{entity.GetTableName()}"));
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName() ??
                    $"fk_{entity.GetTableName()}_{foreignKey.PrincipalEntityType.GetTableName()}"));
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName() ??
                    $"ix_{entity.GetTableName()}_{string.Join("_", index.Properties.Select(p => p.Name))}"));
            }
        }
    }

    /// <summary>
    /// Converts PascalCase string to snake_case for PostgreSQL naming convention
    /// </summary>
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsUpper(c))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }
}
