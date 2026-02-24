# Employee Service Decomposition - Implementation Guide

**Feature**: 003-employee-service-migration
**Status**: Ready for Implementation
**Total Tasks**: 340

---

## Overview

This guide walks you through implementing the Employee Service decomposition from a monolithic service (~82K LOC) into 6 focused microservices.

**Important**: This is a **pre-deployment refactoring** - no live data migration complexity.

---

## Prerequisites

### Required Tools
- ✅ .NET 10.0 SDK
- ✅ Git
- ✅ GitHub CLI (`gh`) - Install from https://cli.github.com/
- ✅ Docker Desktop
- ✅ PowerShell 7+ (for automation scripts)

### GitHub Setup
1. Authenticate with GitHub CLI:
   ```bash
   gh auth login
   ```

2. Verify you have access to the MALIEV-Co-Ltd organization

3. Generate a GITOPS_PAT (Personal Access Token) with `read:packages` scope

---

## Implementation Phases

The implementation is divided into 11 phases with 340 total tasks:

| Phase | Tasks | Description | Can Be Automated |
|-------|-------|-------------|------------------|
| **Phase 1: Setup** | T001-T053 | Create repositories and scaffold projects | ✅ YES (scripts provided) |
| **Phase 2: Foundational** | T054-T090 | ServiceDefaults, CI/CD, Docker, Integration Events | ⚠️ PARTIAL |
| **Phase 3: User Story 1 (P1)** | T091-T131 | Slim down Employee Service | ❌ MANUAL |
| **Phase 4: User Story 2 (P2)** | T132-T161 | Create Leave Service | ❌ MANUAL |
| **Phase 5: User Story 3 (P2)** | T162-T186 | Create Compensation Service | ❌ MANUAL |
| **Phase 6: User Story 4 (P3)** | T187-T209 | Create Performance Service | ❌ MANUAL |
| **Phase 7: User Story 5 (P3)** | T210-T237 | Create Lifecycle Service | ❌ MANUAL |
| **Phase 8: User Story 6 (P3)** | T238-T257 | Create Compliance Service | ❌ MANUAL |
| **Phase 9: User Story 7 (P3)** | T258-T287 | Extend Career Service | ❌ MANUAL |
| **Phase 10: Saga Pattern** | T288-T304 | Implement distributed transactions | ❌ MANUAL |
| **Phase 11: Polish** | T305-T340 | Documentation, validation, testing | ⚠️ PARTIAL |

---

## Step-by-Step Execution

### Step 1: Run Phase 1 Automation (Tasks T001-T053)

**Duration**: ~10-15 minutes

```powershell
# Navigate to specs directory
cd B:\maliev\Maliev.EmployeeService\specs\003-employee-service-migration

# Run setup script (creates 5 new repositories)
.\setup-repositories.ps1 -Organization "MALIEV-Co-Ltd" -ParentDir "B:\maliev"

# Verify setup completed successfully
.\verify-setup.ps1 -ParentDir "B:\maliev"
```

**What this does**:
- ✅ Creates 5 new GitHub repositories (T001-T005)
- ✅ Clones repositories locally (T006-T040)
- ✅ Scaffolds .NET 10.0 projects (Api, Application, Domain, Infrastructure, Tests)
- ✅ Creates nuget.config files (T041-T046)
- ✅ Creates README.md files (T047-T051)
- ✅ Creates .gitignore and .dockerignore files (T052-T053)

**Expected Output**:
```
B:\maliev\
├── Maliev.EmployeeService\          (existing - will be slimmed)
├── Maliev.LeaveService\              (new)
├── Maliev.CompensationService\       (new)
├── Maliev.PerformanceService\        (new)
├── Maliev.LifecycleService\          (new)
└── Maliev.ComplianceService\         (new)
```

**If setup fails**:
- Check GitHub CLI authentication: `gh auth status`
- Check organization access: `gh repo list MALIEV-Co-Ltd`
- Review error messages and retry

---

### Step 2: Configure NuGet Authentication

**Duration**: 2 minutes

Set environment variables for all terminal sessions:

```powershell
# Windows (PowerShell)
[System.Environment]::SetEnvironmentVariable('NUGET_USERNAME', 'your-github-username', 'User')
[System.Environment]::SetEnvironmentVariable('NUGET_PASSWORD', 'your-gitops-pat', 'User')

# For current session only
$env:NUGET_USERNAME = "your-github-username"
$env:NUGET_PASSWORD = "your-gitops-pat"
```

