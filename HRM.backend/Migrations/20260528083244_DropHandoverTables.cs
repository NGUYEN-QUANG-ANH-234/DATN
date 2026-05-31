using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class DropHandoverTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "handover_items");

            migrationBuilder.DropTable(
                name: "handover_requests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "handover_requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReceiverId = table.Column<int>(type: "int", nullable: true),
                    RequestId = table.Column<int>(type: "int", nullable: true),
                    SenderId = table.Column<int>(type: "int", nullable: true),
                    DeadlineAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_handover_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_handover_requests_employees_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_handover_requests_employees_SenderId",
                        column: x => x.SenderId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_handover_requests_requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "requests",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "handover_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    HandoverRequestId = table.Column<int>(type: "int", nullable: true),
                    TaskId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ItemName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_handover_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_handover_items_handover_requests_HandoverRequestId",
                        column: x => x.HandoverRequestId,
                        principalTable: "handover_requests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_handover_items_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "tasks",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_handover_items_HandoverRequestId",
                table: "handover_items",
                column: "HandoverRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_handover_items_TaskId",
                table: "handover_items",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_handover_requests_ReceiverId",
                table: "handover_requests",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_handover_requests_RequestId",
                table: "handover_requests",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_handover_requests_SenderId",
                table: "handover_requests",
                column: "SenderId");
        }
    }
}
