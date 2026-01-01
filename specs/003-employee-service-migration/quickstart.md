# Quickstart: Employee Service Decomposition

**Feature**: 003-employee-service-migration
**Date**: 2025-12-28
**Audience**: Developers implementing the microservices decomposition

---

## Overview

This guide helps you quickly understand and implement the Employee Service decomposition from a monolithic service (~82K LOC) into six focused microservices.

**Important**: This is a **pre-deployment refactoring**. No live migration complexity - we're reorganizing code before initial deployment.

---

## What's Being Done?

### Before (Current State)
```
Maliev.EmployeeService (1 repository)
├── 459 files
├── ~82,000 lines of code
└── 17 controllers handling 12+ domains
```

### After (Target State)
```
7 independent repositories:
1. Maliev.EmployeeService (slimmed) - ~25K LOC
2. Maliev.LeaveService (new) - ~10K LOC
3. Maliev.CompensationService (new) - ~10K LOC
4. Maliev.PerformanceService (new) - ~8K LOC
5. Maliev.LifecycleService (new) - ~10K LOC
6. Maliev.ComplianceService (new) - ~5K LOC
7. Maliev.CareerService (extended) - +~7K LOC
```

---

## Key Decisions Made

| Decision Point | Choice | Rationale |
|----------------|--------|-----------|
| **Transaction Consistency** | Saga pattern with compensating transactions | Ensures cross-service operations can roll back |
| **Observability** | Structured logging + correlation IDs | Enables distributed tracing |
| **Message Broker** | RabbitMQ (via MassTransit) | Enterprise-grade with guaranteed delivery |
| **Saga State** | Database-persisted | Survives orchestrator crashes |
| **Encryption** | TLS in transit + cloud disk encryption at rest | Balances security with operational simplicity |
| **GDPR/Thai Law** | 7-year retention + soft-delete + anonymization | Meets legal requirements |

**Full details**: See [research.md](./research.md)

---

## Architecture Overview

### Service Boundaries

```
┌─────────────────────┐
│  Employee Service   │ ← Core: profiles, departments, teams
│   (PostgreSQL DB)   │
└──────────┬──────────┘
           │ Publishes: EmployeeCreated, EmployeeTerminated
           ▼
    ┌──────────────────────────────────────┐
    │         RabbitMQ (Event Bus)         │
    └──────────────────────────────────────┘
           │
           ├──────► Leave Service (leave requests, balances)
           ├──────► Compensation Service (salary, benefits)
           ├──────► Performance Service (reviews, goals)
           ├──────► Lifecycle Service (onboarding, offboarding)
           ├──────► Compliance Service (work authorization)
           └──────► Career Service (training, skills)

Each service has:
  ✓ Own PostgreSQL database
  ✓ Own Git repository
  ✓ Independent deployment
  ✓ Event-driven communication
```

### Data Flow Examples

**Example 1: Employee Termination (Saga Pattern)**
```
1. HR calls POST /employee/v1/employees/{id}/terminate
2. EmployeeService starts EmployeeTerminationSaga
3. Saga orchestrates:
   ├── Publish CloseLeaveBalanceCommand → LeaveService
   ├── Await LeaveBalanceClosedEvent
   ├── Publish ArchiveCompensationCommand → CompensationService
   ├── Await CompensationArchivedEvent
   ├── Publish RevokeAccessCommand → LifecycleService
   └── Await AccessRevokedEvent
4. Saga completes, publishes EmployeeTerminatedIntegrationEvent
5. All services update their local employee data (soft-delete)
```

**Example 2: Leave Request Submission**
```
1. Employee calls POST /leave/v1/requests
2. LeaveService:
   ├── Validates leave balance (local DB)
   ├── Creates leave request (local DB)
   └── Publishes LeaveRequestSubmittedEvent
3. EmployeeService:
   └── Consumes event, logs to audit trail
```

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- Docker Desktop
- Access to GitHub Packages (`GITOPS_PAT` with `read:packages` scope)
- PostgreSQL 18 (via Docker)
- RabbitMQ (via Docker)
- Redis (via Docker)

### Step 1: Set Up Development Environment

**1.1 Clone Existing Repository**
```bash
git clone https://github.com/MALIEV-Co-Ltd/Maliev.EmployeeService.git
cd Maliev.EmployeeService
git checkout 003-employee-service-migration
```

**1.2 Start Infrastructure (Testcontainers)**

