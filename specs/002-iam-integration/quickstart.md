# Quickstart: IAM Integration

## Development Setup

### 1. Configure IAM Settings
Update `appsettings.Development.json` with the local IAM service endpoint:

```json
{
  "ExternalServices": {
    "IAM": {
      "BaseUrl": "http://localhost:8081",
      "ServiceAccountToken": "dev-token"
    }
  }
}
```

### 2. Run Database Migrations
Apply the new `PrincipalId` column:
```bash
dotnet ef database update --project Maliev.EmployeeService.Infrastructure --startup-project Maliev.EmployeeService.Api
```

## Running the Migration Script

To backfill existing employees with IAM principals:
```bash
dotnet run --project Maliev.EmployeeService.Api -- --migrate-principals
```

## Verification Steps

### 1. Check Migration Status
Run the following SQL to see migration progress:
```sql
SELECT COUNT(*) as total_employees, 
       COUNT(principal_id) as migrated_employees 
FROM employee.employees;
```

### 2. Test Principal Lookup
```bash
curl -X GET http://localhost:8080/employee/v1/employees/by-principal/{YOUR_UUID}
```

### 3. Test Auth Validation
```bash
curl -X POST http://localhost:8080/employee/v1/auth/validate \
     -H "Content-Type: application/json" \
     -d '{"email":"test@example.com", "password":"password123"}'
```
