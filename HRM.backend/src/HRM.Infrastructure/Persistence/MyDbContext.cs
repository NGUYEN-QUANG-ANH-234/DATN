using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Entities.PersonnelChanges;
using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Entities.RequestHandover;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using System.Reflection.Emit;
using System.Security.Claims;
using System.Text.Json;

namespace HRM.backend.src.HRM.Infrastructure.Persistence
{
    public class MyDbContext : DbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly string[] SENSITIVE_FIELDS = {
            "PasswordHash", "RefreshToken", "MfaSecretKey", "MfaRecoveryCodes",
            "BaseSalary", "BaseSalaryActual", "BasicSalary", "BonusAmount", "GrossIncome",
            "InsuranceSalary", "EmployeeInsuranceAmount", "TaxableIncome", "PitAmount",
            "NetPay", "NetSalary", "TotalCompanyCost"
        };

        // Cập nhật constructor (Cho phép Nullable IHttpContextAccessor để tránh lỗi lúc chạy Migration)
        public MyDbContext(DbContextOptions<MyDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        // ==========================================
        // DBSETS: 8 MODULES
        // ==========================================

        // 1. System
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Configuration> Configurations { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<MfaRecoveryCode> MfaRecoveryCodes { get; set; }
        public DbSet<SourceCatalog> SourceCatalogs { get; set; }
        public DbSet<PayrollPolicy> PayrollPolicies { get; set; }
        public DbSet<SlaTrackingTask> SlaTrackingTasks { get; set; }
        public DbSet<ApprovalRequest> ApprovalRequests { get; set; }
        public DbSet<ApprovalStep> ApprovalSteps { get; set; }
        public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        // 2. Organization
        public DbSet<Department> Departments { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<JobLevel> JobLevels { get; set; }
        public DbSet<PositionJobLevelPolicy> PositionJobLevelPolicies { get; set; }

        // 3. Recruitment
        public DbSet<RecruitmentRequest> RecruitmentRequests { get; set; }
        public DbSet<Candidate> Candidates { get; set; }

        // 4. Employee Profile
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Dependent> Dependents { get; set; }
        public DbSet<DependentUpdateRequest> DependentUpdateRequests { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ContractLegalSnapshot> ContractLegalSnapshots { get; set; }
        public DbSet<ContractAddendum> ContractAddendums { get; set; }
        public DbSet<ContractAddendumDetail> ContractAddendumDetails { get; set; }
        public DbSet<MaternityLeave> MaternityLeaves { get; set; }
        public DbSet<TerminationRequest> TerminationRequests { get; set; }
        public DbSet<FinalSettlement> FinalSettlements { get; set; }
        public DbSet<EmploymentServicePeriod> EmploymentServicePeriods { get; set; }
        public DbSet<OnboardingRequest> OnboardingRequests { get; set; }
        public DbSet<ProfileUpdateRequest> ProfileUpdateRequests { get; set; }

        // 5. Time & Attendance
        public DbSet<WorkShift> WorkShifts { get; set; }
        public DbSet<AttendanceLog> AttendanceLogs { get; set; }
        public DbSet<AttendanceSummary> AttendanceSummaries { get; set; }
        public DbSet<AttendanceDailySummary> AttendanceDailySummaries { get; set; }
        public DbSet<AttendanceAdjustmentLog> AttendanceAdjustmentLogs { get; set; }
        public DbSet<WorkCalendarConfig> WorkCalendarConfigs { get; set; }
        public DbSet<CompanyCalendar> CompanyCalendars { get; set; }
        public DbSet<CompanyCalendarDay> CompanyCalendarDays { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<OvertimeRequest> OvertimeRequests { get; set; }
        public DbSet<OvertimeSegment> OvertimeSegments { get; set; }

        // 6. Tasks & Training
        public DbSet<KpiImportBatch> KpiImportBatches { get; set; }
        public DbSet<PerformanceDetail> PerformanceDetails { get; set; }
        public DbSet<PenaltyRule> PenaltyRules { get; set; }
        public DbSet<PenaltyRecord> PenaltyRecords { get; set; }
        public DbSet<WorkTask> Tasks { get; set; }
        public DbSet<PerformanceReview> PerformanceReviews { get; set; }
        public DbSet<TaskProgress> TaskProgresses { get; set; }
        public DbSet<TaskFeedback> TaskFeedbacks { get; set; }
        public DbSet<Training> Trainings { get; set; }

        // 7. Payroll & Allowances
        public DbSet<AllowanceType> AllowanceTypes { get; set; }
        public DbSet<EmployeeAllowance> EmployeeAllowances { get; set; }
        public DbSet<PayrollFormula> PayrollFormulas { get; set; }
        public DbSet<PayrollFormulaLine> PayrollFormulaLines { get; set; }
        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<PayrollDetail> PayrollDetails { get; set; }
        public DbSet<SalaryComponentType> SalaryComponentTypes { get; set; }
        public DbSet<EmployeeSalaryComponent> EmployeeSalaryComponents { get; set; }
        public DbSet<TaxConfig> TaxConfigs { get; set; }
        public DbSet<PITTaxBracket> PITTaxBrackets { get; set; }
        public DbSet<InsuranceConfig> InsuranceConfigs { get; set; }
        public DbSet<MonthlyInsuranceStatus> MonthlyInsuranceStatuses { get; set; }
        public DbSet<PayrollContractSegment> PayrollContractSegments { get; set; }
        public DbSet<PayrollAdjustment> PayrollAdjustments { get; set; }
        public DbSet<OvertimeRateConfig> OvertimeRateConfigs { get; set; }
        public DbSet<ExternalTimesheetImport> ExternalTimesheetImports { get; set; }
        public DbSet<ExternalTimesheetLine> ExternalTimesheetLines { get; set; }
        public DbSet<ProjectBonusImportBatch> ProjectBonusImportBatches { get; set; }
        public DbSet<ProjectBonusImportLine> ProjectBonusImportLines { get; set; }

        // 8. Personnel Changes
        public DbSet<PersonnelChangeRequest> PersonnelChangeRequests { get; set; }
        public DbSet<PersonnelChangeApproval> PersonnelChangeApprovals { get; set; }
        public DbSet<PersonnelChangeHistory> PersonnelChangeHistories { get; set; }
        public DbSet<PersonnelChangeContractLink> PersonnelChangeContractLinks { get; set; }
        public DbSet<PersonnelChangeRiskSnapshot> PersonnelChangeRiskSnapshots { get; set; }

        // 9. Requests & Handover
        public DbSet<Request> Requests { get; set; }
        public DbSet<EmploymentHistory> EmploymentHistories { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // 1. KHÓA CHÍNH PHỨC HỢP (COMPOSITE KEYS)
            // ==========================================
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<LeaveBalance>()
                .HasKey(lb => new { lb.EmployeeId, lb.LeaveTypeId, lb.Year });

            // ==========================================
            // 2. UNIQUE INDEX (Đảm bảo tính duy nhất)
            // ==========================================
            modelBuilder.Entity<Account>().HasIndex(a => a.Email).IsUnique();
            modelBuilder.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
            modelBuilder.Entity<Permission>().HasIndex(p => p.PermissionCode).IsUnique();
            modelBuilder.Entity<Configuration>().HasIndex(c => new { c.ConfigGroup, c.ParamKey }).IsUnique();
            modelBuilder.Entity<SourceCatalog>().HasIndex(s => s.SourcePath).IsUnique();
            modelBuilder.Entity<PayrollPolicy>().HasIndex(p => new { p.PolicyType, p.Code, p.EffectiveFrom }).IsUnique().HasDatabaseName("UX_payroll_policies_Type_Code_EffectiveFrom");
            modelBuilder.Entity<PayrollPolicy>().HasIndex(p => new { p.PolicyType, p.IsActive, p.EffectiveFrom }).HasDatabaseName("IX_payroll_policies_Type_Active_EffectiveFrom");
            modelBuilder.Entity<PayrollPolicy>().HasIndex(p => new { p.PolicyType, p.Status, p.IsActive, p.EffectiveFrom }).HasDatabaseName("IX_payroll_policies_Type_Status_EffectiveFrom");
            modelBuilder.Entity<IdempotencyRecord>().HasIndex(i => new { i.Scope, i.IdempotencyKey }).IsUnique();
            modelBuilder.Entity<OutboxMessage>().HasIndex(o => new { o.Status, o.CreatedAt });
            modelBuilder.Entity<Department>().HasIndex(d => d.DeptCode).IsUnique();
            modelBuilder.Entity<Position>().HasIndex(p => p.Title).IsUnique();
            modelBuilder.Entity<JobLevel>().HasIndex(j => j.Code).IsUnique().HasDatabaseName("UX_job_levels_Code");
            modelBuilder.Entity<JobLevel>().HasIndex(j => new { j.IsActive, j.RankOrder }).HasDatabaseName("IX_job_levels_Active_Rank");
            modelBuilder.Entity<PositionJobLevelPolicy>().HasIndex(p => new { p.PositionId, p.JobLevelId, p.EffectiveFrom }).IsUnique().HasDatabaseName("UX_position_job_level_policies_Position_Level_EffectiveFrom");
            modelBuilder.Entity<PositionJobLevelPolicy>().HasIndex(p => new { p.PositionId, p.JobLevelId, p.IsActive, p.EffectiveFrom }).HasDatabaseName("IX_position_job_level_policies_Lookup");
            modelBuilder.Entity<Contract>().HasIndex(c => c.ContractNumber).IsUnique();
            modelBuilder.Entity<Contract>().HasIndex(c => new { c.EmployeeId, c.Status, c.StartDate }).HasDatabaseName("IX_contracts_Employee_Status_StartDate");
            modelBuilder.Entity<ContractLegalSnapshot>().HasIndex(s => new { s.ContractId, s.Version }).IsUnique().HasDatabaseName("UX_contract_legal_snapshots_Contract_Version");
            modelBuilder.Entity<ContractLegalSnapshot>().HasIndex(s => new { s.ContractId, s.CreatedAt }).HasDatabaseName("IX_contract_legal_snapshots_Contract_CreatedAt");
            modelBuilder.Entity<ContractAddendum>().HasIndex(ca => ca.AddendumNumber).IsUnique();
            modelBuilder.Entity<ContractAddendum>().HasIndex(ca => new { ca.ContractId, ca.Status });
            modelBuilder.Entity<ContractAddendum>().HasIndex(ca => new { ca.AddendumType, ca.Status }).HasDatabaseName("IX_contract_addendums_Type_Status");
            modelBuilder.Entity<ContractAddendumDetail>().HasIndex(d => new { d.ContractAddendumId, d.FieldName }).HasDatabaseName("IX_contract_addendum_details_Addendum_Field");
            modelBuilder.Entity<MaternityLeave>().HasIndex(m => new { m.EmployeeId, m.Status, m.StartDate }).HasDatabaseName("IX_maternity_leaves_Employee_Status_Start");
            modelBuilder.Entity<TerminationRequest>().HasIndex(t => new { t.EmployeeId, t.Status, t.ExpectedLastWorkingDate }).HasDatabaseName("IX_termination_requests_Employee_Status_LastDate");
            modelBuilder.Entity<FinalSettlement>().HasIndex(f => new { f.EmployeeId, f.Status }).HasDatabaseName("IX_final_settlements_Employee_Status");
            modelBuilder.Entity<FinalSettlement>().HasIndex(f => f.TerminationRequestId).IsUnique().HasDatabaseName("UX_final_settlements_TerminationRequest");
            modelBuilder.Entity<EmploymentServicePeriod>().HasIndex(p => new { p.EmployeeId, p.PeriodStart, p.PeriodEnd }).HasDatabaseName("IX_employment_service_periods_Employee_Range");
            modelBuilder.Entity<WorkShift>().HasIndex(s => s.DeptId).IsUnique().HasDatabaseName("UX_work_shifts_DeptId");
            modelBuilder.Entity<WorkCalendarConfig>().HasIndex(w => new { w.DeptId, w.Month, w.Year }).IsUnique().HasDatabaseName("UX_work_calendar_configs_DeptId_Month_Year");
            modelBuilder.Entity<WorkCalendarConfig>().HasIndex(w => w.CompanyCalendarId).HasDatabaseName("IX_work_calendar_configs_CompanyCalendarId");
            modelBuilder.Entity<CompanyCalendar>().HasIndex(c => new { c.Year, c.VersionCode }).IsUnique().HasDatabaseName("UX_company_calendars_Year_Version");
            modelBuilder.Entity<CompanyCalendar>().HasIndex(c => new { c.Year, c.Status, c.EffectiveFrom }).HasDatabaseName("IX_company_calendars_Year_Status_EffectiveFrom");
            modelBuilder.Entity<CompanyCalendarDay>().HasIndex(d => new { d.CalendarId, d.Date }).IsUnique().HasDatabaseName("UX_company_calendar_days_Calendar_Date");
            modelBuilder.Entity<LeaveRequest>().HasIndex(l => new { l.EmployeeId, l.LeaveTypeId, l.StartDate, l.EndDate }).IsUnique().HasDatabaseName("UX_leave_requests_EmployeeId_LeaveTypeId_StartDate_EndDate");
            modelBuilder.Entity<OvertimeRequest>().HasIndex(o => new { o.EmployeeId, o.WorkDate, o.StartTime, o.EndTime }).IsUnique().HasDatabaseName("UX_overtime_requests_EmployeeId_WorkDate_StartTime_EndTime");
            modelBuilder.Entity<OvertimeRequest>().HasIndex(o => new { o.EmployeeId, o.StartAt, o.EndAt }).HasDatabaseName("IX_overtime_requests_EmployeeId_StartAt_EndAt");
            modelBuilder.Entity<OvertimeSegment>().HasIndex(o => new { o.OvertimeRequestId, o.SegmentStartAt }).HasDatabaseName("IX_overtime_segments_Request_Start");
            modelBuilder.Entity<AttendanceDailySummary>().HasIndex(a => new { a.EmployeeId, a.WorkDate }).IsUnique().HasDatabaseName("UX_attendance_daily_summaries_Employee_WorkDate");
            modelBuilder.Entity<AttendanceDailySummary>().HasIndex(a => new { a.ApprovalStatus, a.WorkDate }).HasDatabaseName("IX_attendance_daily_summaries_Status_WorkDate");
            modelBuilder.Entity<AttendanceAdjustmentLog>().HasIndex(a => new { a.AttendanceDailySummaryId, a.AdjustedAt }).HasDatabaseName("IX_attendance_adjustment_logs_Summary_AdjustedAt");
            modelBuilder.Entity<Candidate>().HasIndex(c => new { c.RecruitmentRequestId, c.Email }).IsUnique().HasDatabaseName("UX_candidates_RecruitmentRequestId_Email");
            modelBuilder.Entity<Candidate>().HasIndex(c => c.TrackingCode).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.EmployeeCode).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.AccountId).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.CandidateId).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.IdentityNumber).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.TaxCode).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.SocialInsCode).IsUnique();
            modelBuilder.Entity<Dependent>().HasIndex(d => new { d.EmployeeId, d.TaxDependentCode });
            modelBuilder.Entity<DependentUpdateRequest>().HasIndex(d => new { d.EmployeeId, d.Status });
            modelBuilder.Entity<PerformanceReview>().HasIndex(p => new { p.EmployeeId, p.Period }).IsUnique().HasDatabaseName("UX_performance_reviews_EmployeeId_Period");
            modelBuilder.Entity<PerformanceReview>().HasIndex(p => new { p.DeptId, p.Period, p.Status });
            modelBuilder.Entity<PerformanceDetail>().HasIndex(p => new { p.ReviewId, p.KpiCode }).IsUnique().HasDatabaseName("UX_performance_details_ReviewId_KpiCode");
            modelBuilder.Entity<PenaltyRule>().HasIndex(p => new { p.SourceType, p.RuleCode }).IsUnique().HasDatabaseName("UX_penalty_rules_SourceType_RuleCode");
            modelBuilder.Entity<PenaltyRecord>().HasIndex(p => new { p.EmployeeId, p.Period, p.SourceType });
            modelBuilder.Entity<PenaltyRecord>().HasIndex(p => new { p.SourceType, p.ReferenceId, p.RuleCode }).HasDatabaseName("IX_penalty_records_Source_Reference_Rule");
            modelBuilder.Entity<PenaltyRecord>().HasIndex(p => new { p.Status, p.AffectsAttendance, p.AffectsPerformance }).HasDatabaseName("IX_penalty_records_Status_Impact");
            modelBuilder.Entity<PenaltyRecord>().HasIndex(p => new { p.EmployeeId, p.Period, p.AffectsPerformance, p.Status }).HasDatabaseName("IX_penalty_records_Employee_Performance_Status");
            modelBuilder.Entity<PenaltyRecord>().HasIndex(p => new { p.EmployeeId, p.AffectsPersonnelDecision, p.Severity, p.OccurredAt }).HasDatabaseName("IX_penalty_records_Employee_Personnel_History");
            modelBuilder.Entity<WorkTask>().HasIndex(t => new { t.AssignedTo, t.Status, t.Deadline });
            modelBuilder.Entity<TaskProgress>().HasIndex(t => new { t.TaskId, t.SubmittedAt });
            modelBuilder.Entity<Training>().HasIndex(t => new { t.EmployeeId, t.Status });
            modelBuilder.Entity<Training>().HasIndex(t => new { t.ManagerId, t.EvaluationDeadline, t.Status });
            modelBuilder.Entity<Payroll>().HasIndex(p => new { p.EmployeeId, p.Month, p.Year }).HasDatabaseName("IX_payrolls_Employee_Period");
            modelBuilder.Entity<Payroll>().HasIndex(p => new { p.Month, p.Year, p.Status }).HasDatabaseName("IX_payrolls_Period_Status");
            modelBuilder.Entity<PayrollFormula>().HasIndex(f => new { f.FormulaCode, f.Version }).HasDatabaseName("IX_payroll_formulas_Code_Version");
            modelBuilder.Entity<PayrollFormula>().HasIndex(f => new { f.Status, f.ContractType, f.PayBasis, f.EmployeeType, f.DeptId, f.PositionId, f.JobLevelId, f.EffectiveFrom }).HasDatabaseName("IX_payroll_formulas_Scope_Lookup");
            modelBuilder.Entity<PayrollFormulaLine>().HasIndex(l => new { l.PayrollFormulaId, l.ComponentCode }).IsUnique().HasDatabaseName("UX_payroll_formula_lines_Formula_Component");
            modelBuilder.Entity<PayrollFormulaLine>().HasIndex(l => new { l.PayrollFormulaId, l.CalculationOrder }).HasDatabaseName("IX_payroll_formula_lines_Formula_Order");
            modelBuilder.Entity<PayrollDetail>().HasIndex(p => new { p.PayrollId, p.ComponentCode }).HasDatabaseName("IX_payroll_details_Payroll_Component");
            modelBuilder.Entity<SalaryComponentType>().HasIndex(s => new { s.Code, s.EffectiveFrom }).IsUnique().HasDatabaseName("UX_salary_component_types_Code_EffectiveFrom");
            modelBuilder.Entity<SalaryComponentType>().HasIndex(s => new { s.ComponentGroup, s.IsActive, s.EffectiveFrom }).HasDatabaseName("IX_salary_component_types_Group_Active_EffectiveFrom");
            modelBuilder.Entity<EmployeeSalaryComponent>().HasIndex(s => new { s.EmployeeId, s.SalaryComponentTypeId, s.EffectiveFrom }).HasDatabaseName("IX_employee_salary_components_Employee_Type_EffectiveFrom");
            modelBuilder.Entity<TaxConfig>().HasIndex(t => new { t.Code, t.EffectiveFrom }).IsUnique().HasDatabaseName("UX_tax_configs_Code_EffectiveFrom");
            modelBuilder.Entity<TaxConfig>().HasIndex(t => new { t.Status, t.IsActive, t.EffectiveFrom }).HasDatabaseName("IX_tax_configs_Status_Active_EffectiveFrom");
            modelBuilder.Entity<PITTaxBracket>().HasIndex(t => new { t.Code, t.Level, t.EffectiveFrom }).IsUnique().HasDatabaseName("UX_pit_tax_brackets_Code_Level_EffectiveFrom");
            modelBuilder.Entity<PITTaxBracket>().HasIndex(t => new { t.Status, t.IsActive, t.Code, t.Version, t.EffectiveFrom }).HasDatabaseName("IX_pit_tax_brackets_Status_Version");
            modelBuilder.Entity<InsuranceConfig>().HasIndex(i => new { i.Code, i.EffectiveFrom }).IsUnique().HasDatabaseName("UX_insurance_configs_Code_EffectiveFrom");
            modelBuilder.Entity<InsuranceConfig>().HasIndex(i => new { i.Status, i.IsActive, i.EffectiveFrom }).HasDatabaseName("IX_insurance_configs_Status_Active_EffectiveFrom");
            modelBuilder.Entity<MonthlyInsuranceStatus>().HasIndex(i => new { i.EmployeeId, i.Month, i.Year }).IsUnique().HasDatabaseName("UX_monthly_insurance_statuses_Employee_Period");
            modelBuilder.Entity<PayrollContractSegment>().HasIndex(s => s.PayrollId).HasDatabaseName("IX_payroll_contract_segments_Payroll");
            modelBuilder.Entity<PayrollContractSegment>().HasIndex(s => new { s.EmployeeId, s.StartDate, s.EndDate }).HasDatabaseName("IX_payroll_contract_segments_Employee_Range");
            modelBuilder.Entity<PayrollAdjustment>().HasIndex(a => new { a.EmployeeId, a.RecognizedMonth, a.RecognizedYear, a.Status }).HasDatabaseName("IX_payroll_adjustments_Employee_Recognized_Status");
            modelBuilder.Entity<PayrollAdjustment>().HasIndex(a => a.AppliedPayrollId).HasDatabaseName("IX_payroll_adjustments_AppliedPayroll");
            modelBuilder.Entity<OvertimeRateConfig>().HasIndex(o => new { o.Code, o.EffectiveFrom }).IsUnique().HasDatabaseName("UX_overtime_rate_configs_Code_EffectiveFrom");
            modelBuilder.Entity<OvertimeRateConfig>().HasIndex(o => new { o.OvertimeType, o.IsActive, o.EffectiveFrom }).HasDatabaseName("IX_overtime_rate_configs_Type_Active_EffectiveFrom");
            modelBuilder.Entity<OvertimeRateConfig>().HasIndex(o => new { o.OvertimeType, o.Status, o.IsActive, o.EffectiveFrom }).HasDatabaseName("IX_overtime_rate_configs_Type_Status_EffectiveFrom");
            modelBuilder.Entity<ExternalTimesheetImport>().HasIndex(e => new { e.SourceSystem, e.ImportMonth, e.ImportYear, e.Status }).HasDatabaseName("IX_external_timesheet_imports_Source_Period_Status");
            modelBuilder.Entity<ExternalTimesheetLine>().HasIndex(e => new { e.ImportId, e.CollaboratorEmployeeId, e.WorkDate }).HasDatabaseName("IX_external_timesheet_lines_Import_Employee_WorkDate");
            modelBuilder.Entity<ExternalTimesheetLine>().HasIndex(e => new { e.CollaboratorEmployeeId, e.WorkDate, e.IsPayrollImported }).HasDatabaseName("IX_external_timesheet_lines_Employee_WorkDate_Payroll");
            modelBuilder.Entity<ProjectBonusImportBatch>().HasIndex(b => new { b.PeriodYear, b.PeriodMonth, b.Status }).HasDatabaseName("IX_project_bonus_batches_Period_Status");
            modelBuilder.Entity<ProjectBonusImportBatch>().HasIndex(b => new { b.UploadedByAccountId, b.CreatedAt }).HasDatabaseName("IX_project_bonus_batches_Uploader_CreatedAt");
            modelBuilder.Entity<ProjectBonusImportLine>().HasIndex(l => new { l.BatchId, l.EmployeeId, l.ProjectCode }).HasDatabaseName("IX_project_bonus_lines_Batch_Employee_Project");
            modelBuilder.Entity<ProjectBonusImportLine>().HasIndex(l => new { l.EmployeeId, l.ValidationStatus }).HasDatabaseName("IX_project_bonus_lines_Employee_Validation");
            modelBuilder.Entity<PersonnelChangeRequest>().HasIndex(p => new { p.ChangeType, p.Status, p.RequestedAt }).HasDatabaseName("IX_personnel_change_requests_Type_Status_RequestedAt");
            modelBuilder.Entity<PersonnelChangeRequest>().HasIndex(p => new { p.EmployeeId, p.Status }).HasDatabaseName("IX_personnel_change_requests_Employee_Status");
            modelBuilder.Entity<PersonnelChangeApproval>().HasIndex(p => new { p.RequestId, p.StepName }).HasDatabaseName("IX_personnel_change_approvals_Request_Step");
            modelBuilder.Entity<PersonnelChangeHistory>().HasIndex(p => new { p.RequestId, p.CreatedAt }).HasDatabaseName("IX_personnel_change_histories_Request_CreatedAt");
            modelBuilder.Entity<PersonnelChangeContractLink>().HasIndex(p => new { p.PersonnelChangeRequestId, p.ContractFlowType }).HasDatabaseName("IX_personnel_change_contract_links_Request_FlowType");
            modelBuilder.Entity<PersonnelChangeRiskSnapshot>().HasIndex(p => new { p.RequestId, p.CreatedAt }).HasDatabaseName("IX_personnel_change_risk_snapshots_Request_CreatedAt");

