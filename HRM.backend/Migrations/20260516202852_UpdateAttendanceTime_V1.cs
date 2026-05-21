using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAttendanceTime_V1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "BreakEndTime",
                table: "work_shifts",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "BreakStartTime",
                table: "work_shifts",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EarlyLeaveThresholdMins",
                table: "work_shifts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "work_shifts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "positions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BreakEndTime",
                table: "work_shifts");

            migrationBuilder.DropColumn(
                name: "BreakStartTime",
                table: "work_shifts");

            migrationBuilder.DropColumn(
                name: "EarlyLeaveThresholdMins",
                table: "work_shifts");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "work_shifts");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "positions");
        }
    }
}
