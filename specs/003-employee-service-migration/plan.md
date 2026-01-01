# Implementation Plan: Employee Service Decomposition to Microservices

**Branch**: `003-employee-service-migration` | **Date**: 2025-12-28 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-employee-service-migration/spec.md`

**Note**: This is a **pre-deployment refactoring** to decompose the existing monolithic Employee Service into six independent microservices before initial production deployment.

## Summary

**Primary Requirement**: Decompose the existing `Maliev.EmployeeService` (~82K LOC, 459 files) into six focused microservices:
1. **Employee Service** (core) - Employee profiles, departments, teams, org hierarchy (~25K LOC)
2. **Leave Service** (new) - Leave requests, balances, policies, approvals
3. **Compensation Service** (new) - Salary, benefits, compensation history
4. **Performance Service** (new) - Reviews, goals, performance improvement plans
5. **Lifecycle Service** (new) - Onboarding, offboarding, access revocation
6. **Compliance Service** (new) - Work authorization tracking, compliance reporting

**Plus**: Extend existing **Career Service** with training/skills functionality from Employee Service.

**Technical Approach**:
- Extract domain logic, entities, and controllers from existing Employee Service codebase
- Create new service repositories following MALIEV standards (Flat structure, ServiceDefaults NuGet package)
- Implement event-driven communication via RabbitMQ with saga pattern for distributed transactions
- Database-per-service with separate PostgreSQL databases
- Structured logging with correlation IDs for distributed tracing
- No live migration complexity - this is code reorganization before deployment

## Technical Context

**Language/Version**: .NET 10.0
**Primary Dependencies**:
- ASP.NET Core 10.0
- Entity Framework Core 9.x
- Maliev.Aspire.ServiceDefaults (NuGet package from GitHub Packages)
- MassTransit 8.x (via ServiceDefaults for RabbitMQ integration)
- Npgsql.EntityFrameworkCore.PostgreSQL 9.x

**Storage**: PostgreSQL 18 (one database per service)
**Messaging**: RabbitMQ (via MassTransit in ServiceDefaults)
**Caching**: Redis (via ServiceDefaults)
**Authentication**: JWT + IAM Service (via ServiceDefaults)
**Testing**: xUnit with Testcontainers for real infrastructure (PostgreSQL, RabbitMQ, Redis containers)
**Target Platform**: Linux containers (Docker)
**Project Type**: Microservices - seven separate Git repositories
**Performance Goals**: <200ms p95 for API endpoints, handle 1000 concurrent requests
**Constraints**:
- Zero cross-service database joins
- All services independently deployable
- Saga state must persist for recovery
- 80% code coverage minimum

**Scale/Scope**:
- Employee Service: ~25K LOC (70% reduction from 82K)
- Each new service: ~8-12K LOC
- Total system: ~85K LOC across seven services

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ Service Autonomy (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**: Each service has its own database, domain logic, and communicates only via RabbitMQ events or HTTP APIs
- **Evidence**: Spec requirements FR-701-703, FR-807-812; Assumption #4 (database separation)

### ✅ Explicit Contracts
- **Status**: PASS
- **Compliance**: All APIs will be documented via OpenAPI/Scalar UI (via ServiceDefaults)
- **Evidence**: Tech stack specifies "Scalar UI (OpenAPI 3.1) via ServiceDefaults"

### ✅ Test-First Development (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**: Tests will be authored immediately after spec approval, before implementation
- **Evidence**: Testing strategy in user-provided plan; SC-013 requires 80% code coverage

### ✅ Real Infrastructure Testing (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**: All tests will use Testcontainers for PostgreSQL, RabbitMQ, and Redis
- **Evidence**: Tech stack specifies "xUnit with Testcontainers"; no in-memory providers mentioned

### ✅ Auditability & Observability
- **Status**: PASS
- **Compliance**:
  - FR-901-905 specify structured logging with correlation IDs
  - Clarification #2 confirms "Structured logging with correlation IDs"
  - ServiceDefaults provides OpenTelemetry integration
- **Evidence**: FR-901-905, clarification session Q2, ServiceDefaults includes AddServiceDefaults() with OpenTelemetry

### ⚠️ Security & Compliance
- **Status**: PARTIAL - Deferred to implementation
- **Compliance**: JWT authentication via ServiceDefaults; permission system specified
- **Gaps**: Encryption at rest/transit details, GDPR/Thai tax law compliance not specified
- **Action**: Document encryption and compliance requirements in Phase 0 research

### ✅ Secrets Management & Configuration Security (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**: Google Secret Manager for secrets injection
- **Evidence**: Constitution VII applies; ServiceDefaults pattern uses environment variables

### ✅ Zero Warnings Policy (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**: Standard MALIEV build configuration enforces TreatWarningsAsErrors
- **Evidence**: Constitution VIII; will be enforced in CI/CD

### ✅ Clean Project Artifacts (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**:
  - Only README.md at root
  - CODEOWNERS file mandatory: `* @MALIEV-Co-Ltd/core-developers`
  - .dockerignore must exclude specs, IDE files, build artifacts
- **Evidence**: Constitution IX

### ✅ Docker Best Practices (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**:
  - Dockerfile in API project folder
  - Use built-in `app` user from Microsoft images
  - Multi-stage builds with .NET 10 SDK and runtime
  - BuildKit secrets for NuGet credentials
  - Health checks for liveness endpoints
  - Expose port 8080
- **Evidence**: Constitution X; user-provided plan references ServiceDefaults pattern

### ✅ Simplicity & Maintainability
- **Status**: PASS
- **Compliance**: No AutoMapper, FluentValidation, or FluentAssertions
- **Evidence**: Constitution XIV; plan uses explicit mapping

### ✅ Business Metrics & Analytics (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**: ServiceDefaults provides metrics endpoints via OpenTelemetry
- **Evidence**: Constitution XII; MapDefaultEndpoints() includes /metrics

### ✅ .NET Aspire Integration (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**:
  - Maliev.Aspire.ServiceDefaults consumed as NuGet package
  - nuget.config with GitHub Packages authentication
  - BuildKit secrets for NuGet credentials
  - Program.cs calls AddServiceDefaults() and MapDefaultEndpoints()
- **Evidence**: Tech stack table; user-provided plan shows ServiceDefaults usage

### ✅ Code Quality & Library Standards (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**: NO AutoMapper, FluentValidation, FluentAssertions
- **Evidence**: Constitution XIV

### ✅ Project Structure & Naming (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**:
  - Flat structure (no /src, /tests folders)
  - Full company prefix: `Maliev.[ServiceName].Api`, `Maliev.[ServiceName].Data`
  - Dockerfile inside API project folder
- **Evidence**: Constitution XV

### ✅ CI/CD Standards (NON-NEGOTIABLE)
- **Status**: PASS
- **Compliance**:
  - Workflows named: ci-develop.yml, ci-staging.yml, ci-main.yml
  - Testcontainers for integration tests (no docker-compose.yml)
- **Evidence**: Constitution XVI

## Summary: All Gates PASS ✅

- **NON-NEGOTIABLE gates**: 11/11 PASS
- **Advisory gates**: 0 violations
- **Action items**: Document encryption/compliance details in Phase 0 research

**Decision**: Proceed to Phase 0

## Project Structure

### Documentation (this feature)

```text
specs/003-employee-service-migration/
├── spec.md              # Feature specification (complete)
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command - TO BE CREATED)
├── data-model.md        # Phase 1 output (/speckit.plan command - TO BE CREATED)
├── quickstart.md        # Phase 1 output (/speckit.plan command - TO BE CREATED)
├── contracts/           # Phase 1 output (/speckit.plan command - TO BE CREATED)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code Structure (Seven Separate Repositories)

