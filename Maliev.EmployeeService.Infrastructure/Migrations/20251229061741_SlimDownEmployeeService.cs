using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SlimDownEmployeeService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_departments__employees_department_head_id",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropForeignKey(
                name: "f_k_departments_departments_parent_department_id",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropForeignKey(
                name: "f_k_teams_employees_team_lead_id",
                schema: "employee",
                table: "teams");

            migrationBuilder.DropTable(
                name: "benefits",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "benefits_enrollments",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "certifications",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "compensation_records",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "dependents",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "disciplinary_actions",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "document_versions",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "employee_benefits",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "exit_interviews",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "goals",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "leave_approvals",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "leave_balances",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "leave_policies",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "mandatory_training_requirements",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "offboarding_checklists",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "offboarding_tasks",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "onboarding_checklists",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "performance_improvement_plans",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "personal_documents",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "salary_histories",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "skills",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "training_records",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "trainings",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "work_authorizations",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "performance_reviews",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "leave_requests",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "documents",
                schema: "employee");

            migrationBuilder.DropIndex(
                name: "i_x_teams_is_active",
                schema: "employee",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "i_x_teams_name",
                schema: "employee",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "i_x_teams_team_type_is_active",
                schema: "employee",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "i_x_employee_team_assignments_employee_id_is_primary",
                schema: "employee",
                table: "employee_team_assignments");

            migrationBuilder.DropIndex(
                name: "i_x_employee_team_assignments_employee_id_team_id",
                schema: "employee",
                table: "employee_team_assignments");

            migrationBuilder.DropIndex(
                name: "i_x_departments_department_head_id",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "i_x_departments_is_active",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "i_x_departments_name",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "i_x_departments_parent_department_id_is_active",
                schema: "employee",
                table: "departments");

            migrationBuilder.AlterColumn<string>(
                name: "team_type",
                schema: "employee",
                table: "teams",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "employee",
                table: "teams",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "employee",
                table: "teams",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "anonymized_at",
                schema: "employee",
                table: "employees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "department_id1",
                schema: "employee",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "employee",
                table: "departments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "employee",
                table: "departments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cost_center",
                schema: "employee",
                table: "departments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "department_head_id1",
                schema: "employee",
                table: "departments",
                type: "uuid",
                nullable: true);

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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "i_x_employees_department_id1",
                schema: "employee",
                table: "employees",
                column: "department_id1");

            migrationBuilder.CreateIndex(
                name: "i_x_departments_department_head_id1",
                schema: "employee",
                table: "departments",
                column: "department_head_id1");

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

            migrationBuilder.AddForeignKey(
                name: "f_k_departments__employees_department_head_id1",
                schema: "employee",
                table: "departments",
                column: "department_head_id1",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_departments_departments_parent_department_id",
                schema: "employee",
                table: "departments",
                column: "parent_department_id",
                principalSchema: "employee",
                principalTable: "departments",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_employees_departments_department_id1",
                schema: "employee",
                table: "employees",
                column: "department_id1",
                principalSchema: "employee",
                principalTable: "departments",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_teams_employees_team_lead_id",
                schema: "employee",
                table: "teams",
                column: "team_lead_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_departments__employees_department_head_id1",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropForeignKey(
                name: "f_k_departments_departments_parent_department_id",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropForeignKey(
                name: "f_k_employees_departments_department_id1",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "f_k_teams_employees_team_lead_id",
                schema: "employee",
                table: "teams");

            migrationBuilder.DropTable(
                name: "saga_states",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "saga_step_histories",
                schema: "employee");

            migrationBuilder.DropIndex(
                name: "i_x_employees_department_id1",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "i_x_departments_department_head_id1",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "anonymized_at",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "department_id1",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "department_head_id1",
                schema: "employee",
                table: "departments");

            migrationBuilder.AlterColumn<string>(
                name: "team_type",
                schema: "employee",
                table: "teams",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "employee",
                table: "teams",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "employee",
                table: "teams",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "employee",
                table: "departments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "employee",
                table: "departments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cost_center",
                schema: "employee",
                table: "departments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "benefits",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_benefits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "benefits_enrollments",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    beneficiary_information = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    enrollment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    health_insurance_plan = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retirement_contribution = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_benefits_enrollments", x => x.id);
                    table.ForeignKey(
                        name: "f_k_benefits_enrollments__employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "certifications",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    issued_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_certifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "compensation_records",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bonus_structure = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    change_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    commission_structure = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "THB"),
                    effective_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    salary_amount = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_compensation_records", x => x.id);
                    table.ForeignKey(
                        name: "f_k_compensation_records__employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dependents",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    relationship = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_dependents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "disciplinary_actions",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_disciplinary_actions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    access_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expiration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    storage_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    upload_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_documents", x => x.id);
                    table.ForeignKey(
                        name: "f_k_documents__employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_documents__employees_uploaded_by",
                        column: x => x.uploaded_by,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_benefits",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    benefit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_employee_benefits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "exit_interviews",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conducted_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_exit_interviews", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leave_balances",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    carry_forward_days = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    leave_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    pending_days = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    total_entitlement = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    used_days = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_leave_balances", x => x.id);
                    table.ForeignKey(
                        name: "f_k_leave_balances_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leave_policies",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    accrual_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    blackout_periods_json = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    leave_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    max_carryover = table.Column<int>(type: "integer", nullable: true),
                    minimum_notice_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_leave_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leave_requests",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    approver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    approval_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    leave_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    total_days = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_leave_requests", x => x.id);
                    table.ForeignKey(
                        name: "f_k_leave_requests_employees_approver_id",
                        column: x => x.approver_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_leave_requests_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mandatory_training_requirements",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deadline_days_from_start = table.Column<int>(type: "integer", nullable: false),
                    employment_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    job_role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    required_courses = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_mandatory_training_requirements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "offboarding_checklists",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    blocks_final_paycheck = table.Column<bool>(type: "boolean", nullable: false),
                    completed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completion_status = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    item_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    responsible_party = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_offboarding_checklists", x => x.id);
                    table.ForeignKey(
                        name: "f_k_offboarding_checklists_employees_completed_by",
                        column: x => x.completed_by,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_offboarding_checklists_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "offboarding_tasks",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    task_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_offboarding_tasks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_checklists",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completion_status = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    item_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    responsible_party = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_onboarding_checklists", x => x.id);
                    table.ForeignKey(
                        name: "f_k_onboarding_checklists_employees_completed_by",
                        column: x => x.completed_by,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_onboarding_checklists_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "performance_improvement_plans",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    issues_documented = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    milestones = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    outcome = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    progress_notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_performance_improvement_plans", x => x.id);
                    table.ForeignKey(
                        name: "f_k_performance_improvement_plans_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_performance_improvement_plans_employees_manager_id",
                        column: x => x.manager_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "performance_reviews",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acknowledged_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    feedback = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rating = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    review_cycle = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    review_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    review_period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    review_period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    self_assessment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Draft")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_performance_reviews", x => x.id);
                    table.ForeignKey(
                        name: "f_k_performance_reviews_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_performance_reviews_employees_reviewer_id",
                        column: x => x.reviewer_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "personal_documents",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    document_number = table.Column<string>(type: "text", nullable: false),
                    document_type = table.Column<string>(type: "text", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_personal_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "salary_histories",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_salary_histories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_development_area = table.Column<bool>(type: "boolean", nullable: false),
                    last_assessed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    proficiency_level = table.Column<int>(type: "integer", nullable: false),
                    skill_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_skills", x => x.id);
                    table.ForeignKey(
                        name: "f_k_skills_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "training_records",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    certificate_document_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    completion_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    course_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    training_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_training_records", x => x.id);
                    table.ForeignKey(
                        name: "f_k_training_records_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trainings",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    completion_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_trainings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_versions",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    change_description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    storage_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    upload_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_document_versions", x => x.id);
                    table.ForeignKey(
                        name: "f_k_document_versions__employees_uploaded_by",
                        column: x => x.uploaded_by,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_document_versions_documents_document_id",
                        column: x => x.document_id,
                        principalSchema: "employee",
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_authorizations",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    right_to_work_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    authorization_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    document_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    expiration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    issuing_authority = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    sponsorship_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_work_authorizations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_work_authorizations_documents_right_to_work_document_id",
                        column: x => x.right_to_work_document_id,
                        principalSchema: "employee",
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_work_authorizations_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leave_approvals",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    approver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_level = table.Column<int>(type: "integer", nullable: false),
                    comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    decision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    decision_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_leave_approvals", x => x.id);
                    table.ForeignKey(
                        name: "f_k_leave_approvals__leave_requests_leave_request_id",
                        column: x => x.leave_request_id,
                        principalSchema: "employee",
                        principalTable: "leave_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_leave_approvals_employees_approver_id",
                        column: x => x.approver_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "goals",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    performance_review_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completion_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    progress_updates = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    success_criteria = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    target_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_goals", x => x.id);
                    table.ForeignKey(
                        name: "f_k_goals__performance_reviews_performance_review_id",
                        column: x => x.performance_review_id,
                        principalSchema: "employee",
                        principalTable: "performance_reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_goals_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_teams_is_active",
                schema: "employee",
                table: "teams",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "i_x_teams_name",
                schema: "employee",
                table: "teams",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "i_x_teams_team_type_is_active",
                schema: "employee",
                table: "teams",
                columns: new[] { "team_type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "i_x_employee_team_assignments_employee_id_is_primary",
                schema: "employee",
                table: "employee_team_assignments",
                columns: new[] { "employee_id", "is_primary" });

            migrationBuilder.CreateIndex(
                name: "i_x_employee_team_assignments_employee_id_team_id",
                schema: "employee",
                table: "employee_team_assignments",
                columns: new[] { "employee_id", "team_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_departments_department_head_id",
                schema: "employee",
                table: "departments",
                column: "department_head_id");

            migrationBuilder.CreateIndex(
                name: "i_x_departments_is_active",
                schema: "employee",
                table: "departments",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "i_x_departments_name",
                schema: "employee",
                table: "departments",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "i_x_departments_parent_department_id_is_active",
                schema: "employee",
                table: "departments",
                columns: new[] { "parent_department_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "i_x_benefits_enrollments_employee_id",
                schema: "employee",
                table: "benefits_enrollments",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_benefits_enrollments_employee_id_enrollment_date",
                schema: "employee",
                table: "benefits_enrollments",
                columns: new[] { "employee_id", "enrollment_date" });

            migrationBuilder.CreateIndex(
                name: "i_x_benefits_enrollments_enrollment_date",
                schema: "employee",
                table: "benefits_enrollments",
                column: "enrollment_date");

            migrationBuilder.CreateIndex(
                name: "i_x_compensation_records_effective_date",
                schema: "employee",
                table: "compensation_records",
                column: "effective_date");

            migrationBuilder.CreateIndex(
                name: "i_x_compensation_records_employee_id",
                schema: "employee",
                table: "compensation_records",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_compensation_records_employee_id_effective_date",
                schema: "employee",
                table: "compensation_records",
                columns: new[] { "employee_id", "effective_date" });

            migrationBuilder.CreateIndex(
                name: "i_x_documents_access_level",
                schema: "employee",
                table: "documents",
                column: "access_level");

            migrationBuilder.CreateIndex(
                name: "i_x_documents_document_type",
                schema: "employee",
                table: "documents",
                column: "document_type");

            migrationBuilder.CreateIndex(
                name: "i_x_documents_document_type_access_level",
                schema: "employee",
                table: "documents",
                columns: new[] { "document_type", "access_level" });

            migrationBuilder.CreateIndex(
                name: "i_x_documents_employee_id",
                schema: "employee",
                table: "documents",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_documents_employee_id_document_type",
                schema: "employee",
                table: "documents",
                columns: new[] { "employee_id", "document_type" });

            migrationBuilder.CreateIndex(
                name: "i_x_documents_employee_id_is_archived",
                schema: "employee",
                table: "documents",
                columns: new[] { "employee_id", "is_archived" });

            migrationBuilder.CreateIndex(
                name: "i_x_documents_expiration_date",
                schema: "employee",
                table: "documents",
                column: "expiration_date");

            migrationBuilder.CreateIndex(
                name: "i_x_documents_upload_date",
                schema: "employee",
                table: "documents",
                column: "upload_date");

            migrationBuilder.CreateIndex(
                name: "i_x_documents_uploaded_by",
                schema: "employee",
                table: "documents",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "i_x_document_versions_document_id",
                schema: "employee",
                table: "document_versions",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "i_x_document_versions_document_id_version_number",
                schema: "employee",
                table: "document_versions",
                columns: new[] { "document_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_document_versions_upload_date",
                schema: "employee",
                table: "document_versions",
                column: "upload_date");

            migrationBuilder.CreateIndex(
                name: "i_x_document_versions_uploaded_by",
                schema: "employee",
                table: "document_versions",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "i_x_goals_completion_status",
                schema: "employee",
                table: "goals",
                column: "completion_status");

            migrationBuilder.CreateIndex(
                name: "i_x_goals_employee_id",
                schema: "employee",
                table: "goals",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_goals_employee_id_target_date",
                schema: "employee",
                table: "goals",
                columns: new[] { "employee_id", "target_date" });

            migrationBuilder.CreateIndex(
                name: "i_x_goals_performance_review_id",
                schema: "employee",
                table: "goals",
                column: "performance_review_id");

            migrationBuilder.CreateIndex(
                name: "i_x_goals_target_date",
                schema: "employee",
                table: "goals",
                column: "target_date");

            migrationBuilder.CreateIndex(
                name: "i_x_leave_approvals_approver_id",
                schema: "employee",
                table: "leave_approvals",
                column: "approver_id");

            migrationBuilder.CreateIndex(
                name: "i_x_leave_approvals_leave_request_id",
                schema: "employee",
                table: "leave_approvals",
                column: "leave_request_id");

            migrationBuilder.CreateIndex(
                name: "i_x_leave_approvals_leave_request_id_approval_level",
                schema: "employee",
                table: "leave_approvals",
                columns: new[] { "leave_request_id", "approval_level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_leave_balances_employee_id",
                schema: "employee",
                table: "leave_balances",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_leave_balances_employee_id_leave_type_year",
                schema: "employee",
                table: "leave_balances",
                columns: new[] { "employee_id", "leave_type", "year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_leave_policies_leave_type",
                schema: "employee",
                table: "leave_policies",
                column: "leave_type");

            migrationBuilder.CreateIndex(
                name: "i_x_leave_policies_leave_type_is_active_effective_date",
                schema: "employee",
                table: "leave_policies",
                columns: new[] { "leave_type", "is_active", "effective_date" });

            migrationBuilder.CreateIndex(
                name: "i_x_leave_requests_approver_id",
                schema: "employee",
                table: "leave_requests",
                column: "approver_id");

            migrationBuilder.CreateIndex(
                name: "i_x_leave_requests_approver_id_status",
                schema: "employee",
                table: "leave_requests",
                columns: new[] { "approver_id", "status" });

            migrationBuilder.CreateIndex(
                name: "i_x_leave_requests_employee_id",
                schema: "employee",
                table: "leave_requests",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_leave_requests_start_date_end_date",
                schema: "employee",
                table: "leave_requests",
                columns: new[] { "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "i_x_leave_requests_status",
                schema: "employee",
                table: "leave_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_mandatory_training_requirements_employment_type",
                schema: "employee",
                table: "mandatory_training_requirements",
                column: "employment_type");

            migrationBuilder.CreateIndex(
                name: "i_x_mandatory_training_requirements_employment_type_job_role_is~",
                schema: "employee",
                table: "mandatory_training_requirements",
                columns: new[] { "employment_type", "job_role", "is_active" });

            migrationBuilder.CreateIndex(
                name: "i_x_mandatory_training_requirements_is_active",
                schema: "employee",
                table: "mandatory_training_requirements",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "i_x_mandatory_training_requirements_job_role",
                schema: "employee",
                table: "mandatory_training_requirements",
                column: "job_role");

            migrationBuilder.CreateIndex(
                name: "i_x_offboarding_checklists_blocks_final_paycheck",
                schema: "employee",
                table: "offboarding_checklists",
                column: "blocks_final_paycheck");

            migrationBuilder.CreateIndex(
                name: "i_x_offboarding_checklists_completed_by",
                schema: "employee",
                table: "offboarding_checklists",
                column: "completed_by");

            migrationBuilder.CreateIndex(
                name: "i_x_offboarding_checklists_completion_status",
                schema: "employee",
                table: "offboarding_checklists",
                column: "completion_status");

            migrationBuilder.CreateIndex(
                name: "i_x_offboarding_checklists_due_date",
                schema: "employee",
                table: "offboarding_checklists",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "i_x_offboarding_checklists_employee_id",
                schema: "employee",
                table: "offboarding_checklists",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_offboarding_checklists_employee_id_display_order",
                schema: "employee",
                table: "offboarding_checklists",
                columns: new[] { "employee_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "i_x_offboarding_checklists_responsible_party",
                schema: "employee",
                table: "offboarding_checklists",
                column: "responsible_party");

            migrationBuilder.CreateIndex(
                name: "i_x_onboarding_checklists_completed_by",
                schema: "employee",
                table: "onboarding_checklists",
                column: "completed_by");

            migrationBuilder.CreateIndex(
                name: "i_x_onboarding_checklists_completion_status",
                schema: "employee",
                table: "onboarding_checklists",
                column: "completion_status");

            migrationBuilder.CreateIndex(
                name: "i_x_onboarding_checklists_due_date",
                schema: "employee",
                table: "onboarding_checklists",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "i_x_onboarding_checklists_employee_id",
                schema: "employee",
                table: "onboarding_checklists",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_onboarding_checklists_employee_id_display_order",
                schema: "employee",
                table: "onboarding_checklists",
                columns: new[] { "employee_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "i_x_onboarding_checklists_responsible_party",
                schema: "employee",
                table: "onboarding_checklists",
                column: "responsible_party");

            migrationBuilder.CreateIndex(
                name: "i_x_performance_improvement_plans_employee_id",
                schema: "employee",
                table: "performance_improvement_plans",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_performance_improvement_plans_employee_id_start_date_end_da~",
                schema: "employee",
                table: "performance_improvement_plans",
                columns: new[] { "employee_id", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "i_x_performance_improvement_plans_manager_id",
                schema: "employee",
                table: "performance_improvement_plans",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "i_x_performance_improvement_plans_status",
                schema: "employee",
                table: "performance_improvement_plans",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_performance_improvement_plans_status_end_date",
                schema: "employee",
                table: "performance_improvement_plans",
                columns: new[] { "status", "end_date" });

            migrationBuilder.CreateIndex(
                name: "i_x_performance_reviews_employee_id",
                schema: "employee",
                table: "performance_reviews",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_performance_reviews_employee_id_review_period_start_review_~",
                schema: "employee",
                table: "performance_reviews",
                columns: new[] { "employee_id", "review_period_start", "review_period_end" });

            migrationBuilder.CreateIndex(
                name: "i_x_performance_reviews_review_cycle",
                schema: "employee",
                table: "performance_reviews",
                column: "review_cycle");

            migrationBuilder.CreateIndex(
                name: "i_x_performance_reviews_reviewer_id",
                schema: "employee",
                table: "performance_reviews",
                column: "reviewer_id");

            migrationBuilder.CreateIndex(
                name: "i_x_performance_reviews_status",
                schema: "employee",
                table: "performance_reviews",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_skills_employee_id",
                schema: "employee",
                table: "skills",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_skills_employee_id_skill_name",
                schema: "employee",
                table: "skills",
                columns: new[] { "employee_id", "skill_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_skills_is_development_area",
                schema: "employee",
                table: "skills",
                column: "is_development_area");

            migrationBuilder.CreateIndex(
                name: "i_x_skills_last_assessed_date",
                schema: "employee",
                table: "skills",
                column: "last_assessed_date");

            migrationBuilder.CreateIndex(
                name: "i_x_training_records_completion_date",
                schema: "employee",
                table: "training_records",
                column: "completion_date");

            migrationBuilder.CreateIndex(
                name: "i_x_training_records_employee_id",
                schema: "employee",
                table: "training_records",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_training_records_employee_id_completion_date",
                schema: "employee",
                table: "training_records",
                columns: new[] { "employee_id", "completion_date" });

            migrationBuilder.CreateIndex(
                name: "i_x_training_records_expiration_date",
                schema: "employee",
                table: "training_records",
                column: "expiration_date");

            migrationBuilder.CreateIndex(
                name: "i_x_training_records_status",
                schema: "employee",
                table: "training_records",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_training_records_training_type_status",
                schema: "employee",
                table: "training_records",
                columns: new[] { "training_type", "status" });

            migrationBuilder.CreateIndex(
                name: "i_x_work_authorizations_authorization_type",
                schema: "employee",
                table: "work_authorizations",
                column: "authorization_type");

            migrationBuilder.CreateIndex(
                name: "i_x_work_authorizations_employee_id",
                schema: "employee",
                table: "work_authorizations",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_work_authorizations_employee_id_authorization_type_is_active",
                schema: "employee",
                table: "work_authorizations",
                columns: new[] { "employee_id", "authorization_type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "i_x_work_authorizations_expiration_date",
                schema: "employee",
                table: "work_authorizations",
                column: "expiration_date");

            migrationBuilder.CreateIndex(
                name: "i_x_work_authorizations_expiration_date_is_active",
                schema: "employee",
                table: "work_authorizations",
                columns: new[] { "expiration_date", "is_active" });

            migrationBuilder.CreateIndex(
                name: "i_x_work_authorizations_is_active",
                schema: "employee",
                table: "work_authorizations",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "i_x_work_authorizations_right_to_work_document_id",
                schema: "employee",
                table: "work_authorizations",
                column: "right_to_work_document_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_departments__employees_department_head_id",
                schema: "employee",
                table: "departments",
                column: "department_head_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_departments_departments_parent_department_id",
                schema: "employee",
                table: "departments",
                column: "parent_department_id",
                principalSchema: "employee",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_teams_employees_team_lead_id",
                schema: "employee",
                table: "teams",
                column: "team_lead_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
