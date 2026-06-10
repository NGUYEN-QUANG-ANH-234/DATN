using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyVersioningAndResolver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "work_calendar_configs",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByAccountId",
                table: "work_calendar_configs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "work_calendar_configs",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "work_calendar_configs",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LockedAfterUsed",
                table: "work_calendar_configs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceRef",
                table: "work_calendar_configs",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "work_calendar_configs",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Active")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SupersedesVersionId",
                table: "work_calendar_configs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersionCode",
                table: "work_calendar_configs",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "tax_configs",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByAccountId",
                table: "tax_configs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LockedAfterUsed",
                table: "tax_configs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceRef",
                table: "tax_configs",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "tax_configs",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Active")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SupersedesVersionId",
                table: "tax_configs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersionCode",
                table: "tax_configs",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "pit_tax_brackets",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByAccountId",
                table: "pit_tax_brackets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LockedAfterUsed",
                table: "pit_tax_brackets",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceRef",
                table: "pit_tax_brackets",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "pit_tax_brackets",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Active")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SupersedesVersionId",
                table: "pit_tax_brackets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersionCode",
                table: "pit_tax_brackets",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "payroll_policies",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LockedAfterUsed",
                table: "payroll_policies",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceRef",
                table: "payroll_policies",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "payroll_policies",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Active")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SupersedesVersionId",
                table: "payroll_policies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersionCode",
                table: "payroll_policies",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "overtime_rate_configs",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByAccountId",
                table: "overtime_rate_configs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LockedAfterUsed",
                table: "overtime_rate_configs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceRef",
                table: "overtime_rate_configs",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "overtime_rate_configs",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Active")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SupersedesVersionId",
                table: "overtime_rate_configs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersionCode",
                table: "overtime_rate_configs",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "insurance_configs",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByAccountId",
                table: "insurance_configs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LockedAfterUsed",
                table: "insurance_configs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceRef",
                table: "insurance_configs",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "insurance_configs",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Active")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SupersedesVersionId",
                table: "insurance_configs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersionCode",
                table: "insurance_configs",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "insurance_configs",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ActivatedAt", "CreatedByAccountId", "LockedAfterUsed", "SourceRef", "Status", "SupersedesVersionId", "VersionCode" },
                values: new object[] { new DateTime(2025, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, "Vietnam insurance baseline 2025", "Active", null, "VN_INSURANCE_2025" });

            migrationBuilder.UpdateData(
                table: "overtime_rate_configs",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ActivatedAt", "CreatedByAccountId", "LockedAfterUsed", "SourceRef", "Status", "SupersedesVersionId", "VersionCode" },
                values: new object[] { new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, "Vietnam overtime baseline 2020", "Active", null, "VN_OT_2020" });

            migrationBuilder.UpdateData(
                table: "overtime_rate_configs",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ActivatedAt", "CreatedByAccountId", "LockedAfterUsed", "SourceRef", "Status", "SupersedesVersionId", "VersionCode" },
                values: new object[] { new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, "Vietnam overtime baseline 2020", "Active", null, "VN_OT_2020" });

            migrationBuilder.UpdateData(
                table: "overtime_rate_configs",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ActivatedAt", "CreatedByAccountId", "LockedAfterUsed", "SourceRef", "Status", "SupersedesVersionId", "VersionCode" },
                values: new object[] { new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, "Vietnam overtime baseline 2020", "Active", null, "VN_OT_2020" });

            migrationBuilder.UpdateData(
                table: "overtime_rate_configs",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ActivatedAt", "CreatedByAccountId", "LockedAfterUsed", "SourceRef", "Status", "SupersedesVersionId", "VersionCode" },
                values: new object[] { new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, "Vietnam overtime baseline 2020", "Active", null, "VN_OT_2020" });

            migrationBuilder.UpdateData(
                table: "overtime_rate_configs",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ActivatedAt", "CreatedByAccountId", "LockedAfterUsed", "SourceRef", "Status", "SupersedesVersionId", "VersionCode" },
                values: new object[] { new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, "Vietnam overtime baseline 2020", "Active", null, "VN_OT_2020" });

            migrationBuilder.UpdateData(
                table: "overtime_rate_configs",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ActivatedAt", "CreatedByAccountId", "LockedAfterUsed", "SourceRef", "Status", "SupersedesVersionId", "VersionCode" },
                values: new object[] { new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, "Vietnam overtime baseline 2020", "Active", null, "VN_OT_2020" });

            migrationBuilder.UpdateData(
                table: "pit_tax_brackets",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ActivatedAt", "CreatedByAccountId", "LockedAfterUsed", "SourceRef", "Status", "SupersedesVersionId", "VersionCode" },
                values: new object[] { new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, "Vietnam PIT progressive brackets baseline 2020", "Active", null, "VN_PIT_BRACKET_2020" });

            migrationBuilder.UpdateData(
                table: "pit_tax_brackets",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ActivatedAt", "CreatedByAccountId", "LockedAfterUsed", "SourceRef", "Status", "SupersedesVersionId", "VersionCode" },
                values: new object[] { new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, "Vietnam PIT progressive brackets baseline 2020", "Active", null, "VN_PIT_BRACKET_2020" });

            migrationBuilder.UpdateData(
                table: "pit_tax_brackets",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ActivatedAt", "CreatedByAccountId", "LockedAfterUsed", "SourceRef", "Status", "SupersedesVersionId", "VersionCode" },
                values: new object[] { new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, "Vietnam PIT progressive brackets baseline 2020", "Active", null, "VN_PIT_BRACKET_2020" });

            migrationBuilder.UpdateData(
                table: "pit_tax_brackets",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ActivatedAt", "CreatedByAccountId", "LockedAfterUsed", "SourceRef", "Status", "SupersedesVersionId", "VersionCode" },
                values: new object[] { new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, "Vietnam PIT progressive brackets baseline 2020", "Active", null, "VN_PIT_BRACKET_2020" });

            migrationBuilder.UpdateData(
                table: "pit_tax_brackets",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ActivatedAt", "CreatedByAccountId", "LockedAfterUsed", "SourceRef", "Status", "SupersedesVersionId", "VersionCode" },
                values: new object[] { new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, "Vietnam PIT progressive brackets baseline 2020", "Active", null, "VN_PIT_BRACKET_2020" });

            migrationBuilder.UpdateData(
                table: "pit_tax_brackets",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ActivatedAt", "CreatedByAccountId", "LockedAfterUsed", "SourceRef", "Status", "SupersedesVersionId", "VersionCode" },
                values: new object[] { new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, "Vietnam PIT progressive brackets baseline 2020", "Active", null, "VN_PIT_BRACKET_2020" });

            migrationBuilder.UpdateData(
                table: "pit_tax_brackets",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ActivatedAt", "CreatedByAccountId", "LockedAfterUsed", "SourceRef", "Status", "SupersedesVersionId", "VersionCode" },
                values: new object[] { new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, "Vietnam PIT progressive brackets baseline 2020", "Active", null, "VN_PIT_BRACKET_2020" });

            migrationBuilder.UpdateData(
                table: "tax_configs",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ActivatedAt", "CreatedByAccountId", "LockedAfterUsed", "SourceRef", "Status", "SupersedesVersionId", "VersionCode" },
                values: new object[] { new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, false, "Vietnam PIT baseline 2020", "Active", null, "VN_PIT_2020" });

            migrationBuilder.CreateIndex(
                name: "IX_tax_configs_Status_Active_EffectiveFrom",
                table: "tax_configs",
                columns: new[] { "Status", "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_pit_tax_brackets_Status_Version",
                table: "pit_tax_brackets",
                columns: new[] { "Status", "IsActive", "Code", "Version", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_policies_Type_Status_EffectiveFrom",
                table: "payroll_policies",
                columns: new[] { "PolicyType", "Status", "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_overtime_rate_configs_Type_Status_EffectiveFrom",
                table: "overtime_rate_configs",
                columns: new[] { "OvertimeType", "Status", "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_insurance_configs_Status_Active_EffectiveFrom",
                table: "insurance_configs",
                columns: new[] { "Status", "IsActive", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tax_configs_Status_Active_EffectiveFrom",
                table: "tax_configs");

            migrationBuilder.DropIndex(
                name: "IX_pit_tax_brackets_Status_Version",
                table: "pit_tax_brackets");

            migrationBuilder.DropIndex(
                name: "IX_payroll_policies_Type_Status_EffectiveFrom",
                table: "payroll_policies");

            migrationBuilder.DropIndex(
                name: "IX_overtime_rate_configs_Type_Status_EffectiveFrom",
                table: "overtime_rate_configs");

            migrationBuilder.DropIndex(
                name: "IX_insurance_configs_Status_Active_EffectiveFrom",
                table: "insurance_configs");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "CreatedByAccountId",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "LockedAfterUsed",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "SourceRef",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "SupersedesVersionId",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "VersionCode",
                table: "work_calendar_configs");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "tax_configs");

            migrationBuilder.DropColumn(
                name: "CreatedByAccountId",
                table: "tax_configs");

            migrationBuilder.DropColumn(
                name: "LockedAfterUsed",
                table: "tax_configs");

            migrationBuilder.DropColumn(
                name: "SourceRef",
                table: "tax_configs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "tax_configs");

            migrationBuilder.DropColumn(
                name: "SupersedesVersionId",
                table: "tax_configs");

            migrationBuilder.DropColumn(
                name: "VersionCode",
                table: "tax_configs");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "pit_tax_brackets");

            migrationBuilder.DropColumn(
                name: "CreatedByAccountId",
                table: "pit_tax_brackets");

            migrationBuilder.DropColumn(
                name: "LockedAfterUsed",
                table: "pit_tax_brackets");

            migrationBuilder.DropColumn(
                name: "SourceRef",
                table: "pit_tax_brackets");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "pit_tax_brackets");

            migrationBuilder.DropColumn(
                name: "SupersedesVersionId",
                table: "pit_tax_brackets");

            migrationBuilder.DropColumn(
                name: "VersionCode",
                table: "pit_tax_brackets");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "payroll_policies");

            migrationBuilder.DropColumn(
                name: "LockedAfterUsed",
                table: "payroll_policies");

            migrationBuilder.DropColumn(
                name: "SourceRef",
                table: "payroll_policies");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "payroll_policies");

            migrationBuilder.DropColumn(
                name: "SupersedesVersionId",
                table: "payroll_policies");

            migrationBuilder.DropColumn(
                name: "VersionCode",
                table: "payroll_policies");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "overtime_rate_configs");

            migrationBuilder.DropColumn(
                name: "CreatedByAccountId",
                table: "overtime_rate_configs");

            migrationBuilder.DropColumn(
                name: "LockedAfterUsed",
                table: "overtime_rate_configs");

            migrationBuilder.DropColumn(
                name: "SourceRef",
                table: "overtime_rate_configs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "overtime_rate_configs");

            migrationBuilder.DropColumn(
                name: "SupersedesVersionId",
                table: "overtime_rate_configs");

            migrationBuilder.DropColumn(
                name: "VersionCode",
                table: "overtime_rate_configs");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "insurance_configs");

            migrationBuilder.DropColumn(
                name: "CreatedByAccountId",
                table: "insurance_configs");

            migrationBuilder.DropColumn(
                name: "LockedAfterUsed",
                table: "insurance_configs");

            migrationBuilder.DropColumn(
                name: "SourceRef",
                table: "insurance_configs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "insurance_configs");

            migrationBuilder.DropColumn(
                name: "SupersedesVersionId",
                table: "insurance_configs");

            migrationBuilder.DropColumn(
                name: "VersionCode",
                table: "insurance_configs");
        }
    }
}