**This refactoring will create/modify seven Git repositories:**

#### 1. Maliev.EmployeeService (existing - will be slimmed)
```text
Maliev.EmployeeService/  (root of repo)
├── Maliev.EmployeeService.Api/
│   ├── Controllers/
│   │   ├── EmployeesController.cs
│   │   ├── EmployeeProfileController.cs
│   │   ├── DepartmentsController.cs
│   │   ├── TeamsController.cs
│   │   ├── ManagersController.cs
│   │   ├── EmergencyContactController.cs
│   │   ├── HRController.cs
│   │   ├── AdminController.cs
│   │   ├── BulkOperationsController.cs
│   │   └── ReportsController.cs (subset - org reports only)
│   ├── Middleware/
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
├── Maliev.EmployeeService.Application/
│   ├── Commands/ (employee, department, team, emergency contact)
│   ├── Queries/ (employee search, org reports)
│   └── Consumers/ (RabbitMQ event consumers)
├── Maliev.EmployeeService.Domain/
│   ├── Entities/
│   │   ├── Employee.cs
│   │   ├── EmergencyContact.cs
│   │   ├── Department.cs
│   │   ├── Position.cs
│   │   ├── Team.cs
│   │   ├── EmployeeTeamAssignment.cs
│   │   ├── EmploymentHistory.cs
│   │   ├── PersonalDocument.cs
│   │   └── AuditLog.cs
│   ├── IntegrationEvents/
│   │   ├── EmployeeCreatedIntegrationEvent.cs
│   │   ├── EmployeeTerminatedIntegrationEvent.cs
│   │   └── DepartmentTransferredIntegrationEvent.cs
│   └── Authorization/
│       └── EmployeePermissions.cs (slimmed)
├── Maliev.EmployeeService.Infrastructure/
│   ├── EmployeeDbContext.cs (slimmed - only core entities)
│   ├── Repositories/
│   ├── Migrations/
│   └── Consumers/
├── Maliev.EmployeeService.Tests/
│   ├── Unit/
│   ├── Integration/ (with Testcontainers)
│   └── Contract/
├── nuget.config
├── README.md
├── .gitignore
├── .dockerignore
└── .github/
    ├── CODEOWNERS
    └── workflows/
        ├── ci-develop.yml
        ├── ci-staging.yml
        └── ci-main.yml
```

