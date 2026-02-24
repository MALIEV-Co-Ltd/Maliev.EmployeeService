# Data Model: Employee Service

**Feature Branch**: `001-employee-service-comprehensive`
**Created**: 2025-10-12
**Status**: Design Phase

## Overview

This document defines the complete data model for the Employee Service, including all entities, value objects, enums, relationships, validation rules, and database schema. The model supports 12 user stories covering employee profiles, organizational hierarchy, leave management, performance tracking, training, document management, and compliance.

---

## Core Entities

### 1. Employee

**Purpose**: Represents an individual employed by or affiliated with Maliev Co. Ltd.

**Attributes**:

| Field | Type | Required | Constraints | Encryption | Notes |
|-------|------|----------|-------------|------------|-------|
| `Id` | UUID | Yes | Primary Key | No | Surrogate key |
| `EmployeeNumber` | string(20) | Yes | Unique, Format: EMP-####, HR-###, etc. | No | Business key, human-readable |
| `LegalFirstName` | string(100) | Yes | Thai or English characters | No | Official legal name |
| `LegalLastName` | string(100) | Yes | Thai or English characters | No | Official legal name |
| `LegalMiddleName` | string(100) | No | Thai or English characters | No | Optional middle name |
| `PreferredName` | string(100) | No | Any characters | No | Used in daily interactions |
| `ThaiNationalId` | string(13) | No | 13-digit format validation | **Yes** | Thai national ID (encrypted) |
| `PassportNumber` | string(20) | No | Alphanumeric | **Yes** | For expatriate employees |
| `DateOfBirth` | date | Yes | Must be 18+ years before start date | No | Age validation |
| `Gender` | enum | No | Male, Female, Other, PreferNotToSay | No | For diversity reporting |
| `Nationality` | string(100) | Yes | - | No | Country of citizenship |
| `EmploymentType` | enum | Yes | FullTime, PartTime, Contractor, Intern, Consultant | No | Determines benefits eligibility |
| `EmploymentStatus` | enum | Yes | PendingStart, Active, OnLeave, Suspended, Terminated | No | State machine transitions |
| `JobTitle` | string(200) | Yes | - | No | Current position title |
| `DepartmentId` | UUID | Yes | FK to Department | No | Organizational unit |
| `ManagerId` | UUID | No | FK to Employee (self-reference) | No | Direct manager |
| `WorkLocationId` | UUID | No | External reference to Career Service | No | Office location or Remote |
| `StartDate` | date | Yes | Can be future for pending hires | No | Employment start date |
| `ProbationEndDate` | date | No | Must be after StartDate | No | Typically 90-180 days |
| `ContractEndDate` | date | No | For fixed-term contracts | No | Triggers renewal alerts |
| `TerminationDate` | date | No | Must be after StartDate | No | Last working day |
| `TerminationReason` | enum | No | Resignation, Termination, Retirement, ContractEnd, Other | No | For turnover analysis |
| `IsActive` | boolean | Yes | Default: true | No | Soft delete flag |
| `CreatedAt` | timestamp | Yes | Auto-generated | No | Audit trail |
| `UpdatedAt` | timestamp | Yes | Auto-updated | No | Audit trail |
| `RowVersion` | byte[] | Yes | Concurrency token | No | Optimistic locking |

**Value Objects**:
- `LegalName`: Encapsulates first, middle, last names with validation for Thai/English characters
- `ContactInformation`: Email, phone numbers (embedded in Employee table)

**Relationships**:
- **Department**: Many-to-One (Employee → Department)
- **Manager**: Many-to-One (Employee → Employee, self-reference)
- **DirectReports**: One-to-Many (Employee → Employees who report to this manager)
- **EmergencyContacts**: One-to-Many (Employee → EmergencyContact)
- **LeaveBalances**: One-to-Many (Employee → LeaveBalance)
- **LeaveRequests**: One-to-Many (Employee → LeaveRequest)
- **Compensation**: One-to-Many (Employee → CompensationRecord, history)
- **PerformanceReviews**: One-to-Many (Employee → PerformanceReview)
- **Goals**: One-to-Many (Employee → Goal)
- **TrainingRecords**: One-to-Many (Employee → TrainingRecord)
- **Skills**: One-to-Many (Employee → Skill)
- **Documents**: One-to-Many (Employee → Document)
- **OnboardingChecklist**: One-to-Many (Employee → OnboardingChecklistItem)
- **OffboardingChecklist**: One-to-Many (Employee → OffboardingChecklistItem)
- **WorkAuthorization**: One-to-One (Employee → WorkAuthorization)

**Validation Rules**:
- `EmployeeNumber` must be unique across all employees
- `DateOfBirth` must be at least 18 years before `StartDate`
- `ProbationEndDate` must be after `StartDate` and typically ≤ 180 days
- `TerminationDate` must be after `StartDate`
- If `EmploymentStatus = Terminated`, `TerminationDate` is required
- `ManagerId` cannot equal `Id` (no self-reporting)
- Circular reporting relationships prevented (A → B → C → A)

