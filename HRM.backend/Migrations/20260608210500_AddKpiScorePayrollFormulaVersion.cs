using System;
using HRM.backend.src.HRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(MyDbContext))]
    [Migration("20260608210500_AddKpiScorePayrollFormulaVersion")]
    public partial class AddKpiScorePayrollFormulaVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO payroll_formulas
    (FormulaCode, FormulaName, Expression, IsActive, ContractType, PayBasis, EmployeeType, DeptId, PositionId, JobLevelId, Version, EffectiveFrom, EffectiveTo, Status, DeadlineAt, ApprovedByAccountId, ApprovedAt, RejectReason, CreatedAt)
SELECT
    'DEFAULT_PAYROLL_V2',
    'Công thức lương mặc định - KPI theo điểm',
    'gross_income = sum(payroll_formula_lines)',
    TRUE,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    2,
    '2026-06-01 00:00:00',
    NULL,
    'Approved',
    NULL,
    NULL,
    UTC_TIMESTAMP(),
    NULL,
    UTC_TIMESTAMP()
WHERE NOT EXISTS (
    SELECT 1
    FROM payroll_formulas
    WHERE FormulaCode = 'DEFAULT_PAYROLL_V2'
      AND Version = 2
);
");

            migrationBuilder.Sql(@"
INSERT INTO payroll_formula_lines
    (PayrollFormulaId, SalaryComponentTypeId, ComponentCode, Expression, CalculationOrder, IsGrossComponent, IsTaxable, IsInsuranceBased, IsDeduction, IsSnapshotRequired, Note, CreatedAt)
SELECT
    pf.Id,
    NULL,
    formula_lines.ComponentCode,
    formula_lines.Expression,
    formula_lines.CalculationOrder,
    formula_lines.IsGrossComponent,
    formula_lines.IsTaxable,
    formula_lines.IsInsuranceBased,
    formula_lines.IsDeduction,
    TRUE,
    formula_lines.Note,
    UTC_TIMESTAMP()
FROM payroll_formulas pf
CROSS JOIN (
    SELECT 'BASE_SALARY_ACTUAL' AS ComponentCode, 'contract_segment_salary_amount' AS Expression, 10 AS CalculationOrder, TRUE AS IsGrossComponent, TRUE AS IsTaxable, TRUE AS IsInsuranceBased, FALSE AS IsDeduction, NULL AS Note
    UNION ALL SELECT 'POSITION_ALLOWANCE', 'position_allowance / standard_workdays * actual_workdays', 20, TRUE, TRUE, TRUE, FALSE, NULL
    UNION ALL SELECT 'RESPONSIBILITY_ALLOWANCE', 'responsibility_allowance / standard_workdays * actual_workdays', 30, TRUE, TRUE, TRUE, FALSE, NULL
    UNION ALL SELECT 'SENIORITY_ALLOWANCE', 'seniority_allowance_prorated', 35, TRUE, TRUE, TRUE, FALSE, NULL
    UNION ALL SELECT 'MEAL_ALLOWANCE', 'meal_allowance_per_day * actual_attendance_days', 40, TRUE, FALSE, FALSE, FALSE, NULL
    UNION ALL SELECT 'LEGACY_INSURANCE_ALLOWANCE', 'legacy_insurance_allowance', 50, TRUE, TRUE, TRUE, FALSE, NULL
    UNION ALL SELECT 'LEGACY_TAXABLE_ALLOWANCE', 'legacy_taxable_allowance', 60, TRUE, TRUE, FALSE, FALSE, NULL
    UNION ALL SELECT 'LEGACY_NONTAXABLE_ALLOWANCE', 'legacy_nontaxable_allowance', 70, TRUE, FALSE, FALSE, FALSE, NULL
    UNION ALL SELECT 'KPI_BONUS', 'kpi_bonus_amount * kpi_score / 100', 80, TRUE, TRUE, FALSE, FALSE, 'Khoản thưởng KPI thực nhận = mức thưởng KPI tối đa * điểm KPI / 100.'
    UNION ALL SELECT 'OT_BASE', 'overtime_base_amount', 90, TRUE, TRUE, FALSE, FALSE, NULL
    UNION ALL SELECT 'OT_PREMIUM', 'overtime_premium_amount', 100, TRUE, FALSE, FALSE, FALSE, NULL
    UNION ALL SELECT 'PAYROLL_ADJUSTMENT_TAXABLE_INSURANCE', 'payroll_adjustment_taxable_insurance', 110, TRUE, TRUE, TRUE, FALSE, NULL
    UNION ALL SELECT 'PAYROLL_ADJUSTMENT_TAXABLE', 'payroll_adjustment_taxable', 120, TRUE, TRUE, FALSE, FALSE, NULL
    UNION ALL SELECT 'PAYROLL_ADJUSTMENT_NONTAXABLE', 'payroll_adjustment_nontaxable', 130, TRUE, FALSE, FALSE, FALSE, NULL
    UNION ALL SELECT 'EMPLOYEE_INSURANCE', 'insurance_salary * employee_insurance_rate', 200, FALSE, FALSE, FALSE, TRUE, NULL
    UNION ALL SELECT 'PIT', 'pit(pit_tax_base)', 210, FALSE, FALSE, FALSE, TRUE, NULL
    UNION ALL SELECT 'PAYROLL_ADJUSTMENT_DEDUCTION', 'payroll_adjustment_deduction', 220, FALSE, FALSE, FALSE, TRUE, NULL
) AS formula_lines
WHERE pf.FormulaCode = 'DEFAULT_PAYROLL_V2'
  AND pf.Version = 2
  AND pf.FormulaName = 'Công thức lương mặc định - KPI theo điểm'
  AND NOT EXISTS (
      SELECT 1
      FROM payroll_formula_lines existing
      WHERE existing.PayrollFormulaId = pf.Id
        AND existing.ComponentCode = formula_lines.ComponentCode
  );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE lines
FROM payroll_formula_lines lines
JOIN payroll_formulas formulas ON formulas.Id = lines.PayrollFormulaId
WHERE formulas.FormulaCode = 'DEFAULT_PAYROLL_V2'
  AND formulas.Version = 2
  AND formulas.FormulaName = 'Công thức lương mặc định - KPI theo điểm';
");

            migrationBuilder.Sql(@"
DELETE FROM payroll_formulas
WHERE FormulaCode = 'DEFAULT_PAYROLL_V2'
  AND Version = 2
  AND FormulaName = 'Công thức lương mặc định - KPI theo điểm';
");
        }
    }
}
