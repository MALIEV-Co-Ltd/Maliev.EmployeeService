# Employee Service

Comprehensive HR and employee lifecycle management microservice for Maliev Co. Ltd.

## Overview

The Employee Service is a .NET 9.0 microservice that manages all aspects of the employee lifecycle including:

- **Employee Profile Management** - Self-service profile updates and emergency contacts
- **Leave Management** - Leave requests, approvals, and balance tracking
- **Organizational Structure** - Departments, teams, and reporting hierarchies
- **Performance Management** - Reviews, goals, and feedback
- **Training & Certification** - Training records and compliance tracking
- **Document Management** - Secure document storage with version control
- **Onboarding/Offboarding** - Automated workflows for new hires and departures
- **Work Authorization** - Visa and work permit tracking
- **Reporting & Analytics** - HR metrics and compliance reports

## Architecture

- **Framework**: ASP.NET Core 9.0
- **Database**: PostgreSQL 16+ with Entity Framework Core
- **Messaging**: RabbitMQ via MassTransit (for integration events)
- **Caching**: Redis (optional, falls back to in-memory)
- **Authentication**: JWT Bearer tokens
- **Authorization**: Role-based and resource-based policies
- **API Documentation**: OpenAPI with Scalar UI (development only) and comprehensive XML comments
- **Monitoring**: Prometheus metrics, Grafana dashboards
- **Logging**: Serilog with structured logging and correlation IDs

## Authentication & Authorization

### JWT Token Requirements

The Employee Service uses JWT (JSON Web Token) Bearer authentication for all protected endpoints.

#### Token Structure

JWT tokens must be obtained from the Auth Service and include the following claims:

```json
{
  "sub": "employee-guid",
  "email": "employee@maliev.co.th",
  "role": "Employee|Manager|HR|Admin",
  "name": "John Doe",
  "employeeNumber": "EMP001",
  "iss": "https://maliev.co.th",
  "aud": "employee-service",
  "exp": 1234567890,
  "iat": 1234567890
}
```

**Required Claims**:
- `sub` - Subject (Employee ID as GUID)
- `email` - Employee email address
- `role` - User role (Employee, Manager, HR, or Admin)
- `employeeNumber` - Employee number for logging and auditing
- `iss` - Issuer (must match JWT_ISSUER configuration)
- `aud` - Audience (must be "employee-service")
- `exp` - Expiration timestamp
- `iat` - Issued at timestamp

#### Obtaining a JWT Token

1. **Authenticate with Auth Service**:

```bash
POST https://api.maliev.co.th/auth/login
Content-Type: application/json

{
  "email": "john.doe@maliev.co.th",
  "password": "your-password"
}
```

Response:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh-token-here",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

2. **Use the Access Token** in subsequent requests:

