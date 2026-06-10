using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class MoveContractLegalSnapshotToSeparateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TEMPORARY TABLE IF NOT EXISTS `tmp_contract_legal_snapshots` AS
SELECT
    `Id` AS `ContractId`,
    `Version`,
    `LegalDocumentType`,
    `LegalDocumentNumber`,
    `DocumentTemplateCode`,
    `EmployerLegalName`,
    `EmployerTaxCode`,
    `EmployerAddress`,
    `EmployerRepresentativeName`,
    `EmployerRepresentativeTitle`,
    `EmployerRepresentativeAuthorization`,
    `SigningLocation`,
    `EmployeeFullNameSnapshot`,
    `EmployeeBirthDateSnapshot`,
    `EmployeeGenderSnapshot`,
    `EmployeeIdentityNumberSnapshot`,
    `EmployeeIdentityIssueDate`,
    `EmployeeIdentityIssuePlace`,
    `EmployeeResidenceAddressSnapshot`,
    `EmployeeDepartmentSnapshot`,
    `EmployeePositionSnapshot`,
    `EmployeeJobLevelSnapshot`,
    `JobTitle`,
    `JobDescription`,
    `WorkLocation`,
    `WorkingMode`,
    `WorkingHours`,
    `RestTime`,
    `DirectManagerSnapshot`,
    `SalaryPaymentMethod`,
    `SalaryPaymentDate`,
    `AllowanceDescription`,
    `AdditionalBenefits`,
    `SalaryReviewPolicy`,
    `BonusPolicy`,
    `InsurancePolicy`,
    `LaborProtectionPolicy`,
    `TrainingPolicy`,
    `EmployeeObligations`,
    `EmployerObligations`,
    `ConfidentialityClause`,
    `IntellectualPropertyClause`,
    `TerminationClause`,
    `DisputeResolutionClause`,
    `DocumentDocFilePath`,
    `DocumentPdfFilePath`,
    `IssuedAt`,
    `EmployeeSignedAt`,
    `EmployerSignedAt`
