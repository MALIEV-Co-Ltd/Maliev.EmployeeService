using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "employee",
                table: "departments",
                newName: "modified_date");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "employee",
                table: "departments",
                newName: "created_date");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "employee",
                table: "departments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "employee",
                table: "departments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cost_center",
                schema: "employee",
                table: "departments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "employee",
                table: "departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "department_head_id",
                schema: "employee",
                table: "departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "headcount_limit",
                schema: "employee",
                table: "departments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "employee",
                table: "departments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "modified_by",
                schema: "employee",
                table: "departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_department_id",
                schema: "employee",
                table: "departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "i_x_departments_department_head_id",
                schema: "employee",
                table: "departments",
                column: "department_head_id");

            migrationBuilder.CreateIndex(
                name: "i_x_departments_is_active",
                schema: "employee",
                table: "departments",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "i_x_departments_name",
                schema: "employee",
                table: "departments",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "i_x_departments_parent_department_id",
                schema: "employee",
                table: "departments",
                column: "parent_department_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_departments__employees_department_head_id",
                schema: "employee",
                table: "departments",
                column: "department_head_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_departments_departments_parent_department_id",
                schema: "employee",
                table: "departments",
                column: "parent_department_id",
                principalSchema: "employee",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_departments__employees_department_head_id",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropForeignKey(
                name: "f_k_departments_departments_parent_department_id",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "i_x_departments_department_head_id",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "i_x_departments_is_active",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "i_x_departments_name",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "i_x_departments_parent_department_id",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "cost_center",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "department_head_id",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "headcount_limit",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "modified_by",
                schema: "employee",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "parent_department_id",
                schema: "employee",
                table: "departments");

            migrationBuilder.RenameColumn(
                name: "modified_date",
                schema: "employee",
                table: "departments",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "created_date",
                schema: "employee",
                table: "departments",
                newName: "created_at");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "employee",
                table: "departments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "employee",
                table: "departments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