**Indexes**:
- Primary Key: `Id`
- Unique: `EmployeeNumber`
- Composite: `(DepartmentId, EmploymentStatus)` for department queries
- Composite: `(ManagerId, IsActive)` for manager team queries
- Full-text: `(LegalFirstName, LegalLastName, PreferredName)` for search

---

### 2. Department

**Purpose**: Represents an organizational unit within the company hierarchy.

**Attributes**:

| Field | Type | Required | Constraints | Notes |
|-------|------|----------|-------------|-------|
| `Id` | UUID | Yes | Primary Key | Surrogate key |
| `DepartmentCode` | string(20) | Yes | Unique, e.g., ENG, HR, FIN | Business key |
| `DepartmentName` | string(200) | Yes | - | Display name |
| `ParentDepartmentId` | UUID | No | FK to Department (self-reference) | For hierarchy |
| `DepartmentHeadId` | UUID | No | FK to Employee | Department leader |
| `CostCenter` | string(50) | No | - | For financial tracking |
| `BudgetAllocation` | decimal(18,2) | No | ≥ 0 | Annual budget |
| `HeadcountLimit` | int | No | ≥ 0 | Max employees |
| `IsActive` | boolean | Yes | Default: true | Soft delete |
| `CreatedAt` | timestamp | Yes | Auto-generated | Audit trail |
| `UpdatedAt` | timestamp | Yes | Auto-updated | Audit trail |
| `RowVersion` | byte[] | Yes | Concurrency token | Optimistic locking |

**Relationships**:
- **ParentDepartment**: Many-to-One (Department → Department, self-reference)
- **ChildDepartments**: One-to-Many (Department → Departments)
- **DepartmentHead**: Many-to-One (Department → Employee)
- **Employees**: One-to-Many (Department → Employees)
- **DepartmentHierarchy**: Closure table for efficient ancestor/descendant queries

**Validation Rules**:
- `DepartmentCode` must be unique
- `ParentDepartmentId` cannot equal `Id` (no self-parenting)
- Circular hierarchy prevented (A → B → C → A)
- Cannot delete department if `Employees` exist (require reassignment first)
- `DepartmentHeadId` must reference an active employee in this department

**Indexes**:
- Primary Key: `Id`
- Unique: `DepartmentCode`
- Foreign Key: `ParentDepartmentId`
- Foreign Key: `DepartmentHeadId`

---

### 3. DepartmentHierarchy (Closure Table)

**Purpose**: Efficiently query department ancestors and descendants without recursive queries.

**Attributes**:

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `AncestorId` | UUID | Yes | FK to Department |
| `DescendantId` | UUID | Yes | FK to Department |
| `Depth` | int | Yes | 0 = self-reference, 1 = parent, 2 = grandparent, etc. |

**Relationships**:
- Composite Primary Key: `(AncestorId, DescendantId)`

**Maintenance**:
- Automatically updated when department hierarchy changes
- Includes self-references (Depth = 0)

**Usage Example**:
```sql
-- Get all descendants of Engineering department
SELECT d.* FROM Departments d
JOIN DepartmentHierarchy dh ON d.Id = dh.DescendantId
WHERE dh.AncestorId = 'eng-dept-uuid' AND dh.Depth > 0;

-- Get all ancestors of employee's department
SELECT d.* FROM Departments d
JOIN DepartmentHierarchy dh ON d.Id = dh.AncestorId
WHERE dh.DescendantId = 'employee-dept-uuid' AND dh.Depth > 0;
```

---

### 4. EmergencyContact

**Purpose**: Contact person for emergencies.

**Attributes**:

| Field | Type | Required | Constraints | Notes |
|-------|------|----------|-------------|-------|
| `Id` | UUID | Yes | Primary Key | Surrogate key |
| `EmployeeId` | UUID | Yes | FK to Employee | Owner |
| `ContactName` | string(200) | Yes | - | Full name |
| `Relationship` | string(100) | Yes | e.g., Spouse, Parent, Sibling | Relationship to employee |
| `PrimaryPhone` | string(20) | No | International format | Must have phone OR email |
| `SecondaryPhone` | string(20) | No | International format | Backup contact |
| `Email` | string(200) | No | Valid email format | Must have phone OR email |
| `Priority` | int | Yes | 1 = primary, 2 = secondary, etc. | Contact order |
| `IsActive` | boolean | Yes | Default: true | Soft delete |
| `CreatedAt` | timestamp | Yes | Auto-generated | Audit trail |
| `UpdatedAt` | timestamp | Yes | Auto-updated | Audit trail |

**Relationships**:
- **Employee**: Many-to-One (EmergencyContact → Employee)

**Validation Rules**:
- Must have at least `PrimaryPhone` OR `Email`
- `Priority` must be unique per employee (no two contacts with Priority = 1)

**Indexes**:
- Primary Key: `Id`
- Foreign Key: `EmployeeId`
- Composite: `(EmployeeId, Priority)` for ordering

---

### 5. LeaveBalance

**Purpose**: Tracks accrued and available leave entitlements per employee and leave type.

**Attributes**:

