using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyOutboxAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "idempotency_records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Scope = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdempotencyKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResourceType = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Recipient = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Subject = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Body = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastError = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "UX_work_shifts_DeptId",
                table: "work_shifts",
                column: "DeptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_source_catalogs_SourcePath",
                table: "source_catalogs",
                column: "SourcePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_overtime_requests_EmployeeId_WorkDate_StartTime_EndTime",
                table: "overtime_requests",
                columns: new[] { "EmployeeId", "WorkDate", "StartTime", "EndTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_leave_requests_EmployeeId_LeaveTypeId_StartDate_EndDate",
                table: "leave_requests",
                columns: new[] { "EmployeeId", "LeaveTypeId", "StartDate", "EndDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_configurations_ConfigGroup_ParamKey",
                table: "configurations",
                columns: new[] { "ConfigGroup", "ParamKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_candidates_RecruitmentRequestId_Email",
                table: "candidates",
                columns: new[] { "RecruitmentRequestId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_candidates_TrackingCode",
                table: "candidates",
                column: "TrackingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_records_Scope_IdempotencyKey",
                table: "idempotency_records",
                columns: new[] { "Scope", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_Status_CreatedAt",
                table: "outbox_messages",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_records");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "UX_work_shifts_DeptId",
                table: "work_shifts");

            migrationBuilder.DropIndex(
                name: "IX_source_catalogs_SourcePath",
                table: "source_catalogs");

            migrationBuilder.DropIndex(
                name: "UX_overtime_requests_EmployeeId_WorkDate_StartTime_EndTime",
                table: "overtime_requests");

            migrationBuilder.DropIndex(
                name: "UX_leave_requests_EmployeeId_LeaveTypeId_StartDate_EndDate",
                table: "leave_requests");

            migrationBuilder.DropIndex(
                name: "IX_configurations_ConfigGroup_ParamKey",
                table: "configurations");

            migrationBuilder.DropIndex(
                name: "UX_candidates_RecruitmentRequestId_Email",
                table: "candidates");

            migrationBuilder.DropIndex(
                name: "IX_candidates_TrackingCode",
                table: "candidates");

        }
    }
}
