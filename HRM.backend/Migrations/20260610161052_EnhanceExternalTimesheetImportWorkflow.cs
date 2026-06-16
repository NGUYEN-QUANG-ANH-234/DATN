using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceExternalTimesheetImportWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollaboratorNameSnapshot",
                table: "external_timesheet_lines",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "external_timesheet_lines",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "RowNumber",
                table: "external_timesheet_lines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ValidationStatus",
                table: "external_timesheet_lines",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Valid")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "external_timesheet_imports",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByAccountId",
                table: "external_timesheet_imports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ErrorRows",
                table: "external_timesheet_imports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "external_timesheet_imports",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "external_timesheet_imports",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalHours",
                table: "external_timesheet_imports",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TotalRows",
                table: "external_timesheet_imports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ValidRows",
                table: "external_timesheet_imports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 14,
                column: "Name",
                value: "Thu nhập cộng tác viên");

            migrationBuilder.CreateIndex(
                name: "IX_external_timesheet_imports_ApprovedByAccountId",
                table: "external_timesheet_imports",
                column: "ApprovedByAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_external_timesheet_imports_accounts_ApprovedByAccountId",
                table: "external_timesheet_imports",
                column: "ApprovedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_external_timesheet_imports_accounts_ApprovedByAccountId",
                table: "external_timesheet_imports");

            migrationBuilder.DropIndex(
                name: "IX_external_timesheet_imports_ApprovedByAccountId",
                table: "external_timesheet_imports");

            migrationBuilder.DropColumn(
                name: "CollaboratorNameSnapshot",
                table: "external_timesheet_lines");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "external_timesheet_lines");

            migrationBuilder.DropColumn(
                name: "RowNumber",
                table: "external_timesheet_lines");

            migrationBuilder.DropColumn(
                name: "ValidationStatus",
                table: "external_timesheet_lines");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "external_timesheet_imports");

            migrationBuilder.DropColumn(
                name: "ApprovedByAccountId",
                table: "external_timesheet_imports");

            migrationBuilder.DropColumn(
                name: "ErrorRows",
                table: "external_timesheet_imports");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "external_timesheet_imports");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "external_timesheet_imports");

            migrationBuilder.DropColumn(
                name: "TotalHours",
                table: "external_timesheet_imports");

            migrationBuilder.DropColumn(
                name: "TotalRows",
                table: "external_timesheet_imports");

            migrationBuilder.DropColumn(
                name: "ValidRows",
                table: "external_timesheet_imports");

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 14,
                column: "Name",
                value: "Thu nhập từ timesheet ngoài");
        }
    }
}
