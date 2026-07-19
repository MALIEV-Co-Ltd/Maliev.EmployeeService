# Data Model: Employee Service Decomposition

**Feature**: 003-employee-service-migration
**Date**: 2025-12-28
**Purpose**: Define entity schemas, relationships, and validation rules for seven microservices

---

## Overview

This document defines the data models for seven services after decomposition:
1. Employee Service (core - retained entities)
2. Leave Service (new)
3. Compensation Service (new)
4. Performance Service (new)
5. Lifecycle Service (new)
6. Compliance Service (new)
7. Career Service (extended with training/skills)

**Key Principles**:
- Each service has its own PostgreSQL database
- NO cross-service database joins
- Cross-service data access via APIs or RabbitMQ events
- Soft-delete for GDPR compliance
- Audit trail for all modifications

---

## 1. Employee Service (Core) - Data Model

### Database: `employee_db`

#### Employee (Aggregate Root)
```csharp
public class Employee
{
    // Identity
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = null!; // Unique, e.g., "EMP-2024-001"

    // Personal Information
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string FullName { get; set; } = null!; // Computed
    public string Email { get; set; } = null!; // Unique
    public string? PhoneNumber { get; set; }
    public DateTime DateOfBirth { get; set; }

    // Employment
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public EmploymentStatus Status { get; set; } // Active, Terminated, OnLeave

    // Organizational Relationships
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }

    // Soft Delete (GDPR)
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? AnonymizedAt { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;

    // Navigation Properties
    public ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();
    public ICollection<EmployeeTeamAssignment> TeamAssignments { get; set; } = new List<EmployeeTeamAssignment>();
    public ICollection<EmploymentHistory> EmploymentHistories { get; set; } = new List<EmploymentHistory>();
}

public enum EmploymentStatus
{
    Active = 1,
    OnLeave = 2,
    Terminated = 3
}
```

**Validation Rules**:
- Email must be unique and valid format
- EmployeeNumber must be unique
- HireDate cannot be in the future
- TerminationDate must be >= HireDate
- Age must be >= 18 (DateOfBirth)

**Indexes**:
```sql
CREATE UNIQUE INDEX ix_employees_employee_number ON employees(employee_number);
CREATE UNIQUE INDEX ix_employees_email ON employees(email) WHERE is_deleted = false;
CREATE INDEX ix_employees_department_id ON employees(department_id);
CREATE INDEX ix_employees_manager_id ON employees(manager_id);
CREATE INDEX ix_employees_status ON employees(status);
```

---

#### EmergencyContact
```csharp
public class EmergencyContact
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string FullName { get; set; } = null!;
    public string Relationship { get; set; } = null!; // Spouse, Parent, Sibling, Friend
    public string PrimaryPhone { get; set; } = null!;
    public string? SecondaryPhone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsPrimaryContact { get; set; } // Only one primary per employee

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Validation Rules**:
- EmployeeId must reference valid Employee
- PrimaryPhone must be valid phone format
- Only one IsPrimaryContact = true per EmployeeId

**Indexes**:
```sql
CREATE INDEX ix_emergency_contacts_employee_id ON emergency_contacts(employee_id);
CREATE UNIQUE INDEX ix_emergency_contacts_primary ON emergency_contacts(employee_id)
    WHERE is_primary_contact = true;
```

---

#### Department
```csharp
public class Department
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!; // Unique
    public string Code { get; set; } = null!; // Unique, e.g., "IT", "HR", "FIN"
    public string? Description { get; set; }

    // Hierarchy
    public Guid? ParentDepartmentId { get; set; }
    public Department? ParentDepartment { get; set; }
    public Guid? HeadOfDepartmentId { get; set; } // Manager of department
    public Employee? HeadOfDepartment { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();
}
```

**Validation Rules**:
- Name must be unique
- Code must be unique
- ParentDepartmentId cannot create circular reference

**Indexes**:
```sql
CREATE UNIQUE INDEX ix_departments_name ON departments(name);
CREATE UNIQUE INDEX ix_departments_code ON departments(code);
CREATE INDEX ix_departments_parent_id ON departments(parent_department_id);
```

---

#### Position
```csharp
public class Position
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!; // Unique
    public string Code { get; set; } = null!; // Unique, e.g., "SE-SR", "MGR-01"
    public string? Description { get; set; }
    public int Level { get; set; } // 1 = Entry, 2 = Mid, 3 = Senior, 4 = Lead, 5 = Manager

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
```

**Validation Rules**:
- Title must be unique
- Code must be unique
- Level must be 1-5

**Indexes**:
```sql
CREATE UNIQUE INDEX ix_positions_title ON positions(title);
CREATE UNIQUE INDEX ix_positions_code ON positions(code);
```

---

#### Team
```csharp
public class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? TeamLeadId { get; set; }
    public Employee? TeamLead { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<EmployeeTeamAssignment> TeamMembers { get; set; } = new List<EmployeeTeamAssignment>();
}
```

**Validation Rules**:
- Name must be unique within department scope
- TeamLeadId must be a member of the team

**Indexes**:
```sql
CREATE INDEX ix_teams_team_lead_id ON teams(team_lead_id);
```

---

#### EmployeeTeamAssignment
```csharp
public class EmployeeTeamAssignment
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public DateTime AssignedAt { get; set; }
    public DateTime? RemovedAt { get; set; }
    public bool IsActive { get; set; }
}
```

**Validation Rules**:
- Unique combination of (EmployeeId, TeamId) where IsActive = true
- RemovedAt must be >= AssignedAt

**Indexes**:
```sql
CREATE UNIQUE INDEX ix_team_assignments_active ON employee_team_assignments(employee_id, team_id)
    WHERE is_active = true;