| Field | Type | Required | Constraints | Notes |
|-------|------|----------|-------------|-------|
| `Id` | UUID | Yes | Primary Key | Surrogate key |
| `EmployeeId` | UUID | Yes | FK to Employee | Owner |
| `LeaveType` | enum | Yes | AnnualLeave, SickLeave, ParentalLeave, UnpaidLeave | Leave category |
| `AccruedAmount` | decimal(5,2) | Yes | ≥ 0 | Total accrued days |
| `UsedAmount` | decimal(5,2) | Yes | ≥ 0 | Total used days |
| `PendingAmount` | decimal(5,2) | Yes | ≥ 0 | Days in pending requests |
| `AvailableAmount` | decimal(5,2) | Yes | Computed: Accrued - Used - Pending | Available for new requests |
| `AccrualRate` | decimal(5,2) | Yes | > 0 | Days accrued per period (e.g., 1.25/month) |
| `CarryoverLimit` | decimal(5,2) | No | ≥ 0 | Max days to carry to next year |
| `ExpirationDate` | date | No | - | When unused balance expires |
| `Year` | int | Yes | - | Calendar year for this balance |
| `CreatedAt` | timestamp | Yes | Auto-generated | Audit trail |
| `UpdatedAt` | timestamp | Yes | Auto-updated | Audit trail |

**Relationships**:
- **Employee**: Many-to-One (LeaveBalance → Employee)
- **LeaveRequests**: One-to-Many (LeaveBalance → LeaveRequests of same type)

**Validation Rules**:
- `AvailableAmount` = `AccruedAmount` - `UsedAmount` - `PendingAmount` (computed)
- `UsedAmount` + `PendingAmount` ≤ `AccruedAmount` (cannot overdraw without approval)
- Unique constraint: `(EmployeeId, LeaveType, Year)`

**Indexes**:
- Primary Key: `Id`
- Unique: `(EmployeeId, LeaveType, Year)`

---

### 6. LeaveRequest

**Purpose**: Represents a request for time off.

**Attributes**:

| Field | Type | Required | Constraints | Notes |
|-------|------|----------|-------------|-------|
| `Id` | UUID | Yes | Primary Key | Surrogate key |
| `EmployeeId` | UUID | Yes | FK to Employee | Requestor |
| `LeaveType` | enum | Yes | AnnualLeave, SickLeave, ParentalLeave, UnpaidLeave | Type of leave |
| `StartDate` | date | Yes | - | First day of leave |
| `EndDate` | date | Yes | ≥ StartDate | Last day of leave (inclusive) |
| `TotalDays` | decimal(5,2) | Yes | > 0 | Business days requested |
| `Reason` | text | No | Max 1000 chars | Explanation (optional) |
| `Status` | enum | Yes | PendingApproval, Approved, Denied, Cancelled | Request state |
| `SubmittedAt` | timestamp | Yes | Auto-generated | Submission timestamp |
| `ApprovedAt` | timestamp | No | - | Final approval timestamp |
| `ApprovalLevel` | int | Yes | Default: 1 | Current approval stage |
| `RequiredApprovalLevels` | int | Yes | Default: 1 | Total levels needed |
| `CreatedAt` | timestamp | Yes | Auto-generated | Audit trail |
| `UpdatedAt` | timestamp | Yes | Auto-updated | Audit trail |

**Relationships**:
- **Employee**: Many-to-One (LeaveRequest → Employee)
- **Approvals**: One-to-Many (LeaveRequest → LeaveApproval)

**Validation Rules**:
- `EndDate` ≥ `StartDate`
- `StartDate` must respect minimum notice period (e.g., 30 days for extended leave)
- `StartDate` and `EndDate` must not fall in blackout periods
- Employee must have sufficient `AvailableAmount` in corresponding `LeaveBalance`
- Cannot overlap with existing approved leave requests

**Indexes**:
- Primary Key: `Id`
- Foreign Key: `EmployeeId`
- Composite: `(EmployeeId, Status, StartDate)` for filtering

---

### 7. LeaveApproval

**Purpose**: Tracks approval chain for leave requests.

**Attributes**:

| Field | Type | Required | Constraints | Notes |
|-------|------|----------|-------------|-------|
| `Id` | UUID | Yes | Primary Key | Surrogate key |
| `LeaveRequestId` | UUID | Yes | FK to LeaveRequest | Request being approved |
| `ApproverId` | UUID | Yes | FK to Employee | Person who approved/denied |
| `ApprovalLevel` | int | Yes | 1, 2, 3, etc. | Stage in approval chain |
| `Status` | enum | Yes | Approved, Denied | Decision |
| `Comments` | text | No | Max 1000 chars | Explanation for decision |
| `ApprovedAt` | timestamp | Yes | Auto-generated | Decision timestamp |

**Relationships**:
- **LeaveRequest**: Many-to-One (LeaveApproval → LeaveRequest)
- **Approver**: Many-to-One (LeaveApproval → Employee)

**Validation Rules**:
- `ApprovalLevel` must match `LeaveRequest.ApprovalLevel` at time of approval
- Cannot have multiple approvals at same level for same request

**Indexes**:
- Primary Key: `Id`
- Composite: `(LeaveRequestId, ApprovalLevel)`

---

### 8. CompensationRecord

