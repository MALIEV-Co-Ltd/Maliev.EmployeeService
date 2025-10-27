using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using HealthChecks.UI.Client;
using Scalar.AspNetCore;
using Maliev.EmployeeService.Api.Authorization;
using Maliev.EmployeeService.Api.HealthChecks;
using Maliev.EmployeeService.Api.Middleware;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Infrastructure.Authentication;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Data.Interceptors;
using Maliev.EmployeeService.Infrastructure.ExternalServices;
using Maliev.EmployeeService.Infrastructure.Messaging;
using Maliev.EmployeeService.Infrastructure.Security;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration (Console only) - Phase 16 - T395: Enhanced structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext() // Enable LogContext for correlation IDs
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "EmployeeService")
    .Enrich.WithProperty("Version", typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0")
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting Employee Service");

    // Secrets from Google Secret Manager
    var secretsPath = "/mnt/secrets";
    if (Directory.Exists(secretsPath))
    {
        Log.Information("Loading secrets from Google Secret Manager at {Path}", secretsPath);
        builder.Configuration.AddKeyPerFile(directoryPath: secretsPath, optional: true);
    }
    else
    {
        Log.Warning("Secrets path {Path} not found. Running without Secret Manager (Development mode)", secretsPath);
    }

    // Core Services
    builder.Services.AddControllers(options =>
    {
        // Add input sanitization filter globally (Phase 16 - T381)
        options.Filters.Add<Maliev.EmployeeService.Api.Filters.InputSanitizationFilter>();
    });
    builder.Services.AddMemoryCache(); // Simple configuration without SizeLimit
    builder.Services.AddHttpContextAccessor();

    // FluentValidation (scan Application assembly where validators are located)
    builder.Services.AddValidatorsFromAssemblyContaining<Maliev.EmployeeService.Application.DTOs.CreateDepartmentDto>();

    // API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // CORS Configuration
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var allowedOrigins = builder.Configuration["CORS_ALLOWED_ORIGINS"]?.Split(',')
                ?? new[] { "http://localhost:3000", "https://maliev.co.th" };

            policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    // Rate Limiting (Phase 16 - T380)
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

    // HSTS Configuration (Phase 16 - T382)
    if (!builder.Environment.IsDevelopment())
    {
        builder.Services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
        });
    }

    // Response Compression (Phase 16 - T389)
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

    // Redis Distributed Cache (Phase 16 - T385)
    var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
    var redisEnabled = bool.TryParse(builder.Configuration["Redis:Enabled"], out var isRedisEnabled) ? isRedisEnabled : true;

    if (redisEnabled && !builder.Environment.IsEnvironment("Testing"))
    {
        if (string.IsNullOrEmpty(redisConnectionString))
        {
            if (builder.Environment.IsDevelopment())
            {
                redisConnectionString = "localhost:6379";
                Log.Warning("Using development Redis connection string: {RedisConnectionString}", redisConnectionString);
            }
            else
            {
                Log.Warning("REDIS_CONNECTION_STRING not found. Falling back to in-memory cache.");
                redisEnabled = false;
            }
        }

        if (redisEnabled)
        {
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "EmployeeService:";
            });

            builder.Services.AddScoped<ICacheService, Maliev.EmployeeService.Infrastructure.Services.RedisCacheService>();
            Log.Information("Redis distributed cache configured: {RedisConnectionString}", redisConnectionString);
        }
    }
    else
    {
        Log.Information("Redis disabled - using in-memory cache only");
    }

    // Encryption Service (used by value converters in DbContext)
    builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

    // Audit Logging Interceptor
    builder.Services.AddScoped<AuditLogInterceptor>();

    // Database Metrics Interceptor for Phase 15 - T426 Technical Health Metrics
    builder.Services.AddSingleton<DatabaseMetricsInterceptor>();

    // Current User Service
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // Repository and Unit of Work
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Maliev.EmployeeService.Infrastructure.Repositories.Repository<>));
    builder.Services.AddScoped<IUnitOfWork, Maliev.EmployeeService.Infrastructure.Data.UnitOfWork>();

    // Specialized Repositories for User Story 1 - Employee Self-Service
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.IEmployeeRepository, Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.IEmergencyContactRepository, Maliev.EmployeeService.Infrastructure.Repositories.EmergencyContactRepository>();

    // Specialized Repositories for User Story 2 - HR Employee Lifecycle
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.IDepartmentRepository, Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository>();

    // Specialized Repositories for User Story 4 - Leave Management
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.ILeaveRequestRepository, Maliev.EmployeeService.Infrastructure.Repositories.LeaveRequestRepository>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.ILeaveBalanceRepository, Maliev.EmployeeService.Infrastructure.Repositories.LeaveBalanceRepository>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.ILeaveApprovalRepository, Maliev.EmployeeService.Infrastructure.Repositories.LeaveApprovalRepository>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.ILeavePolicyRepository, Maliev.EmployeeService.Infrastructure.Repositories.LeavePolicyRepository>();

    // Specialized Repositories for User Story 5 - Organizational Structure
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.ITeamRepository, Maliev.EmployeeService.Infrastructure.Repositories.TeamRepository>();

    // Specialized Repositories for User Story 6 - Compensation & Benefits
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.ICompensationRepository, Maliev.EmployeeService.Infrastructure.Repositories.CompensationRepository>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.IBenefitsRepository, Maliev.EmployeeService.Infrastructure.Repositories.BenefitsRepository>();

    // Specialized Repositories for User Story 7 - Performance Management
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.IPerformanceReviewRepository, Maliev.EmployeeService.Infrastructure.Repositories.PerformanceReviewRepository>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.IGoalRepository, Maliev.EmployeeService.Infrastructure.Repositories.GoalRepository>();

    // Specialized Repositories for User Story 8 - Training & Certification Management
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.ITrainingRepository, Maliev.EmployeeService.Infrastructure.Repositories.TrainingRepository>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.ISkillRepository, Maliev.EmployeeService.Infrastructure.Repositories.SkillRepository>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.IMandatoryTrainingRepository, Maliev.EmployeeService.Infrastructure.Repositories.MandatoryTrainingRepository>();

    // Specialized Repositories for User Story 10 - Onboarding/Offboarding
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.IOnboardingRepository, Maliev.EmployeeService.Infrastructure.Repositories.OnboardingRepository>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.IOffboardingRepository, Maliev.EmployeeService.Infrastructure.Repositories.OffboardingRepository>();

    // Specialized Repositories for User Story 9 - Document Management
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.IDocumentRepository, Maliev.EmployeeService.Infrastructure.Repositories.DocumentRepository>();

    // Specialized Repositories for User Story 11 - Work Authorization & Visa Tracking
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.IWorkAuthorizationRepository, Maliev.EmployeeService.Infrastructure.Repositories.WorkAuthorizationRepository>();

    // Specialized Repositories for User Story 12 - Bulk Operations
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Interfaces.IBulkJobRepository, Maliev.EmployeeService.Infrastructure.Repositories.BulkJobRepository>();

    // Query and Command Handlers for User Story 1 - Employee Self-Service
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetEmployeeProfileQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UpdateEmployeeProfileCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CreateEmergencyContactCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UpdateEmergencyContactCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.DeleteEmergencyContactCommandHandler>();

    // Command Handlers for User Story 2 - HR Employee Lifecycle
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CreateEmployeeCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.TransferDepartmentCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CreateDepartmentCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UpdateDepartmentCommandHandler>();

    // Query Handlers for User Story 3 - Manager Team Management
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetTeamQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetOrgChartQueryHandler>();

    // Query and Command Handlers for User Story 4 - Leave Management
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetLeaveBalancesQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetLeaveRequestsQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetPendingApprovalsQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.SubmitLeaveRequestCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.ApproveRejectLeaveCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CancelLeaveRequestCommandHandler>();

    // Query and Command Handlers for User Story 5 - Organizational Structure
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetTeamDetailsQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetEmployeeTeamsQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CreateTeamCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.AddTeamMemberCommandHandler>();

    // Query and Command Handlers for User Story 6 - Compensation & Benefits
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetCompensationDetailsQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetCompensationHistoryQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.RecordCompensationChangeCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UpdateBenefitsEnrollmentCommandHandler>();

    // Query and Command Handlers for User Story 7 - Performance Management
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetPerformanceReviewsQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CreatePerformanceReviewCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.AcknowledgePerformanceReviewCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CreateGoalCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UpdateGoalProgressCommandHandler>();

    // Query and Command Handlers for User Story 8 - Training & Certification Management
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetTrainingRecordsQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetTrainingComplianceReportQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.RecordTrainingCompletionCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.AssignMandatoryTrainingCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UpdateSkillCommandHandler>();

    // Services and Handlers for User Story 10 - Onboarding/Offboarding
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Services.OnboardingTemplateService>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.StartOnboardingCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CompleteOnboardingItemCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.StartOffboardingCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.CompleteOffboardingItemCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetOnboardingStatusQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetOffboardingStatusQueryHandler>();

    // Query and Command Handlers for User Story 9 - Document Management
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UploadDocumentCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UploadDocumentVersionCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetEmployeeDocumentsQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.DownloadDocumentQueryHandler>();

    // Query and Command Handlers for User Story 11 - Work Authorization & Visa Tracking
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.RecordWorkAuthorizationCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.UpdateWorkAuthorizationCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetWorkAuthorizationQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetWorkAuthorizationComplianceReportQueryHandler>();

    // Query Handlers for User Story 12 - Reporting & Analytics
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetHeadcountReportQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetTurnoverAnalysisQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.SearchEmployeesQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetDiversityMetricsQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetCompensationAnalysisQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetSpanOfControlReportQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetLeaveUtilizationReportQueryHandler>();

    // Query Handlers for User Story 12 - Bulk Operations
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.ExportEmployeesQueryHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Queries.GetBulkJobStatusQueryHandler>();

    // Command Handlers for User Story 12 - Bulk Operations
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.ImportEmployeesCommandHandler>();
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Commands.BulkSalaryIncreaseCommandHandler>();

    // Business Metrics Service for Phase 15 - Constitution Principle X
    builder.Services.AddScoped<Maliev.EmployeeService.Application.Services.BusinessMetricsService>();

    // MassTransit with RabbitMQ (User Story 10 - Onboarding/Offboarding Integration Events)
    var rabbitmqHost = builder.Configuration["RabbitMq:Host"] ?? "localhost";
    var rabbitmqPort = int.TryParse(builder.Configuration["RabbitMq:Port"], out var port) ? port : 5672;
    var rabbitmqUser = builder.Configuration["RabbitMq:Username"] ?? "guest";
    var rabbitmqPassword = builder.Configuration["RabbitMq:Password"] ?? "guest";
    var rabbitmqVhost = builder.Configuration["RabbitMq:VirtualHost"] ?? "/";
    var rabbitmqEnabled = bool.TryParse(builder.Configuration["RabbitMq:Enabled"], out var enabled) ? enabled : true;

    if (rabbitmqEnabled)
    {
        builder.Services.AddMassTransit(x =>
        {
            // Configure RabbitMQ transport
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitmqHost, (ushort)rabbitmqPort, rabbitmqVhost, h =>
                {
                    h.Username(rabbitmqUser);
                    h.Password(rabbitmqPassword);
                });

                // Configure endpoints
                cfg.ConfigureEndpoints(context);
            });
        });

        Log.Information("MassTransit configured with RabbitMQ: {Host}:{Port}", rabbitmqHost, rabbitmqPort);
    }
    else
    {
        Log.Warning("RabbitMQ/MassTransit disabled by configuration");
    }

    // Integration Event Publisher (Phase 16 - T396, T397: Polly circuit breaker and retry)
    builder.Services.AddScoped<IntegrationEventPublisher>(); // Inner publisher
    builder.Services.AddScoped<IEventPublisher, ResilientIntegrationEventPublisher>(); // Resilient wrapper

    // Leave Accrual Service for User Story 4
    builder.Services.AddScoped<ILeaveAccrualService, Maliev.EmployeeService.Infrastructure.Services.LeaveAccrualService>();

    // Background Job Status Service (Singleton for cross-service status tracking)
    builder.Services.AddSingleton<IBackgroundJobStatusService, Maliev.EmployeeService.Infrastructure.Services.BackgroundJobStatusService>();

    // Document Authorization Service (User Story 9 - Document Management)
    builder.Services.AddScoped<IDocumentAuthorizationService, Maliev.EmployeeService.Application.Services.DocumentAuthorizationService>();

    // Background Services for User Story 4
    builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.LeaveAccrualBackgroundService>();
    builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.LeaveExpirationAlertBackgroundService>();

    // Background Services for User Story 7 - Performance Management
    builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.PerformanceReviewReminderBackgroundService>();

    // Background Services for User Story 8 - Training & Certification Management
    builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.CertificationExpirationReminderBackgroundService>();
    builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.OverdueTrainingEscalationBackgroundService>();

    // Background Services for User Story 9 - Document Management
    builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.DocumentExpirationReminderBackgroundService>();

    // Background Services for User Story 10 - Onboarding/Offboarding
    builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.OnboardingReminderBackgroundService>();
    builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.AccessRevocationBackgroundService>();

    // Background Services for User Story 11 - Work Authorization & Visa Tracking
    builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.WorkAuthorizationExpirationReminderService>();
    builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.ExpiredWorkAuthorizationFlaggingService>();

    // Background Service for Phase 15 - Business Metrics (Constitution Principle X)
    builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.BackgroundServices.MetricsUpdateBackgroundService>();

    // Career Service Integration - Event Consumer (Deferred to US10 - requires RabbitMQ 7.0 API updates)
    // TODO: Complete CandidateAcceptedEventConsumer for onboarding workflow integration
    // builder.Services.AddHostedService<Maliev.EmployeeService.Infrastructure.Messaging.CandidateAcceptedEventConsumer>();

    // HTTP Clients for External Services
    // Polly v8 resilience policies (T026h): retry with exponential backoff, circuit breaker
    builder.Services.AddHttpClient<ICareerServiceClient, CareerServiceClient>(client =>
    {
        var careerServiceUrl = builder.Configuration["ExternalServices:CareerService:BaseUrl"];
        var timeoutSeconds = int.TryParse(builder.Configuration["ExternalServices:CareerService:TimeoutSeconds"], out var timeout) ? timeout : 30;

        if (string.IsNullOrEmpty(careerServiceUrl))
        {
            if (builder.Environment.IsDevelopment())
            {
                careerServiceUrl = "http://localhost:8081";
                Log.Warning("Using development Career Service URL: {Url}", careerServiceUrl);
            }
            else
            {
                throw new InvalidOperationException(
                    "ExternalServices:CareerService:BaseUrl not found in configuration. " +
                    "Ensure Google Secret Manager is properly configured.");
            }
        }

        client.BaseAddress = new Uri(careerServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    })
    .AddStandardResilienceHandler(options =>
    {
        // Retry policy: 3 retries with exponential backoff
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;
        options.Retry.OnRetry = args =>
        {
            Log.Warning(
                "Career Service retry {RetryCount} after {Delay}s due to: {Exception}",
                args.AttemptNumber,
                args.RetryDelay.TotalSeconds,
                args.Outcome.Exception?.Message ?? "HTTP error");
            return ValueTask.CompletedTask;
        };

        // Circuit breaker: opens after 5 consecutive failures, breaks for 30 seconds
        options.CircuitBreaker.FailureRatio = 1.0;
        options.CircuitBreaker.MinimumThroughput = 5;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
    });

    // Upload Service Configuration (User Story 9 - Document Management)
    builder.Services.AddHttpClient<IUploadServiceClient, UploadServiceClient>(client =>
    {
        var uploadServiceUrl = builder.Configuration["ExternalServices:UploadService:BaseUrl"];
        var timeoutSeconds = int.TryParse(builder.Configuration["ExternalServices:UploadService:TimeoutSeconds"], out var uploadTimeout) ? uploadTimeout : 300;

        if (string.IsNullOrEmpty(uploadServiceUrl))
        {
            if (builder.Environment.IsDevelopment())
            {
                uploadServiceUrl = "http://localhost:8082";
                Log.Warning("Using development Upload Service URL: {Url}", uploadServiceUrl);
            }
            else
            {
                throw new InvalidOperationException(
                    "ExternalServices:UploadService:BaseUrl not found in configuration. " +
                    "Ensure Google Secret Manager is properly configured.");
            }
        }

        client.BaseAddress = new Uri(uploadServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds); // Configured timeout for file uploads
    });

    Log.Information("Upload Service configured");

    // Database Configuration
    // Skip DbContext registration in Testing environment - WebApplicationFactory will provide it
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddDbContext<EmployeeServiceDbContext>((serviceProvider, options) =>
        {
            var connectionString = builder.Configuration.GetConnectionString("EmployeeServiceDb");

            // Development fallback
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = builder.Configuration["ConnectionStrings:EmployeeServiceDb"] ??
                    "Host=localhost;Port=5432;Database=employee_app_db;Username=postgres;Password=postgres";
                Log.Warning("Using fallback connection string for development");
            }

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });

            // Add interceptors (encryption now handled by value converters in entity configuration)
            var auditLogInterceptor = serviceProvider.GetRequiredService<AuditLogInterceptor>();
            var databaseMetricsInterceptor = serviceProvider.GetRequiredService<DatabaseMetricsInterceptor>();
            options.AddInterceptors(auditLogInterceptor, databaseMetricsInterceptor);

        // Enable sensitive data logging in development
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
        });
    }

    // JWT Authentication (RSA Asymmetric)
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "https://dev.api.maliev.com";
        var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "https://dev.api.maliev.com";
        var jwtPublicKey = builder.Configuration["Jwt:PublicKey"];

        SecurityKey? signingKey = null;

        if (string.IsNullOrEmpty(jwtPublicKey))
        {
            if (builder.Environment.IsDevelopment())
            {
                // Development-only fallback: Use symmetric key for local testing
                var devKey = Encoding.UTF8.GetBytes("DevelopmentSecretKey32CharactersLong!!");
                signingKey = null; // Will use symmetric key as fallback
                Log.Warning("Jwt:PublicKey not found. Using development symmetric key. DO NOT use in production!");
            }
            else
            {
                throw new InvalidOperationException(
                    "Jwt:PublicKey not found in configuration. " +
                    "Ensure Google Secret Manager is properly configured with maliev-shared-config.");
            }
        }
        else
        {
            // Parse RSA public key
            try
            {
                var rsa = RSA.Create();

                // The public key from Google Secret Manager is double base64-encoded PEM format
                // First decode to get the PEM string
                var pemString = Encoding.UTF8.GetString(Convert.FromBase64String(jwtPublicKey));

                // Extract base64 content from PEM format (remove headers)
                var base64Key = pemString
                    .Replace("-----BEGIN PUBLIC KEY-----", "")
                    .Replace("-----END PUBLIC KEY-----", "")
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Trim();

                // Decode the base64 content to get raw DER bytes
                var keyBytes = Convert.FromBase64String(base64Key);

                // Import the DER-formatted public key
                rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
                signingKey = new RsaSecurityKey(rsa);
                Log.Information("JWT RSA public key loaded successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to parse JWT public key");
                throw new InvalidOperationException("Invalid JWT public key format", ex);
            }
        }

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = signingKey ?? new SymmetricSecurityKey(Encoding.UTF8.GetBytes("DevelopmentSecretKey32CharactersLong!!")),
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Warning("JWT authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Log.Debug("JWT token validated for user: {User}",
                        context.Principal?.Identity?.Name ?? "Unknown");
                    return Task.CompletedTask;
                }
            };
        });

        Log.Information("JWT authentication configured with issuer: {Issuer}", jwtIssuer);
    }
    else
    {
        // Testing environment: configure fake authentication that accepts all requests
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "TestScheme";
            options.DefaultChallengeScheme = "TestScheme";
        })
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthenticationHandler>("TestScheme", options => { });

        Log.Information("Test authentication scheme configured for Testing environment");
    }

    // Authorization Policies (always registered, even in Testing environment)
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(Policies.RequireAdminRole, policy =>
            policy.RequireRole(Roles.Admin));

        options.AddPolicy(Policies.RequireHRRole, policy =>
            policy.RequireRole(Roles.HR));

        options.AddPolicy(Policies.RequireManagerRole, policy =>
            policy.RequireRole(Roles.Manager));

        options.AddPolicy(Policies.RequireEmployeeRole, policy =>
            policy.RequireRole(Roles.Employee));

        options.AddPolicy(Policies.RequireHROrManager, policy =>
            policy.RequireRole(Roles.HR, Roles.Manager));

        options.AddPolicy(Policies.RequireHROrAdmin, policy =>
            policy.RequireRole(Roles.HR, Roles.Admin));
    });

    // Register custom authorization handler (must be Scoped due to ICurrentUserService dependency)
    builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Maliev.EmployeeService.Api.Security.ResourceAuthorizationHandler>();

    // Health Checks (Phase 16 - T398: Integration health checks)
    var healthChecksBuilder = builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "readiness" });

    // Add RabbitMQ health check if enabled
    if (rabbitmqEnabled)
    {
        healthChecksBuilder.AddCheck<RabbitMQHealthCheck>("rabbitmq", tags: new[] { "readiness", "integration" });
    }

    // Add Redis health check if enabled
    if (redisEnabled)
    {
        healthChecksBuilder.AddCheck<RedisHealthCheck>("redis", tags: new[] { "readiness", "integration" });
    }

    // Add Google Cloud Storage (Upload Service) health check
    healthChecksBuilder.AddCheck<GoogleCloudStorageHealthCheck>("gcs", tags: new[] { "readiness", "integration" });

    // OpenAPI with Scalar (Development only)
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddOpenApi();
    }

    var app = builder.Build();

    // Force initialization of metrics (triggers static constructors)
    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Maliev.EmployeeService.Application.Services.BusinessMetricsService).TypeHandle);
    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Maliev.EmployeeService.Infrastructure.BackgroundServices.BackgroundJobMetrics).TypeHandle);
    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Maliev.EmployeeService.Infrastructure.Data.Interceptors.DatabaseMetricsInterceptor).TypeHandle);

    // Set base path for all routes
    app.UsePathBase("/employees");

    // Middleware Pipeline (EXACT ORDER per CLAUDE.md)
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // Correlation ID (Phase 16 - T395) - Must be early in pipeline for request tracking
    app.UseCorrelationId();

    // Security Headers (Phase 16 - T384)
    app.UseSecurityHeaders();

    // HSTS (Phase 16 - T382) - Only in production
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    // Response Compression (Phase 16 - T389)
    app.UseResponseCompression();

    // OpenAPI with Scalar UI (Development only)
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseHttpsRedirection();
    app.UseCors();

    // Prometheus HTTP Metrics (Constitution Principle X)
    app.UseHttpMetrics();

    app.UseRateLimiter();

    // Authentication & Authorization (always enabled, even in Testing with fake auth)
    app.UseAuthentication();
    app.UseAuthorization();

    // Health check endpoints (excluded from OpenAPI)
    app.MapGet("/liveness", () => Results.Ok(new { status = "Healthy", service = "Employee Service" }))
        .WithName("Liveness")
        .WithTags("Health")
        .AllowAnonymous()
        .ExcludeFromDescription();

    app.MapHealthChecks("/readiness", new HealthCheckOptions
    {
        Predicate = healthCheck => healthCheck.Tags.Contains("readiness"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    })
    .WithName("Readiness")
    .WithTags("Health")
    .AllowAnonymous()
    .ExcludeFromDescription();

    // Prometheus Metrics Endpoint (Constitution Principle X) - excluded from OpenAPI
    app.MapMetrics("/metrics")
        .WithName("Metrics")
        .WithTags("Monitoring")
        .AllowAnonymous()
        .ExcludeFromDescription();

    app.MapControllers();

    // Seed database in development environment
    if (app.Environment.IsDevelopment())
    {
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var context = scope.ServiceProvider.GetRequiredService<EmployeeServiceDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();
                var seeder = new DatabaseSeeder(context, logger);

                await seeder.SeedAllAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while seeding the database");
            }
        }
    }

    Log.Information("Employee Service started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Employee Service failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible to integration tests
public partial class Program { }

/// <summary>
/// Test authentication handler that accepts all requests without validation
/// Used only in Testing environment for integration tests
/// </summary>
public class TestAuthenticationHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        // Create a fake identity with all required roles for testing
        var claimsList = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "TestUser"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-user-id"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "SystemAdministrator"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "HRGeneralist"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Manager"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Employee")
        };

        // Add employee_id claim from X-Test-Employee-Id header if present
        // This allows tests to specify which employee they're acting as
        if (Context.Request.Headers.TryGetValue("X-Test-Employee-Id", out var employeeIdHeader))
        {
            claimsList.Add(new System.Security.Claims.Claim("employee_id", employeeIdHeader.ToString()));
        }

        var identity = new System.Security.Claims.ClaimsIdentity(claimsList, "TestScheme");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
    }
}
