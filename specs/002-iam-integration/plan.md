# Implementation Plan: Principal-First Model Migration (IAM Integration)

**Branch**: `002-iam-integration` | **Date**: 2025-12-21 | **Spec**: [specs/002-iam-integration/spec.md]
**Input**: Feature specification from `/specs/002-iam-integration/spec.md`

## Summary

Migrate `EmployeeService` from a custom `User` entity to a principal-first model where IAM owns identity. This involves adding a `PrincipalId` to the `Employee` entity, integrating with the IAM service for identity creation, and updating the authentication flow to use the IAM `principal_id` as the primary identifier. A legacy fallback and phased cleanup approach will ensure zero downtime during the migration.

## Technical Context

**Language/Version**: .NET 10.0 (C#)
**Primary Dependencies**: 
- `Microsoft.EntityFrameworkCore.PostgreSQL`
- `Microsoft.Extensions.Http` (IAM Client)
- `Maliev.Aspire.ServiceDefaults` (NuGet)
- `StackExchange.Redis` (for PrincipalId mapping)
- `Testcontainers.PostgreSql` & `Testcontainers.Redis` (for integration tests)
**Storage**: PostgreSQL (Employee DB), Redis (Identity Cache)
**Testing**: xUnit, Testcontainers (No in-memory substitutes permitted)
**Target Platform**: Linux (Docker containers on ASP.NET 10.0 runtime)
**Project Type**: .NET Microservice (Web API + Background Scripts)
**Performance Goals**: 
- New employee creation latency increase < 500ms
- Principal-to-Employee lookup cached in Redis (24h)
- Summary dashboard widgets < 2.5s
**Constraints**: 
- Zero Warnings Policy
- NO AutoMapper / FluentValidation / FluentAssertions
- Secrets from Google Secret Manager (prod)
- Strict JWT validation (sub claim mandatory)
- **Mandatory Business Metrics** for migration and identity flows.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Implementation Detail |
|-----------|--------|-----------------------|
| **Service Autonomy** | PASSED | EmployeeService owns HR data; Identity is delegated to IAM. |
| **Explicit Contracts** | PASSED | New endpoint `GET /by-principal/{id}` documented via OpenAPI. |
| **Test-First (Real Infra)** | MANDATORY | Tests created before implementation using Testcontainers. |
| **Auditability** | PASSED | Structured logging for IAM calls and migration script progress. |
| **Secrets Management** | PASSED | IAM tokens and BaseUrl from configuration/Secret Manager. |
| **Zero Warnings** | MANDATORY | Build configuration treats warnings as errors. |
| **Clean Artifacts** | PASSED | Only `plan.md`, `spec.md`, and relevant code/scripts added. |
| **Standard Docker** | PASSED | Using .NET 10 images with `app` user and multi-stage builds. |

## Project Structure

### Documentation (this feature)

```text
specs/002-iam-integration/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── iam-integration-v1.yaml
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
Maliev.EmployeeService.Api/
├── Controllers/
│   ├── EmployeeAuthController.cs (Updated)
│   └── EmployeeProfileController.cs (New Endpoint)
└── Dockerfile

Maliev.EmployeeService.Application/
├── Services/
│   ├── EmployeeService.cs (Updated)
│   └── IEmployeeService.cs (Updated)
└── DTOs/
    ├── CreatePrincipalRequest.cs
    └── CreatePrincipalResponse.cs

Maliev.EmployeeService.Domain/
└── Entities/
    ├── Employee.cs (Updated)
    └── User.cs (To be removed)

Maliev.EmployeeService.Infrastructure/
├── Authentication/
│   ├── CurrentUserService.cs (Updated)
│   └── ICurrentUserService.cs (Updated)
├── IAM/
│   ├── IIAMClient.cs
│   └── IAMClient.cs
└── Data/
    └── Migrations/ (New migration)

Maliev.EmployeeService.Tests/
├── Unit/
│   ├── IAMClientTests.cs
│   └── CurrentUserServiceTests.cs
└── Integration/
    ├── EmployeeCreationTests.cs
    └── MigrationScriptTests.cs

Scripts/
├── MigrateEmployeesToPrincipalsScript.cs
└── MIGRATION_RUNBOOK.md
```

**Structure Decision**: Standard .NET microservice structure following the MALIEV Constitution (Flat Structure, Company Prefix).

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | No violations identified. | |