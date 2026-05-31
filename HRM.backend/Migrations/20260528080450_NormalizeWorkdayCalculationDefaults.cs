using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeWorkdayCalculationDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE work_calendar_configs
                SET IncludePaidLeaveInWorkDays = 1
                WHERE StandardHoursPerDay <= 0
                   OR HalfDayMinHours <= 0
                   OR FullDayMinHours <= 0
                   OR WorkdayCalculationMode IS NULL
                   OR WorkdayCalculationMode = ''
                """);

            migrationBuilder.Sql("""
                UPDATE work_calendar_configs
                SET
                    StandardHoursPerDay = CASE WHEN StandardHoursPerDay <= 0 THEN 8 ELSE StandardHoursPerDay END,
                    HalfDayMinHours = CASE WHEN HalfDayMinHours <= 0 THEN 4 ELSE HalfDayMinHours END,
                    FullDayMinHours = CASE WHEN FullDayMinHours <= 0 THEN 8 ELSE FullDayMinHours END,
                    WorkdayCalculationMode = CASE
                        WHEN WorkdayCalculationMode IS NULL OR WorkdayCalculationMode = '' THEN 'Threshold'
                        ELSE WorkdayCalculationMode
                    END
                """);

            migrationBuilder.Sql("""
                UPDATE attendance_summaries
                SET WorkdayCalculationMode = 'Threshold'
                WHERE WorkdayCalculationMode IS NULL OR WorkdayCalculationMode = ''
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
