using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class RestructureTasksTrainingForModule5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DropForeignKeyIfExists(migrationBuilder, "task_feedbacks", "FK_task_feedbacks_employees_ReviewerId");
            DropForeignKeyIfExists(migrationBuilder, "task_feedbacks", "FK_task_feedbacks_tasks_TaskId");
            DropForeignKeyIfExists(migrationBuilder, "tasks", "FK_tasks_employees_AssignedTo");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "trainings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "trainings",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AddColumn<int>(
                name: "DeptId",
                table: "trainings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EvaluatedAt",
                table: "trainings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EvaluationDeadline",
                table: "trainings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManagerId",
                table: "trainings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromotionRequestId",
                table: "trainings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "trainings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrainingType",
                table: "trainings",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "EvidencePath",
                table: "tasks",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "tasks",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "tasks",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AddColumn<int>(
                name: "CreatedByAccountId",
                table: "tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeptId",
                table: "tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "tasks",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercent",
                table: "tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewDeadline",
                table: "tasks",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "tasks",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrainingId",
                table: "tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedbackType",
                table: "task_feedbacks",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Comment")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ProgressId",
                table: "task_feedbacks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "performance_reviews",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AddColumn<int>(
                name: "CreatedByAccountId",
                table: "performance_reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeptId",
                table: "performance_reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalComment",
                table: "performance_reviews",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalizedAt",
                table: "performance_reviews",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImportBatchId",
                table: "performance_reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPayrollSynced",
                table: "performance_reviews",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PayrollSyncedAt",
                table: "performance_reviews",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewDeadline",
                table: "performance_reviews",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewerAccountId",
                table: "performance_reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalWeight",
                table: "performance_reviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualValue",
                table: "performance_details",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "performance_details",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeComment",
                table: "performance_details",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "EmployeeSelfPercent",
                table: "performance_details",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "EvidencePath",
                table: "performance_details",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "KpiCode",
                table: "performance_details",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ManagerComment",
                table: "performance_details",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "ManagerScore",
                table: "performance_details",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetValue",
                table: "performance_details",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "performance_details",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "kpi_import_batches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Period = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeptId = table.Column<int>(type: "int", nullable: false),
                    ImportedByAccountId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    SuccessRows = table.Column<int>(type: "int", nullable: false),
                    ErrorRows = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kpi_import_batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_kpi_import_batches_accounts_ImportedByAccountId",
                        column: x => x.ImportedByAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_kpi_import_batches_departments_DeptId",
                        column: x => x.DeptId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                "UPDATE performance_details SET KpiCode = CONCAT('KPI-', ReviewId, '-', Id) WHERE KpiCode IS NULL OR KpiCode = '';");

            migrationBuilder.CreateTable(
                name: "task_progresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EvidencePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubmittedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_progresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_progresses_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_task_progresses_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_trainings_DeptId",
                table: "trainings",
                column: "DeptId");

            migrationBuilder.CreateIndex(
                name: "IX_trainings_EmployeeId_Status",
                table: "trainings",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_trainings_ManagerId_EvaluationDeadline_Status",
                table: "trainings",
                columns: new[] { "ManagerId", "EvaluationDeadline", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_tasks_AssignedTo_Status_Deadline",
                table: "tasks",
                columns: new[] { "AssignedTo", "Status", "Deadline" });

            migrationBuilder.CreateIndex(
                name: "IX_tasks_CreatedByAccountId",
                table: "tasks",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_DeptId",
                table: "tasks",
                column: "DeptId");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_TrainingId",
                table: "tasks",
                column: "TrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_task_feedbacks_ProgressId",
                table: "task_feedbacks",
                column: "ProgressId");

            migrationBuilder.CreateIndex(
                name: "IX_performance_reviews_CreatedByAccountId",
                table: "performance_reviews",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_performance_reviews_DeptId_Period_Status",
                table: "performance_reviews",
                columns: new[] { "DeptId", "Period", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_performance_reviews_ImportBatchId",
                table: "performance_reviews",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_performance_reviews_ReviewerAccountId",
                table: "performance_reviews",
                column: "ReviewerAccountId");

            migrationBuilder.CreateIndex(
                name: "UX_performance_reviews_EmployeeId_Period",
                table: "performance_reviews",
                columns: new[] { "EmployeeId", "Period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_performance_details_ReviewId_KpiCode",
                table: "performance_details",
                columns: new[] { "ReviewId", "KpiCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_kpi_import_batches_DeptId",
                table: "kpi_import_batches",
                column: "DeptId");

            migrationBuilder.CreateIndex(
                name: "IX_kpi_import_batches_ImportedByAccountId",
                table: "kpi_import_batches",
                column: "ImportedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_task_progresses_EmployeeId",
                table: "task_progresses",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_task_progresses_TaskId_SubmittedAt",
                table: "task_progresses",
                columns: new[] { "TaskId", "SubmittedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_performance_reviews_accounts_CreatedByAccountId",
                table: "performance_reviews",
                column: "CreatedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_performance_reviews_accounts_ReviewerAccountId",
                table: "performance_reviews",
                column: "ReviewerAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_performance_reviews_departments_DeptId",
                table: "performance_reviews",
                column: "DeptId",
                principalTable: "departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_performance_reviews_kpi_import_batches_ImportBatchId",
                table: "performance_reviews",
                column: "ImportBatchId",
                principalTable: "kpi_import_batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_task_feedbacks_employees_ReviewerId",
                table: "task_feedbacks",
                column: "ReviewerId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_task_feedbacks_task_progresses_ProgressId",
                table: "task_feedbacks",
                column: "ProgressId",
                principalTable: "task_progresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_task_feedbacks_tasks_TaskId",
                table: "task_feedbacks",
                column: "TaskId",
                principalTable: "tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_accounts_CreatedByAccountId",
                table: "tasks",
                column: "CreatedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_departments_DeptId",
                table: "tasks",
                column: "DeptId",
                principalTable: "departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_employees_AssignedTo",
                table: "tasks",
                column: "AssignedTo",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_trainings_TrainingId",
                table: "tasks",
                column: "TrainingId",
                principalTable: "trainings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_trainings_departments_DeptId",
                table: "trainings",
                column: "DeptId",
                principalTable: "departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_trainings_employees_ManagerId",
                table: "trainings",
                column: "ManagerId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_performance_reviews_accounts_CreatedByAccountId",
                table: "performance_reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_performance_reviews_accounts_ReviewerAccountId",
                table: "performance_reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_performance_reviews_departments_DeptId",
                table: "performance_reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_performance_reviews_kpi_import_batches_ImportBatchId",
                table: "performance_reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_task_feedbacks_employees_ReviewerId",
                table: "task_feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_task_feedbacks_task_progresses_ProgressId",
                table: "task_feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_task_feedbacks_tasks_TaskId",
                table: "task_feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_accounts_CreatedByAccountId",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_departments_DeptId",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_employees_AssignedTo",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_trainings_TrainingId",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_trainings_departments_DeptId",
                table: "trainings");

            migrationBuilder.DropForeignKey(
                name: "FK_trainings_employees_ManagerId",
                table: "trainings");

            migrationBuilder.DropTable(
                name: "kpi_import_batches");

            migrationBuilder.DropTable(
                name: "task_progresses");

            migrationBuilder.DropIndex(
                name: "IX_trainings_DeptId",
                table: "trainings");

            migrationBuilder.DropIndex(
                name: "IX_trainings_EmployeeId_Status",
                table: "trainings");

            migrationBuilder.DropIndex(
                name: "IX_trainings_ManagerId_EvaluationDeadline_Status",
                table: "trainings");

            migrationBuilder.DropIndex(
                name: "IX_tasks_AssignedTo_Status_Deadline",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "IX_tasks_CreatedByAccountId",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "IX_tasks_DeptId",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "IX_tasks_TrainingId",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "IX_task_feedbacks_ProgressId",
                table: "task_feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_performance_reviews_CreatedByAccountId",
                table: "performance_reviews");

            migrationBuilder.DropIndex(
                name: "IX_performance_reviews_DeptId_Period_Status",
                table: "performance_reviews");

            migrationBuilder.DropIndex(
                name: "IX_performance_reviews_ImportBatchId",
                table: "performance_reviews");

            migrationBuilder.DropIndex(
                name: "IX_performance_reviews_ReviewerAccountId",
                table: "performance_reviews");

            migrationBuilder.DropIndex(
                name: "UX_performance_reviews_EmployeeId_Period",
                table: "performance_reviews");

            migrationBuilder.DropIndex(
                name: "UX_performance_details_ReviewId_KpiCode",
                table: "performance_details");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "trainings");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "trainings");

            migrationBuilder.DropColumn(
                name: "DeptId",
                table: "trainings");

            migrationBuilder.DropColumn(
                name: "EvaluatedAt",
                table: "trainings");

            migrationBuilder.DropColumn(
                name: "EvaluationDeadline",
                table: "trainings");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "trainings");

            migrationBuilder.DropColumn(
                name: "PromotionRequestId",
                table: "trainings");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "trainings");

            migrationBuilder.DropColumn(
                name: "TrainingType",
                table: "trainings");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "CreatedByAccountId",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "DeptId",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "ProgressPercent",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "ReviewDeadline",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "TrainingId",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "FeedbackType",
                table: "task_feedbacks");

            migrationBuilder.DropColumn(
                name: "ProgressId",
                table: "task_feedbacks");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "CreatedByAccountId",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "DeptId",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "FinalComment",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "FinalizedAt",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "ImportBatchId",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "IsPayrollSynced",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "PayrollSyncedAt",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "ReviewDeadline",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "ReviewerAccountId",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "TotalWeight",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "ActualValue",
                table: "performance_details");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "performance_details");

            migrationBuilder.DropColumn(
                name: "EmployeeComment",
                table: "performance_details");

            migrationBuilder.DropColumn(
                name: "EmployeeSelfPercent",
                table: "performance_details");

            migrationBuilder.DropColumn(
                name: "EvidencePath",
                table: "performance_details");

            migrationBuilder.DropColumn(
                name: "KpiCode",
                table: "performance_details");

            migrationBuilder.DropColumn(
                name: "ManagerComment",
                table: "performance_details");

            migrationBuilder.DropColumn(
                name: "ManagerScore",
                table: "performance_details");

            migrationBuilder.DropColumn(
                name: "TargetValue",
                table: "performance_details");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "performance_details");

            migrationBuilder.AlterColumn<string>(
                name: "EvidencePath",
                table: "tasks",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_task_feedbacks_employees_ReviewerId",
                table: "task_feedbacks",
                column: "ReviewerId",
                principalTable: "employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_task_feedbacks_tasks_TaskId",
                table: "task_feedbacks",
                column: "TaskId",
                principalTable: "tasks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_employees_AssignedTo",
                table: "tasks",
                column: "AssignedTo",
                principalTable: "employees",
                principalColumn: "Id");
        }

        private static void DropForeignKeyIfExists(MigrationBuilder migrationBuilder, string tableName, string foreignKeyName)
        {
            migrationBuilder.Sql($@"
SET @fk_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE CONSTRAINT_SCHEMA = DATABASE()
      AND TABLE_NAME = '{tableName}'
      AND CONSTRAINT_NAME = '{foreignKeyName}'
      AND CONSTRAINT_TYPE = 'FOREIGN KEY'
);
SET @drop_fk_sql := IF(@fk_exists > 0, 'ALTER TABLE `{tableName}` DROP FOREIGN KEY `{foreignKeyName}`', 'SELECT 1');
PREPARE drop_fk_stmt FROM @drop_fk_sql;
EXECUTE drop_fk_stmt;
DEALLOCATE PREPARE drop_fk_stmt;");
        }
    }
}