**Purpose**: Stores salary and benefits information (encrypted) with history.

**Attributes**:

| Field | Type | Required | Constraints | Encryption | Notes |
|-------|------|----------|-------------|------------|-------|
| `Id` | UUID | Yes | Primary Key | No | Surrogate key |
| `EmployeeId` | UUID | Yes | FK to Employee | No | Owner |
| `SalaryAmount` | decimal(18,2) | Yes | > 0 | **Yes** | Monthly/annual salary |
| `SalaryCurrency` | string(3) | Yes | ISO 4217 (e.g., THB, USD) | No | Currency code |
| `SalaryFrequency` | enum | Yes | Hourly, Monthly, Annual | No | Payment frequency |
| `EffectiveDate` | date | Yes | - | No | When this compensation takes effect |
| `ChangeReason` | enum | Yes | NewHire, Promotion, AnnualIncrease, MarketAdjustment, Other | No | Reason for change |
| `BonusStructure` | json | No | - | **Yes** | Bonus eligibility and rates |
| `CommissionStructure` | json | No | - | **Yes** | Commission rates and targets |
| `IsCurrent` | boolean | Yes | Default: false | No | Only one current per employee |
| `CreatedAt` | timestamp | Yes | Auto-generated | No | Audit trail |
| `CreatedBy` | UUID | Yes | FK to User | No | Who entered this record |

**Relationships**:
- **Employee**: Many-to-One (CompensationRecord → Employee)
- **CreatedBy**: Many-to-One (CompensationRecord → User)

**Validation Rules**:
- Only one `CompensationRecord` per employee can have `IsCurrent = true`
- `EffectiveDate` for new record must be ≥ previous record's `EffectiveDate`

**Indexes**:
- Primary Key: `Id`
- Composite: `(EmployeeId, IsCurrent)` for current salary lookup
- Foreign Key: `EmployeeId`

**Security**:
- Access restricted to HR Specialist and Finance roles
- All access logged to AuditLog

---

### 9. PerformanceReview

**Purpose**: Represents a performance evaluation.

**Attributes**:

| Field | Type | Required | Constraints | Notes |
|-------|------|----------|-------------|-------|
| `Id` | UUID | Yes | Primary Key | Surrogate key |
| `EmployeeId` | UUID | Yes | FK to Employee | Reviewee |
| `ReviewerId` | UUID | Yes | FK to Employee | Reviewer (typically manager) |
| `ReviewCycle` | enum | Yes | Quarterly, SemiAnnual, Annual | Review frequency |
| `ReviewPeriodStart` | date | Yes | - | Start of evaluation period |
| `ReviewPeriodEnd` | date | Yes | > ReviewPeriodStart | End of evaluation period |
| `PerformanceRating` | enum | Yes | Exceeds, Meets, NeedsImprovement, Unsatisfactory | Overall rating |
| `Feedback` | text | Yes | Max 5000 chars | Written evaluation |
| `EmployeeSelfAssessment` | text | No | Max 5000 chars | Employee's self-review |
| `ReviewStatus` | enum | Yes | Draft, SubmittedToEmployee, Acknowledged, Finalized | State |
| `ReviewDate` | date | Yes | - | Date review was completed |
| `AcknowledgedAt` | timestamp | No | - | When employee acknowledged |
| `CreatedAt` | timestamp | Yes | Auto-generated | Audit trail |

**Relationships**:
- **Employee**: Many-to-One (PerformanceReview → Employee)
- **Reviewer**: Many-to-One (PerformanceReview → Employee)
- **Goals**: One-to-Many (PerformanceReview → Goal)

**Validation Rules**:
- `ReviewPeriodEnd` > `ReviewPeriodStart`
- `ReviewerId` must be employee's manager or HR specialist
- `ReviewStatus` state transitions: Draft → SubmittedToEmployee → Acknowledged → Finalized

**Indexes**:
- Primary Key: `Id`
- Composite: `(EmployeeId, ReviewPeriodEnd)` for history queries

---

### 10. Goal

**Purpose**: Represents an employee objective.

**Attributes**:

| Field | Type | Required | Constraints | Notes |
|-------|------|----------|-------------|-------|
| `Id` | UUID | Yes | Primary Key | Surrogate key |
| `EmployeeId` | UUID | Yes | FK to Employee | Goal owner |
| `PerformanceReviewId` | UUID | No | FK to PerformanceReview | Associated review |
| `GoalDescription` | text | Yes | Max 2000 chars | What needs to be achieved |
| `SuccessCriteria` | text | Yes | Max 2000 chars | How success is measured |
| `TargetDate` | date | Yes | - | Deadline |
| `Status` | enum | Yes | NotStarted, InProgress, Completed, Abandoned | Progress state |
| `ProgressNotes` | text | No | Max 5000 chars | Ongoing updates |
| `CompletedAt` | timestamp | No | - | When goal achieved |
| `CreatedAt` | timestamp | Yes | Auto-generated | Audit trail |

**Relationships**:
- **Employee**: Many-to-One (Goal → Employee)
- **PerformanceReview**: Many-to-One (Goal → PerformanceReview, optional)

