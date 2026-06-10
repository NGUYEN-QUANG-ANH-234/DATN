using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Models.System;

namespace HRM.backend.src.HRM.Application.Services.System
{
    public class SlaProcessRegistry : ISlaProcessRegistry
    {
        private static readonly IReadOnlyCollection<SlaProcessDefinition> Processes = new List<SlaProcessDefinition>
        {
            Process("Recruitment", "Duyệt nhu cầu tuyển dụng", "Tuyển dụng", "SLA xử lý và duyệt nhu cầu tuyển dụng trước khi mở vị trí.", 72, "HOURS"),
            Process("CandidateApproval", "Duyệt ứng viên", "Tuyển dụng", "SLA duyệt ứng viên qua các cấp phỏng vấn và chốt kết quả.", 3, "DAYS"),
            Process("Onboarding", "Hoàn tất onboarding", "Hồ sơ nhân sự", "SLA hoàn tất tiếp nhận nhân sự mới sau khi ứng viên được nhận.", 5, "DAYS"),
            Process("ProfileUpdate", "Duyệt thay đổi hồ sơ", "Hồ sơ nhân sự", "SLA duyệt yêu cầu cập nhật thông tin hồ sơ nhân sự.", 48, "HOURS"),
            Process("ContractRenewal", "Xử lý hợp đồng", "Hợp đồng", "SLA HR xử lý hợp đồng, gia hạn hoặc phụ lục trước khi chuyển cấp duyệt.", 3, "DAYS"),
            Process("DirectorContractApproval", "Giám đốc duyệt hợp đồng", "Hợp đồng", "SLA giám đốc duyệt hợp đồng, phụ lục hoặc luồng hợp đồng quan trọng.", 2, "DAYS"),
            Process("LeaveRequest", "Duyệt nghỉ phép", "Chấm công", "SLA duyệt đơn nghỉ phép của nhân viên.", 48, "HOURS"),
            Process("OvertimeApproval", "Duyệt tăng ca", "Chấm công", "SLA duyệt yêu cầu tăng ca trước hoặc sau kỳ làm việc.", 24, "HOURS"),
            Process("AttendanceAdjustmentReview", "Duyệt điều chỉnh chấm công", "Chấm công", "SLA HR hoặc quản lý duyệt các yêu cầu bổ sung, chỉnh sửa công.", 24, "HOURS"),
            Process("PayrollCalculationReview", "HR rà soát bảng lương", "Lương", "SLA HR kiểm tra bảng lương sau khi hệ thống tính payroll.", 2, "DAYS"),
            Process("PayrollDirectorApproval", "Giám đốc duyệt bảng lương", "Lương", "SLA cấp duyệt cuối xác nhận bảng lương trước khi phát hành.", 2, "DAYS"),
            Process("PayrollEmployeeConfirm", "Nhân viên xác nhận lương", "Lương", "SLA nhân viên phản hồi phiếu lương sau khi được công bố.", 3, "DAYS"),
            Process("TaskSubmission", "Nhân viên hoàn thành task", "Công việc và đào tạo", "SLA nhân viên hoàn thành task theo hạn được giao.", 2, "DAYS"),
            Process("TaskReview", "Quản lý duyệt task", "Công việc và đào tạo", "SLA quản lý rà soát task sau khi nhân viên nộp kết quả.", 48, "HOURS"),
            Process("KpiReview", "Duyệt KPI", "Công việc và đào tạo", "SLA duyệt KPI hoặc kết quả đánh giá hiệu suất.", 7, "DAYS"),
            Process("PerformanceReviewApproval", "Duyệt đánh giá hiệu suất", "Công việc và đào tạo", "SLA quản lý duyệt phiếu đánh giá hiệu suất.", 2, "DAYS"),
            Process("TrainingEvaluation", "Đánh giá đào tạo", "Công việc và đào tạo", "SLA quản lý đánh giá kết quả đào tạo, thử việc hoặc học việc.", 7, "DAYS"),
            Process("PersonnelChangeHrReview", "HR rà soát biến động nhân sự", "Biến động nhân sự", "SLA HR kiểm tra hồ sơ thuyên chuyển, bổ nhiệm, thăng tiến, nghỉ việc hoặc kỷ luật.", 48, "HOURS"),
            Process("PersonnelChangeEmployeeConsent", "Nhân viên phản hồi biến động", "Biến động nhân sự", "SLA nhân viên đồng ý, từ chối hoặc giải trình trong hồ sơ biến động.", 72, "HOURS"),
            Process("PersonnelChangeDirectorApproval", "Giám đốc duyệt biến động", "Biến động nhân sự", "SLA giám đốc duyệt hồ sơ biến động nhân sự.", 48, "HOURS"),
            Process("PersonnelChangeContractFlow", "Luồng hợp đồng của biến động", "Biến động nhân sự", "SLA xử lý hợp đồng, phụ lục hoặc chấm dứt hợp đồng liên quan Module 7.", 5, "DAYS"),
            Process("PersonnelChangeDecisionIssuance", "Ban hành quyết định biến động", "Biến động nhân sự", "SLA HR ban hành quyết định sau khi hồ sơ được duyệt.", 48, "HOURS"),
            Process("DismissalEmployeeExplanation", "Nhân viên giải trình kỷ luật", "Biến động nhân sự", "SLA nhân viên phản hồi thông báo sa thải hoặc kỷ luật.", 3, "DAYS"),
            Process("ResignationManagerReview", "Quản lý duyệt nghỉ việc", "Biến động nhân sự", "SLA quản lý trực tiếp phản hồi đơn nghỉ việc chủ động.", 48, "HOURS"),
            Process("ResignationHrReview", "HR duyệt nghỉ việc", "Biến động nhân sự", "SLA HR rà soát đơn nghỉ việc, quyền lợi và ngày làm việc cuối.", 48, "HOURS"),
            Process("ResignationDirectorApproval", "Giám đốc duyệt nghỉ việc", "Biến động nhân sự", "SLA giám đốc duyệt đơn nghỉ việc chủ động.", 48, "HOURS")
        };

