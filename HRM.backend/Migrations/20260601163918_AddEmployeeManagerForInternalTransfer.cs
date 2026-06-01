using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeManagerForInternalTransfer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ManagerId",
                table: "employees",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_ManagerId",
                table: "employees",
                column: "ManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_employees_employees_ManagerId",
                table: "employees",
                column: "ManagerId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employees_employees_ManagerId",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employees_ManagerId",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "employees");
        }
    }
}
