using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddContractLegalSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<string>(
                name: "DocumentTemplateCode",
                table: "contracts",
                type: "varchar(80)",
                maxLength: 80,
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

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedAt",
                table: "contracts",
                type: "datetime(6)",
                nullable: true);

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
                name: "LegalDocumentNumber",
                table: "contracts",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LegalDocumentType",
                table: "contracts",
                type: "VARCHAR(50)",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "DocumentTemplateCode",
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
                name: "IssuedAt",
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
                name: "LegalDocumentNumber",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "LegalDocumentType",
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
        }
    }
}
