using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddInternAllowancePayrollComponent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "payroll_policies",
                columns: new[] { "Id", "ActivatedAt", "Amount", "Code", "CreatedAt", "CreatedByAccountId", "Description", "EffectiveFrom", "EffectiveTo", "FormulaJson", "FromAmount", "IsActive", "LockedAfterUsed", "Name", "PolicyType", "QuickDeduction", "RatePercent", "SourceRef", "Status", "SupersedesVersionId", "ToAmount", "UpdatedAt", "UpdatedByAccountId", "ValueType", "Version", "VersionCode" },
                values: new object[] { 20260610, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 2000000m, "HICAS_INTERN_ALLOWANCE_2026", new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Mức trợ cấp tối thiểu cho thực tập sinh. HR có thể override bằng thành phần lương INTERN_ALLOWANCE theo từng hồ sơ.", new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "Trợ cấp thực tập tối thiểu HICAS 2026", "Allowance", null, null, "HICAS internship allowance policy 2026", "Active", null, null, null, null, "Amount", 1, "HICAS_INTERN_ALLOWANCE_2026_V1" });

            migrationBuilder.InsertData(
                table: "salary_component_types",
                columns: new[] { "Id", "CalculationMethod", "Code", "ComponentGroup", "CreatedAt", "EffectiveFrom", "EffectiveTo", "IsActive", "IsAllowance", "IsBonus", "IsDeduction", "IsFixed", "IsIncome", "IsInsuranceBased", "IsOvertime", "IsTaxable", "Name", "Note", "ProrationType", "Status", "TaxExemptCap", "Version", "VersionCode" },
                values: new object[] { 16, "FixedAmount", "INTERN_ALLOWANCE", "Allowance", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, true, false, false, true, true, false, false, true, "Trợ cấp thực tập", "Default minimum internship allowance. Override per intern through employee salary components when needed.", "None", "Active", null, 1, "INTERN_ALLOWANCE_V1" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "payroll_policies",
                keyColumn: "Id",
                keyValue: 20260610);

            migrationBuilder.DeleteData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 16);
        }
    }
}
