using Maliev.EmployeeService.Domain.Authorization;
using Maliev.EmployeeService.Api.HealthChecks;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Infrastructure.Authentication;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Data.Interceptors;
using Maliev.EmployeeService.Infrastructure.ExternalServices;
using Maliev.EmployeeService.Infrastructure.Messaging;
using Maliev.EmployeeService.Infrastructure.Security;
using Maliev.EmployeeService.Infrastructure.IAM;
using Maliev.EmployeeService.Infrastructure.Scripts;
using Microsoft.EntityFrameworkCore;
using Maliev.Aspire.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// --- Secrets & Configuration ---
builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

// --- Infrastructure & Observability ---
builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
builder.AddStandardMiddleware(options =>
{
    options.EnableRequestLogging = true;
});
builder.AddServiceMeters("employees-meter"); // Register service meters for OpenTelemetry business metrics
builder.AddIAMServiceClient(); // IAM service client for permission resolution and resource-scoped authorization

builder.AddRedisDistributedCache(instanceName: "employee:"); // Redis with in-memory fallback
builder.AddMassTransitWithRabbitMq(); // RabbitMQ message bus (non-blocking startup)

builder.AddPostgresDbContext<EmployeeDbContext>(
    connectionName: "EmployeeDbContext",
    configureOptions: options =>
    {
        options.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandError));
    });

// --- API Configuration ---
builder.AddDefaultCors(); // CORS from CORS:AllowedOrigins config
builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

// JWT Authentication (always enabled - test infrastructure configures appropriate keys)
builder.AddJwtAuthentication(); // JWT Bearer authentication with RSA public key

// Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
if (!builder.Environment.IsProduction())
{
    builder.AddStandardOpenApi(
        title: "MALIEV Employee Service API",
        description: "Comprehensive employee management service. Supports self-service profile updates, emergency contacts, leave management with approval workflows, compensation tracking, performance reviews, training records, department and team organization, work authorization, and onboarding/offboarding processes.");
}

// Core Services
builder.Services.AddControllers(options =>
{
    options.Filters.Add<Maliev.EmployeeService.Api.Filters.InputSanitizationFilter>();
});
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// HSTS Configuration (production only)
if (builder.Environment.IsProduction())
{
    builder.Services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
    });
}

// Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// Core application services
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<AuditLogInterceptor>();
builder.Services.AddScoped<DatabaseMetricsInterceptor>();
builder.Services.AddScoped<ICacheService, Maliev.EmployeeService.Infrastructure.Services.RedisCacheService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Repository and Unit of Work
builder.Services.AddScoped(typeof(IRepository<>), typeof(Maliev.EmployeeService.Infrastructure.Repositories.Repository<>));
builder.Services.AddScoped<IUnitOfWork, Maliev.EmployeeService.Infrastructure.Data.UnitOfWork>();

// Specialized Repositories
builder.Services.AddScoped<IEmployeeRepository, Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository>();
builder.Services.AddScoped<IEmergencyContactRepository, Maliev.EmployeeService.Infrastructure.Repositories.EmergencyContactRepository>();
builder.Services.AddScoped<IDepartmentRepository, Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository>();
builder.Services.AddScoped<ILeaveRequestRepository, Maliev.EmployeeService.Infrastructure.Repositories.LeaveRequestRepository>();
builder.Services.AddScoped<ILeaveBalanceRepository, Maliev.EmployeeService.Infrastructure.Repositories.LeaveBalanceRepository>();
builder.Services.AddScoped<ILeaveApprovalRepository, Maliev.EmployeeService.Infrastructure.Repositories.LeaveApprovalRepository>();
builder.Services.AddScoped<ILeavePolicyRepository, Maliev.EmployeeService.Infrastructure.Repositories.LeavePolicyRepository>();
builder.Services.AddScoped<ITeamRepository, Maliev.EmployeeService.Infrastructure.Repositories.TeamRepository>();
builder.Services.AddScoped<ICompensationRepository, Maliev.EmployeeService.Infrastructure.Repositories.CompensationRepository>();
builder.Services.AddScoped<IBenefitsRepository, Maliev.EmployeeService.Infrastructure.Repositories.BenefitsRepository>();
builder.Services.AddScoped<IPerformanceReviewRepository, Maliev.EmployeeService.Infrastructure.Repositories.PerformanceReviewRepository>();
builder.Services.AddScoped<IGoalRepository, Maliev.EmployeeService.Infrastructure.Repositories.GoalRepository>();
builder.Services.AddScoped<ITrainingRepository, Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository>();
builder.Services.AddScoped<ISkillRepository, Maliev.EmployeeService.Infrastructure.Repositories.SkillRepository>();
builder.Services.AddScoped<IMandatoryTrainingRepository, Maliev.EmployeeService.Infrastructure.Repositories.MandatoryTrainingRepository>();
builder.Services.AddScoped<IOnboardingRepository, Maliev.EmployeeService.Infrastructure.Repositories.OnboardingRepository>();
builder.Services.AddScoped<IOffboardingRepository, Maliev.EmployeeService.Infrastructure.Repositories.OffboardingRepository>();
builder.Services.AddScoped<IDocumentRepository, Maliev.EmployeeService.Infrastructure.Repositories.DocumentRepository>();
builder.Services.AddScoped<IWorkAuthorizationRepository, Maliev.EmployeeService.Infrastructure.Repositories.WorkAuthorizationRepository>();
builder.Services.AddScoped<IBulkJobRepository, Maliev.EmployeeService.Infrastructure.Repositories.BulkJobRepository>();

