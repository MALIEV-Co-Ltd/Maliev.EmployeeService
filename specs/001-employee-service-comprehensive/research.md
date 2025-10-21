# Research Document: Employee Service Architecture

**Feature Branch**: `001-employee-service-comprehensive`
**Created**: 2025-10-12
**Status**: Design Phase

## Executive Summary

This document captures architectural research and design decisions for the Employee Service microservice. The service manages comprehensive HR master data including employee profiles, organizational hierarchy, leave management, performance tracking, training records, and document management for Maliev Co. Ltd.

**Key Decisions**:
- Clean Architecture with CQRS pattern for command/query separation
- PostgreSQL with Entity Framework Core for persistence
- Event-driven integration using RabbitMQ
- Multi-project solution structure for separation of concerns
- AES-256 encryption for sensitive data at rest
- JWT-based authentication with role-based authorization

---

## Architectural Decisions

### 1. Clean Architecture Pattern

**Decision**: Implement Clean Architecture with clear separation of concerns across Domain, Application, Infrastructure, and API layers.

**Rationale**:
- **Testability**: Business logic isolated from infrastructure concerns, enabling comprehensive unit testing
- **Maintainability**: Clear boundaries between layers make codebase easier to navigate and modify
- **Flexibility**: Infrastructure (database, external services) can be swapped without affecting business rules
- **Alignment**: Follows enterprise patterns used across Maliev microservices

**Structure**:
```
Maliev.EmployeeService.Domain/       # Entities, Value Objects, Enums
Maliev.EmployeeService.Application/  # Use Cases, DTOs, Interfaces
Maliev.EmployeeService.Infrastructure/ # Repositories, DbContext, External Services
Maliev.EmployeeService.Api/          # Controllers, Middleware, Configuration
```

**Trade-offs**:
- More boilerplate code compared to monolithic structure
- Initial complexity for simple CRUD operations
- **Mitigation**: Use code generation tools for DTOs and mappings

---

### 2. CQRS (Command Query Responsibility Segregation)

**Decision**: Separate command operations (create/update/delete) from query operations (read) with distinct models and handlers.

**Rationale**:
- **Performance Optimization**: Queries optimized independently from commands (e.g., read-only projections, caching)
- **Scalability**: Commands and queries can scale independently based on load patterns
- **Security**: Different authorization rules for reads vs. writes (e.g., managers can read but not modify compensation)
- **Audit Trail**: Commands explicitly capture intent and can be logged comprehensively

**Implementation**:
- MediatR library for command/query dispatch
- Command handlers validate and persist changes
- Query handlers fetch data with optimized projections
- No domain entities exposed in query responses (DTOs only)

**Example**:
```csharp
// Command: Update emergency contact
public class UpdateEmergencyContactCommand : IRequest<Result>
{
    public Guid EmployeeId { get; set; }
    public Guid ContactId { get; set; }
    public string Name { get; set; }
    public string Relationship { get; set; }
    public string Phone { get; set; }
}

// Query: Get employee profile
public class GetEmployeeProfileQuery : IRequest<EmployeeProfileDto>
{
    public Guid EmployeeId { get; set; }
}
```

**Trade-offs**:
- Additional complexity from dual models
- Potential data duplication between command and query models
- **Mitigation**: Use AutoMapper forDTO conversions, maintain clear naming conventions

---

### 3. Repository Pattern for Data Access

**Decision**: Implement repository pattern as abstraction over Entity Framework Core.

**Rationale**:
- **Testability**: Repositories can be mocked for unit testing command/query handlers
- **Encapsulation**: Complex queries and data access logic centralized
- **Flexibility**: Easier to switch ORM or add caching layer
- **Consistency**: Standard pattern across Maliev microservices

**Implementation**:
```csharp
public interface IEmployeeRepository
{
    Task<Employee> GetByIdAsync(Guid id);
    Task<IEnumerable<Employee>> GetByDepartmentAsync(Guid departmentId);
    Task AddAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task<bool> ExistsAsync(string employeeId);
}
```

**Trade-offs**:
- Extra abstraction layer over EF Core
- Repository methods may mirror EF Core methods
- **Mitigation**: Only create repository methods when needed, avoid generic repositories

---

### 4. Event-Driven Integration with RabbitMQ

**Decision**: Use asynchronous messaging via RabbitMQ for inter-service communication and integration events.

**Rationale**:
- **Decoupling**: Services don't need direct knowledge of each other
- **Resilience**: Message queuing provides automatic retry and durability
- **Scalability**: Asynchronous processing prevents blocking operations
- **Audit Trail**: All integration events logged for debugging

