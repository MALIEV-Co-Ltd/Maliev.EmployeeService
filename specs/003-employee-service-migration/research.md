# Research: Employee Service Decomposition Technical Decisions

**Feature**: 003-employee-service-migration
**Date**: 2025-12-28
**Purpose**: Document technical decisions for security, compliance, and distributed transaction patterns

---

## 1. PostgreSQL Encryption (At Rest and In Transit)

### Decision

**Encryption in Transit**: Enable TLS/SSL for all PostgreSQL connections using Npgsql connection string parameters.

**Encryption at Rest**: Use PostgreSQL native encryption features combined with cloud provider disk encryption.

### Rationale

- **In Transit**: Prevents man-in-the-middle attacks and eavesdropping on database connections, especially in cloud/containerized environments
- **At Rest**: Protects data on disk from physical theft or unauthorized access to storage volumes
- Balances security with operational complexity and performance

### Configuration

**Connection String (appsettings.json)**:
```json
{
  "ConnectionStrings": {
    "EmployeeDb": "Host=postgres-host;Database=employee_db;Username=%DB_USERNAME%;Password=%DB_PASSWORD%;SSL Mode=Require;Trust Server Certificate=false"
  }
}
```

**Key Parameters**:
- `SSL Mode=Require`: Enforces TLS encryption for connections
- `Trust Server Certificate=false`: Validates server certificate (production)
- Use `SSL Mode=VerifyFull` for strictest validation

**At-Rest Encryption Options**:

1. **Cloud Provider Disk Encryption** (Recommended for initial deployment):
   - GCP Persistent Disk encryption (automatic)
   - AWS EBS encryption
   - Minimal performance impact
   - Transparent to application

2. **PostgreSQL pgcrypto Extension** (For column-level encryption):
   - Encrypt specific sensitive columns (salaries, benefits)
   - More granular control
   - Requires application-level key management

### Alternatives Considered

- **No encryption at rest**: Rejected - violates GDPR and security best practices
- **Application-level encryption**: Rejected - adds complexity, limits querying capability
- **PostgreSQL Transparent Data Encryption (TDE)**: Not available in community edition

### Implementation Guidance

**1. Configure Npgsql in ServiceDefaults**:
```csharp
// In Maliev.Aspire.ServiceDefaults
builder.Services.AddNpgsql<TDbContext>(
    connectionName,
    configureDbContextOptions: options =>
    {
        options.UseNpgsql(npgsqlBuilder =>
        {
            npgsqlBuilder.EnableRetryOnFailure(maxRetryCount: 3);
            // SSL is configured via connection string
        });
    });
```

**2. Certificate Management**:
- Store PostgreSQL CA certificate in Google Secret Manager
- Mount as environment variable or file in containers
- Update connection string: `SSL Certificate=/app/certs/postgres-ca.crt`

**3. Testing with Testcontainers**:
```csharp
var postgresContainer = new PostgreSqlBuilder()
    .WithImage("postgres:18-alpine")
    .WithDatabase("testdb")
    .WithUsername("testuser")
    .WithPassword("testpass")
    // Note: Testcontainers may not support SSL in test environments
    .Build();

await postgresContainer.StartAsync();
```

---

## 2. GDPR Compliance for HR Systems

### Decision

Implement the following GDPR compliance measures:

1. **Consent Management**: Track employee consent for data processing
2. **Data Retention**: 7-year retention for employment records (post-termination)
3. **Right to Erasure**: Soft-delete with anonymization after retention period
4. **Data Portability**: API endpoints for exporting employee data in JSON format
5. **Audit Logging**: Immutable audit trails for all data access and modifications

### Rationale

- **GDPR Article 5**: Lawfulness, fairness, transparency in data processing
- **GDPR Article 17**: Right to erasure (with exceptions for legal obligations)
- **GDPR Article 20**: Right to data portability
- **GDPR Article 32**: Security of processing (encryption, pseudonymization)