```bash
GET https://api.maliev.co.th/employees/v1/profile/{employeeId}/profile
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Authorization Roles

The service supports four primary roles with hierarchical permissions:

#### 1. Employee (Base Role)
- View own profile and emergency contacts
- Update limited profile fields (personal email, phone, preferred name)
- Manage own emergency contacts
- View own leave balances and requests
- Submit leave requests
- Cancel own pending leave requests
- View own team memberships
- View own documents
- Update own skills

#### 2. Manager
- All Employee permissions
- View direct reports' profiles
- View team member leave requests
- Approve/reject leave requests for direct reports
- View organizational chart for their subtree
- Manage team assignments
- Create and update teams

#### 3. HR
- All Manager permissions
- View all employee profiles
- Create new employees
- Transfer employees between departments
- Manage all leave requests
- Create and manage departments
- Record compensation changes
- Manage benefits enrollment
- Create performance reviews
- Upload and manage employee documents
- Manage work authorizations
- Access all reports and analytics

#### 4. Admin
- All HR permissions
- Full system access
- Manage system configuration
- Access audit logs
- Perform bulk operations

### Authorization Policies

The service implements several authorization policies beyond basic role checks:

#### Policy: RequireHROrManager
- Required for leave approval endpoints
- Allows both HR and Manager roles

#### Policy: RequireHROrAdmin
- Required for sensitive operations like compensation changes
- Allows both HR and Admin roles

#### Policy: RequireAdminRole
- Required for system administration endpoints
- Allows only Admin role

#### Resource-Based Authorization
- Employees can only access their own resources (profile, contacts, leave requests)
- Managers can access direct reports' resources
- HR and Admin have broader access

### Token Validation

The service validates JWT tokens with the following parameters:

- **Validate Issuer**: Yes (must match JWT_ISSUER)
- **Validate Audience**: Yes (must be "employee-service")
- **Validate Lifetime**: Yes (tokens must not be expired)
- **Validate Signature**: Yes (using JWT_SECRET_KEY)
- **Clock Skew**: 5 minutes (allows for time differences between services)
- **Require HTTPS**: Yes (except in development)

### Authentication Flow

```
┌─────────┐           ┌──────────────┐          ┌─────────────────┐
│ Client  │           │ Auth Service │          │ Employee Service│
└────┬────┘           └──────┬───────┘          └────────┬────────┘
     │                       │                           │
     │ 1. POST /auth/login   │                           │
     │──────────────────────>│                           │
     │                       │                           │
     │ 2. JWT Token          │                           │
     │<──────────────────────│                           │
     │                       │                           │
     │ 3. GET /profile with Bearer token                 │
     │───────────────────────────────────────────────────>│
     │                       │                           │
     │                       │  4. Validate token        │
     │                       │  (signature, exp, claims) │
     │                       │                           │
     │ 5. Profile data       │                           │
     │<───────────────────────────────────────────────────│
     │                       │                           │
```

### Token Refresh

JWT access tokens expire after 1 hour. Use the refresh token to obtain a new access token:

```bash
POST https://api.maliev.co.th/auth/refresh
Content-Type: application/json

{
  "refreshToken": "your-refresh-token"
}
```

Response:
```json
{
  "accessToken": "new-jwt-token",
  "refreshToken": "new-refresh-token",
  "expiresIn": 3600
}
```

### Security Headers

The service includes the following security headers on all responses:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Referrer-Policy: no-referrer`
- `Content-Security-Policy: default-src 'self'`
- `Strict-Transport-Security: max-age=31536000; includeSubDomains; preload` (production only)

### CORS Configuration

CORS is configured to allow requests from:
- `http://localhost:3000` (development)
- `https://maliev.co.th` (production)

Configure additional origins via `CORS_ALLOWED_ORIGINS` environment variable (comma-separated).

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- PostgreSQL 16+
- Docker (optional, for Redis and RabbitMQ)
- Git

### Local Development Setup

1. **Clone the repository**:

```bash
git clone https://github.com/MALIEV-Co-Ltd/Maliev.EmployeeService.git
cd Maliev.EmployeeService
```

2. **Set up PostgreSQL database**:

```bash
# Using Docker
docker run --name employee-db -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:16

# Create database
psql -U postgres -c "CREATE DATABASE employee_app_db;"
```

3. **Run database migrations**:

```bash
dotnet ef database update --project Maliev.EmployeeService.Infrastructure --startup-project Maliev.EmployeeService.Api
```

4. **Configure environment variables** (optional, has development defaults):

```bash
export DATABASE_URL="Host=localhost;Port=5432;Database=employee_app_db;Username=postgres;Password=postgres"
export JWT_SECRET_KEY="your-secret-key-min-32-characters"
export JWT_ISSUER="https://maliev.co.th"
export JWT_AUDIENCE="employee-service"
export REDIS_CONNECTION_STRING="localhost:6379"
export RABBITMQ_HOST="localhost"
export RABBITMQ_PORT="5672"
export RABBITMQ_USERNAME="guest"
export RABBITMQ_PASSWORD="guest"
```

5. **Run the service**:

