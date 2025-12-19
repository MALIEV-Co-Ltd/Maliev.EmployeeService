using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.EmployeeService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "title",
                schema: "employee",
                table: "goals");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "employee",
                table: "performance_reviews",
                newName: "review_period_start");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "employee",
                table: "goals",
                newName: "target_date");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "employee",
                table: "performance_reviews",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Draft",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "review_date",
                schema: "employee",
                table: "performance_reviews",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<DateTime>(
                name: "acknowledged_date",
                schema: "employee",
                table: "performance_reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "employee",
                table: "performance_reviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_date",
                schema: "employee",
                table: "performance_reviews",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "feedback",
                schema: "employee",
                table: "performance_reviews",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "modified_by",
                schema: "employee",
                table: "performance_reviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_date",
                schema: "employee",
                table: "performance_reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rating",
                schema: "employee",
                table: "performance_reviews",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "review_cycle",
                schema: "employee",
                table: "performance_reviews",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "review_period_end",
                schema: "employee",
                table: "performance_reviews",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "self_assessment",
                schema: "employee",
                table: "performance_reviews",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "completed_date",
                schema: "employee",
                table: "goals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "completion_status",
                schema: "employee",
                table: "goals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "employee",
                table: "goals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_date",
                schema: "employee",
                table: "goals",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "employee",
                table: "goals",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "modified_by",
                schema: "employee",
                table: "goals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_date",
                schema: "employee",
                table: "goals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "performance_review_id",
                schema: "employee",
                table: "goals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "progress_updates",
                schema: "employee",
                table: "goals",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "success_criteria",
                schema: "employee",
                table: "goals",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "performance_improvement_plans",
                schema: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issues_documented = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    milestones = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    progress_notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    outcome = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_performance_improvement_plans", x => x.id);
                    table.ForeignKey(
                        name: "f_k_performance_improvement_plans_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_performance_improvement_plans_employees_manager_id",
                        column: x => x.manager_id,
                        principalSchema: "employee",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_performance_reviews_employee_id",
                schema: "employee",
                table: "performance_reviews",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_performance_reviews_employee_id_review_period_start_review_~",
                schema: "employee",
                table: "performance_reviews",
                columns: new[] { "employee_id", "review_period_start", "review_period_end" });

            migrationBuilder.CreateIndex(
                name: "i_x_performance_reviews_review_cycle",
                schema: "employee",
                table: "performance_reviews",
                column: "review_cycle");

            migrationBuilder.CreateIndex(
                name: "i_x_performance_reviews_reviewer_id",
                schema: "employee",
                table: "performance_reviews",
                column: "reviewer_id");

            migrationBuilder.CreateIndex(
                name: "i_x_performance_reviews_status",
                schema: "employee",
                table: "performance_reviews",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_goals_completion_status",
                schema: "employee",
                table: "goals",
                column: "completion_status");

            migrationBuilder.CreateIndex(
                name: "i_x_goals_employee_id",
                schema: "employee",
                table: "goals",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_goals_employee_id_target_date",
                schema: "employee",
                table: "goals",
                columns: new[] { "employee_id", "target_date" });

            migrationBuilder.CreateIndex(
                name: "i_x_goals_performance_review_id",
                schema: "employee",
                table: "goals",
                column: "performance_review_id");

            migrationBuilder.CreateIndex(
                name: "i_x_goals_target_date",
                schema: "employee",
                table: "goals",
                column: "target_date");

            migrationBuilder.CreateIndex(
                name: "i_x_performance_improvement_plans_employee_id",
                schema: "employee",
                table: "performance_improvement_plans",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "i_x_performance_improvement_plans_employee_id_start_date_end_da~",
                schema: "employee",
                table: "performance_improvement_plans",
                columns: new[] { "employee_id", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "i_x_performance_improvement_plans_manager_id",
                schema: "employee",
                table: "performance_improvement_plans",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "i_x_performance_improvement_plans_status",
                schema: "employee",
                table: "performance_improvement_plans",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_performance_improvement_plans_status_end_date",
                schema: "employee",
                table: "performance_improvement_plans",
                columns: new[] { "status", "end_date" });

            migrationBuilder.AddForeignKey(
                name: "f_k_goals__performance_reviews_performance_review_id",
                schema: "employee",
                table: "goals",
                column: "performance_review_id",
                principalSchema: "employee",
                principalTable: "performance_reviews",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

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
                name: "f_k_performance_reviews_employees_employee_id",
                schema: "employee",
                table: "performance_reviews",
                column: "employee_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_performance_reviews_employees_reviewer_id",
                schema: "employee",
                table: "performance_reviews",
                column: "reviewer_id",
                principalSchema: "employee",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_goals__performance_reviews_performance_review_id",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropForeignKey(
                name: "f_k_goals_employees_employee_id",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropForeignKey(
                name: "f_k_performance_reviews_employees_employee_id",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropForeignKey(
                name: "f_k_performance_reviews_employees_reviewer_id",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropTable(
                name: "performance_improvement_plans",
                schema: "employee");

            migrationBuilder.DropIndex(
                name: "i_x_performance_reviews_employee_id",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropIndex(
                name: "i_x_performance_reviews_employee_id_review_period_start_review_~",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropIndex(
                name: "i_x_performance_reviews_review_cycle",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropIndex(
                name: "i_x_performance_reviews_reviewer_id",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropIndex(
                name: "i_x_performance_reviews_status",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropIndex(
                name: "i_x_goals_completion_status",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropIndex(
                name: "i_x_goals_employee_id",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropIndex(
                name: "i_x_goals_employee_id_target_date",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropIndex(
                name: "i_x_goals_performance_review_id",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropIndex(
                name: "i_x_goals_target_date",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "acknowledged_date",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "created_date",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "feedback",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "modified_by",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "modified_date",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "rating",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "review_cycle",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "review_period_end",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "self_assessment",
                schema: "employee",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "completed_date",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "completion_status",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "created_date",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "modified_by",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "modified_date",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "performance_review_id",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "progress_updates",
                schema: "employee",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "success_criteria",
                schema: "employee",
                table: "goals");

            migrationBuilder.RenameColumn(
                name: "review_period_start",
                schema: "employee",
                table: "performance_reviews",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "target_date",
                schema: "employee",
                table: "goals",
                newName: "created_at");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "employee",
                table: "performance_reviews",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Draft");

            migrationBuilder.AlterColumn<DateTime>(
                name: "review_date",
                schema: "employee",
                table: "performance_reviews",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "employee",
                table: "goals",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "title",
                schema: "employee",
                table: "goals",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
