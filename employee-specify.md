# EmployeeService Specification - Principal-First Model Migration

## Overview
Migrate EmployeeService from custom User entity to principal-first model where IAM owns identity. Remove duplicate user management and reference principal_id for identity concerns.

## Current State
- EmployeeService owns `User` table with EmployeeId FK
- User table stores username, role, IsActive, LastLoginDate
- CurrentUserService extracts employee_id from JWT claims
- Employee entity has no principal_id reference

## Target State
- EmployeeService removes User table entirely
- Employee entity has `principal_id` column (FK to IAM.principals)
- CurrentUserService extracts principal_id from JWT sub claim
- EmployeeService focuses purely on employee HR data (compensation, performance, org structure)
- New endpoint: `GET /employees/v1/employees/by-principal/{principalId}`

## Core Architectural Principle
> **EmployeeService owns "what" (employee HR data), not "who" (identity).**
> **Identity (principal_id) is owned by IAM.**

## Key Changes Required

### 1. Add principal_id Column to Employee Entity

**File**: `Maliev.EmployeeService.Domain/Entities/Employee.cs`

**Current**:
```csharp
public class Employee : Entity
{
    public string EmployeeNumber { get; set; }
    public LegalName LegalName { get; set; }
    public DateOnly DateOfBirth { get; set; }
    // ... other HR fields
    // NO principal_id
}
```

**Target**:
```csharp
public class Employee : Entity
{
    public Guid? PrincipalId { get; set; }  // NEW: Link to IAM principal
    public string EmployeeNumber { get; set; }
    public LegalName LegalName { get; set; }
    public DateOnly DateOfBirth { get; set; }
    // ... other HR fields
}
```

**Migration Strategy**:
1. Add column as nullable: `ALTER TABLE employees ADD COLUMN principal_id UUID NULL;`
2. Backfill principal_id from User.EmployeeId → Employee.Id mapping
3. Make NOT NULL after verification: `ALTER TABLE employees ALTER COLUMN principal_id SET NOT NULL;`
4. Add unique index: `CREATE UNIQUE INDEX idx_employees_principal ON employees(principal_id);`

### 2. Create Migration for principal_id Column

**File**: `Maliev.EmployeeService.Data/Migrations/YYYYMMDDHHMMSS_AddPrincipalIdToEmployees.cs`

**Migration Up**:
```sql
-- Add principal_id column (nullable during migration)
ALTER TABLE employees ADD COLUMN principal_id UUID NULL;

-- Create index for lookups
CREATE INDEX idx_employees_principal ON employees(principal_id);
```

**Note**: Do NOT drop User table in this migration. That happens in cleanup phase.

### 3. Add New Endpoint: Get Employee by Principal ID

**File**: `Maliev.EmployeeService.Api/Controllers/EmployeeProfileController.cs`

**New Endpoint**:
```csharp
[HttpGet("by-principal/{principalId}")]
[Authorize]
[ProducesResponseType(typeof(EmployeeProfileResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetByPrincipalId(Guid principalId)
{
    var employee = await _employeeService.GetByPrincipalIdAsync(principalId);

    if (employee == null)
    {
        return NotFound(new { Message = $"Employee not found for principal {principalId}" });
    }

    return Ok(employee);
}
```

**Service Method**:
```csharp
// In IEmployeeService
Task<EmployeeProfileResponse?> GetByPrincipalIdAsync(Guid principalId, CancellationToken cancellationToken = default);

// In EmployeeService
public async Task<EmployeeProfileResponse?> GetByPrincipalIdAsync(Guid principalId, CancellationToken ct)
{
    var employee = await _context.Employees
        .Include(e => e.ContactInformation)
        .Include(e => e.Department)
        .Where(e => e.PrincipalId == principalId)
        .FirstOrDefaultAsync(ct);

    return employee == null ? null : MapToResponse(employee);
}
```

### 4. Modify Employee Creation to Create Principal

**File**: `Maliev.EmployeeService.Application/Services/EmployeeService.cs`

**Current Flow**:
```
1. Create Employee entity
2. Create User entity with EmployeeId FK
3. Save to database
```

**New Flow**:
```
1. Call IAM to create principal
2. Get principal_id from IAM response
3. Create Employee entity with principal_id
4. Save to database (no User entity)
```

