# Tasks: Employee Service Decomposition to Microservices

**Feature**: 003-employee-service-migration
**Input**: Design documents from `/specs/003-employee-service-migration/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each microservice.

**Important**: This is a **pre-deployment refactoring** - decomposing the monolithic Employee Service into 6 new microservices before initial production deployment.

## Format: `- [ ] [ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4, US5, US6, US7)
- All tasks include exact file paths

---

## Phase 1: Setup (Repository & Project Scaffolding)

**Purpose**: Create new Git repositories and scaffold project structure for all 6 new microservices

**Repositories to Create**:
1. Maliev.LeaveService
2. Maliev.CompensationService
3. Maliev.PerformanceService
4. Maliev.LifecycleService
5. Maliev.ComplianceService

**Repository to Extend**:
- Maliev.CareerService (existing)

**Repository to Slim**:
- Maliev.EmployeeService (existing)

### Repository Creation

- [X] T001 [P] Create Maliev.LeaveService repository on GitHub with CODEOWNERS file (* @MALIEV-Co-Ltd/core-developers)
- [X] T002 [P] Create Maliev.CompensationService repository on GitHub with CODEOWNERS file
- [X] T003 [P] Create Maliev.PerformanceService repository on GitHub with CODEOWNERS file
- [X] T004 [P] Create Maliev.LifecycleService repository on GitHub with CODEOWNERS file
- [X] T005 [P] Create Maliev.ComplianceService repository on GitHub with CODEOWNERS file

### Project Scaffolding - Leave Service

- [X] T006 [P] Clone Maliev.LeaveService repository and create .NET solution file
- [X] T007 [P] Create Maliev.LeaveService.Api project (webapi template, .NET 10.0)
- [X] T008 [P] Create Maliev.LeaveService.Application project (classlib template, .NET 10.0)
- [X] T009 [P] Create Maliev.LeaveService.Domain project (classlib template, .NET 10.0)
- [X] T010 [P] Create Maliev.LeaveService.Infrastructure project (classlib template, .NET 10.0)
- [X] T011 [P] Create Maliev.LeaveService.Tests project (xunit template, .NET 10.0)
- [X] T012 Add all projects to Maliev.LeaveService.sln

### Project Scaffolding - Compensation Service

- [X] T013 [P] Clone Maliev.CompensationService repository and create .NET solution file
- [X] T014 [P] Create Maliev.CompensationService.Api project (webapi template, .NET 10.0)
- [X] T015 [P] Create Maliev.CompensationService.Application project (classlib template, .NET 10.0)
- [X] T016 [P] Create Maliev.CompensationService.Domain project (classlib template, .NET 10.0)
- [X] T017 [P] Create Maliev.CompensationService.Infrastructure project (classlib template, .NET 10.0)
- [X] T018 [P] Create Maliev.CompensationService.Tests project (xunit template, .NET 10.0)
- [X] T019 Add all projects to Maliev.CompensationService.sln

### Project Scaffolding - Performance Service

- [X] T020 [P] Clone Maliev.PerformanceService repository and create .NET solution file
- [X] T021 [P] Create Maliev.PerformanceService.Api project (webapi template, .NET 10.0)
- [X] T022 [P] Create Maliev.PerformanceService.Application project (classlib template, .NET 10.0)
- [X] T023 [P] Create Maliev.PerformanceService.Domain project (classlib template, .NET 10.0)
- [X] T024 [P] Create Maliev.PerformanceService.Infrastructure project (classlib template, .NET 10.0)
- [X] T025 [P] Create Maliev.PerformanceService.Tests project (xunit template, .NET 10.0)
- [X] T026 Add all projects to Maliev.PerformanceService.sln

### Project Scaffolding - Lifecycle Service

- [X] T027 [P] Clone Maliev.LifecycleService repository and create .NET solution file
- [X] T028 [P] Create Maliev.LifecycleService.Api project (webapi template, .NET 10.0)
- [X] T029 [P] Create Maliev.LifecycleService.Application project (classlib template, .NET 10.0)
- [X] T030 [P] Create Maliev.LifecycleService.Domain project (classlib template, .NET 10.0)
- [X] T031 [P] Create Maliev.LifecycleService.Infrastructure project (classlib template, .NET 10.0)
- [X] T032 [P] Create Maliev.LifecycleService.Tests project (xunit template, .NET 10.0)
- [X] T033 Add all projects to Maliev.LifecycleService.sln

### Project Scaffolding - Compliance Service

- [X] T034 [P] Clone Maliev.ComplianceService repository and create .NET solution file
- [X] T035 [P] Create Maliev.ComplianceService.Api project (webapi template, .NET 10.0)
- [X] T036 [P] Create Maliev.ComplianceService.Application project (classlib template, .NET 10.0)
- [X] T037 [P] Create Maliev.ComplianceService.Domain project (classlib template, .NET 10.0)
- [X] T038 [P] Create Maliev.ComplianceService.Infrastructure project (classlib template, .NET 10.0)
- [X] T039 [P] Create Maliev.ComplianceService.Tests project (xunit template, .NET 10.0)
- [X] T040 Add all projects to Maliev.ComplianceService.sln

### NuGet Configuration (All Services)

- [X] T041 [P] Create nuget.config in Maliev.LeaveService with GitHub Packages authentication
- [X] T042 [X] Create nuget.config in Maliev.CompensationService with GitHub Packages authentication
- [X] T043 [P] Create nuget.config in Maliev.PerformanceService with GitHub Packages authentication
- [X] T044 [P] Create nuget.config in Maliev.LifecycleService with GitHub Packages authentication
- [X] T045 [P] Create nuget.config in Maliev.ComplianceService with GitHub Packages authentication
- [X] T046 [P] Create nuget.config in Maliev.CareerService (if not exists) with GitHub Packages authentication

### Standard Files (All Services)

- [X] T047 [P] Create README.md in Maliev.LeaveService root
- [X] T048 [P] Create README.md in Maliev.CompensationService root
- [X] T049 [P] Create README.md in Maliev.PerformanceService root
- [X] T050 [P] Create README.md in Maliev.LifecycleService root
- [X] T051 [P] Create README.md in Maliev.ComplianceService root
- [X] T052 [P] Create .gitignore in all new service repositories (standard .NET gitignore)
- [X] T053 [P] Create .dockerignore in all new service repositories (exclude specs/, IDE files, build artifacts)

**Checkpoint**: All 5 new repositories created and scaffolded with standard project structure

---

## Phase 2: Foundational (Shared Infrastructure - BLOCKS ALL USER STORIES)

**Purpose**: Core infrastructure that MUST be complete before ANY user story implementation can begin

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### ServiceDefaults Integration

- [X] T054 [P] Add Maliev.Aspire.ServiceDefaults NuGet package to Maliev.LeaveService.Api
- [X] T055 [P] Add Maliev.Aspire.ServiceDefaults NuGet package to Maliev.CompensationService.Api
- [X] T056 [P] Add Maliev.Aspire.ServiceDefaults NuGet package to Maliev.PerformanceService.Api
- [X] T057 [P] Add Maliev.Aspire.ServiceDefaults NuGet package to Maliev.LifecycleService.Api
- [X] T058 [P] Add Maliev.Aspire.ServiceDefaults NuGet package to Maliev.ComplianceService.Api
- [X] T059 [P] Add Maliev.Aspire.ServiceDefaults NuGet package to Maliev.CareerService.Api

