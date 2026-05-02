namespace HRM.backend.src.HRM.Core.Enums
{
    // ==========================================
    // MODULE 1: System
    // ==========================================
    public enum AccountStatus { Active, Inactive }

    // ==========================================
    // MODULE 2 & 3: Organization & Recruitment
    // ==========================================
    public enum DeptStatus { Active, Dissolved }
    public enum RecruitmentRequestStatus { PendingHR, PendingDirector, Approved, Rejected, Closed }
    public enum CandidateStatus { New, Interview, Offer, Hired, Rejected }

    // ==========================================
    // MODULE 4: Employee & Contract
    // ==========================================
    public enum Gender { Male, Female, Other }
    public enum EmployeeStatus { Probation, Official, Resigned, Terminated }
    public enum ContractType { Probation, Definite, Indefinite }
    public enum ContractStatus { Draft, Active, Liquidating, Expired }

    // ==========================================
    // MODULE 5: Time & Attendance
    // ==========================================
    public enum AttendanceStatus { Valid, Invalid, Late, Early }
    public enum LeaveRequestStatus { Pending, Approved, Rejected, Auto_Approved }

    // ==========================================
    // MODULE 6: Tasks & Training
    // ==========================================
    public enum BudgetStatus { Pending, Approved, Rejected }
    public enum TaskType { Project, SelfStudy, Research } // Bỏ dấu gạch ngang của "Self-Study" để hợp lệ C#
    public enum TaskStatus { Todo, Doing, Done, Overdue }
    public enum TrainingStatus { InProgress, Completed, Failed, Overdue }

    // ==========================================
    // MODULE 7: Payroll
    // ==========================================
    public enum FormulaStatus { Pending, Approved, Rejected }
    public enum PayrollStatus { Draft, Finalized, Paid }

    // ==========================================
    // MODULE 8: Requests & Handover
    // ==========================================
    public enum RequestType { Promotion, Appointment, Resignation, Transfer, Disciplinary }
    public enum RequestStatus { Pending, Manager_Approved, Director_Approved, Rejected, Auto_Rejected }
    public enum HandoverStatus { Pending, Completed }
    public enum HistoryType { Promotion, Appointment, Transfer, Salary_Change, Termination, Disciplinary }
}