**Validation Rules**:
- If `Status = Completed`, `CompletedAt` is required

**Indexes**:
- Primary Key: `Id`
- Foreign Key: `EmployeeId`
- Composite: `(EmployeeId, TargetDate)` for deadline queries

---

### 11. TrainingRecord

**Purpose**: Tracks completed training courses and certifications.

**Attributes**:

| Field | Type | Required | Constraints | Notes |
|-------|------|----------|-------------|-------|
| `Id` | UUID | Yes | Primary Key | Surrogate key |
| `EmployeeId` | UUID | Yes | FK to Employee | Trainee |
| `TrainingName` | string(500) | Yes | - | Course or certification name |
| `TrainingType` | enum | Yes | Mandatory, Voluntary | Classification |
| `TrainingProvider` | string(200) | No | - | Organization providing training |
| `CompletionDate` | date | Yes | - | When training was completed |
| `ExpirationDate` | date | No | > CompletionDate | For time-limited certifications |
| `CertificateDocumentId` | UUID | No | FK to Document | Certificate file reference |
| `IsExpired` | boolean | Yes | Computed: ExpirationDate < Now | Expired flag |
| `CreatedAt` | timestamp | Yes | Auto-generated | Audit trail |

**Relationships**:
- **Employee**: Many-to-One (TrainingRecord → Employee)
- **CertificateDocument**: Many-to-One (TrainingRecord → Document, optional)

**Validation Rules**:
- If `ExpirationDate` is set, must be > `CompletionDate`

**Indexes**:
- Primary Key: `Id`
- Composite: `(EmployeeId, TrainingType)` for mandatory training queries
- Composite: `(ExpirationDate, IsExpired)` for expiration alerts

---

### 12. Skill

**Purpose**: Represents an employee competency assessment.

**Attributes**:

| Field | Type | Required | Constraints | Notes |
|-------|------|----------|-------------|-------|
| `Id` | UUID | Yes | Primary Key | Surrogate key |
| `EmployeeId` | UUID | Yes | FK to Employee | Skill owner |
| `SkillId` | UUID | Yes | External reference to Career Service | Skill from catalog |
| `ProficiencyLevel` | int | Yes | 1-5 (1=Beginner, 5=Expert) | Competency level |
| `LastAssessedDate` | date | Yes | - | When proficiency was evaluated |
| `IsDevelopmentArea` | boolean | Yes | Default: false | Flagged for improvement |
| `AssessedBy` | UUID | No | FK to Employee | Manager or self-assessment |
| `CreatedAt` | timestamp | Yes | Auto-generated | Audit trail |
| `UpdatedAt` | timestamp | Yes | Auto-updated | Audit trail |

**Relationships**:
- **Employee**: Many-to-One (Skill → Employee)
- **AssessedBy**: Many-to-One (Skill → Employee, optional)
- **SkillId**: External reference to Career Service Skills catalog (skill name, description retrieved via API)

**Validation Rules**:
- `ProficiencyLevel` must be 1-5
- Unique constraint: `(EmployeeId, SkillId)` (one proficiency record per skill per employee)

**Indexes**:
- Primary Key: `Id`
- Unique: `(EmployeeId, SkillId)`

---

### 13. Document

**Purpose**: Stores employee-related documents securely.

**Attributes**:

| Field | Type | Required | Constraints | Encryption | Notes |
|-------|------|----------|-------------|------------|-------|
| `Id` | UUID | Yes | Primary Key | No | Surrogate key |
| `EmployeeId` | UUID | Yes | FK to Employee | No | Document owner |
| `DocumentType` | enum | Yes | Contract, OfferLetter, IdDocument, Certificate, PerformanceReview, DisciplinaryRecord, ResignationLetter, PolicyAcknowledgment | No | Category |
| `FileName` | string(500) | Yes | - | No | Original file name |
| `FileStoragePath` | string(1000) | Yes | - | No | Cloud storage path |
| `FileSizeBytes` | long | Yes | > 0 | No | File size |
| `MimeType` | string(100) | Yes | - | No | Content type |
| `VersionNumber` | int | Yes | ≥ 1 | No | Document version |
| `UploadedAt` | timestamp | Yes | Auto-generated | No | Upload timestamp |
| `UploadedBy` | UUID | Yes | FK to User | No | Who uploaded |
| `ExpirationDate` | date | No | - | No | For time-sensitive docs |
| `AccessRestriction` | enum | Yes | Public, EmployeeOnly, ManagerAndHR, HRSpecialistOnly | No | Who can access |
| `IsCurrentVersion` | boolean | Yes | Default: true | No | Latest version flag |
| `PreviousVersionId` | UUID | No | FK to Document (self-reference) | No | Version chain |
| `IsDeleted` | boolean | Yes | Default: false | No | Soft delete |
| `CreatedAt` | timestamp | Yes | Auto-generated | No | Audit trail |

**Relationships**:
- **Employee**: Many-to-One (Document → Employee)
- **UploadedBy**: Many-to-One (Document → User)
- **PreviousVersion**: Many-to-One (Document → Document, self-reference)

