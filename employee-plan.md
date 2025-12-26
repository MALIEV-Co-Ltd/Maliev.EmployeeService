# EmployeeService Implementation Plan - Principal-First Migration

## Overview
Migrate EmployeeService from custom User entity to principal-first model. Implementation follows similar pattern to CustomerService migration.

## Phase 1: Add principal_id Column (Week 2, Day 1)
**Goal**: Add principal_id column without breaking existing functionality

### Tasks
1. Create EF Core migration
   ```bash
   cd Maliev.EmployeeService.Data
   dotnet ef migrations add AddPrincipalIdToEmployees
   ```

2. Review and test migration
   - Verify adds principal_id UUID NULL
   - Verify creates index
   - Test in development

3. Deploy to production
   - Run migration during maintenance window
   - Verify deployment successful

**Estimated Time**: 2 hours

**Files Modified**:
- `Maliev.EmployeeService.Domain/Entities/Employee.cs`
- `Maliev.EmployeeService.Data/Migrations/YYYYMMDDHHMMSS_AddPrincipalIdToEmployees.cs`

## Phase 2: Create IAM Client (Week 2, Days 1-2)
**Goal**: Implement HTTP client for IAM principal creation

### Tasks
1. Add Models
   - `Infrastructure/IAM/CreatePrincipalRequest.cs`
   - `Infrastructure/IAM/CreatePrincipalResponse.cs`

2. Create IAM Client
   - `Infrastructure/IAM/IIAMClient.cs`
   - `Infrastructure/IAM/IAMClient.cs`

3. Register in Program.cs
   ```csharp
   builder.Services.AddHttpClient<IIAMClient, IAMClient>(client =>
   {
       var iamUrl = builder.Configuration["ExternalServices:IAM:BaseUrl"];
       client.BaseAddress = new Uri(iamUrl!);
       client.Timeout = TimeSpan.FromSeconds(5);
   });
   ```

4. Add configuration
   - Update appsettings.json

5. Write unit tests
   - Mock HTTP responses
   - Test success/failure scenarios

**Estimated Time**: 3 hours

**Files Created**:
- `Maliev.EmployeeService.Infrastructure/IAM/CreatePrincipalRequest.cs`
- `Maliev.EmployeeService.Infrastructure/IAM/CreatePrincipalResponse.cs`
- `Maliev.EmployeeService.Infrastructure/IAM/IIAMClient.cs`
- `Maliev.EmployeeService.Infrastructure/IAM/IAMClient.cs`
- `Maliev.EmployeeService.Tests/Unit/IAMClientTests.cs`

## Phase 3: Backfill Migration Script (Week 2, Days 2-3)
**Goal**: Create principals for existing employees

### Tasks
1. Create migration script
   ```csharp
   // Scripts/MigrateEmployeesToPrincipalsScript.cs
   public class MigrateEmployeesToPrincipalsScript
   {
       public async Task ExecuteAsync() { ... }
   }
   ```

2. Implement batch processing
   - Load employees without principal_id
   - Create principal for each
   - Update with principal_id
   - Commit in batches of 100

3. Add CLI command
   ```csharp
   if (args.Contains("--migrate-principals"))
   {
       await app.Services.GetRequiredService<MigrateEmployeesToPrincipalsScript>().ExecuteAsync();
       return;
   }
   ```

4. Test in development and staging

5. Document procedure

**Estimated Time**: 4 hours

**Files Created**:
- `Maliev.EmployeeService/Scripts/MigrateEmployeesToPrincipalsScript.cs`
- `Maliev.EmployeeService/Scripts/MIGRATION_RUNBOOK.md`

## Phase 4: Update Employee Creation (Week 2, Day 3)
**Goal**: New employees automatically get principals

### Tasks
1. Modify EmployeeService.CreateEmployeeAsync
   - Call IAM before creating employee
   - Set principal_id on employee
   - Handle failures

2. Add feature flag check
   ```csharp
   if (_configuration.GetValue<bool>("Features:PrincipalBasedAuthEnabled"))
   {
       var principal = await _iamClient.CreatePrincipalAsync(...);
       employee.PrincipalId = principal.PrincipalId;
   }
   ```

3. Write unit tests
   - Mock IAM client
   - Test creation with principal
   - Test IAM failure handling

4. Write integration tests
   - End-to-end employee creation
   - Verify principal created

**Estimated Time**: 3 hours

**Files Modified**:
- `Maliev.EmployeeService.Application/Services/EmployeeService.cs`
- `Maliev.EmployeeService.Tests/Unit/EmployeeServiceTests.cs`
- `Maliev.EmployeeService.Tests/Integration/EmployeeCreationTests.cs`

## Phase 5: Add GetByPrincipalId Endpoint (Week 2, Day 4)
**Goal**: Enable lookups by principal_id

### Tasks
1. Add service method
   ```csharp
   public async Task<EmployeeProfileResponse?> GetByPrincipalIdAsync(Guid principalId, CancellationToken ct)
   {
       var employee = await _context.Employees
           .Where(e => e.PrincipalId == principalId)
           .FirstOrDefaultAsync(ct);
       return employee == null ? null : MapToResponse(employee);
   }
   ```

2. Add controller endpoint
   ```csharp
   [HttpGet("by-principal/{principalId}")]
   public async Task<IActionResult> GetByPrincipalId(Guid principalId)
   {
       var employee = await _employeeService.GetByPrincipalIdAsync(principalId);
       if (employee == null)
           return NotFound();
       return Ok(employee);
   }
   ```

