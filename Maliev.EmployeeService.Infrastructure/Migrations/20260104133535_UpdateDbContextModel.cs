using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDbContextModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_employee_termination_saga_states", x => x.correlation_id);
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_termination_saga_states",
                schema: "employee");
        }
    }
}
