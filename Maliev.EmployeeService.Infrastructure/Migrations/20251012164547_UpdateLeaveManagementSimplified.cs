using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLeaveManagementSimplified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_leave_requests_employees_current_approver_id",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "approval_level",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "approved_date",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "cancelled_date",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.RenameColumn(
                name: "rejection_reason",
                schema: "employee",
                table: "leave_requests",
                newName: "approval_comments");

            migrationBuilder.RenameColumn(
                name: "rejected_date",
                schema: "employee",
                table: "leave_requests",
                newName: "approval_date");

            migrationBuilder.RenameColumn(
                name: "current_approver_id",
                schema: "employee",
                table: "leave_requests",
                newName: "approver_id");

            migrationBuilder.RenameIndex(
                name: "i_x_leave_requests_current_approver_id",
                schema: "employee",
                table: "leave_requests",
                newName: "i_x_leave_requests_approver_id");

            migrationBuilder.CreateTable(
                name: "leave_policies",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    accrual_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    max_carryover = table.Column<int>(type: "integer", nullable: true),
                    minimum_notice_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    blackout_periods_json = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    effective_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_leave_policies", x => x.id);
                });

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

            migrationBuilder.AddForeignKey(
                name: "f_k_leave_requests_employees_approver_id",
                schema: "employee",
                table: "leave_requests",
                column: "approver_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_leave_requests_employees_approver_id",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropTable(
                name: "leave_policies",
                schema: "employee");

            migrationBuilder.RenameColumn(
                name: "approver_id",
                schema: "employee",
                table: "leave_requests",
                newName: "current_approver_id");

            migrationBuilder.RenameColumn(
                name: "approval_date",
                schema: "employee",
                table: "leave_requests",
                newName: "rejected_date");

            migrationBuilder.RenameColumn(
                name: "approval_comments",
                schema: "employee",
                table: "leave_requests",
                newName: "rejection_reason");

            migrationBuilder.RenameIndex(
                name: "i_x_leave_requests_approver_id",
                schema: "employee",
                table: "leave_requests",
                newName: "i_x_leave_requests_current_approver_id");

            migrationBuilder.AddColumn<int>(
                name: "approval_level",
                schema: "employee",
                table: "leave_requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "approved_date",
                schema: "employee",
                table: "leave_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                schema: "employee",
                table: "leave_requests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_date",
                schema: "employee",
                table: "leave_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "f_k_leave_requests_employees_current_approver_id",
                schema: "employee",
                table: "leave_requests",
                column: "current_approver_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
