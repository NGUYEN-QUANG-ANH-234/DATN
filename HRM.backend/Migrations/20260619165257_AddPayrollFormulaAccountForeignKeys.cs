using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollFormulaAccountForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_payroll_formulas_ActivatedByAccountId",
                table: "payroll_formulas",
                column: "ActivatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_formulas_ApprovedByAccountId",
                table: "payroll_formulas",
                column: "ApprovedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_formulas_ArchivedByAccountId",
                table: "payroll_formulas",
                column: "ArchivedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_formulas_CreatedByAccountId",
                table: "payroll_formulas",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_formulas_SubmittedByAccountId",
                table: "payroll_formulas",
                column: "SubmittedByAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_formulas_accounts_ActivatedByAccountId",
                table: "payroll_formulas",
                column: "ActivatedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_formulas_accounts_ApprovedByAccountId",
                table: "payroll_formulas",
                column: "ApprovedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_formulas_accounts_ArchivedByAccountId",
                table: "payroll_formulas",
                column: "ArchivedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_formulas_accounts_CreatedByAccountId",
                table: "payroll_formulas",
                column: "CreatedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_formulas_accounts_SubmittedByAccountId",
                table: "payroll_formulas",
                column: "SubmittedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payroll_formulas_accounts_ActivatedByAccountId",
                table: "payroll_formulas");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_formulas_accounts_ApprovedByAccountId",
                table: "payroll_formulas");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_formulas_accounts_ArchivedByAccountId",
                table: "payroll_formulas");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_formulas_accounts_CreatedByAccountId",
                table: "payroll_formulas");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_formulas_accounts_SubmittedByAccountId",
                table: "payroll_formulas");

            migrationBuilder.DropIndex(
                name: "IX_payroll_formulas_ActivatedByAccountId",
                table: "payroll_formulas");

            migrationBuilder.DropIndex(
                name: "IX_payroll_formulas_ApprovedByAccountId",
                table: "payroll_formulas");

            migrationBuilder.DropIndex(
                name: "IX_payroll_formulas_ArchivedByAccountId",
                table: "payroll_formulas");

            migrationBuilder.DropIndex(
                name: "IX_payroll_formulas_CreatedByAccountId",
                table: "payroll_formulas");

            migrationBuilder.DropIndex(
                name: "IX_payroll_formulas_SubmittedByAccountId",
                table: "payroll_formulas");
        }
    }
}
