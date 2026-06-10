using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollLegalPolicyVersions2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "insurance_configs",
                columns: new[] { "Id", "ActivatedAt", "Code", "CreatedAt", "CreatedByAccountId", "EffectiveFrom", "EffectiveTo", "HealthInsuranceEmployeeRate", "HealthInsuranceEmployerRate", "IsActive", "LockedAfterUsed", "MaxInsuranceSalary", "MinContractMonthsForContribution", "MinInsuranceSalary", "Name", "Note", "SocialInsuranceEmployeeRate", "SocialInsuranceEmployerRate", "SourceRef", "Status", "SupersedesVersionId", "UnemploymentInsuranceEmployeeRate", "UnemploymentInsuranceEmployerRate", "UnionFeeEmployerRate", "UnpaidLeaveNoContributionThresholdDays", "Version", "VersionCode" },
                values: new object[] { 202601, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "VN_STANDARD_INSURANCE_2026", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 0.015m, 0.03m, true, false, null, 1, null, "Cấu hình bảo hiểm Việt Nam 2026", "Insurance 2026 version. Minimum wage region policies are tracked separately for cap review.", 0.08m, 0.175m, "Vietnam insurance policy 2026", "Active", 1, 0.01m, 0.01m, 0.02m, 14, 2, "VN_INSURANCE_2026" });

            migrationBuilder.InsertData(
                table: "payroll_policies",
                columns: new[] { "Id", "ActivatedAt", "Amount", "Code", "CreatedAt", "CreatedByAccountId", "Description", "EffectiveFrom", "EffectiveTo", "FormulaJson", "FromAmount", "IsActive", "LockedAfterUsed", "Name", "PolicyType", "QuickDeduction", "RatePercent", "SourceRef", "Status", "SupersedesVersionId", "ToAmount", "UpdatedAt", "UpdatedByAccountId", "ValueType", "Version", "VersionCode" },
                values: new object[,]
                {
                    { 20260101, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 5310000m, "VN_MIN_WAGE_REGION_1_2026", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Theo dõi lương tối thiểu vùng để đối chiếu trần/sàn chính sách bảo hiểm và lương.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "Lương tối thiểu vùng I 2026", "MinimumWage", null, null, "Vietnam regional minimum wage 2026", "Active", null, null, null, null, "Amount", 1, "VN_MIN_WAGE_2026" },
                    { 20260102, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 4730000m, "VN_MIN_WAGE_REGION_2_2026", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Theo dõi lương tối thiểu vùng để đối chiếu trần/sàn chính sách bảo hiểm và lương.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "Lương tối thiểu vùng II 2026", "MinimumWage", null, null, "Vietnam regional minimum wage 2026", "Active", null, null, null, null, "Amount", 1, "VN_MIN_WAGE_2026" },
                    { 20260103, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 4140000m, "VN_MIN_WAGE_REGION_3_2026", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Theo dõi lương tối thiểu vùng để đối chiếu trần/sàn chính sách bảo hiểm và lương.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "Lương tối thiểu vùng III 2026", "MinimumWage", null, null, "Vietnam regional minimum wage 2026", "Active", null, null, null, null, "Amount", 1, "VN_MIN_WAGE_2026" },
                    { 20260104, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 3700000m, "VN_MIN_WAGE_REGION_4_2026", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Theo dõi lương tối thiểu vùng để đối chiếu trần/sàn chính sách bảo hiểm và lương.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "Lương tối thiểu vùng IV 2026", "MinimumWage", null, null, "Vietnam regional minimum wage 2026", "Active", null, null, null, null, "Amount", 1, "VN_MIN_WAGE_2026" }
                });

            migrationBuilder.InsertData(
                table: "pit_tax_brackets",
                columns: new[] { "Id", "ActivatedAt", "Code", "CreatedAt", "CreatedByAccountId", "EffectiveFrom", "EffectiveTo", "IsActive", "Level", "LockedAfterUsed", "MaxIncome", "MinIncome", "Note", "QuickDeduction", "SourceRef", "Status", "SupersedesVersionId", "TaxRate", "Version", "VersionCode" },
                values: new object[,]
                {
                    { 20260101, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "VN_PROGRESSIVE_PIT_2026", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 1, false, 5000000m, 0m, null, 0m, "Vietnam PIT progressive brackets 2026", "Active", 1, 0.05m, 2, "VN_PIT_BRACKET_2026" },
                    { 20260102, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "VN_PROGRESSIVE_PIT_2026", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 2, false, 10000000m, 5000000m, null, 250000m, "Vietnam PIT progressive brackets 2026", "Active", 2, 0.10m, 2, "VN_PIT_BRACKET_2026" },
                    { 20260103, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "VN_PROGRESSIVE_PIT_2026", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 3, false, 18000000m, 10000000m, null, 750000m, "Vietnam PIT progressive brackets 2026", "Active", 3, 0.15m, 2, "VN_PIT_BRACKET_2026" },
                    { 20260104, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "VN_PROGRESSIVE_PIT_2026", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 4, false, 32000000m, 18000000m, null, 1650000m, "Vietnam PIT progressive brackets 2026", "Active", 4, 0.20m, 2, "VN_PIT_BRACKET_2026" },
                    { 20260105, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "VN_PROGRESSIVE_PIT_2026", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 5, false, 52000000m, 32000000m, null, 3250000m, "Vietnam PIT progressive brackets 2026", "Active", 5, 0.25m, 2, "VN_PIT_BRACKET_2026" },
                    { 20260106, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "VN_PROGRESSIVE_PIT_2026", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 6, false, 80000000m, 52000000m, null, 5850000m, "Vietnam PIT progressive brackets 2026", "Active", 6, 0.30m, 2, "VN_PIT_BRACKET_2026" },
                    { 20260107, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "VN_PROGRESSIVE_PIT_2026", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 7, false, null, 80000000m, null, 9850000m, "Vietnam PIT progressive brackets 2026", "Active", 7, 0.35m, 2, "VN_PIT_BRACKET_2026" }
                });

            migrationBuilder.InsertData(
                table: "tax_configs",
                columns: new[] { "Id", "ActivatedAt", "Code", "CreatedAt", "CreatedByAccountId", "DependentDeduction", "EffectiveFrom", "EffectiveTo", "FlatTaxRate", "FlatTaxThreshold", "IsActive", "LockedAfterUsed", "Name", "NonResidentTaxRate", "Note", "PersonalDeduction", "SourceRef", "Status", "SupersedesVersionId", "Version", "VersionCode" },
                values: new object[] { 202601, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "VN_PERSONAL_INCOME_TAX_2026", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 6200000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 0.10m, 2000000m, true, false, "Cấu hình thuế TNCN Việt Nam 2026", 0.20m, "PIT 2026 version. Keeps historical 2020 config available for older payroll periods.", 15500000m, "Vietnam PIT family deduction policy 2026", "Active", 1, 2, "VN_PIT_2026" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "insurance_configs",
                keyColumn: "Id",
                keyValue: 202601);

            migrationBuilder.DeleteData(
                table: "payroll_policies",
                keyColumn: "Id",
                keyValue: 20260101);

            migrationBuilder.DeleteData(
                table: "payroll_policies",
                keyColumn: "Id",
                keyValue: 20260102);

            migrationBuilder.DeleteData(
                table: "payroll_policies",
                keyColumn: "Id",
                keyValue: 20260103);

            migrationBuilder.DeleteData(
                table: "payroll_policies",
                keyColumn: "Id",
                keyValue: 20260104);

            migrationBuilder.DeleteData(
                table: "pit_tax_brackets",
                keyColumn: "Id",
                keyValue: 20260101);

            migrationBuilder.DeleteData(
                table: "pit_tax_brackets",
                keyColumn: "Id",
                keyValue: 20260102);

            migrationBuilder.DeleteData(
                table: "pit_tax_brackets",
                keyColumn: "Id",
                keyValue: 20260103);

            migrationBuilder.DeleteData(
                table: "pit_tax_brackets",
                keyColumn: "Id",
                keyValue: 20260104);

            migrationBuilder.DeleteData(
                table: "pit_tax_brackets",
                keyColumn: "Id",
                keyValue: 20260105);

            migrationBuilder.DeleteData(
                table: "pit_tax_brackets",
                keyColumn: "Id",
                keyValue: 20260106);

            migrationBuilder.DeleteData(
                table: "pit_tax_brackets",
                keyColumn: "Id",
                keyValue: 20260107);

            migrationBuilder.DeleteData(
                table: "tax_configs",
                keyColumn: "Id",
                keyValue: 202601);
        }
    }
}
