# Quickstart Guide: Employee Service

**Feature Branch**: `001-employee-service-comprehensive`
**Created**: 2025-10-12
**Status**: Development Guide

## Overview

This guide helps developers set up the Employee Service locally for development and testing. The service manages comprehensive HR master data including employee profiles, organizational hierarchy, leave management, and compliance tracking.

---

## Prerequisites

### Required Software
- **.NET 9.0 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/9.0))
- **Docker Desktop** ([Download](https://www.docker.com/products/docker-desktop))
- **Git** (for version control)
- **Visual Studio 2022** or **VS Code** with C# extension
- **kubectl** (for Kubernetes operations)
- **PostgreSQL client** (psql) - optional for database exploration

### Verify Installation
```bash
dotnet --version  # Should show 9.x.x
docker --version  # Should show 20.x or higher
git --version
```

---

## Local Development Setup

### Step 1: Clone the Repository
```bash
git clone https://github.com/MALIEV-Co-Ltd/Maliev.EmployeeService.git
cd Maliev.EmployeeService
git checkout 001-employee-service-comprehensive
```

### Step 2: Start Infrastructure Dependencies
Start PostgreSQL and RabbitMQ using Docker Compose:

```bash
# Create docker-compose.dev.yml
cat > docker-compose.dev.yml << EOF
version: '3.8'

services:
  postgres:
    image: postgres:18-alpine
    container_name: employee-service-postgres
    environment:
      POSTGRES_DB: employee_app_db
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: dev_password_123
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  rabbitmq:
    image: rabbitmq:3.12-management-alpine
    container_name: employee-service-rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    ports:
      - "5672:5672"   # AMQP port
      - "15672:15672" # Management UI
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: employee-service-redis
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  postgres_data:
EOF

# Start services
docker-compose -f docker-compose.dev.yml up -d

# Verify services are running
docker-compose -f docker-compose.dev.yml ps
```

**Access Management UIs**:
- RabbitMQ Management: http://localhost:15672 (guest/guest)
- PostgreSQL: `psql -h localhost -U postgres -d employee_app_db`

### Step 3: Configure Application Settings
Create `appsettings.Development.json` in `Maliev.EmployeeService.Api/`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=employee_app_db;User Id=postgres;Password=dev_password_123;"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Jwt": {
    "Authority": "https://api.maliev.com/auth",
    "Audience": "employee-service",
    "RequireHttpsMetadata": false
  },
  "CareerService": {
    "BaseUrl": "http://localhost:8081",
    "Timeout": 30
  },
  "Encryption": {
    "Key": "dev-encryption-key-32-characters-long-placeholder"
  }
}
```

### Step 4: Restore Dependencies
```bash
# Navigate to solution directory
cd Maliev.EmployeeService

# Restore NuGet packages
dotnet restore Maliev.EmployeeService.sln
```

### Step 5: Apply Database Migrations
```bash
# Set connection string environment variable
export EmployeeServiceDbContext="Server=localhost;Port=5432;Database=employee_app_db;User Id=postgres;Password=dev_password_123;"

# Apply migrations (creates database schema)
dotnet ef database update --project Maliev.EmployeeService.Infrastructure --startup-project Maliev.EmployeeService.Api

# Verify migration success
psql -h localhost -U postgres -d employee_app_db -c "\dt"
```

**Expected Tables**:
- Employees
- Departments
- DepartmentHierarchy
- EmergencyContacts
- LeaveBalances
- LeaveRequests
- LeaveApprovals
- CompensationRecords
- PerformanceReviews
- Goals
- TrainingRecords
- Skills
- Documents
- OnboardingChecklistItems
- OffboardingChecklistItems
- WorkAuthorizations
- AuditLogs
- Users

### Step 6: Seed Test Data (Optional)
```bash
# Run seeder to populate test data
dotnet run --project Maliev.EmployeeService.Api -- --seed

# This creates:
# - 3 departments (Engineering, HR, Finance)
# - 10 sample employees
# - Leave balances for all employees
# - Sample leave requests
```

### Step 7: Build and Run
```bash
# Build solution
dotnet build Maliev.EmployeeService.sln --no-restore

# Run API service
dotnet run --project Maliev.EmployeeService.Api

# Service should start on http://localhost:8080
```

**Verify Service is Running**:
```bash
# Health check
curl http://localhost:8080/employees/liveness
# Expected: "Healthy"

# Readiness check (validates database connection)
curl http://localhost:8080/employees/readiness
# Expected: JSON with status "Healthy"

# Swagger UI
# Open browser: http://localhost:8080/employees/swagger
```

---

## Running Tests

### Unit Tests
```bash
# Run all unit tests
dotnet test Maliev.EmployeeService.Tests/Unit --verbosity normal

# Run with coverage
dotnet test Maliev.EmployeeService.Tests/Unit --collect:"XPlat Code Coverage"
```

### Integration Tests
```bash
# Integration tests use Testcontainers (requires Docker running)
dotnet test Maliev.EmployeeService.Tests/Integration --verbosity normal

# Note: Integration tests will automatically start PostgreSQL and RabbitMQ containers
```

### Contract Tests
```bash
# Validate OpenAPI schema compliance
dotnet test Maliev.EmployeeService.Tests/Contract --verbosity normal
```

### Run All Tests
```bash
dotnet test Maliev.EmployeeService.sln --verbosity normal
```

---

## API Testing with Swagger

### Access Swagger UI
1. Start the service: `dotnet run --project Maliev.EmployeeService.Api`
2. Open browser: http://localhost:8080/employees/swagger
3. Click "Authorize" button
4. Enter JWT token (obtain from Auth Service or use mock token in development)

### Sample API Calls

**Create Employee** (requires HR role):
```bash
curl -X POST http://localhost:8080/employees/v1/employees \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "employeeNumber": "EMP-0501",
    "legalFirstName": "Somchai",
    "legalLastName": "Prasert",
    "dateOfBirth": "1990-05-15",
    "nationality": "Thai",
    "employmentType": "FullTime",
    "jobTitle": "Software Engineer",
    "departmentId": "dept-uuid-here",
    "startDate": "2025-11-01",
    "email": "somchai@maliev.co.th"
  }'
