using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddOvertimeReconciliationSegments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndAt",
                table: "overtime_requests",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartAt",
                table: "overtime_requests",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "WorkDate",
                table: "attendance_logs",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql("""
                UPDATE attendance_logs
                SET WorkDate = DATE(CheckIn)
                WHERE CheckIn IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE overtime_requests
                SET
                    StartAt = CAST(CONCAT(DATE(WorkDate), ' ', StartTime) AS DATETIME(6)),
                    EndAt = CASE
                        WHEN EndTime < StartTime
                            THEN CAST(CONCAT(DATE_ADD(DATE(WorkDate), INTERVAL 1 DAY), ' ', EndTime) AS DATETIME(6))
                        ELSE CAST(CONCAT(DATE(WorkDate), ' ', EndTime) AS DATETIME(6))
                    END;
                """);

            migrationBuilder.CreateTable(
                name: "overtime_segments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OvertimeRequestId = table.Column<int>(type: "int", nullable: false),
                    SegmentStartAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SegmentEndAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Minutes = table.Column<int>(type: "int", nullable: false),
                    PolicyCode = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RateMultiplierSnapshot = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    PolicySnapshotJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_overtime_segments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_overtime_segments_overtime_requests_OvertimeRequestId",
                        column: x => x.OvertimeRequestId,
                        principalTable: "overtime_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_overtime_requests_EmployeeId_StartAt_EndAt",
                table: "overtime_requests",
                columns: new[] { "EmployeeId", "StartAt", "EndAt" });

            migrationBuilder.CreateIndex(
                name: "IX_overtime_segments_Request_Start",
                table: "overtime_segments",
                columns: new[] { "OvertimeRequestId", "SegmentStartAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "overtime_segments");

            migrationBuilder.DropIndex(
                name: "IX_overtime_requests_EmployeeId_StartAt_EndAt",
                table: "overtime_requests");

            migrationBuilder.DropColumn(
                name: "EndAt",
                table: "overtime_requests");

            migrationBuilder.DropColumn(
                name: "StartAt",
                table: "overtime_requests");

            migrationBuilder.DropColumn(
                name: "WorkDate",
                table: "attendance_logs");
        }
    }
}