### Program.cs Configuration (All Services)

- [X] T060 [P] Configure Program.cs in Maliev.LeaveService.Api with AddServiceDefaults, AddPostgresDbContext, AddMassTransitWithRabbitMq, AddRedisDistributedCache, MapDefaultEndpoints
- [X] T061 [P] Configure Program.cs in Maliev.CompensationService.Api with AddServiceDefaults, AddPostgresDbContext, AddMassTransitWithRabbitMq, AddRedisDistributedCache, MapDefaultEndpoints
- [X] T062 [P] Configure Program.cs in Maliev.PerformanceService.Api with AddServiceDefaults, AddPostgresDbContext, AddMassTransitWithRabbitMq, AddRedisDistributedCache, MapDefaultEndpoints
- [X] T063 [P] Configure Program.cs in Maliev.LifecycleService.Api with AddServiceDefaults, AddPostgresDbContext, AddMassTransitWithRabbitMq, AddRedisDistributedCache, MapDefaultEndpoints
- [X] T064 [P] Configure Program.cs in Maliev.ComplianceService.Api with AddServiceDefaults, AddPostgresDbContext, AddMassTransitWithRabbitMq, AddRedisDistributedCache, MapDefaultEndpoints

### Integration Events (Shared Contracts)

- [X] T065 [P] Create EmployeeCreatedIntegrationEvent in Maliev.EmployeeService.Domain/IntegrationEvents with EmployeeId, EmployeeNumber, FullName, Email, HireDate, DepartmentId, ManagerId, JobTitle
- [X] T066 [P] Create EmployeeTerminatedIntegrationEvent in Maliev.EmployeeService.Domain/IntegrationEvents with EmployeeId, EmployeeNumber, TerminationDate
- [X] T067 [P] Create DepartmentTransferredIntegrationEvent in Maliev.EmployeeService.Domain/IntegrationEvents with EmployeeId, OldDepartmentId, NewDepartmentId, EffectiveDate

### Saga Infrastructure (Employee Service)

- [X] T068 Create saga_state table migration in Maliev.EmployeeService.Infrastructure/Migrations with correlation_id, saga_type, current_step, status, payload, created_at, updated_at
- [X] T069 Create saga_step_history table migration in Maliev.EmployeeService.Infrastructure/Migrations with id, correlation_id, step_name, step_type, status, executed_at, error_message
- [X] T070 Configure MassTransit saga persistence in Maliev.EmployeeService.Api Program.cs with EntityFrameworkRepository using EmployeeDbContext

### Dockerfiles (All Services)

- [X] T071 [P] Create Dockerfile in Maliev.LeaveService.Api with multi-stage build, BuildKit secrets, health checks, port 8080, app user
- [X] T072 [P] Create Dockerfile in Maliev.CompensationService.Api with multi-stage build, BuildKit secrets, health checks, port 8080, app user
- [X] T073 [P] Create Dockerfile in Maliev.PerformanceService.Api with multi-stage build, BuildKit secrets, health checks, port 8080, app user
- [X] T074 [P] Create Dockerfile in Maliev.LifecycleService.Api with multi-stage build, BuildKit secrets, health checks, port 8080, app user
- [X] T075 [P] Create Dockerfile in Maliev.ComplianceService.Api with multi-stage build, BuildKit secrets, health checks, port 8080, app user

### CI/CD Workflows (All Services)

- [X] T076 [P] Create .github/workflows/ci-develop.yml in Maliev.LeaveService with build, test, Testcontainers integration tests
- [X] T077 [P] Create .github/workflows/ci-staging.yml in Maliev.LeaveService with build, test, Docker image push
- [X] T078 [P] Create .github/workflows/ci-main.yml in Maliev.LeaveService with build, test, Docker image push, production deployment
- [X] T079 [P] Create .github/workflows/ci-develop.yml in Maliev.CompensationService
- [X] T080 [P] Create .github/workflows/ci-staging.yml in Maliev.CompensationService
- [X] T081 [P] Create .github/workflows/ci-main.yml in Maliev.CompensationService
- [X] T082 [P] Create .github/workflows/ci-develop.yml in Maliev.PerformanceService
- [X] T083 [P] Create .github/workflows/ci-staging.yml in Maliev.PerformanceService
- [X] T084 [P] Create .github/workflows/ci-main.yml in Maliev.PerformanceService
- [X] T085 [P] Create .github/workflows/ci-develop.yml in Maliev.LifecycleService
- [X] T086 [P] Create .github/workflows/ci-staging.yml in Maliev.LifecycleService
- [X] T087 [P] Create .github/workflows/ci-main.yml in Maliev.LifecycleService
- [X] T088 [P] Create .github/workflows/ci-develop.yml in Maliev.ComplianceService
- [X] T089 [P] Create .github/workflows/ci-staging.yml in Maliev.ComplianceService
- [X] T090 [P] Create .github/workflows/ci-main.yml in Maliev.ComplianceService

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Core Employee Management Service Created (Priority: P1) 🎯 MVP

**Goal**: Refactor the existing Employee Service to retain only core employee profile, department, team, and emergency contact functionality

**Independent Test**: Verify all core employee endpoints (GET /employee/v1/employees/{id}, POST /employee/v1/employees, GET /employee/v1/profile, etc.) function correctly with proper data persistence and retrieval

### Slim Down Employee Service (Remove Migrated Code)

- [X] T091 [P] [US1] Delete LeaveController.cs from Maliev.EmployeeService.Api/Controllers
- [X] T092 [P] [US1] Delete CompensationController.cs from Maliev.EmployeeService.Api/Controllers
- [X] T093 [P] [US1] Delete PerformanceController.cs from Maliev.EmployeeService.Api/Controllers
- [X] T094 [P] [US1] Delete OnboardingOffboardingController.cs from Maliev.EmployeeService.Api/Controllers
- [X] T095 [P] [US1] Delete WorkAuthorizationController.cs from Maliev.EmployeeService.Api/Controllers
- [X] T096 [P] [US1] Delete TrainingController.cs from Maliev.EmployeeService.Api/Controllers

### Remove Migrated Entities

- [X] T097 [P] [US1] Delete LeaveRequest.cs, LeaveBalance.cs, LeaveApproval.cs, LeavePolicy.cs from Maliev.EmployeeService.Domain/Entities
- [X] T098 [P] [US1] Delete CompensationRecord.cs, SalaryHistory.cs, Benefit.cs, BenefitsEnrollment.cs, EmployeeBenefit.cs, Dependent.cs from Maliev.EmployeeService.Domain/Entities
- [X] T099 [P] [US1] Delete PerformanceReview.cs, Goal.cs, PerformanceImprovementPlan.cs, DisciplinaryAction.cs from Maliev.EmployeeService.Domain/Entities
- [X] T100 [P] [US1] Delete OnboardingChecklist.cs, OffboardingChecklist.cs, OffboardingTask.cs, ExitInterview.cs from Maliev.EmployeeService.Domain/Entities
- [X] T101 [P] [US1] Delete WorkAuthorization.cs from Maliev.EmployeeService.Domain/Entities
- [X] T102 [P] [US1] Delete Training.cs, TrainingRecord.cs, MandatoryTrainingRequirement.cs, Certification.cs, Skill.cs from Maliev.EmployeeService.Domain/Entities

### Update Employee Service DbContext

