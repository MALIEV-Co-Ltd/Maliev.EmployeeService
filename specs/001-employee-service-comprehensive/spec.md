# Feature Specification: Employee Service - Comprehensive HR Master Data Management

**Feature Branch**: `001-employee-service-comprehensive`
**Created**: 2025-10-11
**Status**: Draft
**Input**: User description: "Employee Service microservice managing employee master data, employment records, HR workflows, and organizational structure for Maliev Co. Ltd."

## User Scenarios & Testing

### User Story 1 - Employee Self-Service Profile Management (Priority: P1)

Employees need to view and update their own personal information, emergency contacts, and review their employment details. This is the foundation for employee engagement and data accuracy, as employees are the primary source of truth for their personal information.

**Why this priority**: Core functionality that provides immediate value to all employees while reducing HR administrative burden. Without this, employees cannot access their own information, forcing all updates through HR staff.

**Independent Test**: Can be fully tested by creating an employee account, logging in, viewing profile data, updating emergency contacts, and verifying changes persist. Delivers immediate value by giving employees 24/7 access to their information.

**Acceptance Scenarios**:

1. **Given** an active employee is logged in, **When** they navigate to their profile, **Then** they see their personal information (name, date of birth, contact details, employment details) but cannot edit sensitive fields like salary or employee ID
2. **Given** an employee views their profile, **When** they update their emergency contact information, **Then** the changes are saved immediately, their manager receives a notification, and an audit log entry is created
3. **Given** an employee views their employment details, **When** they review their job title, department, manager, and work location, **Then** all information is current and matches HR records
4. **Given** an employee attempts to access compensation information, **When** they navigate to that section, **Then** they see their current salary information but cannot modify it

---

### User Story 2 - HR Personnel Employee Lifecycle Management (Priority: P1)

HR personnel need to manage the complete employee lifecycle from candidate to new hire through active employment to offboarding. This includes creating employee records, updating employment details, managing department assignments, and processing employment status changes.

**Why this priority**: Essential for HR operations. Without this capability, the organization cannot onboard new employees or maintain accurate employment records, blocking all other HR functions.

**Independent Test**: Can be fully tested by HR user creating a new employee record from candidate data, updating employment details during tenure, processing department transfers, and completing offboarding workflow. Delivers core HR operational capability.

**Acceptance Scenarios**:

1. **Given** Career Service publishes a `CandidateAccepted` event for a hired candidate, **When** Employee Service receives the event, **Then** a new employee record is created with employee ID and status "Pending Start", onboarding workflow is initiated, and IT/Facilities are notified for equipment provisioning
2. **Given** an employee is being transferred to a new department, **When** HR processes the department transfer, **Then** manager approvals are required, organizational hierarchy is updated, access control systems are notified, and email distribution lists are updated
3. **Given** an employee has resigned, **When** HR initiates the offboarding workflow, **Then** exit interview is scheduled, asset return checklist is created, final paycheck calculation begins, and system access revocation is queued for the termination date
4. **Given** HR is creating a new employee record, **When** they enter employment start date in the future, **Then** the system accepts the date and marks the employee as "Pending Start"
5. **Given** HR is updating employee information, **When** they assign a manager, **Then** the system validates the manager is an active employee and does not create circular reporting relationships

---

### User Story 3 - Manager Team Management and Oversight (Priority: P2)

Managers need to view their team structure, review direct and indirect reports' information (with appropriate access restrictions), approve leave requests, and track team performance and training completion.

**Why this priority**: Enables managers to effectively lead their teams and make informed decisions. While important, the system can function without this if HR acts as intermediary for approvals.

**Independent Test**: Can be fully tested by creating manager account with direct reports, viewing team organizational chart, receiving and approving leave request, and reviewing team training compliance. Delivers manager self-service capability.

**Acceptance Scenarios**:

1. **Given** a manager is logged in, **When** they view their team, **Then** they see all direct reports with basic information (name, job title, employment status, location) and can drill down to indirect reports
2. **Given** a direct report submits a leave request, **When** the manager receives the notification, **Then** they can review leave balance, requested dates, and approve or deny with comments
3. **Given** a manager views team information, **When** they attempt to access compensation details, **Then** access is denied unless they have HR specialist role
4. **Given** a manager reviews team training compliance, **When** they view the training dashboard, **Then** they see completion status for mandatory training with upcoming deadlines and overdue items highlighted

---

### User Story 4 - Leave and Absence Management (Priority: P2)

Employees need to request time off, view their leave balances, see public holidays, and track leave history. Managers need to approve leave requests considering team coverage and business needs. HR needs to configure leave policies, accrual rules, and blackout periods.

**Why this priority**: Critical for employee satisfaction and operational planning. However, can be temporarily managed through manual processes while core employee records (P1) are established.

**Independent Test**: Can be fully tested by employee submitting leave request, manager approving/denying request, system calculating leave balance with accruals and usage, and generating leave calendar. Delivers complete leave management capability.

**Acceptance Scenarios**:

1. **Given** an employee has accrued 15 days of annual leave, **When** they submit a request for 5 days, **Then** the system validates sufficient balance, checks for blackout periods, sends approval request to manager, and shows "Pending Approval" status
2. **Given** an employee views their leave balance, **When** they check entitlements, **Then** they see annual leave, sick leave, and parental leave with accrued amounts, used amounts, pending requests, and available balance
3. **Given** a leave request requires 30 days advance notice, **When** an employee submits a request with only 10 days notice, **Then** the system rejects the request with a clear error message explaining the minimum notice requirement
4. **Given** HR configures leave accrual rules, **When** they set annual leave accrual based on tenure, **Then** employees with 0-2 years get 10 days, 2-5 years get 15 days, and 5+ years get 20 days per year
5. **Given** the month-end arrives, **When** the automated leave accrual process runs, **Then** all active employees receive their monthly accrual, balances are updated, and employees approaching expiration receive notifications

---

### User Story 5 - Organizational Structure and Reporting Hierarchy (Priority: P2)

HR and leadership need to define and maintain the organizational structure including departments, teams, reporting relationships, and cost centers. Users need to visualize the org chart and understand reporting lines.

**Why this priority**: Important for clarity and governance, but the organization can function with basic department assignments (already in P1) while sophisticated hierarchy features are developed.

**Independent Test**: Can be fully tested by creating hierarchical department structure, assigning department heads, establishing dotted-line reporting relationships, viewing org chart visualization, and validating circular relationship prevention. Delivers organizational clarity.

**Acceptance Scenarios**:

