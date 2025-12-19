using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompensation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "benefits_enrollments",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    health_insurance_plan = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    retirement_contribution = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    beneficiary_information = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    enrollment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "compensation_records",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    salary_amount = table.Column<string>(type: "text", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "THB"),
                    effective_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    change_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    bonus_structure = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    commission_structure = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "benefits_enrollments",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "compensation_records",
                schema: "employee");
        }
    }
}
