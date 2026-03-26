# Agent Operational Guide

This repository contains the `Maliev.EmployeeService`, a .NET 10 microservice following Clean Architecture principles.
Use this guide to understand how to build, test, and contribute to the codebase.

## 1. Build, Lint, and Test

### Build
The project enforces `TreatWarningsAsErrors`, so the build process acts as a strict linter.
```bash
# Build the entire solution
dotnet build Maliev.EmployeeService.slnx

# Build specific project
dotnet build Maliev.EmployeeService.Api/Maliev.EmployeeService.Api.csproj
```

### Test
The project uses **xUnit** with **Moq** and **Testcontainers**.
```bash
# Run all tests
dotnet test Maliev.EmployeeService.slnx

# Run a single test (by fully qualified name)
dotnet test --filter "FullyQualifiedName=Maliev.EmployeeService.Tests.Domain.EmployeeTests.ShouldCreateEmployee"

# Run tests matching a display name
dotnet test --filter "DisplayName~ShouldCreateEmployee"
```

### Linting & Formatting
*   Code style is enforced via compiler warnings treated as errors.
*   Ensure all code compiles without warnings before submitting.
*   Standard C# formatting rules apply (suggest running `dotnet format` if unsure, though not explicitly configured in CI yet).

## 2. Code Style & Conventions

### General
*   **Framework:** .NET 10.0
*   **Language Version:** Latest (C# 13/14 equivalent).
*   **Nullable Reference Types:** Enabled (`<Nullable>enable</Nullable>`). Handle nullability explicitly.
*   **Implicit Usings:** Enabled.

### Formatting
*   **Namespaces:** Use **file-scoped namespaces** (`namespace Maliev.EmployeeService.Api.Controllers;`).
*   **Braces:** Use Allman style (braces on new lines).
*   **Indentation:** 4 spaces.
*   **Line Length:** Aim for < 120 characters where possible.

### Naming Conventions
*   **Classes/Methods/Properties:** PascalCase (`DepartmentController`, `GetAllDepartments`).
*   **Local Variables/Parameters:** camelCase (`departmentId`, `createDto`).
*   **Private Fields:** _camelCase (`_departmentRepository`, `_logger`).
*   **Async Methods:** Suffix with `Async` (`HandleAsync`, `GetByIdAsync`).
*   **Interfaces:** Prefix with I (`IRepository`).

### Architecture & Patterns
*   **Clean Architecture:**
    *   `Api`: Controllers, Entry point.
    *   `Application`: Business logic, Commands, Queries, DTOs, Interfaces.
    *   `Domain`: Entities, Value Objects, Domain Logic (Pure C#).
    *   `Infrastructure`: Data access, External services implementations.
*   **CQRS-like Handlers:**
    *   Handlers are injected directly into Controllers (e.g., `CreateDepartmentCommandHandler`), not via `IMediator`.
    *   Command/Query segregation is encouraged.
*   **API Versioning:** Use `[ApiVersion("1.0")]` on Controllers.
*   **Entities:** Rich Domain Models. Use logic inside entities where possible.

### Error Handling
*   **Result Pattern:** Prefer returning a Result object (e.g., `Result.Success` or `Result.Failure`) for business logic failures rather than throwing exceptions.
*   **Controllers:** Map Result objects to HTTP status codes (`Ok`, `BadRequest`, `NotFound`).
*   **Exceptions:** Reserve for unexpected system errors or critical validation failures (e.g., `ArgumentNullException` in constructors).

### Documentation
*   **Public APIs:** Use XML documentation (`/// <summary>`) for Controllers and DTOs.
*   **Comments:** Sparse. Explain *why*, not *what*. Code should be self-documenting.

### Imports (Usings)
*   Place `using` directives at the top of the file.
*   Remove unused usings.
*   Sort alphabetically (System.* first is common but not strictly enforced if consistent).

## 3. Agent Behavioral Guidelines

*   **Safety First:** Always run tests (`dotnet test`) after making changes.
*   **Incremental Changes:** Verify existing functionality before adding new features.
*   **Proactive Fixes:** If you see a warning, fix it. The build will fail otherwise.
*   **Context Awareness:** Read related files (e.g., the Controller when modifying a Handler) to ensure interface consistency.
*   **No "Magic":** Do not assume libraries like AutoMapper or MediatR are configured unless you verify them. Use manual mapping if explicit mappers are not found.


## Git & Version Control — Mandatory Rules

### 🚨 CRITICAL: Always Commit Code Changes (Non-Negotiable)
- **You MUST commit your changes to the local repository after completing any meaningful unit of work.**
- **Never accumulate uncommitted changes.** Do not wait until end of session or until something breaks.
- **Commit early and often** — if a change is meaningful (even a small fix or refactor), commit it.
- **You do NOT need to push to remote** — local commits are sufficient to protect against accidental loss.
- **If you are unsure whether to commit, commit anyway.** Extra commits are harmless; lost work is irreversible.
- This rule applies even if you are just "testing" or "exploring" — use git branches to isolate experimental work and commit those changes too.

### 🚨 CRITICAL: Never Use `git checkout` to Restore Broken Files
- **NEVER use `git checkout` to restore or recover files.** This operation discards uncommitted changes permanently and will result in data loss.
- **To undo/recover from broken files: first commit your current changes, then use `git revert` or `git reset --soft` to safely undo.**

## Database & EF Core — Mandatory Rules

### EF Core Design Package
- ❌ `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- ✅ It belongs ONLY in the Infrastructure (or Data) project where migrations live
- Migration commands must target Infrastructure as both project and startup-project (since EF Core Design package is in Infrastructure):
  ```
  dotnet ef migrations add <Name> --project Maliev.<Domain>Service.Infrastructure --startup-project Maliev.<Domain>Service.Infrastructure
  ```

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- ❌ Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- ❌ Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- ❌ Never use `.Ignore(e => e.Xmin)` — remove the entity property instead
