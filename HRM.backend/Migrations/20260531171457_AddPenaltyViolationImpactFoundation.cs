using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPenaltyViolationImpactFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AffectsAttendance",
                table: "penalty_rules",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AffectsPerformance",
                table: "penalty_rules",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AffectsPersonnelDecision",
                table: "penalty_rules",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AttendanceAdjustmentType",
                table: "penalty_rules",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "None")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "AttendanceDeductMinutes",
                table: "penalty_rules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AttendanceDeductWorkday",
                table: "penalty_rules",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresDirectorApproval",
                table: "penalty_rules",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresEmployeeExplanation",
                table: "penalty_rules",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresHRApproval",
                table: "penalty_rules",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "penalty_rules",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Low")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "AffectsAttendance",
                table: "penalty_records",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AffectsPerformance",
                table: "penalty_records",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AffectsPersonnelDecision",
                table: "penalty_records",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AppliedAt",
                table: "penalty_records",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByAccountId",
                table: "penalty_records",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttendanceAdjustmentLogId",
                table: "penalty_records",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeductedMinutes",
                table: "penalty_records",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeductedWorkday",
                table: "penalty_records",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeExplanation",
                table: "penalty_records",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EvidenceFilePath",
                table: "penalty_records",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "HRNote",
                table: "penalty_records",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ManagerNote",
                table: "penalty_records",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "OccurredAt",
                table: "penalty_records",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "penalty_records",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "penalty_records",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Low")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "penalty_records",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Approved")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ViolationType",
                table: "penalty_records",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Manual")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_penalty_records_ApprovedByAccountId",
                table: "penalty_records",
                column: "ApprovedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_penalty_records_AttendanceAdjustmentLogId",
                table: "penalty_records",
                column: "AttendanceAdjustmentLogId");

            migrationBuilder.CreateIndex(
                name: "IX_penalty_records_Employee_Performance_Status",
                table: "penalty_records",
                columns: new[] { "EmployeeId", "Period", "AffectsPerformance", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_penalty_records_Employee_Personnel_History",
                table: "penalty_records",
                columns: new[] { "EmployeeId", "AffectsPersonnelDecision", "Severity", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_penalty_records_Status_Impact",
                table: "penalty_records",
                columns: new[] { "Status", "AffectsAttendance", "AffectsPerformance" });

            migrationBuilder.AddForeignKey(
                name: "FK_penalty_records_accounts_ApprovedByAccountId",
                table: "penalty_records",
                column: "ApprovedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_penalty_records_attendance_adjustment_logs_AttendanceAdjustm~",
                table: "penalty_records",
                column: "AttendanceAdjustmentLogId",
                principalTable: "attendance_adjustment_logs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_penalty_records_accounts_ApprovedByAccountId",
                table: "penalty_records");

            migrationBuilder.DropForeignKey(
                name: "FK_penalty_records_attendance_adjustment_logs_AttendanceAdjustm~",
                table: "penalty_records");

            migrationBuilder.DropIndex(
                name: "IX_penalty_records_ApprovedByAccountId",
                table: "penalty_records");

            migrationBuilder.DropIndex(
                name: "IX_penalty_records_AttendanceAdjustmentLogId",
                table: "penalty_records");

            migrationBuilder.DropIndex(
                name: "IX_penalty_records_Employee_Performance_Status",
                table: "penalty_records");

            migrationBuilder.DropIndex(
                name: "IX_penalty_records_Employee_Personnel_History",
                table: "penalty_records");

            migrationBuilder.DropIndex(
                name: "IX_penalty_records_Status_Impact",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "AffectsAttendance",
                table: "penalty_rules");

            migrationBuilder.DropColumn(
                name: "AffectsPerformance",
                table: "penalty_rules");

            migrationBuilder.DropColumn(
                name: "AffectsPersonnelDecision",
                table: "penalty_rules");

            migrationBuilder.DropColumn(
                name: "AttendanceAdjustmentType",
                table: "penalty_rules");

            migrationBuilder.DropColumn(
                name: "AttendanceDeductMinutes",
                table: "penalty_rules");

            migrationBuilder.DropColumn(
                name: "AttendanceDeductWorkday",
                table: "penalty_rules");

            migrationBuilder.DropColumn(
                name: "RequiresDirectorApproval",
                table: "penalty_rules");

            migrationBuilder.DropColumn(
                name: "RequiresEmployeeExplanation",
                table: "penalty_rules");

            migrationBuilder.DropColumn(
                name: "RequiresHRApproval",
                table: "penalty_rules");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "penalty_rules");

            migrationBuilder.DropColumn(
                name: "AffectsAttendance",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "AffectsPerformance",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "AffectsPersonnelDecision",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "AppliedAt",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "ApprovedByAccountId",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "AttendanceAdjustmentLogId",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "DeductedMinutes",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "DeductedWorkday",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "EmployeeExplanation",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "EvidenceFilePath",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "HRNote",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "ManagerNote",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "OccurredAt",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "penalty_records");

            migrationBuilder.DropColumn(
                name: "ViolationType",
                table: "penalty_records");
        }
    }
}