```bash
# Linux/Mac
export NUGET_USERNAME=your-github-username
export NUGET_PASSWORD=your-gitops-pat

# Add to ~/.bashrc or ~/.zshrc for persistence
echo 'export NUGET_USERNAME=your-github-username' >> ~/.bashrc
echo 'export NUGET_PASSWORD=your-gitops-pat' >> ~/.bashrc
```

---

### Step 3: Phase 2 - Foundational Setup (Tasks T054-T090)

**Duration**: 1-2 hours

This phase must be completed **before** any user story implementation.

#### T054-T059: Add ServiceDefaults Package

For each service:
```bash
cd B:\maliev\Maliev.LeaveService
dotnet add Maliev.LeaveService.Api package Maliev.Aspire.ServiceDefaults
```

Repeat for:
- Maliev.CompensationService.Api
- Maliev.PerformanceService.Api
- Maliev.LifecycleService.Api
- Maliev.ComplianceService.Api

#### T060-T064: Configure Program.cs

Update each service's `Program.cs`:

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

#### T065-T067: Create Integration Events (Employee Service)

In `Maliev.EmployeeService.Domain/IntegrationEvents/`:

**EmployeeCreatedIntegrationEvent.cs**:
```csharp
public record EmployeeCreatedIntegrationEvent
{
    public Guid EmployeeId { get; init; }
    public string EmployeeNumber { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public DateTime HireDate { get; init; }
    public Guid DepartmentId { get; init; }
    public Guid? ManagerId { get; init; }
    public string JobTitle { get; init; } = null!;
}
```

**EmployeeTerminatedIntegrationEvent.cs**:
```csharp
public record EmployeeTerminatedIntegrationEvent
{
    public Guid EmployeeId { get; init; }
    public string EmployeeNumber { get; init; } = null!;
    public DateTime TerminationDate { get; init; }
}
```

**DepartmentTransferredIntegrationEvent.cs**:
```csharp
public record DepartmentTransferredIntegrationEvent
{
    public Guid EmployeeId { get; init; }
    public Guid OldDepartmentId { get; init; }
    public Guid NewDepartmentId { get; init; }
    public DateTime EffectiveDate { get; init; }
}
```

#### T068-T070: Saga Infrastructure

Create migration in `Maliev.EmployeeService.Infrastructure/Migrations/`:

```bash
cd B:\maliev\Maliev.EmployeeService
dotnet ef migrations add AddSagaTables --project Maliev.EmployeeService.Infrastructure
```

#### T071-T075: Create Dockerfiles

In each service's `Api` project folder, create `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY nuget.config ./
COPY ["Maliev.LeaveService.Api/Maliev.LeaveService.Api.csproj", "Maliev.LeaveService.Api/"]
RUN --mount=type=secret,id=nuget_username \
    --mount=type=secret,id=nuget_password \
    NUGET_USERNAME=$(cat /run/secrets/nuget_username) \
    NUGET_PASSWORD=$(cat /run/secrets/nuget_password) \
    dotnet restore "Maliev.LeaveService.Api/Maliev.LeaveService.Api.csproj"
COPY . .
WORKDIR "/src/Maliev.LeaveService.Api"
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

#### T076-T090: Create CI/CD Workflows

In each service's `.github/workflows/` directory, create:

**ci-develop.yml**, **ci-staging.yml**, **ci-main.yml**

(Use existing Employee Service workflows as templates)

**Checkpoint**: Verify all services build successfully:
```bash
cd B:\maliev\Maliev.LeaveService && dotnet build
cd B:\maliev\Maliev.CompensationService && dotnet build
# ... repeat for all services
```

---

### Step 4: Phase 3 - User Story 1 (Slim Employee Service)

**Duration**: 4-6 hours

Work in `B:\maliev\Maliev.EmployeeService`

#### Key Tasks:
- **T091-T096**: Delete migrated controllers (LeaveController, CompensationController, etc.)
- **T097-T102**: Delete migrated entities (LeaveRequest, CompensationRecord, etc.)
- **T103-T104**: Update EmployeeDbContext (remove DbSets for migrated entities)
- **T105-T113**: Verify core entities remain (Employee, Department, Team, etc.)
- **T114-T122**: Verify core controllers remain
- **T123-T124**: Update EmployeePermissions (remove migrated permissions)
- **T125-T127**: Implement integration event publishing
- **T128-T129**: Add GDPR compliance background service
- **T130-T131**: Create and apply database migration