- [X] T103 [US1] Remove DbSets for migrated entities from Maliev.EmployeeService.Infrastructure/EmployeeDbContext.cs (LeaveRequests, CompensationRecords, PerformanceReviews, OnboardingChecklists, WorkAuthorizations, TrainingRecords)
- [X] T104 [US1] Update EmployeeDbContext to retain only Employee, EmergencyContact, Department, Position, Team, EmployeeTeamAssignment, EmploymentHistory, PersonalDocument, AuditLog

### Retain Core Entities (Verify)

- [X] T105 [P] [US1] Verify Employee.cs in Maliev.EmployeeService.Domain/Entities has soft-delete fields (IsDeleted, DeletedAt, AnonymizedAt)
- [X] T106 [P] [US1] Verify EmergencyContact.cs in Maliev.EmployeeService.Domain/Entities
- [X] T107 [P] [US1] Verify Department.cs in Maliev.EmployeeService.Domain/Entities with hierarchy support
- [X] T108 [P] [US1] Verify Position.cs in Maliev.EmployeeService.Domain/Entities
- [X] T109 [P] [US1] Verify Team.cs in Maliev.EmployeeService.Domain/Entities
- [X] T110 [P] [US1] Verify EmployeeTeamAssignment.cs in Maliev.EmployeeService.Domain/Entities
- [X] T111 [P] [US1] Verify EmploymentHistory.cs in Maliev.EmployeeService.Domain/Entities
- [X] T112 [P] [US1] Verify PersonalDocument.cs in Maliev.EmployeeService.Domain/Entities
- [X] T113 [P] [US1] Verify AuditLog.cs in Maliev.EmployeeService.Domain/Entities

### Retain Core Controllers (Verify)

- [X] T114 [P] [US1] Verify EmployeesController.cs in Maliev.EmployeeService.Api/Controllers (GET by ID, by number, by principal; POST create; PUT update; DELETE soft-delete)
- [X] T115 [P] [US1] Verify EmployeeProfileController.cs in Maliev.EmployeeService.Api/Controllers (GET /profile, PUT /profile, GET /profile/export)
- [X] T116 [P] [US1] Verify DepartmentsController.cs in Maliev.EmployeeService.Api/Controllers (GET list, GET by ID, POST create, PUT update, DELETE)
- [X] T117 [P] [US1] Verify TeamsController.cs in Maliev.EmployeeService.Api/Controllers (GET list, GET by ID, GET members, POST create, PUT update, DELETE, POST/DELETE members)
- [X] T118 [P] [US1] Verify ManagersController.cs in Maliev.EmployeeService.Api/Controllers (GET direct reports, GET org chart)
- [X] T119 [P] [US1] Verify EmergencyContactController.cs in Maliev.EmployeeService.Api/Controllers (GET list, POST create, PUT update, DELETE)
- [X] T120 [P] [US1] Verify ReportsController.cs in Maliev.EmployeeService.Api/Controllers (GET org chart, headcount, span-of-control, turnover, diversity)
- [X] T121 [P] [US1] Verify BulkOperationsController.cs in Maliev.EmployeeService.Api/Controllers (POST import CSV, GET export CSV, GET job status)
- [X] T122 [P] [US1] Verify AdminController.cs in Maliev.EmployeeService.Api/Controllers (GET background jobs, POST run job)

### Update Employee Permissions

- [X] T123 [US1] Remove migrated permissions from Maliev.EmployeeService.Domain/Authorization/EmployeePermissions.cs (leave.*, compensation.*, performance.*, lifecycle.*, compliance.*, career.training.*)
- [X] T124 [US1] Retain core permissions in EmployeePermissions.cs (employee.profiles.*, employee.departments.*, employee.teams.*, employee.reports.*, employee.admin.*)

### Integration Event Publishing

- [X] T125 [US1] Implement EmployeeCreatedIntegrationEvent publishing in CreateEmployeeCommandHandler in Maliev.EmployeeService.Application/Commands/CreateEmployeeCommandHandler.cs
- [X] T126 [X] Implement EmployeeTerminatedIntegrationEvent publishing in TerminateEmployeeCommandHandler in Maliev.EmployeeService.Application/Commands/TerminateEmployeeCommandHandler.cs
- [X] T127 [X] Implement DepartmentTransferredIntegrationEvent publishing in TransferEmployeeDepartmentCommandHandler in Maliev.EmployeeService.Application/Commands/TransferEmployeeDepartmentCommandHandler.cs

### GDPR Compliance Background Service

- [X] T128 [US1] Create DataRetentionBackgroundService in Maliev.EmployeeService.Application/BackgroundServices to anonymize employees 7 years post-termination
- [X] T129 [US1] Register DataRetentionBackgroundService in Maliev.EmployeeService.Api Program.cs

### Database Migrations

- [X] T130 [US1] Create EF Core migration to remove tables for migrated entities in Maliev.EmployeeService.Infrastructure/Migrations
- [X] T131 [US1] Apply migration to slim down employee_db schema

**Checkpoint**: Employee Service is slimmed to ~25K LOC with only core employee management functionality

---

## Phase 4: User Story 2 - Leave Management Service Created (Priority: P2)

**Goal**: Create a dedicated Leave Service for managing leave requests, balances, policies, and approvals

**Independent Test**: Perform complete leave request lifecycle (submit → approve → track balance) through Leave Service endpoints (POST /leave/v1/requests, GET /leave/v1/balances, POST /leave/v1/approvals/{id}/approve)

### Domain Entities

- [X] T132 [P] [US2] Create LeaveRequest.cs in Maliev.LeaveService.Domain/Entities with EmployeeId, EmployeeNumber, LeaveTypeId, StartDate, EndDate, DaysRequested, Status, ApproverId, ReviewedAt, ReviewComments
- [X] T133 [P] [US2] Create LeaveBalance.cs in Maliev.LeaveService.Domain/Entities with EmployeeId, EmployeeNumber, LeaveTypeId, Year, Entitled, Used, Remaining, LastAccrualDate
- [X] T134 [P] [US2] Create LeaveType.cs in Maliev.LeaveService.Domain/Entities with Name, Code, AnnualEntitlement, RequiresApproval, MaxCarryOverDays, MaxConsecutiveDays
- [X] T135 [P] [US2] Create LeaveRequestStatus enum in Maliev.LeaveService.Domain/Entities (Pending, Approved, Rejected, Cancelled)

### Infrastructure - DbContext

- [X] T136 [US2] Create LeaveDbContext.cs in Maliev.LeaveService.Infrastructure with DbSets for LeaveRequest, LeaveBalance, LeaveType
- [X] T137 [US2] Configure entity relationships and indexes in LeaveDbContext.OnModelCreating (unique index on employee_id + leave_type_id + year for balances)
- [X] T138 [US2] Configure global query filters in LeaveDbContext (if needed)
- [X] T139 [US2] Create initial EF Core migration for leave_db in Maliev.LeaveService.Infrastructure/Migrations

### Application Layer - Commands

- [X] T140 [P] [US2] Create SubmitLeaveRequestCommand and handler in Maliev.LeaveService.Application/Commands with balance validation
- [X] T141 [P] [US2] Create ApproveLeaveRequestCommand and handler in Maliev.LeaveService.Application/Commands
- [X] T142 [P] [US2] Create RejectLeaveRequestCommand and handler in Maliev.LeaveService.Application/Commands
- [X] T143 [P] [US2] Create CancelLeaveRequestCommand and handler in Maliev.LeaveService.Application/Commands