```bash
dotnet run --project Maliev.EmployeeService.Api
```

The service will be available at `https://localhost:7001` (HTTPS) or `http://localhost:5000` (HTTP).

6. **Access API documentation (development only)**:

Navigate to `https://localhost:7001/employees/scalar/v1` to view the interactive Scalar API documentation.

### Running with Docker

```bash
# Build image
docker build -t maliev-employee-service:latest -f Maliev.EmployeeService.Api/Dockerfile .

# Run container
docker run -p 8080:8080 \
  -e DATABASE_URL="Host=host.docker.internal;Port=5432;Database=employee_app_db;Username=postgres;Password=postgres" \
  -e JWT_SECRET_KEY="your-secret-key" \
  maliev-employee-service:latest
```

## API Documentation

### Base URL

- **Development**: `http://localhost:5000/employees`
- **Production**: `https://api.maliev.co.th/employees`

### API Version

All endpoints are versioned and accessed via `/v1/` prefix.

### Endpoints

#### Employee Profile Management

```
GET    /v1/profile/{employeeId}/profile               - Get employee profile
PUT    /v1/profile/{employeeId}/profile               - Update employee profile
POST   /v1/profile/{employeeId}/emergency-contacts    - Create emergency contact
PUT    /v1/profile/{employeeId}/emergency-contacts/{contactId} - Update emergency contact
DELETE /v1/profile/{employeeId}/emergency-contacts/{contactId} - Delete emergency contact
```

#### Leave Management

```
GET    /v1/leave/balances/{employeeId}                - Get leave balances
GET    /v1/leave/requests/{employeeId}                - Get leave requests
GET    /v1/leave/pending-approvals                    - Get pending approvals (Manager/HR)
POST   /v1/leave/requests/{employeeId}                - Submit leave request
PUT    /v1/leave/requests/{leaveRequestId}/decision   - Approve/reject leave (Manager/HR)
PUT    /v1/leave/requests/{leaveRequestId}/cancel     - Cancel leave request
```

#### Team Management

```
GET    /v1/teams/{teamId}                             - Get team details
GET    /v1/teams/employee/{employeeId}                - Get employee teams
POST   /v1/teams                                      - Create team (Manager/HR)
POST   /v1/teams/{teamId}/members                     - Add team member (Manager/HR)
DELETE /v1/teams/{teamId}/members/{employeeId}        - Remove team member (Manager/HR)
```

#### Department Management

```
GET    /v1/departments                                - List all departments
GET    /v1/departments/{departmentId}                 - Get department details
GET    /v1/departments/{departmentId}/employees       - Get department employees
POST   /v1/departments                                - Create department (HR/Admin)
PUT    /v1/departments/{departmentId}                 - Update department (HR/Admin)
```

#### Document Management

```
GET    /v1/documents/employee/{employeeId}            - Get employee documents
POST   /v1/documents/employee/{employeeId}/upload     - Upload document
GET    /v1/documents/{documentId}/download            - Download document
POST   /v1/documents/{documentId}/versions            - Upload new version
```

For complete API documentation with request/response examples, visit the Scalar UI at `/employees/scalar/v1` (development environment only).

## Configuration

### Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `DATABASE_URL` | Yes | localhost connection | PostgreSQL connection string |
| `JWT_SECRET_KEY` | Yes (prod) | Dev key | Secret key for JWT signing (min 32 chars) |
| `JWT_ISSUER` | No | https://maliev.co.th | JWT token issuer |
| `JWT_AUDIENCE` | No | employee-service | JWT token audience |
| `REDIS_CONNECTION_STRING` | No | localhost:6379 | Redis connection string |
| `REDIS_ENABLED` | No | true | Enable/disable Redis caching |
| `RABBITMQ_HOST` | No | localhost | RabbitMQ host |
| `RABBITMQ_PORT` | No | 5672 | RabbitMQ port |
| `RABBITMQ_USERNAME` | No | guest | RabbitMQ username |
| `RABBITMQ_PASSWORD` | No | guest | RabbitMQ password |
| `RABBITMQ_ENABLED` | No | true | Enable/disable RabbitMQ messaging |
| `UPLOAD_SERVICE_URL` | No | http://localhost:8082 | Upload service URL for documents |
| `CAREER_SERVICE_URL` | No | http://localhost:8081 | Career service URL for integrations |
| `CORS_ALLOWED_ORIGINS` | No | localhost:3000,maliev.co.th | Comma-separated allowed origins |