CREATE INDEX ix_team_assignments_team_id ON employee_team_assignments(team_id);
```

---

#### EmploymentHistory
```csharp
public class EmploymentHistory
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string ChangeType { get; set; } = null!; // Promotion, Transfer, StatusChange, Termination
    public DateTime EffectiveDate { get; set; }

    // Previous Values
    public Guid? PreviousDepartmentId { get; set; }
    public Guid? PreviousPositionId { get; set; }
    public EmploymentStatus? PreviousStatus { get; set; }

    // New Values
    public Guid? NewDepartmentId { get; set; }
    public Guid? NewPositionId { get; set; }
    public EmploymentStatus? NewStatus { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
}
```

**Validation Rules**:
- EffectiveDate cannot be in the future (beyond today)
- At least one of (PreviousDepartmentId, PreviousPositionId, PreviousStatus) must differ from (NewDepartmentId, NewPositionId, NewStatus)

**Indexes**:
```sql
CREATE INDEX ix_employment_history_employee_id ON employment_history(employee_id);
CREATE INDEX ix_employment_history_effective_date ON employment_history(effective_date DESC);
```

---

#### PersonalDocument
```csharp
public class PersonalDocument
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string DocumentType { get; set; } = null!; // Resume, Contract, ID_Card, etc.
    public string FileName { get; set; } = null!;
    public string FileUrl { get; set; } = null!; // URL from UploadService
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; } = null!;

    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = null!;
}
```

**Validation Rules**:
- FileUrl must be valid URL
- FileSizeBytes must be > 0
- MimeType must be from allowed list (PDF, PNG, JPG, DOCX)

**Indexes**:
```sql
CREATE INDEX ix_personal_documents_employee_id ON personal_documents(employee_id);
CREATE INDEX ix_personal_documents_document_type ON personal_documents(document_type);
```

---

#### AuditLog
```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = null!; // Employee, Department, Team
    public Guid EntityId { get; set; }
    public string Action { get; set; } = null!; // Created, Updated, Deleted
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    public string ChangedBy { get; set; } = null!;
    public DateTime ChangedAt { get; set; }
}
```

**Validation Rules**:
- Immutable - no updates or deletes allowed
- OldValues and NewValues must be valid JSON

**Indexes**:
```sql
CREATE INDEX ix_audit_logs_entity ON audit_logs(entity_type, entity_id);
CREATE INDEX ix_audit_logs_changed_at ON audit_logs(changed_at DESC);
CREATE INDEX ix_audit_logs_changed_by ON audit_logs(changed_by);
```

---

## 2. Leave Service - Data Model

### Database: `leave_db`

#### LeaveRequest
```csharp
public class LeaveRequest
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; } // Reference to Employee Service
    public string EmployeeNumber { get; set; } = null!; // Denormalized for convenience

    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysRequested { get; set; } // Business days
    public string? Reason { get; set; }

    public LeaveRequestStatus Status { get; set; }
    public Guid? ApproverId { get; set; } // Manager who approved/rejected
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewComments { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum LeaveRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}
```

**Validation Rules**:
- EndDate must be >= StartDate
- DaysRequested must be > 0
- No overlapping leave requests for same employee with status Approved
- Cannot request leave for past dates (StartDate >= Today)

**Indexes**:
```sql
CREATE INDEX ix_leave_requests_employee_id ON leave_requests(employee_id);
CREATE INDEX ix_leave_requests_status ON leave_requests(status);
CREATE INDEX ix_leave_requests_start_date ON leave_requests(start_date);
```

---

#### LeaveBalance
```csharp
public class LeaveBalance
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = null!;

    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public int Year { get; set; }
    public decimal Entitled { get; set; } // Annual entitlement
    public decimal Used { get; set; }
    public decimal Remaining { get; set; } // Computed: Entitled - Used

    public DateTime LastAccrualDate { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Validation Rules**:
- Unique combination of (EmployeeId, LeaveTypeId, Year)
- Used <= Entitled
- Remaining = Entitled - Used (computed)

**Indexes**:
```sql
CREATE UNIQUE INDEX ix_leave_balances_employee_type_year ON leave_balances(employee_id, leave_type_id, year);
```

---

#### LeaveType (LeavePolicy)
```csharp
public class LeaveType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!; // Annual, Sick, Personal, Parental
    public string Code { get; set; } = null!; // Unique
    public decimal AnnualEntitlement { get; set; } // Default days per year
    public bool RequiresApproval { get; set; }
    public int MaxCarryOverDays { get; set; }
    public int MaxConsecutiveDays { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Validation Rules**:
- Name and Code must be unique
- AnnualEntitlement must be > 0
- MaxCarryOverDays <= AnnualEntitlement

**Indexes**:
```sql
CREATE UNIQUE INDEX ix_leave_types_code ON leave_types(code);
```

---

## 3. Compensation Service - Data Model

### Database: `compensation_db`

#### CompensationRecord
```csharp
public class CompensationRecord
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = null!;

    public decimal BaseSalary { get; set; }
    public string Currency { get; set; } = "THB";
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }

    public string? ChangeReason { get; set; } // Promotion, AnnualIncrease, MarketAdjustment
    public Guid? ApprovedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
}
```

**Validation Rules**:
- BaseSalary must be > 0
- EffectiveDate cannot be in the future
- EndDate must be >= EffectiveDate

**Indexes**:
```sql
CREATE INDEX ix_compensation_records_employee_id ON compensation_records(employee_id);
CREATE INDEX ix_compensation_records_effective_date ON compensation_records(effective_date DESC);
```

---

#### BenefitsEnrollment
```csharp
public class BenefitsEnrollment
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = null!;

    public Guid BenefitPlanId { get; set; }
    public BenefitPlan BenefitPlan { get; set; } = null!;

    public DateTime EnrollmentDate { get; set; }
    public DateTime CoverageStartDate { get; set; }
    public DateTime? CoverageEndDate { get; set; }

    public decimal EmployeePremium { get; set; }
    public decimal EmployerPremium { get; set; }

    public ICollection<Dependent> Dependents { get; set; } = new List<Dependent>();
}
```

**Validation Rules**:
- CoverageStartDate >= EnrollmentDate
- CoverageEndDate >= CoverageStartDate
- EmployeePremium >= 0, EmployerPremium >= 0

**Indexes**:
```sql
CREATE INDEX ix_benefits_enrollment_employee_id ON benefits_enrollments(employee_id);
CREATE INDEX ix_benefits_enrollment_plan_id ON benefits_enrollments(benefit_plan_id);
```

---

#### Dependent
```csharp
public class Dependent
{
    public Guid Id { get; set; }
    public Guid BenefitsEnrollmentId { get; set; }
    public BenefitsEnrollment BenefitsEnrollment { get; set; } = null!;

