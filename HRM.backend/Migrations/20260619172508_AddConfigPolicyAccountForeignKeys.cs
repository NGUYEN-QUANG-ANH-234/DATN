using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigPolicyAccountForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_tax_configs_CreatedByAccountId",
                table: "tax_configs",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_pit_tax_brackets_CreatedByAccountId",
                table: "pit_tax_brackets",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_policies_CreatedByAccountId",
                table: "payroll_policies",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_policies_UpdatedByAccountId",
                table: "payroll_policies",
                column: "UpdatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_overtime_rate_configs_CreatedByAccountId",
                table: "overtime_rate_configs",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_insurance_configs_CreatedByAccountId",
                table: "insurance_configs",
                column: "CreatedByAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_insurance_configs_accounts_CreatedByAccountId",
                table: "insurance_configs",
                column: "CreatedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_overtime_rate_configs_accounts_CreatedByAccountId",
                table: "overtime_rate_configs",
                column: "CreatedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_policies_accounts_CreatedByAccountId",
                table: "payroll_policies",
                column: "CreatedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_policies_accounts_UpdatedByAccountId",
                table: "payroll_policies",
                column: "UpdatedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_pit_tax_brackets_accounts_CreatedByAccountId",
                table: "pit_tax_brackets",
                column: "CreatedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_tax_configs_accounts_CreatedByAccountId",
                table: "tax_configs",
                column: "CreatedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_insurance_configs_accounts_CreatedByAccountId",
                table: "insurance_configs");

            migrationBuilder.DropForeignKey(
                name: "FK_overtime_rate_configs_accounts_CreatedByAccountId",
                table: "overtime_rate_configs");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_policies_accounts_CreatedByAccountId",
                table: "payroll_policies");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_policies_accounts_UpdatedByAccountId",
                table: "payroll_policies");

            migrationBuilder.DropForeignKey(
                name: "FK_pit_tax_brackets_accounts_CreatedByAccountId",
                table: "pit_tax_brackets");

            migrationBuilder.DropForeignKey(
                name: "FK_tax_configs_accounts_CreatedByAccountId",
                table: "tax_configs");

            migrationBuilder.DropIndex(
                name: "IX_tax_configs_CreatedByAccountId",
                table: "tax_configs");

            migrationBuilder.DropIndex(
                name: "IX_pit_tax_brackets_CreatedByAccountId",
                table: "pit_tax_brackets");

            migrationBuilder.DropIndex(
                name: "IX_payroll_policies_CreatedByAccountId",
                table: "payroll_policies");

            migrationBuilder.DropIndex(
                name: "IX_payroll_policies_UpdatedByAccountId",
                table: "payroll_policies");

            migrationBuilder.DropIndex(
                name: "IX_overtime_rate_configs_CreatedByAccountId",
                table: "overtime_rate_configs");

            migrationBuilder.DropIndex(
                name: "IX_insurance_configs_CreatedByAccountId",
                table: "insurance_configs");
        }
    }
}