### Google Secret Manager (Production)

In production, secrets are loaded from Google Secret Manager mounted at `/mnt/secrets`:

```yaml
# Kubernetes example
volumeMounts:
  - name: secrets
    mountPath: /mnt/secrets
    readOnly: true
volumes:
  - name: secrets
    csi:
      driver: secrets-store.csi.k8s.io
      readOnly: true
      volumeAttributes:
        secretProviderClass: "employee-service-secrets"
```

## Database Migrations

### Create a New Migration

```bash
dotnet ef migrations add MigrationName \
  --project Maliev.EmployeeService.Infrastructure \
  --startup-project Maliev.EmployeeService.Api
```

### Apply Migrations

```bash
# Local development
dotnet ef database update \
  --project Maliev.EmployeeService.Infrastructure \
  --startup-project Maliev.EmployeeService.Api

# With specific connection string
export DATABASE_URL="your-connection-string"
dotnet ef database update \
  --project Maliev.EmployeeService.Infrastructure \
  --startup-project Maliev.EmployeeService.Api
```

### Kubernetes Migration (Production)

```bash
# Port forward to PostgreSQL pod
kubectl port-forward -n maliev-prod postgres-cluster-1 5432:5432 &

# Set connection string
export DATABASE_URL="Server=localhost;Port=5432;Database=employee_db;User Id=postgres;Password=ACTUAL_PASSWORD;"

# Run migration
dotnet ef database update --project Maliev.EmployeeService.Infrastructure
```

## Testing

### Run All Tests

```bash
dotnet test Maliev.EmployeeService.sln --verbosity normal
```

### Run Unit Tests Only

```bash
dotnet test Maliev.EmployeeService.Tests/Maliev.EmployeeService.Tests.csproj \
  --filter "FullyQualifiedName~Unit"
```

### Run Integration Tests

```bash
dotnet test Maliev.EmployeeService.Tests/Maliev.EmployeeService.Tests.csproj \
  --filter "FullyQualifiedName~Integration"
```

### Code Coverage

```bash
dotnet test Maliev.EmployeeService.sln \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage

# Generate report
reportgenerator \
  -reports:./coverage/**/coverage.cobertura.xml \
  -targetdir:./coverage/report \
  -reporttypes:Html
```

## Monitoring & Observability

### Health Checks

- **Liveness**: `GET /employees/liveness` - Returns 200 if service is alive
- **Readiness**: `GET /employees/readiness` - Returns 200 if service is ready (DB, Redis, RabbitMQ healthy)

### Prometheus Metrics

Metrics available at `/employees/metrics`:

- **Technical Metrics**:
  - `http_requests_total` - Total HTTP requests by method, endpoint, status
  - `http_request_duration_seconds` - Request duration histogram
  - `database_query_duration_seconds` - Database query performance
  - `rabbitmq_publish_total` - Message publishing attempts
  - `circuit_breaker_state` - Circuit breaker state (open/closed/half-open)

- **Business Metrics**:
  - `employees_total` - Total employee count
  - `employees_by_status` - Employee count by employment status
  - `leave_requests_pending` - Pending leave request count
  - `leave_utilization_rate` - Leave utilization percentage
  - `onboarding_progress` - Onboarding completion percentage
  - `average_tenure_days` - Average employee tenure

### Grafana Dashboards

Pre-built dashboards available in `monitoring/` directory:

