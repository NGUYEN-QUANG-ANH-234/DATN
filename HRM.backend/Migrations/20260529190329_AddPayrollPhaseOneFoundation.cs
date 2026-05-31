using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollPhaseOneFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "payroll_formulas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByAccountId",
                table: "payroll_formulas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractType",
                table: "payroll_formulas",
                type: "VARCHAR(50)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "payroll_formulas",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DeptId",
                table: "payroll_formulas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "payroll_formulas",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "payroll_formulas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeType",
                table: "payroll_formulas",
                type: "VARCHAR(50)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FormulaCode",
                table: "payroll_formulas",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "DEFAULT_PAYROLL")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "JobLevelId",
                table: "payroll_formulas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayBasis",
                table: "payroll_formulas",
                type: "VARCHAR(50)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "PositionId",
                table: "payroll_formulas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "payroll_formulas",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "payroll_formulas",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "JobLevelId",
                table: "employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResidenceStatus",
                table: "employees",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Resident")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TaxCodeStatus",
                table: "employees",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Unknown")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "DailyRate",
                table: "contracts",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "contracts",
                type: "DECIMAL(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInsuranceEligible",
                table: "contracts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "PayBasis",
                table: "contracts",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Monthly")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "StandardHoursPerDaySnapshot",
                table: "contracts",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 8m);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardWorkdaysSnapshot",
                table: "contracts",
                type: "DECIMAL(15,2)",
                nullable: false,
                defaultValue: 22m);

            migrationBuilder.AddColumn<string>(
                name: "TaxMethodOverride",
                table: "contracts",
                type: "VARCHAR(50)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "insurance_configs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SocialInsuranceEmployeeRate = table.Column<decimal>(type: "DECIMAL(7,4)", nullable: false),
                    HealthInsuranceEmployeeRate = table.Column<decimal>(type: "DECIMAL(7,4)", nullable: false),
                    UnemploymentInsuranceEmployeeRate = table.Column<decimal>(type: "DECIMAL(7,4)", nullable: false),
                    SocialInsuranceEmployerRate = table.Column<decimal>(type: "DECIMAL(7,4)", nullable: false),
                    HealthInsuranceEmployerRate = table.Column<decimal>(type: "DECIMAL(7,4)", nullable: false),
                    UnemploymentInsuranceEmployerRate = table.Column<decimal>(type: "DECIMAL(7,4)", nullable: false),
                    UnionFeeEmployerRate = table.Column<decimal>(type: "DECIMAL(7,4)", nullable: false),
                    MinInsuranceSalary = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    MaxInsuranceSalary = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    UnpaidLeaveNoContributionThresholdDays = table.Column<int>(type: "int", nullable: false),
                    MinContractMonthsForContribution = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_insurance_configs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "job_levels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RankOrder = table.Column<int>(type: "int", nullable: false),
                    IsManagementLevel = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_levels", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pit_tax_brackets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    MinIncome = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    MaxIncome = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    TaxRate = table.Column<decimal>(type: "DECIMAL(7,4)", nullable: false),
                    QuickDeduction = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
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
                    table.PrimaryKey("PK_pit_tax_brackets", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "salary_component_types",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ComponentGroup = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsIncome = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeduction = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsTaxable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsInsuranceBased = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsFixed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsAllowance = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsBonus = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsOvertime = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ProrationType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CalculationMethod = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TaxExemptCap = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
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
                    table.PrimaryKey("PK_salary_component_types", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tax_configs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PersonalDeduction = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    DependentDeduction = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    FlatTaxThreshold = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    FlatTaxRate = table.Column<decimal>(type: "DECIMAL(7,4)", nullable: false),
                    NonResidentTaxRate = table.Column<decimal>(type: "DECIMAL(7,4)", nullable: false),
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
                    table.PrimaryKey("PK_tax_configs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "position_job_level_policies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    JobLevelId = table.Column<int>(type: "int", nullable: false),
                    BaseSalaryMin = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    BaseSalaryMax = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    PositionAllowance = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    ResponsibilityAllowance = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    IsInsuranceBased = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsTaxable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_position_job_level_policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_position_job_level_policies_job_levels_JobLevelId",
                        column: x => x.JobLevelId,
                        principalTable: "job_levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_position_job_level_policies_positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "employee_salary_components",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    SalaryComponentTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SourceReference = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_salary_components", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_salary_components_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_salary_components_salary_component_types_SalaryComp~",
                        column: x => x.SalaryComponentTypeId,
                        principalTable: "salary_component_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payroll_formula_lines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PayrollFormulaId = table.Column<int>(type: "int", nullable: false),
                    SalaryComponentTypeId = table.Column<int>(type: "int", nullable: true),
                    ComponentCode = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Expression = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CalculationOrder = table.Column<int>(type: "int", nullable: false),
                    IsGrossComponent = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsTaxable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsInsuranceBased = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeduction = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsSnapshotRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_formula_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payroll_formula_lines_payroll_formulas_PayrollFormulaId",
                        column: x => x.PayrollFormulaId,
                        principalTable: "payroll_formulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payroll_formula_lines_salary_component_types_SalaryComponent~",
                        column: x => x.SalaryComponentTypeId,
                        principalTable: "salary_component_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "insurance_configs",
                columns: new[] { "Id", "Code", "CreatedAt", "EffectiveFrom", "EffectiveTo", "HealthInsuranceEmployeeRate", "HealthInsuranceEmployerRate", "IsActive", "MaxInsuranceSalary", "MinContractMonthsForContribution", "MinInsuranceSalary", "Name", "Note", "SocialInsuranceEmployeeRate", "SocialInsuranceEmployerRate", "UnemploymentInsuranceEmployeeRate", "UnemploymentInsuranceEmployerRate", "UnionFeeEmployerRate", "UnpaidLeaveNoContributionThresholdDays", "Version" },
                values: new object[] { 1, "VN_STANDARD_INSURANCE_2025", new DateTime(2025, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 0.015m, 0.03m, true, null, 1, null, "Cấu hình bảo hiểm Việt Nam", "Baseline insurance config for payroll engine. Salary caps should be updated by policy version when needed.", 0.08m, 0.175m, 0.01m, 0.01m, 0.02m, 14, 1 });

            migrationBuilder.InsertData(
                table: "job_levels",
                columns: new[] { "Id", "Code", "CreatedAt", "IsActive", "IsManagementLevel", "Name", "RankOrder" },
                values: new object[,]
                {
                    { 1, "INTERN", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, false, "Thực tập sinh", 1 },
                    { 2, "JUNIOR", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, false, "Junior", 2 },
                    { 3, "MIDDLE", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, false, "Middle", 3 },
                    { 4, "SENIOR", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, false, "Senior", 4 },
                    { 5, "LEAD", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, true, "Lead", 5 },
                    { 6, "MANAGER", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, true, "Manager", 6 },
                    { 7, "DIRECTOR", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, true, "Director", 7 }
                });

            migrationBuilder.InsertData(
                table: "pit_tax_brackets",
                columns: new[] { "Id", "Code", "CreatedAt", "EffectiveFrom", "EffectiveTo", "IsActive", "Level", "MaxIncome", "MinIncome", "Note", "QuickDeduction", "TaxRate", "Version" },
                values: new object[,]
                {
                    { 1, "VN_PROGRESSIVE_PIT_2020", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 1, 5000000m, 0m, null, 0m, 0.05m, 1 },
                    { 2, "VN_PROGRESSIVE_PIT_2020", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 2, 10000000m, 5000000m, null, 250000m, 0.10m, 1 },
                    { 3, "VN_PROGRESSIVE_PIT_2020", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 3, 18000000m, 10000000m, null, 750000m, 0.15m, 1 },
                    { 4, "VN_PROGRESSIVE_PIT_2020", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 4, 32000000m, 18000000m, null, 1650000m, 0.20m, 1 },
                    { 5, "VN_PROGRESSIVE_PIT_2020", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 5, 52000000m, 32000000m, null, 3250000m, 0.25m, 1 },
                    { 6, "VN_PROGRESSIVE_PIT_2020", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 6, 80000000m, 52000000m, null, 5850000m, 0.30m, 1 },
                    { 7, "VN_PROGRESSIVE_PIT_2020", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, 7, null, 80000000m, null, 9850000m, 0.35m, 1 }
                });

            migrationBuilder.InsertData(
                table: "salary_component_types",
                columns: new[] { "Id", "CalculationMethod", "Code", "ComponentGroup", "CreatedAt", "EffectiveFrom", "EffectiveTo", "IsActive", "IsAllowance", "IsBonus", "IsDeduction", "IsFixed", "IsIncome", "IsInsuranceBased", "IsOvertime", "IsTaxable", "Name", "Note", "ProrationType", "TaxExemptCap", "Version" },
                values: new object[,]
                {
                    { 1, "Formula", "BASE_SALARY_ACTUAL", "BaseSalary", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, false, false, true, true, true, false, true, "Lương cơ bản theo công", "Base salary prorated by approved workdays.", "ByWorkingDays", null, 1 },
                    { 2, "FixedAmount", "POSITION_ALLOWANCE", "Allowance", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, true, false, false, true, true, true, false, true, "Phụ cấp chức vụ", "Configured by PositionJobLevelPolicy when fixed and recurring.", "ByWorkingDays", null, 1 },
                    { 3, "FixedAmount", "RESPONSIBILITY_ALLOWANCE", "Allowance", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, true, false, false, true, true, true, false, true, "Phụ cấp trách nhiệm", null, "ByWorkingDays", null, 1 },
                    { 4, "FixedPerDay", "MEAL_ALLOWANCE", "Allowance", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, true, false, false, false, true, false, false, false, "Phụ cấp ăn ca", "Tax-exempt cap is stored as policy data and should be versioned when changed.", "FixedPerDay", 730000m, 1 },
                    { 5, "Formula", "KPI_BONUS", "Bonus", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, true, false, false, true, false, false, true, "Thưởng KPI", null, "None", null, 1 },
                    { 6, "Formula", "OT_BASE", "Overtime", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, false, false, false, true, false, true, true, "OT phần 100% chịu thuế", null, "ByHours", null, 1 },
                    { 7, "Formula", "OT_PREMIUM", "Overtime", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, false, false, false, true, false, true, false, "OT phần hệ số tăng thêm", null, "ByHours", null, 1 },
                    { 8, "Formula", "EMPLOYEE_INSURANCE", "Insurance", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, false, true, false, false, false, false, false, "Bảo hiểm người lao động đóng", null, "None", null, 1 },
                    { 9, "Formula", "PIT", "Tax", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, false, true, false, false, false, false, false, "Thuế thu nhập cá nhân", null, "None", null, 1 }
                });

            migrationBuilder.InsertData(
                table: "tax_configs",
                columns: new[] { "Id", "Code", "CreatedAt", "DependentDeduction", "EffectiveFrom", "EffectiveTo", "FlatTaxRate", "FlatTaxThreshold", "IsActive", "Name", "NonResidentTaxRate", "Note", "PersonalDeduction", "Version" },
                values: new object[] { 1, "VN_PERSONAL_INCOME_TAX_2020", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 4400000m, new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 0.10m, 2000000m, true, "Cấu hình thuế TNCN Việt Nam", 0.20m, "Baseline PIT config. Update by creating a newer effective version.", 11000000m, 1 });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_formulas_Code_Version",
                table: "payroll_formulas",
                columns: new[] { "FormulaCode", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_formulas_Scope_Lookup",
                table: "payroll_formulas",
                columns: new[] { "Status", "ContractType", "PayBasis", "EmployeeType", "DeptId", "PositionId", "JobLevelId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_employees_JobLevelId",
                table: "employees",
                column: "JobLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_Employee_Status_StartDate",
                table: "contracts",
                columns: new[] { "EmployeeId", "Status", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_salary_components_Employee_Type_EffectiveFrom",
                table: "employee_salary_components",
                columns: new[] { "EmployeeId", "SalaryComponentTypeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_salary_components_SalaryComponentTypeId",
                table: "employee_salary_components",
                column: "SalaryComponentTypeId");

            migrationBuilder.CreateIndex(
                name: "UX_insurance_configs_Code_EffectiveFrom",
                table: "insurance_configs",
                columns: new[] { "Code", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_levels_Active_Rank",
                table: "job_levels",
                columns: new[] { "IsActive", "RankOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_job_levels_Code",
                table: "job_levels",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payroll_formula_lines_Formula_Order",
                table: "payroll_formula_lines",
                columns: new[] { "PayrollFormulaId", "CalculationOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_formula_lines_SalaryComponentTypeId",
                table: "payroll_formula_lines",
                column: "SalaryComponentTypeId");

            migrationBuilder.CreateIndex(
                name: "UX_payroll_formula_lines_Formula_Component",
                table: "payroll_formula_lines",
                columns: new[] { "PayrollFormulaId", "ComponentCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_pit_tax_brackets_Code_Level_EffectiveFrom",
                table: "pit_tax_brackets",
                columns: new[] { "Code", "Level", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_position_job_level_policies_JobLevelId",
                table: "position_job_level_policies",
                column: "JobLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_position_job_level_policies_Lookup",
                table: "position_job_level_policies",
                columns: new[] { "PositionId", "JobLevelId", "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "UX_position_job_level_policies_Position_Level_EffectiveFrom",
                table: "position_job_level_policies",
                columns: new[] { "PositionId", "JobLevelId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salary_component_types_Group_Active_EffectiveFrom",
                table: "salary_component_types",
                columns: new[] { "ComponentGroup", "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "UX_salary_component_types_Code_EffectiveFrom",
                table: "salary_component_types",
                columns: new[] { "Code", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_tax_configs_Code_EffectiveFrom",
                table: "tax_configs",
                columns: new[] { "Code", "EffectiveFrom" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_employees_job_levels_JobLevelId",
                table: "employees",
                column: "JobLevelId",
                principalTable: "job_levels",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employees_job_levels_JobLevelId",
                table: "employees");

            migrationBuilder.DropTable(
                name: "employee_salary_components");

            migrationBuilder.DropTable(
                name: "insurance_configs");

            migrationBuilder.DropTable(
                name: "payroll_formula_lines");

            migrationBuilder.DropTable(
                name: "pit_tax_brackets");

            migrationBuilder.DropTable(
                name: "position_job_level_policies");

            migrationBuilder.DropTable(
                name: "tax_configs");

            migrationBuilder.DropTable(
                name: "salary_component_types");

            migrationBuilder.DropTable(
                name: "job_levels");

            migrationBuilder.DropIndex(
                name: "IX_payroll_formulas_Code_Version",
                table: "payroll_formulas");

            migrationBuilder.DropIndex(
                name: "IX_payroll_formulas_Scope_Lookup",
                table: "payroll_formulas");

            migrationBuilder.DropIndex(
                name: "IX_employees_JobLevelId",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_contracts_Employee_Status_StartDate",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "ApprovedByAccountId",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "ContractType",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "DeptId",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "EmployeeType",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "FormulaCode",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "JobLevelId",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "PayBasis",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "payroll_formulas");

            migrationBuilder.DropColumn(
                name: "JobLevelId",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "ResidenceStatus",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "TaxCodeStatus",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "DailyRate",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "IsInsuranceEligible",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "PayBasis",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "StandardHoursPerDaySnapshot",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "StandardWorkdaysSnapshot",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "TaxMethodOverride",
                table: "contracts");

        }
    }
}
