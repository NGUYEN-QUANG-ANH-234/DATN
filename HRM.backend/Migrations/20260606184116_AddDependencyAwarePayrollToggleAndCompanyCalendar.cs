using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDependencyAwarePayrollToggleAndCompanyCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "company_calendars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    VersionCode = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false, defaultValue: "Draft")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceRef = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SupersedesVersionId = table.Column<int>(type: "int", nullable: true),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LockedAfterUsed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedByAccountId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_calendars", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "company_calendar_days",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CalendarId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DayType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsPaid = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsOvertimeHoliday = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsWorkingDayOverride = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_calendar_days", x => x.Id);
                    table.ForeignKey(
                        name: "FK_company_calendar_days_company_calendars_CalendarId",
                        column: x => x.CalendarId,
                        principalTable: "company_calendars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "UX_company_calendar_days_Calendar_Date",
                table: "company_calendar_days",
                columns: new[] { "CalendarId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_company_calendars_Year_Status_EffectiveFrom",
                table: "company_calendars",
                columns: new[] { "Year", "Status", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "UX_company_calendars_Year_Version",
                table: "company_calendars",
                columns: new[] { "Year", "VersionCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_calendar_days");

            migrationBuilder.DropTable(
                name: "company_calendars");
        }
    }
}