**Validation Rules**:
- Documents are immutable (cannot modify after upload, only create new versions)
- Only one document of each `(EmployeeId, DocumentType, IsCurrentVersion=true)` unless multiple versions allowed
- Document files encrypted at rest in cloud storage

**Indexes**:
- Primary Key: `Id`
- Composite: `(EmployeeId, DocumentType, IsCurrentVersion)` for current document lookup

**Security**:
- Access controlled via `AccessRestriction` field
- All access logged to AuditLog

---

### 14. OnboardingChecklistItem

**Purpose**: Tracks tasks to complete for new hires.

**Attributes**:

| Field | Type | Required | Constraints | Notes |
|-------|------|----------|-------------|-------|
| `Id` | UUID | Yes | Primary Key | Surrogate key |
| `EmployeeId` | UUID | Yes | FK to Employee | New hire |
| `TaskDescription` | string(500) | Yes | - | What needs to be done |
| `ResponsibleParty` | enum | Yes | HR, IT, Facilities, Manager, Employee | Who is responsible |
| `DueDate` | date | Yes | - | Deadline |
| `CompletionStatus` | enum | Yes | NotStarted, InProgress, Completed | Task state |
| `CompletedAt` | timestamp | No | - | When task was completed |
| `CompletedBy` | UUID | No | FK to User | Who completed |
| `Notes` | text | No | Max 1000 chars | Additional details |
| `CreatedAt` | timestamp | Yes | Auto-generated | Audit trail |

**Relationships**:
- **Employee**: Many-to-One (OnboardingChecklistItem → Employee)
- **CompletedBy**: Many-to-One (OnboardingChecklistItem → User)

**Validation Rules**:
- If `CompletionStatus = Completed`, `CompletedAt` and `CompletedBy` are required

**Indexes**:
- Primary Key: `Id`
- Composite: `(EmployeeId, CompletionStatus)` for progress tracking

---

### 15. OffboardingChecklistItem

**Purpose**: Tracks tasks to complete when employees leave.

**Attributes**:

| Field | Type | Required | Constraints | Notes |
|-------|------|----------|-------------|-------|
| `Id` | UUID | Yes | Primary Key | Surrogate key |
| `EmployeeId` | UUID | Yes | FK to Employee | Departing employee |
| `TaskDescription` | string(500) | Yes | - | What needs to be done |
| `ResponsibleParty` | enum | Yes | HR, IT, Facilities, Manager, Employee | Who is responsible |
| `DueDate` | date | Yes | - | Deadline (typically before/on termination date) |
| `CompletionStatus` | enum | Yes | NotStarted, InProgress, Completed | Task state |
| `CompletedAt` | timestamp | No | - | When task was completed |
| `CompletedBy` | UUID | No | FK to User | Who completed |
| `Notes` | text | No | Max 1000 chars | Additional details |
| `CreatedAt` | timestamp | Yes | Auto-generated | Audit trail |

**Relationships**:
- **Employee**: Many-to-One (OffboardingChecklistItem → Employee)
- **CompletedBy**: Many-to-One (OffboardingChecklistItem → User)

**Validation Rules**:
- If `CompletionStatus = Completed`, `CompletedAt` and `CompletedBy` are required
- Cannot finalize termination if any items are not `Completed`

**Indexes**:
- Primary Key: `Id`
- Composite: `(EmployeeId, CompletionStatus)` for progress tracking

---

### 16. WorkAuthorization

**Purpose**: Tracks immigration and work permit documentation.

**Attributes**:

| Field | Type | Required | Constraints | Encryption | Notes |
|-------|------|----------|-------------|------------|-------|
| `Id` | UUID | Yes | Primary Key | No | Surrogate key |
| `EmployeeId` | UUID | Yes | FK to Employee | No | Owner (unique) |
| `AuthorizationType` | enum | Yes | WorkPermit, Visa, Citizenship | No | Type of authorization |
| `DocumentNumber` | string(50) | Yes | - | **Yes** | Permit/visa number |
| `IssueDate` | date | Yes | - | No | When issued |
| `ExpirationDate` | date | Yes | > IssueDate | No | When expires |
| `IssuingAuthority` | string(200) | Yes | - | No | Government body |
| `SponsorshipStatus` | enum | No | NotRequired, CompanySponsored, SelfSponsored | No | Sponsorship type |
| `IsValid` | boolean | Yes | Computed: ExpirationDate ≥ Now | No | Validity flag |
| `RenewalInProgress` | boolean | Yes | Default: false | No | Renewal status |
| `CreatedAt` | timestamp | Yes | Auto-generated | No | Audit trail |
| `UpdatedAt` | timestamp | Yes | Auto-updated | No | Audit trail |

**Relationships**:
- **Employee**: One-to-One (WorkAuthorization → Employee)
- **Documents**: One-to-Many (WorkAuthorization → Documents for permit copies)

**Validation Rules**:
- `ExpirationDate` > `IssueDate`
- One-to-one relationship: each employee has at most one `WorkAuthorization` record

**Indexes**:
- Primary Key: `Id`
- Unique: `EmployeeId`
- Index: `(ExpirationDate, IsValid)` for expiration alerts

---

### 17. AuditLog