1. **Given** HR is creating a new department, **When** they specify department name, parent department, department head, cost center code, and headcount limit, **Then** the department is created and appears in the organizational hierarchy
2. **Given** an employee is assigned to multiple teams (matrix organization), **When** their profile is viewed, **Then** their primary department and secondary team assignments are clearly displayed with primary manager and dotted-line managers
3. **Given** HR attempts to assign a manager, **When** the assignment would create a circular reporting relationship (A reports to B, B reports to C, C reports to A), **Then** the system prevents the assignment and displays an error message
4. **Given** a department approaches its headcount limit, **When** HR attempts to add another employee, **Then** the system warns that headcount limit is being approached and requires approval override
5. **Given** a user views the organizational chart, **When** they navigate the hierarchy, **Then** they see departments nested by reporting structure, department heads identified, and employee counts per department

---

### User Story 6 - Compensation and Benefits Administration (Priority: P3)

HR specialists and Finance personnel need to manage employee compensation including salary, bonuses, commissions, benefits enrollment, and beneficiary information with strict access controls and comprehensive audit logging.

**Why this priority**: Highly sensitive but can be managed through existing payroll systems initially. Important for HR analytics and pay equity, but not required for basic operations.

**Independent Test**: Can be fully tested by HR specialist recording salary information (encrypted), tracking salary history with effective dates, managing bonus structures, recording benefits elections, and reviewing compensation audit logs. Delivers complete compensation management.

**Acceptance Scenarios**:

1. **Given** an HR specialist records a salary change, **When** they enter the new salary with effective date, **Then** the salary is encrypted at rest, salary history is preserved, an audit log entry is created with timestamp and accessing user, and only authorized personnel can view
2. **Given** an employee enrolls in benefits during open enrollment, **When** they select health insurance plan and retirement contribution percentage, **Then** their selections are recorded, beneficiary information is captured, and the data is synchronized to the benefits administration platform
3. **Given** a Finance user views compensation data, **When** they generate a compensation report, **Then** they see salary ranges by department and job title, but individual employee identities are anonymized unless they have HR specialist privileges
4. **Given** an HR generalist attempts to access compensation details, **When** they try to view an employee's salary, **Then** access is denied and an audit log entry records the access attempt
5. **Given** a sales employee has commission structure, **When** HR records the commission plan, **Then** the commission rate, quota targets, and payment schedule are documented and accessible to Payroll Service

---

### User Story 7 - Performance Management and Goal Tracking (Priority: P3)

Managers and HR need to conduct performance reviews, set goals and objectives, provide feedback, manage performance improvement plans, and track skill development. Employees need to view their performance history and update progress on goals.

**Why this priority**: Important for employee development but not blocking for basic HR operations. Can be managed through separate performance management tools initially.

**Independent Test**: Can be fully tested by manager creating performance review cycle, setting employee goals, providing feedback, completing review with ratings, and tracking historical performance trends. Delivers performance management capability.

**Acceptance Scenarios**:

1. **Given** a quarterly performance review cycle begins, **When** HR initiates the cycle, **Then** all managers receive notifications with review deadlines, review templates are distributed, and employees are notified to prepare self-assessments
2. **Given** a manager is conducting a performance review, **When** they provide performance ratings and written feedback, **Then** the review is saved with timestamp, employee receives notification to review and acknowledge, and the review is added to employee's performance history
3. **Given** an employee is placed on a performance improvement plan (PIP), **When** the manager documents performance issues and improvement milestones, **Then** the PIP is tracked with milestone deadlines, progress updates are recorded, and HR receives escalation alerts for milestone failures
4. **Given** 360-degree feedback is collected for an employee, **When** peers, direct reports, and managers provide input, **Then** feedback is anonymized, aggregated by category, and presented to the employee and their manager with reviewer identities protected
5. **Given** an employee sets quarterly goals, **When** they document objectives with success criteria and target dates, **Then** the goals are visible to their manager, progress can be updated throughout the quarter, and completion status is tracked

---

### User Story 8 - Training and Certification Management (Priority: P3)

HR and managers need to track employee training completion, manage mandatory training requirements, monitor certification expirations, maintain skills matrix, and send automated reminders for upcoming training deadlines.

**Why this priority**: Important for compliance and workforce development, but can be managed manually or through learning management systems initially. Not blocking for core HR operations.

**Independent Test**: Can be fully tested by assigning mandatory training to employees, tracking completion with certificate storage, monitoring certification expiration dates, sending automated reminders, and generating training compliance reports. Delivers training oversight capability.

**Acceptance Scenarios**:

1. **Given** a new employee joins the organization, **When** their employment type and role are recorded, **Then** mandatory training courses are automatically assigned based on role requirements with deadlines calculated from start date
2. **Given** an employee completes a safety certification course, **When** they upload the certificate with completion date and expiration date, **Then** the certificate is stored securely, expiration is tracked, and automated renewal reminders are scheduled for 60 days before expiration
3. **Given** mandatory training becomes overdue, **When** the deadline passes without completion, **Then** the employee receives escalating reminders, their manager is notified, and HR receives compliance alert
4. **Given** HR reviews training compliance, **When** they generate a training report, **Then** they see completion rates by department, identify skill gaps, view upcoming certification expirations, and export results for compliance audits
5. **Given** an employee's professional certification expires, **When** the expiration date is reached, **Then** the employee and manager receive urgent notifications, the certification is marked as expired, and renewal requirements are displayed

---

### User Story 9 - Document Management and Compliance (Priority: P3)

HR needs to securely store and manage employee documents including contracts, offer letters, identification documents, performance reviews, disciplinary records, and policy acknowledgments with version control, access restrictions, and audit logging.

**Why this priority**: Critical for legal compliance and record-keeping, but many organizations manage this through separate document management systems. Can be integrated later while core employee data is prioritized.

**Independent Test**: Can be fully tested by uploading employment contract with encryption, implementing version control for document amendments, enforcing access controls based on document sensitivity, logging all document access, and tracking document expiration dates. Delivers secure document repository.

**Acceptance Scenarios**:

1. **Given** HR uploads an employment contract, **When** the document is saved, **Then** it is encrypted at rest and in transit, access is restricted to HR specialists and the specific employee, document metadata is indexed, and an audit log entry is created
2. **Given** an employment contract is amended, **When** HR uploads the updated version, **Then** the previous version is preserved with version history, the new version is marked as current, and the employee receives notification of the change
3. **Given** a work permit is uploaded with expiration date, **When** the document is stored, **Then** the expiration date is tracked, automated alerts are sent 90 days before expiration, and the document is linked to the employee's work authorization record
4. **Given** an employee's disciplinary record is accessed, **When** someone views the document, **Then** access is restricted to HR specialists and senior management, the access is logged with user identity, timestamp, and purpose, and the employee cannot view disciplinary documents about themselves
5. **Given** a new employee signs policy acknowledgments, **When** the signed documents are uploaded, **Then** they are associated with the specific policy version, the acknowledgment date is recorded, and HR can generate compliance reports showing which employees have acknowledged current policies

---

### User Story 10 - Onboarding and Offboarding Workflows (Priority: P2)

