using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModuleTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_task_feedbacks_employees_ReviewerId",
                table: "task_feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_department_budgets_DeptBudgetId",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_employees_AssignedTo",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_trainings_employees_EmployeeId",
                table: "trainings");

            migrationBuilder.DropTable(
                name: "department_budgets");

            migrationBuilder.DropTable(
                name: "task_progresses");

            migrationBuilder.DropIndex(
                name: "IX_tasks_DeptBudgetId",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "DeptBudgetId",
                table: "tasks");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "trainings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalScore",
                table: "trainings",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPassed",
                table: "trainings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ManagerEvaluation",
                table: "trainings",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "performance_reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalScore = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    FinalRating = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_performance_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_performance_reviews_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "performance_details",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReviewId = table.Column<int>(type: "int", nullable: false),
                    KpiName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WeightPercent = table.Column<int>(type: "int", nullable: false),
                    AchievedPercent = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    FinalPoint = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_performance_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_performance_details_performance_reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "performance_reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_performance_details_ReviewId",
                table: "performance_details",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_performance_reviews_EmployeeId",
                table: "performance_reviews",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_task_feedbacks_employees_ReviewerId",
                table: "task_feedbacks",
                column: "ReviewerId",
                principalTable: "employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_employees_AssignedTo",
                table: "tasks",
                column: "AssignedTo",
                principalTable: "employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_trainings_employees_EmployeeId",
                table: "trainings",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_task_feedbacks_employees_ReviewerId",
                table: "task_feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_employees_AssignedTo",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_trainings_employees_EmployeeId",
                table: "trainings");

            migrationBuilder.DropTable(
                name: "performance_details");

            migrationBuilder.DropTable(
                name: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "FinalScore",
                table: "trainings");

            migrationBuilder.DropColumn(
                name: "IsPassed",
                table: "trainings");

            migrationBuilder.DropColumn(
                name: "ManagerEvaluation",
                table: "trainings");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "trainings",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "DeptBudgetId",
                table: "tasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "department_budgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DeptId = table.Column<int>(type: "int", nullable: true),
                    DeadlineAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MonthYear = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalBudget = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    UsedBudget = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_department_budgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_department_budgets_departments_DeptId",
                        column: x => x.DeptId,
                        principalTable: "departments",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "task_progresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TaskId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_progresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_progresses_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "tasks",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_DeptBudgetId",
                table: "tasks",
                column: "DeptBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_department_budgets_DeptId",
                table: "department_budgets",
                column: "DeptId");

            migrationBuilder.CreateIndex(
                name: "IX_task_progresses_TaskId",
                table: "task_progresses",
                column: "TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_task_feedbacks_employees_ReviewerId",
                table: "task_feedbacks",
                column: "ReviewerId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_department_budgets_DeptBudgetId",
                table: "tasks",
                column: "DeptBudgetId",
                principalTable: "department_budgets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_employees_AssignedTo",
                table: "tasks",
                column: "AssignedTo",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_trainings_employees_EmployeeId",
                table: "trainings",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id");
        }
    }
}