**Purpose**: Immutable record of all system activity for compliance and debugging.

**Attributes**:

| Field | Type | Required | Constraints | Notes |
|-------|------|----------|-------------|-------|
| `Id` | UUID | Yes | Primary Key | Surrogate key |
| `Timestamp` | timestamp | Yes | Auto-generated | When action occurred |
| `UserId` | UUID | Yes | FK to User | Who performed action |
| `UserRole` | string(100) | Yes | - | Role at time of action |
| `EntityType` | string(100) | Yes | - | Entity affected (e.g., Employee) |
| `EntityId` | UUID | Yes | - | ID of affected entity |
| `Action` | enum | Yes | Create, Read, Update, Delete | Operation type |
| `OldValues` | json | No | - | Before state (for updates) |
| `NewValues` | json | No | - | After state (for creates/updates) |
| `IpAddress` | string(50) | No | - | Request origin |
| `Purpose` | string(500) | No | - | Reason for access (for sensitive data) |

**Relationships**:
- **User**: Many-to-One (AuditLog → User)

**Validation Rules**:
- Audit logs are immutable (INSERT only, no UPDATE/DELETE)
- Retention period: 7 years minimum

**Indexes**:
- Primary Key: `Id`
- Composite: `(EntityType, EntityId, Timestamp)` for entity history queries
- Composite: `(UserId, Timestamp)` for user activity queries
- Index: `Timestamp` for time-range queries

---

### 18. User

**Purpose**: Represents a system user with access credentials and permissions.

**Attributes**:

| Field | Type | Required | Constraints | Notes |
|-------|------|----------|-------------|-------|
| `Id` | UUID | Yes | Primary Key | Surrogate key |
| `Username` | string(100) | Yes | Unique | Login identifier |
| `EmployeeId` | UUID | No | FK to Employee | Employee reference (if employee user) |
| `Email` | string(200) | Yes | Valid email format | Contact email |
| `Role` | enum | Yes | Employee, Manager, HRGeneralist, HRSpecialist, SystemAdministrator | Authorization role |
| `IsActive` | boolean | Yes | Default: true | Account status |
| `LastLoginAt` | timestamp | No | - | Last successful login |
| `PasswordHash` | string(500) | No | - | Hashed password (if not using JWT) |
| `CreatedAt` | timestamp | Yes | Auto-generated | Audit trail |
| `UpdatedAt` | timestamp | Yes | Auto-updated | Audit trail |

**Relationships**:
- **Employee**: Many-to-One (User → Employee, optional)
- **AuditLogs**: One-to-Many (User → AuditLog)

**Validation Rules**:
- `Username` and `Email` must be unique
- If `Role = Manager`, `EmployeeId` must reference an employee with direct reports

**Indexes**:
- Primary Key: `Id`
- Unique: `Username`
- Unique: `Email`
- Foreign Key: `EmployeeId`

---

## Enumerations

### EmploymentType
- `FullTime`
- `PartTime`
- `Contractor`
- `Intern`
- `Consultant`

### EmploymentStatus
- `PendingStart` → `Active` (on start date)
- `Active` → `OnLeave` (during leave period)
- `OnLeave` → `Active` (after leave ends)
- `Active` → `Suspended` (disciplinary action)
- `Suspended` → `Active` (reinstatement)
- `Active` | `Suspended` → `Terminated` (termination)

### Gender
- `Male`
- `Female`
- `Other`
- `PreferNotToSay`

### TerminationReason
- `Resignation`
- `Termination`
- `Retirement`
- `ContractEnd`
- `Other`

### LeaveType
- `AnnualLeave`
- `SickLeave`
- `ParentalLeave`
- `UnpaidLeave`

### LeaveRequestStatus
- `PendingApproval`
- `Approved`
- `Denied`
- `Cancelled`

### ApprovalStatus
- `Approved`
- `Denied`

### ChangeReason (Compensation)
- `NewHire`
- `Promotion`
- `AnnualIncrease`
- `MarketAdjustment`
- `Other`

### SalaryFrequency
- `Hourly`
- `Monthly`
- `Annual`

### ReviewCycle
- `Quarterly`
- `SemiAnnual`
- `Annual`

### PerformanceRating
- `Exceeds`
- `Meets`
- `NeedsImprovement`
- `Unsatisfactory`

### ReviewStatus
- `Draft`
- `SubmittedToEmployee`
- `Acknowledged`
- `Finalized`

### GoalStatus
- `NotStarted`
- `InProgress`
- `Completed`
- `Abandoned`

### TrainingType
- `Mandatory`
- `Voluntary`

### DocumentType
- `Contract`
- `OfferLetter`
- `IdDocument`
- `Certificate`
- `PerformanceReview`
- `DisciplinaryRecord`
- `ResignationLetter`
- `PolicyAcknowledgment`