**Implementation**:
```csharp
public async Task<EmployeeProfileResponse> CreateEmployeeAsync(CreateEmployeeRequest request, ...)
{
    // NEW: Create principal in IAM first
    Guid principalId;
    try
    {
        var principalRequest = new CreatePrincipalRequest
        {
            PrincipalType = "user",
            LinkedService = "EmployeeService",
            Email = request.ContactInformation.Email,
            DisplayName = request.LegalName.FullName
        };

        var principalResponse = await _iamClient.CreatePrincipalAsync(principalRequest);
        principalId = principalResponse.PrincipalId;

        _logger.LogInformation("Created principal {PrincipalId} for employee", principalId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create principal in IAM for employee");
        throw new InvalidOperationException("Failed to create employee identity", ex);
    }

    // Create employee with principal_id
    var employee = new Employee
    {
        Id = Guid.NewGuid(),
        PrincipalId = principalId,  // NEW
        EmployeeNumber = await GenerateEmployeeNumberAsync(),
        LegalName = request.LegalName,
        ContactInformation = request.ContactInformation,
        // ... other fields
    };

    await _context.Employees.AddAsync(employee);
    await _context.SaveChangesAsync();

    return MapToResponse(employee);
}
```

### 5. Update CurrentUserService

**File**: `Maliev.EmployeeService.Infrastructure/Authentication/CurrentUserService.cs`

**Current Implementation**:
```csharp
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Guid? EmployeeId =>
        Guid.TryParse(_httpContextAccessor.HttpContext?.User.FindFirstValue("employee_id"), out var id)
            ? id : null;

    public string? Email =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public IEnumerable<string> Roles =>
        _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? Enumerable.Empty<string>();
}
```

**New Implementation**:
```csharp
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmployeeRepository _employeeRepository;  // NEW: Inject repository

    // NEW: Extract principal_id from JWT sub claim
    public Guid? PrincipalId =>
        Guid.TryParse(_httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) // "sub" claim
            ?? _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub),
            out var id) ? id : null;

    // NEW: Look up employee by principal_id (cached)
    public async Task<Guid?> GetEmployeeIdAsync(CancellationToken ct = default)
    {
        if (PrincipalId == null)
            return null;

        var employee = await _employeeRepository.GetByPrincipalIdAsync(PrincipalId.Value, ct);
        return employee?.Id;
    }

    public string? Email =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    // Roles now come from JWT permissions claim (handled by RequirePermission attribute)
    public IEnumerable<string> Permissions =>
        _httpContextAccessor.HttpContext?.User.FindAll("permissions").Select(c => c.Value) ?? Enumerable.Empty<string>();
}
```

**Interface Update**:
```csharp
public interface ICurrentUserService
{
    Guid? PrincipalId { get; }                    // NEW
    Task<Guid?> GetEmployeeIdAsync(CancellationToken ct = default);  // CHANGED: now async
    string? Email { get; }
    IEnumerable<string> Permissions { get; }      // CHANGED: from Roles
    bool HasPermission(string permission);        // NEW
    bool IsAuthenticated { get; }
}
```

### 6. Modify Credential Validation Response

**File**: `Maliev.EmployeeService.Api/Controllers/EmployeeAuthController.cs`

**Current Endpoint**:
```csharp
[HttpPost("validate")]
public async Task<IActionResult> ValidateCredentials([FromBody] ValidateCredentialsRequest request)
{
    // Returns: UserId, Email, Name, IsValid
}
```

**New Endpoint**:
```csharp
[HttpPost("validate")]
[ProducesResponseType(typeof(CredentialValidationResponse), StatusCodes.Status200OK)]
public async Task<IActionResult> ValidateCredentials([FromBody] ValidateCredentialsRequest request)
{
    var user = await _context.Users
        .Include(u => u.Employee)
        .FirstOrDefaultAsync(u => u.Username == request.Email);

    if (user == null || user.Employee?.PrincipalId == null)
    {
        return Ok(new CredentialValidationResponse { IsValid = false });
    }

    // Validate password (using existing User table during migration)
    bool isValid = VerifyPassword(user.PasswordHash, request.Password);

    if (!isValid)
    {
        return Ok(new CredentialValidationResponse { IsValid = false });
    }

    return Ok(new CredentialValidationResponse
    {
        IsValid = true,
        PrincipalId = user.Employee.PrincipalId.Value,  // NEW: Return principal_id
        Email = user.Employee.ContactInformation.Email,
        Name = user.Employee.LegalName.FullName
    });
}
```

### 7. Add IAM Client for Principal Creation

**File**: `Maliev.EmployeeService.Infrastructure/IAM/IIAMClient.cs`

**Interface** (same as CustomerService):
```csharp
public interface IIAMClient
{
    Task<CreatePrincipalResponse> CreatePrincipalAsync(
        CreatePrincipalRequest request,
        CancellationToken cancellationToken = default);
}
```

**Registration in Program.cs**:
```csharp
builder.Services.AddHttpClient<IIAMClient, IAMClient>(client =>
{
    var iamUrl = builder.Configuration["ExternalServices:IAM:BaseUrl"];
    client.BaseAddress = new Uri(iamUrl!);
    client.Timeout = TimeSpan.FromSeconds(5);

    var token = builder.Configuration["ExternalServices:IAM:ServiceAccountToken"];
    if (!string.IsNullOrEmpty(token))
    {
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }
});
```

### 8. Remove User Table (Cleanup Phase)