HR needs automated onboarding workflows to guide new hires from offer acceptance through first day readiness, and offboarding workflows to ensure proper knowledge transfer and asset return when employees leave.

**Why this priority**: Significantly improves HR efficiency and employee experience, but the organization can manage this manually initially. Higher priority than P3 features because it directly impacts new hire productivity and company security during offboarding.

**Independent Test**: Can be fully tested by initiating onboarding workflow for new hire, tracking completion of onboarding checklist items (equipment provisioning, account creation, orientation scheduling), completing offboarding workflow with asset return tracking, and archiving employee records. Delivers workflow automation.

**Acceptance Scenarios**:

1. **Given** a candidate accepts a job offer in Career Service, **When** Career Service publishes `CandidateAccepted` event and Employee Service receives it, **Then** onboarding workflow is initiated, equipment provisioning requests are sent to IT, account creation requests are sent to identity management system, orientation is scheduled, and onboarding buddy is assigned
2. **Given** a new hire's first day approaches, **When** 3 days before start date is reached, **Then** HR receives checklist reminder to verify all equipment is ready, accounts are created, workspace is prepared, and orientation materials are distributed
3. **Given** an employee submits resignation, **When** HR initiates offboarding workflow, **Then** exit interview is scheduled, asset return checklist is created (laptop, phone, badge, keys), knowledge transfer plan is requested, system access revocation is queued for termination date, and final paycheck calculation begins
4. **Given** offboarding is in progress, **When** the termination date arrives, **Then** system access is automatically revoked across all platforms, email is preserved for 90 days for knowledge retention, active directory entries are disabled, and distribution list memberships are removed
5. **Given** offboarding checklist is incomplete, **When** HR attempts to finalize the termination, **Then** the system prevents final paycheck release until all checklist items (asset return, knowledge transfer, exit interview) are marked complete

---

### User Story 11 - Work Authorization and Visa Tracking (Priority: P3)

HR needs to track work permits, visa status, sponsorship requirements, and right-to-work documentation for international employees with automated expiration alerts and compliance tracking.

**Why this priority**: Critical for legal compliance in organizations with international employees, but Maliev Co. Ltd. may primarily employ Thai nationals. This can be prioritized lower if the workforce is predominantly local.

**Independent Test**: Can be fully tested by recording work permit details with expiration dates, storing visa documentation, sending automated renewal reminders 90 days before expiration, flagging employees with expiring authorization, and generating compliance reports. Delivers immigration compliance capability.

**Acceptance Scenarios**:

1. **Given** HR hires an expatriate employee, **When** they record work authorization details, **Then** work permit type, issue date, expiration date, sponsorship status, and right-to-work documents are stored with automated expiration tracking
2. **Given** a work permit is expiring in 90 days, **When** the automated alert schedule runs, **Then** the employee receives a reminder, HR receives an alert, and the employee is flagged on compliance dashboard
3. **Given** a work permit expires, **When** the expiration date is reached without renewal, **Then** HR receives urgent alert, the employee is flagged as working without valid authorization, and immediate intervention is required
4. **Given** HR reviews work authorization compliance, **When** they generate the compliance report, **Then** they see all employees with work permits, upcoming expirations in the next 180 days, recently expired authorizations, and renewal status

---

### User Story 12 - Reporting, Analytics, and Bulk Operations (Priority: P3)

HR and leadership need comprehensive reports on headcount, turnover, diversity, compensation equity, span of control, training compliance, and leave utilization. HR needs bulk import/export capabilities for organizational changes and system migration.

**Why this priority**: Important for strategic planning and analytics, but basic reporting can be handled through database queries initially. Bulk operations are needed for initial implementation but can be manual for ongoing use.

**Independent Test**: Can be fully tested by generating headcount report by department, running turnover analysis with trends, producing diversity metrics dashboard, executing bulk salary increase for entire department, and performing data export for backup. Delivers analytics and administrative capabilities.

**Acceptance Scenarios**:

1. **Given** leadership requests headcount report, **When** HR generates the report, **Then** they see employee counts by department, location, employment type, and tenure band with trend analysis over time
2. **Given** HR analyzes turnover, **When** they run the turnover report, **Then** they see voluntary vs. involuntary termination rates, turnover by department, reasons for departure, and year-over-year trends
3. **Given** HR performs pay equity audit, **When** they generate compensation analysis, **Then** they see salary ranges by job title, tenure, and performance rating with statistical analysis identifying potential pay inequities, but individual employee identities are anonymized
4. **Given** HR implements company-wide 3% salary increase, **When** they execute bulk salary update, **Then** the system validates all records before commit, provides preview of changes, applies increases with single effective date, creates salary history entries for all employees, and creates consolidated audit log
5. **Given** HR exports employee data for backup, **When** they initiate the export, **Then** the system generates a CSV file with all employee records, respecting data privacy controls based on user's access level, and logs the export operation for compliance

---

### Edge Cases

- **Concurrent Edits**: What happens when two HR personnel edit the same employee record simultaneously? System must implement optimistic locking and detect conflicts, requiring the second user to refresh and re-apply their changes.

- **Manager Circular Relationship**: What happens when HR attempts to create a reporting relationship where A reports to B, B reports to C, and C reports to A? System must detect circular relationships during validation and prevent the assignment with clear error message.

- **Terminated Employee as Manager**: What happens when an employee who manages others is terminated? System must detect active direct reports and either require manager reassignment before termination or automatically reassign to the terminated manager's manager.

- **Leave Balance Negative**: What happens when an employee requests leave exceeding their available balance? System must either reject the request with error message, or allow it with manager override approval and flag the negative balance for HR review.

- **Probation Period Auto-Update**: What happens when an employee's probation end date is reached? System must automatically update employment status from "Probationary" to "Confirmed" and send notification to employee and manager.

- **Work Authorization Expiration**: What happens when an employee's work permit expires but they continue to be employed? System must flag the employee on compliance dashboard, send urgent alerts to HR, and potentially auto-suspend the employment status pending resolution.

- **Future-Dated Employment Start**: What happens when a new hire has a start date 2 months in the future? System must accept the record with "Pending Start" status, begin onboarding workflow at appropriate time (e.g., 2 weeks before start), and auto-update to "Active" on start date.

- **Department Deletion with Employees**: What happens when HR attempts to delete a department that still has active employees assigned? System must prevent deletion and require all employees to be reassigned to other departments first.

- **Bulk Operation Partial Failure**: What happens when a bulk salary update succeeds for 950 out of 1000 employees due to validation errors on 50 records? System must provide detailed error report identifying which records failed and why, allow correction and retry of failed records, and prevent partial updates from corrupting data integrity.

- **Document Storage Quota Exceeded**: What happens when HR attempts to upload a document but storage quota is exceeded? System must reject the upload with clear error message, notify administrators of quota exhaustion, and suggest archival or quota expansion.

