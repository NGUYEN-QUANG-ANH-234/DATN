using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDismissalPersonnelChangeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AccountLockedAt",
                table: "personnel_change_requests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeExplanation",
                table: "personnel_change_requests",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmployeeExplanationAt",
                table: "personnel_change_requests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmployeeNotifiedAt",
                table: "personnel_change_requests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceFilePath",
                table: "personnel_change_requests",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "LockAccountOnExecution",
                table: "personnel_change_requests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ManagerNote",
                table: "personnel_change_requests",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "RelatedFinalSettlementId",
                table: "personnel_change_requests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresFinalSettlement",
                table: "personnel_change_requests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResponseDeadlineAt",
                table: "personnel_change_requests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_RelatedFinalSettlementId",
                table: "personnel_change_requests",
                column: "RelatedFinalSettlementId");

            migrationBuilder.AddForeignKey(
                name: "FK_personnel_change_requests_final_settlements_RelatedFinalSett~",
                table: "personnel_change_requests",
                column: "RelatedFinalSettlementId",
                principalTable: "final_settlements",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_personnel_change_requests_final_settlements_RelatedFinalSett~",
                table: "personnel_change_requests");

            migrationBuilder.DropIndex(
                name: "IX_personnel_change_requests_RelatedFinalSettlementId",
                table: "personnel_change_requests");

            migrationBuilder.DropColumn(
                name: "AccountLockedAt",
                table: "personnel_change_requests");

            migrationBuilder.DropColumn(
                name: "EmployeeExplanation",
                table: "personnel_change_requests");

            migrationBuilder.DropColumn(
                name: "EmployeeExplanationAt",
                table: "personnel_change_requests");

            migrationBuilder.DropColumn(
                name: "EmployeeNotifiedAt",
                table: "personnel_change_requests");

            migrationBuilder.DropColumn(
                name: "EvidenceFilePath",
                table: "personnel_change_requests");

            migrationBuilder.DropColumn(
                name: "LockAccountOnExecution",
                table: "personnel_change_requests");

            migrationBuilder.DropColumn(
                name: "ManagerNote",
                table: "personnel_change_requests");

            migrationBuilder.DropColumn(
                name: "RelatedFinalSettlementId",
                table: "personnel_change_requests");

            migrationBuilder.DropColumn(
                name: "RequiresFinalSettlement",
                table: "personnel_change_requests");

            migrationBuilder.DropColumn(
                name: "ResponseDeadlineAt",
                table: "personnel_change_requests");
        }
    }
}
