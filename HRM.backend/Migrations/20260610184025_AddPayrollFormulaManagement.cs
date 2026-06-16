using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollFormulaManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "payroll_formulas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActivatedByAccountId",
                table: "payroll_formulas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "payroll_formulas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedByAccountId",
                table: "payroll_formulas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByAccountId",
                table: "payroll_formulas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "payroll_formulas",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "payroll_formulas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedByAccountId",
                table: "payroll_formulas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "payroll_formulas",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "ActivatedByAccountId",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "ArchivedByAccountId",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "CreatedByAccountId",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "SubmittedByAccountId",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "payroll_formulas");
        }
    }
}
