using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveManagementEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "total_days",
                schema: "employee",
                table: "leave_balances");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "employee",
                table: "leave_requests",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "employee",
                table: "leave_balances",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "employee",
                table: "leave_approvals",
                newName: "created_date");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "employee",
                table: "leave_requests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "leave_type",
                schema: "employee",
                table: "leave_requests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

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

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "employee",
                table: "leave_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "current_approver_id",
                schema: "employee",
                table: "leave_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "modified_by",
                schema: "employee",
                table: "leave_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_date",
                schema: "employee",
                table: "leave_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reason",
                schema: "employee",
                table: "leave_requests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "rejected_date",
                schema: "employee",
                table: "leave_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                schema: "employee",
                table: "leave_requests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "total_days",
                schema: "employee",
                table: "leave_requests",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "used_days",
                schema: "employee",
                table: "leave_balances",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "leave_type",
                schema: "employee",
                table: "leave_balances",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<decimal>(
                name: "carry_forward_days",
                schema: "employee",
                table: "leave_balances",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "employee",
                table: "leave_balances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "expiry_date",
                schema: "employee",
                table: "leave_balances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "modified_by",
                schema: "employee",
                table: "leave_balances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_date",
                schema: "employee",
                table: "leave_balances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "pending_days",
                schema: "employee",
                table: "leave_balances",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_entitlement",
                schema: "employee",
                table: "leave_balances",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<DateTime>(
                name: "decision_date",
                schema: "employee",
                table: "leave_approvals",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "decision",
                schema: "employee",
                table: "leave_approvals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "approval_level",
                schema: "employee",
                table: "leave_approvals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "comments",
                schema: "employee",
                table: "leave_approvals",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "employee",
                table: "leave_approvals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "modified_by",
                schema: "employee",
                table: "leave_approvals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_date",
                schema: "employee",
                table: "leave_approvals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "i_x_leave_requests_current_approver_id",
                schema: "employee",
                table: "leave_requests",
                column: "current_approver_id");

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

            migrationBuilder.AddForeignKey(
                name: "f_k_leave_approvals__leave_requests_leave_request_id",
                schema: "employee",
                table: "leave_approvals",
                column: "leave_request_id",
                principalSchema: "employee",
                principalTable: "leave_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_leave_approvals_employees_approver_id",
                schema: "employee",
                table: "leave_approvals",
                column: "approver_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_leave_balances_employees_employee_id",
                schema: "employee",
                table: "leave_balances",
                column: "employee_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_leave_requests_employees_current_approver_id",
                schema: "employee",
                table: "leave_requests",
                column: "current_approver_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_leave_requests_employees_employee_id",
                schema: "employee",
                table: "leave_requests",
                column: "employee_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_leave_approvals__leave_requests_leave_request_id",
                schema: "employee",
                table: "leave_approvals");

            migrationBuilder.DropForeignKey(
                name: "f_k_leave_approvals_employees_approver_id",
                schema: "employee",
                table: "leave_approvals");

            migrationBuilder.DropForeignKey(
                name: "f_k_leave_balances_employees_employee_id",
                schema: "employee",
                table: "leave_balances");

            migrationBuilder.DropForeignKey(
                name: "f_k_leave_requests_employees_current_approver_id",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropForeignKey(
                name: "f_k_leave_requests_employees_employee_id",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropIndex(
                name: "i_x_leave_requests_current_approver_id",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropIndex(
                name: "i_x_leave_requests_employee_id",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropIndex(
                name: "i_x_leave_requests_start_date_end_date",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropIndex(
                name: "i_x_leave_requests_status",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropIndex(
                name: "i_x_leave_balances_employee_id",
                schema: "employee",
                table: "leave_balances");

            migrationBuilder.DropIndex(
                name: "i_x_leave_balances_employee_id_leave_type_year",
                schema: "employee",
                table: "leave_balances");

            migrationBuilder.DropIndex(
                name: "i_x_leave_approvals_approver_id",
                schema: "employee",
                table: "leave_approvals");

            migrationBuilder.DropIndex(
                name: "i_x_leave_approvals_leave_request_id",
                schema: "employee",
                table: "leave_approvals");

            migrationBuilder.DropIndex(
                name: "i_x_leave_approvals_leave_request_id_approval_level",
                schema: "employee",
                table: "leave_approvals");

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

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "current_approver_id",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "modified_by",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "modified_date",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "reason",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "rejected_date",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "total_days",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "carry_forward_days",
                schema: "employee",
                table: "leave_balances");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "employee",
                table: "leave_balances");

            migrationBuilder.DropColumn(
                name: "expiry_date",
                schema: "employee",
                table: "leave_balances");

            migrationBuilder.DropColumn(
                name: "modified_by",
                schema: "employee",
                table: "leave_balances");

            migrationBuilder.DropColumn(
                name: "modified_date",
                schema: "employee",
                table: "leave_balances");

            migrationBuilder.DropColumn(
                name: "pending_days",
                schema: "employee",
                table: "leave_balances");

            migrationBuilder.DropColumn(
                name: "total_entitlement",
                schema: "employee",
                table: "leave_balances");

            migrationBuilder.DropColumn(
                name: "approval_level",
                schema: "employee",
                table: "leave_approvals");

            migrationBuilder.DropColumn(
                name: "comments",
                schema: "employee",
                table: "leave_approvals");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "employee",
                table: "leave_approvals");

            migrationBuilder.DropColumn(
                name: "modified_by",
                schema: "employee",
                table: "leave_approvals");

            migrationBuilder.DropColumn(
                name: "modified_date",
                schema: "employee",
                table: "leave_approvals");

            migrationBuilder.RenameColumn(
                name: "created_date",
                schema: "employee",
                table: "leave_requests",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "created_date",
                schema: "employee",
                table: "leave_balances",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "created_date",
                schema: "employee",
                table: "leave_approvals",
                newName: "created_at");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "employee",
                table: "leave_requests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "leave_type",
                schema: "employee",
                table: "leave_requests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "used_days",
                schema: "employee",
                table: "leave_balances",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "leave_type",
                schema: "employee",
                table: "leave_balances",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<decimal>(
                name: "total_days",
                schema: "employee",
                table: "leave_balances",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<DateTime>(
                name: "decision_date",
                schema: "employee",
                table: "leave_approvals",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "decision",
                schema: "employee",
                table: "leave_approvals",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }
    }
}
