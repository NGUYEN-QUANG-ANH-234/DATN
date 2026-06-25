using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddOvertimeRequestAccountRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_overtime_requests_DirectorReviewerAccountId",
                table: "overtime_requests",
                column: "DirectorReviewerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_overtime_requests_HrReviewerAccountId",
                table: "overtime_requests",
                column: "HrReviewerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_overtime_requests_ManagerReviewerAccountId",
                table: "overtime_requests",
                column: "ManagerReviewerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_overtime_requests_RequestedByAccountId",
                table: "overtime_requests",
                column: "RequestedByAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_overtime_requests_accounts_DirectorReviewerAccountId",
                table: "overtime_requests",
                column: "DirectorReviewerAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_overtime_requests_accounts_HrReviewerAccountId",
                table: "overtime_requests",
                column: "HrReviewerAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_overtime_requests_accounts_ManagerReviewerAccountId",
                table: "overtime_requests",
                column: "ManagerReviewerAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_overtime_requests_accounts_RequestedByAccountId",
                table: "overtime_requests",
                column: "RequestedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_overtime_requests_accounts_DirectorReviewerAccountId",
                table: "overtime_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_overtime_requests_accounts_HrReviewerAccountId",
                table: "overtime_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_overtime_requests_accounts_ManagerReviewerAccountId",
                table: "overtime_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_overtime_requests_accounts_RequestedByAccountId",
                table: "overtime_requests");

            migrationBuilder.DropIndex(
                name: "IX_overtime_requests_DirectorReviewerAccountId",
                table: "overtime_requests");

            migrationBuilder.DropIndex(
                name: "IX_overtime_requests_HrReviewerAccountId",
                table: "overtime_requests");

            migrationBuilder.DropIndex(
                name: "IX_overtime_requests_ManagerReviewerAccountId",
                table: "overtime_requests");

            migrationBuilder.DropIndex(
                name: "IX_overtime_requests_RequestedByAccountId",
                table: "overtime_requests");
        }
    }
}
