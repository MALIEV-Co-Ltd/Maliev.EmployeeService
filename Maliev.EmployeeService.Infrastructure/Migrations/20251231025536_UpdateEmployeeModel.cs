using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmployeeModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_employees_departments_department_id1",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "f_k_teams_employees_team_lead_id",
                schema: "employee",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "i_x_employees_department_id1",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "department_id1",
                schema: "employee",
                table: "employees");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "employee",
                table: "employment_histories",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "change_type",
                schema: "employee",
                table: "employment_histories",
                newName: "event_type");

            migrationBuilder.AlterColumn<Guid>(
                name: "team_lead_id",
                schema: "employee",
                table: "teams",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "employee",
                table: "employment_histories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "employee",
                table: "employment_histories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "modified_by",
                schema: "employee",
                table: "employment_histories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_date",
                schema: "employee",
                table: "employment_histories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "i_x_employment_histories_employee_id",
                schema: "employee",
                table: "employment_histories",
                column: "employee_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_employment_histories_employees_employee_id",
                schema: "employee",
                table: "employment_histories",
                column: "employee_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_teams_employees_team_lead_id",
                schema: "employee",
                table: "teams",
                column: "team_lead_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_employment_histories_employees_employee_id",
                schema: "employee",
                table: "employment_histories");

            migrationBuilder.DropForeignKey(
                name: "f_k_teams_employees_team_lead_id",
                schema: "employee",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "i_x_employment_histories_employee_id",
                schema: "employee",
                table: "employment_histories");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "employee",
                table: "employment_histories");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "employee",
                table: "employment_histories");

            migrationBuilder.DropColumn(
                name: "modified_by",
                schema: "employee",
                table: "employment_histories");

            migrationBuilder.DropColumn(
                name: "modified_date",
                schema: "employee",
                table: "employment_histories");

            migrationBuilder.RenameColumn(
                name: "event_type",
                schema: "employee",
                table: "employment_histories",
                newName: "change_type");

            migrationBuilder.RenameColumn(
                name: "created_date",
                schema: "employee",
                table: "employment_histories",
                newName: "created_at");

            migrationBuilder.AlterColumn<Guid>(
                name: "team_lead_id",
                schema: "employee",
                table: "teams",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "department_id1",
                schema: "employee",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "i_x_employees_department_id1",
                schema: "employee",
                table: "employees",
                column: "department_id1");

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
    }
}