```

**Get Employee Profile**:
```bash
curl http://localhost:8080/employees/v1/employees/{employee-id} \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Submit Leave Request**:
```bash
curl -X POST http://localhost:8080/employees/v1/leave/requests \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "leaveType": "AnnualLeave",
    "startDate": "2025-11-15",
    "endDate": "2025-11-19",
    "reason": "Family vacation"
  }'
```

---

## Database Management

### Create New Migration
```bash
# After modifying entity models, create migration
dotnet ef migrations add MigrationName \
  --project Maliev.EmployeeService.Infrastructure \
  --startup-project Maliev.EmployeeService.Api \
  --output-dir Migrations

# Apply migration
dotnet ef database update \
  --project Maliev.EmployeeService.Infrastructure \
  --startup-project Maliev.EmployeeService.Api
```

### Rollback Migration
```bash
# Rollback to specific migration
dotnet ef database update PreviousMigrationName \
  --project Maliev.EmployeeService.Infrastructure \
  --startup-project Maliev.EmployeeService.Api

# Remove last migration (if not applied to production)
dotnet ef migrations remove \
  --project Maliev.EmployeeService.Infrastructure \
  --startup-project Maliev.EmployeeService.Api
```

### View Migration History
```bash
# List applied migrations
dotnet ef migrations list \
  --project Maliev.EmployeeService.Infrastructure \
  --startup-project Maliev.EmployeeService.Api

# Generate SQL script for migration
dotnet ef migrations script \
  --project Maliev.EmployeeService.Infrastructure \
  --startup-project Maliev.EmployeeService.Api \
  --output migration.sql
```

### Database Exploration
```bash
# Connect to PostgreSQL
psql -h localhost -U postgres -d employee_app_db

# Common queries
\dt                              # List all tables
\d+ Employees                    # Describe Employees table
SELECT * FROM Employees LIMIT 10; # View sample data
SELECT * FROM AuditLogs ORDER BY Timestamp DESC LIMIT 20; # Recent audit logs
```

---

## Docker Development

### Build Docker Image
```bash
# Build image
docker build -t maliev-employee-service:dev -f Maliev.EmployeeService.Api/Dockerfile .

# Run container
docker run -d \
  --name employee-service \
  -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Port=5432;Database=employee_app_db;User Id=postgres;Password=dev_password_123;" \
  -e RabbitMQ__Host="host.docker.internal" \
  -e Redis__ConnectionString="host.docker.internal:6379" \
  maliev-employee-service:dev

# View logs
docker logs -f employee-service

# Stop container
docker stop employee-service
docker rm employee-service
```

### Run Full Stack with Docker Compose
```bash
# Create docker-compose.yml with API + dependencies
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down
```

---

## Troubleshooting

### PostgreSQL Connection Issues
```bash
# Check PostgreSQL is running
docker ps | grep postgres

# Test connection
psql -h localhost -U postgres -d employee_app_db -c "SELECT 1;"

# View PostgreSQL logs
docker logs employee-service-postgres

# Common fix: Restart PostgreSQL
docker restart employee-service-postgres
```

### RabbitMQ Connection Issues
```bash
# Check RabbitMQ is running
docker ps | grep rabbitmq

# Access management UI
# http://localhost:15672 (guest/guest)

# View RabbitMQ logs
docker logs employee-service-rabbitmq

# Common fix: Restart RabbitMQ
docker restart employee-service-rabbitmq
```