**File to Remove** (after migration verified):
- `Maliev.EmployeeService.Domain/Entities/User.cs`

**Database Table to Drop** (after verification):
```sql
-- Drop User table
DROP TABLE IF EXISTS users CASCADE;
```

**Warning**: Only drop after:
1. All employees have principal_id
2. Production verification complete
3. Backup created
4. Rollback plan tested

## Migration Script

**File**: `B:\maliev\scripts\migrate-employees-to-principals.sql`

```sql
-- Step 1: Verify User-Employee linkage
SELECT COUNT(*) as orphaned_users
FROM employee.users u
LEFT JOIN employee.employees e ON u.employee_id = e.id
WHERE e.id IS NULL;
-- Should return 0

-- Step 2: Create principals in IAM for existing employees
-- Note: Done via application code (MigrateEmployeesToPrincipals.cs)

-- Step 3: Update employees with principal_id
-- Note: After IAM principals created, application code updates employees table

-- Step 4: Verification query
SELECT
    e.id as employee_id,
    e.principal_id,
    e.employee_number,
    e.legal_name_full_name
FROM employee.employees e
WHERE e.principal_id IS NULL;
-- Should return 0 rows

-- Step 5: Make principal_id NOT NULL (after verification)
ALTER TABLE employee.employees
ALTER COLUMN principal_id SET NOT NULL;

-- Step 6: Add unique constraint
CREATE UNIQUE INDEX idx_employees_principal_unique
ON employee.employees(principal_id);
```

## Migration Application Script

**File**: `Maliev.EmployeeService/Scripts/MigrateEmployeesToPrincipals.cs`

```csharp
public class MigrateEmployeesToPrincipalsScript
{
    private readonly EmployeeDbContext _context;
    private readonly IIAMClient _iamClient;
    private readonly ILogger<MigrateEmployeesToPrincipalsScript> _logger;

    public async Task ExecuteAsync()
    {
        var employees = await _context.Employees
            .Include(e => e.ContactInformation)
            .Include(e => e.LegalName)
            .Where(e => e.PrincipalId == null)
            .ToListAsync();

        _logger.LogInformation("Found {Count} employees without principal_id", employees.Count);

        int successCount = 0;
        int failureCount = 0;

        foreach (var employee in employees)
        {
            try
            {
                var principal = await _iamClient.CreatePrincipalAsync(new CreatePrincipalRequest
                {
                    PrincipalType = "user",
                    LinkedService = "EmployeeService",
                    Email = employee.ContactInformation.Email,
                    DisplayName = employee.LegalName.FullName
                });

                employee.PrincipalId = principal.PrincipalId;
                successCount++;

                if (successCount % 100 == 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Migrated {Count} employees...", successCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create principal for employee {EmployeeId}", employee.Id);
                failureCount++;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Migration complete. Success: {SuccessCount}, Failures: {FailureCount}",
            successCount, failureCount);
    }
}
```

## Configuration Changes

**File**: `Maliev.EmployeeService.Api/appsettings.json`

**Add**:
```json
{
  "ExternalServices": {
    "IAM": {
      "BaseUrl": "http://iam-service:8080",
      "ServiceAccountToken": "<secret-from-vault>",
      "Timeout": 5000
    }
  },
  "Features": {
    "PrincipalBasedAuthEnabled": false
  }
}
```

## API Contract Changes

### New Endpoint
```http
GET /employees/v1/employees/by-principal/{principalId}
Response: EmployeeProfileResponse (same as GET /employees/{id})
```

### Modified Endpoint
```http
POST /employees/v1/auth/validate
Request: { "email": "...", "password": "..." }
Response: {
  "isValid": true,
  "principalId": "uuid",  // Changed from "userId"
  "email": "...",
  "name": "..."
}
```

## Testing Requirements

### Unit Tests
- GetByPrincipalId returns employee when exists
- GetByPrincipalId returns null when not found
- CreateEmployee creates principal in IAM
- CreateEmployee handles IAM failure gracefully
- ValidateCredentials returns principal_id
- CurrentUserService extracts principal_id from JWT

### Integration Tests
- Create employee flow with IAM integration
- Get employee by principal_id
- Migration script creates principals correctly
- Validation returns principal_id after migration
- CurrentUserService resolves employee from principal_id

## Success Criteria

- [ ] All employees have principal_id
- [ ] GET /by-principal/{principalId} works
- [ ] New employees get principal automatically
- [ ] Credential validation returns principal_id
- [ ] CurrentUserService uses principal_id
- [ ] No User table code remains
- [ ] All tests pass
- [ ] Migration script succeeds
- [ ] Production verification complete

## Dependencies

### External Services
- IAM service must be deployed and accessible
- IAM principal creation endpoint available

### Internal Dependencies
- AuthService must be updated to use principal_id
- Migration must complete before making principal_id NOT NULL
