using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OldValue",
                table: "audit_logs",
                newName: "OldValues");

            migrationBuilder.RenameColumn(
                name: "NewValue",
                table: "audit_logs",
                newName: "NewValues");

            migrationBuilder.AddColumn<string>(
                name: "AffectedColumns",
                table: "audit_logs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AffectedColumns",
                table: "audit_logs");

            migrationBuilder.RenameColumn(
                name: "OldValues",
                table: "audit_logs",
                newName: "OldValue");

            migrationBuilder.RenameColumn(
                name: "NewValues",
                table: "audit_logs",
                newName: "NewValue");
        }
    }
}