### Migration Errors
```bash
# If migration fails, check database state
psql -h localhost -U postgres -d employee_app_db -c "SELECT * FROM __EFMigrationsHistory;"

# Drop database and recreate (WARNING: Deletes all data)
psql -h localhost -U postgres -c "DROP DATABASE employee_app_db;"
psql -h localhost -U postgres -c "CREATE DATABASE employee_app_db;"
dotnet ef database update --project Maliev.EmployeeService.Infrastructure --startup-project Maliev.EmployeeService.Api
```

### Build Errors
```bash
# Clean solution
dotnet clean Maliev.EmployeeService.sln

# Remove bin/obj directories
find . -name "bin" -o -name "obj" | xargs rm -rf

# Restore and rebuild
dotnet restore Maliev.EmployeeService.sln
dotnet build Maliev.EmployeeService.sln --no-restore
```

### Port Already in Use
```bash
# Find process using port 8080
lsof -i :8080  # macOS/Linux
netstat -ano | findstr :8080  # Windows

# Kill process
kill -9 <PID>  # macOS/Linux
taskkill /PID <PID> /F  # Windows

# Or change port in launchSettings.json
```

---

## Development Workflow

### Feature Development
1. **Create Feature Branch**:
   ```bash
   git checkout -b feature/user-story-description
   ```

2. **Write Tests First** (TDD):
   - Unit tests for business logic
   - Integration tests for repositories
   - Contract tests for API endpoints

3. **Implement Feature**:
   - Domain entities
   - Command/query handlers
   - Controllers
   - Database migrations

4. **Run Tests**:
   ```bash
   dotnet test Maliev.EmployeeService.sln --verbosity normal
   ```

5. **Manual Testing**:
   - Use Swagger UI for API testing
   - Verify database changes with psql
   - Check RabbitMQ messages in management UI

6. **Commit Changes**:
   ```bash
   git add .
   git commit -m "feat: implement user story description"
   git push origin feature/user-story-description
   ```

### Code Quality Checks
```bash
# Format code
dotnet format Maliev.EmployeeService.sln

# Run linter
dotnet build Maliev.EmployeeService.sln --no-incremental /p:TreatWarningsAsErrors=true

# Check for security vulnerabilities
dotnet list package --vulnerable
```

---

## Environment Variables Reference

### Required for Local Development
```bash
# Database
ConnectionStrings__DefaultConnection="Server=localhost;Port=5432;Database=employee_app_db;User Id=postgres;Password=dev_password_123;"

# RabbitMQ
RabbitMQ__Host="localhost"
RabbitMQ__Port=5672
RabbitMQ__Username="guest"
RabbitMQ__Password="guest"

# Redis
Redis__ConnectionString="localhost:6379"

# JWT Authentication
Jwt__Authority="https://api.maliev.com/auth"
Jwt__Audience="employee-service"
Jwt__RequireHttpsMetadata=false

# Career Service Integration
CareerService__BaseUrl="http://localhost:8081"
CareerService__Timeout=30

# Encryption (Development only - use Secret Manager in production)
Encryption__Key="dev-encryption-key-32-characters-long-placeholder"
```

### Optional Environment Variables
```bash
# Logging
Serilog__MinimumLevel__Default="Information"
Serilog__MinimumLevel__Override__Microsoft="Warning"

# Performance
AspNetCore__KestrelServerLimits__MaxConcurrentConnections=1000
AspNetCore__KestrelServerLimits__MaxRequestBodySize=10485760

# Feature Flags
FeatureFlags__EnableBackgroundJobs=true
FeatureFlags__EnableCaching=true
```

---

## Useful Commands Cheat Sheet

```bash
# Start development environment
docker-compose -f docker-compose.dev.yml up -d

# Run API
dotnet run --project Maliev.EmployeeService.Api

# Run tests
dotnet test Maliev.EmployeeService.sln

# Apply migrations
dotnet ef database update --project Maliev.EmployeeService.Infrastructure --startup-project Maliev.EmployeeService.Api

# Create migration
dotnet ef migrations add MigrationName --project Maliev.EmployeeService.Infrastructure --startup-project Maliev.EmployeeService.Api

# View logs
docker-compose -f docker-compose.dev.yml logs -f

# Clean and rebuild
dotnet clean && dotnet build --no-restore

# Format code
dotnet format Maliev.EmployeeService.sln

# Stop development environment
docker-compose -f docker-compose.dev.yml down
```

---

## Next Steps

- **Review Architecture**: Read `research.md` for architectural decisions
- **Understand Data Model**: Review `data-model.md` for entity relationships
- **API Contracts**: Explore `contracts/` for OpenAPI specifications
- **Implementation Tasks**: Check `tasks.md` for prioritized development tasks
- **Testing Strategy**: Follow TDD approach outlined in `research.md`

---

## Support and Resources

- **Documentation**: https://github.com/MALIEV-Co-Ltd/Maliev.EmployeeService
- **Issue Tracker**: https://github.com/MALIEV-Co-Ltd/Maliev.EmployeeService/issues
- **Architecture Review**: Schedule with Tech Lead before implementing complex features

---

**Document Status**: Complete
**Last Updated**: 2025-10-12