No docker-compose needed! Tests use Testcontainers:
```csharp
// In integration tests
var postgresContainer = new PostgreSqlBuilder()
    .WithImage("postgres:18-alpine")
    .WithDatabase("employee_db")
    .Build();

var rabbitmqContainer = new RabbitMqBuilder()
    .WithImage("rabbitmq:4.2-alpine")
    .Build();

var redisContainer = new RedisBuilder()
    .WithImage("redis:8.4-alpine")
    .Build();

await Task.WhenAll(
    postgresContainer.StartAsync(),
    rabbitmqContainer.StartAsync(),
    redisContainer.StartAsync()
);
```

**1.3 Configure NuGet Authentication**

Create `nuget.config` (if not exists):
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/MALIEV-Co-Ltd/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="%NUGET_USERNAME%" />
      <add key="ClearTextPassword" value="%NUGET_PASSWORD%" />
    </github>
  </packageSourceCredentials>
</configuration>
```

Set environment variables:
```bash
# Windows
set NUGET_USERNAME=your-github-username
set NUGET_PASSWORD=your-gitops-pat

# Linux/Mac
export NUGET_USERNAME=your-github-username
export NUGET_PASSWORD=your-gitops-pat
```

---

### Step 2: Understand the Code Structure

#### Existing Employee Service (TO BE SLIMMED)
```
Maliev.EmployeeService/
├── Maliev.EmployeeService.Api/
│   ├── Controllers/
│   │   ├── ✅ EmployeesController.cs (KEEP)
│   │   ├── ✅ DepartmentsController.cs (KEEP)
│   │   ├── ❌ LeaveController.cs (REMOVE → LeaveService)
│   │   ├── ❌ CompensationController.cs (REMOVE → CompensationService)
│   │   └── ... (see data-model.md)
│   └── Program.cs (SLIM - remove registrations)
├── Maliev.EmployeeService.Domain/
│   ├── Entities/
│   │   ├── ✅ Employee.cs (KEEP)
│   │   ├── ✅ Department.cs (KEEP)
│   │   ├── ❌ LeaveRequest.cs (REMOVE → LeaveService)
│   │   └── ...
│   └── Authorization/
│       └── EmployeePermissions.cs (SLIM - remove migrated permissions)
└── Maliev.EmployeeService.Infrastructure/
    └── EmployeeDbContext.cs (SLIM - remove DbSets)
