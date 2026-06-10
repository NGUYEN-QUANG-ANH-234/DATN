using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class CompleteContractAddendumLegalDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddendumType",
                table: "contract_addendums",
                type: "VARCHAR(50)",
                nullable: false,
                defaultValue: "Other")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("UPDATE contract_addendums SET AddendumType = 'Other' WHERE AddendumType IS NULL OR AddendumType = '';");

            migrationBuilder.AddColumn<DateTime>(
                name: "BaseContractEndDateSnapshot",
                table: "contract_addendums",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BaseContractNumberSnapshot",
                table: "contract_addendums",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "BaseContractStartDateSnapshot",
                table: "contract_addendums",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChangedContentSummary",
                table: "contract_addendums",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DocumentDocFilePath",
                table: "contract_addendums",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DocumentPdfFilePath",
                table: "contract_addendums",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DocumentTemplateCode",
                table: "contract_addendums",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmployeeSignedAt",
                table: "contract_addendums",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmployerSignedAt",
                table: "contract_addendums",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedAt",
                table: "contract_addendums",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalDocumentNumber",
                table: "contract_addendums",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UnchangedTerms",
                table: "contract_addendums",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_contract_addendums_Type_Status",
                table: "contract_addendums",
                columns: new[] { "AddendumType", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_contract_addendums_Type_Status",
                table: "contract_addendums");

            migrationBuilder.DropColumn(
                name: "AddendumType",
                table: "contract_addendums");

            migrationBuilder.DropColumn(
                name: "BaseContractEndDateSnapshot",
                table: "contract_addendums");

            migrationBuilder.DropColumn(
                name: "BaseContractNumberSnapshot",
                table: "contract_addendums");

            migrationBuilder.DropColumn(
                name: "BaseContractStartDateSnapshot",
                table: "contract_addendums");

            migrationBuilder.DropColumn(
                name: "ChangedContentSummary",
                table: "contract_addendums");

            migrationBuilder.DropColumn(
                name: "DocumentDocFilePath",
                table: "contract_addendums");

            migrationBuilder.DropColumn(
                name: "DocumentPdfFilePath",
                table: "contract_addendums");

            migrationBuilder.DropColumn(
                name: "DocumentTemplateCode",
                table: "contract_addendums");

            migrationBuilder.DropColumn(
                name: "EmployeeSignedAt",
                table: "contract_addendums");

            migrationBuilder.DropColumn(
                name: "EmployerSignedAt",
                table: "contract_addendums");

            migrationBuilder.DropColumn(
                name: "IssuedAt",
                table: "contract_addendums");

            migrationBuilder.DropColumn(
                name: "LegalDocumentNumber",
                table: "contract_addendums");

            migrationBuilder.DropColumn(
                name: "UnchangedTerms",
                table: "contract_addendums");
        }
    }
}