**Integration Events**:
- **Inbound**: `CandidateAccepted` from Career Service (triggers onboarding)
- **Outbound**:
  - `EmployeeCreated` to Payroll Service, Access Control Service
  - `EmployeeStatusChanged` to all dependent services
  - `LeaveRequestApproved` to Time Tracking Service
  - `EmployeeTerminated` to IT, Facilities, Access Control

**Implementation**:
- MassTransit library for RabbitMQ abstraction
- Saga pattern for complex workflows (onboarding, offboarding)
- Dead letter queues for failed message handling
- Idempotent message processing (deduplication)

**Trade-offs**:
- Eventual consistency between services
- Increased debugging complexity (distributed tracing required)
- **Mitigation**: Correlation IDs, comprehensive logging, message replay capabilities

---

### 5. Multi-Level Leave Approval Workflow

**Decision**: Implement configurable multi-level approval workflow for leave requests.

**Rationale**:
- **Business Requirement**: Extended leave or exceptional cases require senior management approval
- **Flexibility**: Different approval chains based on leave type, duration, or employee level
- **Audit Trail**: Complete approval history with timestamps and comments

**Workflow Design**:
```
Employee submits leave request
  ↓
Direct manager reviews (Level 1)
  ↓ (if approved AND duration > 10 days OR negative balance)
Department head reviews (Level 2)
  ↓ (if approved AND duration > 30 days)
HR specialist reviews (Level 3)
  ↓
Final approval/denial
```

**Implementation**:
- `LeaveApproval` entity tracks approval chain
- Workflow state machine using Stateless library
- Configurable approval rules in database
- Email notifications at each approval stage

**Trade-offs**:
- Complex state management
- Potential bottlenecks if approvers unavailable
- **Mitigation**: Approval delegation, timeout escalation, mobile notifications

---

### 6. Background Job Processing with Hosted Services

**Decision**: Use ASP.NET Core Hosted Services for scheduled background tasks.

**Rationale**:
- **Native Integration**: Built into ASP.NET Core, no external dependencies
- **Lifecycle Management**: Automatic start/stop with application
- **Simplicity**: Sufficient for monthly accruals, daily reminders, expiration checks

**Background Jobs**:
- **Monthly Leave Accrual**: Calculate and apply leave accruals on 1st of each month
- **Daily Expiration Checks**: Scan for expiring work permits, certifications, probation periods
- **Hourly Notification Processing**: Send queued email notifications
- **Daily Compliance Reports**: Generate overnight reports for HR dashboard

**Implementation**:
```csharp
public class LeaveAccrualHostedService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessMonthlyAccruals();
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
```

**Trade-offs**:
- No built-in distributed scheduling (single instance)
- Manual failure recovery
- **Mitigation**: Use distributed lock (Redis) for multi-instance deployments, comprehensive error logging

---

### 7. Data Encryption Strategy

**Decision**: Implement field-level encryption for sensitive data using AES-256 with key management via Google Secret Manager.

**Rationale**:
- **Compliance**: PDPA, GDPR require encryption at rest for personal data
- **Security**: Protects against database breaches
- **Auditability**: Encryption/decryption logged

**Encrypted Fields**:
- Employee salary information
- Thai national ID
- Passport numbers
- Bank account details (if stored)
- Disciplinary records

**Implementation**:
- EF Core value converter for transparent encryption/decryption
- Encryption key rotation support
- Search on encrypted fields requires tokenization or hashing

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Compensation>()
        .Property(c => c.SalaryAmount)
        .HasConversion(new EncryptedConverter(_encryptionService));
}
```

**Trade-offs**:
- Cannot query encrypted fields directly (no WHERE clause on salary)
- Performance overhead for encryption/decryption
- **Mitigation**: Create searchable tokens/hashes, use caching for frequently accessed data

---

### 8. Organizational Hierarchy Design

**Decision**: Use adjacency list pattern with closure table for efficient hierarchy queries.

**Rationale**:
- **Flexibility**: Supports unlimited nesting depth
- **Performance**: Closure table enables fast ancestor/descendant queries
- **Integrity**: Database constraints prevent circular relationships

**Data Model**:
```sql
-- Adjacency List (primary representation)
CREATE TABLE Departments (
    Id UUID PRIMARY KEY,
    Name VARCHAR(200),
    ParentDepartmentId UUID REFERENCES Departments(Id),
    DepartmentHeadId UUID REFERENCES Employees(Id)
);

