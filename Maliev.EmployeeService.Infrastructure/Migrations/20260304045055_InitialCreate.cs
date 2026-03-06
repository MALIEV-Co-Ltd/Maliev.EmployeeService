using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "employee");

            migrationBuilder.CreateTable(
                name: "attendance_records",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    check_in_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    check_out_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_attendance_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "employee",
                columns: table => new
                {
                    log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    principal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    old_values = table.Column<string>(type: "text", nullable: true),
                    new_values = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    purpose = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_audit_logs", x => x.log_id);
                    table.CheckConstraint("CK_AuditLog_Immutable", "1=1");
                });

            migrationBuilder.CreateTable(
                name: "bulk_jobs",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    total_records = table.Column<int>(type: "integer", nullable: false),
                    successful_records = table.Column<int>(type: "integer", nullable: false),
                    failed_records = table.Column<int>(type: "integer", nullable: false),
                    errors = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    initiated_by_principal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    result_data = table.Column<string>(type: "text", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_bulk_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee_termination_saga_states",
                schema: "employee",
                columns: table => new
                {
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_state = table.Column<string>(type: "text", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    termination_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    leave_balance_closed = table.Column<bool>(type: "boolean", nullable: false),
                    compensation_archived = table.Column<bool>(type: "boolean", nullable: false),
                    access_revoked = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_employee_termination_saga_states", x => x.correlation_id);
                });

            migrationBuilder.CreateTable(
                name: "incidents",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    incident_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    incident_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_incidents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "positions",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_positions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "saga_states",
                schema: "employee",
                columns: table => new
                {
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    saga_type = table.Column<string>(type: "text", nullable: false),
                    current_step = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_saga_states", x => x.correlation_id);
                });

            migrationBuilder.CreateTable(
                name: "saga_step_histories",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_name = table.Column<string>(type: "text", nullable: false),
                    step_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_saga_step_histories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                schema: "employee",
                columns: table => new
                {
                    principal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    preference_data = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_user_preferences", x => new { x.principal_id, x.scope });
                });

            migrationBuilder.CreateTable(
                name: "work_schedules",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<string>(type: "text", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_work_schedules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    parent_department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    department_head_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cost_center = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    headcount_limit = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_departments", x => x.id);
                    table.ForeignKey(
                        name: "f_k_departments_departments_parent_department_id",
                        column: x => x.parent_department_id,
                        principalSchema: "employee",
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    principal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    employee_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    preferred_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    nationality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    mobile_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    personal_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    work_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    employment_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    employment_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    job_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    manager_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dotted_line_manager_id = table.Column<Guid>(type: "uuid", nullable: true),
                    work_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    probation_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    termination_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    anonymized_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    national_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    job_application_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_employees", x => x.id);
                    table.ForeignKey(
                        name: "f_k_employees_departments_department_id",
                        column: x => x.department_id,
                        principalSchema: "employee",
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_employees_employees_dotted_line_manager_id",
                        column: x => x.dotted_line_manager_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_employees_employees_manager_id",
                        column: x => x.manager_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "emergency_contacts",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    relationship = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    priority_order = table.Column<int>(type: "integer", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_emergency_contacts", x => x.id);
                    table.ForeignKey(
                        name: "f_k_emergency_contacts__employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employment_histories",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_employment_histories", x => x.id);
                    table.ForeignKey(
                        name: "f_k_employment_histories_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    team_lead_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_type = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_teams", x => x.id);
                    table.ForeignKey(
                        name: "f_k_teams_employees_team_lead_id",
                        column: x => x.team_lead_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_team_assignments",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_employee_team_assignments", x => x.id);
                    table.ForeignKey(
                        name: "f_k_employee_team_assignments__teams_team_id",
                        column: x => x.team_id,
                        principalSchema: "employee",
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_employee_team_assignments_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_audit_logs_entity_type_entity_id",
                schema: "employee",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "i_x_audit_logs_principal_id",
                schema: "employee",
                table: "audit_logs",
                column: "principal_id");

            migrationBuilder.CreateIndex(
                name: "i_x_audit_logs_timestamp",
                schema: "employee",
                table: "audit_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "i_x_bulk_jobs_completed_at",
                schema: "employee",
                table: "bulk_jobs",
                column: "completed_at");

            migrationBuilder.CreateIndex(
                name: "i_x_bulk_jobs_initiated_by_principal_id",
                schema: "employee",
                table: "bulk_jobs",
                column: "initiated_by_principal_id");

            migrationBuilder.CreateIndex(
                name: "i_x_bulk_jobs_job_id",
                schema: "employee",
                table: "bulk_jobs",
                column: "job_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_bulk_jobs_job_type",
                schema: "employee",
                table: "bulk_jobs",
                column: "job_type");

            migrationBuilder.CreateIndex(
                name: "i_x_bulk_jobs_status",
                schema: "employee",
                table: "bulk_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_bulk_jobs_status_started_at",
                schema: "employee",
                table: "bulk_jobs",
                columns: new[] { "status", "started_at" });

            migrationBuilder.CreateIndex(
                name: "i_x_departments_department_head_id",
                schema: "employee",
                table: "departments",
                column: "department_head_id");

            migrationBuilder.CreateIndex(
                name: "i_x_departments_parent_department_id",
                schema: "employee",
                table: "departments",
                column: "parent_department_id");

            migrationBuilder.CreateIndex(
                name: "i_x_emergency_contacts_employee_id",
                schema: "employee",
                table: "emergency_contacts",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_emergency_contacts_employee_id_priority_order",
                schema: "employee",
                table: "emergency_contacts",
                columns: new[] { "employee_id", "priority_order" });

            migrationBuilder.CreateIndex(
                name: "i_x_employees_department_id",
                schema: "employee",
                table: "employees",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "i_x_employees_department_id_employment_status",
                schema: "employee",
                table: "employees",
                columns: new[] { "department_id", "employment_status" });

            migrationBuilder.CreateIndex(
                name: "i_x_employees_dotted_line_manager_id",
                schema: "employee",
                table: "employees",
                column: "dotted_line_manager_id");

            migrationBuilder.CreateIndex(
                name: "i_x_employees_employee_number",
                schema: "employee",
                table: "employees",
                column: "employee_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_employees_employment_status",
                schema: "employee",
                table: "employees",
                column: "employment_status");

            migrationBuilder.CreateIndex(
                name: "i_x_employees_employment_type",
                schema: "employee",
                table: "employees",
                column: "employment_type");

            migrationBuilder.CreateIndex(
                name: "i_x_employees_manager_id",
                schema: "employee",
                table: "employees",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "i_x_employees_principal_id",
                schema: "employee",
                table: "employees",
                column: "principal_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_employees_start_date",
                schema: "employee",
                table: "employees",
                column: "start_date");

            migrationBuilder.CreateIndex(
                name: "i_x_employees_termination_date",
                schema: "employee",
                table: "employees",
                column: "termination_date");

            migrationBuilder.CreateIndex(
                name: "i_x_employee_team_assignments_employee_id",
                schema: "employee",
                table: "employee_team_assignments",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_employee_team_assignments_team_id",
                schema: "employee",
                table: "employee_team_assignments",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "i_x_employee_termination_saga_states_current_state",
                schema: "employee",
                table: "employee_termination_saga_states",
                column: "current_state");

            migrationBuilder.CreateIndex(
                name: "i_x_employee_termination_saga_states_employee_id",
                schema: "employee",
                table: "employee_termination_saga_states",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_employment_histories_employee_id",
                schema: "employee",
                table: "employment_histories",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_saga_states_saga_type",
                schema: "employee",
                table: "saga_states",
                column: "saga_type");

            migrationBuilder.CreateIndex(
                name: "i_x_saga_states_status",
                schema: "employee",
                table: "saga_states",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_saga_step_histories_correlation_id",
                schema: "employee",
                table: "saga_step_histories",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "i_x_saga_step_histories_executed_at",
                schema: "employee",
                table: "saga_step_histories",
                column: "executed_at");

            migrationBuilder.CreateIndex(
                name: "i_x_teams_team_lead_id",
                schema: "employee",
                table: "teams",
                column: "team_lead_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_departments__employees_department_head_id",
                schema: "employee",
                table: "departments",
                column: "department_head_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_departments__employees_department_head_id",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropTable(
                name: "attendance_records",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "bulk_jobs",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "emergency_contacts",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "employee_team_assignments",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "employee_termination_saga_states",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "employment_histories",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "incidents",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "positions",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "saga_states",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "saga_step_histories",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "user_preferences",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "work_schedules",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "teams",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "employees",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "departments",
                schema: "employee");
        }
    }
}