            // ==========================================
            // 3. QUAN HỆ (RELATIONSHIPS) & CHỐNG LỖI CASCADE
            // ==========================================

            // --- SYSTEM ---
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Role).WithMany(r => r.Accounts)
                .HasForeignKey(a => a.RoleId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MfaRecoveryCode>()
                .HasOne<Account>()
                .WithMany()
                .HasForeignKey(m => m.AccountId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Account -> Xóa luôn Recovery Codes

            // --- ORGANIZATION & EMPLOYEE ---
            modelBuilder.Entity<Department>()
                .HasOne(d => d.ParentDepartment).WithMany(d => d.SubDepartments)
                .HasForeignKey(d => d.ParentDeptId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Department>()
                .HasOne(d => d.Manager).WithMany()
                .HasForeignKey(d => d.ManagerId).OnDelete(DeleteBehavior.Restrict); // Tránh vòng lặp Employee -> Dept -> Manager

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees) // Giả định class Department có ICollection<Employee> Employees
                .HasForeignKey(e => e.DeptId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Position)
                .WithMany(p => p.Employees) // Chỉ định rõ collection trong class Position
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.JobLevel)
                .WithMany(j => j.Employees)
                .HasForeignKey(e => e.JobLevelId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Manager)
                .WithMany()
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PositionJobLevelPolicy>()
                .HasOne(p => p.Position)
                .WithMany(p => p.JobLevelPolicies)
                .HasForeignKey(p => p.PositionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PositionJobLevelPolicy>()
                .HasOne(p => p.JobLevel)
                .WithMany(j => j.PositionPolicies)
                .HasForeignKey(p => p.JobLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Dependent>()
                .HasOne(d => d.Employee)
                .WithMany(e => e.Dependents)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DependentUpdateRequest>()
                .HasOne(d => d.Employee)
                .WithMany()
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DependentUpdateRequest>()
                .HasOne(d => d.Dependent)
                .WithMany()
                .HasForeignKey(d => d.DependentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ContractLegalSnapshot>()
                .HasOne(s => s.Contract)
                .WithMany(c => c.LegalSnapshots)
                .HasForeignKey(s => s.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ContractLegalSnapshot>()
                .HasOne(s => s.CreatedByAccount)
                .WithMany()
                .HasForeignKey(s => s.CreatedByAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ContractAddendum>()
                .HasOne(ca => ca.Contract)
                .WithMany(c => c.Addendums)
                .HasForeignKey(ca => ca.ContractId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ContractAddendum>()
                .HasMany(ca => ca.Details)
                .WithOne(d => d.ContractAddendum)
                .HasForeignKey(d => d.ContractAddendumId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MaternityLeave>()
                .HasOne(m => m.Employee)
                .WithMany()
                .HasForeignKey(m => m.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MaternityLeave>()
                .HasOne(m => m.LeaveRequest)
                .WithMany()
                .HasForeignKey(m => m.LeaveRequestId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<MaternityLeave>()
                .HasOne(m => m.ApprovedByAccount)
                .WithMany()
                .HasForeignKey(m => m.ApprovedByAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TerminationRequest>()
                .HasOne(t => t.Employee)
                .WithMany()
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TerminationRequest>()
                .HasOne(t => t.ApprovedByAccount)
                .WithMany()
                .HasForeignKey(t => t.ApprovedByAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FinalSettlement>()
                .HasOne(f => f.Employee)
                .WithMany()
                .HasForeignKey(f => f.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FinalSettlement>()
                .HasOne(f => f.TerminationRequest)
                .WithOne(t => t.FinalSettlement)
                .HasForeignKey<FinalSettlement>(f => f.TerminationRequestId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FinalSettlement>()
                .HasOne(f => f.ApprovedByAccount)
                .WithMany()
                .HasForeignKey(f => f.ApprovedByAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FinalSettlement>()
                .HasOne(f => f.LockedByAccount)
                .WithMany()
                .HasForeignKey(f => f.LockedByAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<EmploymentServicePeriod>()
                .HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- TASKS & TRAINING ---
            modelBuilder.Entity<KpiImportBatch>()
                .HasOne(b => b.Department)
                .WithMany()
                .HasForeignKey(b => b.DeptId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<KpiImportBatch>()
                .HasOne(b => b.ImportedByAccount)
                .WithMany()
                .HasForeignKey(b => b.ImportedByAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PerformanceReview>()
                .HasOne(pr => pr.Employee)
                .WithMany()
                .HasForeignKey(pr => pr.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict); // Không cho phép xóa Employee nếu đã có điểm đánh giá (Nên dùng soft-delete cho nhân viên)

            modelBuilder.Entity<PerformanceReview>()
                .HasOne(pr => pr.Department)
                .WithMany()
                .HasForeignKey(pr => pr.DeptId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PerformanceReview>()
                .HasOne(pr => pr.ImportBatch)
                .WithMany(b => b.Reviews)
                .HasForeignKey(pr => pr.ImportBatchId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PerformanceReview>()
                .HasOne(pr => pr.CreatedByAccount)
                .WithMany()
                .HasForeignKey(pr => pr.CreatedByAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PerformanceReview>()
                .HasOne(pr => pr.ReviewerAccount)
                .WithMany()
                .HasForeignKey(pr => pr.ReviewerAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PerformanceReview>()
                .HasMany(pr => pr.Details)
                .WithOne(pd => pd.Review)
                .HasForeignKey(pd => pd.ReviewId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa phiếu đánh giá thì xóa luôn các dòng chi tiết KPI

            modelBuilder.Entity<PenaltyRecord>()
                .HasOne(pr => pr.Employee)
                .WithMany()
                .HasForeignKey(pr => pr.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PenaltyRecord>()
                .HasOne(pr => pr.CreatedByAccount)
                .WithMany()
                .HasForeignKey(pr => pr.CreatedByAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PenaltyRecord>()
                .HasOne(pr => pr.ApprovedByAccount)
                .WithMany()
                .HasForeignKey(pr => pr.ApprovedByAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PenaltyRecord>()
                .HasOne(pr => pr.AttendanceAdjustmentLog)
                .WithMany()
                .HasForeignKey(pr => pr.AttendanceAdjustmentLogId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PenaltyRecord>()
                .HasOne(pr => pr.PerformanceReview)
                .WithMany()
                .HasForeignKey(pr => pr.PerformanceReviewId)
                .OnDelete(DeleteBehavior.SetNull);

            // --- PERSONNEL CHANGES ---
            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.RequestedByAccount)
                .WithMany()
                .HasForeignKey(p => p.RequestedByAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.CurrentDepartment)
                .WithMany()
                .HasForeignKey(p => p.CurrentDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.NewDepartment)
                .WithMany()
                .HasForeignKey(p => p.NewDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.CurrentPosition)
                .WithMany()
                .HasForeignKey(p => p.CurrentPositionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.NewPosition)
                .WithMany()
                .HasForeignKey(p => p.NewPositionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.CurrentManager)
                .WithMany()
                .HasForeignKey(p => p.CurrentManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.NewManager)
                .WithMany()
                .HasForeignKey(p => p.NewManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.CurrentJobLevel)
                .WithMany()
                .HasForeignKey(p => p.CurrentJobLevelId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.NewJobLevel)
                .WithMany()
                .HasForeignKey(p => p.NewJobLevelId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.DirectorApprovedByAccount)
                .WithMany()
                .HasForeignKey(p => p.DirectorApprovedByAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.HRAssignedAccount)
                .WithMany()
                .HasForeignKey(p => p.HRAssignedAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.RelatedContract)
                .WithMany()
                .HasForeignKey(p => p.RelatedContractId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.RelatedContractAddendum)
                .WithMany()
                .HasForeignKey(p => p.RelatedContractAddendumId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.RelatedFinalSettlement)
                .WithMany()
                .HasForeignKey(p => p.RelatedFinalSettlementId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.SourcePenaltyRecord)
                .WithMany()
                .HasForeignKey(p => p.SourcePenaltyRecordId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasOne(p => p.SourcePerformanceReview)
                .WithMany()
                .HasForeignKey(p => p.SourcePerformanceReviewId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasMany(p => p.Approvals)
                .WithOne(p => p.Request)
                .HasForeignKey(p => p.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasMany(p => p.Histories)
                .WithOne(p => p.Request)
                .HasForeignKey(p => p.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasMany(p => p.ContractLinks)
                .WithOne(p => p.PersonnelChangeRequest)
                .HasForeignKey(p => p.PersonnelChangeRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PersonnelChangeRequest>()
                .HasMany(p => p.RiskSnapshots)
                .WithOne(p => p.Request)
                .HasForeignKey(p => p.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PersonnelChangeApproval>()
                .HasOne(p => p.ApproverAccount)
                .WithMany()
                .HasForeignKey(p => p.ApproverAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeHistory>()
                .HasOne(p => p.ActorAccount)
                .WithMany()
                .HasForeignKey(p => p.ActorAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeContractLink>()
                .HasOne(p => p.Contract)
                .WithMany()
                .HasForeignKey(p => p.ContractId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeContractLink>()
                .HasOne(p => p.ContractAddendum)
                .WithMany()
                .HasForeignKey(p => p.ContractAddendumId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelChangeRiskSnapshot>()
                .HasOne(p => p.CreatedByAccount)
                .WithMany()
                .HasForeignKey(p => p.CreatedByAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WorkTask>()
                .HasOne(t => t.Department)
                .WithMany()
                .HasForeignKey(t => t.DeptId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkTask>()
                .HasOne(t => t.Assignee)
                .WithMany()
                .HasForeignKey(t => t.AssignedTo)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkTask>()
                .HasOne(t => t.CreatedByAccount)
                .WithMany()
                .HasForeignKey(t => t.CreatedByAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WorkTask>()
                .HasOne(t => t.Training)
                .WithMany(t => t.Tasks)
                .HasForeignKey(t => t.TrainingId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WorkTask>()
                .HasMany(t => t.Progresses)
                .WithOne(p => p.Task)
                .HasForeignKey(p => p.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkTask>()
                .HasMany(t => t.Feedbacks)
                .WithOne(f => f.Task)
                .HasForeignKey(f => f.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskProgress>()
                .HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskProgress>()
                .HasMany(p => p.Feedbacks)
                .WithOne(f => f.Progress)
                .HasForeignKey(f => f.ProgressId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TaskFeedback>()
                .HasOne(f => f.Reviewer)
                .WithMany()
                .HasForeignKey(f => f.ReviewerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Training>()
                .HasOne(t => t.Employee)
                .WithMany()
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Training>()
                .HasOne(t => t.Department)
                .WithMany()
                .HasForeignKey(t => t.DeptId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Training>()
                .HasOne(t => t.Manager)
                .WithMany()
                .HasForeignKey(t => t.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            // --- REQUESTS & HISTORY ---
            modelBuilder.Entity<EmploymentHistory>()
                .HasOne(eh => eh.Approver).WithMany()
                .HasForeignKey(eh => eh.ApprovedBy).OnDelete(DeleteBehavior.SetNull);

            // --- ACCOUNT & EMPLOYEE ---
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.AccountId)
                .IsUnique();

            // --- CANDIDATE & EMPLOYEE (khi trúng tuyển) ---
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.CandidateId)
                .IsUnique();

            modelBuilder.Entity<OvertimeRequest>()
                .HasOne(o => o.Employee)
                .WithMany()
                .HasForeignKey(o => o.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OvertimeRequest>()
                .HasMany(o => o.Segments)
                .WithOne(s => s.OvertimeRequest)
                .HasForeignKey(s => s.OvertimeRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OvertimeSegment>()
                .Property(s => s.OvertimeType)
                .HasDefaultValue(OvertimeType.Weekday);

            modelBuilder.Entity<AttendanceSummary>()
                .HasIndex(s => new { s.EmployeeId, s.Month, s.Year })
                .IsUnique();

            modelBuilder.Entity<AttendanceSummary>()
                .HasOne(s => s.Employee)
                .WithMany()
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AttendanceDailySummary>()
                .HasOne(s => s.Employee)
                .WithMany()
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AttendanceDailySummary>()
                .HasMany(s => s.AdjustmentLogs)
                .WithOne(l => l.AttendanceDailySummary)
                .HasForeignKey(l => l.AttendanceDailySummaryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payroll>()
                .HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payroll>()
                .HasMany(p => p.Details)
                .WithOne(d => d.Payroll)
                .HasForeignKey(d => d.PayrollId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payroll>()
                .HasMany(p => p.ContractSegments)
                .WithOne(s => s.Payroll)
                .HasForeignKey(s => s.PayrollId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PayrollContractSegment>()
                .HasOne(s => s.Employee)
                .WithMany()
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PayrollContractSegment>()
                .HasOne(s => s.Contract)
                .WithMany()
                .HasForeignKey(s => s.ContractId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PayrollAdjustment>()
                .HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PayrollAdjustment>()
                .HasOne(a => a.RelatedPayroll)
                .WithMany()
                .HasForeignKey(a => a.RelatedPayrollId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PayrollAdjustment>()
                .HasOne(a => a.AppliedPayroll)
                .WithMany()
                .HasForeignKey(a => a.AppliedPayrollId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<MonthlyInsuranceStatus>()
                .HasOne(s => s.Employee)
                .WithMany()
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MonthlyInsuranceStatus>()
                .Property(s => s.IsUnemploymentInsuranceContributed)
                .HasDefaultValue(true);

            modelBuilder.Entity<ExternalTimesheetImport>()
                .HasOne(i => i.ImportedByAccount)
                .WithMany()
                .HasForeignKey(i => i.ImportedByAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExternalTimesheetImport>()
                .HasMany(i => i.Lines)
                .WithOne(l => l.Import)
                .HasForeignKey(l => l.ImportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExternalTimesheetLine>()
                .HasOne(l => l.CollaboratorEmployee)
                .WithMany()
                .HasForeignKey(l => l.CollaboratorEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ExternalTimesheetLine>()
                .HasOne(l => l.Payroll)
                .WithMany()
                .HasForeignKey(l => l.PayrollId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProjectBonusImportBatch>()
                .HasOne(b => b.UploadedByAccount)
                .WithMany()
                .HasForeignKey(b => b.UploadedByAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjectBonusImportBatch>()
                .HasOne(b => b.ApprovedByAccount)
                .WithMany()
                .HasForeignKey(b => b.ApprovedByAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProjectBonusImportBatch>()
                .HasMany(b => b.Lines)
                .WithOne(l => l.Batch)
                .HasForeignKey(l => l.BatchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectBonusImportLine>()
                .HasOne(l => l.Employee)
                .WithMany()
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PayrollFormula>()
                .HasMany(f => f.Lines)
                .WithOne(l => l.PayrollFormula)
                .HasForeignKey(l => l.PayrollFormulaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PayrollFormulaLine>()
                .HasOne(l => l.SalaryComponentType)
                .WithMany()
                .HasForeignKey(l => l.SalaryComponentTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeSalaryComponent>()
                .HasOne(s => s.Employee)
                .WithMany()
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeSalaryComponent>()
                .HasOne(s => s.SalaryComponentType)
                .WithMany(t => t.EmployeeSalaryComponents)
                .HasForeignKey(s => s.SalaryComponentTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .Property(e => e.ResidenceStatus)
                .HasDefaultValue(ResidenceStatus.Resident);

            modelBuilder.Entity<Employee>()
                .Property(e => e.TaxCodeStatus)
                .HasDefaultValue(TaxCodeStatus.Unknown);

            modelBuilder.Entity<Contract>()
                .Property(c => c.PayBasis)
                .HasDefaultValue(PayBasis.Monthly);

            modelBuilder.Entity<Contract>()
                .Property(c => c.IsInsuranceEligible)
                .HasDefaultValue(true);

            modelBuilder.Entity<Contract>()
                .Property(c => c.StandardHoursPerDaySnapshot)
                .HasDefaultValue(8m);

            modelBuilder.Entity<Contract>()
                .Property(c => c.StandardWorkdaysSnapshot)
                .HasDefaultValue(22m);

            modelBuilder.Entity<PayrollFormula>()
                .Property(f => f.FormulaCode)
                .HasDefaultValue("DEFAULT_PAYROLL");

            modelBuilder.Entity<PayrollFormula>()
                .Property(f => f.Version)
                .HasDefaultValue(1);

            modelBuilder.Entity<PayrollFormula>()
                .Property(f => f.EffectiveFrom)
                .HasDefaultValue(new DateTime(2020, 7, 1));

            modelBuilder.Entity<PayrollFormula>()
                .Property(f => f.CreatedAt)
                .HasDefaultValue(new DateTime(2020, 7, 1));

            modelBuilder.Entity<WorkCalendarConfig>()
                .HasOne(w => w.Department)
                .WithMany()
                .HasForeignKey(w => w.DeptId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkCalendarConfig>()
                .HasOne(w => w.CompanyCalendar)
                .WithMany()
                .HasForeignKey(w => w.CompanyCalendarId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CompanyCalendar>()
                .HasMany(c => c.Days)
                .WithOne(d => d.Calendar)
                .HasForeignKey(d => d.CalendarId)
                .OnDelete(DeleteBehavior.Cascade);

            SeedPayrollPhaseOneReferenceData(modelBuilder);

            // Tự động chuyển đổi Enum thành String khi lưu vào DB
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    // Lấy kiểu dữ liệu gốc (xử lý cả trường hợp Enum?)
                    var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

                    if (type.IsEnum)
                    {
                        // Tạo Converter phù hợp với kiểu Enum cụ thể đó
                        var converterType = typeof(EnumToStringConverter<>).MakeGenericType(type);
                        var converter = (ValueConverter)Activator.CreateInstance(converterType)!;

                        property.SetValueConverter(converter);
                        property.SetColumnType("VARCHAR(50)");
                    }
                }
            }

            foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {

                property.SetColumnType("DECIMAL(15,2)");
            }

            // Payroll policy rates must keep four decimal places, e.g. 10.5% = 0.1050.
            modelBuilder.Entity<TaxConfig>().Property(t => t.FlatTaxRate).HasColumnType("DECIMAL(7,4)");
            modelBuilder.Entity<TaxConfig>().Property(t => t.NonResidentTaxRate).HasColumnType("DECIMAL(7,4)");
            modelBuilder.Entity<PITTaxBracket>().Property(t => t.TaxRate).HasColumnType("DECIMAL(7,4)");
            modelBuilder.Entity<InsuranceConfig>().Property(i => i.SocialInsuranceEmployeeRate).HasColumnType("DECIMAL(7,4)");
            modelBuilder.Entity<InsuranceConfig>().Property(i => i.HealthInsuranceEmployeeRate).HasColumnType("DECIMAL(7,4)");
            modelBuilder.Entity<InsuranceConfig>().Property(i => i.UnemploymentInsuranceEmployeeRate).HasColumnType("DECIMAL(7,4)");
            modelBuilder.Entity<InsuranceConfig>().Property(i => i.SocialInsuranceEmployerRate).HasColumnType("DECIMAL(7,4)");
            modelBuilder.Entity<InsuranceConfig>().Property(i => i.HealthInsuranceEmployerRate).HasColumnType("DECIMAL(7,4)");
            modelBuilder.Entity<InsuranceConfig>().Property(i => i.UnemploymentInsuranceEmployerRate).HasColumnType("DECIMAL(7,4)");
            modelBuilder.Entity<InsuranceConfig>().Property(i => i.UnionFeeEmployerRate).HasColumnType("DECIMAL(7,4)");
            modelBuilder.Entity<OvertimeSegment>().Property(o => o.RateMultiplierSnapshot).HasColumnType("DECIMAL(7,4)");
            modelBuilder.Entity<OvertimeRateConfig>().Property(o => o.BaseMultiplier).HasColumnType("DECIMAL(7,4)");
            modelBuilder.Entity<OvertimeRateConfig>().Property(o => o.NightAllowanceRate).HasColumnType("DECIMAL(7,4)");
            modelBuilder.Entity<OvertimeRateConfig>().Property(o => o.NightOvertimeExtraRate).HasColumnType("DECIMAL(7,4)");
            modelBuilder.Entity<AttendanceDailySummary>().Property(a => a.WorkdayValue).HasColumnType("DECIMAL(5,2)");
            modelBuilder.Entity<MonthlyInsuranceStatus>().Property(i => i.UnpaidLeaveWorkingDays).HasColumnType("DECIMAL(5,2)");
            modelBuilder.Entity<MonthlyInsuranceStatus>().Property(i => i.MaternityLeaveDays).HasColumnType("DECIMAL(5,2)");
            modelBuilder.Entity<MonthlyInsuranceStatus>().Property(i => i.SickLeaveDays).HasColumnType("DECIMAL(5,2)");
            modelBuilder.Entity<MonthlyInsuranceStatus>().Property(i => i.OfficialContractWorkingDays).HasColumnType("DECIMAL(5,2)");
            modelBuilder.Entity<PayrollContractSegment>().Property(s => s.SalaryPercentage).HasColumnType("DECIMAL(5,2)");
            modelBuilder.Entity<PayrollContractSegment>().Property(s => s.StandardWorkdays).HasColumnType("DECIMAL(5,2)");
            modelBuilder.Entity<PayrollContractSegment>().Property(s => s.ActualWorkdays).HasColumnType("DECIMAL(5,2)");
            modelBuilder.Entity<FinalSettlement>().Property(f => f.UnusedAnnualLeaveDays).HasColumnType("DECIMAL(5,2)");
            modelBuilder.Entity<ExternalTimesheetLine>().Property(l => l.ApprovedHours).HasColumnType("DECIMAL(7,2)");
        }

        private static void SeedPayrollPhaseOneReferenceData(ModelBuilder modelBuilder)
        {
            var pitEffectiveFrom = new DateTime(2020, 7, 1);
            var insuranceEffectiveFrom = new DateTime(2025, 7, 1);
            var pit2026EffectiveFrom = new DateTime(2026, 1, 1);
            var insurance2026EffectiveFrom = new DateTime(2026, 1, 1);

            modelBuilder.Entity<JobLevel>().HasData(
                new JobLevel { Id = 1, Code = "INTERN", Name = "Thực tập sinh", RankOrder = 1, IsManagementLevel = false, IsActive = true, CreatedAt = pitEffectiveFrom },
                new JobLevel { Id = 2, Code = "JUNIOR", Name = "Junior", RankOrder = 2, IsManagementLevel = false, IsActive = true, CreatedAt = pitEffectiveFrom },
                new JobLevel { Id = 3, Code = "MIDDLE", Name = "Middle", RankOrder = 3, IsManagementLevel = false, IsActive = true, CreatedAt = pitEffectiveFrom },
                new JobLevel { Id = 4, Code = "SENIOR", Name = "Senior", RankOrder = 4, IsManagementLevel = false, IsActive = true, CreatedAt = pitEffectiveFrom },
                new JobLevel { Id = 5, Code = "LEAD", Name = "Lead", RankOrder = 5, IsManagementLevel = true, IsActive = true, CreatedAt = pitEffectiveFrom },
                new JobLevel { Id = 6, Code = "MANAGER", Name = "Manager", RankOrder = 6, IsManagementLevel = true, IsActive = true, CreatedAt = pitEffectiveFrom },
                new JobLevel { Id = 7, Code = "DIRECTOR", Name = "Director", RankOrder = 7, IsManagementLevel = true, IsActive = true, CreatedAt = pitEffectiveFrom }
            );

            modelBuilder.Entity<SalaryComponentType>().HasData(
                new SalaryComponentType
                {
                    Id = 1,
                    Code = "BASE_SALARY_ACTUAL",
                    Name = "Lương cơ bản theo công",
                    ComponentGroup = SalaryComponentGroup.BaseSalary,
                    IsIncome = true,
                    IsDeduction = false,
                    IsTaxable = true,
                    IsInsuranceBased = true,
                    IsFixed = true,
                    IsAllowance = false,
                    IsBonus = false,
                    IsOvertime = false,
                    ProrationType = ProrationType.ByWorkingDays,
                    CalculationMethod = CalculationMethod.Formula,
                    EffectiveFrom = pitEffectiveFrom,
                    Version = 1,
                    IsActive = true,
                    CreatedAt = pitEffectiveFrom,
                    Note = "Base salary prorated by approved workdays."
                },
                new SalaryComponentType
                {
                    Id = 2,
                    Code = "POSITION_ALLOWANCE",
                    Name = "Phụ cấp chức vụ",
                    ComponentGroup = SalaryComponentGroup.Allowance,
                    IsIncome = true,
                    IsTaxable = true,
                    IsInsuranceBased = true,
                    IsFixed = true,
                    IsAllowance = true,
                    ProrationType = ProrationType.ByWorkingDays,
                    CalculationMethod = CalculationMethod.FixedAmount,
                    EffectiveFrom = pitEffectiveFrom,
                    Version = 1,
                    IsActive = true,
                    CreatedAt = pitEffectiveFrom,
                    Note = "Configured by PositionJobLevelPolicy when fixed and recurring."
                },
                new SalaryComponentType
                {
                    Id = 3,
                    Code = "RESPONSIBILITY_ALLOWANCE",
                    Name = "Phụ cấp trách nhiệm",
                    ComponentGroup = SalaryComponentGroup.Allowance,
                    IsIncome = true,
                    IsTaxable = true,
                    IsInsuranceBased = true,
                    IsFixed = true,
                    IsAllowance = true,
                    ProrationType = ProrationType.ByWorkingDays,
                    CalculationMethod = CalculationMethod.FixedAmount,
                    EffectiveFrom = pitEffectiveFrom,
                    Version = 1,
                    IsActive = true,
                    CreatedAt = pitEffectiveFrom
                },
                new SalaryComponentType
                {
                    Id = 4,
                    Code = "MEAL_ALLOWANCE",
                    Name = "Phụ cấp ăn ca",
                    ComponentGroup = SalaryComponentGroup.Allowance,
                    IsIncome = true,
                    IsTaxable = false,
                    IsInsuranceBased = false,
                    IsFixed = false,
                    IsAllowance = true,
                    ProrationType = ProrationType.FixedPerDay,
                    CalculationMethod = CalculationMethod.FixedPerDay,
                    TaxExemptCap = 730000m,
                    EffectiveFrom = pitEffectiveFrom,
                    Version = 1,
                    IsActive = true,
                    CreatedAt = pitEffectiveFrom,
                    Note = "Tax-exempt cap is stored as policy data and should be versioned when changed."
                },
                new SalaryComponentType
                {
                    Id = 5,
                    Code = "KPI_BONUS",
                    Name = "Mức thưởng KPI tối đa",
                    ComponentGroup = SalaryComponentGroup.Bonus,
                    IsIncome = true,
                    IsTaxable = true,
                    IsInsuranceBased = false,
                    IsBonus = true,
                    ProrationType = ProrationType.None,
                    CalculationMethod = CalculationMethod.Formula,
                    EffectiveFrom = pitEffectiveFrom,
                    Version = 1,
                    IsActive = true,
                    CreatedAt = pitEffectiveFrom
                },
                new SalaryComponentType
                {
                    Id = 6,
                    Code = "OT_BASE",
                    Name = "OT phần 100% chịu thuế",
                    ComponentGroup = SalaryComponentGroup.Overtime,
                    IsIncome = true,
                    IsTaxable = true,
                    IsInsuranceBased = false,
                    IsOvertime = true,
                    ProrationType = ProrationType.ByHours,
                    CalculationMethod = CalculationMethod.Formula,
                    EffectiveFrom = pitEffectiveFrom,
                    Version = 1,
                    IsActive = true,
                    CreatedAt = pitEffectiveFrom
                },
                new SalaryComponentType
                {
                    Id = 7,
                    Code = "OT_PREMIUM",
                    Name = "OT phần hệ số tăng thêm",
                    ComponentGroup = SalaryComponentGroup.Overtime,
                    IsIncome = true,
                    IsTaxable = false,
                    IsInsuranceBased = false,
                    IsOvertime = true,
                    ProrationType = ProrationType.ByHours,
                    CalculationMethod = CalculationMethod.Formula,
                    EffectiveFrom = pitEffectiveFrom,
                    Version = 1,
                    IsActive = true,
                    CreatedAt = pitEffectiveFrom
                },
                new SalaryComponentType
                {
                    Id = 8,
                    Code = "EMPLOYEE_INSURANCE",
                    Name = "Bảo hiểm người lao động đóng",
                    ComponentGroup = SalaryComponentGroup.Insurance,
                    IsIncome = false,
                    IsDeduction = true,
                    IsTaxable = false,
                    IsInsuranceBased = false,
                    ProrationType = ProrationType.None,
                    CalculationMethod = CalculationMethod.Formula,
                    EffectiveFrom = pitEffectiveFrom,
                    Version = 1,
                    IsActive = true,
                    CreatedAt = pitEffectiveFrom
                },
                new SalaryComponentType
                {
                    Id = 9,
                    Code = "PIT",
                    Name = "Thuế thu nhập cá nhân",
                    ComponentGroup = SalaryComponentGroup.Tax,
                    IsIncome = false,
                    IsDeduction = true,
                    IsTaxable = false,
                    IsInsuranceBased = false,
                    ProrationType = ProrationType.None,
                    CalculationMethod = CalculationMethod.Formula,
                    EffectiveFrom = pitEffectiveFrom,
                    Version = 1,
                    IsActive = true,
                    CreatedAt = pitEffectiveFrom
                },
                new SalaryComponentType
                {
                    Id = 10,
                    Code = "PAYROLL_ADJUSTMENT_TAXABLE_INSURANCE",
                    Name = "Truy lĩnh chịu thuế và tính bảo hiểm",
                    ComponentGroup = SalaryComponentGroup.Adjustment,
                    IsIncome = true,
                    IsTaxable = true,
                    IsInsuranceBased = true,
                    ProrationType = ProrationType.None,
                    CalculationMethod = CalculationMethod.Formula,
                    EffectiveFrom = pitEffectiveFrom,
                    Version = 1,
                    IsActive = true,
                    CreatedAt = pitEffectiveFrom
                },
                new SalaryComponentType
                {
                    Id = 11,
                    Code = "PAYROLL_ADJUSTMENT_TAXABLE",
                    Name = "Truy lĩnh/truy thu chịu thuế",
                    ComponentGroup = SalaryComponentGroup.Adjustment,
                    IsIncome = true,
                    IsTaxable = true,
                    IsInsuranceBased = false,
                    ProrationType = ProrationType.None,
                    CalculationMethod = CalculationMethod.Formula,
                    EffectiveFrom = pitEffectiveFrom,
                    Version = 1,
                    IsActive = true,
                    CreatedAt = pitEffectiveFrom
                },
                new SalaryComponentType
                {
                    Id = 12,
                    Code = "PAYROLL_ADJUSTMENT_NONTAXABLE",
                    Name = "Truy lĩnh/truy thu không chịu thuế",
                    ComponentGroup = SalaryComponentGroup.Adjustment,
                    IsIncome = true,
                    IsTaxable = false,
                    IsInsuranceBased = false,
                    ProrationType = ProrationType.None,
                    CalculationMethod = CalculationMethod.Formula,
                    EffectiveFrom = pitEffectiveFrom,
                    Version = 1,
                    IsActive = true,
                    CreatedAt = pitEffectiveFrom
                },
                new SalaryComponentType
                {
                    Id = 13,
                    Code = "PAYROLL_ADJUSTMENT_DEDUCTION",
                    Name = "Khoản truy thu/điều chỉnh khấu trừ",
                    ComponentGroup = SalaryComponentGroup.Adjustment,
                    IsIncome = false,
                    IsDeduction = true,
                    IsTaxable = false,
                    IsInsuranceBased = false,
                    ProrationType = ProrationType.None,
                    CalculationMethod = CalculationMethod.Formula,
                    EffectiveFrom = pitEffectiveFrom,
                    Version = 1,
                    IsActive = true,
                    CreatedAt = pitEffectiveFrom
                },
                new SalaryComponentType
                {
                    Id = 14,
                    Code = "EXTERNAL_TIMESHEET_PAY",
                    Name = "Thu nhập từ timesheet ngoài",
                    ComponentGroup = SalaryComponentGroup.BaseSalary,
                    IsIncome = true,
                    IsTaxable = true,
                    IsInsuranceBased = false,
                    IsFixed = false,
                    IsAllowance = false,
                    IsBonus = false,
                    IsOvertime = false,
                    ProrationType = ProrationType.ByHours,
                    CalculationMethod = CalculationMethod.Formula,
                    EffectiveFrom = pitEffectiveFrom,
                    Version = 1,
                    IsActive = true,
                    CreatedAt = pitEffectiveFrom,
                    Note = "Used for collaborators/freelancers imported from approved external timesheets."
                },
                new SalaryComponentType
                {
                    Id = 15,
                    Code = "PROJECT_BONUS",
                    Name = "Thưởng dự án",
                    ComponentGroup = SalaryComponentGroup.Bonus,
                    IsIncome = true,
                    IsDeduction = false,
                    IsTaxable = true,
                    IsInsuranceBased = false,
                    IsFixed = false,
                    IsAllowance = false,
                    IsBonus = true,
                    IsOvertime = false,
                    ProrationType = ProrationType.None,
                    CalculationMethod = CalculationMethod.Formula,
                    EffectiveFrom = pitEffectiveFrom,
                    Version = 1,
                    VersionCode = "PROJECT_BONUS_V1",
                    Status = PolicyVersionStatus.Active,
                    IsActive = true,
                    CreatedAt = pitEffectiveFrom,
                    Note = "Approved project bonus imported from ERP/accounting and included as taxable bonus income by default."
                }
            );

            modelBuilder.Entity<TaxConfig>().HasData(
                new TaxConfig
                {
                    Id = 1,
                    Code = "VN_PERSONAL_INCOME_TAX_2020",
                    Name = "Cấu hình thuế TNCN Việt Nam",
                    PersonalDeduction = 11000000m,
                    DependentDeduction = 4400000m,
                    FlatTaxThreshold = 2000000m,
                    FlatTaxRate = 0.10m,
                    NonResidentTaxRate = 0.20m,
                    EffectiveFrom = pitEffectiveFrom,
                    Version = 1,
                    VersionCode = "VN_PIT_2020",
                    Status = PolicyVersionStatus.Active,
                    SourceRef = "Vietnam PIT baseline 2020",
                    ActivatedAt = pitEffectiveFrom,
                    IsActive = true,
                    CreatedAt = pitEffectiveFrom,
                    Note = "Baseline PIT config. Update by creating a newer effective version."
                },
                new TaxConfig
                {
                    Id = 202601,
                    Code = "VN_PERSONAL_INCOME_TAX_2026",
                    Name = "Cấu hình thuế TNCN Việt Nam 2026",
                    PersonalDeduction = 15500000m,
                    DependentDeduction = 6200000m,
                    FlatTaxThreshold = 2000000m,
                    FlatTaxRate = 0.10m,
                    NonResidentTaxRate = 0.20m,
                    EffectiveFrom = pit2026EffectiveFrom,
                    Version = 2,
                    VersionCode = "VN_PIT_2026",
                    Status = PolicyVersionStatus.Active,
                    SourceRef = "Vietnam PIT family deduction policy 2026",
                    SupersedesVersionId = 1,
                    ActivatedAt = pit2026EffectiveFrom,
                    IsActive = true,
                    CreatedAt = pit2026EffectiveFrom,
                    Note = "PIT 2026 version. Keeps historical 2020 config available for older payroll periods."
                });

            modelBuilder.Entity<PITTaxBracket>().HasData(
                new PITTaxBracket { Id = 1, Code = "VN_PROGRESSIVE_PIT_2020", Level = 1, MinIncome = 0m, MaxIncome = 5000000m, TaxRate = 0.05m, QuickDeduction = 0m, EffectiveFrom = pitEffectiveFrom, Version = 1, VersionCode = "VN_PIT_BRACKET_2020", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam PIT progressive brackets baseline 2020", ActivatedAt = pitEffectiveFrom, IsActive = true, CreatedAt = pitEffectiveFrom },
                new PITTaxBracket { Id = 2, Code = "VN_PROGRESSIVE_PIT_2020", Level = 2, MinIncome = 5000000m, MaxIncome = 10000000m, TaxRate = 0.10m, QuickDeduction = 250000m, EffectiveFrom = pitEffectiveFrom, Version = 1, VersionCode = "VN_PIT_BRACKET_2020", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam PIT progressive brackets baseline 2020", ActivatedAt = pitEffectiveFrom, IsActive = true, CreatedAt = pitEffectiveFrom },
                new PITTaxBracket { Id = 3, Code = "VN_PROGRESSIVE_PIT_2020", Level = 3, MinIncome = 10000000m, MaxIncome = 18000000m, TaxRate = 0.15m, QuickDeduction = 750000m, EffectiveFrom = pitEffectiveFrom, Version = 1, VersionCode = "VN_PIT_BRACKET_2020", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam PIT progressive brackets baseline 2020", ActivatedAt = pitEffectiveFrom, IsActive = true, CreatedAt = pitEffectiveFrom },
                new PITTaxBracket { Id = 4, Code = "VN_PROGRESSIVE_PIT_2020", Level = 4, MinIncome = 18000000m, MaxIncome = 32000000m, TaxRate = 0.20m, QuickDeduction = 1650000m, EffectiveFrom = pitEffectiveFrom, Version = 1, VersionCode = "VN_PIT_BRACKET_2020", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam PIT progressive brackets baseline 2020", ActivatedAt = pitEffectiveFrom, IsActive = true, CreatedAt = pitEffectiveFrom },
                new PITTaxBracket { Id = 5, Code = "VN_PROGRESSIVE_PIT_2020", Level = 5, MinIncome = 32000000m, MaxIncome = 52000000m, TaxRate = 0.25m, QuickDeduction = 3250000m, EffectiveFrom = pitEffectiveFrom, Version = 1, VersionCode = "VN_PIT_BRACKET_2020", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam PIT progressive brackets baseline 2020", ActivatedAt = pitEffectiveFrom, IsActive = true, CreatedAt = pitEffectiveFrom },
                new PITTaxBracket { Id = 6, Code = "VN_PROGRESSIVE_PIT_2020", Level = 6, MinIncome = 52000000m, MaxIncome = 80000000m, TaxRate = 0.30m, QuickDeduction = 5850000m, EffectiveFrom = pitEffectiveFrom, Version = 1, VersionCode = "VN_PIT_BRACKET_2020", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam PIT progressive brackets baseline 2020", ActivatedAt = pitEffectiveFrom, IsActive = true, CreatedAt = pitEffectiveFrom },
                new PITTaxBracket { Id = 7, Code = "VN_PROGRESSIVE_PIT_2020", Level = 7, MinIncome = 80000000m, MaxIncome = null, TaxRate = 0.35m, QuickDeduction = 9850000m, EffectiveFrom = pitEffectiveFrom, Version = 1, VersionCode = "VN_PIT_BRACKET_2020", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam PIT progressive brackets baseline 2020", ActivatedAt = pitEffectiveFrom, IsActive = true, CreatedAt = pitEffectiveFrom },
                new PITTaxBracket { Id = 20260101, Code = "VN_PROGRESSIVE_PIT_2026", Level = 1, MinIncome = 0m, MaxIncome = 5000000m, TaxRate = 0.05m, QuickDeduction = 0m, EffectiveFrom = pit2026EffectiveFrom, Version = 2, VersionCode = "VN_PIT_BRACKET_2026", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam PIT progressive brackets 2026", SupersedesVersionId = 1, ActivatedAt = pit2026EffectiveFrom, IsActive = true, CreatedAt = pit2026EffectiveFrom },
                new PITTaxBracket { Id = 20260102, Code = "VN_PROGRESSIVE_PIT_2026", Level = 2, MinIncome = 5000000m, MaxIncome = 10000000m, TaxRate = 0.10m, QuickDeduction = 250000m, EffectiveFrom = pit2026EffectiveFrom, Version = 2, VersionCode = "VN_PIT_BRACKET_2026", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam PIT progressive brackets 2026", SupersedesVersionId = 2, ActivatedAt = pit2026EffectiveFrom, IsActive = true, CreatedAt = pit2026EffectiveFrom },
                new PITTaxBracket { Id = 20260103, Code = "VN_PROGRESSIVE_PIT_2026", Level = 3, MinIncome = 10000000m, MaxIncome = 18000000m, TaxRate = 0.15m, QuickDeduction = 750000m, EffectiveFrom = pit2026EffectiveFrom, Version = 2, VersionCode = "VN_PIT_BRACKET_2026", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam PIT progressive brackets 2026", SupersedesVersionId = 3, ActivatedAt = pit2026EffectiveFrom, IsActive = true, CreatedAt = pit2026EffectiveFrom },
                new PITTaxBracket { Id = 20260104, Code = "VN_PROGRESSIVE_PIT_2026", Level = 4, MinIncome = 18000000m, MaxIncome = 32000000m, TaxRate = 0.20m, QuickDeduction = 1650000m, EffectiveFrom = pit2026EffectiveFrom, Version = 2, VersionCode = "VN_PIT_BRACKET_2026", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam PIT progressive brackets 2026", SupersedesVersionId = 4, ActivatedAt = pit2026EffectiveFrom, IsActive = true, CreatedAt = pit2026EffectiveFrom },
                new PITTaxBracket { Id = 20260105, Code = "VN_PROGRESSIVE_PIT_2026", Level = 5, MinIncome = 32000000m, MaxIncome = 52000000m, TaxRate = 0.25m, QuickDeduction = 3250000m, EffectiveFrom = pit2026EffectiveFrom, Version = 2, VersionCode = "VN_PIT_BRACKET_2026", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam PIT progressive brackets 2026", SupersedesVersionId = 5, ActivatedAt = pit2026EffectiveFrom, IsActive = true, CreatedAt = pit2026EffectiveFrom },
                new PITTaxBracket { Id = 20260106, Code = "VN_PROGRESSIVE_PIT_2026", Level = 6, MinIncome = 52000000m, MaxIncome = 80000000m, TaxRate = 0.30m, QuickDeduction = 5850000m, EffectiveFrom = pit2026EffectiveFrom, Version = 2, VersionCode = "VN_PIT_BRACKET_2026", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam PIT progressive brackets 2026", SupersedesVersionId = 6, ActivatedAt = pit2026EffectiveFrom, IsActive = true, CreatedAt = pit2026EffectiveFrom },
                new PITTaxBracket { Id = 20260107, Code = "VN_PROGRESSIVE_PIT_2026", Level = 7, MinIncome = 80000000m, MaxIncome = null, TaxRate = 0.35m, QuickDeduction = 9850000m, EffectiveFrom = pit2026EffectiveFrom, Version = 2, VersionCode = "VN_PIT_BRACKET_2026", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam PIT progressive brackets 2026", SupersedesVersionId = 7, ActivatedAt = pit2026EffectiveFrom, IsActive = true, CreatedAt = pit2026EffectiveFrom }
            );

            modelBuilder.Entity<InsuranceConfig>().HasData(
                new InsuranceConfig
                {
                    Id = 1,
                    Code = "VN_STANDARD_INSURANCE_2025",
                    Name = "Cấu hình bảo hiểm Việt Nam",
                    SocialInsuranceEmployeeRate = 0.08m,
                    HealthInsuranceEmployeeRate = 0.015m,
                    UnemploymentInsuranceEmployeeRate = 0.01m,
                    SocialInsuranceEmployerRate = 0.175m,
                    HealthInsuranceEmployerRate = 0.03m,
                    UnemploymentInsuranceEmployerRate = 0.01m,
                    UnionFeeEmployerRate = 0.02m,
                    MinInsuranceSalary = null,
                    MaxInsuranceSalary = null,
                    UnpaidLeaveNoContributionThresholdDays = 14,
                    MinContractMonthsForContribution = 1,
                    EffectiveFrom = insuranceEffectiveFrom,
                    Version = 1,
                    VersionCode = "VN_INSURANCE_2025",
                    Status = PolicyVersionStatus.Active,
                    SourceRef = "Vietnam insurance baseline 2025",
                    ActivatedAt = insuranceEffectiveFrom,
                    IsActive = true,
                    CreatedAt = insuranceEffectiveFrom,
                    Note = "Baseline insurance config for payroll engine. Salary caps should be updated by policy version when needed."
                },
                new InsuranceConfig
                {
                    Id = 202601,
                    Code = "VN_STANDARD_INSURANCE_2026",
                    Name = "Cấu hình bảo hiểm Việt Nam 2026",
                    SocialInsuranceEmployeeRate = 0.08m,
                    HealthInsuranceEmployeeRate = 0.015m,
                    UnemploymentInsuranceEmployeeRate = 0.01m,
                    SocialInsuranceEmployerRate = 0.175m,
                    HealthInsuranceEmployerRate = 0.03m,
                    UnemploymentInsuranceEmployerRate = 0.01m,
                    UnionFeeEmployerRate = 0.02m,
                    MinInsuranceSalary = null,
                    MaxInsuranceSalary = null,
                    UnpaidLeaveNoContributionThresholdDays = 14,
                    MinContractMonthsForContribution = 1,
                    EffectiveFrom = insurance2026EffectiveFrom,
                    Version = 2,
                    VersionCode = "VN_INSURANCE_2026",
                    Status = PolicyVersionStatus.Active,
                    SourceRef = "Vietnam insurance policy 2026",
                    SupersedesVersionId = 1,
                    ActivatedAt = insurance2026EffectiveFrom,
                    IsActive = true,
                    CreatedAt = insurance2026EffectiveFrom,
                    Note = "Insurance 2026 version. Minimum wage region policies are tracked separately for cap review."
                });

            modelBuilder.Entity<OvertimeRateConfig>().HasData(
                new OvertimeRateConfig { Id = 1, Code = "VN_OT_WEEKDAY_2020", OvertimeType = OvertimeType.Weekday, BaseMultiplier = 1.5m, NightAllowanceRate = 0m, NightOvertimeExtraRate = 0m, EffectiveFrom = pitEffectiveFrom, Version = 1, VersionCode = "VN_OT_2020", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam overtime baseline 2020", ActivatedAt = pitEffectiveFrom, IsActive = true, CreatedAt = pitEffectiveFrom, Note = "Baseline weekday OT multiplier." },
                new OvertimeRateConfig { Id = 2, Code = "VN_OT_WEEKEND_2020", OvertimeType = OvertimeType.Weekend, BaseMultiplier = 2.0m, NightAllowanceRate = 0m, NightOvertimeExtraRate = 0m, EffectiveFrom = pitEffectiveFrom, Version = 1, VersionCode = "VN_OT_2020", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam overtime baseline 2020", ActivatedAt = pitEffectiveFrom, IsActive = true, CreatedAt = pitEffectiveFrom, Note = "Baseline weekly rest day OT multiplier." },
                new OvertimeRateConfig { Id = 3, Code = "VN_OT_HOLIDAY_2020", OvertimeType = OvertimeType.Holiday, BaseMultiplier = 3.0m, NightAllowanceRate = 0m, NightOvertimeExtraRate = 0m, EffectiveFrom = pitEffectiveFrom, Version = 1, VersionCode = "VN_OT_2020", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam overtime baseline 2020", ActivatedAt = pitEffectiveFrom, IsActive = true, CreatedAt = pitEffectiveFrom, Note = "Baseline public holiday OT multiplier." },
                new OvertimeRateConfig { Id = 4, Code = "VN_OT_WEEKDAY_NIGHT_2020", OvertimeType = OvertimeType.WeekdayNight, BaseMultiplier = 1.5m, NightAllowanceRate = 0.3m, NightOvertimeExtraRate = 0.2m, EffectiveFrom = pitEffectiveFrom, Version = 1, VersionCode = "VN_OT_2020", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam overtime baseline 2020", ActivatedAt = pitEffectiveFrom, IsActive = true, CreatedAt = pitEffectiveFrom, Note = "Baseline weekday night OT config." },
                new OvertimeRateConfig { Id = 5, Code = "VN_OT_WEEKEND_NIGHT_2020", OvertimeType = OvertimeType.WeekendNight, BaseMultiplier = 2.0m, NightAllowanceRate = 0.3m, NightOvertimeExtraRate = 0.2m, EffectiveFrom = pitEffectiveFrom, Version = 1, VersionCode = "VN_OT_2020", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam overtime baseline 2020", ActivatedAt = pitEffectiveFrom, IsActive = true, CreatedAt = pitEffectiveFrom, Note = "Baseline weekend night OT config." },
                new OvertimeRateConfig { Id = 6, Code = "VN_OT_HOLIDAY_NIGHT_2020", OvertimeType = OvertimeType.HolidayNight, BaseMultiplier = 3.0m, NightAllowanceRate = 0.3m, NightOvertimeExtraRate = 0.2m, EffectiveFrom = pitEffectiveFrom, Version = 1, VersionCode = "VN_OT_2020", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam overtime baseline 2020", ActivatedAt = pitEffectiveFrom, IsActive = true, CreatedAt = pitEffectiveFrom, Note = "Baseline holiday night OT config." }
            );

            modelBuilder.Entity<PayrollPolicy>().HasData(
                new PayrollPolicy { Id = 20260101, PolicyType = PayrollPolicyType.MinimumWage, Code = "VN_MIN_WAGE_REGION_1_2026", Name = "Lương tối thiểu vùng I 2026", ValueType = PayrollPolicyValueType.Amount, Amount = 5310000m, EffectiveFrom = insurance2026EffectiveFrom, Version = 1, VersionCode = "VN_MIN_WAGE_2026", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam regional minimum wage 2026", ActivatedAt = insurance2026EffectiveFrom, IsActive = true, CreatedAt = insurance2026EffectiveFrom, Description = "Theo dõi lương tối thiểu vùng để đối chiếu trần/sàn chính sách bảo hiểm và lương." },
                new PayrollPolicy { Id = 20260102, PolicyType = PayrollPolicyType.MinimumWage, Code = "VN_MIN_WAGE_REGION_2_2026", Name = "Lương tối thiểu vùng II 2026", ValueType = PayrollPolicyValueType.Amount, Amount = 4730000m, EffectiveFrom = insurance2026EffectiveFrom, Version = 1, VersionCode = "VN_MIN_WAGE_2026", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam regional minimum wage 2026", ActivatedAt = insurance2026EffectiveFrom, IsActive = true, CreatedAt = insurance2026EffectiveFrom, Description = "Theo dõi lương tối thiểu vùng để đối chiếu trần/sàn chính sách bảo hiểm và lương." },
                new PayrollPolicy { Id = 20260103, PolicyType = PayrollPolicyType.MinimumWage, Code = "VN_MIN_WAGE_REGION_3_2026", Name = "Lương tối thiểu vùng III 2026", ValueType = PayrollPolicyValueType.Amount, Amount = 4140000m, EffectiveFrom = insurance2026EffectiveFrom, Version = 1, VersionCode = "VN_MIN_WAGE_2026", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam regional minimum wage 2026", ActivatedAt = insurance2026EffectiveFrom, IsActive = true, CreatedAt = insurance2026EffectiveFrom, Description = "Theo dõi lương tối thiểu vùng để đối chiếu trần/sàn chính sách bảo hiểm và lương." },
                new PayrollPolicy { Id = 20260104, PolicyType = PayrollPolicyType.MinimumWage, Code = "VN_MIN_WAGE_REGION_4_2026", Name = "Lương tối thiểu vùng IV 2026", ValueType = PayrollPolicyValueType.Amount, Amount = 3700000m, EffectiveFrom = insurance2026EffectiveFrom, Version = 1, VersionCode = "VN_MIN_WAGE_2026", Status = PolicyVersionStatus.Active, SourceRef = "Vietnam regional minimum wage 2026", ActivatedAt = insurance2026EffectiveFrom, IsActive = true, CreatedAt = insurance2026EffectiveFrom, Description = "Theo dõi lương tối thiểu vùng để đối chiếu trần/sàn chính sách bảo hiểm và lương." },
                new PayrollPolicy { Id = 20260601, PolicyType = PayrollPolicyType.KpiBonus, Code = "HICAS_KPI_BONUS_2026", Name = "Quy chế thưởng KPI HICAS 2026", ValueType = PayrollPolicyValueType.Formula, FormulaJson = "{\"kpiBonusTargetSource\":\"EmployeeSalaryComponent.KPI_BONUS\",\"scoreFormula\":\"Điểm KPI chính thức = tổng max(0, trọng số KPI * điểm trưởng phòng / 100 - điểm trừ).\",\"payoutFormula\":\"Thưởng KPI thực nhận = mức thưởng KPI tối đa * điểm KPI / 100.\",\"eligibilityRule\":\"Người lao động chỉ nhận thưởng KPI khi kết quả KPI kỳ đó đã được chốt, không thuộc trường hợp bị hủy hoặc không áp dụng theo quy chế lương thưởng và quyết định kỷ luật liên quan.\",\"paymentPeriod\":\"Chi trả theo kỳ lương sau khi kết quả KPI được chốt và bảng lương được phê duyệt.\",\"approverRole\":\"Trưởng phòng chốt điểm KPI; HR kiểm tra chính sách; Giám đốc phê duyệt bảng lương.\"}", EffectiveFrom = new DateTime(2026, 6, 1), Version = 1, VersionCode = "HICAS_KPI_BONUS_2026_V1", Status = PolicyVersionStatus.Active, SourceRef = "HICAS compensation policy 2026", ActivatedAt = new DateTime(2026, 6, 1), IsActive = true, CreatedAt = new DateTime(2026, 6, 1), Description = "Lưu quy chế thưởng KPI theo version. Hợp đồng chỉ viện dẫn nguyên tắc; thay đổi công thức tạo version mới, không cần ký lại phụ lục từng lần." }
            );
        }
        

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var userIdClaim = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            int? currentUserId = int.TryParse(userIdClaim, out int id) ? id : null;

            // Bắt các bản ghi bị thay đổi (Bỏ qua bảng AuditLog và MfaRecoveryCode)
            var modifiedEntries = ChangeTracker.Entries()
                .Where(e => e.Entity is not AuditLog && e.Entity is not MfaRecoveryCode &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();

            var auditEntries = new List<AuditLog>();

            foreach (var entry in modifiedEntries)
            {
                var auditLog = new AuditLog
                {
                    TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                    AccountId = currentUserId,
                    ActionType = entry.State.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();
                var affectedColumns = new List<string>();

                foreach (var property in entry.Properties)
                {
                    if (property.IsTemporary) continue;

                    string propertyName = property.Metadata.Name;

                    // BẢO MẬT: Bỏ qua không ghi log các trường nhạy cảm
                    if (SENSITIVE_FIELDS.Contains(propertyName)) continue;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            newValues[propertyName] = property.CurrentValue;
                            affectedColumns.Add(propertyName);
                            break;

                        case EntityState.Deleted:
                            oldValues[propertyName] = property.OriginalValue;
                            affectedColumns.Add(propertyName);
                            break;

                        case EntityState.Modified:
                            // TỐI ƯU: Chỉ ghi nhận nếu giá trị thực sự có sự thay đổi
                            if (!Equals(property.OriginalValue, property.CurrentValue))
                            {
                                oldValues[propertyName] = property.OriginalValue;
                                newValues[propertyName] = property.CurrentValue;
                                affectedColumns.Add(propertyName);
                            }
                            break;
                    }
                }

                // CHỈ LƯU LOG NẾU CÓ ÍT NHẤT 1 TRƯỜNG DỮ LIỆU ĐƯỢC CẬP NHẬT
                if (affectedColumns.Count > 0)
                {
                    auditLog.OldValues = oldValues.Any() ? JsonSerializer.Serialize(oldValues) : null;
                    auditLog.NewValues = newValues.Any() ? JsonSerializer.Serialize(newValues) : null;
                    auditLog.AffectedColumns = JsonSerializer.Serialize(affectedColumns);

                    auditEntries.Add(auditLog);
                }
            }

            if (auditEntries.Any())
            {
                await AuditLogs.AddRangeAsync(auditEntries, cancellationToken);
            }

            await NormalizeAuditLogAccountIdsAsync(cancellationToken);

            return await base.SaveChangesAsync(cancellationToken);
        }

        private async Task NormalizeAuditLogAccountIdsAsync(CancellationToken cancellationToken)
        {
            var auditLogEntries = ChangeTracker.Entries<AuditLog>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .ToList();

            if (!auditLogEntries.Any())
                return;

            foreach (var entry in auditLogEntries.Where(e => e.Entity.AccountId.HasValue && e.Entity.AccountId.Value <= 0))
            {
                entry.Entity.AccountId = null;
            }

            var accountIds = auditLogEntries
                .Select(e => e.Entity.AccountId)
                .Where(id => id.HasValue && id.Value > 0)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (!accountIds.Any())
                return;

            var existingAccountIds = await Accounts
                .Where(a => accountIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync(cancellationToken);

            var existingSet = existingAccountIds.ToHashSet();
            foreach (var entry in auditLogEntries.Where(e => e.Entity.AccountId.HasValue && !existingSet.Contains(e.Entity.AccountId.Value)))
            {
                entry.Entity.AccountId = null;
            }
        }
    }
}