### Application Layer - Queries

- [X] T144 [P] [US2] Create GetLeaveRequestsQuery and handler in Maliev.LeaveService.Application/Queries (filtered by employee)
- [X] T145 [P] [US2] Create GetLeaveBalancesQuery and handler in Maliev.LeaveService.Application/Queries
- [X] T146 [P] [US2] Create GetPendingApprovalsQuery and handler in Maliev.LeaveService.Application/Queries (for managers)
- [X] T147 [P] [US2] Create GetLeaveTypesQuery and handler in Maliev.LeaveService.Application/Queries

### API Controllers

- [X] T148 [US2] Create LeaveController.cs in Maliev.LeaveService.Api/Controllers with endpoints for POST /requests, GET /requests, GET /requests/{id}, PUT /requests/{id}/cancel
- [X] T149 [US2] Add approval endpoints to LeaveController.cs (GET /approvals/pending, POST /approvals/{id}/approve, POST /approvals/{id}/reject)
- [X] T150 [US2] Add balance endpoints to LeaveController.cs (GET /balances, GET /balances/{employeeId})
- [X] T151 [US2] Add leave type endpoints to LeaveController.cs (GET /leave-types, GET /leave-types/{id})

### Permissions

- [X] T152 [US2] Create LeavePermissions.cs in Maliev.LeaveService.Domain/Authorization with leave.create, leave.read, leave.cancel, leave.approve, leave.reports.view
- [X] T153 [US2] Apply permission authorization attributes to LeaveController endpoints

### Integration Event Consumers

- [X] T154 [US2] Create EmployeeCreatedEventConsumer.cs in Maliev.LeaveService.Infrastructure/Consumers to create leave balances for new employees
- [X] T155 [US2] Create EmployeeTerminatedEventConsumer.cs in Maliev.LeaveService.Infrastructure/Consumers to handle employee termination (mark leave requests as cancelled)
- [X] T156 [US2] Register consumers in Maliev.LeaveService.Api Program.cs with MassTransit

### Background Services

- [X] T157 [US2] Create LeaveAccrualBackgroundService.cs in Maliev.LeaveService.Application/BackgroundServices to accrue leave balances monthly
- [X] T158 [US2] Create LeaveExpirationAlertBackgroundService.cs in Maliev.LeaveService.Application/BackgroundServices to send alerts for expiring leave
- [X] T159 [US2] Register background services in Maliev.LeaveService.Api Program.cs

### Reporting

- [X] T160 [US2] Create ReportsController.cs in Maliev.LeaveService.Api/Controllers with GET /reports/utilization, GET /reports/balances
- [X] T161 [US2] Implement leave utilization report query in Maliev.LeaveService.Application/Queries

**Checkpoint**: Leave Service is fully functional and can be deployed independently

---

## Phase 5: User Story 3 - Compensation Service Created (Priority: P2)

**Goal**: Create a dedicated Compensation Service for managing employee compensation, salary history, and benefits enrollment

**Independent Test**: Record compensation changes, view salary history, and manage benefits enrollment through Compensation Service endpoints (POST /compensation/v1/employees/{id}/compensation, GET /compensation/v1/employees/{id}/compensation/history, POST /compensation/v1/employees/{id}/benefits/enroll) with full audit trail verification

### Domain Entities

- [X] T162 [P] [US3] Create CompensationRecord.cs in Maliev.CompensationService.Domain/Entities with EmployeeId, EmployeeNumber, BaseSalary, Currency, EffectiveDate, EndDate, ChangeReason, ApprovedBy
- [X] T163 [P] [US3] Create BenefitPlan.cs in Maliev.CompensationService.Domain/Entities with Name, Description, Type, CoverageDetails
- [X] T164 [P] [US3] Create BenefitsEnrollment.cs in Maliev.CompensationService.Domain/Entities with EmployeeId, EmployeeNumber, BenefitPlanId, EnrollmentDate, CoverageStartDate, CoverageEndDate, EmployeePremium, EmployerPremium
- [X] T165 [P] [US3] Create Dependent.cs in Maliev.CompensationService.Domain/Entities with BenefitsEnrollmentId, FullName, Relationship, DateOfBirth, IdentificationNumber

### Infrastructure - DbContext

- [X] T166 [US3] Create CompensationDbContext.cs in Maliev.CompensationService.Infrastructure with DbSets for CompensationRecord, BenefitPlan, BenefitsEnrollment, Dependent
- [X] T167 [US3] Configure entity relationships and indexes in CompensationDbContext.OnModelCreating
- [X] T168 [US3] Create initial EF Core migration for compensation_db in Maliev.CompensationService.Infrastructure/Migrations

### Application Layer - Commands

- [X] T169 [P] [US3] Create RecordCompensationChangeCommand and handler in Maliev.CompensationService.Application/Commands with audit logging
- [X] T170 [P] [US3] Create EnrollInBenefitsCommand and handler in Maliev.CompensationService.Application/Commands
- [X] T171 [P] [US3] Create UpdateBenefitsEnrollmentCommand and handler in Maliev.CompensationService.Application/Commands
- [X] T172 [P] [US3] Create AddDependentCommand and handler in Maliev.CompensationService.Application/Commands
- [X] T173 [P] [US3] Create BulkSalaryIncreaseCommand and handler in Maliev.CompensationService.Application/Commands

### Application Layer - Queries

- [X] T174 [P] [US3] Create GetCurrentCompensationQuery and handler in Maliev.CompensationService.Application/Queries
- [X] T175 [P] [US3] Create GetCompensationHistoryQuery and handler in Maliev.CompensationService.Application/Queries
- [X] T176 [P] [US3] Create GetBenefitsEnrollmentQuery and handler in Maliev.CompensationService.Application/Queries
- [X] T177 [P] [US3] Create GetCompensationAnalysisQuery and handler in Maliev.CompensationService.Application/Queries (for reporting)

### API Controllers

- [X] T178 [US3] Create CompensationController.cs in Maliev.CompensationService.Api/Controllers with endpoints for GET /employees/{id}/compensation, GET /employees/{id}/compensation/history, POST /employees/{id}/compensation
- [X] T179 [US3] Add benefits endpoints to CompensationController.cs (GET /employees/{id}/benefits, POST /employees/{id}/benefits/enroll, PUT /employees/{id}/benefits/{enrollmentId}, POST /employees/{id}/benefits/{enrollmentId}/dependents)
- [X] T180 [US3] Create ReportsController.cs in Maliev.CompensationService.Api/Controllers with GET /reports/compensation-analysis
- [X] T181 [US3] Add bulk operations endpoint (POST /bulk/salary-increase)

### Permissions

- [X] T182 [US3] Create CompensationPermissions.cs in Maliev.CompensationService.Domain/Authorization with compensation.read, compensation.update, compensation.reports.view, compensation.admin
- [X] T183 [US3] Apply permission authorization attributes to CompensationController endpoints

### Integration Event Consumers

- [X] T184 [US3] Create EmployeeCreatedEventConsumer.cs in Maliev.CompensationService.Infrastructure/Consumers to initialize compensation record for new employees
- [X] T185 [US3] Create EmployeeTerminatedEventConsumer.cs in Maliev.CompensationService.Infrastructure/Consumers to archive compensation records
- [X] T186 [US3] Register consumers in Maliev.CompensationService.Api Program.cs with MassTransit

**Checkpoint**: Compensation Service is fully functional with strong security and audit controls

---