- `grafana-dashboard-api-metrics.json` - API performance and reliability
- `grafana-dashboard-database-performance.json` - Database performance and connection pooling

Import these into Grafana for comprehensive monitoring.

### Structured Logging

All logs include correlation IDs for request tracing:

```json
{
  "timestamp": "2025-10-18T10:30:00.123Z",
  "level": "Information",
  "message": "Employee profile updated",
  "correlationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "employeeId": "1a2b3c4d-5e6f-7890-abcd-ef1234567890",
  "endpoint": "PUT /v1/profile/{employeeId}/profile",
  "duration": 45.2,
  "statusCode": 200
}
```

## Deployment

### CI/CD Pipelines

The service uses GitHub Actions for CI/CD:

- `.github/workflows/ci-develop.yml` - Deploy to development on push to `develop`
- `.github/workflows/ci-staging.yml` - Deploy to staging on push to `staging`
- `.github/workflows/ci-main.yml` - Deploy to production on push to `main`

### GitOps Deployment

The service is deployed to Kubernetes via ArgoCD using GitOps practices:

1. **Code changes** pushed to GitHub
2. **GitHub Actions** builds Docker image and pushes to GCP Artifact Registry
3. **Kustomize** updates image tags in maliev-gitops repository
4. **ArgoCD** detects changes and deploys to Kubernetes cluster

### Kubernetes Resources

Base manifests located in `maliev-gitops/3-apps/employee-service/`:

- `base/deployment.yaml` - Deployment configuration
- `base/service.yaml` - Service configuration
- `base/servicemonitor.yaml` - Prometheus monitoring
- `overlays/development/` - Development environment overrides
- `overlays/staging/` - Staging environment overrides
- `overlays/production/` - Production environment overrides

## Security

### Data Encryption

- **At-Rest**: Sensitive fields (SSN, emergency contact info) encrypted using AES-256
- **In-Transit**: All communication over HTTPS/TLS 1.2+
- **Database**: PostgreSQL connection with SSL required in production

### Input Validation

- FluentValidation for all DTOs
- Global input sanitization filter (XSS prevention)
- SQL injection prevention via parameterized queries (EF Core)

### Rate Limiting

- 100 requests per minute per user
- Configured globally in Program.cs
- Applies to all authenticated endpoints

### Security Headers

- Strict Transport Security (HSTS)
- Content Security Policy (CSP)
- X-Frame-Options: DENY
- X-Content-Type-Options: nosniff

## Integration Events

The service publishes integration events to RabbitMQ for cross-service communication:

### Published Events

- `EmployeeCreatedIntegrationEvent` - New employee created
- `EmployeeOnboardingStartedIntegrationEvent` - Onboarding workflow initiated
- `EmployeeTerminatedIntegrationEvent` - Employee offboarded
- `DepartmentTransferredIntegrationEvent` - Employee transferred to new department
- `AccessRevocationRequiredIntegrationEvent` - Access revocation needed for terminated employee
- `OnboardingReminderNeededIntegrationEvent` - Onboarding task reminder

### Event Bus Configuration

Events are published via MassTransit with:
- **Retry Policy**: 3 attempts with exponential backoff (2s, 4s, 8s)
- **Circuit Breaker**: Opens after 5 failures (50% failure ratio), breaks for 30 seconds
- **Resilience**: Automatic recovery and health monitoring

## Contributing

1. Create feature branch from `develop`
2. Make changes with comprehensive tests
3. Ensure all tests pass: `dotnet test`
4. Ensure build succeeds: `dotnet build`
5. Create pull request to `develop`
6. Code review and approval required
7. Squash and merge after approval

## Support

For issues or questions:

- **GitHub Issues**: https://github.com/MALIEV-Co-Ltd/Maliev.EmployeeService/issues
- **Email**: dev@maliev.co.th
- **Documentation**: https://docs.maliev.co.th/employee-service

## License

Copyright © 2025 Maliev Co. Ltd. All rights reserved.