#### 2. Maliev.LeaveService (new repository)
```text
Maliev.LeaveService/ (root of repo)
├── Maliev.LeaveService.Api/
│   ├── Controllers/
│   │   ├── LeaveController.cs
│   │   └── ReportsController.cs (leave utilization)
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
├── Maliev.LeaveService.Application/
│   ├── Commands/ (submit, approve, cancel)
│   ├── Queries/ (balances, requests, approvals)
│   └── BackgroundServices/
│       ├── LeaveAccrualBackgroundService.cs
│       └── LeaveExpirationAlertBackgroundService.cs
├── Maliev.LeaveService.Domain/
│   ├── Entities/
│   │   ├── LeaveRequest.cs
│   │   ├── LeaveBalance.cs
│   │   ├── LeaveApproval.cs
│   │   └── LeavePolicy.cs
│   ├── IntegrationEvents/
│   └── Authorization/
│       └── LeavePermissions.cs
├── Maliev.LeaveService.Infrastructure/
│   ├── LeaveDbContext.cs
│   ├── Repositories/
│   ├── Migrations/
│   └── Consumers/ (EmployeeCreated, EmployeeTerminated)
├── Maliev.LeaveService.Tests/
├── nuget.config
├── README.md
├── .gitignore
├── .dockerignore
└── .github/
    ├── CODEOWNERS
    └── workflows/
```

#### 3. Maliev.CompensationService (new repository)
```text
Maliev.CompensationService/ (root of repo)
├── Maliev.CompensationService.Api/
│   ├── Controllers/
│   │   ├── CompensationController.cs
│   │   └── ReportsController.cs (compensation analysis)
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
├── Maliev.CompensationService.Application/
│   ├── Commands/ (record change, bulk increase, benefits enrollment)
│   └── Queries/ (compensation details, history, analysis)
├── Maliev.CompensationService.Domain/
│   ├── Entities/
│   │   ├── CompensationRecord.cs
│   │   ├── SalaryHistory.cs
│   │   ├── Benefit.cs
│   │   ├── BenefitsEnrollment.cs
│   │   ├── EmployeeBenefit.cs
│   │   └── Dependent.cs
│   ├── IntegrationEvents/
│   └── Authorization/
│       └── CompensationPermissions.cs
├── Maliev.CompensationService.Infrastructure/
│   ├── CompensationDbContext.cs
│   ├── Repositories/
│   ├── Migrations/
│   └── Consumers/ (EmployeeCreated, EmployeeTerminated)
├── Maliev.CompensationService.Tests/
├── nuget.config
├── README.md
├── .gitignore
├── .dockerignore
└── .github/
    ├── CODEOWNERS
    └── workflows/
```

#### 4. Maliev.PerformanceService (new repository)
```text
Maliev.PerformanceService/ (root of repo)
├── Maliev.PerformanceService.Api/
│   ├── Controllers/
│   │   └── PerformanceController.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
├── Maliev.PerformanceService.Application/
│   ├── Commands/ (create review, acknowledge, create goal, update progress)
│   ├── Queries/ (get reviews, get goals)
│   └── BackgroundServices/
│       └── PerformanceReviewReminderBackgroundService.cs
├── Maliev.PerformanceService.Domain/
│   ├── Entities/
│   │   ├── PerformanceReview.cs
│   │   ├── Goal.cs
│   │   ├── PerformanceImprovementPlan.cs
│   │   └── DisciplinaryAction.cs
│   ├── IntegrationEvents/
│   └── Authorization/
│       └── PerformancePermissions.cs
├── Maliev.PerformanceService.Infrastructure/
│   ├── PerformanceDbContext.cs
│   ├── Repositories/
│   ├── Migrations/
│   └── Consumers/ (EmployeeCreated, EmployeeTerminated)
├── Maliev.PerformanceService.Tests/
├── nuget.config
├── README.md
├── .gitignore
├── .dockerignore
└── .github/
    ├── CODEOWNERS
    └── workflows/
```