- **Emergency Contact Missing**: What happens when an emergency occurs but the employee has not provided emergency contact information? System must flag incomplete emergency contacts during onboarding, send periodic reminders to employees with missing information, and alert HR to contact the employee directly.

- **Leave Request During Notice Period**: What happens when an employee in their notice period (resignation submitted, termination date set) requests leave that would extend beyond their termination date? System must validate leave dates against termination date and reject requests extending beyond the last day of employment.

- **Duplicate Employee ID**: What happens when HR attempts to create a new employee with an employee ID that already exists? System must enforce unique constraint on employee ID, reject the duplicate, and suggest the next available ID following the company numbering convention.

- **Performance Review After Termination**: What happens when a manager attempts to complete a performance review for an employee who has been terminated? System must allow completion of reviews for terminated employees if the review period occurred during active employment, but prevent new review creation.

- **Integration Service Unavailable**: What happens when the Payroll Service is unavailable during employee creation? System must use asynchronous messaging with retry logic, queue the integration event, implement circuit breaker to prevent cascading failures, and alert administrators of integration issues.

## Requirements

### Functional Requirements

#### Employee Profile and Personal Information

- **FR-001**: System MUST store employee personal information including full legal name with support for Thai, English, and other international name formats, preferred name for daily use, Thai national ID (13-digit format), date of birth, nationality, and citizenship status
- **FR-002**: System MUST enforce data privacy ensuring sensitive personal information is only accessible to authorized HR personnel and the employee themselves, with all access logged
- **FR-003**: System MUST allow employees to view their own profile information including personal details, employment information, and contact details
- **FR-004**: System MUST allow employees to update specific fields (emergency contacts, preferred name, personal phone and email) while restricting editing of sensitive fields (salary, employee ID, job title)
- **FR-005**: System MUST maintain comprehensive contact information including multiple phone numbers (mobile, home, emergency) with international dialing support, and email addresses (personal and work)
- **FR-006**: System MUST create audit log entries for all personal information access and modifications including timestamp, user identity, action performed, old and new values, and IP address

#### Employment Details and Lifecycle

- **FR-007**: System MUST support multiple employment types including full-time permanent, part-time, contractor, intern, and consultant with type-specific data requirements and business rules
- **FR-008**: System MUST track employment status including Active, On Leave, Suspended, and Terminated with state transition validation and audit trails
- **FR-009**: System MUST capture employment details including job title, employee ID following company numbering conventions, department assignment, direct manager, work location ID (references Career Service work locations catalog such as Bangkok Office, Chiang Mai Factory, Remote, Hybrid), employment start date, probation period end date, and contract type
- **FR-010**: System MUST automatically update employment status from Probationary to Confirmed when probation end date is reached
- **FR-011**: System MUST support fixed-term contract tracking with renewal dates and automated alerts 60 days before contract expiration
- **FR-012**: System MUST assign unique employee IDs following company numbering convention and prevent duplicate IDs
- **FR-013**: System MUST validate that employment start date is in the past for active employees and allow future dates for pending new hires
- **FR-014**: System MUST validate that probation end date is after start date and typically within 180 days
- **FR-015**: System MUST validate that termination date is after start date and that no future-dated activity exists after termination

#### Organizational Structure and Reporting Hierarchy

- **FR-016**: System MUST support hierarchical department structure with unlimited nesting depth to model complex organizational hierarchies
- **FR-017**: System MUST allow each department to have a designated department head, cost center code, budget allocation, and headcount limit
- **FR-018**: System MUST support matrix organizations where employees can belong to multiple teams with primary department and secondary team assignments
- **FR-019**: System MUST track both direct reporting relationships and dotted-line reporting relationships
- **FR-020**: System MUST validate manager assignments to ensure managers are active employees with appropriate permissions
- **FR-021**: System MUST detect and prevent circular reporting relationships (A reports to B, B reports to C, C reports to A)
- **FR-022**: System MUST support flexible team structures including project-based teams, functional teams, and cross-functional teams
- **FR-023**: System MUST send alerts when department headcount approaches the defined limit
- **FR-024**: System MUST trigger workflow approvals for department transfers and update related systems including access control, email distribution lists, and physical access badges
- **FR-025**: System MUST prevent department deletion if active employees are still assigned to that department

#### Compensation and Benefits

- **FR-026**: System MUST store salary information encrypted at rest with access restricted to HR specialists and Finance personnel only
- **FR-027**: System MUST track salary history with effective dates, previous salary amounts, change reasons, and approval records
- **FR-028**: System MUST manage bonus and commission structures for sales and performance-based roles including rates, quota targets, and payment schedules
- **FR-029**: System MUST record benefits enrollment selections including health insurance plans, retirement plan contributions, and voluntary benefits
- **FR-030**: System MUST maintain beneficiary information for insurance and retirement accounts
- **FR-031**: System MUST log all compensation data access with timestamp, accessing user, purpose, and data accessed for compliance and audit
- **FR-032**: System MUST provide compensation anonymization for analytics and reporting, showing salary ranges without individual employee identities unless user has HR specialist privileges

#### Leave and Absence Management

- **FR-033**: System MUST track leave entitlements including annual leave based on tenure, sick leave, parental leave per Thai labor law, and unpaid leave
- **FR-034**: System MUST calculate leave balances accounting for accruals, usage, expirations, and carryovers
- **FR-035**: System MUST support leave accrual rules that vary by employment tenure (e.g., 0-2 years = 10 days, 2-5 years = 15 days, 5+ years = 20 days annually)
- **FR-036**: System MUST allow employees to submit leave requests specifying leave type, start date, end date, and reason
- **FR-037**: System MUST validate leave requests against available balance, blackout periods, and minimum notice requirements
- **FR-038**: System MUST route leave requests to direct manager for approval with notification
- **FR-039**: System MUST support manager approval workflow with ability to approve, deny, or request modifications with comments
- **FR-040**: System MUST enforce blackout periods where leave cannot be requested or approved for critical business periods
- **FR-041**: System MUST enforce minimum notice requirements for leave requests (e.g., 30 days for extended leave)
- **FR-042**: System MUST send automated notifications when employees approach leave balance limits or expiration deadlines
- **FR-043**: System MUST track public holidays that vary by work location and country
- **FR-044**: System MUST support leave escalation workflows for extended absences requiring senior management approval

#### Performance Management

- **FR-045**: System MUST support performance review cycles on quarterly, semi-annual, and annual schedules
- **FR-046**: System MUST allow managers to provide performance ratings and written feedback for direct reports
- **FR-047**: System MUST track performance review history with timestamp, reviewer, ratings, and feedback for trend analysis
- **FR-048**: System MUST manage performance improvement plans (PIPs) with milestone tracking, progress updates, and outcome documentation
- **FR-049**: System MUST allow goal and objective setting with success criteria, target dates, and progress tracking
- **FR-050**: System MUST support 360-degree feedback collection from peers, direct reports, and managers with anonymization
- **FR-051**: System MUST maintain skill assessments and competency evaluations (skill IDs reference Career Service skills catalog) to support workforce planning
- **FR-052**: System MUST send notifications to managers and employees when review cycles begin and deadlines approach
- **FR-053**: System MUST allow employees to provide self-assessments as part of the review process