-- Closure Table (for efficient queries)
CREATE TABLE DepartmentHierarchy (
    AncestorId UUID REFERENCES Departments(Id),
    DescendantId UUID REFERENCES Departments(Id),
    Depth INT,
    PRIMARY KEY (AncestorId, DescendantId)
);
```

**Circular Relationship Prevention**:
- Check constraint: Prevent self-reference in ParentDepartmentId
- Application validation: Before assigning manager, traverse hierarchy to detect cycles
- Trigger/stored procedure: Maintain closure table consistency

**Trade-offs**:
- Closure table requires maintenance on hierarchy changes
- Increased storage for hierarchy paths
- **Mitigation**: Use database triggers or application-level consistency checks

---

### 9. Optimistic Concurrency Control

**Decision**: Implement optimistic locking using EF Core row versioning to prevent lost updates.

**Rationale**:
- **Data Integrity**: Prevents concurrent edits from overwriting changes
- **User Experience**: Second user receives clear conflict message
- **Performance**: No database locks required

**Implementation**:
```csharp
public class Employee
{
    [Timestamp]
    public byte[] RowVersion { get; set; }
}

// On concurrent update
try {
    await _context.SaveChangesAsync();
} catch (DbUpdateConcurrencyException ex) {
    return Result.Failure("Record was modified by another user. Please refresh and try again.");
}
```

**Trade-offs**:
- User must retry on conflict
- Potential for repeated conflicts in high-contention scenarios
- **Mitigation**: Implement automatic retry with exponential backoff, show diff of changes

---

### 10. Caching Strategy

**Decision**: Multi-layer caching with in-memory cache (L1) and distributed cache (L2) for read-heavy data.

**Rationale**:
- **Performance**: Reduce database load for frequently accessed data
- **Scalability**: Support high concurrent user load
- **Cost Efficiency**: Fewer database queries

**Cache Targets**:
- **L1 (In-Memory)**: Department hierarchy, leave policies, public holidays (rarely change)
- **L2 (Redis)**: Employee profiles, leave balances (moderate change frequency)
- **Cache Invalidation**: Event-driven invalidation when data changes

**Implementation**:
```csharp
public async Task<EmployeeProfileDto> GetEmployeeProfileAsync(Guid id)
{
    // Try L1 cache
    var cacheKey = $"employee-profile:{id}";
    if (_memoryCache.TryGetValue(cacheKey, out EmployeeProfileDto cachedProfile))
        return cachedProfile;

    // Try L2 cache (Redis)
    var redisValue = await _distributedCache.GetStringAsync(cacheKey);
    if (redisValue != null)
    {
        var profile = JsonSerializer.Deserialize<EmployeeProfileDto>(redisValue);
        _memoryCache.Set(cacheKey, profile, TimeSpan.FromMinutes(5));
        return profile;
    }

    // Fetch from database
    var employee = await _repository.GetByIdAsync(id);
    var dto = _mapper.Map<EmployeeProfileDto>(employee);

    // Populate caches
    await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
    _memoryCache.Set(cacheKey, dto, TimeSpan.FromMinutes(5));

    return dto;
}
```

**Trade-offs**:
- Cache coherence complexity
- Stale data risk
- **Mitigation**: TTL-based expiration, event-driven invalidation, cache versioning

---

## Technology Stack

### Backend Framework
- **ASP.NET Core 9.0**: Latest LTS, high performance, built-in DI, middleware pipeline
- **Entity Framework Core 9.0**: ORM with migrations, LINQ support, change tracking
- **MediatR**: CQRS implementation, request/response pipeline
- **FluentValidation**: Command/query input validation

### Database
- **PostgreSQL 16**: ACID compliance, JSON support, full-text search, robust indexing
- **Npgsql**: PostgreSQL driver for .NET

### Messaging
- **RabbitMQ**: Message broker for asynchronous integration
- **MassTransit**: .NET abstraction over RabbitMQ with saga support

### Security
- **JWT Bearer Authentication**: Stateless authentication via Maliev Auth Service
- **ASP.NET Core Authorization**: Role-based and policy-based authorization
- **Google Secret Manager**: Centralized secret storage

### Caching
- **IMemoryCache**: Built-in ASP.NET Core in-memory cache (L1)
- **Redis**: Distributed cache for multi-instance deployments (L2)

### Testing
- **xUnit**: Unit testing framework
- **FluentAssertions**: Readable test assertions
- **Moq**: Mocking framework for dependencies
- **Testcontainers**: Integration testing with real PostgreSQL and RabbitMQ

### Monitoring
- **Serilog**: Structured logging to console
- **Prometheus**: Metrics collection (counters, gauges, histograms)
- **Grafana**: Metrics visualization and alerting
- **OpenTelemetry**: Distributed tracing (optional future enhancement)

---

## Performance Considerations

### Expected Load
- **Peak Users**: 500 concurrent (all employees during open enrollment or leave request periods)
- **Database Size**: ~500 employees × 50 related records = ~25,000 records initially
- **Growth Rate**: ~10% annual employee growth = 50 new employees/year
- **Request Volume**: 1000 req/s peak (leave requests, profile views)

### Optimization Strategies
1. **Database Indexing**:
   - Composite index on `(EmployeeId, EmploymentStatus)` for active employee queries
   - Full-text search index on employee names
   - Index on `DepartmentId`, `ManagerId` for hierarchy queries

2. **Query Optimization**:
   - Use `.AsNoTracking()` for read-only queries
   - Eager loading for related entities (`.Include()`)
   - Pagination for list endpoints (limit 50 items per page)

3. **API Response Times**:
   - **Target**: p95 < 200ms, p99 < 500ms
   - **Strategy**: Caching, async I/O, database connection pooling

4. **Horizontal Scaling**:
   - Stateless API design enables multiple instances behind load balancer
   - Distributed cache (Redis) for session-independent caching
   - Kubernetes HPA based on CPU/memory metrics

---

## Security Architecture

### Authentication Flow
```
User → Login → Auth Service (JWT issuer)
  ↓
