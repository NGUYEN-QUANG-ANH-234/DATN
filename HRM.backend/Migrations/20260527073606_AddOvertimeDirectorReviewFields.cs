using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddOvertimeDirectorReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DirectorNote",
                table: "overtime_requests",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "DirectorReviewedAt",
                table: "overtime_requests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DirectorReviewerAccountId",
                table: "overtime_requests",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DirectorNote",
                table: "overtime_requests");

            migrationBuilder.DropColumn(
                name: "DirectorReviewedAt",
                table: "overtime_requests");

            migrationBuilder.DropColumn(
                name: "DirectorReviewerAccountId",
                table: "overtime_requests");
        }
    }
}
