# Data Model: Principal-First Migration

## Entities

### Employee (Updated)
Represents a workforce member with HR-specific data.

| Field | Type | Nullable | Constraints | Description |
|-------|------|----------|-------------|-------------|
| Id | UUID | No | Primary Key | Internal employee identifier. |
| PrincipalId | UUID | Yes -> No | Unique | Link to the IAM identity. Becomes NOT NULL after migration. |
| EmployeeNumber | String | No | Unique | Company-assigned identifier. |
| LegalName | ValueObject | No | | Composite of First, Last, Full Name. |
| ContactInfo | ValueObject | No | | Email, Phone, etc. |

### User (Deprecated)
Legacy table managed by EmployeeService for authentication.

**Status**: To be dropped in the Cleanup Phase.

## Relationships
- **Employee (1) -> (1) IAM Principal**: Linked via `PrincipalId`. One-to-one mapping across the ecosystem.

## Validation Rules
- `PrincipalId` must be a valid UUID.
- `PrincipalId` must be unique across all active and inactive employees.

## State Transitions
1. **Pending Migration**: `PrincipalId` is NULL. System uses legacy `userId` for identification.
2. **Migrated**: `PrincipalId` is assigned. System uses `PrincipalId` for all identity concerns.
3. **Finalized**: Schema is updated to `NOT NULL`. `User` table is dropped.