#### Training and Development

- **FR-054**: System MUST track completed training courses with completion date, certificate storage, and expiration date for time-limited certifications
- **FR-055**: System MUST automatically assign mandatory training to new employees based on employment type and role
- **FR-056**: System MUST track professional certifications including credential name, issuing body, issue date, expiration date, and renewal requirements
- **FR-057**: System MUST maintain a skills matrix showing employee competencies (skill IDs reference Career Service skills catalog), proficiency levels (1-5 scale), and development areas
- **FR-058**: System MUST support individual development plans with career progression goals and required competencies
- **FR-059**: System MUST send automated reminders for upcoming training deadlines 30, 14, and 7 days before due date
- **FR-060**: System MUST send automated reminders for expiring certifications 60, 30, and 14 days before expiration
- **FR-061**: System MUST escalate overdue mandatory training to employee's manager and HR for compliance enforcement
- **FR-062**: System MUST generate training compliance reports showing completion rates by department and identifying skill gaps

#### Document Management

- **FR-063**: System MUST provide secure storage for employment contracts, offer letters, identification documents, educational certificates, performance reviews, disciplinary records, resignation letters, and policy acknowledgments
- **FR-064**: System MUST encrypt all documents at rest and in transit using industry-standard encryption
- **FR-065**: System MUST implement version control for documents with amendment tracking and historical version preservation
- **FR-066**: System MUST enforce access controls based on document sensitivity (e.g., disciplinary records accessible only to HR specialists)
- **FR-067**: System MUST log all document access with timestamp, user identity, document accessed, and purpose for audit and legal discovery
- **FR-068**: System MUST track document expiration dates for time-sensitive documents (work permits, certifications) and send automated alerts
- **FR-069**: System MUST associate policy acknowledgments with specific policy versions and track which employees have acknowledged current policies
- **FR-070**: System MUST prevent document deletion and support document archival with retention policy enforcement

#### Emergency Contacts

- **FR-071**: System MUST maintain multiple emergency contacts per employee with full name, relationship, phone numbers, email addresses, and priority order
- **FR-072**: System MUST allow employees to update their own emergency contacts with manager notification for audit purposes
- **FR-073**: System MUST validate that each emergency contact has at least phone number or email address to enable contact
- **FR-074**: System MUST flag incomplete or outdated emergency contact information and send periodic reminders to employees
- **FR-075**: System MUST support international phone numbers with country code validation

#### Onboarding Workflow

- **FR-076**: System MUST initiate onboarding workflow when candidate is transitioned to new hire status
- **FR-077**: System MUST track offer acceptance, background check status, and employment start date
- **FR-078**: System MUST trigger equipment provisioning requests to IT for laptops, phones, and accessories
- **FR-079**: System MUST send account creation requests to identity management services for email, network access, and business applications
- **FR-080**: System MUST support role-specific onboarding checklists with different requirements for office workers, factory employees, local employees, expatriates, individual contributors, and managers
- **FR-081**: System MUST schedule orientation sessions and assign onboarding buddy or mentor
- **FR-082**: System MUST distribute required reading materials and policy documents with tracking of acknowledgment signatures
- **FR-083**: System MUST coordinate with IT, Facilities, and Finance to ensure new hire readiness before first day
- **FR-084**: System MUST send onboarding status reminders to HR 3 days before employee start date

#### Offboarding Workflow

- **FR-085**: System MUST initiate offboarding workflow when resignation is submitted or termination is processed
- **FR-086**: System MUST schedule exit interviews and track completion
- **FR-087**: System MUST create asset return checklist for laptops, phones, access badges, keys, and proprietary materials
- **FR-088**: System MUST coordinate final paycheck calculation including unused leave payout and pro-rated bonuses with Payroll Service
- **FR-089**: System MUST queue system access revocation for effective termination date to prevent unauthorized access
- **FR-090**: System MUST preserve email for knowledge retention while removing active directory entries and distribution list memberships
- **FR-091**: System MUST track knowledge transfer activities including documentation of ongoing projects and client relationships
- **FR-092**: System MUST prevent final paycheck release until all offboarding checklist items are marked complete
- **FR-093**: System MUST archive employee records in compliance with legal retention requirements after termination

#### Work Authorization and Visa Tracking

- **FR-094**: System MUST store work permit information including permit type, issue date, expiration date, issuing authority, and permit number
- **FR-095**: System MUST track visa types, validity periods, entry dates, and visa sponsorship status
- **FR-096**: System MUST store citizenship verification documents and right-to-work compliance documentation
- **FR-097**: System MUST send escalating alerts when work authorization is expiring: 90 days, 60 days, 30 days, and 14 days before expiration
- **FR-098**: System MUST flag employees working without valid work authorization for immediate HR intervention
- **FR-099**: System MUST generate work authorization compliance reports showing upcoming expirations and expired authorizations

#### Access Control and Security

- **FR-100**: System MUST implement role-based access control with roles including Employee, Manager, HR Generalist, HR Specialist, and System Administrator
- **FR-101**: System MUST allow employees to view and update only their own information (restricted fields)
- **FR-102**: System MUST allow managers to view direct and indirect reports' information with limited editing capabilities
- **FR-103**: System MUST allow HR generalists broad read access with editing rights for non-sensitive fields
- **FR-104**: System MUST allow HR specialists full access to compensation and disciplinary records
- **FR-105**: System MUST allow system administrators unrestricted access with all actions logged
- **FR-106**: System MUST create audit log entries for every data access and modification recording timestamp, user identity, action, old/new values, and IP address
- **FR-107**: System MUST comply with Thailand's Personal Data Protection Act (PDPA) requiring explicit consent for data collection and documented legal basis for processing
- **FR-108**: System MUST support data subject access requests allowing employees to retrieve all data held about them in portable format
- **FR-109**: System MUST support right to erasure for ex-employees after retention periods expire while preserving anonymized data for analytics
- **FR-110**: System MUST encrypt all personally identifiable information (PII) at rest and in transit using industry-standard algorithms
- **FR-111**: System MUST implement data retention policies that automatically archive or purge records after legally required periods

#### System Integrations

