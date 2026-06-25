namespace HRM.backend.src.HRM.Core.Enums
{
    // ==========================================
    // MODULE 1: System
    // ==========================================
    public enum AccountStatus { Active, Inactive, Locked, Suspended }

    // ==========================================
    // MODULE 2 & 3: Organization & Recruitment
    // ==========================================
    public enum DeptStatus { Active, Dissolved }
    public enum RecruitmentRequestStatus { PendingHR, PendingDirector, Approved, Rejected, Closed }
    public enum CandidateStatus { New, Interview_Pending, Interview_Passed, Offer, Hired, Rejected, SLA_Expired }

    // ==========================================
    // MODULE 4: Employee & Contract
    // ==========================================
    public enum Gender { Male, Female, Other }
    public enum EmployeeStatus { Probation, Official, OnMaternityLeave, Resigned, Terminated, Dismissed }
    public enum ContractType { Probation, Definite, Indefinite, PartTime }
    public enum ContractLegalDocumentType { ProbationContract, FixedTermLaborContract, IndefiniteTermLaborContract, ContractAddendum }
    public enum ContractChangeFlowType { Addendum, NewContract, PolicyUpdate, TemporaryDecision, NotAllowed }
    public enum ContractChangeType
    {
        BasicSalaryChange,
        InsuranceSalaryChange,
        DepartmentChangePermanent,
        PositionChangePermanent,
        JobLevelChangePermanent,
        EmployeeTypeChangePermanent,
        ContractEndDateChange,
        ContractStartDateChange,
        ContractTypeChange,
        RenewExpiredContract,
        KpiFormulaChange,
        PayrollPolicyChange,
        TemporaryDepartmentAssignment,
        TemporaryPositionAssignment,
        Unknown
    }
    public enum ContractStatus
    {
        Draft,
        PendingDept,
        PendingHR,
        Negotiating,
        PendingDirector,
        ApprovedByDirector,
        Rejected,
        Active,
        Liquidating,
        Expired,
        Draft_Cancelled,
        PendingManagerContentReview,
        PendingEmployee,
        PendingHRRevision
    }

    public enum DependentRelation { Child, Parent, Spouse, Other }
    public enum EmployeeType { Intern, Official, Probation, PartTime, Contractual }
    public enum OnboardingStatus
    {
        Pending_HR,
        Completed,
        Rejected,
        PendingCandidateProfile
    }

    public enum PenaltySourceType
    {
        Attendance,
        Leave,
        SLA,
        KPI,
        Task,
        Manual
    }

    public enum PenaltyRecordStatus
    {
        Draft,
        PendingEmployeeExplanation,
        PendingHRReview,
        PendingDirectorApproval,
        Approved,
        Rejected,
        Applied,
        Cancelled
    }

    public enum ViolationType
    {
        AttendanceLate,
        EarlyLeave,
        UnauthorizedAbsence,
        LeftWorkplace,
        NotAtWorkLocation,
        SlaMissed,
        TaskMissed,
        ProcessViolation,
        KpiManualAdjustment,
        TrainingEvaluationSla,
        Manual
    }

    public enum PenaltySeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum AttendanceAdjustmentImpactType
    {
        None,
        DeductWorkedMinutes,
        DeductPayableHours,
        DeductWorkday,
        MarkAbsence,
        MarkUnpaidLeave,
        ManualAdjusted
    }

    // ==========================================
    // MODULE 5: Time & Attendance
    // ==========================================
    public enum AttendanceStatus { Valid, Invalid, Late, Early, OnLeave }
    public enum AttendanceDailyStatus
    {
        Present,
        HalfDay,
        PaidLeave,
        UnpaidLeave,
        Absence,
        Holiday,
        Weekend,
        MaternityLeave,
        SickLeave,
        ManualAdjusted
    }
    public enum AttendancePayrollApprovalStatus { Draft, PendingHRReview, Approved, Rejected, Locked }
    public enum CompanyCalendarDayType
    {
        PublicHoliday,
        CompanyHoliday,
        CompensatoryWorkingDay,
        CompensatoryDayOff,
        SpecialPaidLeave,
        UnpaidCompanyClosure
    }
    public enum LeaveCategory
    {
        AnnualPaid,
        Unpaid,
        Sick,
        Maternity,
        SpecialPaid
    }
    public enum LeaveRequestStatus
    {
        Pending,
        Approved,
        Rejected,
        Auto_Approved,
        PendingDept,
        PendingDirector,
        RejectedByDept,
        RejectedByDirector,
        AutoDeptApproved,
        AutoFinalApproved,
        PendingHR,
        RejectedByHR
    }
    public enum OvertimeRequestStatus
    {
        PendingManager = 0,
        PendingHR = 1,
        Approved = 2,
        Rejected = 3,
        Cancelled = 4,
        PendingDirector = 5,
        Reconciled = 6,
        PayrollLocked = 7
    }

    // ==========================================
    // MODULE 6: Tasks & Training
    // ==========================================
    public enum ReviewStatus
    {
        Draft,
        PendingEmployeeUpdate,
        PendingEvaluation,
        ReworkRequired,
        Evaluated,
        AutoEvaluated,
        Approved,
        Rejected
    }

    // 2. Dùng cho bảng Training (Đào tạo thực tập sinh)
    public enum TrainingStatus
    {
        InProgress,
        Extended,
        Completed,
        PendingEvaluation,
        Evaluated,
        AutoCompleted,
        Failed,
        Overdue,
        Cancelled
    }
    public enum TaskType { Project, SelfStudy, Research } // Bỏ dấu gạch ngang của "Self-Study" để hợp lệ C#
    public enum TaskStatus
    {
        Todo,
        Doing,
        Done,
        Assigned,
        InProgress,
        PendingReview,
        ReworkRequired,
        Completed,
        AutoApproved,
        Overdue,
        Cancelled
    }

    public enum TaskFeedbackType
    {
        Comment,
        ReworkRequest,
        Approved,
        Rejected,
        AutoApproved
    }

    public enum ImportBatchStatus
    {
        Processing,
        Completed,
        Failed
    }


    // ==========================================
    // MODULE 7: Payroll
    // ==========================================
    public enum FormulaStatus
    {
        Pending,
        Approved,
        Rejected,
        Expired,
        Draft,
        PendingDirectorApproval,
        RevisionRequired,
        Active,
        Archived
    }
    public enum PayrollStatus { Draft, Calculated, HRReviewed, PendingApproval, Approved, Locked, Finalized, Paid, Cancelled, RevisionRequired, Rejected }
    public enum SalaryVariableDataType { Number, Money, Hours, Days, Percent }
    public enum SalaryAggregationType { Latest, Sum, Count, MonthlyTotal, Manual }
    public enum PayrollPolicyType { Overtime, PitTax, Insurance, Allowance, Deduction, Seniority, MinimumWage, KpiBonus }
    public enum PayrollPolicyValueType { RatePercent, Amount, Bracket, Formula }
    public enum PolicyVersionStatus { Draft, Active, Archived }
    public enum PayrollAccessScope { All, Department, Individual }
    public enum PayBasis { Monthly, Daily, Hourly, FixedPackage }
    public enum TaxMethod { Progressive, Flat10Percent, NonResident20Percent, None }
    public enum ResidenceStatus { Resident, NonResident }
    public enum TaxCodeStatus { Unknown, NotRegistered, Registered, Invalid }
    public enum ProrationType { None, ByWorkingDays, ByAttendanceDays, ByCalendarDays, ByHours, FixedPerDay }
    public enum CalculationMethod { FixedAmount, FixedPerDay, FixedPerHour, PercentOfBaseSalary, Formula }
    public enum SalaryComponentGroup { BaseSalary, Allowance, Bonus, Overtime, Insurance, Tax, Deduction, Adjustment }
    public enum PayrollAdjustmentType { RetroactiveSalaryIncrease, RetroactiveAllowance, InsuranceArrears, TaxAdjustment, ManualCorrection, Other }
    public enum PayrollAdjustmentStatus { Draft, Approved, Rejected, Applied, Cancelled }
    public enum InsuranceContributionStatus { Pending, Contributed, NotContributed }
    public enum PayrollContractSegmentType { Contract, Addendum, ManualAdjustment }
    public enum OvertimeType { Weekday, Weekend, Holiday, Night, WeekdayNight, WeekendNight, HolidayNight }
    public enum MaternityLeaveStatus { Draft, PendingApproval, Approved, Active, Completed, Cancelled }
    public enum TerminationType { Resignation, ContractExpired, MutualAgreement, TerminationByCompany, Dismissal, RedundancyOrRestructure, Retirement }
    public enum TerminationLegalStatus { PendingHrReview, Lawful, LawfulNoNotice, UnlawfulNoNotice, UnlawfulInsufficientNotice }
    public enum TerminationRequestStatus { Draft, PendingHR, PendingDirector, Approved, Rejected, Cancelled, Settled }
    public enum FinalSettlementStatus { Draft, Calculated, PendingApproval, Approved, Locked, Paid, Cancelled }
    public enum EmploymentServicePeriodType { Probation, OfficialWork, UnpaidLeave, MaternityLeave, SickLeave, Suspension, UnemploymentInsurance, PriorSeverancePaid }
    public enum ExternalTimesheetImportStatus { Draft, Imported, Validated, Approved, Rejected, PayrollImported, Cancelled }
    public enum ProjectBonusImportStatus { Draft, PendingReview, Approved, Rejected, Cancelled }
    public enum ProjectBonusLineValidationStatus { Pending, Valid, Invalid }
    public enum ContractAddendumType { SalaryAdjustment, Extension, InternalTransfer, SeniorAppointment, Other }
    public enum ContractAddendumDetailValueType { Text, Money, Date, Number, Json, Boolean }

    // ==========================================
    // MODULE 7: Personnel Changes
    // ==========================================
    public enum PersonnelChangeType
    {
        ConvertToOfficial,
        Promotion,
        SeniorAppointment,
        VoluntaryTermination,
        Dismissal,
        InternalTransfer
    }

    public enum PersonnelChangeStatus
    {
        Draft,
        PendingHRReview,
        PendingEmployeeConsent,
        EmployeeDeclined,
        PendingDirectorApproval,
        ApprovedByDirector,
        PendingContractFlow,
        ContractNegotiating,
        ContractAccepted,
        ContractRejected,
        PendingDecisionIssuance,
        ReadyToExecute,
        Completed,
        Rejected,
        Cancelled,
        Escalated,
        PendingCurrentManagerOpinion,
        PendingEmployeeNotification,
        PendingEmployeeExplanation,
        PendingManagerReview,
        ContractRevisionClosed
    }

    public enum PersonnelChangeConsentStatus
    {
        NotRequired,
        Pending,
        Accepted,
        Declined,
        Acknowledged
    }

    public enum PersonnelChangeContractFlowType
    {
        None,
        NewContract,
        ContractRenewal,
        ContractAddendum,
        ContractTermination
    }

    public enum PersonnelChangePromotionType
    {
        ConvertToOfficial,
        PositionPromotion,
        JobLevelPromotion
    }

    public enum PersonnelChangeApprovalDecision
    {
        Pending,
        Approved,
        Rejected,
        Escalated
    }

    // ==========================================
    // MODULE 9: Workflow Requests
    // ==========================================
    public enum RequestStatus
    {
        Draft = 0,               // Đang soạn nháp, chưa gửi
        Pending_Manager = 1,     // Chờ Trưởng phòng phê duyệt
        Pending_HR = 2,          // Chờ HR thẩm định (chính sách, lương)
        Pending_Director = 3,    // Chờ Giám đốc chốt hạ
        Approved = 4,            // Đã duyệt hoàn tất
        Rejected = 5,            // Bị từ chối (bởi bất kỳ cấp nào)
        Auto_Rejected = 6        // SLA Worker tự động hủy vì quá hạn xử lý
    }
    public enum HandoverStatus
    {
        Not_Required = 0,        // Không yêu cầu bàn giao (VD: Kỷ luật nhẹ)
        In_Progress = 1,         // Đang thực hiện bàn giao tài sản/tài liệu
        Pending_Verification = 2,// Nhân viên đã nộp, chờ Quản lý/IT xác nhận
        Completed = 3,           // Hoàn tất bàn giao an toàn
        Overdue = 4              // Quá hạn bàn giao (SLA Worker sẽ bắt trạng thái này để HR chặn lương)
    }
    public enum RequestType
    {
        Promotion = 1,       // Thăng chức
        Appointment = 2,     // Bổ nhiệm (Vào vị trí quản lý)
        Transfer = 3,        // Luân chuyển phòng ban
        Resignation = 4,     // Xin nghỉ việc
        Termination = 5,     // Sa thải (Công ty chủ động)
        Disciplinary = 6,    // Kỷ luật
        Salary_Review = 7    // Đề xuất tăng lương đột xuất
    }

    public enum HistoryType
    {
        Onboarding = 1,      // Bắt đầu làm việc/Ký HĐ chính thức
        Promotion = 2,
        Appointment = 3,
        Transfer = 4,
        Salary_Change = 5,
        Disciplinary = 6,
        Termination = 7      // Chấm dứt hợp đồng (Bao gồm cả nghỉ việc và sa thải)
    }

    // ==========================================
    // SLA
    // ==========================================
    public enum SlaModuleType { Onboarding, ProfileUpdate, ContractRenewal, LeaveRequest,
        Recruitment, CandidateApproval, DirectorContractApproval
    }
    public enum SlaTaskStatus { Pending, Resolved, Violated }

    // ==========================================
    // Multi Phrase Approval (Dùng chung cho nhiều module có luồng phê duyệt nhiều cấp)
    // ==========================================
    public enum ApprovalStatus { Pending, Approved, Rejected, NeedMoreInfo }
    public enum ApprovalWorkflowAction { Approve, Reject, RequestRevision }
}
