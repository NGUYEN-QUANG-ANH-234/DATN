using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkCalendarCompanyCalendarOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyCalendarId",
                table: "work_calendar_configs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "HolidayWorkingEndTime",
                table: "work_calendar_configs",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "HolidayWorkingStartTime",
                table: "work_calendar_configs",
                type: "time(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_calendar_configs_CompanyCalendarId",
                table: "work_calendar_configs",
                column: "CompanyCalendarId");

            migrationBuilder.AddForeignKey(
                name: "FK_work_calendar_configs_company_calendars_CompanyCalendarId",
                table: "work_calendar_configs",
                column: "CompanyCalendarId",
                principalTable: "company_calendars",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_work_calendar_configs_company_calendars_CompanyCalendarId",
                table: "work_calendar_configs");

            migrationBuilder.DropIndex(
                name: "IX_work_calendar_configs_CompanyCalendarId",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "CompanyCalendarId",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "HolidayWorkingEndTime",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "HolidayWorkingStartTime",
                table: "work_calendar_configs");
        }
    }
}