// Query and Command Handlers
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetEmployeeProfileQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UpdateEmployeeProfileCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CreateEmergencyContactCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UpdateEmergencyContactCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.DeleteEmergencyContactCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CreateEmployeeCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.TransferDepartmentCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CreateDepartmentCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UpdateDepartmentCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetTeamQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetOrgChartQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetLeaveBalancesQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetLeaveRequestsQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetPendingApprovalsQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.SubmitLeaveRequestCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.ApproveRejectLeaveCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CancelLeaveRequestCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetTeamDetailsQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetEmployeeTeamsQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CreateTeamCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.AddTeamMemberCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetCompensationDetailsQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetCompensationHistoryQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.RecordCompensationChangeCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UpdateBenefitsEnrollmentCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetPerformanceReviewsQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CreatePerformanceReviewCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.AcknowledgePerformanceReviewCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CreateGoalCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UpdateGoalProgressCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetTrainingRecordsQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetTrainingComplianceReportQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.RecordTrainingCompletionCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.AssignMandatoryTrainingCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UpdateSkillCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Services.OnboardingTemplateService>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.StartOnboardingCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CompleteOnboardingItemCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.StartOffboardingCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CompleteOffboardingItemCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetOnboardingStatusQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetOffboardingStatusQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UploadDocumentCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UploadDocumentVersionCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetEmployeeDocumentsQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.DownloadDocumentQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.RecordWorkAuthorizationCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UpdateWorkAuthorizationCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetWorkAuthorizationQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetWorkAuthorizationComplianceReportQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetHeadcountReportQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetTurnoverAnalysisQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.SearchEmployeesQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetDiversityMetricsQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetCompensationAnalysisQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetSpanOfControlReportQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetLeaveUtilizationReportQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.ExportEmployeesQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetBulkJobStatusQueryHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.ImportEmployeesCommandHandler>();
builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.BulkSalaryIncreaseCommandHandler>();

// Business Metrics Service
builder.Services.AddScoped<Maliev.EmployeeService.Application.Services.BusinessMetricsService>();

// Migration Script (US1)
builder.Services.AddScoped<MigrateEmployeesToPrincipalsScript>();

// Integration Event Publisher
builder.Services.AddScoped<IntegrationEventPublisher>();
builder.Services.AddScoped<IEventPublisher, ResilientIntegrationEventPublisher>();

// Leave Accrual Service
builder.Services.AddScoped<ILeaveAccrualService, Maliev.EmployeeService.Infrastructure.Services.LeaveAccrualService>();

// Background Job Status Service
builder.Services.AddSingleton<IBackgroundJobStatusService, Maliev.EmployeeService.Infrastructure.Services.BackgroundJobStatusService>();

// Document Authorization Service
builder.Services.AddScoped<IDocumentAuthorizationService, Maliev.EmployeeService.Application.Services.DocumentAuthorizationService>();

