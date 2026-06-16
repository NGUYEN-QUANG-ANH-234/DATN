using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollRunApprovalFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payrolls_accounts_CalculatedByAccountId",
                table: "payrolls");

            migrationBuilder.DropForeignKey(
                name: "FK_payrolls_accounts_LockedByAccountId",
                table: "payrolls");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "payrolls",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByAccountId",
                table: "payrolls",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "payrolls",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "payrolls",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedByAccountId",
                table: "payrolls",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_payrolls_ApprovedByAccountId",
                table: "payrolls",
                column: "ApprovedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_payrolls_SubmittedByAccountId",
                table: "payrolls",
                column: "SubmittedByAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_payrolls_accounts_ApprovedByAccountId",
                table: "payrolls",
                column: "ApprovedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_payrolls_accounts_CalculatedByAccountId",
                table: "payrolls",
                column: "CalculatedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_payrolls_accounts_LockedByAccountId",
                table: "payrolls",
                column: "LockedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_payrolls_accounts_SubmittedByAccountId",
                table: "payrolls",
                column: "SubmittedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payrolls_accounts_ApprovedByAccountId",
                table: "payrolls");

            migrationBuilder.DropForeignKey(
                name: "FK_payrolls_accounts_CalculatedByAccountId",
                table: "payrolls");

            migrationBuilder.DropForeignKey(
                name: "FK_payrolls_accounts_LockedByAccountId",
                table: "payrolls");

            migrationBuilder.DropForeignKey(
                name: "FK_payrolls_accounts_SubmittedByAccountId",
                table: "payrolls");

            migrationBuilder.DropIndex(
                name: "IX_payrolls_ApprovedByAccountId",
                table: "payrolls");

            migrationBuilder.DropIndex(
                name: "IX_payrolls_SubmittedByAccountId",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "ApprovedByAccountId",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "payrolls");

            migrationBuilder.DropColumn(
                name: "SubmittedByAccountId",
                table: "payrolls");

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
        }
    }
}
