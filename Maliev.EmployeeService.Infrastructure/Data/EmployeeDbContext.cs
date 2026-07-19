using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Sagas;
using Maliev.EmployeeService.Infrastructure.Data.Extensions;
using Maliev.EmployeeService.Infrastructure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;

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
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();

    // Attendance & Time Tracking
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();

    // Discipline & Incidents
    public DbSet<Incident> Incidents => Set<Incident>();

    // Bulk Operations (User Story 12)
    public DbSet<BulkJob> BulkJobs => Set<BulkJob>();

    // Saga Infrastructure (Phase 2 - T068, T069)
    public DbSet<SagaState> SagaStates => Set<SagaState>();
    public DbSet<SagaStepHistory> SagaStepHistories => Set<SagaStepHistory>();
    public DbSet<EmployeeTerminationSagaState> EmployeeTerminationSagaStates => Set<EmployeeTerminationSagaState>();

    // User Preferences (Dashboard Persistence)
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Note: No separate IEntityTypeConfiguration classes exist in this assembly.
        // All entity configurations are defined inline below.
        // The previous ApplyConfigurationsFromAssembly call was causing a startup hang
        // due to unnecessary assembly scanning when no configurations exist.

        // Configure schema
        modelBuilder.HasDefaultSchema("employee");

        // Configure AuditLog entity (immutable)
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.PrincipalId);

            entity.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(50);

            // Make AuditLog read-only
            entity.ToTable(t => t.HasCheckConstraint("CK_AuditLog_Immutable", "1=1"));
        });

        // Configure Employee entity (User Story 1)
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.EmployeeNumber).IsUnique();
            entity.HasIndex(e => e.PrincipalId).IsUnique();
            entity.HasIndex(e => e.EmploymentStatus);
            entity.HasIndex(e => e.DepartmentId);
            entity.HasIndex(e => e.ManagerId);
            entity.HasIndex(e => e.EmploymentType); // Phase 16 - T387: Performance optimization
            entity.HasIndex(e => e.StartDate); // Phase 16 - T387: For GetEmployeesByStartDateAsync
            entity.HasIndex(e => e.TerminationDate); // Phase 16 - T387: For GetEmployeesByTerminationDateAsync
            entity.HasIndex(e => new { e.DepartmentId, e.EmploymentStatus }); // Phase 16 - T387: Composite for GetCountByDepartmentAsync

            entity.Property(e => e.PrincipalId).IsRequired();
            entity.Property(e => e.EmployeeNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PreferredName).HasMaxLength(100);
            entity.Property(e => e.Nationality).HasMaxLength(50);
            entity.Property(e => e.JobTitle).HasMaxLength(200);
            entity.Property(e => e.WorkLocation).HasMaxLength(200);

            // NationalId is encrypted using value converter
            // Entity property remains plaintext, database stores encrypted value
            ArgumentNullException.ThrowIfNull(_encryptionService);
            entity.HasEncryption(e => e.NationalId, _encryptionService, maxLength: 255);

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
                .WithMany(d => d.Employees)
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
        });

        // Configure EmploymentHistory entity
        modelBuilder.Entity<EmploymentHistory>(entity =>
        {
            entity.Property(e => e.EventType).HasConversion<string>().HasMaxLength(50);
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

        // Configure Department entity (User Story 2)
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ParentDepartmentId);
            entity.HasIndex(e => e.DepartmentHeadId);

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CostCenter).HasMaxLength(50);

            // Configure self-referencing relationship for hierarchy
            entity.HasOne(d => d.ParentDepartment)
                .WithMany(d => d.SubDepartments)
                .HasForeignKey(d => d.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship to Department Head (Employee)
            entity.HasOne(d => d.DepartmentHead)
                .WithMany()
                .HasForeignKey(d => d.DepartmentHeadId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure BulkJob entity (User Story 12)
        modelBuilder.Entity<BulkJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.JobType);
            entity.HasIndex(e => e.InitiatedByPrincipalId);
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

        // Configure SagaState entity
        modelBuilder.Entity<SagaState>(entity =>
        {
            entity.HasKey(e => e.CorrelationId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SagaType);
        });

        // Configure SagaStepHistory entity
        modelBuilder.Entity<SagaStepHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.ExecutedAt);
        });

        // Configure EmployeeTerminationSagaState entity
        modelBuilder.Entity<EmployeeTerminationSagaState>(entity =>
        {
            entity.HasKey(e => e.CorrelationId);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.CurrentState);
        });

        // Configure UserPreference entity
        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => new { e.PrincipalId, e.Scope });
            entity.Property(e => e.Scope).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PreferenceData).HasColumnType("jsonb").IsRequired();
        });

        // Configure naming convention for PostgreSQL (snake_case)
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Convert table names to snake_case
            if (!entity.IsOwned())
            {
                entity.SetTableName(ToSnakeCase(entity.GetTableName() ?? entity.DisplayName()));
            }

            // Convert column names to snake_case (including owned properties)
            foreach (var property in entity.GetProperties())
            {
                if (entity.IsOwned() && property.IsPrimaryKey())
                {
                    // For owned entities, the PK column should usually match the owner's PK column
                    var ownership = entity.GetForeignKeys().FirstOrDefault(fk => fk.IsOwnership);
                    if (ownership != null)
                    {
                        var ownerPk = ownership.PrincipalEntityType.FindPrimaryKey();
                        if (ownerPk != null && ownerPk.Properties.Count == 1)
                        {
                            property.SetColumnName(ToSnakeCase(ownerPk.Properties[0].Name));
                            continue;
                        }
                    }
                }
                property.SetColumnName(ToSnakeCase(property.Name));
            }
            if (!entity.IsOwned())
            {
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
