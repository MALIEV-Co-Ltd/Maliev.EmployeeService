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