HR systems have special provisions under GDPR allowing retention for legal and tax compliance.

### Key GDPR Requirements for HR Data

| Requirement | Implementation | Notes |
|-------------|----------------|-------|
| **Lawful Basis** | Employment contract (GDPR Art. 6.1.b) | Processing necessary for employment |
| **Data Minimization** | Store only necessary fields | Avoid collecting unnecessary personal data |
| **Storage Limitation** | 7-year retention post-termination | Aligns with tax/labor law requirements |
| **Right to Access** | GET /employee/v1/profile/export endpoint | JSON export of all employee data |
| **Right to Rectification** | PUT /employee/v1/profile endpoint | Employees can update their data |
| **Right to Erasure** | Soft-delete + anonymization after 7 years | DELETE /employee/v1/employees/{id} |
| **Data Portability** | JSON export in machine-readable format | Standard REST API response |
| **Security** | TLS, encryption at rest, access controls | Via JWT + IAM permissions |
| **Breach Notification** | 72-hour notification requirement | Implement alerting for security events |

### Data Retention Strategy

**Employee Record Lifecycle**:

1. **Active Employment**: Full data access, all operations permitted
2. **Terminated (0-7 years)**: Read-only access, soft-delete flag set
3. **After 7 years**: Anonymize PII, retain only statistical/aggregated data

**Soft-Delete Implementation**:

```csharp
public class Employee
{
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;

    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? AnonymizedAt { get; set; }

    // Retention tracking
    public DateTime? TerminationDate { get; set; }
}

// Global query filter in DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Employee>()
        .HasQueryFilter(e => !e.IsDeleted);
}
```

**Anonymization Process** (Background Service):
```csharp
public class DataRetentionBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var cutoffDate = DateTime.UtcNow.AddYears(-7);

            var expiredEmployees = await _context.Employees
                .IgnoreQueryFilters()
                .Where(e => e.TerminationDate < cutoffDate && !e.AnonymizedAt.HasValue)
                .ToListAsync();

            foreach (var employee in expiredEmployees)
            {
                // Anonymize PII
                employee.FullName = $"REDACTED_{employee.Id}";
                employee.Email = $"redacted_{employee.Id}@anonymized.local";
                employee.AnonymizedAt = DateTime.UtcNow;
                // Retain EmployeeNumber for statistical purposes
            }

            await _context.SaveChangesAsync();
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
```

### Alternatives Considered

- **Hard delete immediately**: Rejected - violates tax/labor law retention requirements
- **10-year retention**: Rejected - longer than legally required, increases liability
- **No anonymization**: Rejected - violates GDPR storage limitation principle

### Implementation Guidance

**Required Endpoints**:

1. `GET /employee/v1/profile/export` - Data portability (JSON export)
2. `DELETE /employee/v1/employees/{id}` - Right to erasure (soft-delete)
3. `GET /employee/v1/employees/{id}/audit-log` - View access history

**Permissions**:
- `employee.data.export` - Export own data
- `employee.data.delete` - Request deletion (own data)
- `employee.admin.gdpr` - Admin GDPR operations

---

## 3. Thai Labor Law and Tax Compliance

### Decision

Implement **7-year retention policy** for all employee records, aligning with Thai tax law requirements.

### Rationale

**Thai Revenue Code (Section 70)**:
- Businesses must retain accounting documents and supporting evidence for at least **5 years**
- Employee salary records fall under "supporting evidence"
- 7-year retention provides safety margin beyond legal minimum

**Thai Labor Protection Act B.E. 2541 (1998)**:
- Employers must maintain employee records during employment
- Records include: name, address, work hours, wages, leave entitlements
- No specific retention period post-termination specified in labor law

**Best Practice**: 7-year retention aligns with:
- Thai Revenue Code compliance (5+ years)
- International tax audit requirements
- GDPR's allowance for legal retention obligations

### Key Requirements

