# Feature Specification: Principal-First Model Migration (IAM Integration)

**Feature Branch**: `002-iam-integration`  
**Created**: 2025-12-21  
**Status**: Draft  
**Input**: Migration of EmployeeService to principal-first model where IAM owns identity.

## Clarifications

### Session 2025-12-21
- Q: What defines a "successful verification" before dropping the legacy User table? → A: 100% Null Check: Zero records in the Employee table have a null PrincipalId.
- Q: How to handle login for employees with NULL PrincipalId during migration? → A: Legacy Fallback: Return legacy userId if PrincipalId is NULL.
- Q: How should the migration script handle IAM failures? → A: Skip and Log: Log the failure for that employee and continue with the next.
- Q: How should CurrentUserService behave if the sub claim is missing/malformed? → A: Strict: Throw unauthorized exception immediately.
- Q: What is the required caching strategy for PrincipalId to EmployeeId mapping? → A: Long-Lived: Cache for 24 hours or indefinitely as the link is permanent.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Employee Creation with Identity Delegation (Priority: P1)

As the HR System, when I create a new employee, I want the identity to be automatically created in the IAM service so that the employee has a unique principal identity across the entire ecosystem from day one.

**Why this priority**: Foundational for the principal-first model. Ensures all new data follows the target architecture.

**Independent Test**: Create an employee via the API and verify that a call was made to the IAM service, a `principal_id` was received, and that ID is stored in the `employees` table.

**Acceptance Scenarios**:

1. **Given** a valid `CreateEmployeeRequest`, **When** the creation endpoint is called, **Then** a principal is created in IAM and the employee record is persisted with the returned `PrincipalId`.
2. **Given** the IAM service is unavailable, **When** an employee creation is attempted, **Then** the system returns a 500 error and does not persist an orphaned employee record.

---

### User Story 2 - Identity-Based Employee Lookup (Priority: P1)

As a downstream service (like Payroll or Benefits), I want to look up an employee's HR profile using their IAM `principal_id` so that I can link identity-based events (like logging in) to specific HR records.

**Why this priority**: Essential for integrating with the new authentication flow where only the `principal_id` is present in the JWT.

**Independent Test**: Call the new endpoint `GET /employees/v1/employees/by-principal/{principalId}` with a known ID and verify the correct HR profile is returned.

**Acceptance Scenarios**:

1. **Given** an existing employee with a `PrincipalId`, **When** I query the "by-principal" endpoint, **Then** I receive the full `EmployeeProfileResponse`.
2. **Given** a `PrincipalId` that does not exist in EmployeeService, **When** I query the endpoint, **Then** I receive a 404 Not Found response.

---

### User Story 3 - Legacy Credential Validation (Priority: P2)

As the Authentication Service, I want the EmployeeService to return a `principal_id` instead of a `user_id` during credential validation so that I can issue tokens that are compatible with the new principal-first model.

**Why this priority**: Required for backward compatibility during the migration phase while the `User` table still exists.

**Independent Test**: Call the `/auth/validate` endpoint with valid credentials and verify the JSON response contains `principalId` (UUID).

**Acceptance Scenarios**:

1. **Given** valid employee credentials, **When** I call `/auth/validate`, **Then** the response contains `isValid: true` and the employee's `principalId`.

---

### Edge Cases

- **IAM Timeout**: If IAM takes too long to respond during employee creation, the system must handle the timeout and fail safely without creating a partial employee.
- **Duplicate Emails**: If IAM rejects a principal creation due to a duplicate email, the EmployeeService must return a meaningful error.
- **Migration Synchronization**: During backfill, the `/auth/validate` endpoint MUST return the legacy `userId` if `PrincipalId` is NULL to ensure uninterrupted access.
- **Concurrent Creation**: New employee creations take precedence and MUST always be created with a `PrincipalId` via IAM even if the backfill script is running.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST add a `PrincipalId` (UUID) column to the `Employee` entity and database table.
- **FR-002**: System MUST implement a new endpoint `GET /employees/v1/employees/by-principal/{principalId}`.
- **FR-003**: System MUST integrate an `IIAMClient` to communicate with the IAM Service for principal creation.
- **FR-004**: System MUST update the `CreateEmployee` flow to call IAM before persisting the employee.
- **FR-005**: System MUST update `CurrentUserService` to extract `principal_id` from the JWT `sub` claim; the service MUST throw an unauthorized exception if the claim is missing or malformed.
- **FR-006**: System MUST update the `/auth/validate` endpoint to return `principalId` instead of `userId`.
- **FR-007**: System MUST provide a migration script to backfill `PrincipalId` for all existing employees via IAM; the script MUST log failures for individual records and continue processing the remaining records.
- **FR-008**: System MUST support a "Cleanup Phase" to remove the legacy `User` table and related domain logic once verification confirms zero records in the Employee table have a null `PrincipalId`.
- **FR-009**: System MUST cache the mapping between `PrincipalId` and `EmployeeId` using a long-lived strategy (e.g., 24 hours) to minimize database lookups in `CurrentUserService`.

### Key Entities *(include if feature involves data)*

- **Employee**: Modified to include `PrincipalId` as a unique identifier linking to IAM.
- **IAM Principal**: External entity representing the "Who" (identity) in the system.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of existing employee records successfully linked to an IAM principal via the migration script.
- **SC-002**: New employee creation latency increases by no more than 500ms despite the external IAM call.
- **SC-003**: The `User` table is successfully dropped from the production database without data loss in the `Employee` table.
- **SC-004**: All API responses for identity-related fields strictly use the `principal_id` format.
- **SC-005**: Zero "orphaned" principals (principals created in IAM but not linked to an employee in EmployeeService).

## Assumptions & Scope

### Assumptions
- IAM service provides a stable API for principal creation and is highly available.
- JWT tokens will be updated by the identity provider to include the `sub` claim correctly.

### Out of Scope
- Migrating actual login credentials/passwords to IAM (This is handled by the IAM/Auth service transition).
- Updating external services that consume EmployeeService (they must update to use the new IDs).