using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using HRM.backend.src.HRM.Infrastructure.Persistence;

#nullable disable

namespace HRM.backend.Migrations
{
    [DbContext(typeof(MyDbContext))]
    [Migration("20260526093000_AddKpiPenaltyFields")]
    public partial class AddKpiPenaltyFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PenaltyPoint",
                table: "performance_details",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PenaltyReason",
                table: "performance_details",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PenaltyPoint",
                table: "performance_details");

            migrationBuilder.DropColumn(
                name: "PenaltyReason",
                table: "performance_details");
        }
    }
}