## Phase 6: User Story 4 - Performance Service Created (Priority: P3)

**Goal**: Create a dedicated Performance Service for conducting performance reviews, setting goals, and tracking employee development

**Independent Test**: Create performance reviews, set goals, track progress, and acknowledge reviews through Performance Service endpoints (POST /performance/v1/reviews, POST /performance/v1/goals, PUT /performance/v1/goals/{id}/progress, PUT /performance/v1/reviews/{id}/acknowledge)

### Domain Entities

- [X] T187 [P] [US4] Create PerformanceReview.cs in Maliev.PerformanceService.Domain/Entities with EmployeeId, EmployeeNumber, ReviewerId, ReviewPeriodStart, ReviewPeriodEnd, OverallRating, Strengths, AreasForImprovement, Goals, AcknowledgedByEmployee, AcknowledgedAt
- [X] T188 [P] [US4] Create Goal.cs in Maliev.PerformanceService.Domain/Entities with EmployeeId, EmployeeNumber, Title, Description, TargetDate, Status, ProgressPercentage
- [X] T189 [P] [US4] Create PerformanceImprovementPlan.cs in Maliev.PerformanceService.Domain/Entities
- [X] T190 [P] [US4] Create DisciplinaryAction.cs in Maliev.PerformanceService.Domain/Entities
- [X] T191 [P] [US4] Create GoalStatus enum in Maliev.PerformanceService.Domain/Entities (NotStarted, InProgress, Completed, Cancelled)

### Infrastructure - DbContext

- [X] T192 [US4] Create PerformanceDbContext.cs in Maliev.PerformanceService.Infrastructure with DbSets for PerformanceReview, Goal, PerformanceImprovementPlan, DisciplinaryAction
- [X] T193 [US4] Configure entity relationships and indexes in PerformanceDbContext.OnModelCreating
- [X] T194 [US4] Create initial EF Core migration for performance_db in Maliev.PerformanceService.Infrastructure/Migrations

### Application Layer - Commands

- [X] T195 [P] [US4] Create CreatePerformanceReviewCommand and handler in Maliev.PerformanceService.Application/Commands
- [X] T196 [P] [US4] Create AcknowledgeReviewCommand and handler in Maliev.PerformanceService.Application/Commands
- [X] T197 [P] [US4] Create CreateGoalCommand and handler in Maliev.PerformanceService.Application/Commands
- [X] T198 [P] [US4] Create UpdateGoalProgressCommand and handler in Maliev.PerformanceService.Application/Commands

### Application Layer - Queries

- [X] T199 [P] [US4] Create GetPerformanceReviewsQuery and handler in Maliev.PerformanceService.Application/Queries
- [X] T200 [P] [US4] Create GetGoalsQuery and handler in Maliev.PerformanceService.Application/Queries

### API Controllers

- [X] T201 [US4] Create PerformanceController.cs in Maliev.PerformanceService.Api/Controllers with endpoints for GET /reviews, GET /reviews/{id}, POST /reviews, PUT /reviews/{id}/acknowledge
- [X] T202 [US4] Add goal endpoints to PerformanceController.cs (GET /goals, GET /goals/{id}, POST /goals, PUT /goals/{id}, PUT /goals/{id}/progress)

### Permissions

- [X] T203 [US4] Create PerformancePermissions.cs in Maliev.PerformanceService.Domain/Authorization with performance.create, performance.read, performance.acknowledge
- [X] T204 [US4] Apply permission authorization attributes to PerformanceController endpoints

### Integration Event Consumers

- [X] T205 [US4] Create EmployeeCreatedEventConsumer.cs in Maliev.PerformanceService.Infrastructure/Consumers
- [X] T206 [US4] Create EmployeeTerminatedEventConsumer.cs in Maliev.PerformanceService.Infrastructure/Consumers
- [X] T207 [US4] Register consumers in Maliev.PerformanceService.Api Program.cs with MassTransit

### Background Services

- [X] T208 [US4] Create PerformanceReviewReminderBackgroundService.cs in Maliev.PerformanceService.Application/BackgroundServices to send review reminders
- [X] T209 [US4] Register background service in Maliev.PerformanceService.Api Program.cs

**Checkpoint**: Performance Service is fully functional for managing reviews and goals

---

## Phase 7: User Story 5 - Lifecycle Service Created (Priority: P3)

**Goal**: Create a dedicated Lifecycle Service for coordinating employee onboarding and offboarding processes with checklists, tasks, and exit procedures

**Independent Test**: Initiate onboarding for new hires and offboarding for departing employees, verifying all checklist items and access revocation workflows complete successfully (POST /lifecycle/v1/onboarding/{employeeId}/start, POST /lifecycle/v1/offboarding/{employeeId}/start, PUT /lifecycle/v1/onboarding/{employeeId}/tasks/{taskId}/complete)

### Domain Entities

- [X] T210 [P] [US5] Create OnboardingChecklist.cs in Maliev.LifecycleService.Domain/Entities with EmployeeId, EmployeeNumber, StartDate, CompletionDate, Status, Tasks collection
- [X] T211 [P] [US5] Create OnboardingTask.cs in Maliev.LifecycleService.Domain/Entities with OnboardingChecklistId, Title, AssignedTo, DueDate, CompletedAt, Status
- [X] T212 [P] [US5] Create OffboardingChecklist.cs in Maliev.LifecycleService.Domain/Entities with EmployeeId, EmployeeNumber, InitiatedDate, CompletionDate, Status, Tasks collection
- [X] T213 [P] [US5] Create OffboardingTask.cs in Maliev.LifecycleService.Domain/Entities
- [X] T214 [P] [US5] Create ExitInterview.cs in Maliev.LifecycleService.Domain/Entities
- [X] T215 [P] [US5] Create OnboardingStatus and OffboardingStatus enums in Maliev.LifecycleService.Domain/Entities (NotStarted, InProgress, Completed)

### Infrastructure - DbContext

- [X] T216 [US5] Create LifecycleDbContext.cs in Maliev.LifecycleService.Infrastructure with DbSets for OnboardingChecklist, OnboardingTask, OffboardingChecklist, OffboardingTask, ExitInterview
- [X] T217 [US5] Configure entity relationships and indexes in LifecycleDbContext.OnModelCreating
- [X] T218 [US5] Create initial EF Core migration for lifecycle_db in Maliev.LifecycleService.Infrastructure/Migrations

### Application Layer - Commands

- [X] T219 [P] [US5] Create StartOnboardingCommand and handler in Maliev.LifecycleService.Application/Commands with template-based checklist generation
- [X] T220 [P] [US5] Create CompleteOnboardingTaskCommand and handler in Maliev.LifecycleService.Application/Commands
- [X] T221 [P] [US5] Create StartOffboardingCommand and handler in Maliev.LifecycleService.Application/Commands
- [X] T222 [P] [US5] Create CompleteOffboardingTaskCommand and handler in Maliev.LifecycleService.Application/Commands

### Application Layer - Queries

- [X] T223 [P] [US5] Create GetOnboardingStatusQuery and handler in Maliev.LifecycleService.Application/Queries
- [X] T224 [P] [US5] Create GetOffboardingStatusQuery and handler in Maliev.LifecycleService.Application/Queries

### API Controllers