JWT Token (claims: user_id, employee_id, roles)
  ↓
Employee Service (validates JWT signature + claims)
  ↓
Authorization (role-based access control)
  ↓
Resource Access
```

### Authorization Roles
- **Employee**: View own profile, update emergency contacts, submit leave requests
- **Manager**: View direct reports, approve leave requests, view team performance
- **HR Generalist**: View all employees, update non-sensitive fields
- **HR Specialist**: Full access including compensation, disciplinary records
- **System Administrator**: Unrestricted access with all actions logged

### Data Protection
- **At Rest**: AES-256 encryption for sensitive fields
- **In Transit**: TLS 1.3 for all API communication
- **Audit Logging**: All access to sensitive data logged with user identity, timestamp, action

---

## Testing Strategy

### Test Pyramid
```
       /\
      /  \    E2E (10%)
     /____\   Contract Tests (20%)
    /      \  Integration Tests (30%)
   /________\ Unit Tests (40%)
```

### Test Categories

**1. Unit Tests** (40% of test effort)
- Domain logic (leave balance calculations, date validations)
- Command/query handlers (mocked repositories)
- Value objects (Thai national ID validation)
- Business rules (circular hierarchy detection)

**2. Integration Tests** (30%)
- Repository implementations (real PostgreSQL via Testcontainers)
- Database migrations
- Event publishing/consumption (real RabbitMQ via Testcontainers)
- Background jobs

**3. Contract Tests** (20%)
- API endpoints (OpenAPI compliance)
- Integration events (schema validation)
- External service mocks (Career Service)

**4. E2E Tests** (10%)
- Critical user journeys (onboarding, leave request approval)
- Security scenarios (unauthorized access attempts)
- Performance tests (load testing with k6)

### Test Data Strategy
- **In-Memory Database**: Fast unit tests, no persistence
- **Testcontainers**: Integration tests with real PostgreSQL/RabbitMQ
- **Fixtures**: Reusable test data builders (EmployeeBuilder, DepartmentBuilder)
- **Anonymization**: Production data never used in tests

---

## Migration and Rollout Strategy

### Phase 1: Core Foundation (Weeks 1-4)
- Employee profile management (US-1)
- HR employee lifecycle management (US-2)
- Department structure (US-5)
- Authentication and authorization

### Phase 2: Leave Management (Weeks 5-6)
- Leave balances and accrual (US-4)
- Leave request submission and approval (US-4)
- Manager team oversight (US-3)

### Phase 3: Onboarding/Offboarding (Weeks 7-8)
- Workflow automation (US-10)
- Career Service integration (US-2)
- IT/Facilities integration

### Phase 4: Advanced Features (Weeks 9-12)
- Compensation management (US-6)
- Performance reviews (US-7)
- Training tracking (US-8)
- Document management (US-9)

### Phase 5: Compliance and Reporting (Weeks 13-14)
- Work authorization tracking (US-11)
- Analytics and reporting (US-12)
- Audit log queries
- Data export for PDPA compliance

### Rollback Strategy
- Blue-green deployment for zero-downtime rollback
- Database migrations backward-compatible (no column drops in same release)
- Feature flags for gradual rollout

---

## Open Questions and Risks

### Questions Requiring Clarification
1. **Learning Management System**: Does Maliev have an existing LMS, or should Employee Service manage training records directly?
   - **Impact**: Integration vs. native implementation decision

2. **Document Storage**: Should documents be stored in database (PostgreSQL bytea) or cloud storage (Google Cloud Storage)?
   - **Impact**: Storage costs, backup strategy, retrieval performance

3. **Compensation Data**: Should salary history be stored in Employee Service or delegated to Payroll Service?
   - **Impact**: Service boundaries, data duplication

### Risks and Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Career Service integration failure during candidate transition | High | Medium | Implement manual employee creation fallback, retry logic with exponential backoff |
| Performance degradation with 500 concurrent users | High | Low | Load testing in staging, horizontal scaling with Kubernetes HPA, caching strategy |
| Data migration from existing HR system | High | High | Comprehensive data mapping, validation scripts, pilot migration with subset of employees |
| Circular hierarchy detection performance | Medium | Low | Pre-computed closure table, limit hierarchy depth to 10 levels |
| Leave balance calculation errors | High | Medium | Extensive unit tests, monthly reconciliation with audit logs, manual override capability |
| PDPA compliance gaps | High | Low | Legal review of data handling, annual compliance audit, explicit consent tracking |

---

## Appendices

### Appendix A: Database Schema Overview

**Core Tables**:
- `Employees` (500 rows initially)
- `Departments` (20 rows)
- `EmergencyContacts` (1000 rows, 2 per employee avg)
- `LeaveBalances` (2000 rows, 4 leave types per employee)
- `LeaveRequests` (5000 rows annually, 10 requests per employee)
- `LeaveApprovals` (7500 rows annually, 1.5 approvals per request)
- `AuditLogs` (growing indefinitely, 7-year retention)

**Estimated Storage**:
- Initial data: ~500 MB
- Annual growth: ~100 MB
- 5-year projection: ~1 GB

### Appendix B: API Endpoint Summary

**Employee Management**:
- `GET /employees/v1/employees/{id}` - Get employee profile
- `PUT /employees/v1/employees/{id}` - Update employee (HR only)
- `POST /employees/v1/employees` - Create employee (HR only)
- `GET /employees/v1/employees/{id}/emergency-contacts` - List emergency contacts
- `PUT /employees/v1/employees/{id}/emergency-contacts/{contactId}` - Update emergency contact

**Leave Management**:
- `GET /employees/v1/leave/balances/{employeeId}` - Get leave balances
- `POST /employees/v1/leave/requests` - Submit leave request
- `GET /employees/v1/leave/requests/{id}` - Get leave request details
- `POST /employees/v1/leave/requests/{id}/approve` - Approve leave request (Manager)
- `POST /employees/v1/leave/requests/{id}/deny` - Deny leave request (Manager)

**Department Management**:
- `GET /employees/v1/departments` - List departments (tree structure)
- `GET /employees/v1/departments/{id}` - Get department details
- `POST /employees/v1/departments` - Create department (HR only)
- `PUT /employees/v1/departments/{id}` - Update department (HR only)

**Manager Operations**:
- `GET /employees/v1/managers/team` - Get team members (direct + indirect reports)
- `GET /employees/v1/managers/leave-requests` - Get pending leave requests for approval

**Health and Metrics**:
- `GET /employees/liveness` - Kubernetes liveness probe
- `GET /employees/readiness` - Kubernetes readiness probe (checks DB connection)
- `GET /employees/metrics` - Prometheus metrics endpoint

### Appendix C: Integration Event Schemas

**Inbound Events**:
```json
{
  "eventType": "CandidateAccepted",
  "eventId": "uuid",
  "timestamp": "2025-10-12T10:30:00Z",
  "payload": {
    "candidateId": "uuid",
    "fullName": "Somchai Prasert",
    "email": "somchai@maliev.co.th",
    "phone": "+66812345678",
    "jobPositionId": "uuid",
    "startDate": "2025-11-01"
  }
}
```

**Outbound Events**:
```json
{
  "eventType": "EmployeeCreated",
  "eventId": "uuid",
  "timestamp": "2025-10-12T10:30:00Z",
  "payload": {
    "employeeId": "uuid",
    "employeeNumber": "EMP-0501",
    "fullName": "Somchai Prasert",
    "email": "somchai@maliev.co.th",
    "departmentId": "uuid",
    "startDate": "2025-11-01",
    "employmentStatus": "PendingStart"
  }
}
```

---

**Document Status**: Complete
**Next Steps**: Proceed to Phase 1 (data-model.md, contracts/, quickstart.md)
