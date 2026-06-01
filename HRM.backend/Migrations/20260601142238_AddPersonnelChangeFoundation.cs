using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonnelChangeFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "personnel_change_requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ChangeType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestedByAccountId = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EffectiveDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CurrentDepartmentId = table.Column<int>(type: "int", nullable: true),
                    CurrentPositionId = table.Column<int>(type: "int", nullable: true),
                    CurrentManagerId = table.Column<int>(type: "int", nullable: true),
                    CurrentJobLevelId = table.Column<int>(type: "int", nullable: true),
                    CurrentEmployeeType = table.Column<string>(type: "VARCHAR(50)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewDepartmentId = table.Column<int>(type: "int", nullable: true),
                    NewPositionId = table.Column<int>(type: "int", nullable: true),
                    NewManagerId = table.Column<int>(type: "int", nullable: true),
                    NewJobLevelId = table.Column<int>(type: "int", nullable: true),
                    NewEmployeeType = table.Column<string>(type: "VARCHAR(50)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiresEmployeeConsent = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EmployeeConsentStatus = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeConsentAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EmployeeConsentNote = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiresContractFlow = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ContractFlowType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RelatedContractId = table.Column<int>(type: "int", nullable: true),
                    RelatedContractRequestId = table.Column<int>(type: "int", nullable: true),
                    RelatedContractAddendumId = table.Column<int>(type: "int", nullable: true),
                    ContractFlowStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiresDirectorApproval = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DirectorApprovedByAccountId = table.Column<int>(type: "int", nullable: true),
                    DirectorApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DirectorNote = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiresHRProcessing = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HRAssignedAccountId = table.Column<int>(type: "int", nullable: true),
                    HRNote = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HRProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SourcePenaltyRecordId = table.Column<int>(type: "int", nullable: true),
                    SourcePerformanceReviewId = table.Column<int>(type: "int", nullable: true),
                    DecisionNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DecisionFilePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DecisionIssuedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RejectedReason = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personnel_change_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_personnel_change_requests_accounts_DirectorApprovedByAccount~",
                        column: x => x.DirectorApprovedByAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personnel_change_requests_accounts_HRAssignedAccountId",
                        column: x => x.HRAssignedAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personnel_change_requests_accounts_RequestedByAccountId",
                        column: x => x.RequestedByAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_personnel_change_requests_contract_addendums_RelatedContract~",
                        column: x => x.RelatedContractAddendumId,
                        principalTable: "contract_addendums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personnel_change_requests_contracts_RelatedContractId",
                        column: x => x.RelatedContractId,
                        principalTable: "contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personnel_change_requests_departments_CurrentDepartmentId",
                        column: x => x.CurrentDepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personnel_change_requests_departments_NewDepartmentId",
                        column: x => x.NewDepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personnel_change_requests_employees_CurrentManagerId",
                        column: x => x.CurrentManagerId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personnel_change_requests_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_personnel_change_requests_employees_NewManagerId",
                        column: x => x.NewManagerId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personnel_change_requests_job_levels_CurrentJobLevelId",
                        column: x => x.CurrentJobLevelId,
                        principalTable: "job_levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personnel_change_requests_job_levels_NewJobLevelId",
                        column: x => x.NewJobLevelId,
                        principalTable: "job_levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personnel_change_requests_penalty_records_SourcePenaltyRecor~",
                        column: x => x.SourcePenaltyRecordId,
                        principalTable: "penalty_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_personnel_change_requests_performance_reviews_SourcePerforma~",
                        column: x => x.SourcePerformanceReviewId,
                        principalTable: "performance_reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_personnel_change_requests_positions_CurrentPositionId",
                        column: x => x.CurrentPositionId,
                        principalTable: "positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personnel_change_requests_positions_NewPositionId",
                        column: x => x.NewPositionId,
                        principalTable: "positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "personnel_change_approvals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    StepName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApproverRole = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApproverAccountId = table.Column<int>(type: "int", nullable: true),
                    Decision = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DecidedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personnel_change_approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_personnel_change_approvals_accounts_ApproverAccountId",
                        column: x => x.ApproverAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personnel_change_approvals_personnel_change_requests_Request~",
                        column: x => x.RequestId,
                        principalTable: "personnel_change_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "personnel_change_contract_links",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PersonnelChangeRequestId = table.Column<int>(type: "int", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: true),
                    ContractRequestId = table.Column<int>(type: "int", nullable: true),
                    ContractAddendumId = table.Column<int>(type: "int", nullable: true),
                    ContractFlowType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personnel_change_contract_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_personnel_change_contract_links_contract_addendums_ContractA~",
                        column: x => x.ContractAddendumId,
                        principalTable: "contract_addendums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personnel_change_contract_links_contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personnel_change_contract_links_personnel_change_requests_Pe~",
                        column: x => x.PersonnelChangeRequestId,
                        principalTable: "personnel_change_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "personnel_change_histories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OldStatus = table.Column<string>(type: "VARCHAR(50)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewStatus = table.Column<string>(type: "VARCHAR(50)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActorAccountId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personnel_change_histories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_personnel_change_histories_accounts_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personnel_change_histories_personnel_change_requests_Request~",
                        column: x => x.RequestId,
                        principalTable: "personnel_change_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "personnel_change_risk_snapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    SnapshotJson = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personnel_change_risk_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_personnel_change_risk_snapshots_accounts_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personnel_change_risk_snapshots_personnel_change_requests_Re~",
                        column: x => x.RequestId,
                        principalTable: "personnel_change_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_approvals_ApproverAccountId",
                table: "personnel_change_approvals",
                column: "ApproverAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_approvals_Request_Step",
                table: "personnel_change_approvals",
                columns: new[] { "RequestId", "StepName" });

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_contract_links_ContractAddendumId",
                table: "personnel_change_contract_links",
                column: "ContractAddendumId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_contract_links_ContractId",
                table: "personnel_change_contract_links",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_contract_links_Request_FlowType",
                table: "personnel_change_contract_links",
                columns: new[] { "PersonnelChangeRequestId", "ContractFlowType" });

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_histories_ActorAccountId",
                table: "personnel_change_histories",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_histories_Request_CreatedAt",
                table: "personnel_change_histories",
                columns: new[] { "RequestId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_CurrentDepartmentId",
                table: "personnel_change_requests",
                column: "CurrentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_CurrentJobLevelId",
                table: "personnel_change_requests",
                column: "CurrentJobLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_CurrentManagerId",
                table: "personnel_change_requests",
                column: "CurrentManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_CurrentPositionId",
                table: "personnel_change_requests",
                column: "CurrentPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_DirectorApprovedByAccountId",
                table: "personnel_change_requests",
                column: "DirectorApprovedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_Employee_Status",
                table: "personnel_change_requests",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_HRAssignedAccountId",
                table: "personnel_change_requests",
                column: "HRAssignedAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_NewDepartmentId",
                table: "personnel_change_requests",
                column: "NewDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_NewJobLevelId",
                table: "personnel_change_requests",
                column: "NewJobLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_NewManagerId",
                table: "personnel_change_requests",
                column: "NewManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_NewPositionId",
                table: "personnel_change_requests",
                column: "NewPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_RelatedContractAddendumId",
                table: "personnel_change_requests",
                column: "RelatedContractAddendumId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_RelatedContractId",
                table: "personnel_change_requests",
                column: "RelatedContractId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_RequestedByAccountId",
                table: "personnel_change_requests",
                column: "RequestedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_SourcePenaltyRecordId",
                table: "personnel_change_requests",
                column: "SourcePenaltyRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_SourcePerformanceReviewId",
                table: "personnel_change_requests",
                column: "SourcePerformanceReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_requests_Type_Status_RequestedAt",
                table: "personnel_change_requests",
                columns: new[] { "ChangeType", "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_risk_snapshots_CreatedByAccountId",
                table: "personnel_change_risk_snapshots",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_personnel_change_risk_snapshots_Request_CreatedAt",
                table: "personnel_change_risk_snapshots",
                columns: new[] { "RequestId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "personnel_change_approvals");

            migrationBuilder.DropTable(
                name: "personnel_change_contract_links");

            migrationBuilder.DropTable(
                name: "personnel_change_histories");

            migrationBuilder.DropTable(
                name: "personnel_change_risk_snapshots");

            migrationBuilder.DropTable(
                name: "personnel_change_requests");
        }
    }
}
