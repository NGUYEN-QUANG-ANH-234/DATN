using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkCalendarConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "work_calendar_configs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DeptId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    StandardWorkDays = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    WorkingDaysOfWeek = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HolidayDatesJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsLocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedByAccountId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_calendar_configs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_calendar_configs_departments_DeptId",
                        column: x => x.DeptId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "UX_work_calendar_configs_DeptId_Month_Year",
                table: "work_calendar_configs",
                columns: new[] { "DeptId", "Month", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_calendar_configs");
        }
    }
}