3. Add database index
   ```sql
   CREATE INDEX idx_employees_principal_lookup ON employees(principal_id);
   ```

4. Write tests

5. Update OpenAPI docs

**Estimated Time**: 2 hours

**Files Modified**:
- `Maliev.EmployeeService.Application/Services/IEmployeeService.cs`
- `Maliev.EmployeeService.Application/Services/EmployeeService.cs`
- `Maliev.EmployeeService.Api/Controllers/EmployeeProfileController.cs`
- `Maliev.EmployeeService.Tests/Integration/EmployeeControllerTests.cs`

## Phase 6: Update CurrentUserService (Week 2, Day 4)
**Goal**: Extract principal_id from JWT and lookup employee

### Tasks
1. Update ICurrentUserService interface
   ```csharp
   public interface ICurrentUserService
   {
       Guid? PrincipalId { get; }
       Task<Guid?> GetEmployeeIdAsync(CancellationToken ct = default);
       string? Email { get; }
       IEnumerable<string> Permissions { get; }
       bool HasPermission(string permission);
   }
   ```

2. Modify CurrentUserService implementation
   - Extract principal_id from JWT sub claim
   - Async method to lookup employee by principal_id
   - Extract permissions from JWT

3. Update all usages of CurrentUserService
   - Change EmployeeId property to GetEmployeeIdAsync() method
   - Update authorization checks to use permissions

4. Write unit tests
   - Mock HttpContext with JWT claims
   - Test principal_id extraction
   - Test employee lookup

**Estimated Time**: 3 hours

**Files Modified**:
- `Maliev.EmployeeService.Infrastructure/Authentication/ICurrentUserService.cs`
- `Maliev.EmployeeService.Infrastructure/Authentication/CurrentUserService.cs`
- `Maliev.EmployeeService.Api/Controllers/*.cs` (wherever CurrentUserService is used)
- `Maliev.EmployeeService.Tests/Unit/CurrentUserServiceTests.cs`

## Phase 7: Update Credential Validation (Week 2, Day 4)
**Goal**: Return principal_id in validation response

### Tasks
1. Update response model
   ```csharp
   public record CredentialValidationResponse
   {
       public bool IsValid { get; init; }
       public Guid PrincipalId { get; init; }  // Changed
       public string Email { get; init; }
       public string Name { get; init; }
   }
   ```

2. Modify validation endpoint
   - Lookup employee with principal_id
   - Return principal_id in response

3. Write tests

**Estimated Time**: 2 hours

**Files Modified**:
- `Maliev.EmployeeService.Api/Models/CredentialValidationResponse.cs`
- `Maliev.EmployeeService.Api/Controllers/EmployeeAuthController.cs`
- `Maliev.EmployeeService.Tests/Integration/CredentialValidationTests.cs`

## Phase 8: Production Migration (Week 3, Day 1)
**Goal**: Execute backfill migration in production

### Pre-Migration Checklist
- [ ] IAM service deployed
- [ ] Database backup completed
- [ ] Migration tested in staging
- [ ] Rollback procedure ready
- [ ] Monitoring ready

### Migration Steps
1. Create database backup
2. Run migration script
3. Monitor progress
4. Run verification queries
5. Test sample lookups
6. Document results

**Estimated Time**: 2-3 hours

## Phase 9: Enable Principal-Based Auth (Week 3, Day 2)
**Goal**: Enable feature flag

### Tasks
1. Deploy code (flag OFF)
2. Enable feature flag in production
3. Create test employee
4. Monitor for 24 hours

**Estimated Time**: 1 hour + monitoring

## Phase 10: Cleanup (Week 4)
**Goal**: Remove User table and related code

### Tasks
1. Make principal_id NOT NULL
2. Add unique constraint
3. Remove User entity code
4. Drop users table
5. Remove feature flags
6. Update documentation

**Estimated Time**: 3 hours

**Files Removed**:
- `Maliev.EmployeeService.Domain/Entities/User.cs`

## Testing Strategy

### Unit Tests
- IAMClient: Principal creation
- EmployeeService: Create with principal, GetByPrincipalId
- CurrentUserService: Principal extraction, employee lookup
- CredentialValidation: Returns principal_id

### Integration Tests
- End-to-end employee creation
- Get by principal_id
- Credential validation
- Migration script

### Manual Testing
- [ ] Create employee → principal created
- [ ] Get by principal_id → works
- [ ] Validate credentials → returns principal_id
- [ ] CurrentUserService → resolves employee
- [ ] Migration → all employees migrated

## Rollback Procedures

### Rollback from Code Deployment
- Deploy previous version
- principal_id column remains (no harm)

### Rollback from Migration
- Restore from backup
- Re-deploy previous code

### Rollback from Cleanup
NOT POSSIBLE - must restore from backup

## Success Criteria

- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] All employees have principal_id
- [ ] Migration succeeds
- [ ] Employee creation includes principal
- [ ] GET /by-principal/{id} works
- [ ] Credential validation returns principal_id
- [ ] CurrentUserService uses principal_id
- [ ] No User table remains
- [ ] Production running 1 week
- [ ] Documentation updated

## Total Estimated Time

- Phase 1: 2 hours
- Phase 2: 3 hours
- Phase 3: 4 hours
- Phase 4: 3 hours
- Phase 5: 2 hours
- Phase 6: 3 hours
- Phase 7: 2 hours
- Phase 8: 3 hours
- Phase 9: 1 hour
- Phase 10: 3 hours

**Total: ~26 hours (~3-4 days)**

## Dependencies

- IAM service deployed before Phase 3
- Database backup before Phase 8
- 1 week soak time before Phase 10
