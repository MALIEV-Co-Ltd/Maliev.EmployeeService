using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "documents",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    upload_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    expiration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    access_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_documents", x => x.id);
                    table.ForeignKey(
                        name: "f_k_documents__employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_documents__employees_uploaded_by",
                        column: x => x.uploaded_by,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_versions",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    storage_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    upload_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    change_description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_document_versions", x => x.id);
                    table.ForeignKey(
                        name: "f_k_document_versions__employees_uploaded_by",
                        column: x => x.uploaded_by,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_document_versions_documents_document_id",
                        column: x => x.document_id,
                        principalSchema: "employee",
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_documents_access_level",
                schema: "employee",
                table: "documents",
                column: "access_level");

            migrationBuilder.CreateIndex(
                name: "i_x_documents_document_type",
                schema: "employee",
                table: "documents",
                column: "document_type");

            migrationBuilder.CreateIndex(
                name: "i_x_documents_document_type_access_level",
                schema: "employee",
                table: "documents",
                columns: new[] { "document_type", "access_level" });

            migrationBuilder.CreateIndex(
                name: "i_x_documents_employee_id",
                schema: "employee",
                table: "documents",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_documents_employee_id_document_type",
                schema: "employee",
                table: "documents",
                columns: new[] { "employee_id", "document_type" });

            migrationBuilder.CreateIndex(
                name: "i_x_documents_employee_id_is_archived",
                schema: "employee",
                table: "documents",
                columns: new[] { "employee_id", "is_archived" });

            migrationBuilder.CreateIndex(
                name: "i_x_documents_expiration_date",
                schema: "employee",
                table: "documents",
                column: "expiration_date");

            migrationBuilder.CreateIndex(
                name: "i_x_documents_upload_date",
                schema: "employee",
                table: "documents",
                column: "upload_date");

            migrationBuilder.CreateIndex(
                name: "i_x_documents_uploaded_by",
                schema: "employee",
                table: "documents",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "i_x_document_versions_document_id",
                schema: "employee",
                table: "document_versions",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "i_x_document_versions_document_id_version_number",
                schema: "employee",
                table: "document_versions",
                columns: new[] { "document_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_document_versions_upload_date",
                schema: "employee",
                table: "document_versions",
                column: "upload_date");

            migrationBuilder.CreateIndex(
                name: "i_x_document_versions_uploaded_by",
                schema: "employee",
                table: "document_versions",
                column: "uploaded_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_versions",
                schema: "employee");

            migrationBuilder.DropTable(
                name: "documents",
                schema: "employee");
        }
    }
}
