using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkAuthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "work_authorizations",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    authorization_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    document_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    issuing_authority = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    sponsorship_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    right_to_work_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_work_authorizations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_work_authorizations_documents_right_to_work_document_id",
                        column: x => x.right_to_work_document_id,
                        principalSchema: "employee",
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_work_authorizations_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_work_authorizations_authorization_type",
                schema: "employee",
                table: "work_authorizations",
                column: "authorization_type");

            migrationBuilder.CreateIndex(
                name: "i_x_work_authorizations_employee_id",
                schema: "employee",
                table: "work_authorizations",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_work_authorizations_employee_id_authorization_type_is_active",
                schema: "employee",
                table: "work_authorizations",
                columns: new[] { "employee_id", "authorization_type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "i_x_work_authorizations_expiration_date",
                schema: "employee",
                table: "work_authorizations",
                column: "expiration_date");

            migrationBuilder.CreateIndex(
                name: "i_x_work_authorizations_expiration_date_is_active",
                schema: "employee",
                table: "work_authorizations",
                columns: new[] { "expiration_date", "is_active" });

            migrationBuilder.CreateIndex(
                name: "i_x_work_authorizations_is_active",
                schema: "employee",
                table: "work_authorizations",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "i_x_work_authorizations_right_to_work_document_id",
                schema: "employee",
                table: "work_authorizations",
                column: "right_to_work_document_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_authorizations",
                schema: "employee");
        }
    }
}