    public string FullName { get; set; } = null!;
    public string Relationship { get; set; } = null!; // Spouse, Child
    public DateTime DateOfBirth { get; set; }
    public string? IdentificationNumber { get; set; }
}
```

**Validation Rules**:
- DateOfBirth must be in the past
- Relationship must be from allowed list (Spouse, Child, Parent)

---

## 4. Performance Service - Data Model

### Database: `performance_db`

#### PerformanceReview
```csharp
public class PerformanceReview
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = null!;

    public Guid ReviewerId { get; set; } // Manager conducting review
    public DateTime ReviewPeriodStart { get; set; }
    public DateTime ReviewPeriodEnd { get; set; }

    public int OverallRating { get; set; } // 1-5 scale
    public string? Strengths { get; set; }
    public string? AreasForImprovement { get; set; }
    public string? Goals { get; set; }

    public bool AcknowledgedByEmployee { get; set; }
    public DateTime? AcknowledgedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Validation Rules**:
- ReviewPeriodEnd must be >= ReviewPeriodStart
- OverallRating must be 1-5
- Cannot have multiple reviews for same employee with overlapping review periods

**Indexes**:
```sql
CREATE INDEX ix_performance_reviews_employee_id ON performance_reviews(employee_id);
CREATE INDEX ix_performance_reviews_period ON performance_reviews(review_period_start, review_period_end);
```

---

#### Goal
```csharp
public class Goal
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime TargetDate { get; set; }

    public GoalStatus Status { get; set; }
    public int ProgressPercentage { get; set; } // 0-100

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum GoalStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}
```

**Validation Rules**:
- ProgressPercentage must be 0-100
- If Status = Completed, ProgressPercentage must be 100

