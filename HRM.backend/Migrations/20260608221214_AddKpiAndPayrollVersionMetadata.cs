using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddKpiAndPayrollVersionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScoringVersion",
                table: "performance_reviews",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VersionCode",
                table: "payroll_formulas",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "Mức thưởng KPI tối đa");

            migrationBuilder.Sql(@"
UPDATE performance_reviews
SET ScoringVersion = 'Legacy'
WHERE ScoringVersion IS NULL
  AND Status IN ('Evaluated', 'AutoEvaluated', 'Approved');
");

            migrationBuilder.Sql(@"
UPDATE performance_reviews
SET ScoringVersion = 'WeightedV2'
WHERE ScoringVersion IS NULL
  AND Status NOT IN ('Evaluated', 'AutoEvaluated', 'Approved');
");

            migrationBuilder.Sql(@"
UPDATE payroll_formulas pf
SET VersionCode = CASE
    WHEN pf.FormulaCode = 'DEFAULT_PAYROLL_V2'
      OR EXISTS (
          SELECT 1
          FROM payroll_formula_lines pfl
          WHERE pfl.PayrollFormulaId = pf.Id
            AND pfl.ComponentCode = 'KPI_BONUS'
            AND pfl.Expression LIKE '%kpi_score%'
      )
    THEN 'KPI_PAYOUT_V2'
    ELSE 'LEGACY_KPI_TARGET_V1'
END
WHERE pf.VersionCode IS NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScoringVersion",
                table: "performance_reviews");

            migrationBuilder.DropColumn(
                name: "VersionCode",
                table: "payroll_formulas");

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "Thưởng KPI");
        }
    }
}