| Requirement | Implementation | Regulatory Source |
|-------------|----------------|-------------------|
| **Salary Records** | 7-year retention | Thai Revenue Code §70 |
| **Tax Withholding** | PND1 forms, 7 years | Revenue Department regulations |
| **Social Security** | Contribution records, 7 years | Social Security Act |
| **Work Permits** | Foreign employee work authorization | Immigration Act |

### Data Subject Rights (Thai PDPA)

Thailand's Personal Data Protection Act (PDPA) B.E. 2562 (2019) is similar to GDPR:

- **Right to Access**: Employees can request copies of their data
- **Right to Rectification**: Employees can correct inaccurate data
- **Right to Erasure**: Limited - does not override legal retention obligations
- **Right to Portability**: Export data in machine-readable format

**PDPA vs GDPR Differences**:
- PDPA allows longer retention if justified by legal obligations (Thai Revenue Code)
- No conflict with 7-year retention policy

### Implementation Guidance

**Tax Reporting Integration**:
- Generate PND1 (withholding tax forms) from CompensationService
- Export to Thai e-Filing system
- Maintain immutable tax records separate from employee profiles

**Foreign Employee Compliance**:
- Track work permit expiration in ComplianceService
- Alert HR 90/60/30 days before expiry
- Integrate with Immigration Bureau reporting if required

---

## 4. RabbitMQ Security Best Practices

### Decision

Configure RabbitMQ with:
1. **TLS encryption** for all client connections
2. **Username/password authentication** with strong passwords from Google Secret Manager
3. **Access Control Lists (ACLs)** limiting each service to its own queues/exchanges

### Rationale

- **TLS Encryption**: Prevents eavesdropping on messages containing sensitive employee data
- **Authentication**: Prevents unauthorized access to message broker
- **ACLs**: Principle of least privilege - services can only access their own queues

### Configuration

**RabbitMQ Server (Docker)**:
```yaml
# docker-compose.yml for development
version: '3.8'
services:
  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports:
      - "5672:5672"   # AMQP
      - "15672:15672" # Management UI
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_ADMIN_PASSWORD}
      RABBITMQ_SSL_CERTFILE: /etc/rabbitmq/certs/cert.pem
      RABBITMQ_SSL_KEYFILE: /etc/rabbitmq/certs/key.pem
      RABBITMQ_SSL_CACERTFILE: /etc/rabbitmq/certs/ca.pem
    volumes:
      - ./rabbitmq-certs:/etc/rabbitmq/certs
```

**MassTransit Configuration (via ServiceDefaults)**:
```csharp
// In Maliev.Aspire.ServiceDefaults
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConfig = builder.Configuration.GetSection("RabbitMQ");

        cfg.Host(rabbitConfig["Host"], rabbitConfig["VirtualHost"], h =>
        {
            h.Username(Environment.GetEnvironmentVariable("RABBITMQ_USERNAME"));
            h.Password(Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD"));

            // Enable TLS
            h.UseSsl(s =>
            {
                s.ServerName = rabbitConfig["Host"];
                s.CertificatePath = "/app/certs/rabbitmq-client.pem";
                s.CertificatePassphrase = Environment.GetEnvironmentVariable("RABBITMQ_CERT_PASSPHRASE");
            });
        });

        cfg.ConfigureEndpoints(context);
    });
});
```

**Access Control per Service**:
```bash
# Grant permissions for EmployeeService
rabbitmqctl set_permissions -p / employee_service "^employee\..*" "^employee\..*" "^employee\..*"

# Grant permissions for LeaveService
rabbitmqctl set_permissions -p / leave_service "^leave\..*|^employee\.events$" "^leave\..*" "^employee\.events$"
```

**Pattern**:
- Configure: `^service\\..*` (own exchanges/queues)
- Write: `^service\\..*` (publish to own exchanges)
- Read: `^service\\..*|^employee\\.events$` (read from own queues + shared event exchanges)

### Alternatives Considered

