using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTraining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mandatory_training_requirements",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employment_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    job_role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    required_courses = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    deadline_days_from_start = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_mandatory_training_requirements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    proficiency_level = table.Column<int>(type: "integer", nullable: false),
                    last_assessed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_development_area = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
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
                    course_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    completion_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    certificate_document_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    training_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mandatory_training_requirements",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "skills",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "training_records",
                schema: "employee");
        }
    }
}
