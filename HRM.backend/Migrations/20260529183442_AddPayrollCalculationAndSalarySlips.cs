using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollCalculationAndSalarySlips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payrolls_employees_EmployeeId",
                table: "payrolls");

            migrationBuilder.DropIndex(
                name: "IX_payrolls_EmployeeId",
                table: "payrolls");

            migrationBuilder.AddColumn<int>(
                name: "ActualOtMinutes",
                table: "payrolls",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualWorkHours",
                table: "payrolls",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseSalary",
                table: "payrolls",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseSalaryActual",
                table: "payrolls",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CalculatedAt",
                table: "payrolls",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CalculatedByAccountId",
                table: "payrolls",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EmployeeInsuranceAmount",
                table: "payrolls",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EmployerContributionAmount",
                table: "payrolls",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormulaSnapshotJson",
                table: "payrolls",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "GrossIncome",
                table: "payrolls",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceSalary",
                table: "payrolls",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "payrolls",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LockedByAccountId",
                table: "payrolls",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherDeductions",
                table: "payrolls",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Period",
                table: "payrolls",
                type: "varchar(7)",
                maxLength: 7,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PolicySnapshotJson",
                table: "payrolls",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableGrossIncome",
                table: "payrolls",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCompanyCost",
                table: "payrolls",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "payroll_details",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PayrollId = table.Column<int>(type: "int", nullable: false),
                    ComponentCode = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ComponentName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    InsuranceBaseAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    IsIncome = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeduction = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsTaxable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsInsuranceBased = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ProrationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CalculationMethod = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SnapshotJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payroll_details_payrolls_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_payrolls_CalculatedByAccountId",
                table: "payrolls",
                column: "CalculatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_payrolls_Employee_Period",
                table: "payrolls",
                columns: new[] { "EmployeeId", "Month", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_payrolls_LockedByAccountId",
                table: "payrolls",
                column: "LockedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_payrolls_Period_Status",
                table: "payrolls",
                columns: new[] { "Month", "Year", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_details_Payroll_Component",
                table: "payroll_details",
                columns: new[] { "PayrollId", "ComponentCode" });

            migrationBuilder.AddForeignKey(
                name: "FK_payrolls_accounts_CalculatedByAccountId",
                table: "payrolls",
                column: "CalculatedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_payrolls_accounts_LockedByAccountId",
                table: "payrolls",
                column: "LockedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_payrolls_employees_EmployeeId",
                table: "payrolls",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payrolls_accounts_CalculatedByAccountId",
                table: "payrolls");

            migrationBuilder.DropForeignKey(
                name: "FK_payrolls_accounts_LockedByAccountId",
                table: "payrolls");

            migrationBuilder.DropForeignKey(
                name: "FK_payrolls_employees_EmployeeId",
                table: "payrolls");

            migrationBuilder.DropTable(
                name: "payroll_details");

            migrationBuilder.DropIndex(
                name: "IX_payrolls_CalculatedByAccountId",
                table: "payrolls");

            migrationBuilder.DropIndex(
                name: "IX_payrolls_Employee_Period",
                table: "payrolls");

            migrationBuilder.DropIndex(
                name: "IX_payrolls_LockedByAccountId",
                table: "payrolls");

            migrationBuilder.DropIndex(
                name: "IX_payrolls_Period_Status",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "ActualOtMinutes",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "ActualWorkHours",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "BaseSalary",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "BaseSalaryActual",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "CalculatedAt",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "CalculatedByAccountId",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "EmployeeInsuranceAmount",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "EmployerContributionAmount",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "FormulaSnapshotJson",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "GrossIncome",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "InsuranceSalary",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "LockedByAccountId",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "OtherDeductions",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "Period",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "PolicySnapshotJson",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "TaxableGrossIncome",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "TotalCompanyCost",
                table: "payrolls");

            migrationBuilder.CreateIndex(
                name: "IX_payrolls_EmployeeId",
                table: "payrolls",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_payrolls_employees_EmployeeId",
                table: "payrolls",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id");
        }
    }
}
