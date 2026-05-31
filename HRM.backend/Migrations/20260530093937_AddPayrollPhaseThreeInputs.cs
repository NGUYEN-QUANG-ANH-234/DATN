using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollPhaseThreeInputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "RateMultiplierSnapshot",
                table: "overtime_segments",
                type: "DECIMAL(7,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(15,2)");

            migrationBuilder.AddColumn<string>(
                name: "OvertimeType",
                table: "overtime_segments",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Weekday")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "TaxExemptAmountSnapshot",
                table: "overtime_segments",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableAmountSnapshot",
                table: "overtime_segments",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "attendance_daily_summaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FirstCheckIn = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastCheckOut = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    WorkingMinutes = table.Column<int>(type: "int", nullable: false),
                    LateMinutes = table.Column<int>(type: "int", nullable: false),
                    EarlyLeaveMinutes = table.Column<int>(type: "int", nullable: false),
                    OvertimeMinutes = table.Column<int>(type: "int", nullable: false),
                    WorkdayValue = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    AttendanceStatus = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovalStatus = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LeaveRequestId = table.Column<int>(type: "int", nullable: true),
                    IsManualAdjusted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AdjustedByAccountId = table.Column<int>(type: "int", nullable: true),
                    AdjustedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AdjustmentReason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayrollPeriod = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsPayrollLocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_daily_summaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendance_daily_summaries_accounts_AdjustedByAccountId",
                        column: x => x.AdjustedByAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_attendance_daily_summaries_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendance_daily_summaries_leave_requests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "leave_requests",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "monthly_insurance_statuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    PayrollPeriod = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InsuranceSalary = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    UnpaidLeaveWorkingDays = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    MaternityLeaveDays = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    SickLeaveDays = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    OfficialContractWorkingDays = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    IsSocialInsuranceContributed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NonContributionReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeInsuranceAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    EmployerContributionAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    ConfigSnapshotJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_insurance_statuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_monthly_insurance_statuses_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "overtime_rate_configs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OvertimeType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BaseMultiplier = table.Column<decimal>(type: "DECIMAL(7,4)", nullable: false),
                    NightAllowanceRate = table.Column<decimal>(type: "DECIMAL(7,4)", nullable: false),
                    NightOvertimeExtraRate = table.Column<decimal>(type: "DECIMAL(7,4)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_overtime_rate_configs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payroll_adjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    RelatedPayrollId = table.Column<int>(type: "int", nullable: true),
                    AdjustmentType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecognizedMonth = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    RecognizedYear = table.Column<short>(type: "smallint", nullable: false),
                    RecognizedPayrollPeriod = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EffectiveFromMonth = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EffectiveToMonth = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    IsTaxable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsInsuranceBased = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeduction = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovedByAccountId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AppliedPayrollId = table.Column<int>(type: "int", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_adjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payroll_adjustments_accounts_ApprovedByAccountId",
                        column: x => x.ApprovedByAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_payroll_adjustments_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payroll_adjustments_payrolls_AppliedPayrollId",
                        column: x => x.AppliedPayrollId,
                        principalTable: "payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_payroll_adjustments_payrolls_RelatedPayrollId",
                        column: x => x.RelatedPayrollId,
                        principalTable: "payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payroll_contract_segments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PayrollId = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ContractType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayBasis = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TaxMethod = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsInsuranceEligible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SegmentType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BaseSalary = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    SalaryPercentage = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    StandardWorkdays = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    ActualWorkdays = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    SalaryAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    InsuranceBaseAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    SnapshotJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_contract_segments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payroll_contract_segments_contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payroll_contract_segments_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payroll_contract_segments_payrolls_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "attendance_adjustment_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AttendanceDailySummaryId = table.Column<int>(type: "int", nullable: false),
                    OldValueJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewValueJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdjustedByAccountId = table.Column<int>(type: "int", nullable: false),
                    AdjustedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_adjustment_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendance_adjustment_logs_accounts_AdjustedByAccountId",
                        column: x => x.AdjustedByAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attendance_adjustment_logs_attendance_daily_summaries_Attend~",
                        column: x => x.AttendanceDailySummaryId,
                        principalTable: "attendance_daily_summaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "overtime_rate_configs",
                columns: new[] { "Id", "BaseMultiplier", "Code", "CreatedAt", "EffectiveFrom", "EffectiveTo", "IsActive", "NightAllowanceRate", "NightOvertimeExtraRate", "Note", "OvertimeType", "Version" },
                values: new object[,]
                {
                    { 1, 1.5m, "VN_OT_WEEKDAY_2020", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 0m, 0m, "Baseline weekday OT multiplier.", "Weekday", 1 },
                    { 2, 2.0m, "VN_OT_WEEKEND_2020", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 0m, 0m, "Baseline weekly rest day OT multiplier.", "Weekend", 1 },
                    { 3, 3.0m, "VN_OT_HOLIDAY_2020", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 0m, 0m, "Baseline public holiday OT multiplier.", "Holiday", 1 },
                    { 4, 1.5m, "VN_OT_WEEKDAY_NIGHT_2020", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 0.3m, 0.2m, "Baseline weekday night OT config.", "WeekdayNight", 1 },
                    { 5, 2.0m, "VN_OT_WEEKEND_NIGHT_2020", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 0.3m, 0.2m, "Baseline weekend night OT config.", "WeekendNight", 1 },
                    { 6, 3.0m, "VN_OT_HOLIDAY_NIGHT_2020", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 0.3m, 0.2m, "Baseline holiday night OT config.", "HolidayNight", 1 }
                });

            migrationBuilder.InsertData(
                table: "salary_component_types",
                columns: new[] { "Id", "CalculationMethod", "Code", "ComponentGroup", "CreatedAt", "EffectiveFrom", "EffectiveTo", "IsActive", "IsAllowance", "IsBonus", "IsDeduction", "IsFixed", "IsIncome", "IsInsuranceBased", "IsOvertime", "IsTaxable", "Name", "Note", "ProrationType", "TaxExemptCap", "Version" },
                values: new object[,]
                {
                    { 10, "Formula", "PAYROLL_ADJUSTMENT_TAXABLE_INSURANCE", "Adjustment", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, false, false, false, true, true, false, true, "Truy lĩnh chịu thuế và tính bảo hiểm", null, "None", null, 1 },
                    { 11, "Formula", "PAYROLL_ADJUSTMENT_TAXABLE", "Adjustment", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, false, false, false, true, false, false, true, "Truy lĩnh/truy thu chịu thuế", null, "None", null, 1 },
                    { 12, "Formula", "PAYROLL_ADJUSTMENT_NONTAXABLE", "Adjustment", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, false, false, false, true, false, false, false, "Truy lĩnh/truy thu không chịu thuế", null, "None", null, 1 },
                    { 13, "Formula", "PAYROLL_ADJUSTMENT_DEDUCTION", "Adjustment", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, false, true, false, false, false, false, false, "Khoản truy thu/điều chỉnh khấu trừ", null, "None", null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_adjustment_logs_AdjustedByAccountId",
                table: "attendance_adjustment_logs",
                column: "AdjustedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_adjustment_logs_Summary_AdjustedAt",
                table: "attendance_adjustment_logs",
                columns: new[] { "AttendanceDailySummaryId", "AdjustedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_daily_summaries_AdjustedByAccountId",
                table: "attendance_daily_summaries",
                column: "AdjustedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_daily_summaries_LeaveRequestId",
                table: "attendance_daily_summaries",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_daily_summaries_Status_WorkDate",
                table: "attendance_daily_summaries",
                columns: new[] { "ApprovalStatus", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "UX_attendance_daily_summaries_Employee_WorkDate",
                table: "attendance_daily_summaries",
                columns: new[] { "EmployeeId", "WorkDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_monthly_insurance_statuses_Employee_Period",
                table: "monthly_insurance_statuses",
                columns: new[] { "EmployeeId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_overtime_rate_configs_Type_Active_EffectiveFrom",
                table: "overtime_rate_configs",
                columns: new[] { "OvertimeType", "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "UX_overtime_rate_configs_Code_EffectiveFrom",
                table: "overtime_rate_configs",
                columns: new[] { "Code", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payroll_adjustments_AppliedPayroll",
                table: "payroll_adjustments",
                column: "AppliedPayrollId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_adjustments_ApprovedByAccountId",
                table: "payroll_adjustments",
                column: "ApprovedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_adjustments_Employee_Recognized_Status",
                table: "payroll_adjustments",
                columns: new[] { "EmployeeId", "RecognizedMonth", "RecognizedYear", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_adjustments_RelatedPayrollId",
                table: "payroll_adjustments",
                column: "RelatedPayrollId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_contract_segments_ContractId",
                table: "payroll_contract_segments",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_contract_segments_Employee_Range",
                table: "payroll_contract_segments",
                columns: new[] { "EmployeeId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_contract_segments_Payroll",
                table: "payroll_contract_segments",
                column: "PayrollId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_adjustment_logs");

            migrationBuilder.DropTable(
                name: "monthly_insurance_statuses");

            migrationBuilder.DropTable(
                name: "overtime_rate_configs");

            migrationBuilder.DropTable(
                name: "payroll_adjustments");

            migrationBuilder.DropTable(
                name: "payroll_contract_segments");

            migrationBuilder.DropTable(
                name: "attendance_daily_summaries");

            migrationBuilder.DeleteData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DropColumn(
                name: "OvertimeType",
                table: "overtime_segments");

            migrationBuilder.DropColumn(
                name: "TaxExemptAmountSnapshot",
                table: "overtime_segments");

            migrationBuilder.DropColumn(
                name: "TaxableAmountSnapshot",
                table: "overtime_segments");

            migrationBuilder.AlterColumn<decimal>(
                name: "RateMultiplierSnapshot",
                table: "overtime_segments",
                type: "DECIMAL(15,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(7,4)");
        }
    }
}
