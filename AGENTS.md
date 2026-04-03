# Agent Operational Guide

This repository contains the `Maliev.EmployeeService`, a .NET 10 microservice following Clean Architecture principles.
Use this guide to understand how to build, test, and contribute to the codebase.

---

## Build, Test & Lint Commands

All commands run from within this service directory (`B:\maliev\Maliev.EmployeeService`).

```powershell
# Build (treats warnings as errors — all must be fixed)
dotnet build Maliev.EmployeeService.slnx

# Run all tests
dotnet test Maliev.EmployeeService.slnx --verbosity normal

# Run a single test method
dotnet test --filter "FullyQualifiedName~EmployeeTests.ShouldCreateEmployee"

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~EmployeeTests"

# Run with code coverage
dotnet test Maliev.EmployeeService.slnx --collect:"XPlat Code Coverage"

# Format check
dotnet format Maliev.EmployeeService.slnx

# EF Core migrations (Infrastructure project only)
dotnet ef migrations add <Name> --project Maliev.EmployeeService.Infrastructure --startup-project Maliev.EmployeeService.Infrastructure
```

---

## Code Style & Conventions

### Workspace Structure
```
Maliev.EmployeeService/
├── Maliev.EmployeeService.Api/           # Controllers, Consumers, Middleware
├── Maliev.EmployeeService.Application/   # Use cases, DTOs, Interfaces, Handlers
├── Maliev.EmployeeService.Domain/        # Entities, value objects, domain interfaces
├── Maliev.EmployeeService.Infrastructure/ # EF Core DbContext, repositories, HTTP clients
├── Maliev.EmployeeService.Tests/         # Unit + Integration tests (xUnit)
├── Directory.Build.props                 # Central package versioning
└── Maliev.EmployeeService.slnx          # Solution file (.slnx preferred over .sln)
```

### C# Naming & Formatting
- **Namespaces**: File-scoped (`namespace Maliev.EmployeeService.Api.Controllers;`)
- **Classes/Methods/Properties**: `PascalCase`
- **Private fields**: `_camelCase` (underscore prefix)
- **Parameters/locals**: `camelCase`
- **Async methods**: Suffix with `Async` (e.g., `GetByIdAsync`)
- **Interfaces**: Prefix with `I` (e.g., `IRepository`)
- **Permissions**: GCP-style `{domain}.{plural-resource}.{action}` as `public const string` in a `Permissions` static class
  - Valid: `employee.employees.create`, `employee.departments.update`
  - Invalid: `employee.employee.create` (singular), `employee.create` (missing resource)
- **XML docs**: Required on ALL public methods and properties
- **Nullable**: Enabled (`<Nullable>enable</Nullable>`). Use `?` explicitly
- **Imports**: System first, then third-party, then local. Alphabetize within groups. Remove unused `using`
- **Braces**: Allman style (new line) for methods and control structures. Expression-bodied for properties/accessors
- **Indentation**: 4 spaces, LF line endings, UTF-8, trim trailing whitespace

### C# Patterns
- **DI**: Constructor injection with `private readonly` fields
- **Controllers**: `[ApiController]`, `[ApiVersion("1")]`, `[Route("employee/v{version:apiVersion}")]`
- **Logging**: `ILogger<T>` with structured placeholders (never interpolate): `_logger.LogInformation("Processing {DepartmentId}", departmentId)`
- **Error handling**: Global exception middleware. Return `ProblemDetails` / `ErrorResponse` DTOs. Never expose stack traces
- **Manual mapping**: Static extension methods (`ToDto()`, `ToEntity()`). AutoMapper is banned
- **Validation**: `System.ComponentModel.DataAnnotations` on DTOs. FluentValidation is banned

