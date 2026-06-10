using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectBonusImportFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "salary_component_types",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Active")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VersionCode",
                table: "salary_component_types",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "project_bonus_import_batches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PeriodMonth = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    PeriodYear = table.Column<short>(type: "smallint", nullable: false),
                    PayrollPeriod = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadedByAccountId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    ValidRows = table.Column<int>(type: "int", nullable: false),
                    ErrorRows = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ApprovedByAccountId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_bonus_import_batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_bonus_import_batches_accounts_ApprovedByAccountId",
                        column: x => x.ApprovedByAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_project_bonus_import_batches_accounts_UploadedByAccountId",
                        column: x => x.UploadedByAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "project_bonus_import_lines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    EmployeeCodeSnapshot = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeNameSnapshot = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProjectCode = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProjectName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BonusAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    Taxable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    InsuranceContributable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValidationStatus = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_bonus_import_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_bonus_import_lines_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_project_bonus_import_lines_project_bonus_import_batches_Batc~",
                        column: x => x.BatchId,
                        principalTable: "project_bonus_import_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Status", "VersionCode" },
                values: new object[] { "Active", null });

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Status", "VersionCode" },
                values: new object[] { "Active", null });

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Status", "VersionCode" },
                values: new object[] { "Active", null });

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Status", "VersionCode" },
                values: new object[] { "Active", null });

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Status", "VersionCode" },
                values: new object[] { "Active", null });

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Status", "VersionCode" },
                values: new object[] { "Active", null });

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Status", "VersionCode" },
                values: new object[] { "Active", null });

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Status", "VersionCode" },
                values: new object[] { "Active", null });

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Status", "VersionCode" },
                values: new object[] { "Active", null });

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Status", "VersionCode" },
                values: new object[] { "Active", null });

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Status", "VersionCode" },
                values: new object[] { "Active", null });

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Status", "VersionCode" },
                values: new object[] { "Active", null });

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Status", "VersionCode" },
                values: new object[] { "Active", null });

            migrationBuilder.UpdateData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Status", "VersionCode" },
                values: new object[] { "Active", null });

            migrationBuilder.InsertData(
                table: "salary_component_types",
                columns: new[] { "Id", "CalculationMethod", "Code", "ComponentGroup", "CreatedAt", "EffectiveFrom", "EffectiveTo", "IsActive", "IsAllowance", "IsBonus", "IsDeduction", "IsFixed", "IsIncome", "IsInsuranceBased", "IsOvertime", "IsTaxable", "Name", "Note", "ProrationType", "Status", "TaxExemptCap", "Version", "VersionCode" },
                values: new object[] { 15, "Formula", "PROJECT_BONUS", "Bonus", new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2020, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, true, false, false, true, false, false, true, "Thưởng dự án", "Approved project bonus imported from ERP/accounting and included as taxable bonus income by default.", "None", "Active", null, 1, "PROJECT_BONUS_V1" });

            migrationBuilder.CreateIndex(
                name: "IX_project_bonus_batches_Period_Status",
                table: "project_bonus_import_batches",
                columns: new[] { "PeriodYear", "PeriodMonth", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_project_bonus_batches_Uploader_CreatedAt",
                table: "project_bonus_import_batches",
                columns: new[] { "UploadedByAccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_project_bonus_import_batches_ApprovedByAccountId",
                table: "project_bonus_import_batches",
                column: "ApprovedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_project_bonus_lines_Batch_Employee_Project",
                table: "project_bonus_import_lines",
                columns: new[] { "BatchId", "EmployeeId", "ProjectCode" });

            migrationBuilder.CreateIndex(
                name: "IX_project_bonus_lines_Employee_Validation",
                table: "project_bonus_import_lines",
                columns: new[] { "EmployeeId", "ValidationStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_bonus_import_lines");

            migrationBuilder.DropTable(
                name: "project_bonus_import_batches");

            migrationBuilder.DeleteData(
                table: "salary_component_types",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DropColumn(
                name: "Status",
                table: "salary_component_types");

            migrationBuilder.DropColumn(
                name: "VersionCode",
                table: "salary_component_types");
        }
    }
}
