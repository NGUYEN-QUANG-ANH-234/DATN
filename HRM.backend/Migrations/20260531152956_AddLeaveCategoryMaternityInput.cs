using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveCategoryMaternityInput : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AffectsKpiPenalty",
                table: "leave_types",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "leave_types",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "AnnualPaid")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "CountsAsUnpaidForInsurance",
                table: "leave_types",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CountsAsWorkday",
                table: "leave_types",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeductAnnualLeave",
                table: "leave_types",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("""
                UPDATE leave_types
                SET Category = 'AnnualPaid',
                    IsPaid = 1,
                    CountsAsUnpaidForInsurance = 0,
                    CountsAsWorkday = 1,
                    DeductAnnualLeave = 1,
                    AffectsKpiPenalty = 0
                WHERE TypeName = 'Phép năm';

                UPDATE leave_types
                SET Category = 'Sick',
                    IsPaid = 1,
                    CountsAsUnpaidForInsurance = 0,
                    CountsAsWorkday = 0,
                    DeductAnnualLeave = 0,
                    AffectsKpiPenalty = 0
                WHERE TypeName = 'Nghỉ ốm';

                UPDATE leave_types
                SET Category = 'Unpaid',
                    IsPaid = 0,
                    CountsAsUnpaidForInsurance = 1,
                    CountsAsWorkday = 0,
                    DeductAnnualLeave = 0,
                    AffectsKpiPenalty = 0
                WHERE TypeName = 'Nghỉ không lương';

                INSERT INTO leave_types
                    (TypeName, IsPaid, Category, CountsAsUnpaidForInsurance, CountsAsWorkday, DeductAnnualLeave, AffectsKpiPenalty)
                SELECT
                    'Nghỉ thai sản', 0, 'Maternity', 0, 0, 0, 0
                WHERE NOT EXISTS (
                    SELECT 1 FROM leave_types WHERE TypeName = 'Nghỉ thai sản'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM leave_types WHERE TypeName = 'Nghỉ thai sản';");

            migrationBuilder.DropColumn(
                name: "AffectsKpiPenalty",
                table: "leave_types");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "leave_types");

            migrationBuilder.DropColumn(
                name: "CountsAsUnpaidForInsurance",
                table: "leave_types");

            migrationBuilder.DropColumn(
                name: "CountsAsWorkday",
                table: "leave_types");

            migrationBuilder.DropColumn(
                name: "DeductAnnualLeave",
                table: "leave_types");
        }
    }
}