        private static readonly IReadOnlyCollection<SlaProcessAlias> Aliases = new List<SlaProcessAlias>
        {
            Alias("LEAVE_APPROVAL", "LeaveRequest"),
            Alias("CONTRACT_REVIEW", "ContractRenewal"),
            Alias("PROFILE_CHANGE", "ProfileUpdate"),
            Alias("PAYROLL_CONFIRM", "PayrollEmployeeConfirm"),
            Alias("RECRUITMENT_APPROVAL", "Recruitment"),
            Alias("OVERTIME_APPROVAL", "OvertimeApproval")
        };

        public IReadOnlyCollection<SlaProcessDefinition> GetProcesses() => Processes;

        public IReadOnlyCollection<SlaProcessAlias> GetAliases() => Aliases;

        public SlaProcessDefinition? FindByCode(string code)
        {
            var canonicalCode = ResolveCanonicalCode(code);
            if (canonicalCode == null)
                return null;

            return Processes.FirstOrDefault(process =>
                string.Equals(process.Code, canonicalCode, StringComparison.OrdinalIgnoreCase));
        }

        public string? ResolveCanonicalCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var normalized = code.Trim();
            var process = Processes.FirstOrDefault(item =>
                string.Equals(item.Code, normalized, StringComparison.OrdinalIgnoreCase));
            if (process != null)
                return process.Code;

            var alias = Aliases.FirstOrDefault(item =>
                string.Equals(item.LegacyCode, normalized, StringComparison.OrdinalIgnoreCase));

            return alias?.CanonicalCode;
        }

        private static SlaProcessDefinition Process(
            string code,
            string displayName,
            string moduleName,
            string description,
            int defaultValue,
            string defaultUnit)
        {
            return new SlaProcessDefinition
            {
                Code = code,
                DisplayName = displayName,
                ModuleName = moduleName,
                Description = description,
                DefaultValue = defaultValue,
                DefaultUnit = defaultUnit
            };
        }

        private static SlaProcessAlias Alias(string legacyCode, string canonicalCode)
        {
            return new SlaProcessAlias
            {
                LegacyCode = legacyCode,
                CanonicalCode = canonicalCode
            };
        }
    }
}
