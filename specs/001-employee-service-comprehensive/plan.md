# Implementation Plan: Employee Service - Comprehensive HR Master Data Management

**Branch**: `001-employee-service-comprehensive` | **Date**: 2025-10-12 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-employee-service-comprehensive/spec.md`

## Summary

The Employee Service is a comprehensive microservice for managing HR master data at Maliev Co. Ltd. It provides employee profile management, organizational hierarchy, leave request workflows, performance tracking, training management, document storage, and compliance features. The service supports 12 user stories covering the complete employee lifecycle from onboarding through offboarding, with role-based access control, event-driven integration, and GDPR/PDPA compliance.

**Technical Approach**: Clean Architecture with CQRS pattern, PostgreSQL persistence with Entity Framework Core, RabbitMQ for asynchronous integration, multi-level leave approval workflows, and field-level encryption for sensitive data. The service integrates with Career Service for skills catalog and work locations, and publishes events to downstream services (Payroll, Access Control, Time Tracking).

---

## Technical Context

**Language/Version**: .NET 9.0 (ASP.NET Core 9.0)
**Primary Dependencies**:
- ASP.NET Core 9.0 (Web API framework)
- Entity Framework Core 9.0 (ORM)
- Npgsql 9.0.2 (PostgreSQL driver)
- MediatR (CQRS implementation)
- MassTransit (RabbitMQ integration)
- FluentValidation (input validation)
- Serilog 8.0.2 (structured logging)

**Storage**: PostgreSQL 18 with encryption for sensitive fields (AES-256)

**Testing**:
- xUnit (test framework)
- FluentAssertions 8.6.0 (test assertions)
- Moq 4.20.72 (mocking)
- Testcontainers or Docker Compose (PostgreSQL integration testing - **IN-MEMORY DATABASES PROHIBITED**)

**Target Platform**: Linux containers (Docker/Kubernetes) deployed on GKE via ArgoCD GitOps

**Project Type**: Backend microservice (multi-project .NET solution)

**Performance Goals**:
- API response time: p95 < 200ms, p99 < 500ms
- Throughput: Handle 1000 req/s during peak load (leave request submission periods)
- Database queries: < 50ms for indexed lookups
- Concurrent users: Support 500 concurrent users without degradation

**Constraints**:
- GDPR and Thai PDPA compliant (encryption, audit logging, data subject access)
- Zero warnings build policy (warnings treated as errors)
- Secrets managed via Google Secret Manager (no hardcoded credentials)
- JWT-based authentication via Maliev Auth Service
- Asynchronous integration with Career Service, Payroll Service, Time Tracking Service

**Scale/Scope**:
- Initial deployment: 500 employees, 20 departments
- Growth projection: 10% annual employee growth (~50 new employees/year)
- 12 user stories spanning employee lifecycle, leave management, performance tracking, training, and compliance
- Database size: ~500 MB initial, ~100 MB annual growth
- API endpoints: ~30 REST endpoints across employee, leave, department, and manager operations

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Service Autonomy (NON-NEGOTIABLE)
✅ **PASS**: Employee Service has its own PostgreSQL database (`employee_app_db`). No direct database access to other services. Integration via APIs (Career Service) and events (RabbitMQ).

### II. Explicit Contracts
✅ **PASS**: All APIs documented via OpenAPI with Scalar UI (development only). Contracts defined in `contracts/` directory:
- `employees-api.yaml` - Employee profile management
- `leave-api.yaml` - Leave balances and requests
- `departments-api.yaml` - Organizational structure

API versioning: `/employees/v1/` with backward-compatible changes only.

### III. Test-First Development (NON-NEGOTIABLE)
✅ **PASS**: Tests authored before implementation following TDD (Red-Green-Refactor):
- Unit tests for domain logic (leave balance calculations, circular hierarchy detection)
- Integration tests for repositories (with PostgreSQL via Testcontainers)
- Contract tests for API endpoints (OpenAPI compliance)
- Minimum 80% coverage for business-critical logic

Test structure: `Maliev.EmployeeService.Tests/{Unit,Integration,Contract}`

### IV. PostgreSQL-Only Testing (NON-NEGOTIABLE)
✅ **PASS**: ALL integration tests use real PostgreSQL via Docker containers:
- Testcontainers or Docker Compose provisions PostgreSQL test databases
- NO EF Core InMemoryDatabase provider in any test project
- Test databases use same schema and migrations as production
- CI/CD pipelines start PostgreSQL containers before test execution
- Integration test base class handles PostgreSQL connection and transaction-based cleanup

**Implementation**:
- `docker-compose.test.yml` for local PostgreSQL test database
- `IntegrationTestBase` class with PostgreSQL setup and teardown
- CI workflows modified to provision PostgreSQL before `dotnet test`

### V. Auditability & Observability
✅ **PASS**:
- Structured JSON logging via Serilog (console output)
- Immutable `AuditLog` entity tracking all CRUD operations on sensitive data
- 7-year audit log retention for compliance
- Health checks: `/employees/liveness` (Kubernetes liveness probe), `/employees/readiness` (database + RabbitMQ health)
- Prometheus metrics endpoint: `/metrics`

### VI. Security & Compliance
✅ **PASS**:
- JWT authentication via Maliev Auth Service (Bearer token)
- Role-based authorization: Employee, Manager, HRGeneralist, HRSpecialist, SystemAdministrator
- Field-level encryption for sensitive data (salary, Thai national ID, passport) using AES-256
- TLS 1.3 for all API communication
- GDPR/PDPA compliance: explicit consent tracking, data subject access requests, right to erasure

### VII. Secrets Management & Configuration Security (NON-NEGOTIABLE)
✅ **PASS**:
- All secrets injected from Google Secret Manager (mounted at `/mnt/secrets`)
- No secrets in source code or `appsettings.json`
- Development uses `appsettings.Development.json` with local credentials (not committed)
- Connection strings, encryption keys, API keys loaded from secrets

### VIII. Zero Warnings Policy (NON-NEGOTIABLE)
✅ **PASS**:
- CI/CD pipeline enforces `/p:TreatWarningsAsErrors=true`
- Build fails on any compiler warnings
- Code analysis enabled with strict ruleset

### IX. Clean Project Artifacts (NON-NEGOTIABLE)
✅ **PASS**:
- `.gitignore` excludes `bin/`, `obj/`, `.vs/`, `*.user`, `appsettings.Development.json`
- No unused files or generated artifacts in repository
- Pre-release cleanup enforced in CI/CD

### X. Simplicity & Maintainability
✅ **PASS**:
- YAGNI applied: No speculative features beyond 12 user stories
- Clean Architecture: Clear separation of Domain, Application, Infrastructure, API layers
- CQRS pattern: Separate command/query models for clarity
- Repository pattern: Abstraction over EF Core for testability
- Shared libraries: None yet (will version and document if needed)

### XI. Business Metrics & Analytics (NON-NEGOTIABLE)
✅ **PASS**:
- Prometheus metrics exposed at `/metrics` endpoint
- Business metrics tracked:
  - Employee onboarding cycle time (candidate to first-day-ready)
  - Leave request approval time (submission to final approval)
  - Active employees by department and employment type
  - Training compliance rate (mandatory training completion)
  - Work authorization expiration alerts (90-day advance notice)
- System health metrics: request rate, response time, error rate, database connection pool
- Metrics tagged with: `service_name`, `version`, `environment`, `region`
- No PII exposure in metrics (employee IDs anonymized)

**Constitution Compliance**: ✅ **ALL PRINCIPLES SATISFIED** - No violations requiring justification.

---

## Project Structure

### Documentation (this feature)

```
specs/001-employee-service-comprehensive/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Architectural decisions and design rationale
├── data-model.md        # Complete data model with entities, relationships, validation rules
├── quickstart.md        # Development setup and testing guide
├── contracts/           # OpenAPI specifications
│   ├── employees-api.yaml      # Employee profile management API
│   ├── leave-api.yaml          # Leave management API
│   └── departments-api.yaml    # Department and organizational hierarchy API
├── spec.md              # Feature specification (12 user stories)
└── tasks.md             # Implementation tasks (created by /speckit.tasks command)
```

### Source Code (repository root)

```
Maliev.EmployeeService/
├── .github/
│   └── workflows/
│       ├── ci-develop.yml       # CI/CD for develop branch
│       ├── ci-staging.yml       # CI/CD for staging branch
│       └── ci-main.yml          # CI/CD for main branch
│
├── Maliev.EmployeeService.Domain/
│   ├── Entities/                # Employee, Department, LeaveRequest, etc.
│   ├── ValueObjects/            # LegalName, ContactInformation, etc.
│   ├── Enums/                   # EmploymentType, EmploymentStatus, LeaveType, etc.
│   └── Interfaces/              # IEmployeeRepository, IDepartmentRepository, etc.
│
├── Maliev.EmployeeService.Application/
│   ├── Commands/                # CQRS commands (CreateEmployee, SubmitLeaveRequest, etc.)
│   ├── Queries/                 # CQRS queries (GetEmployeeProfile, GetLeaveBalances, etc.)
│   ├── DTOs/                    # Data transfer objects
│   ├── Validators/              # FluentValidation validators
│   ├── Mappings/                # AutoMapper profiles
│   └── Interfaces/              # IEncryptionService, ICareerServiceClient, etc.
│
├── Maliev.EmployeeService.Infrastructure/
│   ├── Persistence/
│   │   ├── EmployeeServiceDbContext.cs
│   │   ├── Configurations/      # EF Core entity configurations
│   │   ├── Repositories/        # Repository implementations
│   │   ├── Migrations/          # EF Core migrations
│   │   └── Interceptors/        # Encryption interceptor, audit log interceptor
│   ├── Integration/
│   │   ├── RabbitMQ/            # MassTransit message consumers and publishers
│   │   ├── CareerService/       # Career Service client (skills, work locations)
│   │   └── Events/              # Integration event definitions
│   ├── BackgroundServices/      # Hosted services (leave accrual, expiration checks)
│   ├── Encryption/              # AES encryption service
│   └── Caching/                 # Redis caching service
│
├── Maliev.EmployeeService.Api/
│   ├── Controllers/             # Employee, Leave, Department, Manager controllers
│   ├── Middleware/              # Exception handling, request logging
│   ├── Configurations/          # Scalar (dev), authentication, authorization setup
│   ├── Models/                  # API request/response models
│   ├── Dockerfile               # Multi-stage Docker build
│   ├── Program.cs               # Application entry point
│   ├── appsettings.json         # Configuration template
│   └── Properties/
│       └── launchSettings.json  # Development launch profiles
│
├── Maliev.EmployeeService.Tests/
│   ├── Unit/
│   │   ├── Domain/              # Entity business logic tests
│   │   ├── Application/         # Command/query handler tests
│   │   └── Validators/          # Validation logic tests
│   ├── Integration/
│   │   ├── Repositories/        # Repository tests with Testcontainers
│   │   ├── BackgroundServices/  # Background job tests
│   │   └── Integration/         # RabbitMQ integration tests
│   └── Contract/
│       └── ApiTests/            # OpenAPI schema compliance tests
│
├── specs/                       # Feature specifications (this directory)
├── .dockerignore
├── .gitignore
├── Maliev.EmployeeService.sln   # Solution file
└── README.md                    # Project overview
```

**Structure Decision**: Multi-project .NET solution following Clean Architecture principles. This structure provides:
- **Clear Separation of Concerns**: Domain, Application, Infrastructure, API layers isolated
- **Testability**: Business logic decoupled from infrastructure (database, external services)
- **Maintainability**: Dependencies flow inward (API → Application → Domain)
- **Scalability**: Infrastructure layer can be swapped (e.g., replace PostgreSQL with SQL Server) without affecting business logic

**Layer Dependencies**:
- **Domain**: No dependencies (pure business logic)
- **Application**: Depends on Domain (orchestrates business logic)
- **Infrastructure**: Depends on Application and Domain (implements interfaces)
- **Api**: Depends on Application and Infrastructure (composition root)

---

## Complexity Tracking

*No constitution violations detected. This section is not applicable.*

---

## Implementation Phases

### Phase 0: Research & Outline ✅ COMPLETE
- ✅ Architectural decisions documented in `research.md`
- ✅ Technical context filled in this document
- ✅ Constitution compliance verified

### Phase 1: Design & Contracts ✅ COMPLETE
- ✅ Complete data model documented in `data-model.md`
- ✅ OpenAPI contracts created in `contracts/` directory
- ✅ Quickstart guide created in `quickstart.md`
- ✅ Project structure defined

### Phase 2: Task Generation (Next Step)
- Run `/speckit.tasks` to generate `tasks.md` with prioritized implementation tasks
- Tasks will be dependency-ordered based on user story priorities and technical dependencies

### Phase 3: Implementation (After Task Generation)
- Execute tasks following TDD approach (tests before implementation)
- Implement in priority order: P1 → P2 → P3
- CI/CD pipelines validate constitution compliance at each commit

---

## Key Design Decisions

### 1. Clean Architecture with CQRS
**Rationale**: Separation of concerns, testability, independent scaling of commands vs. queries.

### 2. Event-Driven Integration (RabbitMQ)
**Rationale**: Decoupling, resilience, asynchronous processing prevents blocking operations.

### 3. Multi-Level Leave Approval Workflow
**Rationale**: Business requirement for extended leave and exceptional cases requiring senior management approval.

### 4. Field-Level Encryption (AES-256)
**Rationale**: GDPR/PDPA compliance, protection against database breaches.

### 5. Repository Pattern
**Rationale**: Testability (mock repositories), encapsulation of complex queries, flexibility to swap ORM.

### 6. Optimistic Concurrency Control
**Rationale**: Prevent lost updates from concurrent edits without database locks.

### 7. Closure Table for Department Hierarchy
**Rationale**: Efficient ancestor/descendant queries without recursive SQL.

### 8. Background Jobs with Hosted Services
**Rationale**: Native ASP.NET Core integration, sufficient for monthly accruals and daily checks.

For detailed rationale and trade-offs, see `research.md`.

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Career Service integration failure | Manual employee creation fallback, retry logic with exponential backoff |
| Performance degradation with 500 concurrent users | Load testing in staging, horizontal scaling with Kubernetes HPA, caching strategy |
| Leave balance calculation errors | Extensive unit tests, monthly reconciliation with audit logs |
| PDPA compliance gaps | Legal review of data handling, annual compliance audit |
| Circular hierarchy detection performance | Pre-computed closure table, limit hierarchy depth to 10 levels |

---

## Next Steps

1. **Generate Implementation Tasks**: Run `/speckit.tasks` to create dependency-ordered tasks
2. **Set Up Development Environment**: Follow `quickstart.md` to set up local PostgreSQL, RabbitMQ, Redis
3. **Create Project Structure**: Scaffold projects following structure defined above
4. **Implement Foundation**: User entity, authentication, health checks
5. **Execute P1 User Stories**: Employee profile management (US-1), HR lifecycle management (US-2)

---

**Document Status**: Complete
**Last Updated**: 2025-10-12
**Next Action**: Run `/speckit.tasks` to generate implementation tasks