- **No TLS**: Rejected - exposes sensitive employee data in transit
- **Certificate-based auth**: Deferred - username/password sufficient for initial deployment
- **No ACLs**: Rejected - violates principle of least privilege

### Implementation Guidance

**Development Environment** (Testcontainers):
```csharp
var rabbitmqContainer = new RabbitMqBuilder()
    .WithImage("rabbitmq:4.2-alpine")
    .WithUsername("guest")
    .WithPassword("guest")
    // Note: TLS not typically configured in test environments
    .Build();

await rabbitmqContainer.StartAsync();
```

**Production Environment**:
- Store RabbitMQ credentials in Google Secret Manager
- Inject via environment variables: `RABBITMQ_USERNAME`, `RABBITMQ_PASSWORD`
- Mount TLS certificates from Secret Manager to `/app/certs/`

---

## 5. Saga Pattern Persistence Strategy

### Decision

Implement **database-based saga state tracking** with dedicated saga state tables in the orchestrating service's database.

### Rationale

- **Durability**: Saga state survives orchestrator crashes and restarts
- **Recovery**: Can resume in-progress sagas after failure
- **Auditability**: Complete history of distributed transactions
- **Simplicity**: Reuses existing PostgreSQL infrastructure

### Database Schema

**Saga State Table**:
```sql
CREATE TABLE saga_state (
    correlation_id UUID PRIMARY KEY,
    saga_type VARCHAR(255) NOT NULL,
    current_step VARCHAR(100) NOT NULL,
    status VARCHAR(50) NOT NULL, -- InProgress, Completed, Compensating, Failed
    payload JSONB NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_saga_state_status ON saga_state(status, created_at);
CREATE INDEX idx_saga_state_type ON saga_state(saga_type);
```

**Saga Step History Table**:
```sql
CREATE TABLE saga_step_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    correlation_id UUID NOT NULL REFERENCES saga_state(correlation_id),
    step_name VARCHAR(100) NOT NULL,
    step_type VARCHAR(50) NOT NULL, -- Execute, Compensate
    status VARCHAR(50) NOT NULL, -- Succeeded, Failed, Skipped
    executed_at TIMESTAMP NOT NULL DEFAULT NOW(),
    error_message TEXT
);

CREATE INDEX idx_saga_step_correlation ON saga_step_history(correlation_id);
```

### Saga Implementation Pattern

**Employee Termination Saga** (Orchestrator in EmployeeService):

```csharp
public class EmployeeTerminationSaga :
    ISaga,
    InitiatedBy<TerminateEmployeeCommand>,
    Orchestrates<LeaveBalanceClosedEvent>,
    Orchestrates<CompensationArchivedEvent>,
    Orchestrates<AccessRevokedEvent>
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = "Initiated";

    // Saga state
    public Guid EmployeeId { get; set; }
    public DateTime TerminationDate { get; set; }
    public bool LeaveBalanceClosed { get; set; }
    public bool CompensationArchived { get; set; }
    public bool AccessRevoked { get; set; }

    public async Task Consume(ConsumeContext<TerminateEmployeeCommand> context)
    {
        EmployeeId = context.Message.EmployeeId;
        TerminationDate = context.Message.TerminationDate;
        CurrentState = "TerminatingEmployee";

        // Step 1: Close leave balances
        await context.Publish(new CloseLeaveBalanceCommand
        {
            CorrelationId = CorrelationId,
            EmployeeId = EmployeeId,
            EffectiveDate = TerminationDate
        });
    }

    public async Task Consume(ConsumeContext<LeaveBalanceClosedEvent> context)
    {
        LeaveBalanceClosed = true;
        CurrentState = "LeaveBalanceClosed";

        // Step 2: Archive compensation
        await context.Publish(new ArchiveCompensationCommand
        {
            CorrelationId = CorrelationId,
            EmployeeId = EmployeeId
        });
    }

    public async Task Consume(ConsumeContext<CompensationArchivedEvent> context)
    {
        CompensationArchived = true;
        CurrentState = "CompensationArchived";

        // Step 3: Revoke access
        await context.Publish(new RevokeAccessCommand
        {
            CorrelationId = CorrelationId,
            EmployeeId = EmployeeId
        });
    }

    public async Task Consume(ConsumeContext<AccessRevokedEvent> context)
    {
        AccessRevoked = true;
        CurrentState = "Completed";

        // Final step: Mark employee as terminated in EmployeeService
        await context.Publish(new EmployeeTerminatedIntegrationEvent
        {
            EmployeeId = EmployeeId,
            TerminationDate = TerminationDate
        });
    }
}
```