- **FR-112**: System MUST provide API for Payroll Service to retrieve employee compensation, deductions, and employment status
- **FR-113**: System MUST provide API for Time Tracking Service to validate employee IDs, retrieve work schedules, and sync leave calendars
- **FR-114**: System MUST provide API for Access Control Service to provision building access based on work location and employment status
- **FR-115**: System MUST integrate with email and collaboration platforms to create and deactivate accounts based on employee lifecycle events
- **FR-116**: System MUST integrate with benefits administration platforms to sync employee eligibility and enrollment selections
- **FR-117**: System MUST integrate with Career Service to transition candidate data into employee records upon hire via `CandidateAccepted` events containing candidate information and job position details
- **FR-118**: System MUST use asynchronous messaging for integrations where appropriate to prevent cascading failures
- **FR-119**: System MUST implement circuit breakers and retry logic for integration resilience
- **FR-120**: System MUST log all integration events including data sent and received, timestamps, and status for troubleshooting
- **FR-163**: System MUST integrate with Career Service to retrieve skills catalog (skill name, description, category) when displaying employee skill assessments, using Career Service Skills API
- **FR-164**: System MUST integrate with Career Service to retrieve work locations catalog (location name, address, type) when recording employee work location assignments, using Career Service Work Locations API
- **FR-165**: System MUST consume `CandidateAccepted` events from Career Service via message queue (RabbitMQ) to automatically initiate employee onboarding when a candidate is hired
- **FR-166**: When receiving `CandidateAccepted` event, system MUST extract candidate information (name, email, phone, job position ID, start date) and create a new employee record with status "Pending Start", then initiate onboarding workflow as defined in FR-076

#### Data Validation and Error Handling

- **FR-121**: System MUST validate that department assignments reference valid existing departments with active status
- **FR-122**: System MUST validate that manager span of control does not exceed 15 direct reports for managers of individual contributors or 8 direct reports for managers of managers. System warns at 80% of limit (12 and 6 respectively) and prevents assignment at the limit
- **FR-123**: System MUST provide user-friendly error messages that explain what went wrong and how to fix it without exposing technical details
- **FR-124**: System MUST provide field-level validation with immediate feedback as users enter data
- **FR-125**: System MUST implement cross-field validation ensuring related fields are consistent (e.g., termination date after start date)
- **FR-126**: System MUST implement optimistic locking to prevent lost updates when multiple users edit the same record simultaneously
- **FR-127**: System MUST detect concurrent edit conflicts and require the second user to refresh and re-apply changes

#### Search, Filtering, and Reporting

- **FR-128**: System MUST support employee search by name, employee ID, email, phone number, department, job title, employment status, and manager name
- **FR-129**: System MUST support advanced filtering combining multiple criteria (e.g., active employees in Engineering with 2+ years tenure)
- **FR-130**: System MUST support search result export to spreadsheet format respecting data privacy controls
- **FR-131**: System MUST provide headcount reports by department, location, employment type, and tenure band
- **FR-132**: System MUST provide turnover analysis showing voluntary vs. involuntary termination rates with trend analysis
- **FR-133**: System MUST provide diversity metrics tracking representation across gender, nationality, and age groups for DEI initiatives
- **FR-134**: System MUST provide compensation analysis showing salary ranges by role, tenure, and performance rating with anonymization
- **FR-135**: System MUST provide span of control reports identifying managers with excessive or insufficient direct reports
- **FR-136**: System MUST provide training completion reports showing compliance rates and identifying skill gaps
- **FR-137**: System MUST provide leave utilization analysis showing accrual, usage, and carryover patterns
- **FR-138**: System MUST provide performance distribution reports showing rating distributions to identify grade inflation or deflation

#### Bulk Operations and Data Management

- **FR-139**: System MUST support bulk import from CSV and Excel formats for initial data migration and periodic updates
- **FR-140**: System MUST support bulk export for disaster recovery, system migration, and reporting
- **FR-141**: System MUST validate all records before committing bulk operations with detailed error reports for failed records
- **FR-142**: System MUST provide preview capability for bulk operations allowing users to review changes before applying
- **FR-143**: System MUST implement automatic rollback if critical errors are detected during bulk operations to prevent partial updates
- **FR-144**: System MUST support bulk updates for organizational changes (company-wide salary increases, department restructures, policy changes)

#### Notifications and Alerts

- **FR-145**: System MUST send email notifications to employees when their profile is updated by HR with details of changes
- **FR-146**: System MUST send notifications to managers when direct reports submit leave requests requiring approval
- **FR-147**: System MUST send notifications to managers when performance review deadlines approach
- **FR-148**: System MUST send notifications to HR when work authorization or certifications are expiring
- **FR-149**: System MUST send notifications to HR when mandatory training becomes overdue
- **FR-150**: System MUST send recognition notifications to HR for employee milestone anniversaries and birthdays
- **FR-151**: System MUST send alerts to system administrators when integration failures occur
- **FR-152**: System MUST send alerts to system administrators when database storage approaches capacity
- **FR-153**: System MUST send alerts to system administrators when suspicious access patterns suggest security issues
- **FR-154**: System MUST allow users to configure notification preferences to prevent notification fatigue while ensuring critical alerts are delivered

#### Audit Logging

- **FR-155**: System MUST log all create, read, update, and delete operations on employee records with before and after values
- **FR-156**: System MUST log all authentication and authorization events including successful and failed login attempts
- **FR-157**: System MUST log all sensitive data access including compensation and disciplinary record views
- **FR-158**: System MUST log all bulk operations and administrative actions
- **FR-159**: System MUST log all integration events including data exchanged with external systems
- **FR-160**: Audit logs MUST be immutable to prevent tampering
- **FR-161**: Audit logs MUST be retained for minimum 7 years to satisfy legal requirements
- **FR-162**: Audit logs MUST be searchable by date range, user, entity type, and action for compliance investigations

### Key Entities

- **Employee**: Represents an individual employed by or affiliated with Maliev Co. Ltd. Attributes include employee ID, legal name, preferred name, personal identification (Thai national ID), date of birth, nationality, contact information (phones, emails), employment type (full-time, part-time, contractor, intern, consultant), employment status (active, on leave, suspended, terminated), job title, department, manager, work location ID (references Career Service work locations catalog for office locations like Bangkok Office, Chiang Mai Factory, or work arrangement like Remote/Hybrid), start date, probation end date, contract type, and termination date. Relationships to Department, Manager (self-referential), Leave Balances, Performance Reviews, Training Records, Documents, Emergency Contacts, Compensation History, and Work Authorization.

- **Department**: Represents an organizational unit within the company hierarchy. Attributes include department ID, department name, parent department (for hierarchical structure), department head (Employee reference), cost center code, budget allocation, headcount limit, and active status. Relationships to parent Department, child Departments, and Employees assigned to this department.

- **Team**: Represents cross-functional or project-based groups that may span departments. Attributes include team ID, team name, team type (project-based, functional, cross-functional), team lead (Employee reference), and active status. Relationships to Employees with team assignments.

- **Leave Balance**: Represents an employee's accrued and available leave entitlements. Attributes include employee reference, leave type (annual, sick, parental, unpaid), accrued amount, used amount, pending requests, available balance, expiration date, and carryover rules. Relationships to Employee and Leave Requests.

