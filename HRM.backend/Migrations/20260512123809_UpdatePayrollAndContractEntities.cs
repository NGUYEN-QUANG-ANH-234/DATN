using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePayrollAndContractEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "dependents");

            migrationBuilder.AddColumn<decimal>(
                name: "ActualWorkDays",
                table: "payrolls",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AdvancePayment",
                table: "payrolls",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxDeductionPersonal",
                table: "payrolls",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidFrom",
                table: "dependents",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidTo",
                table: "dependents",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceSalary",
                table: "contracts",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PayrollFormulaId",
                table: "contracts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_contracts_PayrollFormulaId",
                table: "contracts",
                column: "PayrollFormulaId");

            migrationBuilder.AddForeignKey(
                name: "FK_contracts_payroll_formulas_PayrollFormulaId",
                table: "contracts",
                column: "PayrollFormulaId",
                principalTable: "payroll_formulas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_contracts_payroll_formulas_PayrollFormulaId",
                table: "contracts");

            migrationBuilder.DropIndex(
                name: "IX_contracts_PayrollFormulaId",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "ActualWorkDays",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "AdvancePayment",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "TaxDeductionPersonal",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "ValidFrom",
                table: "dependents");

            migrationBuilder.DropColumn(
                name: "ValidTo",
                table: "dependents");

            migrationBuilder.DropColumn(
                name: "InsuranceSalary",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "PayrollFormulaId",
                table: "contracts");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "dependents",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
