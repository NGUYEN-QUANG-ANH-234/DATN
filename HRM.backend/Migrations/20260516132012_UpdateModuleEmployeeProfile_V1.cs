using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModuleEmployeeProfile_V1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "employees",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IdentityBackUrl",
                table: "employees",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IdentityFrontUrl",
                table: "employees",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "DirectorDeadline",
                table: "contracts",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmployeeDeadline",
                table: "contracts",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NegotiationNote",
                table: "contracts",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "contracts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "IdentityBackUrl",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "IdentityFrontUrl",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "DirectorDeadline",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployeeDeadline",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "NegotiationNote",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "contracts");
        }
    }
}
