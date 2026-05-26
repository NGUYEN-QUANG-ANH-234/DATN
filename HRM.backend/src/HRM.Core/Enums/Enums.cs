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
    public enum EmployeeStatus { Probation, Official, Resigned, Terminated }
    public enum ContractType { Probation, Definite, Indefinite, PartTime }
    public enum ContractStatus { Draft, PendingDept, PendingHR, Negotiating, PendingDirector, Rejected, Active, Liquidating, Expired, Draft_Cancelled }

    public enum DependentRelation { Child, Parent, Spouse, Other }
    public enum EmployeeType { Intern, Official, Probation, PartTime, Contractual }
    public enum OnboardingStatus
    {
        Pending_HR,
        Completed,
        Rejected
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
    // ==========================================
    // MODULE 5: Time & Attendance
    // ==========================================
    public enum AttendanceStatus { Valid, Invalid, Late, Early, OnLeave }
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
        AutoFinalApproved
    }
    public enum OvertimeRequestStatus
    {
        PendingManager = 0,
        PendingHR = 1,
        Approved = 2,
        Rejected = 3,
        Cancelled = 4,
        PendingDirector = 5
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
    public enum FormulaStatus { Pending, Approved, Rejected }
    public enum PayrollStatus { Draft, Finalized, Paid }
    public enum SalaryVariableDataType { Number, Money, Hours, Days, Percent }
    public enum SalaryAggregationType { Latest, Sum, Count, MonthlyTotal, Manual }

    // ==========================================
    // MODULE 8: Requests & Handover
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
    public enum ApprovalStatus { Pending, Approved, Rejected }
}
