using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDepartment_V1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeptId",
                table: "work_shifts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_shifts_DeptId",
                table: "work_shifts",
                column: "DeptId");

            migrationBuilder.AddForeignKey(
                name: "FK_work_shifts_departments_DeptId",
                table: "work_shifts",
                column: "DeptId",
                principalTable: "departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_work_shifts_departments_DeptId",
                table: "work_shifts");

            migrationBuilder.DropIndex(
                name: "IX_work_shifts_DeptId",
                table: "work_shifts");

            migrationBuilder.DropColumn(
                name: "DeptId",
                table: "work_shifts");
        }
    }
}
