using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamsAndMatrixReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "dotted_line_manager_id",
                schema: "employee",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "teams",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    team_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    team_lead_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                        onDelete: ReferentialAction.Restrict);
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
                name: "i_x_employees_dotted_line_manager_id",
                schema: "employee",
                table: "employees",
                column: "dotted_line_manager_id");

            migrationBuilder.CreateIndex(
                name: "i_x_employee_team_assignments_employee_id",
                schema: "employee",
                table: "employee_team_assignments",
                column: "employee_id");

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
                name: "i_x_employee_team_assignments_team_id",
                schema: "employee",
                table: "employee_team_assignments",
                column: "team_id");

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
                name: "i_x_teams_team_lead_id",
                schema: "employee",
                table: "teams",
                column: "team_lead_id");

            migrationBuilder.CreateIndex(
                name: "i_x_teams_team_type_is_active",
                schema: "employee",
                table: "teams",
                columns: new[] { "team_type", "is_active" });

            migrationBuilder.AddForeignKey(
                name: "f_k_employees_employees_dotted_line_manager_id",
                schema: "employee",
                table: "employees",
                column: "dotted_line_manager_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_employees_employees_dotted_line_manager_id",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropTable(
                name: "employee_team_assignments",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "teams",
                schema: "employee");

            migrationBuilder.DropIndex(
                name: "i_x_employees_dotted_line_manager_id",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "dotted_line_manager_id",
                schema: "employee",
                table: "employees");
        }
    }
}
