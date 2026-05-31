using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDependentUpdateRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dependents_employees_EmployeeId",
                table: "dependents");

            migrationBuilder.DropIndex(
                name: "IX_dependents_EmployeeId",
                table: "dependents");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "dependents",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "EvidenceUrl",
                table: "dependents",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "dependents",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "dependents",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "dependent_update_requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DependentId = table.Column<int>(type: "int", nullable: true),
                    ActionType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestedDataJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EvidenceUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RejectReason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReviewerAccountId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dependent_update_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dependent_update_requests_dependents_DependentId",
                        column: x => x.DependentId,
                        principalTable: "dependents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_dependent_update_requests_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_dependents_EmployeeId_TaxDependentCode",
                table: "dependents",
                columns: new[] { "EmployeeId", "TaxDependentCode" });

            migrationBuilder.CreateIndex(
                name: "IX_dependent_update_requests_DependentId",
                table: "dependent_update_requests",
                column: "DependentId");

            migrationBuilder.CreateIndex(
                name: "IX_dependent_update_requests_EmployeeId_Status",
                table: "dependent_update_requests",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_dependents_employees_EmployeeId",
                table: "dependents",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dependents_employees_EmployeeId",
                table: "dependents");

            migrationBuilder.DropTable(
                name: "dependent_update_requests");

            migrationBuilder.DropIndex(
                name: "IX_dependents_EmployeeId_TaxDependentCode",
                table: "dependents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "dependents");

            migrationBuilder.DropColumn(
                name: "EvidenceUrl",
                table: "dependents");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "dependents");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "dependents");

            migrationBuilder.CreateIndex(
                name: "IX_dependents_EmployeeId",
                table: "dependents",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_dependents_employees_EmployeeId",
                table: "dependents",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id");
        }
    }
}
