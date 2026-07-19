# Tasks: Principal-First Model Migration (IAM Integration)

**Input**: Design documents from `specs/002-iam-integration/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included as per MALIEV Constitution (Principle III: Test-First Development). All tests MUST use Testcontainers for real infrastructure (Principle IV).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- File paths are relative to the repository root.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Configure IAM external service settings in `Maliev.EmployeeService.Api/appsettings.json`
- [x] T002 [P] Configure feature flag `PrincipalBasedAuthEnabled` in `Maliev.EmployeeService.Api/appsettings.json`
- [x] T003 Register `IIAMClient` and `HttpClient` factory in `Maliev.EmployeeService.Api/Program.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure and database schema updates

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Add `PrincipalId` property to `Employee` entity in `Maliev.EmployeeService.Domain/Entities/Employee.cs`
- [x] T005 Create EF Core migration `AddPrincipalIdToEmployees` in `Maliev.EmployeeService.Data`
- [x] T006 Create `CreatePrincipalRequest` DTO in `Maliev.EmployeeService.Application/DTOs/CreatePrincipalRequest.cs`
- [x] T007 Create `CreatePrincipalResponse` DTO in `Maliev.EmployeeService.Application/DTOs/CreatePrincipalResponse.cs`
- [x] T008 [P] Define `IIAMClient` interface in `Maliev.EmployeeService.Infrastructure/IAM/IIAMClient.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Employee Creation with Identity Delegation (Priority: P1) 🎯 MVP

**Goal**: Automatically create IAM identity when creating a new employee.

**Independent Test**: Create an employee via the API and verify `PrincipalId` is populated and IAM client was invoked.

### Tests for User Story 1

- [x] T009 [P] [US1] Unit test for `IAMClient` success/failure scenarios in `Maliev.EmployeeService.Tests/Unit/IAMClientTests.cs`
- [x] T010 [P] [US1] Integration test for employee creation with IAM integration in `Maliev.EmployeeService.Tests/Integration/EmployeeCreationTests.cs`

### Implementation for User Story 1

- [x] T011 [US1] Implement `IAMClient` using `HttpClient` in `Maliev.EmployeeService.Infrastructure/IAM/IAMClient.cs`
- [x] T012 [US1] Update `EmployeeService.CreateEmployeeAsync` to call IAM and set `PrincipalId` in `Maliev.EmployeeService.Application/Services/EmployeeService.cs`
- [x] T013 [US1] Add IAM timeout and error handling in `EmployeeService.CreateEmployeeAsync` (depends on T011)

**Checkpoint**: New employees are now created with IAM principals. Verified via integration tests.

---

## Phase 4: User Story 2 - Identity-Based Employee Lookup (Priority: P1)

**Goal**: Look up employee HR profile using IAM `principal_id`.

**Independent Test**: `GET /employees/v1/employees/by-principal/{id}` returns the correct profile.

### Tests for User Story 2

- [x] T014 [P] [US2] Integration test for `GetByPrincipalId` endpoint with existing/missing IDs in `Maliev.EmployeeService.Tests/Integration/EmployeeControllerTests.cs`

### Implementation for User Story 2

- [x] T015 [US2] Add `GetByPrincipalIdAsync` to `IEmployeeService` in `Maliev.EmployeeService.Application/Services/IEmployeeService.cs`
- [x] T016 [US2] Implement `GetByPrincipalIdAsync` in `Maliev.EmployeeService.Application/Services/EmployeeService.cs`
- [x] T017 [US2] Implement `GetByPrincipalId` action in `Maliev.EmployeeService.Api/Controllers/EmployeeProfileController.cs`
- [x] T018 [US2] Add database index for `PrincipalId` in `Maliev.EmployeeService.Data` (via Fluent API or Migration)

**Checkpoint**: Employees can now be retrieved by their IAM identity. Verified via integration tests.

---

## Phase 5: User Story 3 - Legacy Credential Validation & Authentication (Priority: P2)

**Goal**: Transition authentication to use `principal_id` while maintaining legacy fallback.

**Independent Test**: `/auth/validate` returns `principalId` for migrated users and legacy `userId` for others.

### Tests for User Story 3

- [x] T019 [P] [US3] Unit test for `CurrentUserService` extraction of `sub` claim in `Maliev.EmployeeService.Tests/Unit/CurrentUserServiceTests.cs`
- [x] T020 [P] [US3] Integration test for `ValidateCredentials` with fallback logic in `Maliev.EmployeeService.Tests/Integration/CredentialValidationTests.cs`

### Implementation for User Story 3

- [x] T021 [US3] Update `ICurrentUserService` interface with `PrincipalId` and `GetEmployeeIdAsync` in `Maliev.EmployeeService.Infrastructure/Authentication/ICurrentUserService.cs`
- [x] T022 [US3] Implement `sub` claim extraction and Redis-backed 24-hour caching in `Maliev.EmployeeService.Infrastructure/Authentication/CurrentUserService.cs`
- [x] T022b [US3] Add unit tests for Redis cache hit/miss scenarios in `Maliev.EmployeeService.Tests/Unit/CurrentUserServiceTests.cs`

---

## Phase 6: Workforce Migration (Backfill)

**Purpose**: Migrate all existing employees to IAM principals.

- [x] T025 [P] Unit test for migration script batching and error handling in `Maliev.EmployeeService.Tests/Unit/MigrationScriptTests.cs`
- [x] T026 Implement `MigrateEmployeesToPrincipalsScript` in `Scripts/MigrateEmployeesToPrincipalsScript.cs`
- [x] T026b [US1] Instrument migration script with business metrics (Total processed, success, failures) per Principle XII
- [x] T027 Add CLI command `--migrate-principals` to `Maliev.EmployeeService.Api/Program.cs`
- [x] T028 Create `MIGRATION_RUNBOOK.md` in `Scripts/MIGRATION_RUNBOOK.md`

---

## Phase 7: Cleanup Phase (Final State)

**Purpose**: Remove legacy `User` table and finalize schema.

- [x] T028b Verify SC-005: Execute cross-service SQL query to ensure zero orphaned principals between IAM and EmployeeService
- [x] T029 Create EF Core migration to make `PrincipalId` `NOT NULL` and `UNIQUE` in `Maliev.EmployeeService.Data`
- [x] T030 Remove `User` entity and related configuration in `Maliev.EmployeeService.Domain/Entities/User.cs`
- [x] T031 Remove legacy `User` database table using migration or SQL script.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [x] T031b [P] Instrument API endpoints with business metrics (Principal lookups, validation success/fail) in `Maliev.EmployeeService.Api/Controllers/`
- [x] T031c [P] Add integration test for `/metrics` endpoint to verify business telemetry presence in `Maliev.EmployeeService.Tests/Integration/MetricsTests.cs`
- [x] T032 [P] Update OpenAPI contract in `specs/002-iam-integration/contracts/iam-integration-v1.yaml` with any implementation deviations
- [x] T033 [P] Conduct constitution audit (Zero Warnings, explicit mapping, real infra tests)
- [x] T034 [P] Verify `quickstart.md` instructions against final implementation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** & **Foundational (Phase 2)** MUST be complete before any user story.
- **User Story 1 & 2 (P1)** can proceed in parallel once Phase 2 is done.
- **User Story 3 (P2)** depends on US1/US2 logic for full functionality but can be started in parallel.
- **Workforce Migration (Phase 6)** requires `IAMClient` (US1) and `PrincipalId` schema (Phase 2).
- **Cleanup (Phase 7)** MUST ONLY start after Phase 6 is 100% verified in production.

### Parallel Opportunities

- T001, T002, T003 (Setup)
- T006, T007, T008 (Foundational)
- US1, US2, US3 can be worked on by different developers.
- All test tasks marked [P] can run concurrently with their implementation tasks.

---

## Implementation Strategy

### MVP First (US1 & US2)

1. Complete Setup + Foundational.
2. Implement US1 (Employee Creation).
3. Implement US2 (Identity Lookup).
4. **STOP and VALIDATE**: Verify that new employees can be created and looked up via IAM ID.

### Migration Path

1. Deploy code with fallback support (US3).
2. Run Migration Script (Phase 6).
3. Verify 100% data integrity.
4. Execute Cleanup (Phase 7).

---

## Notes

- All tests MUST fail first (Principle III).
- Use `Testcontainers.PostgreSql` for all database integration tests (Principle IV).
- No AutoMapper; use explicit mapping in `EmployeeService`.
- Ensure structured JSON logging for the migration script progress.
