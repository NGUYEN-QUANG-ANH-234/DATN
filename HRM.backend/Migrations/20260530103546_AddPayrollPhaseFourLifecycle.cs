using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollPhaseFourLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsUnemploymentInsuranceContributed",
                table: "monthly_insurance_statuses",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnemploymentInsuranceAmount",
                table: "monthly_insurance_statuses",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "contract_addendum_details",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ContractAddendumId = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OldValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValueType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_addendum_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contract_addendum_details_contract_addendums_ContractAddendu~",
                        column: x => x.ContractAddendumId,
                        principalTable: "contract_addendums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "employment_service_periods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PeriodType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActualWorkingTime = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsSocialInsuranceContributed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsUnemploymentInsuranceContributed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsExcludedFromSeverance = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsSeverancePaid = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsJobLossPaid = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SourceType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employment_service_periods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employment_service_periods_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "external_timesheet_imports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SourceSystem = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImportMonth = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ImportYear = table.Column<short>(type: "smallint", nullable: false),
                    PayrollPeriod = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImportedByAccountId = table.Column<int>(type: "int", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_timesheet_imports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_external_timesheet_imports_accounts_ImportedByAccountId",
                        column: x => x.ImportedByAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "maternity_leaves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LeaveRequestId = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpectedReturnDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ActualReturnDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovedByAccountId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maternity_leaves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maternity_leaves_accounts_ApprovedByAccountId",
                        column: x => x.ApprovedByAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_maternity_leaves_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maternity_leaves_leave_requests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "leave_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "termination_requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TerminationType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LegalStatus = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reason = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NoticeDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpectedLastWorkingDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ApprovedLastWorkingDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ActualLastWorkingDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RequiredNoticeDays = table.Column<int>(type: "int", nullable: false),
                    ActualNoticeDays = table.Column<int>(type: "int", nullable: false),
                    MissingNoticeDays = table.Column<int>(type: "int", nullable: false),
                    ApprovedByAccountId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_termination_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_termination_requests_accounts_ApprovedByAccountId",
                        column: x => x.ApprovedByAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_termination_requests_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "external_timesheet_lines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ImportId = table.Column<int>(type: "int", nullable: false),
                    CollaboratorEmployeeId = table.Column<int>(type: "int", nullable: true),
                    CollaboratorCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WorkDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ProjectCode = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TaskCode = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovedHours = table.Column<decimal>(type: "DECIMAL(7,2)", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    IsPayrollImported = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PayrollId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_timesheet_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_external_timesheet_lines_employees_CollaboratorEmployeeId",
                        column: x => x.CollaboratorEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_external_timesheet_lines_external_timesheet_imports_ImportId",
                        column: x => x.ImportId,
                        principalTable: "external_timesheet_imports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_external_timesheet_lines_payrolls_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "final_settlements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TerminationRequestId = table.Column<int>(type: "int", nullable: true),
                    TerminationType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastWorkingDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UnpaidSalaryAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    UnusedAnnualLeaveDays = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    UnusedAnnualLeaveAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    SeveranceAllowanceAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    JobLossAllowanceAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    HalfMonthSalaryCompensation = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    InsufficientNoticeCompensation = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    TrainingCostCompensation = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    AssetCompensation = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    OtherDeductions = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    InsuranceAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    FinalNetAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CalculationSnapshotJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovedByAccountId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LockedByAccountId = table.Column<int>(type: "int", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_final_settlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_final_settlements_accounts_ApprovedByAccountId",
                        column: x => x.ApprovedByAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_final_settlements_accounts_LockedByAccountId",
                        column: x => x.LockedByAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_final_settlements_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_final_settlements_termination_requests_TerminationRequestId",
                        column: x => x.TerminationRequestId,
                        principalTable: "termination_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "salary_component_types",
                columns: new[] { "Id", "CalculationMethod", "Code", "ComponentGroup", "CreatedAt", "EffectiveFrom", "EffectiveTo", "IsActive", "IsAllowance", "IsBonus", "IsDeduction", "IsFixed", "IsIncome", "IsInsuranceBased", "IsOvertime", "IsTaxable", "Name", "Note", "ProrationType", "TaxExemptCap", "Version" },
                values: new object[] { 14, "Formula", "EXTERNAL_TIMESHEET_PAY", "BaseSalary", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, false, false, false, true, false, false, true, "Thu nhập từ timesheet ngoài", "Used for collaborators/freelancers imported from approved external timesheets.", "ByHours", null, 1 });

            migrationBuilder.CreateIndex(
                name: "IX_contract_addendum_details_Addendum_Field",
                table: "contract_addendum_details",
                columns: new[] { "ContractAddendumId", "FieldName" });

            migrationBuilder.CreateIndex(
                name: "IX_employment_service_periods_Employee_Range",
                table: "employment_service_periods",
                columns: new[] { "EmployeeId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_external_timesheet_imports_ImportedByAccountId",
                table: "external_timesheet_imports",
                column: "ImportedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_external_timesheet_imports_Source_Period_Status",
                table: "external_timesheet_imports",
                columns: new[] { "SourceSystem", "ImportMonth", "ImportYear", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_external_timesheet_lines_Employee_WorkDate_Payroll",
                table: "external_timesheet_lines",
                columns: new[] { "CollaboratorEmployeeId", "WorkDate", "IsPayrollImported" });

            migrationBuilder.CreateIndex(
                name: "IX_external_timesheet_lines_Import_Employee_WorkDate",
                table: "external_timesheet_lines",
                columns: new[] { "ImportId", "CollaboratorEmployeeId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_external_timesheet_lines_PayrollId",
                table: "external_timesheet_lines",
                column: "PayrollId");

            migrationBuilder.CreateIndex(
                name: "IX_final_settlements_ApprovedByAccountId",
                table: "final_settlements",
                column: "ApprovedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_final_settlements_Employee_Status",
                table: "final_settlements",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_final_settlements_LockedByAccountId",
                table: "final_settlements",
                column: "LockedByAccountId");

            migrationBuilder.CreateIndex(
                name: "UX_final_settlements_TerminationRequest",
                table: "final_settlements",
                column: "TerminationRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_maternity_leaves_ApprovedByAccountId",
                table: "maternity_leaves",
                column: "ApprovedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_maternity_leaves_Employee_Status_Start",
                table: "maternity_leaves",
                columns: new[] { "EmployeeId", "Status", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_maternity_leaves_LeaveRequestId",
                table: "maternity_leaves",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_termination_requests_ApprovedByAccountId",
                table: "termination_requests",
                column: "ApprovedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_termination_requests_Employee_Status_LastDate",
                table: "termination_requests",
                columns: new[] { "EmployeeId", "Status", "ExpectedLastWorkingDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contract_addendum_details");

            migrationBuilder.DropTable(
                name: "employment_service_periods");

            migrationBuilder.DropTable(
                name: "external_timesheet_lines");

            migrationBuilder.DropTable(
                name: "final_settlements");

            migrationBuilder.DropTable(
                name: "maternity_leaves");

            migrationBuilder.DropTable(
                name: "external_timesheet_imports");

            migrationBuilder.DropTable(
                name: "termination_requests");

            migrationBuilder.DeleteData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DropColumn(
                name: "IsUnemploymentInsuranceContributed",
                table: "monthly_insurance_statuses");

            migrationBuilder.DropColumn(
                name: "UnemploymentInsuranceAmount",
                table: "monthly_insurance_statuses");
        }
    }
}