**Testing**: Verify Employee Service endpoints work:
```bash
dotnet test Maliev.EmployeeService.Tests
```

---

### Step 5: Phase 4-9 - User Stories 2-7 (New Services)

**Duration**: 3-5 days (varies by service complexity)

For each service (Leave, Compensation, Performance, Lifecycle, Compliance, Career):

1. **Create domain entities** (see `data-model.md`)
2. **Create DbContext** with entity configurations
3. **Create commands and queries** (Application layer)
4. **Create API controllers** (see `contracts/api-contracts-summary.md`)
5. **Create permissions** (Authorization)
6. **Create integration event consumers**
7. **Create background services** (if applicable)
8. **Write integration tests** (with Testcontainers)

**Reference**:
- Entity schemas: `data-model.md`
- API endpoints: `contracts/api-contracts-summary.md`
- Technical decisions: `research.md`
- Code examples: `quickstart.md`

---

### Step 6: Phase 10 - Saga Pattern Implementation

**Duration**: 1-2 days

Implement employee termination saga with compensating transactions.

See `tasks.md` T288-T304 for detailed steps.

---

### Step 7: Phase 11 - Polish & Validation

**Duration**: 1 day

- Update README files
- Verify OpenAPI/Scalar UI documentation
- Verify health check endpoints
- Run code quality checks
- Security hardening
- Performance validation
- Final testing

---

## Parallel Execution Strategy

**With 7 Developers** (1 per service):

After Phase 2 completes:
- Developer A: User Story 1 (Employee Service) - **BLOCKING** for saga
- Developer B: User Story 2 (Leave Service)
- Developer C: User Story 3 (Compensation Service)
- Developer D: User Story 4 (Performance Service)
- Developer E: User Story 5 (Lifecycle Service)
- Developer F: User Story 6 (Compliance Service)
- Developer G: User Story 7 (Career Service)

Then team reconvenes for Phase 10 (Saga) and Phase 11 (Polish).

**With 1-2 Developers** (sequential):

Follow priority order: US1 (P1) → US2, US3 (P2) → US4, US5, US6, US7 (P3)

---

## Troubleshooting

### Issue: NuGet restore fails
**Solution**: Check environment variables:
```powershell
echo $env:NUGET_USERNAME
echo $env:NUGET_PASSWORD
```

### Issue: GitHub CLI authentication fails
**Solution**: Re-authenticate:
```bash
gh auth login
gh auth status
```

### Issue: Repository creation fails
**Solution**: Check organization permissions:
```bash
gh api /user/memberships/orgs/MALIEV-Co-Ltd
```

### Issue: .NET SDK version mismatch
**Solution**: Verify .NET 10.0 is installed:
```bash
dotnet --list-sdks
```

---

## Success Criteria

**Phase 1 Complete When**:
- ✅ 5 new repositories exist on GitHub
- ✅ All repositories cloned locally
- ✅ All projects build successfully (`dotnet build`)
- ✅ `verify-setup.ps1` passes all checks

**Phase 2 Complete When**:
- ✅ All services reference Maliev.Aspire.ServiceDefaults
- ✅ All Program.cs files configured
- ✅ Integration events created
- ✅ Dockerfiles created
- ✅ CI/CD workflows created

**Full Implementation Complete When**:
- ✅ Employee Service LOC reduced from ~82K to ~25K (70% reduction)
- ✅ All 7 services independently deployable
- ✅ All integration tests pass
- ✅ All integration events publish/consume correctly
- ✅ Saga pattern demonstrates rollback capability
- ✅ All services expose functional health check endpoints
- ✅ Zero cross-service database joins exist

---

## Next Steps

1. **Run Phase 1 automation**:
   ```powershell
   .\setup-repositories.ps1
   ```

2. **Verify setup**:
   ```powershell
   .\verify-setup.ps1
   ```

3. **Begin Phase 2** (Foundational tasks)

4. **Track progress** in `tasks.md` by marking completed tasks as `[x]`

---

## Support

- **Specification**: See `spec.md`
- **Architecture**: See `plan.md`
- **Data Models**: See `data-model.md`
- **API Contracts**: See `contracts/api-contracts-summary.md`
- **Technical Research**: See `research.md`
- **Code Examples**: See `quickstart.md`
- **Full Task List**: See `tasks.md`

**Questions?** Review the documentation above or consult the MALIEV constitution at `.specify/memory/constitution.md`
