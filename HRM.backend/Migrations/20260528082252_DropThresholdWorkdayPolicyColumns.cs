using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class DropThresholdWorkdayPolicyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullDayMinHours",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "HalfDayMinHours",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "WorkdayCalculationMode",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "WorkdayCalculationMode",
                table: "attendance_summaries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FullDayMinHours",
                table: "work_calendar_configs",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HalfDayMinHours",
                table: "work_calendar_configs",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "WorkdayCalculationMode",
                table: "work_calendar_configs",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "WorkdayCalculationMode",
                table: "attendance_summaries",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