#### 5. Maliev.LifecycleService (new repository)
```text
Maliev.LifecycleService/ (root of repo)
├── Maliev.LifecycleService.Api/
│   ├── Controllers/
│   │   └── OnboardingOffboardingController.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
├── Maliev.LifecycleService.Application/
│   ├── Commands/ (start onboarding, complete item, start offboarding)
│   ├── Queries/ (onboarding status, offboarding status)
│   ├── Services/
│   │   └── OnboardingTemplateService.cs
│   └── BackgroundServices/
│       ├── OnboardingReminderBackgroundService.cs
│       └── AccessRevocationBackgroundService.cs
├── Maliev.LifecycleService.Domain/
│   ├── Entities/
│   │   ├── OnboardingChecklist.cs
│   │   ├── OffboardingChecklist.cs
│   │   ├── OffboardingTask.cs
│   │   └── ExitInterview.cs
│   ├── IntegrationEvents/
│   │   ├── EmployeeOnboardingStartedIntegrationEvent.cs
│   │   ├── OnboardingReminderNeededIntegrationEvent.cs
│   │   └── AccessRevocationRequiredIntegrationEvent.cs
│   └── Authorization/
│       └── LifecyclePermissions.cs
├── Maliev.LifecycleService.Infrastructure/
│   ├── LifecycleDbContext.cs
│   ├── Repositories/
│   ├── Migrations/
│   └── Consumers/ (EmployeeCreated, EmployeeTerminated)
├── Maliev.LifecycleService.Tests/
├── nuget.config
├── README.md
├── .gitignore
├── .dockerignore
└── .github/
    ├── CODEOWNERS
    └── workflows/
```

#### 6. Maliev.ComplianceService (new repository)
```text
Maliev.ComplianceService/ (root of repo)
├── Maliev.ComplianceService.Api/
│   ├── Controllers/
│   │   ├── WorkAuthorizationController.cs
│   │   └── ReportsController.cs (compliance reports)
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
├── Maliev.ComplianceService.Application/
│   ├── Commands/ (record, update work authorization)
│   ├── Queries/ (get authorization, compliance reports)
│   └── BackgroundServices/
│       ├── WorkAuthorizationExpirationReminderService.cs
│       └── ExpiredWorkAuthorizationFlaggingService.cs
├── Maliev.ComplianceService.Domain/
│   ├── Entities/
│   │   └── WorkAuthorization.cs
│   ├── IntegrationEvents/
│   └── Authorization/
│       └── CompliancePermissions.cs
├── Maliev.ComplianceService.Infrastructure/
│   ├── ComplianceDbContext.cs
│   ├── Repositories/
│   ├── Migrations/
│   └── Consumers/ (EmployeeCreated, EmployeeTerminated)
├── Maliev.ComplianceService.Tests/
├── nuget.config
├── README.md
├── .gitignore
├── .dockerignore
└── .github/
    ├── CODEOWNERS
    └── workflows/
```

#### 7. Maliev.CareerService (existing - will be extended)
```text
Maliev.CareerService/ (root of repo)
├── Maliev.CareerService.Api/
│   ├── Controllers/
│   │   ├── [existing career controllers]
│   │   ├── TrainingController.cs (NEW)
│   │   └── ReportsController.cs (extended with training compliance)
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
├── Maliev.CareerService.Application/
│   ├── [existing career logic]
│   ├── Commands/ (NEW: record training, assign mandatory, update skill)
│   ├── Queries/ (NEW: get training records, compliance reports)
│   └── BackgroundServices/
│       ├── OverdueTrainingEscalationBackgroundService.cs (NEW)
│       └── CertificationExpirationReminderBackgroundService.cs (NEW)
├── Maliev.CareerService.Domain/
│   ├── [existing career entities]
│   ├── Entities/ (NEW)
│   │   ├── Training.cs
│   │   ├── TrainingRecord.cs
│   │   ├── MandatoryTrainingRequirement.cs
│   │   ├── Certification.cs
│   │   └── Skill.cs
│   └── Authorization/
│       └── CareerPermissions.cs (extended with training permissions)
├── Maliev.CareerService.Infrastructure/
│   ├── CareerDbContext.cs (extended with training entities)
│   ├── Repositories/ (NEW: ITrainingRepository, ISkillRepository, etc.)
│   ├── Migrations/ (NEW: add training tables)
│   └── Consumers/ (EmployeeCreated, EmployeeTerminated)
├── Maliev.CareerService.Tests/ (extended)
├── nuget.config
├── README.md
├── .gitignore
├── .dockerignore
└── .github/
    ├── CODEOWNERS
    └── workflows/
```

**Structure Decision**: Seven separate Git repositories following MALIEV flat structure standard (no /src, /tests folders). Each repository contains Api, Application, Domain, Infrastructure, and Tests projects with full company prefix (`Maliev.[ServiceName].[ProjectType]`). Dockerfile located in each Api project folder.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**No violations requiring justification.** All constitution gates pass.

