using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeAndEmergencyContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_attendance_records__employees_employee_id",
                schema: "employee",
                table: "attendance_records");

            migrationBuilder.DropForeignKey(
                name: "f_k_certifications__employees_employee_id",
                schema: "employee",
                table: "certifications");

            migrationBuilder.DropForeignKey(
                name: "f_k_dependents__employees_employee_id",
                schema: "employee",
                table: "dependents");

            migrationBuilder.DropForeignKey(
                name: "f_k_employees__positions_position_id",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "f_k_employees_departments_department_id",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "f_k_employees_employees_manager_id",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "f_k_employment_histories_employees_employee_id",
                schema: "employee",
                table: "employment_histories");

            migrationBuilder.DropForeignKey(
                name: "f_k_goals_employees_employee_id",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropForeignKey(
                name: "f_k_leave_balances_employees_employee_id",
                schema: "employee",
                table: "leave_balances");

            migrationBuilder.DropForeignKey(
                name: "f_k_leave_requests_employees_employee_id",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropForeignKey(
                name: "f_k_performance_reviews_employees_employee_id",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropForeignKey(
                name: "f_k_personal_documents_employees_employee_id",
                schema: "employee",
                table: "personal_documents");

            migrationBuilder.DropForeignKey(
                name: "f_k_salary_histories_employees_employee_id",
                schema: "employee",
                table: "salary_histories");

            migrationBuilder.DropForeignKey(
                name: "f_k_trainings_employees_employee_id",
                schema: "employee",
                table: "trainings");

            migrationBuilder.DropIndex(
                name: "i_x_trainings_employee_id",
                schema: "employee",
                table: "trainings");

            migrationBuilder.DropIndex(
                name: "i_x_salary_histories_employee_id",
                schema: "employee",
                table: "salary_histories");

            migrationBuilder.DropIndex(
                name: "i_x_personal_documents_employee_id",
                schema: "employee",
                table: "personal_documents");

            migrationBuilder.DropIndex(
                name: "i_x_performance_reviews_employee_id",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropIndex(
                name: "i_x_leave_requests_employee_id",
                schema: "employee",
                table: "leave_requests");

            migrationBuilder.DropIndex(
                name: "i_x_leave_balances_employee_id",
                schema: "employee",
                table: "leave_balances");

            migrationBuilder.DropIndex(
                name: "i_x_goals_employee_id",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropIndex(
                name: "i_x_employment_histories_employee_id",
                schema: "employee",
                table: "employment_histories");

            migrationBuilder.DropIndex(
                name: "i_x_employees_position_id",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "i_x_dependents_employee_id",
                schema: "employee",
                table: "dependents");

            migrationBuilder.DropIndex(
                name: "i_x_certifications_employee_id",
                schema: "employee",
                table: "certifications");

            migrationBuilder.DropIndex(
                name: "i_x_attendance_records_employee_id",
                schema: "employee",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "address_line1",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "address_line2",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "bank_account_number",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "city",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "country",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "email",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "phone_number",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "position_id",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "postal_code",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "state",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "tax_id",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "name",
                schema: "employee",
                table: "emergency_contacts");

            migrationBuilder.RenameColumn(
                name: "last_name",
                schema: "employee",
                table: "employees",
                newName: "LegalName_LastName");

            migrationBuilder.RenameColumn(
                name: "first_name",
                schema: "employee",
                table: "employees",
                newName: "LegalName_FirstName");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                schema: "employee",
                table: "employees",
                newName: "modified_by");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "employee",
                table: "employees",
                newName: "probation_end_date");

            migrationBuilder.RenameColumn(
                name: "hire_date",
                schema: "employee",
                table: "employees",
                newName: "start_date");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "employee",
                table: "employees",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "employee",
                table: "emergency_contacts",
                newName: "created_date");

            migrationBuilder.AlterColumn<string>(
                name: "employment_type",
                schema: "employee",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "employee_number",
                schema: "employee",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LegalName_LastName",
                schema: "employee",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LegalName_FirstName",
                schema: "employee",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ContactInformation_MobilePhone",
                schema: "employee",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactInformation_PersonalEmail",
                schema: "employee",
                table: "employees",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactInformation_WorkEmail",
                schema: "employee",
                table: "employees",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LegalName_MiddleName",
                schema: "employee",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "employment_status",
                schema: "employee",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "job_title",
                schema: "employee",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_date",
                schema: "employee",
                table: "employees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nationality",
                schema: "employee",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preferred_name",
                schema: "employee",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "work_location",
                schema: "employee",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "relationship",
                schema: "employee",
                table: "emergency_contacts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "phone_number",
                schema: "employee",
                table: "emergency_contacts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "contact_name",
                schema: "employee",
                table: "emergency_contacts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "employee",
                table: "emergency_contacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                schema: "employee",
                table: "emergency_contacts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "modified_by",
                schema: "employee",
                table: "emergency_contacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_date",
                schema: "employee",
                table: "emergency_contacts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "priority_order",
                schema: "employee",
                table: "emergency_contacts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "i_x_employees_employee_number",
                schema: "employee",
                table: "employees",
                column: "employee_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_employees_employment_status",
                schema: "employee",
                table: "employees",
                column: "employment_status");

            migrationBuilder.CreateIndex(
                name: "i_x_emergency_contacts_employee_id_priority_order",
                schema: "employee",
                table: "emergency_contacts",
                columns: new[] { "employee_id", "priority_order" });

            migrationBuilder.AddForeignKey(
                name: "f_k_employees_departments_department_id",
                schema: "employee",
                table: "employees",
                column: "department_id",
                principalSchema: "employee",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_employees_employees_manager_id",
                schema: "employee",
                table: "employees",
                column: "manager_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_employees_departments_department_id",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "f_k_employees_employees_manager_id",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "i_x_employees_employee_number",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "i_x_employees_employment_status",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "i_x_emergency_contacts_employee_id_priority_order",
                schema: "employee",
                table: "emergency_contacts");

            migrationBuilder.DropColumn(
                name: "ContactInformation_MobilePhone",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "ContactInformation_PersonalEmail",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "ContactInformation_WorkEmail",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "LegalName_MiddleName",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "employment_status",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "job_title",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "modified_date",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "nationality",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "preferred_name",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "work_location",
                schema: "employee",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "contact_name",
                schema: "employee",
                table: "emergency_contacts");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "employee",
                table: "emergency_contacts");

            migrationBuilder.DropColumn(
                name: "email",
                schema: "employee",
                table: "emergency_contacts");

            migrationBuilder.DropColumn(
                name: "modified_by",
                schema: "employee",
                table: "emergency_contacts");

            migrationBuilder.DropColumn(
                name: "modified_date",
                schema: "employee",
                table: "emergency_contacts");

            migrationBuilder.DropColumn(
                name: "priority_order",
                schema: "employee",
                table: "emergency_contacts");

            migrationBuilder.RenameColumn(
                name: "LegalName_LastName",
                schema: "employee",
                table: "employees",
                newName: "last_name");

            migrationBuilder.RenameColumn(
                name: "LegalName_FirstName",
                schema: "employee",
                table: "employees",
                newName: "first_name");

            migrationBuilder.RenameColumn(
                name: "start_date",
                schema: "employee",
                table: "employees",
                newName: "hire_date");

            migrationBuilder.RenameColumn(
                name: "probation_end_date",
                schema: "employee",
                table: "employees",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "modified_by",
                schema: "employee",
                table: "employees",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_date",
                schema: "employee",
                table: "employees",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "created_date",
                schema: "employee",
                table: "emergency_contacts",
                newName: "created_at");

            migrationBuilder.AlterColumn<string>(
                name: "employment_type",
                schema: "employee",
                table: "employees",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "employee_number",
                schema: "employee",
                table: "employees",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                schema: "employee",
                table: "employees",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                schema: "employee",
                table: "employees",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "address_line1",
                schema: "employee",
                table: "employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_line2",
                schema: "employee",
                table: "employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_account_number",
                schema: "employee",
                table: "employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                schema: "employee",
                table: "employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country",
                schema: "employee",
                table: "employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                schema: "employee",
                table: "employees",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "phone_number",
                schema: "employee",
                table: "employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "position_id",
                schema: "employee",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                schema: "employee",
                table: "employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "state",
                schema: "employee",
                table: "employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "employee",
                table: "employees",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tax_id",
                schema: "employee",
                table: "employees",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "relationship",
                schema: "employee",
                table: "emergency_contacts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "phone_number",
                schema: "employee",
                table: "emergency_contacts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "name",
                schema: "employee",
                table: "emergency_contacts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "i_x_trainings_employee_id",
                schema: "employee",
                table: "trainings",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_salary_histories_employee_id",
                schema: "employee",
                table: "salary_histories",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_personal_documents_employee_id",
                schema: "employee",
                table: "personal_documents",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_performance_reviews_employee_id",
                schema: "employee",
                table: "performance_reviews",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_leave_requests_employee_id",
                schema: "employee",
                table: "leave_requests",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_leave_balances_employee_id",
                schema: "employee",
                table: "leave_balances",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_goals_employee_id",
                schema: "employee",
                table: "goals",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_employment_histories_employee_id",
                schema: "employee",
                table: "employment_histories",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_employees_position_id",
                schema: "employee",
                table: "employees",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "i_x_dependents_employee_id",
                schema: "employee",
                table: "dependents",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_certifications_employee_id",
                schema: "employee",
                table: "certifications",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_attendance_records_employee_id",
                schema: "employee",
                table: "attendance_records",
                column: "employee_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_attendance_records__employees_employee_id",
                schema: "employee",
                table: "attendance_records",
                column: "employee_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_certifications__employees_employee_id",
                schema: "employee",
                table: "certifications",
                column: "employee_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_dependents__employees_employee_id",
                schema: "employee",
                table: "dependents",
                column: "employee_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_employees__positions_position_id",
                schema: "employee",
                table: "employees",
                column: "position_id",
                principalSchema: "employee",
                principalTable: "positions",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_employees_departments_department_id",
                schema: "employee",
                table: "employees",
                column: "department_id",
                principalSchema: "employee",
                principalTable: "departments",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_employees_employees_manager_id",
                schema: "employee",
                table: "employees",
                column: "manager_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id");

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
                name: "f_k_goals_employees_employee_id",
                schema: "employee",
                table: "goals",
                column: "employee_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

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
                name: "f_k_leave_requests_employees_employee_id",
                schema: "employee",
                table: "leave_requests",
                column: "employee_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_performance_reviews_employees_employee_id",
                schema: "employee",
                table: "performance_reviews",
                column: "employee_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_personal_documents_employees_employee_id",
                schema: "employee",
                table: "personal_documents",
                column: "employee_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_salary_histories_employees_employee_id",
                schema: "employee",
                table: "salary_histories",
                column: "employee_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_trainings_employees_employee_id",
                schema: "employee",
                table: "trainings",
                column: "employee_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
