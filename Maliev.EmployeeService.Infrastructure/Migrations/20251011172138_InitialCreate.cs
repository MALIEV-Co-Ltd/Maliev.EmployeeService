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
                name: "audit_logs",
                schema: "employee",
                columns: table => new
                {
                    log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                name: "benefits",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_benefits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_departments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "disciplinary_actions",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "text", nullable: false),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_disciplinary_actions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee_benefits",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    benefit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    conducted_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_exit_interviews", x => x.id);
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
                name: "leave_approvals",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "text", nullable: false),
                    decision_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_leave_approvals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "offboarding_tasks",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_offboarding_tasks", x => x.id);
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
                name: "employees",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_number = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    hire_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    termination_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    position_id = table.Column<Guid>(type: "uuid", nullable: true),
                    manager_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employment_type = table.Column<string>(type: "text", nullable: false),
                    national_id = table.Column<string>(type: "text", nullable: true),
                    tax_id = table.Column<string>(type: "text", nullable: true),
                    bank_account_number = table.Column<string>(type: "text", nullable: true),
                    address_line1 = table.Column<string>(type: "text", nullable: true),
                    address_line2 = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    state = table.Column<string>(type: "text", nullable: true),
                    postal_code = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    job_application_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_employees", x => x.id);
                    table.ForeignKey(
                        name: "f_k_employees__positions_position_id",
                        column: x => x.position_id,
                        principalSchema: "employee",
                        principalTable: "positions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "f_k_employees_departments_department_id",
                        column: x => x.department_id,
                        principalSchema: "employee",
                        principalTable: "departments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "f_k_employees_employees_manager_id",
                        column: x => x.manager_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id");
                });

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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_attendance_records", x => x.id);
                    table.ForeignKey(
                        name: "f_k_attendance_records__employees_employee_id",
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
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    issued_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_certifications", x => x.id);
                    table.ForeignKey(
                        name: "f_k_certifications__employees_employee_id",
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
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    relationship = table.Column<string>(type: "text", nullable: false),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_dependents", x => x.id);
                    table.ForeignKey(
                        name: "f_k_dependents__employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "emergency_contacts",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    relationship = table.Column<string>(type: "text", nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    change_type = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "goals",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_goals", x => x.id);
                    table.ForeignKey(
                        name: "f_k_goals_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leave_balances",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type = table.Column<string>(type: "text", nullable: false),
                    total_days = table.Column<decimal>(type: "numeric", nullable: false),
                    used_days = table.Column<decimal>(type: "numeric", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "leave_requests",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_leave_requests", x => x.id);
                    table.ForeignKey(
                        name: "f_k_leave_requests_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "performance_reviews",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "personal_documents",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "text", nullable: false),
                    document_number = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_personal_documents", x => x.id);
                    table.ForeignKey(
                        name: "f_k_personal_documents_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "salary_histories",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    effective_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_salary_histories", x => x.id);
                    table.ForeignKey(
                        name: "f_k_salary_histories_employees_employee_id",
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
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    completion_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_trainings", x => x.id);
                    table.ForeignKey(
                        name: "f_k_trainings_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_users", x => x.id);
                    table.ForeignKey(
                        name: "f_k_users_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_attendance_records_employee_id",
                schema: "employee",
                table: "attendance_records",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_audit_logs_entity_type_entity_id",
                schema: "employee",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "i_x_audit_logs_timestamp",
                schema: "employee",
                table: "audit_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "i_x_audit_logs_user_id",
                schema: "employee",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_certifications_employee_id",
                schema: "employee",
                table: "certifications",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_dependents_employee_id",
                schema: "employee",
                table: "dependents",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_emergency_contacts_employee_id",
                schema: "employee",
                table: "emergency_contacts",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_employees_department_id",
                schema: "employee",
                table: "employees",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "i_x_employees_manager_id",
                schema: "employee",
                table: "employees",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "i_x_employees_position_id",
                schema: "employee",
                table: "employees",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "i_x_employment_histories_employee_id",
                schema: "employee",
                table: "employment_histories",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_goals_employee_id",
                schema: "employee",
                table: "goals",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_leave_balances_employee_id",
                schema: "employee",
                table: "leave_balances",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_leave_requests_employee_id",
                schema: "employee",
                table: "leave_requests",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_performance_reviews_employee_id",
                schema: "employee",
                table: "performance_reviews",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_personal_documents_employee_id",
                schema: "employee",
                table: "personal_documents",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_salary_histories_employee_id",
                schema: "employee",
                table: "salary_histories",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_trainings_employee_id",
                schema: "employee",
                table: "trainings",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_users_employee_id",
                schema: "employee",
                table: "users",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_users_username",
                schema: "employee",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_records",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "benefits",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "certifications",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "dependents",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "disciplinary_actions",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "emergency_contacts",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "employee_benefits",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "employment_histories",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "exit_interviews",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "goals",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "incidents",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "leave_approvals",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "leave_balances",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "leave_requests",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "offboarding_tasks",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "performance_reviews",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "personal_documents",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "salary_histories",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "trainings",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "users",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "work_schedules",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "employees",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "positions",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "departments",
                schema: "employee");
        }
    }
}