- [X] T225 [US5] Create OnboardingOffboardingController.cs in Maliev.LifecycleService.Api/Controllers with endpoints for GET /onboarding/{employeeId}, POST /onboarding/{employeeId}/start, PUT /onboarding/{employeeId}/tasks/{taskId}/complete
- [X] T226 [US5] Add offboarding endpoints to controller (GET /offboarding/{employeeId}, POST /offboarding/{employeeId}/start, PUT /offboarding/{employeeId}/tasks/{taskId}/complete)

### Permissions

- [X] T227 [US5] Create LifecyclePermissions.cs in Maliev.LifecycleService.Domain/Authorization with lifecycle.onboarding.manage, lifecycle.offboarding.manage
- [X] T228 [US5] Apply permission authorization attributes to OnboardingOffboardingController endpoints

### Integration Events

- [X] T229 [P] [US5] Create EmployeeOnboardingStartedIntegrationEvent in Maliev.LifecycleService.Domain/IntegrationEvents
- [X] T230 [P] [US5] Create OnboardingReminderNeededIntegrationEvent in Maliev.LifecycleService.Domain/IntegrationEvents
- [X] T231 [P] [US5] Create AccessRevocationRequiredIntegrationEvent in Maliev.LifecycleService.Domain/IntegrationEvents

### Integration Event Consumers

- [X] T232 [US5] Create EmployeeCreatedEventConsumer.cs in Maliev.LifecycleService.Infrastructure/Consumers to optionally auto-start onboarding
- [X] T233 [US5] Create EmployeeTerminatedEventConsumer.cs in Maliev.LifecycleService.Infrastructure/Consumers to auto-start offboarding
- [X] T234 [US5] Register consumers in Maliev.LifecycleService.Api Program.cs with MassTransit

### Background Services

- [X] T235 [US5] Create OnboardingReminderBackgroundService.cs in Maliev.LifecycleService.Application/BackgroundServices to send reminders for overdue tasks
- [X] T236 [US5] Create AccessRevocationBackgroundService.cs in Maliev.LifecycleService.Application/BackgroundServices to publish access revocation events
- [X] T237 [US5] Register background services in Maliev.LifecycleService.Api Program.cs

**Checkpoint**: Lifecycle Service is fully functional for managing onboarding and offboarding

---

## Phase 8: User Story 6 - Compliance Service Created (Priority: P3)

**Goal**: Create a dedicated Compliance Service for tracking employee work authorization documentation and expiration dates with automated expiration alerts

**Independent Test**: Record work authorization documents, track expiration dates, and verify expiration reminder notifications are sent appropriately (POST /compliance/v1/work-authorization, GET /compliance/v1/work-authorization/{employeeId}, GET /compliance/v1/reports/expiring-authorizations)

### Domain Entities

- [X] T238 [P] [US6] Create WorkAuthorization.cs in Maliev.ComplianceService.Domain/Entities with EmployeeId, EmployeeNumber, AuthorizationType, DocumentNumber, IssueDate, ExpiryDate, Status
- [X] T239 [P] [US6] Create WorkAuthorizationStatus enum in Maliev.ComplianceService.Domain/Entities (Valid, Expiring, Expired)

### Infrastructure - DbContext

- [X] T240 [US6] Create ComplianceDbContext.cs in Maliev.ComplianceService.Infrastructure with DbSet for WorkAuthorization
- [X] T241 [US6] Configure entity relationships and indexes in ComplianceDbContext.OnModelCreating (unique index on document_number)
- [X] T242 [US6] Create initial EF Core migration for compliance_db in Maliev.ComplianceService.Infrastructure/Migrations

### Application Layer - Commands

- [X] T243 [P] [US6] Create RecordWorkAuthorizationCommand and handler in Maliev.ComplianceService.Application/Commands
- [X] T244 [P] [US6] Create UpdateWorkAuthorizationCommand and handler in Maliev.ComplianceService.Application/Commands

### Application Layer - Queries

- [X] T245 [P] [US6] Create GetWorkAuthorizationQuery and handler in Maliev.ComplianceService.Application/Queries
- [X] T246 [P] [US6] Create GetExpiringAuthorizationsQuery and handler in Maliev.ComplianceService.Application/Queries
- [X] T247 [P] [US6] Create GetComplianceReportQuery and handler in Maliev.ComplianceService.Application/Queries

### API Controllers

- [X] T248 [US6] Create WorkAuthorizationController.cs in Maliev.ComplianceService.Api/Controllers with endpoints for GET /work-authorization/{employeeId}, POST /work-authorization, PUT /work-authorization/{id}
- [X] T249 [US6] Create ReportsController.cs in Maliev.ComplianceService.Api/Controllers with GET /reports/work-authorization-compliance, GET /reports/expiring-authorizations

### Permissions

- [X] T250 [US6] Create CompliancePermissions.cs in Maliev.ComplianceService.Domain/Authorization with compliance.workauth.manage, compliance.reports.view
- [X] T251 [US6] Apply permission authorization attributes to WorkAuthorizationController endpoints

### Integration Event Consumers

- [X] T252 [X] Create EmployeeCreatedEventConsumer.cs in Maliev.ComplianceService.Infrastructure/Consumers
- [X] T253 [X] Create EmployeeTerminatedEventConsumer.cs in Maliev.ComplianceService.Infrastructure/Consumers
- [X] T254 [X] Register consumers in Maliev.ComplianceService.Api Program.cs with MassTransit

### Background Services

- [X] T255 [X] Create WorkAuthorizationExpirationReminderService.cs in Maliev.ComplianceService.Application/BackgroundServices to send reminders 90/60/30 days before expiry
- [X] T256 [X] Create ExpiredWorkAuthorizationFlaggingService.cs in Maliev.ComplianceService.Application/BackgroundServices to update status to Expired
- [X] T257 [X] Register background services in Maliev.ComplianceService.Api Program.cs

**Checkpoint**: Compliance Service is fully functional for tracking work authorization

---

## Phase 9: User Story 7 - Career Service Extended with Training Features (Priority: P3)

**Goal**: Extend the existing Career Service to track training completion, certifications, and skill profiles

**Independent Test**: Record training completions, track certifications, manage skills, and generate training compliance reports through Career Service endpoints (POST /career/v1/training/records, POST /career/v1/certifications, POST /career/v1/skills, GET /career/v1/reports/training-compliance)

### Domain Entities (Add to Existing Career Service)

- [X] T258 [P] [US7] Create TrainingProgram.cs in Maliev.CareerService.Data/Models with Name, Code, Description, DurationHours, IsMandatory, ValidityMonths
- [X] T259 [P] [US7] Create TrainingRecord.cs in Maliev.CareerService.Data/Models with EmployeeId, EmployeeNumber, TrainingProgramId, CompletionDate, Score, PassedTraining, ExpirationDate
- [X] T260 [P] [US7] Create MandatoryTrainingRequirement.cs in Maliev.CareerService.Data/Models
- [X] T261 [P] [US7] Create Certification.cs in Maliev.CareerService.Data/Models (Note: Integrated into TrainingRecord)
- [X] T262 [P] [US7] Create Skill.cs in Maliev.CareerService.Data/Models with EmployeeId, EmployeeNumber, SkillName, Category, ProficiencyLevel, AcquiredDate

### Infrastructure - Extend DbContext

- [X] T263 [US7] Add DbSets for TrainingProgram, TrainingRecord, MandatoryTrainingRequirement, Skill to Maliev.CareerService.Data/CareerDbContext.cs
- [X] T264 [US7] Configure entity relationships and indexes in CareerDbContext.OnModelCreating for new entities
- [X] T265 [US7] Create EF Core migration to add training tables to career_db in Maliev.CareerService.Data/Migrations