- **Leave Request**: Represents a request for time off. Attributes include employee reference, leave type, start date, end date, total days requested, reason, status (pending, approved, denied), approval date, approver (Manager reference), and approval comments. Relationships to Employee and Manager.

- **Compensation Record**: Represents salary and benefits information (encrypted). Attributes include employee reference, salary amount (encrypted), effective date, change reason, bonus structure, commission structure, benefits enrollment selections, and beneficiary information. Relationships to Employee and audit trail of Compensation History.

- **Performance Review**: Represents a performance evaluation. Attributes include employee reference, reviewer (Manager reference), review period start/end dates, review cycle (quarterly, semi-annual, annual), performance rating, written feedback, review date, employee acknowledgment date, and review status. Relationships to Employee, Manager, and Goals.

- **Goal**: Represents an employee objective. Attributes include employee reference, goal description, success criteria, target date, progress updates, completion status, and related performance review. Relationships to Employee and Performance Review.

- **Training Record**: Represents completed training or certifications. Attributes include employee reference, training course name, completion date, certificate document reference, expiration date (for time-limited certifications), training type (mandatory, voluntary), and training provider. Relationships to Employee and Document.

- **Skill**: Represents an employee competency assessment. Attributes include employee reference, skill ID (references Career Service skills catalog), proficiency level (1-5 scale), last assessed date, and development area flag. The skill name and description are retrieved from Career Service API. Relationships to Employee.

- **Document**: Represents an employee-related document stored in the system. Attributes include document ID, employee reference, document type (contract, offer letter, ID document, certificate, performance review, disciplinary record, resignation letter, policy acknowledgment), document file (encrypted), upload date, uploaded by (User reference), document version, expiration date, and access restrictions. Relationships to Employee and Document Versions.

- **Emergency Contact**: Represents a contact person for emergencies. Attributes include employee reference, contact name, relationship to employee, phone numbers, email addresses, and priority order. Relationships to Employee.

- **Onboarding Checklist**: Represents tasks to be completed for new hires. Attributes include employee reference, checklist item description, responsible party (HR, IT, Facilities, Manager), due date, completion status, completion date, and completed by (User reference). Relationships to Employee.

- **Offboarding Checklist**: Represents tasks to be completed when employees leave. Attributes include employee reference, checklist item description (asset return, knowledge transfer, exit interview, access revocation), responsible party, due date, completion status, completion date, and completed by (User reference). Relationships to Employee.

- **Work Authorization**: Represents immigration and work permit documentation. Attributes include employee reference, authorization type (work permit, visa), document number, issue date, expiration date, issuing authority, sponsorship status, and right-to-work verification. Relationships to Employee and Document.

- **Audit Log**: Represents a record of system activity. Attributes include log ID, timestamp, user identity, entity type, entity ID, action (create, read, update, delete), old values, new values, IP address, and access purpose. Immutable record with 7-year retention.

- **User**: Represents a system user with access credentials and permissions. Attributes include user ID, username, employee reference (if applicable), role (Employee, Manager, HR Generalist, HR Specialist, System Administrator), authentication credentials, last login date, and active status. Relationships to Employee and Audit Logs.

## Success Criteria

### Measurable Outcomes

- **SC-001**: New employees can be onboarded from candidate to first-day-ready status in under 5 business days with all equipment, accounts, and orientation scheduled
- **SC-002**: Employees can submit leave requests and receive manager approval or denial within 24 hours for standard requests
- **SC-003**: HR personnel can complete employee profile updates in under 2 minutes with automatic validation preventing data entry errors
- **SC-004**: 95% of leave balance calculations are accurate on first attempt with automated accrual, usage, and expiration tracking eliminating manual calculations
- **SC-005**: Work authorization expiration alerts provide 90-day advance notice with zero instances of employees working without valid authorization
- **SC-006**: System supports 500 concurrent users (all employees) without performance degradation during peak usage (leave request submission periods)
- **SC-007**: Employee data access requests are fulfilled within 48 hours providing complete data export in portable format for PDPA compliance
- **SC-008**: 100% of sensitive data access (compensation, disciplinary records) is logged with complete audit trail for compliance verification
- **SC-009**: Offboarding workflows ensure 100% asset recovery with no final paychecks released until all checklist items are complete
- **SC-010**: Employee self-service reduces HR administrative workload by 40% as employees update their own emergency contacts, view leave balances, and access pay information
- **SC-011**: Training compliance improves to 95% completion rate for mandatory training within deadline through automated reminders and manager escalations
- **SC-012**: Organizational reporting queries (headcount by department, turnover analysis, diversity metrics) complete in under 5 seconds enabling real-time decision making
- **SC-013**: Integration with downstream services (Payroll, Time Tracking, Access Control) achieves 99.5% reliability with automatic retry and circuit breaker preventing data inconsistencies
- **SC-014**: Zero compensation data breaches with encryption at rest and in transit, role-based access controls, and comprehensive audit logging
- **SC-015**: Manager approval workflows reduce leave request processing time by 60% compared to manual email-based approvals
- **SC-016**: Performance review cycles achieve 90% on-time completion through automated deadline reminders and manager notifications
- **SC-017**: Bulk operations (company-wide salary increases, organizational restructures) process 1000 employee records in under 10 minutes with validation and rollback capabilities
- **SC-018**: Employee satisfaction with HR self-service capabilities reaches 85% or higher based on quarterly surveys
- **SC-019**: Circular reporting relationship detection prevents 100% of invalid organizational hierarchy configurations
- **SC-020**: Document storage and retrieval achieves sub-2-second response time for document access supporting efficient HR operations

## Assumptions