```

#### New Service Structure (TO BE CREATED)
```
Maliev.LeaveService/ (new repository)
├── Maliev.LeaveService.Api/
│   ├── Controllers/
│   │   └── LeaveController.cs (MOVED from EmployeeService)
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
├── Maliev.LeaveService.Application/
│   ├── Commands/
│   ├── Queries/
│   └── BackgroundServices/
├── Maliev.LeaveService.Domain/
│   ├── Entities/
│   │   ├── LeaveRequest.cs (MOVED)
│   │   └── LeaveBalance.cs (MOVED)
│   └── IntegrationEvents/
├── Maliev.LeaveService.Infrastructure/
│   ├── LeaveDbContext.cs
│   └── Consumers/ (RabbitMQ event consumers)
└── Maliev.LeaveService.Tests/
```

---

### Step 3: Implementation Workflow

#### Phase 1: Create New Service Repositories

For each new service:

**3.1 Create Repository**
```bash
# Example: Leave Service
cd ..
mkdir Maliev.LeaveService
cd Maliev.LeaveService
git init
git remote add origin https://github.com/MALIEV-Co-Ltd/Maliev.LeaveService.git
```

**3.2 Scaffold Project Structure**
```bash
dotnet new webapi -n Maliev.LeaveService.Api -f net10.0
dotnet new classlib -n Maliev.LeaveService.Application -f net10.0
dotnet new classlib -n Maliev.LeaveService.Domain -f net10.0
dotnet new classlib -n Maliev.LeaveService.Infrastructure -f net10.0
dotnet new xunit -n Maliev.LeaveService.Tests -f net10.0
dotnet new sln -n Maliev.LeaveService
dotnet sln add **/*.csproj
```

**3.3 Add ServiceDefaults Package**
```bash
dotnet add Maliev.LeaveService.Api package Maliev.Aspire.ServiceDefaults
```

**3.4 Configure Program.cs**
```csharp
using Maliev.Aspire.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Add ServiceDefaults (OpenTelemetry, health checks, JWT, etc.)
builder.AddServiceDefaults();

// Add database
builder.AddPostgresDbContext<LeaveDbContext>("LeaveDb");

// Add RabbitMQ + MassTransit
builder.AddMassTransitWithRabbitMq();

// Add Redis cache
builder.AddRedisDistributedCache("Redis");

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// Add standard middleware (correlation ID, exception handling)
app.AddStandardMiddleware();

// Map endpoints
app.MapControllers();
app.MapDefaultEndpoints(); // Health checks, metrics

app.Run();
```

**3.5 Move Code from EmployeeService**

Copy relevant files:
- Controllers → `Maliev.LeaveService.Api/Controllers/`
- Entities → `Maliev.LeaveService.Domain/Entities/`
- Handlers → `Maliev.LeaveService.Application/Commands|Queries/`
- DbContext configurations → `Maliev.LeaveService.Infrastructure/`

**Important**: Update namespaces!

---

#### Phase 2: Slim Down Employee Service

**3.6 Remove Controllers**
```bash
cd Maliev.EmployeeService
rm Maliev.EmployeeService.Api/Controllers/LeaveController.cs
rm Maliev.EmployeeService.Api/Controllers/CompensationController.cs
# ... etc.
```

**3.7 Remove Entities**
```csharp
// Maliev.EmployeeService.Domain/Entities/
// DELETE: LeaveRequest.cs, LeaveBalance.cs, etc.
```

**3.8 Update DbContext**
```csharp
public class EmployeeDbContext : DbContext
{
    // KEEP
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<Emergency Contact>();
    public DbSet<Department> Departments => Set<Department>();
    // ... (see data-model.md for full list)

    // REMOVE
    // public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    // public DbSet<CompensationRecord> CompensationRecords => Set<CompensationRecord>();
    // ... etc.
}
```

**3.9 Update Permissions**
```csharp
public static class EmployeePermissions
{
    // KEEP
    public const string ProfilesCreate = "employee.profiles.create";
    public const string ProfilesRead = "employee.profiles.read";
    // ...

    // REMOVE
    // public const string LeaveCreate = "employee.leave.create";
    // public const string CompensationRead = "employee.compensation.read";
    // ... (moved to LeavePermissions, CompensationPermissions)
}
```

---

#### Phase 3: Implement Event-Driven Communication

**3.10 Publish Integration Events**

In `EmployeeService`:
```csharp
public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Guid>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public async Task<Guid> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = new Employee { /* ... */ };
        await _context.Employees.AddAsync(employee);
        await _context.SaveChangesAsync();

        // Publish event
        await _publishEndpoint.Publish(new EmployeeCreatedIntegrationEvent
        {
            EmployeeId = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            FullName = employee.FullName,
            Email = employee.Email,
            StartDate = employee.HireDate,
            DepartmentId = employee.DepartmentId,
            ManagerId = employee.ManagerId,
            JobTitle = employee.Position.Title
        }, cancellationToken);

        return employee.Id;
    }
}
```

**3.11 Consume Integration Events**

In `LeaveService`:
```csharp
public class EmployeeCreatedEventConsumer : IConsumer<EmployeeCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<EmployeeCreatedIntegrationEvent> context)
    {
        var message = context.Message;

        // Create leave balances for new employee
        var leaveTypes = await _context.LeaveTypes.Where(lt => lt.IsActive).ToListAsync();

        foreach (var leaveType in leaveTypes)
        {
            var balance = new LeaveBalance
            {
                EmployeeId = message.EmployeeId,
                EmployeeNumber = message.EmployeeNumber,
                LeaveTypeId = leaveType.Id,
                Year = DateTime.UtcNow.Year,
                Entitled = leaveType.AnnualEntitlement,
                Used = 0,
                Remaining = leaveType.AnnualEntitlement
            };

            await _context.LeaveBalances.AddAsync(balance);
        }

        await _context.SaveChangesAsync();
    }
}

// Register in Program.cs
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EmployeeCreatedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});
```

---

#### Phase 4: Implement Saga Pattern

**3.12 Define Saga (in EmployeeService)**

See `research.md` section 5 for full saga implementation.

```csharp
public class EmployeeTerminationSaga : ISaga, ...
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = "Initiated";
    public Guid EmployeeId { get; set; }
    // ... (see research.md)
}

// Register in Program.cs
builder.Services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<EmployeeTerminationSaga, EmployeeTerminationSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<EmployeeDbContext>();
            r.UsePostgres();
        });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});