### Application Layer - Commands (Services in CareerService)

- [X] T266 [P] [US7] Create RecordTrainingCompletion logic in ITrainingRecordService
- [X] T267 [P] [US7] Create AssignMandatoryTraining logic in IMandatoryTrainingService
- [X] T268 [P] [US7] Create AddCertification logic (Note: Integrated into TrainingRecordService)
- [X] T269 [P] [US7] Create UpdateCertification logic (Note: Integrated into TrainingRecordService)
- [X] T270 [P] [US7] Create AddSkill logic in IEmployeeSkillService
- [X] T271 [P] [US7] Create UpdateSkill logic in IEmployeeSkillService

### Application Layer - Queries

- [X] T272 [P] [US7] Create GetTrainingRecordsQuery logic in ITrainingRecordService
- [X] T273 [P] [US7] Create GetCertificationsQuery logic (Note: Integrated into TrainingRecordService)
- [X] T274 [P] [US7] Create GetSkillsQuery logic in IEmployeeSkillService
- [X] T275 [P] [US7] Create GetTrainingComplianceReportQuery logic

### API Controllers

- [X] T276 [US7] Create TrainingRecordsController.cs in Maliev.CareerService.Api/Controllers with endpoints for GET /training-records, POST /training-records
- [X] T277 [US7] Add certification endpoints (Note: Integrated into TrainingRecordsController)
- [X] T278 [US7] Add skill endpoints in SkillsController.cs
- [X] T279 [US7] Extend ReportsController.cs in Maliev.CareerService.Api/Controllers with GET /reports/training-compliance

### Permissions

- [X] T280 [US7] Extend CareerPermissions.cs in Maliev.CareerService.Api/Authentication
- [X] T281 [US7] Apply permission authorization attributes to TrainingControllers

### Integration Event Consumers

- [X] T282 [US7] Create EmployeeCreatedEventConsumer.cs in Maliev.CareerService.Api/Consumers
- [X] T283 [US7] Create EmployeeTerminatedEventConsumer.cs in Maliev.CareerService.Api/Consumers
- [X] T284 [US7] Register consumers in Maliev.CareerService.Api Program.cs with MassTransit

### Background Services

- [X] T285 [US7] Create OverdueTrainingEscalationBackgroundService.cs (Implemented as reminder/compliance checks)
- [X] T286 [US7] Create CertificationExpirationReminderBackgroundService.cs in Maliev.CareerService.Api/BackgroundServices
- [X] T287 [US7] Register background services in Maliev.CareerService.Api Program.cs

**Checkpoint**: Career Service is extended with training, certification, and skills management

---

## Phase 10: Saga Pattern Implementation (Cross-Service Orchestration)

**Goal**: Implement saga pattern for distributed transactions requiring cross-service coordination

### Employee Termination Saga (Primary Use Case)

- [X] T288 [US1] Create EmployeeTerminationSagaState.cs in Maliev.EmployeeService.Domain/Sagas with CorrelationId, CurrentState, EmployeeId, TerminationDate, LeaveBalanceClosed, CompensationArchived, AccessRevoked
- [X] T289 [US1] Create EmployeeTerminationSaga state machine in Maliev.EmployeeService.Application/Sagas implementing ISaga
- [X] T290 [US1] Implement saga steps: InitiatedBy<EmployeeTerminatedIntegrationEvent>, Orchestrates<LeaveBalanceClosedEvent>, Orchestrates<CompensationArchivedEvent>, Orchestrates<AccessRevokedEvent>

### Saga Commands and Events

- [X] T291 [P] [US1] Create CloseLeaveBalanceCommand in Maliev.LeaveService.Domain/Commands
- [X] T292 [P] [US1] Create LeaveBalanceClosedEvent in Maliev.LeaveService.Domain/IntegrationEvents
- [X] T293 [P] [US1] Create ArchiveCompensationCommand in Maliev.CompensationService.Domain/Commands
- [X] T294 [P] [US1] Create CompensationArchivedEvent in Maliev.CompensationService.Domain/IntegrationEvents
- [X] T295 [P] [US1] Create RevokeAccessCommand in Maliev.LifecycleService.Domain/Commands
- [X] T296 [P] [US1] Create AccessRevokedEvent in Maliev.LifecycleService.Domain/IntegrationEvents

### Saga Command Handlers

- [X] T297 [US2] Implement CloseLeaveBalanceCommandHandler in Maliev.LeaveService.Application/Commands to close all leave balances and publish LeaveBalanceClosedEvent
- [X] T298 [US3] Implement ArchiveCompensationCommandHandler in Maliev.CompensationService.Application/Commands to archive compensation and publish CompensationArchivedEvent
- [X] T299 [US5] Implement RevokeAccessCommandHandler in Maliev.LifecycleService.Application/Commands to trigger access revocation and publish AccessRevokedEvent

### Compensating Transactions

- [X] T300 [US1] Implement compensating transaction for CloseLeaveBalance in EmployeeTerminationSaga (restore leave balances if saga fails)
- [X] T301 [US1] Implement compensating transaction for ArchiveCompensation in EmployeeTerminationSaga
- [X] T302 [US1] Implement compensating transaction for RevokeAccess in EmployeeTerminationSaga

### Saga Registration and Testing

- [X] T303 [US1] Register EmployeeTerminationSaga with MassTransit in Maliev.EmployeeService.Api Program.cs with EntityFrameworkRepository
- [X] T304 [US1] Create saga recovery service in Maliev.EmployeeService.Application/BackgroundServices to resume in-progress sagas on orchestrator restart

**Checkpoint**: Saga pattern implemented for employee termination with full rollback capability

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple services

### Documentation

- [X] T305 [P] Update README.md in Maliev.EmployeeService with slimmed functionality and API endpoints
- [X] T306 [P] Complete README.md in Maliev.LeaveService with getting started instructions
- [X] T307 [P] Complete README.md in Maliev.CompensationService with getting started instructions
- [X] T308 [P] Complete README.md in Maliev.PerformanceService with getting started instructions
- [X] T309 [P] Complete README.md in Maliev.LifecycleService with getting started instructions
- [X] T310 [P] Complete README.md in Maliev.ComplianceService with getting started instructions
- [X] T311 [P] Update README.md in Maliev.CareerService with training features

### OpenAPI/Scalar UI Verification

- [X] T312 [P] Verify Scalar UI documentation at /employee/scalar for Employee Service
- [X] T313 [P] Verify Scalar UI documentation at /leave/scalar for Leave Service
- [X] T314 [P] Verify Scalar UI documentation at /compensation/scalar for Compensation Service
- [X] T315 [P] Verify Scalar UI documentation at /performance/scalar for Performance Service
- [X] T316 [P] Verify Scalar UI documentation at /lifecycle/scalar for Lifecycle Service
- [X] T317 [P] Verify Scalar UI documentation at /compliance/scalar for Compliance Service
- [X] T318 [P] Verify Scalar UI documentation at /career/scalar for Career Service

### Health Checks Verification

- [X] T319 [P] Verify health check endpoints (/employee/liveness, /employee/readiness, /employee/health) for Employee Service
- [X] T320 [P] Verify health check endpoints for all 6 new services (Leave, Compensation, Performance, Lifecycle, Compliance) and extended Career Service

### Code Quality

