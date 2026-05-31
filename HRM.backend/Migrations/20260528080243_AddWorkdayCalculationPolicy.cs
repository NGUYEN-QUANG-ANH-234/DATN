using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkdayCalculationPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FullDayMinHours",
                table: "work_calendar_configs",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 8m);

            migrationBuilder.AddColumn<decimal>(
                name: "HalfDayMinHours",
                table: "work_calendar_configs",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 4m);

            migrationBuilder.AddColumn<bool>(
                name: "IncludePaidLeaveInWorkDays",
                table: "work_calendar_configs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardHoursPerDay",
                table: "work_calendar_configs",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 8m);

            migrationBuilder.AddColumn<string>(
                name: "WorkdayCalculationMode",
                table: "work_calendar_configs",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Threshold")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "PayableWorkHours",
                table: "attendance_summaries",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "WorkdayCalculationMode",
                table: "attendance_summaries",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Threshold")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "WorkedMinutes",
                table: "attendance_summaries",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullDayMinHours",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "HalfDayMinHours",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "IncludePaidLeaveInWorkDays",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "StandardHoursPerDay",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "WorkdayCalculationMode",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "PayableWorkHours",
                table: "attendance_summaries");

            migrationBuilder.DropColumn(
                name: "WorkdayCalculationMode",
                table: "attendance_summaries");

            migrationBuilder.DropColumn(
                name: "WorkedMinutes",
                table: "attendance_summaries");
        }
    }
}
