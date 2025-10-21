using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingOffboardingChecklists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "offboarding_checklists",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    responsible_party = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completion_status = table.Column<bool>(type: "boolean", nullable: false),
                    completed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    blocks_final_paycheck = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "onboarding_checklists",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    responsible_party = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completion_status = table.Column<bool>(type: "boolean", nullable: false),
                    completed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "offboarding_checklists",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "onboarding_checklists",
                schema: "employee");
        }
    }
}