- [X] T321 Run code quality checks across all services (dotnet format, code analysis)
- [X] T322 Verify TreatWarningsAsErrors is enabled in all project files
- [X] T323 Ensure no AutoMapper, FluentValidation, or FluentAssertions references exist in any service

### Security Hardening

- [X] T324 [P] Configure TLS for PostgreSQL connections in all services (SSL Mode=Require in connection strings)
- [X] T325 [P] Configure TLS for RabbitMQ connections in all services via MassTransit
- [X] T326 Verify all secrets are stored in Google Secret Manager (database passwords, RabbitMQ credentials)
- [X] T327 Verify all sensitive endpoints have proper permission authorization

### Performance Validation

- [X] T328 Load test Employee Service endpoints to verify <200ms p95 response time
- [X] T329 Load test Leave Service endpoints to verify <200ms p95 response time
- [X] T330 Verify all services can handle 1000 concurrent requests

### Observability Validation

- [X] T331 Verify correlation IDs are propagated across all service boundaries in distributed traces
- [X] T332 Verify structured logging is consistent across all services
- [X] T333 Test saga state persistence and recovery after orchestrator restart

### Final Validation

- [X] T334 Verify Employee Service LOC reduced from ~82K to ~25K (70% reduction)
- [X] T335 Verify Employee Service file count reduced from 459 to 150-180 files
- [X] T336 Verify all 7 services are independently deployable
- [X] T337 Run quickstart.md validation across all services
- [X] T338 Verify zero cross-service database joins exist
- [X] T339 Verify all integration events are published and consumed correctly
- [X] T340 Verify all background services run successfully

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-9)**: All depend on Foundational phase completion
  - User stories can proceed in parallel (if staffed)
  - Or sequentially in priority order: US1 (P1) → US2, US3 (P2) → US4, US5, US6, US7 (P3)
- **Saga Implementation (Phase 10)**: Depends on US1 (Employee), US2 (Leave), US3 (Compensation), US5 (Lifecycle) completion
- **Polish (Phase 11)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Independent of other stories
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Independent of other stories
- **User Story 4 (P3)**: Can start after Foundational (Phase 2) - Independent of other stories
- **User Story 5 (P3)**: Can start after Foundational (Phase 2) - Independent of other stories
- **User Story 6 (P3)**: Can start after Foundational (Phase 2) - Independent of other stories
- **User Story 7 (P3)**: Can start after Foundational (Phase 2) - Independent of other stories

### Saga Dependencies

- **Employee Termination Saga**: Requires US1 (Employee Service), US2 (Leave Service), US3 (Compensation Service), US5 (Lifecycle Service) to be complete

### Within Each User Story

- Domain entities before DbContext
- DbContext before application layer
- Commands/Queries before controllers
- Controllers before permissions
- Integration event consumers after event definitions
- Background services last within each story

### Parallel Opportunities

- All Setup tasks (T001-T053) can run in parallel within their subsections
- All Foundational tasks (T054-T090) can run in parallel within their subsections
- Once Foundational completes, all user stories (US1-US7) can start in parallel if team capacity allows
- All domain entities within a story marked [P] can run in parallel
- All commands within a story marked [P] can run in parallel
- All queries within a story marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 2 (Leave Service)

```bash
# Launch all domain entities together:
Task T132: "Create LeaveRequest.cs in Maliev.LeaveService.Domain/Entities"
Task T133: "Create LeaveBalance.cs in Maliev.LeaveService.Domain/Entities"
Task T134: "Create LeaveType.cs in Maliev.LeaveService.Domain/Entities"
Task T135: "Create LeaveRequestStatus enum in Maliev.LeaveService.Domain/Entities"

# Then create DbContext (depends on entities):
Task T136: "Create LeaveDbContext.cs"

# Launch all commands together:
Task T140: "Create SubmitLeaveRequestCommand and handler"
Task T141: "Create ApproveLeaveRequestCommand and handler"
Task T142: "Create RejectLeaveRequestCommand and handler"
Task T143: "Create CancelLeaveRequestCommand and handler"

# Launch all queries together:
Task T144: "Create GetLeaveRequestsQuery and handler"
Task T145: "Create GetLeaveBalancesQuery and handler"
Task T146: "Create GetPendingApprovalsQuery and handler"
Task T147: "Create GetLeaveTypesQuery and handler"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Slim Employee Service)
4. **STOP and VALIDATE**: Test Employee Service independently
5. Deploy/demo if ready

### Incremental Delivery by Priority

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 (P1) → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 (P2) → Test independently → Deploy/Demo
4. Add User Story 3 (P2) → Test independently → Deploy/Demo
5. Add User Story 4 (P3) → Test independently → Deploy/Demo
6. Add User Story 5 (P3) → Test independently → Deploy/Demo
7. Add User Story 6 (P3) → Test independently → Deploy/Demo
8. Add User Story 7 (P3) → Test independently → Deploy/Demo
9. Implement Saga Pattern (Phase 10) → Test distributed transactions
10. Complete Polish (Phase 11)

Each story adds value without breaking previous stories.

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Employee Service - P1)
   - Developer B: User Story 2 (Leave Service - P2)
   - Developer C: User Story 3 (Compensation Service - P2)
   - Developer D: User Story 4 (Performance Service - P3)
   - Developer E: User Story 5 (Lifecycle Service - P3)
   - Developer F: User Story 6 (Compliance Service - P3)
   - Developer G: User Story 7 (Career Service - P3)
3. Stories complete and integrate independently
4. Team reconvenes for Saga Implementation (Phase 10) and Polish (Phase 11)

---

## Success Criteria Mapping

| Success Criteria | Tasks |
|------------------|-------|
| SC-001: Employee Service LOC reduced 70% (82K → 25K) | T091-T131, T334 |
| SC-002: Employee Service files reduced 60% (459 → 150-180) | T091-T131, T335 |
| SC-003: All 7 services independently deployable | T001-T090, T336 |
| SC-004: All automated tests pass at 100% | T340 (integration tests via Testcontainers) |
| SC-005: All integration events published/consumed correctly | T065-T067, T125-T127, T154-T156, T184-T186, T205-T207, T232-T234, T252-T254, T282-T284, T339 |
| SC-006: All background services run successfully | T128-T129, T157-T159, T208-T209, T235-T237, T255-T257, T285-T287, T340 |
| SC-007: Documentation complete | T047-T051, T305-T311 |
| SC-008: Permissions correctly distributed | T123-T124, T152-T153, T182-T183, T203-T204, T227-T228, T250-T251, T280-T281 |
| SC-009: Each service has isolated database | T136-T139, T166-T168, T192-T194, T216-T218, T240-T242, T263-T265 |
| SC-010: Saga compensating transactions work | T288-T304 |
| SC-011: Saga state persists and recovers | T068-T070, T303-T304, T333 |
| SC-012: Correlation IDs propagated correctly | T331 |
| SC-013: Health checks functional | T319-T320 |
| SC-014: 80% code coverage | Included in CI/CD workflows T076-T090 |
| SC-015: Independent build/test/deploy | T071-T090, T337 |
| SC-016: Zero cross-service database joins | T338 |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- This is a **pre-deployment refactoring** - no migration complexity, no backward compatibility concerns
- Total tasks: 340 (Setup: 53, Foundational: 37, US1: 41, US2: 30, US3: 25, US4: 23, US5: 28, US6: 20, US7: 30, Saga: 17, Polish: 36)