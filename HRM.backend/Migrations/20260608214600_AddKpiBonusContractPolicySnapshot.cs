using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddKpiBonusContractPolicySnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddOrModifyContractLegalSnapshotColumn(migrationBuilder, "KpiBonusApproverRole", "text CHARACTER SET utf8mb4");
            AddOrModifyContractLegalSnapshotColumn(migrationBuilder, "KpiBonusEligibilityRule", "text CHARACTER SET utf8mb4");
            AddOrModifyContractLegalSnapshotColumn(migrationBuilder, "KpiBonusPaymentPeriod", "text CHARACTER SET utf8mb4");
            AddOrModifyContractLegalSnapshotColumn(migrationBuilder, "KpiBonusPolicyCode", "varchar(80) CHARACTER SET utf8mb4");
            AddOrModifyContractLegalSnapshotColumn(migrationBuilder, "KpiBonusPolicyVersionCode", "varchar(80) CHARACTER SET utf8mb4");
            AddOrModifyContractLegalSnapshotColumn(migrationBuilder, "KpiBonusTargetAmount", "DECIMAL(15,2)");
            AddOrModifyContractLegalSnapshotColumn(migrationBuilder, "KpiPayoutFormula", "text CHARACTER SET utf8mb4");
            AddOrModifyContractLegalSnapshotColumn(migrationBuilder, "KpiScoreFormula", "text CHARACTER SET utf8mb4");

            migrationBuilder.Sql("DELETE FROM payroll_policies WHERE Id = 20260601;");

            migrationBuilder.InsertData(
                table: "payroll_policies",
                columns: new[] { "Id", "ActivatedAt", "Amount", "Code", "CreatedAt", "CreatedByAccountId", "Description", "EffectiveFrom", "EffectiveTo", "FormulaJson", "FromAmount", "IsActive", "LockedAfterUsed", "Name", "PolicyType", "QuickDeduction", "RatePercent", "SourceRef", "Status", "SupersedesVersionId", "ToAmount", "UpdatedAt", "UpdatedByAccountId", "ValueType", "Version", "VersionCode" },
                values: new object[] { 20260601, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "HICAS_KPI_BONUS_2026", new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Lưu quy chế thưởng KPI theo version. Hợp đồng chỉ viện dẫn nguyên tắc; thay đổi công thức tạo version mới, không cần ký lại phụ lục từng lần.", new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "{\"kpiBonusTargetSource\":\"EmployeeSalaryComponent.KPI_BONUS\",\"scoreFormula\":\"Điểm KPI chính thức = tổng max(0, trọng số KPI * điểm trưởng phòng / 100 - điểm trừ).\",\"payoutFormula\":\"Thưởng KPI thực nhận = mức thưởng KPI tối đa * điểm KPI / 100.\",\"eligibilityRule\":\"Người lao động chỉ nhận thưởng KPI khi kết quả KPI kỳ đó đã được chốt, không thuộc trường hợp bị hủy hoặc không áp dụng theo quy chế lương thưởng và quyết định kỷ luật liên quan.\",\"paymentPeriod\":\"Chi trả theo kỳ lương sau khi kết quả KPI được chốt và bảng lương được phê duyệt.\",\"approverRole\":\"Trưởng phòng chốt điểm KPI; HR kiểm tra chính sách; Giám đốc phê duyệt bảng lương.\"}", null, true, false, "Quy chế thưởng KPI HICAS 2026", "KpiBonus", null, null, "HICAS compensation policy 2026", "Active", null, null, null, null, "Formula", 1, "HICAS_KPI_BONUS_2026_V1" });

            migrationBuilder.Sql(@"
UPDATE contract_legal_snapshots
SET
    BonusPolicy = COALESCE(NULLIF(BonusPolicy, ''), 'Các khoản thưởng, phụ cấp và thu nhập biến động khác áp dụng theo quy chế lương thưởng hiện hành của công ty.'),
    KpiBonusPolicyCode = COALESCE(KpiBonusPolicyCode, 'HICAS_KPI_BONUS_2026'),
    KpiBonusPolicyVersionCode = COALESCE(KpiBonusPolicyVersionCode, 'HICAS_KPI_BONUS_2026_V1'),
    KpiScoreFormula = COALESCE(KpiScoreFormula, 'Điểm KPI chính thức = tổng max(0, trọng số KPI * điểm trưởng phòng / 100 - điểm trừ).'),
    KpiPayoutFormula = COALESCE(KpiPayoutFormula, 'Thưởng KPI thực nhận = mức thưởng KPI tối đa * điểm KPI / 100.'),
    KpiBonusEligibilityRule = COALESCE(KpiBonusEligibilityRule, 'Người lao động chỉ nhận thưởng KPI khi kết quả KPI kỳ đó đã được chốt, không thuộc trường hợp bị hủy hoặc không áp dụng theo quy chế lương thưởng và quyết định kỷ luật liên quan.'),
    KpiBonusPaymentPeriod = COALESCE(KpiBonusPaymentPeriod, 'Chi trả theo kỳ lương sau khi kết quả KPI được chốt và bảng lương được phê duyệt.'),
    KpiBonusApproverRole = COALESCE(KpiBonusApproverRole, 'Trưởng phòng chốt điểm KPI; HR kiểm tra chính sách; Giám đốc phê duyệt bảng lương.')
WHERE KpiBonusPolicyCode IS NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "payroll_policies",
                keyColumn: "Id",
                keyValue: 20260601);

            migrationBuilder.DropColumn(
                name: "KpiBonusApproverRole",
                table: "contract_legal_snapshots");

            migrationBuilder.DropColumn(
                name: "KpiBonusEligibilityRule",
                table: "contract_legal_snapshots");

            migrationBuilder.DropColumn(
                name: "KpiBonusPaymentPeriod",
                table: "contract_legal_snapshots");

            migrationBuilder.DropColumn(
                name: "KpiBonusPolicyCode",
                table: "contract_legal_snapshots");

            migrationBuilder.DropColumn(
                name: "KpiBonusPolicyVersionCode",
                table: "contract_legal_snapshots");

            migrationBuilder.DropColumn(
                name: "KpiBonusTargetAmount",
                table: "contract_legal_snapshots");

            migrationBuilder.DropColumn(
                name: "KpiPayoutFormula",
                table: "contract_legal_snapshots");

            migrationBuilder.DropColumn(
                name: "KpiScoreFormula",
                table: "contract_legal_snapshots");
        }

        private static void AddOrModifyContractLegalSnapshotColumn(
            MigrationBuilder migrationBuilder,
            string columnName,
            string columnDefinition)
        {
            migrationBuilder.Sql($@"
SET @column_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'contract_legal_snapshots'
      AND COLUMN_NAME = '{columnName}'
);
SET @sql := IF(
    @column_exists = 0,
    'ALTER TABLE `contract_legal_snapshots` ADD COLUMN `{columnName}` {columnDefinition} NULL',
    'ALTER TABLE `contract_legal_snapshots` MODIFY COLUMN `{columnName}` {columnDefinition} NULL'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
");
        }
    }
}