### AccessRestriction
- `Public` (all authenticated users)
- `EmployeeOnly` (employee and HR)
- `ManagerAndHR` (employee's manager and HR)
- `HRSpecialistOnly` (HR specialist only)

### ResponsibleParty
- `HR`
- `IT`
- `Facilities`
- `Manager`
- `Employee`

### CompletionStatus
- `NotStarted`
- `InProgress`
- `Completed`

### AuthorizationType
- `WorkPermit`
- `Visa`
- `Citizenship`

### SponsorshipStatus
- `NotRequired`
- `CompanySponsored`
- `SelfSponsored`

### AuditAction
- `Create`
- `Read`
- `Update`
- `Delete`

### UserRole
- `Employee`
- `Manager`
- `HRGeneralist`
- `HRSpecialist`
- `SystemAdministrator`

---

## Entity Relationship Diagram (Textual)

```
User
├── 1:N → AuditLog
└── N:1 → Employee (optional)

Employee
├── N:1 → Department
├── N:1 → Manager (Employee, self-reference)
├── 1:N → DirectReports (Employees)
├── 1:N → EmergencyContact
├── 1:N → LeaveBalance
├── 1:N → LeaveRequest
├── 1:N → CompensationRecord
├── 1:N → PerformanceReview (as reviewee)
├── 1:N → PerformanceReview (as reviewer)
├── 1:N → Goal
├── 1:N → TrainingRecord
├── 1:N → Skill
├── 1:N → Document
├── 1:N → OnboardingChecklistItem
├── 1:N → OffboardingChecklistItem
└── 1:1 → WorkAuthorization

Department
├── N:1 → ParentDepartment (self-reference)
├── 1:N → ChildDepartments
├── N:1 → DepartmentHead (Employee)
├── 1:N → Employees
└── N:N → DepartmentHierarchy (closure table)

LeaveRequest
├── N:1 → Employee
└── 1:N → LeaveApproval

LeaveApproval
├── N:1 → LeaveRequest
└── N:1 → Approver (Employee)

Document
├── N:1 → Employee
├── N:1 → UploadedBy (User)
└── N:1 → PreviousVersion (Document, self-reference)
```

---

## Database Schema (PostgreSQL DDL Summary)

```sql
-- Core tables
CREATE TABLE Users (...);
CREATE TABLE Employees (...);
CREATE TABLE Departments (...);
CREATE TABLE DepartmentHierarchy (...); -- Closure table

-- Employee details
CREATE TABLE EmergencyContacts (...);
CREATE TABLE LeaveBalances (...);
CREATE TABLE LeaveRequests (...);
CREATE TABLE LeaveApprovals (...);
CREATE TABLE CompensationRecords (...);

-- Performance and training
CREATE TABLE PerformanceReviews (...);
CREATE TABLE Goals (...);
CREATE TABLE TrainingRecords (...);
CREATE TABLE Skills (...);

-- Documents and workflows
CREATE TABLE Documents (...);
CREATE TABLE OnboardingChecklistItems (...);
CREATE TABLE OffboardingChecklistItems (...);
CREATE TABLE WorkAuthorizations (...);

-- Audit
CREATE TABLE AuditLogs (...);

-- Indexes (examples)
CREATE INDEX idx_employees_department_status ON Employees(DepartmentId, EmploymentStatus);
CREATE INDEX idx_employees_manager_active ON Employees(ManagerId, IsActive);
CREATE INDEX idx_leave_requests_employee_status ON LeaveRequests(EmployeeId, Status, StartDate);
CREATE INDEX idx_audit_logs_entity ON AuditLogs(EntityType, EntityId, Timestamp);
CREATE INDEX idx_audit_logs_user ON AuditLogs(UserId, Timestamp);
```

---

## Data Migration Considerations

### Initial Data Import
- **Source**: Existing HR system (Excel, legacy database)
- **Validation**: Pre-import validation script to check data integrity
- **Strategy**: Pilot migration with 50 employees, validate, then full migration
- **Rollback**: Maintain backup of legacy system during parallel run period

### Ongoing Synchronization
- **Career Service**: Skills and work locations fetched on-demand, cached locally
- **External Systems**: Payroll, Time Tracking sync via integration events

---

## Performance Optimization

### Query Optimization
- Use `.AsNoTracking()` for read-only queries to avoid EF change tracking overhead
- Eager loading with `.Include()` for related entities to avoid N+1 queries
- Pagination with `Skip()` and `Take()` for list endpoints

### Indexing Strategy
- Composite indexes on frequently queried columns
- Full-text search index on employee names
- Covering indexes for reporting queries

### Caching Strategy
- **L1 (In-Memory)**: Department hierarchy (rarely changes), public holidays
- **L2 (Redis)**: Employee profiles, leave balances (invalidate on update)

---

## Security and Compliance

### Data Encryption
- **At Rest**: Sensitive fields (salary, national ID, document storage) encrypted with AES-256
- **In Transit**: TLS 1.3 for all API communication

### Access Control
- Role-based authorization enforced at API and database levels
- Row-level security for sensitive tables (compensation)

### Audit Logging
- All data access logged to immutable `AuditLogs` table
- 7-year retention for compliance

### PDPA Compliance
- Explicit consent tracking for data collection
- Data subject access request support (export all employee data)
- Right to erasure (anonymize after retention period)

---

**Document Status**: Complete
**Next Steps**: Create contracts/ directory with OpenAPI schemas, create quickstart.md