**Indexes**:
```sql
CREATE INDEX ix_goals_employee_id ON goals(employee_id);
CREATE INDEX ix_goals_target_date ON goals(target_date);
```

---

## 5. Lifecycle Service - Data Model

### Database: `lifecycle_db`

#### OnboardingChecklist
```csharp
public class OnboardingChecklist
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public OnboardingStatus Status { get; set; }

    public ICollection<OnboardingTask> Tasks { get; set; } = new List<OnboardingTask>();

    public DateTime CreatedAt { get; set; }
}

public enum OnboardingStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3
}
```

---

#### OffboardingChecklist
```csharp
public class OffboardingChecklist
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = null!;

    public DateTime InitiatedDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public OffboardingStatus Status { get; set; }

    public ICollection<OffboardingTask> Tasks { get; set; } = new List<OffboardingTask>();

    public DateTime CreatedAt { get; set; }
}

public enum OffboardingStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3
}
```

---

## 6. Compliance Service - Data Model

### Database: `compliance_db`

#### WorkAuthorization
```csharp
public class WorkAuthorization
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = null!;

    public string AuthorizationType { get; set; } = null!; // WorkPermit, Visa, Citizenship
    public string DocumentNumber { get; set; } = null!;
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }

    public WorkAuthorizationStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum WorkAuthorizationStatus
{
    Valid = 1,
    Expiring = 2, // Within 90 days
    Expired = 3
}
```

**Validation Rules**:
- ExpiryDate must be >= IssueDate
- DocumentNumber must be unique

**Indexes**:
```sql
CREATE INDEX ix_work_authorizations_employee_id ON work_authorizations(employee_id);
CREATE INDEX ix_work_authorizations_expiry_date ON work_authorizations(expiry_date);
CREATE UNIQUE INDEX ix_work_authorizations_document_number ON work_authorizations(document_number);
```

---

## 7. Career Service (Extended) - Data Model

### Database: `career_db` (existing + new training tables)

#### TrainingRecord
```csharp
public class TrainingRecord
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = null!;

    public Guid TrainingProgramId { get; set; }
    public TrainingProgram TrainingProgram { get; set; } = null!;

    public DateTime CompletionDate { get; set; }
    public decimal? Score { get; set; } // Percentage if applicable
    public bool PassedTraining { get; set; }

    public DateTime CreatedAt { get; set; }
}
```

---

#### TrainingProgram
```csharp
public class TrainingProgram
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!; // Unique
    public string? Description { get; set; }
    public int DurationHours { get; set; }
    public bool IsMandatory { get; set; }
    public int ValidityMonths { get; set; } // How long certification is valid

    public DateTime CreatedAt { get; set; }
}
```

---

#### Certification
```csharp
public class Certification
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = null!;

    public string CertificationName { get; set; } = null!;
    public string IssuingOrganization { get; set; } = null!;
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? CertificationNumber { get; set; }

    public DateTime CreatedAt { get; set; }
}
```

---

#### Skill
```csharp
public class Skill
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = null!;

    public string SkillName { get; set; } = null!; // C#, Python, Project Management
    public string Category { get; set; } = null!; // Technical, Leadership, Communication
    public int ProficiencyLevel { get; set; } // 1 = Beginner, 2 = Intermediate, 3 = Advanced, 4 = Expert
    public DateTime AcquiredDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

## Cross-Service Data Access Patterns

### Pattern 1: Denormalization (EmployeeNumber)
- **Why**: Avoid frequent cross-service lookups
- **Example**: LeaveRequest stores `EmployeeNumber` from Employee Service
- **Sync**: Update on `EmployeeTerminatedIntegrationEvent`

### Pattern 2: API Calls (Synchronous)
- **Why**: Real-time data retrieval when needed
- **Example**: Compensation Service calls Employee Service to get current Department for reporting

### Pattern 3: Integration Events (Asynchronous)
- **Why**: Eventual consistency for non-critical data
- **Example**: `EmployeeCreatedIntegrationEvent` triggers creation of LeaveBalance records

---

## Summary

| Service | Database | Tables | Key Entities |
|---------|----------|--------|--------------|
| Employee | employee_db | 9 | Employee, Department, Team, EmergencyContact |
| Leave | leave_db | 3 | LeaveRequest, LeaveBalance, LeaveType |
| Compensation | compensation_db | 4 | CompensationRecord, BenefitsEnrollment, Dependent |
| Performance | performance_db | 2 | PerformanceReview, Goal |
| Lifecycle | lifecycle_db | 2 | OnboardingChecklist, OffboardingChecklist |
| Compliance | compliance_db | 1 | WorkAuthorization |
| Career | career_db | 4 | TrainingRecord, TrainingProgram, Certification, Skill |

**Total**: 7 databases, 25 entity types

**Next Step**: Define API contracts in `/contracts/` directory.
