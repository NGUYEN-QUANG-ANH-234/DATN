using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using HRM.backend.src.HRM.Infrastructure.Persistence;

#nullable disable

namespace HRM.backend.Migrations
{
    [DbContext(typeof(MyDbContext))]
    [Migration("20260526110000_AddKpiPenaltyFoundation")]
    public partial class AddKpiPenaltyFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SystemPenaltyPoint",
                table: "performance_details",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SystemPenaltyReason",
                table: "performance_details",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "ManualPenaltyPoint",
                table: "performance_details",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ManualPenaltyReason",
                table: "performance_details",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "penalty_rules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SourceType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RuleCode = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RuleName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThresholdValue = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    ThresholdUnit = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PenaltyPoint = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_penalty_rules", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "penalty_records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    RuleCode = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PenaltyPoint = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedBySystem = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: true),
                    PerformanceReviewId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_penalty_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_penalty_records_accounts_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_penalty_records_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_penalty_records_performance_reviews_PerformanceReviewId",
                        column: x => x.PerformanceReviewId,
                        principalTable: "performance_reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "UX_penalty_rules_SourceType_RuleCode",
                table: "penalty_rules",
                columns: new[] { "SourceType", "RuleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_penalty_records_CreatedByAccountId",
                table: "penalty_records",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_penalty_records_EmployeeId_Period_SourceType",
                table: "penalty_records",
                columns: new[] { "EmployeeId", "Period", "SourceType" });

            migrationBuilder.CreateIndex(
                name: "IX_penalty_records_PerformanceReviewId",
                table: "penalty_records",
                column: "PerformanceReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_penalty_records_Source_Reference_Rule",
                table: "penalty_records",
                columns: new[] { "SourceType", "ReferenceId", "RuleCode" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "penalty_records");
            migrationBuilder.DropTable(name: "penalty_rules");

            migrationBuilder.DropColumn(name: "SystemPenaltyPoint", table: "performance_details");
            migrationBuilder.DropColumn(name: "SystemPenaltyReason", table: "performance_details");
            migrationBuilder.DropColumn(name: "ManualPenaltyPoint", table: "performance_details");
            migrationBuilder.DropColumn(name: "ManualPenaltyReason", table: "performance_details");
        }
    }
}