### Service-Specific Architecture & Patterns
- **Clean Architecture Layers:**
  - `Api`: Controllers, Entry point.
  - `Application`: Business logic, Commands, Queries, DTOs, Interfaces.
  - `Domain`: Entities, Value Objects, Domain Logic (Pure C#).
  - `Infrastructure`: Data access, External services implementations.
- **CQRS-like Handlers:**
  - Handlers are injected directly into Controllers (e.g., `CreateDepartmentCommandHandler`), not via `IMediator`.
  - Command/Query segregation is encouraged.
- **Entities:** Rich Domain Models. Use logic inside entities where possible.
- **Result Pattern:** Prefer returning a Result object (e.g., `Result.Success` or `Result.Failure`) for business logic failures rather than throwing exceptions.
  - Controllers map Result objects to HTTP status codes (`Ok`, `BadRequest`, `NotFound`).
  - Exceptions are reserved for unexpected system errors or critical validation failures.

---

## Banned Libraries (Build Will Fail)

| Banned | Use Instead |
|--------|-------------|
| AutoMapper | Manual mapping extensions |
| FluentValidation | DataAnnotations or manual validation |
| FluentAssertions | Standard xUnit `Assert.*` |
| Swashbuckle/Swagger | Scalar (at `/employee/scalar`) |
| InMemoryDatabase (EF Core) | Testcontainers with real PostgreSQL |

---

## Testing Rules

- **Framework**: xUnit with standard `Assert` (`Assert.Equal`, `Assert.NotNull`, etc.)
- **Naming**: `MethodName_StateUnderTest_ExpectedBehavior` or `HTTP_METHOD_Path_Scenario_ExpectedStatus`
- **Coverage**: Minimum 80% per service
- **Integration tests**: `BaseIntegrationTestFactory<TProgram, TDbContext>` with Testcontainers (PostgreSQL, Redis, RabbitMQ). Never InMemoryDatabase
- **System tests** (Tier 3): `AspireTestFixture` with `[Collection("AspireDomainTests")]` — shared AppHost, never one per class
- **Eventual consistency**: Use `TestHelpers.WaitForAsync`. Never `Task.Delay`
- **MassTransit consumers**: Must have consumer tests using `AddMassTransitTestHarness()`

### Testing Strategy (4-Tier Pyramid Context)

This service's tests cover **Tier 1 (Unit)** and **Tier 2 (Service Integration)** of the Maliev testing pyramid:

| Tier | What to Test | Infrastructure |
|------|-------------|---------------|
| **Unit** | Business logic, domain models, service methods with mocked dependencies | None (mocks only) |
| **Service Integration** | API endpoints, database persistence, permission enforcement, input validation | `BaseIntegrationTestFactory` + Testcontainers (Postgres/Redis/RabbitMQ) |

**Tier 3 (System Integration)** — cross-service workflows and event chains — is tested in `Maliev.Aspire.Tests/`.

> Full ecosystem test strategy: `Maliev.Aspire.Tests/TEST_PLAN.md`

---

## Mandatory Rules

- **`TreatWarningsAsErrors = true`**: Zero warnings allowed. No suppression
- **`[RequirePermission("domain.resources.action")]`**: On all endpoints, not plain `[Authorize]`
- **API versioning**: All routes versioned (`v1/`)
- **Service prefix**: Routes prefixed with service domain (e.g., `/employee`)
- **Scalar docs**: Configured at `/employee/scalar`
- **Secrets**: Never hardcoded. Use GCP Secret Manager or environment variables
- **Async/await**: All the way down. Pass `CancellationToken`
- **EF Core Design package**: Only in Infrastructure project, never in Api
- **PostgreSQL xmin**: Shadow property only — `entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion()`. Never add entity property
- **Temporary files**: Generate in `/temp` folder, clean up afterwards

### EF Core Design Package
- `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- It belongs ONLY in the Infrastructure project where migrations live
- Migration commands must target Infrastructure as both project and startup-project:
  ```
  dotnet ef migrations add <Name> --project Maliev.EmployeeService.Infrastructure --startup-project Maliev.EmployeeService.Infrastructure
  ```

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- Never use `.Ignore(e => e.Xmin)` — remove the entity property instead

---

## Git Rules

- Each `Maliev.*` folder is an independent git repo. `cd` into it before git commands
- **Commit early and often** after every meaningful unit of work. Do not accumulate changes
- **Never use `git checkout` to restore files** — commit first, then `git revert` or `git reset --soft`
- Feature branches merged to `develop` via PR. Do not push without being asked

---

## Agent Behavioral Guidelines

- **Safety First:** Always run tests (`dotnet test`) after making changes.
- **Incremental Changes:** Verify existing functionality before adding new features.
- **Proactive Fixes:** If you see a warning, fix it. The build will fail otherwise.
- **Context Awareness:** Read related files (e.g., the Controller when modifying a Handler) to ensure interface consistency.
- **No "Magic":** Do not assume libraries like AutoMapper or MediatR are configured unless you verify them. Use manual mapping if explicit mappers are not found.