FROM `contracts`;
");

            migrationBuilder.DropColumn(
                name: "AdditionalBenefits",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "AllowanceDescription",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "BonusPolicy",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "ConfidentialityClause",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "DirectManagerSnapshot",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "DisputeResolutionClause",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "DocumentDocFilePath",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "DocumentPdfFilePath",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployeeBirthDateSnapshot",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployeeDepartmentSnapshot",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployeeFullNameSnapshot",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployeeGenderSnapshot",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployeeIdentityIssueDate",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployeeIdentityIssuePlace",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployeeIdentityNumberSnapshot",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployeeJobLevelSnapshot",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployeeObligations",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployeePositionSnapshot",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployeeResidenceAddressSnapshot",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployeeSignedAt",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployerAddress",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployerLegalName",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployerObligations",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployerRepresentativeAuthorization",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployerRepresentativeName",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployerRepresentativeTitle",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployerSignedAt",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "EmployerTaxCode",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "InsurancePolicy",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "IntellectualPropertyClause",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "JobDescription",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "LaborProtectionPolicy",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "RestTime",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "SalaryPaymentDate",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "SalaryPaymentMethod",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "SalaryReviewPolicy",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "SigningLocation",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "TerminationClause",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "TrainingPolicy",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "WorkLocation",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "WorkingHours",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "WorkingMode",
                table: "contracts");

            migrationBuilder.CreateTable(
                name: "contract_legal_snapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    LegalDocumentType = table.Column<string>(type: "VARCHAR(50)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LegalDocumentNumber = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocumentTemplateCode = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployerLegalName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployerTaxCode = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployerAddress = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployerRepresentativeName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployerRepresentativeTitle = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployerRepresentativeAuthorization = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SigningLocation = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeFullNameSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeBirthDateSnapshot = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EmployeeGenderSnapshot = table.Column<string>(type: "VARCHAR(50)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeIdentityNumberSnapshot = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeIdentityIssueDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EmployeeIdentityIssuePlace = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeResidenceAddressSnapshot = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeDepartmentSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeePositionSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeJobLevelSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JobTitle = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JobDescription = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WorkLocation = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WorkingMode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WorkingHours = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RestTime = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DirectManagerSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SalaryPaymentMethod = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SalaryPaymentDate = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AllowanceDescription = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdditionalBenefits = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SalaryReviewPolicy = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BonusPolicy = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InsurancePolicy = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LaborProtectionPolicy = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrainingPolicy = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeObligations = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployerObligations = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfidentialityClause = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IntellectualPropertyClause = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TerminationClause = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisputeResolutionClause = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocumentDocFilePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocumentPdfFilePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IssuedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EmployeeSignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EmployerSignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_legal_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contract_legal_snapshots_accounts_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_contract_legal_snapshots_contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_contract_legal_snapshots_Contract_CreatedAt",
                table: "contract_legal_snapshots",
                columns: new[] { "ContractId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_contract_legal_snapshots_CreatedByAccountId",
                table: "contract_legal_snapshots",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "UX_contract_legal_snapshots_Contract_Version",
                table: "contract_legal_snapshots",
                columns: new[] { "ContractId", "Version" },
                unique: true);

            migrationBuilder.Sql(@"
INSERT INTO `contract_legal_snapshots` (
    `ContractId`,
    `Version`,
    `LegalDocumentType`,
    `LegalDocumentNumber`,
    `DocumentTemplateCode`,
    `EmployerLegalName`,
    `EmployerTaxCode`,
    `EmployerAddress`,
    `EmployerRepresentativeName`,
    `EmployerRepresentativeTitle`,
    `EmployerRepresentativeAuthorization`,
    `SigningLocation`,
    `EmployeeFullNameSnapshot`,
    `EmployeeBirthDateSnapshot`,
    `EmployeeGenderSnapshot`,
    `EmployeeIdentityNumberSnapshot`,
    `EmployeeIdentityIssueDate`,
    `EmployeeIdentityIssuePlace`,
    `EmployeeResidenceAddressSnapshot`,
    `EmployeeDepartmentSnapshot`,
    `EmployeePositionSnapshot`,
    `EmployeeJobLevelSnapshot`,
    `JobTitle`,
    `JobDescription`,
    `WorkLocation`,
    `WorkingMode`,
    `WorkingHours`,
    `RestTime`,
    `DirectManagerSnapshot`,
    `SalaryPaymentMethod`,
    `SalaryPaymentDate`,
    `AllowanceDescription`,
    `AdditionalBenefits`,
    `SalaryReviewPolicy`,
    `BonusPolicy`,
    `InsurancePolicy`,
    `LaborProtectionPolicy`,
    `TrainingPolicy`,
    `EmployeeObligations`,
    `EmployerObligations`,
    `ConfidentialityClause`,
    `IntellectualPropertyClause`,
    `TerminationClause`,
    `DisputeResolutionClause`,
    `DocumentDocFilePath`,
    `DocumentPdfFilePath`,
    `IssuedAt`,
    `EmployeeSignedAt`,
    `EmployerSignedAt`,
    `CreatedAt`,
    `CreatedByAccountId`
)
SELECT
    `ContractId`,
    `Version`,
    `LegalDocumentType`,
    `LegalDocumentNumber`,
    `DocumentTemplateCode`,
    `EmployerLegalName`,
    `EmployerTaxCode`,
    `EmployerAddress`,
    `EmployerRepresentativeName`,
    `EmployerRepresentativeTitle`,
    `EmployerRepresentativeAuthorization`,
    `SigningLocation`,
    `EmployeeFullNameSnapshot`,
    `EmployeeBirthDateSnapshot`,
    `EmployeeGenderSnapshot`,
    `EmployeeIdentityNumberSnapshot`,
    `EmployeeIdentityIssueDate`,
    `EmployeeIdentityIssuePlace`,
    `EmployeeResidenceAddressSnapshot`,
    `EmployeeDepartmentSnapshot`,
    `EmployeePositionSnapshot`,
    `EmployeeJobLevelSnapshot`,
    `JobTitle`,
    `JobDescription`,
    `WorkLocation`,
    `WorkingMode`,
    `WorkingHours`,
    `RestTime`,
    `DirectManagerSnapshot`,
    `SalaryPaymentMethod`,
    `SalaryPaymentDate`,
    `AllowanceDescription`,
    `AdditionalBenefits`,
    `SalaryReviewPolicy`,
    `BonusPolicy`,
    `InsurancePolicy`,
    `LaborProtectionPolicy`,
    `TrainingPolicy`,
    `EmployeeObligations`,
    `EmployerObligations`,
    `ConfidentialityClause`,
    `IntellectualPropertyClause`,
    `TerminationClause`,
    `DisputeResolutionClause`,
    `DocumentDocFilePath`,
    `DocumentPdfFilePath`,
    `IssuedAt`,
    `EmployeeSignedAt`,
    `EmployerSignedAt`,
    UTC_TIMESTAMP(6),
    NULL
FROM `tmp_contract_legal_snapshots`;

DROP TEMPORARY TABLE IF EXISTS `tmp_contract_legal_snapshots`;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TEMPORARY TABLE IF NOT EXISTS `tmp_contract_legal_snapshots_down` AS
SELECT *
FROM `contract_legal_snapshots`;
");

            migrationBuilder.DropTable(
                name: "contract_legal_snapshots");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalBenefits",
                table: "contracts",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AllowanceDescription",
                table: "contracts",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BonusPolicy",
                table: "contracts",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ConfidentialityClause",
                table: "contracts",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DirectManagerSnapshot",
                table: "contracts",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DisputeResolutionClause",
                table: "contracts",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DocumentDocFilePath",
                table: "contracts",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DocumentPdfFilePath",
                table: "contracts",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmployeeBirthDateSnapshot",
                table: "contracts",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeDepartmentSnapshot",
                table: "contracts",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeFullNameSnapshot",
                table: "contracts",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeGenderSnapshot",
                table: "contracts",
                type: "VARCHAR(50)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmployeeIdentityIssueDate",
                table: "contracts",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeIdentityIssuePlace",
                table: "contracts",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeIdentityNumberSnapshot",
                table: "contracts",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeJobLevelSnapshot",
                table: "contracts",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeObligations",
                table: "contracts",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmployeePositionSnapshot",
                table: "contracts",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeResidenceAddressSnapshot",
                table: "contracts",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmployeeSignedAt",
                table: "contracts",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployerAddress",
                table: "contracts",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmployerLegalName",
                table: "contracts",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmployerObligations",
                table: "contracts",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmployerRepresentativeAuthorization",
                table: "contracts",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmployerRepresentativeName",
                table: "contracts",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmployerRepresentativeTitle",
                table: "contracts",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmployerSignedAt",
                table: "contracts",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployerTaxCode",
                table: "contracts",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "InsurancePolicy",
                table: "contracts",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IntellectualPropertyClause",
                table: "contracts",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "JobDescription",
                table: "contracts",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "contracts",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LaborProtectionPolicy",
                table: "contracts",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RestTime",
                table: "contracts",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SalaryPaymentDate",
                table: "contracts",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SalaryPaymentMethod",
                table: "contracts",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SalaryReviewPolicy",
                table: "contracts",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SigningLocation",
                table: "contracts",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TerminationClause",
                table: "contracts",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TrainingPolicy",
                table: "contracts",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "WorkLocation",
                table: "contracts",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "WorkingHours",
                table: "contracts",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "WorkingMode",
                table: "contracts",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(@"
UPDATE `contracts` c
JOIN (
    SELECT s.*
    FROM `tmp_contract_legal_snapshots_down` s
    JOIN (
        SELECT `ContractId`, MAX(`Version`) AS `LatestVersion`
        FROM `tmp_contract_legal_snapshots_down`
        GROUP BY `ContractId`
    ) latest
        ON latest.`ContractId` = s.`ContractId`
       AND latest.`LatestVersion` = s.`Version`
) snapshot
    ON snapshot.`ContractId` = c.`Id`
SET
    c.`LegalDocumentType` = snapshot.`LegalDocumentType`,
    c.`LegalDocumentNumber` = snapshot.`LegalDocumentNumber`,
    c.`DocumentTemplateCode` = snapshot.`DocumentTemplateCode`,
    c.`IssuedAt` = snapshot.`IssuedAt`,
    c.`AdditionalBenefits` = snapshot.`AdditionalBenefits`,
    c.`AllowanceDescription` = snapshot.`AllowanceDescription`,
    c.`BonusPolicy` = snapshot.`BonusPolicy`,
    c.`ConfidentialityClause` = snapshot.`ConfidentialityClause`,
    c.`DirectManagerSnapshot` = snapshot.`DirectManagerSnapshot`,
    c.`DisputeResolutionClause` = snapshot.`DisputeResolutionClause`,
    c.`DocumentDocFilePath` = snapshot.`DocumentDocFilePath`,
    c.`DocumentPdfFilePath` = snapshot.`DocumentPdfFilePath`,
    c.`EmployeeBirthDateSnapshot` = snapshot.`EmployeeBirthDateSnapshot`,
    c.`EmployeeDepartmentSnapshot` = snapshot.`EmployeeDepartmentSnapshot`,
    c.`EmployeeFullNameSnapshot` = snapshot.`EmployeeFullNameSnapshot`,
    c.`EmployeeGenderSnapshot` = snapshot.`EmployeeGenderSnapshot`,
    c.`EmployeeIdentityIssueDate` = snapshot.`EmployeeIdentityIssueDate`,
    c.`EmployeeIdentityIssuePlace` = snapshot.`EmployeeIdentityIssuePlace`,
    c.`EmployeeIdentityNumberSnapshot` = snapshot.`EmployeeIdentityNumberSnapshot`,
    c.`EmployeeJobLevelSnapshot` = snapshot.`EmployeeJobLevelSnapshot`,
    c.`EmployeeObligations` = snapshot.`EmployeeObligations`,
    c.`EmployeePositionSnapshot` = snapshot.`EmployeePositionSnapshot`,
    c.`EmployeeResidenceAddressSnapshot` = snapshot.`EmployeeResidenceAddressSnapshot`,
    c.`EmployeeSignedAt` = snapshot.`EmployeeSignedAt`,
    c.`EmployerAddress` = snapshot.`EmployerAddress`,
    c.`EmployerLegalName` = snapshot.`EmployerLegalName`,
    c.`EmployerObligations` = snapshot.`EmployerObligations`,
    c.`EmployerRepresentativeAuthorization` = snapshot.`EmployerRepresentativeAuthorization`,
    c.`EmployerRepresentativeName` = snapshot.`EmployerRepresentativeName`,
    c.`EmployerRepresentativeTitle` = snapshot.`EmployerRepresentativeTitle`,
    c.`EmployerSignedAt` = snapshot.`EmployerSignedAt`,
    c.`EmployerTaxCode` = snapshot.`EmployerTaxCode`,
    c.`InsurancePolicy` = snapshot.`InsurancePolicy`,
    c.`IntellectualPropertyClause` = snapshot.`IntellectualPropertyClause`,
    c.`JobDescription` = snapshot.`JobDescription`,
    c.`JobTitle` = snapshot.`JobTitle`,
    c.`LaborProtectionPolicy` = snapshot.`LaborProtectionPolicy`,
    c.`RestTime` = snapshot.`RestTime`,
    c.`SalaryPaymentDate` = snapshot.`SalaryPaymentDate`,
    c.`SalaryPaymentMethod` = snapshot.`SalaryPaymentMethod`,
    c.`SalaryReviewPolicy` = snapshot.`SalaryReviewPolicy`,
    c.`SigningLocation` = snapshot.`SigningLocation`,
    c.`TerminationClause` = snapshot.`TerminationClause`,
    c.`TrainingPolicy` = snapshot.`TrainingPolicy`,
    c.`WorkLocation` = snapshot.`WorkLocation`,
    c.`WorkingHours` = snapshot.`WorkingHours`,
    c.`WorkingMode` = snapshot.`WorkingMode`;

DROP TEMPORARY TABLE IF EXISTS `tmp_contract_legal_snapshots_down`;
");
        }
    }
}
