using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendancePeriodClosing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPayrollLocked",
                table: "leave_requests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PayrollLockedAt",
                table: "leave_requests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayrollPeriod",
                table: "leave_requests",
                type: "varchar(7)",
                maxLength: 7,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "attendance_summaries",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Draft")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "attendance_summaries",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByAccountId",
                table: "attendance_summaries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "attendance_summaries",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LockedByAccountId",
                table: "attendance_summaries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PeriodNote",
                table: "attendance_summaries",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "attendance_summaries",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedByAccountId",
                table: "attendance_summaries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_PayrollPeriod_Locked",
                table: "leave_requests",
                columns: new[] { "PayrollPeriod", "IsPayrollLocked" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_summaries_Period_Status_Locked",
                table: "attendance_summaries",
                columns: new[] { "Month", "Year", "ApprovalStatus", "IsPayrollLocked" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_leave_requests_PayrollPeriod_Locked",
                table: "leave_requests");

            migrationBuilder.DropIndex(
                name: "IX_attendance_summaries_Period_Status_Locked",
                table: "attendance_summaries");

            migrationBuilder.DropColumn(
                name: "IsPayrollLocked",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "PayrollLockedAt",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "PayrollPeriod",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "attendance_summaries");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "attendance_summaries");

            migrationBuilder.DropColumn(
                name: "ApprovedByAccountId",
                table: "attendance_summaries");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "attendance_summaries");

            migrationBuilder.DropColumn(
                name: "LockedByAccountId",
                table: "attendance_summaries");

            migrationBuilder.DropColumn(
                name: "PeriodNote",
                table: "attendance_summaries");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "attendance_summaries");

            migrationBuilder.DropColumn(
                name: "SubmittedByAccountId",
                table: "attendance_summaries");
        }
    }
}
