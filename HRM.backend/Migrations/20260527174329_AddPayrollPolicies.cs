using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payroll_policies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PolicyType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValueType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RatePercent = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    Amount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    FromAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    ToAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    QuickDeduction = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    FormulaJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedByAccountId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_policies", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_policies_Type_Active_EffectiveFrom",
                table: "payroll_policies",
                columns: new[] { "PolicyType", "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "UX_payroll_policies_Type_Code_EffectiveFrom",
                table: "payroll_policies",
                columns: new[] { "PolicyType", "Code", "EffectiveFrom" },
                unique: true);

            var effectiveFrom = new DateTime(2026, 1, 1);
            var createdAt = new DateTime(2026, 5, 27, 17, 43, 29);

            migrationBuilder.InsertData(
                table: "payroll_policies",
                columns: new[]
                {
                    "PolicyType", "Code", "Name", "ValueType", "RatePercent", "Amount",
                    "FromAmount", "ToAmount", "QuickDeduction", "FormulaJson", "EffectiveFrom",
                    "EffectiveTo", "Version", "IsActive", "Description", "CreatedAt"
                },
                values: new object[,]
                {
                    { "Overtime", "OT_NORMAL", "OT ngay lam viec", "RatePercent", 150m, null, null, null, null, null, effectiveFrom, null, 1, true, "He so OT mac dinh cho ngay lam viec.", createdAt },
                    { "Overtime", "OT_WEEKEND", "OT ngay nghi cuoi tuan", "RatePercent", 200m, null, null, null, null, null, effectiveFrom, null, 1, true, "He so OT mac dinh cho ngay nghi hang tuan.", createdAt },
                    { "Overtime", "OT_HOLIDAY", "OT ngay le", "RatePercent", 300m, null, null, null, null, null, effectiveFrom, null, 1, true, "He so OT mac dinh cho ngay le, tet.", createdAt },
                    { "Overtime", "OT_NIGHT", "OT ban dem", "RatePercent", 130m, null, null, null, null, null, effectiveFrom, null, 1, true, "Ty le bo sung cho lam viec ban dem.", createdAt },
                    { "Insurance", "INS_EMP_SOCIAL", "BHXH nguoi lao dong", "RatePercent", 8m, null, null, null, null, null, effectiveFrom, null, 1, true, "Ty le BHXH nguoi lao dong dong.", createdAt },
                    { "Insurance", "INS_EMP_HEALTH", "BHYT nguoi lao dong", "RatePercent", 1.5m, null, null, null, null, null, effectiveFrom, null, 1, true, "Ty le BHYT nguoi lao dong dong.", createdAt },
                    { "Insurance", "INS_EMP_UNEMPLOYMENT", "BHTN nguoi lao dong", "RatePercent", 1m, null, null, null, null, null, effectiveFrom, null, 1, true, "Ty le BHTN nguoi lao dong dong.", createdAt },
                    { "PitTax", "PIT_STANDARD_DEDUCTION", "Giam tru ban than", "Amount", null, 11000000m, null, null, null, null, effectiveFrom, null, 1, true, "Muc giam tru gia canh cho ban than.", createdAt },
                    { "PitTax", "PIT_DEPENDENT_DEDUCTION", "Giam tru nguoi phu thuoc", "Amount", null, 4400000m, null, null, null, null, effectiveFrom, null, 1, true, "Muc giam tru cho moi nguoi phu thuoc.", createdAt },
                    { "PitTax", "PIT_BRACKET_1", "Bac thue 1", "Bracket", 5m, null, 0m, 5000000m, 0m, null, effectiveFrom, null, 1, true, "Bac thue TNCN luy tien 1.", createdAt },
                    { "PitTax", "PIT_BRACKET_2", "Bac thue 2", "Bracket", 10m, null, 5000000m, 10000000m, 250000m, null, effectiveFrom, null, 1, true, "Bac thue TNCN luy tien 2.", createdAt },
                    { "PitTax", "PIT_BRACKET_3", "Bac thue 3", "Bracket", 15m, null, 10000000m, 18000000m, 750000m, null, effectiveFrom, null, 1, true, "Bac thue TNCN luy tien 3.", createdAt },
                    { "PitTax", "PIT_BRACKET_4", "Bac thue 4", "Bracket", 20m, null, 18000000m, 32000000m, 1650000m, null, effectiveFrom, null, 1, true, "Bac thue TNCN luy tien 4.", createdAt },
                    { "PitTax", "PIT_BRACKET_5", "Bac thue 5", "Bracket", 25m, null, 32000000m, 52000000m, 3250000m, null, effectiveFrom, null, 1, true, "Bac thue TNCN luy tien 5.", createdAt },
                    { "PitTax", "PIT_BRACKET_6", "Bac thue 6", "Bracket", 30m, null, 52000000m, 80000000m, 5850000m, null, effectiveFrom, null, 1, true, "Bac thue TNCN luy tien 6.", createdAt },
                    { "PitTax", "PIT_BRACKET_7", "Bac thue 7", "Bracket", 35m, null, 80000000m, null, 9850000m, null, effectiveFrom, null, 1, true, "Bac thue TNCN luy tien 7.", createdAt }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payroll_policies");
        }
    }
}
