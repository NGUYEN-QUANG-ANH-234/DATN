using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarAccountAuditRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_work_calendar_configs_CreatedByAccountId",
                table: "work_calendar_configs",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_work_calendar_configs_UpdatedByAccountId",
                table: "work_calendar_configs",
                column: "UpdatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_company_calendars_CreatedByAccountId",
                table: "company_calendars",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_company_calendars_UpdatedByAccountId",
                table: "company_calendars",
                column: "UpdatedByAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_company_calendars_accounts_CreatedByAccountId",
                table: "company_calendars",
                column: "CreatedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_company_calendars_accounts_UpdatedByAccountId",
                table: "company_calendars",
                column: "UpdatedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_work_calendar_configs_accounts_CreatedByAccountId",
                table: "work_calendar_configs",
                column: "CreatedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_work_calendar_configs_accounts_UpdatedByAccountId",
                table: "work_calendar_configs",
                column: "UpdatedByAccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_company_calendars_accounts_CreatedByAccountId",
                table: "company_calendars");

            migrationBuilder.DropForeignKey(
                name: "FK_company_calendars_accounts_UpdatedByAccountId",
                table: "company_calendars");

            migrationBuilder.DropForeignKey(
                name: "FK_work_calendar_configs_accounts_CreatedByAccountId",
                table: "work_calendar_configs");

            migrationBuilder.DropForeignKey(
                name: "FK_work_calendar_configs_accounts_UpdatedByAccountId",
                table: "work_calendar_configs");

            migrationBuilder.DropIndex(
                name: "IX_work_calendar_configs_CreatedByAccountId",
                table: "work_calendar_configs");

            migrationBuilder.DropIndex(
                name: "IX_work_calendar_configs_UpdatedByAccountId",
                table: "work_calendar_configs");

            migrationBuilder.DropIndex(
                name: "IX_company_calendars_CreatedByAccountId",
                table: "company_calendars");

            migrationBuilder.DropIndex(
                name: "IX_company_calendars_UpdatedByAccountId",
                table: "company_calendars");
        }
    }
}