```

---

### Step 4: Testing

**4.1 Write Integration Tests**

Example for LeaveService:
```csharp
public class LeaveServiceIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer = null!;
    private RabbitMqContainer _rabbitmqContainer = null!;

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
            .WithDatabase("leave_db_test")
            .Build();

        _rabbitmqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:4.2-alpine")
            .Build();

        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _rabbitmqContainer.StartAsync()
        );
    }

    [Fact]
    public async Task SubmitLeaveRequest_Should_DeductBalance()
    {
        // Arrange
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["ConnectionStrings:LeaveDb"] = _postgresContainer.GetConnectionString(),
                        ["RabbitMQ:Host"] = _rabbitmqContainer.Hostname,
                        ["RabbitMQ:Port"] = _rabbitmqContainer.GetMappedPublicPort(5672).ToString()
                    });
                });
            });

        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/leave/v1/requests", new
        {
            LeaveTypeId = /* ... */,
            StartDate = "2025-01-10",
            EndDate = "2025-01-12",
            Reason = "Vacation"
        });

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Created);
        // ... verify balance deduction
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _rabbitmqContainer.DisposeAsync();
    }
}
```

---

## Common Patterns

### Pattern 1: Entity Mapping (No AutoMapper!)
```csharp
// Explicit mapping
public static EmployeeDto ToDto(this Employee employee)
{
    return new EmployeeDto
    {
        Id = employee.Id,
        EmployeeNumber = employee.EmployeeNumber,
        FullName = employee.FullName,
        Email = employee.Email,
        DepartmentName = employee.Department.Name,
        ManagerName = employee.Manager?.FullName
    };
}
```

### Pattern 2: Correlation ID Propagation
```csharp
// ServiceDefaults adds middleware automatically
// Access in controllers:
public class LeaveController : ControllerBase
{
    [HttpPost("requests")]
    public async Task<IActionResult> SubmitLeaveRequest(...)
    {
        var correlationId = HttpContext.TraceIdentifier; // Auto-populated
        _logger.LogInformation("Processing leave request. CorrelationId: {CorrelationId}", correlationId);
        // ...
    }
}
```

### Pattern 3: Structured Logging
```csharp
_logger.LogInformation(
    "Employee {EmployeeId} submitted leave request {LeaveRequestId} for {Days} days",
    employeeId,
    leaveRequestId,
    daysRequested
);
```

---

## Deployment

### Docker Build

Each service has a Dockerfile in the Api project folder:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /
COPY nuget.config ./
COPY ["Maliev.LeaveService.Api/Maliev.LeaveService.Api.csproj", "Maliev.LeaveService.Api/"]
RUN --mount=type=secret,id=nuget_username \
    --mount=type=secret,id=nuget_password \
    NUGET_USERNAME=$(cat /run/secrets/nuget_username) \
    NUGET_PASSWORD=$(cat /run/secrets/nuget_password) \
    dotnet restore "Maliev.LeaveService.Api/Maliev.LeaveService.Api.csproj"
COPY . .
WORKDIR "/Maliev.LeaveService.Api"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN chown -R app:app /app
USER app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
HEALTHCHECK CMD curl -f http://localhost:8080/leave/liveness || exit 1
ENTRYPOINT ["dotnet", "Maliev.LeaveService.Api.dll"]
```

Build with BuildKit secrets:
```bash
docker build \
  --secret id=nuget_username,env=NUGET_USERNAME \
  --secret id=nuget_password,env=NUGET_PASSWORD \
  -t maliev/leave-service:latest \
  -f Maliev.LeaveService.Api/Dockerfile .
```

---

## Troubleshooting

### Issue: RabbitMQ Connection Refused
**Solution**: Ensure RabbitMQ container is running and accessible:
```bash
docker ps | grep rabbitmq
# Check connection string in appsettings.json
```

### Issue: Saga Not Persisting State
**Solution**: Verify saga state tables exist in database:
```sql
SELECT * FROM information_schema.tables
WHERE table_name LIKE 'saga%';
```

Run migrations:
```bash
dotnet ef migrations add AddSagaState --project Maliev.EmployeeService.Infrastructure
dotnet ef database update --project Maliev.EmployeeService.Infrastructure
```

### Issue: NuGet Package Not Found
**Solution**: Verify authentication:
```bash
dotnet nuget list source
# Should show GitHub Packages source

# Test authentication
dotnet restore
```

---

## Next Steps

1. **Read Full Specifications**:
   - [spec.md](./spec.md) - Feature requirements
   - [research.md](./research.md) - Technical decisions
   - [data-model.md](./data-model.md) - Entity schemas
   - [contracts/api-contracts-summary.md](./contracts/api-contracts-summary.md) - API endpoints

2. **Review Constitution**: [.specify/memory/constitution.md](../../.specify/memory/constitution.md)

3. **Generate Tasks**: Run `/speckit.tasks` to break down implementation into actionable tasks

4. **Start Implementation**: Begin with highest priority service (Leave Service recommended)

---

## Questions?

Refer to:
- **Feature Spec**: [spec.md](./spec.md)
- **Technical Research**: [research.md](./research.md)
- **Data Models**: [data-model.md](./data-model.md)
- **API Contracts**: [contracts/api-contracts-summary.md](./contracts/api-contracts-summary.md)
