using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "i_x_leave_requests_approver_id_status",
                schema: "employee",
                table: "leave_requests",
                columns: new[] { "approver_id", "status" });

            migrationBuilder.CreateIndex(
                name: "i_x_employees_department_id_employment_status",
                schema: "employee",
                table: "employees",
                columns: new[] { "department_id", "employment_status" });

            migrationBuilder.CreateIndex(
                name: "i_x_employees_employment_type",
                schema: "employee",
                table: "employees",
                column: "employment_type");

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
                name: "i_x_departments_parent_department_id_is_active",
                schema: "employee",
                table: "departments",
                columns: new[] { "parent_department_id", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "i_x_leave_requests_approver_id_status",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropIndex(
                name: "i_x_employees_department_id_employment_status",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "i_x_employees_employment_type",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "i_x_employees_start_date",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "i_x_employees_termination_date",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "i_x_departments_parent_department_id_is_active",
                schema: "employee",
                table: "departments");
        }
    }
}
