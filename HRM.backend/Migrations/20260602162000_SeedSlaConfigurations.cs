using HRM.backend.src.HRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(MyDbContext))]
    [Migration("20260602162000_SeedSlaConfigurations")]
    public partial class SeedSlaConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MigrateLegacySlaCode(migrationBuilder, "LEAVE_APPROVAL", "LeaveRequest");
            MigrateLegacySlaCode(migrationBuilder, "CONTRACT_REVIEW", "ContractRenewal");
            MigrateLegacySlaCode(migrationBuilder, "PROFILE_CHANGE", "ProfileUpdate");
            MigrateLegacySlaCode(migrationBuilder, "PAYROLL_CONFIRM", "PayrollEmployeeConfirm");
            MigrateLegacySlaCode(migrationBuilder, "RECRUITMENT_APPROVAL", "Recruitment");
            MigrateLegacySlaCode(migrationBuilder, "OVERTIME_APPROVAL", "OvertimeApproval");

            migrationBuilder.Sql("""
                INSERT INTO configurations (ConfigGroup, ParamKey, ParamValue, Description, IsActive)
                VALUES
                    ('SLA_TIME', 'SLA_Recruitment', '72', 'Unit: HOURS', 1),
                    ('SLA_TIME', 'SLA_CandidateApproval', '3', 'Unit: DAYS', 1),
                    ('SLA_TIME', 'SLA_Onboarding', '5', 'Unit: DAYS', 1),
                    ('SLA_TIME', 'SLA_ProfileUpdate', '48', 'Unit: HOURS', 1),
                    ('SLA_TIME', 'SLA_ContractRenewal', '3', 'Unit: DAYS', 1),
                    ('SLA_TIME', 'SLA_DirectorContractApproval', '2', 'Unit: DAYS', 1),
                    ('SLA_TIME', 'SLA_LeaveRequest', '48', 'Unit: HOURS', 1),
                    ('SLA_TIME', 'SLA_OvertimeApproval', '24', 'Unit: HOURS', 1),
                    ('SLA_TIME', 'SLA_AttendanceAdjustmentReview', '24', 'Unit: HOURS', 1),
                    ('SLA_TIME', 'SLA_PayrollCalculationReview', '2', 'Unit: DAYS', 1),
                    ('SLA_TIME', 'SLA_PayrollDirectorApproval', '2', 'Unit: DAYS', 1),
                    ('SLA_TIME', 'SLA_PayrollEmployeeConfirm', '3', 'Unit: DAYS', 1),
                    ('SLA_TIME', 'SLA_TaskSubmission', '2', 'Unit: DAYS', 1),
                    ('SLA_TIME', 'SLA_TaskReview', '48', 'Unit: HOURS', 1),
                    ('SLA_TIME', 'SLA_KpiReview', '7', 'Unit: DAYS', 1),
                    ('SLA_TIME', 'SLA_PerformanceReviewApproval', '2', 'Unit: DAYS', 1),
                    ('SLA_TIME', 'SLA_TrainingEvaluation', '7', 'Unit: DAYS', 1),
                    ('SLA_TIME', 'SLA_PersonnelChangeHrReview', '48', 'Unit: HOURS', 1),
                    ('SLA_TIME', 'SLA_PersonnelChangeEmployeeConsent', '72', 'Unit: HOURS', 1),
                    ('SLA_TIME', 'SLA_PersonnelChangeDirectorApproval', '48', 'Unit: HOURS', 1),
                    ('SLA_TIME', 'SLA_PersonnelChangeContractFlow', '5', 'Unit: DAYS', 1),
                    ('SLA_TIME', 'SLA_PersonnelChangeDecisionIssuance', '48', 'Unit: HOURS', 1),
                    ('SLA_TIME', 'SLA_DismissalEmployeeExplanation', '3', 'Unit: DAYS', 1),
                    ('SLA_TIME', 'SLA_ResignationManagerReview', '48', 'Unit: HOURS', 1),
                    ('SLA_TIME', 'SLA_ResignationHrReview', '48', 'Unit: HOURS', 1),
                    ('SLA_TIME', 'SLA_ResignationDirectorApproval', '48', 'Unit: HOURS', 1)
                ON DUPLICATE KEY UPDATE
                    Description = VALUES(Description);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM configurations
                WHERE ConfigGroup = 'SLA_TIME'
                  AND ParamKey IN (
                    'SLA_Recruitment',
                    'SLA_CandidateApproval',
                    'SLA_Onboarding',
                    'SLA_ProfileUpdate',
                    'SLA_ContractRenewal',
                    'SLA_DirectorContractApproval',
                    'SLA_LeaveRequest',
                    'SLA_OvertimeApproval',
                    'SLA_AttendanceAdjustmentReview',
                    'SLA_PayrollCalculationReview',
                    'SLA_PayrollDirectorApproval',
                    'SLA_PayrollEmployeeConfirm',
                    'SLA_TaskSubmission',
                    'SLA_TaskReview',
                    'SLA_KpiReview',
                    'SLA_PerformanceReviewApproval',
                    'SLA_TrainingEvaluation',
                    'SLA_PersonnelChangeHrReview',
                    'SLA_PersonnelChangeEmployeeConsent',
                    'SLA_PersonnelChangeDirectorApproval',
                    'SLA_PersonnelChangeContractFlow',
                    'SLA_PersonnelChangeDecisionIssuance',
                    'SLA_DismissalEmployeeExplanation',
                    'SLA_ResignationManagerReview',
                    'SLA_ResignationHrReview',
                    'SLA_ResignationDirectorApproval'
                  );
                """);
        }

        private static void MigrateLegacySlaCode(MigrationBuilder migrationBuilder, string legacyCode, string canonicalCode)
        {
            migrationBuilder.Sql($"""
                UPDATE configurations legacy
                LEFT JOIN configurations canonical
                  ON canonical.ConfigGroup = 'SLA_TIME'
                 AND canonical.ParamKey = 'SLA_{canonicalCode}'
                SET legacy.ParamKey = 'SLA_{canonicalCode}'
                WHERE legacy.ConfigGroup = 'SLA_TIME'
                  AND legacy.ParamKey = 'SLA_{legacyCode}'
                  AND canonical.Id IS NULL;
                """);

            migrationBuilder.Sql($"""
                UPDATE configurations
                SET IsActive = 0,
                    Description = 'Legacy SLA process replaced by {canonicalCode}.'
                WHERE ConfigGroup = 'SLA_TIME'
                  AND ParamKey = 'SLA_{legacyCode}';
                """);
        }
    }
}