// Background Services
builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.LeaveAccrualBackgroundService>();
builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.LeaveExpirationAlertBackgroundService>();
builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.PerformanceReviewReminderBackgroundService>();
builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.CertificationExpirationReminderBackgroundService>();
builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.OverdueTrainingEscalationBackgroundService>();
builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.DocumentExpirationReminderBackgroundService>();
builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.OnboardingReminderBackgroundService>();
builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.AccessRevocationBackgroundService>();
builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.WorkAuthorizationExpirationReminderService>();
builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.ExpiredWorkAuthorizationFlaggingService>();
builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.MetricsUpdateBackgroundService>();

// IAM Registration Service
builder.Services.AddIAMRegistration<Maliev.EmployeeService.Api.Services.EmployeeIAMRegistrationService>();

// HTTP Clients for External Services
builder.AddServiceClient<ICareerServiceClient, CareerServiceClient>("CareerService");
builder.AddServiceClient<IUploadServiceClient, UploadServiceClient>("UploadService");
builder.AddServiceClient<IIAMClient, IAMClient>("IAM");

// Authorization
builder.Services.AddPermissionAuthorization();

// Custom authorization handler for resource-based access
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Maliev.EmployeeService.Api.Security.ResourceAuthorizationHandler>();

var app = builder.Build();

// CLI Commands (US1)
if (args.Contains("--migrate-principals"))
{
    using var scope = app.Services.CreateScope();
    var script = scope.ServiceProvider.GetRequiredService<MigrateEmployeesToPrincipalsScript>();
    await script.ExecuteAsync();
    return;
}

var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Run database migrations on startup
try
{
    await app.MigrateDatabaseAsync<EmployeeDbContext>();
}
catch (Exception ex)
{
    Log.MigrationFailed(logger, ex);
    // Don't throw - allow app to start for debugging
}

// Force initialization of metrics
System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Maliev.EmployeeService.Application.Services.BusinessMetricsService).TypeHandle);
System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Maliev.EmployeeService.Infrastructure.BackgroundServices.BackgroundJobMetrics).TypeHandle);
System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Maliev.EmployeeService.Infrastructure.Data.Interceptors.DatabaseMetricsInterceptor).TypeHandle);

// Middleware Pipeline
app.UseStandardMiddleware();

if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseCors();
app.UseRouting();
app.UseRateLimiter();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints after middleware
app.MapControllers();

// Map Aspire default endpoints (/health, /alive, /metrics)
app.MapDefaultEndpoints(servicePrefix: "employee");

// Map OpenAPI and Scalar documentation (dev/staging only)
app.MapApiDocumentation(servicePrefix: "employee");

// Seed database ONLY for local development (not Kubernetes)
var enableSeeding = app.Configuration.GetValue<bool>("Database:EnableSeeding", false);

if (app.Environment.IsDevelopment() && enableSeeding)
{
    Log.SeedingStarted(logger);
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
            var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();
            var seeder = new DatabaseSeeder(context, seedLogger);

            await seeder.SeedAllAsync();
            Log.SeedingCompleted(logger);
        }
        catch (Exception ex)
        {
            Log.SeedingFailed(logger, ex);
        }
    }
}
else if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    Log.SeedingDisabled(logger);
}

Log.ServiceStarted(logger);
await app.RunAsync();

/// <summary>
/// Program class for Employee Service API
/// </summary>
public partial class Program
{
    internal static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "EmployeeService started successfully")]
        public static partial void ServiceStarted(ILogger logger);

        [LoggerMessage(Level = LogLevel.Error, Message = "Database migration failed - application may not function correctly")]
        public static partial void MigrationFailed(ILogger logger, Exception exception);

        [LoggerMessage(Level = LogLevel.Information, Message = "Starting database seeding (Development mode with EnableSeeding=true)...")]
        public static partial void SeedingStarted(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information, Message = "Database seeding completed")]
        public static partial void SeedingCompleted(ILogger logger);

        [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while seeding the database")]
        public static partial void SeedingFailed(ILogger logger, Exception exception);

        [LoggerMessage(Level = LogLevel.Information, Message = "Database seeding disabled (set Database:EnableSeeding=true to enable)")]
        public static partial void SeedingDisabled(ILogger logger);
    }
}