- **A-001**: Maliev Co. Ltd. follows Thai labor law requirements for leave entitlements (minimum 6 days annual leave per year, sick leave provisions, parental leave as mandated)
- **A-002**: Company numbering convention for employee IDs follows a sequential pattern (e.g., EMP-0001, EMP-0002) or department-based pattern (e.g., ENG-001, HR-001)
- **A-003**: Maximum span of control for managers is assumed to be 15 direct reports for individual contributors, 8 direct reports for managers of managers unless specified otherwise
- **A-004**: Probation period is standardly 90-180 days (3-6 months) unless otherwise specified in employment contract
- **A-005**: Leave accrual is monthly (e.g., 15 days annual leave = 1.25 days accrued per month) with carryover limits defined by company policy
- **A-006**: Work authorization tracking primarily applies to expatriate employees, as Maliev Co. Ltd. is Thailand-based and most employees are Thai nationals
- **A-007**: Document retention follows Thai legal requirements: employment contracts retained for 2 years after termination, payroll records for 5 years, other HR records for 7 years
- **A-008**: Salary information encryption uses AES-256 or equivalent industry-standard encryption algorithm
- **A-009**: Performance review ratings follow a standardized scale (e.g., 1-5 or Exceeds Expectations, Meets Expectations, Needs Improvement, Unsatisfactory)
- **A-010**: Training completion tracking integrates with external Learning Management System (LMS) if one exists, or manages training records directly if no LMS is present
- **A-011**: Authentication and authorization leverage the existing Maliev authentication service (JWT-based) as specified in CLAUDE.md
- **A-012**: System operates in Thai time zone (ICT - UTC+7) for all date/time operations including leave calculations and notification scheduling
- **A-013**: Email notifications are sent via existing company email infrastructure (SMTP or email service API)
- **A-014**: Reporting and analytics anonymize individual employee data when showing aggregated metrics unless user has HR Specialist role with explicit need-to-know
- **A-015**: Integration with external systems (Payroll, Time Tracking, Access Control, Email platforms) uses RESTful APIs with JSON payloads unless specified otherwise
- **A-016**: Bulk operations are limited to users with HR Specialist or System Administrator roles for data integrity and security
- **A-017**: Emergency contact information supports international phone numbers for employees with family members outside Thailand
- **A-018**: Leave blackout periods are configurable by HR and vary by department (e.g., Finance during year-end close, Manufacturing during peak production)
- **A-019**: Onboarding checklists are customizable based on employment type and role, with standard templates provided for common scenarios
- **A-020**: System availability target is 99.5% uptime during business hours (Monday-Friday 8:00-18:00 ICT) with maintenance windows during off-hours
- **A-021**: Career Service is available and provides Skills catalog API, Work Locations catalog API, and publishes `CandidateAccepted` events via RabbitMQ when candidates are hired. Employee Service caches Career Service data to maintain functionality during temporary Career Service outages

## Open Questions

None - all clarifications have been resolved.

## Dependencies

- **Career Service**: Employee Service depends on Career Service for Skills catalog (to display skill names when showing employee competencies) and Work Locations catalog (to display location details when showing employee work locations). Employee Service also consumes `CandidateAccepted` events from Career Service to automatically initiate onboarding when candidates are hired
- **External System Integrations**: Employee Service depends on integration with Payroll Service (for compensation data sync), Time Tracking Service (for work schedule and leave calendar sync), Access Control Service (for building access provisioning), Email/Collaboration platforms (for account lifecycle management), Benefits Administration platform (for enrollment sync), and Recruitment system (for candidate-to-employee transition)
- **Authentication Service**: Employee Service depends on Maliev authentication service for JWT-based authentication and authorization as specified in the company's microservices architecture
- **Identity Management System**: Onboarding workflow depends on identity management system to create employee accounts across all company platforms
- **Google Secret Manager**: Service configuration depends on Google Secret Manager for secure storage of database connection strings, API keys, and encryption keys as specified in CLAUDE.md
- **PostgreSQL Database**: Service depends on PostgreSQL for data persistence with Entity Framework Core as the data access layer
- **Kubernetes Infrastructure**: Service deployment depends on GKE cluster with ArgoCD for GitOps-based continuous deployment
- **Email Service**: Notification capabilities depend on company email infrastructure (SMTP server or email service API) for sending alerts and reminders
- **Document Storage**: Document management depends on secure file storage system (cloud object storage like Google Cloud Storage or equivalent) for encrypted document storage
- **Learning Management System (Optional)**: Training tracking may integrate with existing LMS if available, or manage training records directly

## Testing Strategy

### Database Testing (NON-NEGOTIABLE)

- **ALL tests MUST use PostgreSQL database** - no in-memory databases permitted
- Integration tests MUST use real PostgreSQL instances via Docker containers
- Test database must be provisioned using Docker Compose or Testcontainers
- Each test class should use a fresh database instance or transaction rollback for isolation
- Test databases must use the same schema and migrations as production
- CI/CD pipelines MUST provision PostgreSQL test containers before running tests
- NO EF Core InMemoryDatabase provider usage allowed - violates Constitution Principle IV

**Rationale**: In-memory databases have different behavior, concurrency handling, and constraint enforcement than PostgreSQL. Testing against production-like databases ensures test fidelity, catches real-world database issues early (deadlocks, constraint violations, transaction isolation), and prevents false positives from in-memory quirks.

**Implementation Requirements**:
- Docker Compose file for local PostgreSQL test database (`docker-compose.test.yml`)
- Test base classes that handle PostgreSQL connection and cleanup
- CI workflow modifications to start PostgreSQL container before test execution
- Connection string configuration for test database

## Out of Scope

- **Payroll Calculation**: Employee Service manages employee data but does NOT perform payroll calculations, tax withholding, or payment processing - these are handled by the dedicated Payroll Service
- **Time and Attendance Tracking**: Employee Service does NOT track daily clock-in/clock-out, time worked, or overtime - these are handled by the Time Tracking Service
- **Recruitment and Applicant Tracking**: While Employee Service transitions candidates to employees via `CandidateAccepted` events, it does NOT manage job postings, applicant screening, interview scheduling, or recruitment pipeline - these are handled by the Career Service
- **Skills Catalog Management**: Employee Service tracks which skills each employee possesses and at what proficiency level, but does NOT manage the master catalog of skills (skill names, descriptions, categories) - this catalog is owned and managed by Career Service
- **Work Locations Catalog Management**: Employee Service records where each employee works, but does NOT manage the master catalog of work locations (location names, addresses, types) - this catalog is owned and managed by Career Service
- **Learning Content Management**: Employee Service tracks training completion but does NOT host training courses, videos, or learning materials - these are managed by Learning Management System
- **Physical Access Control Hardware**: Employee Service provides data to Access Control Service but does NOT directly control door locks, badge readers, or security cameras
- **Email Account Management**: Employee Service triggers account creation/deactivation but does NOT directly manage email servers, distribution lists, or mailbox quotas beyond API integration
- **Financial Budgeting and Planning**: While Employee Service tracks department cost centers and budget allocations, it does NOT perform financial planning, budget approval workflows, or expense tracking
- **Benefits Administration Platform**: Employee Service syncs enrollment data but does NOT manage insurance plan details, claims processing, or carrier integrations - these are handled by Benefits platform
- **Legal Contract Management**: Employee Service stores employment contracts as documents but does NOT provide contract authoring, legal review workflows, or e-signature capabilities beyond basic storage
- **Employee Social Networking**: Employee Service does NOT provide social features like employee directories with photos, team chat, or social recognition feeds - these may be provided by collaboration platforms
- **Talent Acquisition and Succession Planning**: While Employee Service tracks skills and performance, it does NOT provide talent pipeline management, succession planning workflows, or high-potential identification tools
- **Employee Wellness and Engagement**: Employee Service does NOT track employee wellness programs, engagement surveys, or workplace satisfaction metrics beyond what's captured in exit interviews
- **Project Management**: While Employee Service supports project-based team structures, it does NOT provide project planning, task management, or project tracking capabilities
