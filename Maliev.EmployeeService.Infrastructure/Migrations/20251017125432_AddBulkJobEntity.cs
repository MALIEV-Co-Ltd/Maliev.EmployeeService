using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBulkJobEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bulk_jobs",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    total_records = table.Column<int>(type: "integer", nullable: false),
                    successful_records = table.Column<int>(type: "integer", nullable: false),
                    failed_records = table.Column<int>(type: "integer", nullable: false),
                    errors = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    initiated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    result_data = table.Column<string>(type: "text", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_bulk_jobs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_bulk_jobs_completed_at",
                schema: "employee",
                table: "bulk_jobs",
                column: "completed_at");

            migrationBuilder.CreateIndex(
                name: "i_x_bulk_jobs_initiated_by_user_id",
                schema: "employee",
                table: "bulk_jobs",
                column: "initiated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_bulk_jobs_job_id",
                schema: "employee",
                table: "bulk_jobs",
                column: "job_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_bulk_jobs_job_type",
                schema: "employee",
                table: "bulk_jobs",
                column: "job_type");

            migrationBuilder.CreateIndex(
                name: "i_x_bulk_jobs_status",
                schema: "employee",
                table: "bulk_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_bulk_jobs_status_started_at",
                schema: "employee",
                table: "bulk_jobs",
                columns: new[] { "status", "started_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bulk_jobs",
                schema: "employee");
        }
    }
}
