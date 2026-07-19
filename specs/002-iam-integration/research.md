# Research: IAM Integration Strategy

## Decision 1: IAM Client Communication
- **Decision**: Use `HttpClient` with a pre-configured `ServiceAccountToken` in the `Authorization` header.
- **Rationale**: Simplest and most secure way for service-to-service communication within the internal network. Aligns with `CustomerService` patterns.
- **Alternatives Considered**: 
    - mTLS: Rejected as overhead exceeds current security requirements for internal traffic.
    - OIDC Flow: Too complex for simple principal creation tasks.

## Decision 2: Migration Failure Handling
- **Decision**: "Skip and Log" strategy for the backfill migration script.
- **Rationale**: Prevents a single IAM failure from blocking the migration of thousands of other records. Allows for manual re-runs on failed IDs.
- **Alternatives Considered**:
    - Stop on Error: Rejected as it makes the migration too fragile and labor-intensive.
    - Automatic Retry: Rejected for Phase 1 to keep script complexity low; can be added later if needed.

## Decision 3: Identity Caching
- **Decision**: Long-lived (24h) caching of `PrincipalId` -> `EmployeeId` mapping in the `CurrentUserService`.
- **Rationale**: This link is immutable (an employee never changes their primary IAM identity). Caching significantly reduces database load on every request.
- **Alternatives Considered**:
    - Per-request caching: Rejected due to performance impact.
    - No caching: Rejected as it forces a DB hit for every authorized API call.

## Decision 4: Legacy Fallback in Auth Validation
- **Decision**: Return legacy `userId` if `PrincipalId` is NULL during the migration period.
- **Rationale**: Ensures zero downtime for employees who haven't been backfilled yet.
- **Alternatives Considered**:
    - Just-in-Time Migration: Rejected to avoid slowing down the login process with an external API call.

## Decision 5: Data Persistence Strategy
- **Decision**: Add `PrincipalId` as a nullable UUID initially, then make it NOT NULL and UNIQUE after backfill completion.
- **Rationale**: Allows the schema to evolve safely without breaking the system during the migration window.