**Saga State Persistence** (MassTransit with EF Core):

```csharp
// In EmployeeService Infrastructure
services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<EmployeeTerminationSaga, EmployeeTerminationSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<EmployeeDbContext>();
            r.UsePostgres();
        });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});
```

**Compensation Actions** (Stored in saga state):

```csharp
public class EmployeeTerminationSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;

    // Compensation data (for rollback)
    public Guid? LeaveBalanceSnapshotId { get; set; }
    public Guid? CompensationSnapshotId { get; set; }
    public Guid? AccessTokensRevokedId { get; set; }
}
```

### Recovery Mechanism

**On Orchestrator Restart**:
```csharp
public class SagaRecoveryService : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Find all in-progress sagas
        var inProgressSagas = await _dbContext.SagaState
            .Where(s => s.Status == "InProgress" || s.Status == "Compensating")
            .ToListAsync(cancellationToken);

        foreach (var saga in inProgressSagas)
        {
            // Re-queue saga for processing
            await _sagaRepository.LoadAndExecute(saga.CorrelationId);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

### Alternatives Considered

- **In-memory state**: Rejected - lost on crash
- **Event sourcing**: Deferred - adds complexity, overkill for initial deployment
- **Redis-based state**: Rejected - PostgreSQL already available, better durability guarantees

### Implementation Guidance

**Testing Sagas with Testcontainers**:
```csharp
[Fact]
public async Task EmployeeTerminationSaga_Should_RollbackOn_CompensationFailure()
{
    // Arrange
    var postgresContainer = await StartPostgresContainer();
    var rabbitmqContainer = await StartRabbitMqContainer();

    // Simulate compensation service failure
    var compensationServiceMock = new Mock<ICompensationService>();
    compensationServiceMock
        .Setup(x => x.ArchiveCompensation(It.IsAny<Guid>()))
        .ThrowsAsync(new Exception("Compensation service unavailable"));

    // Act
    await PublishTerminateEmployeeCommand(employeeId);
    await Task.Delay(TimeSpan.FromSeconds(5)); // Wait for saga processing

    // Assert
    var sagaState = await GetSagaState(correlationId);
    Assert.Equal("Compensating", sagaState.CurrentState);
    Assert.True(sagaState.LeaveBalanceClosed); // Should be rolled back
}
```

**MassTransit Saga Configuration**:
- Use `EntityFrameworkRepository` for saga persistence
- Configure retry policies for transient failures
- Implement compensating transactions for each saga step

---

## Summary

| Topic | Decision | Priority |
|-------|----------|----------|
| **Encryption (In Transit)** | TLS for PostgreSQL & RabbitMQ | P0 (Security) |
| **Encryption (At Rest)** | Cloud provider disk encryption | P0 (Security) |
| **GDPR Compliance** | 7-year retention + soft-delete + anonymization | P0 (Legal) |
| **Thai Tax Law** | 7-year retention aligns with Revenue Code | P0 (Legal) |
| **RabbitMQ Security** | TLS + username/password + ACLs | P0 (Security) |
| **Saga Persistence** | PostgreSQL-based state with EF Core | P0 (Reliability) |

**All research complete. Ready to proceed to Phase 1: Data Model and Contracts.**
