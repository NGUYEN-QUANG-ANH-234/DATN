using System.Text.Encodings.Web;
using System.Text.Json;
using HRM.backend.src.HRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(MyDbContext))]
    [Migration("20260602164000_SeedNotificationAndDocumentTemplates")]
    public partial class SeedNotificationAndDocumentTemplates : Migration
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SeedNotificationTemplates(migrationBuilder);
            SeedDocumentTemplates(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM configurations
                WHERE ConfigGroup IN ('MAIL_TEMPLATE', 'DOCUMENT_TEMPLATE')
                  AND ParamKey IN (
                    'TEMPLATE_PROMOTION',
                    'TEMPLATE_NEW_TASK',
                    'TEMPLATE_SLA_WARNING',
                    'TEMPLATE_LEAVE_REQUEST_CREATED',
                    'TEMPLATE_LEAVE_REQUEST_APPROVED',
                    'TEMPLATE_LEAVE_REQUEST_REJECTED',
                    'TEMPLATE_RECRUITMENT_REQUEST_SUBMITTED',
                    'TEMPLATE_RECRUITMENT_APPROVED',
                    'TEMPLATE_CANDIDATE_APPROVAL_REQUIRED',
                    'TEMPLATE_CANDIDATE_APPROVED',
                    'TEMPLATE_CANDIDATE_REJECTED',
                    'TEMPLATE_ONBOARDING_REQUEST_CREATED',
                    'TEMPLATE_PROFILE_UPDATE_SUBMITTED',
                    'TEMPLATE_PROFILE_UPDATE_APPROVED',
                    'TEMPLATE_PROFILE_UPDATE_REJECTED',
                    'TEMPLATE_CONTRACT_FLOW_REQUIRED',
                    'TEMPLATE_CONTRACT_SIGNED',
                    'TEMPLATE_CONTRACT_REJECTED',
                    'TEMPLATE_OVERTIME_REQUEST_CREATED',
                    'TEMPLATE_OVERTIME_APPROVED',
                    'TEMPLATE_OVERTIME_REJECTED',
                    'TEMPLATE_PAYROLL_PUBLISHED',
                    'TEMPLATE_PAYROLL_ADJUSTMENT_APPROVED',
                    'TEMPLATE_KPI_REVIEW_CREATED',
                    'TEMPLATE_TRAINING_EVALUATION_DUE',
                    'TEMPLATE_PENALTY_CREATED',
                    'TEMPLATE_PERSONNEL_CHANGE_CREATED',
                    'TEMPLATE_PERSONNEL_CHANGE_EMPLOYEE_CONSENT',
                    'TEMPLATE_PERSONNEL_CHANGE_DIRECTOR_APPROVAL',
                    'TEMPLATE_PERSONNEL_CHANGE_APPROVED',
                    'TEMPLATE_PERSONNEL_CHANGE_REJECTED',
                    'TEMPLATE_PERSONNEL_CHANGE_EXECUTED',
                    'EXPORT_CONTRACT',
                    'EXPORT_CONTRACT_ADDENDUM',
                    'EXPORT_LEAVE_REQUEST',
                    'EXPORT_OVERTIME_REQUEST',
                    'EXPORT_PROFILE_UPDATE_REQUEST',
                    'EXPORT_ONBOARDING_PROFILE',
                    'EXPORT_RECRUITMENT_REQUEST',
                    'EXPORT_KPI_REVIEW',
                    'EXPORT_PAYSLIP',
                    'EXPORT_PERSONNEL_CHANGE_DECISION'
                  );
                """);
        }

        private static void SeedNotificationTemplates(MigrationBuilder migrationBuilder)
        {
            UpsertNotification(migrationBuilder, "PROMOTION", "Thông báo quyết định thăng tiến", "<p>Xin chào {name},</p><p>Bạn đã được ghi nhận thay đổi vị trí/chức danh mới: <b>{position}</b>, hiệu lực từ ngày <b>{date}</b>.</p>");
            UpsertNotification(migrationBuilder, "NEW_TASK", "Bạn được giao công việc mới: {task_name}", "<p>Xin chào {name},</p><p>Bạn vừa được giao công việc <b>{task_name}</b>. Hạn hoàn thành: <b>{deadline}</b>.</p>");
            UpsertNotification(migrationBuilder, "SLA_WARNING", "Cảnh báo SLA sắp quá hạn: {module}", "<p>Xin chào {name},</p><p>Quy trình <b>{module}</b> còn <b>{hours_left}</b> giờ trước khi quá hạn SLA.</p>");
            UpsertNotification(migrationBuilder, "LEAVE_REQUEST_CREATED", "Đơn nghỉ phép mới từ {name}", "<p>Nhân viên <b>{name}</b> vừa tạo đơn <b>{leave_type}</b> từ <b>{start_date}</b> đến <b>{end_date}</b>, tổng <b>{days}</b> ngày. Trạng thái: <b>{status}</b>.</p>");
            UpsertNotification(migrationBuilder, "LEAVE_REQUEST_APPROVED", "Đơn nghỉ phép của {name} đã được duyệt", "<p>Đơn <b>{leave_type}</b> của <b>{name}</b> từ <b>{start_date}</b> đến <b>{end_date}</b> đã được duyệt.</p>");
            UpsertNotification(migrationBuilder, "LEAVE_REQUEST_REJECTED", "Đơn nghỉ phép của {name} bị từ chối", "<p>Đơn <b>{leave_type}</b> của <b>{name}</b> đã bị từ chối. Lý do: <b>{reason}</b>.</p>");
            UpsertNotification(migrationBuilder, "RECRUITMENT_REQUEST_SUBMITTED", "Nhu cầu tuyển dụng mới: {position}", "<p><b>{name}</b> vừa tạo nhu cầu tuyển dụng vị trí <b>{position}</b> cho phòng ban <b>{department}</b>. Số lượng: <b>{quantity}</b>. Hạn cần nhân sự: <b>{deadline}</b>.</p>");
            UpsertNotification(migrationBuilder, "RECRUITMENT_APPROVED", "Nhu cầu tuyển dụng đã được duyệt", "<p>Nhu cầu tuyển dụng vị trí <b>{position}</b> của phòng ban <b>{department}</b> đã được duyệt. Trạng thái: <b>{status}</b>.</p>");
            UpsertNotification(migrationBuilder, "CANDIDATE_APPROVAL_REQUIRED", "Cần duyệt ứng viên {candidate_name}", "<p>Ứng viên <b>{candidate_name}</b> cho vị trí <b>{position}</b> đang chờ duyệt ở bước <b>{stage}</b>. Hạn xử lý: <b>{deadline}</b>.</p>");
            UpsertNotification(migrationBuilder, "CANDIDATE_APPROVED", "Ứng viên {candidate_name} đã được duyệt", "<p>Ứng viên <b>{candidate_name}</b> cho vị trí <b>{position}</b> đã được duyệt. Trạng thái: <b>{status}</b>.</p>");
            UpsertNotification(migrationBuilder, "CANDIDATE_REJECTED", "Ứng viên {candidate_name} bị từ chối", "<p>Ứng viên <b>{candidate_name}</b> cho vị trí <b>{position}</b> đã bị từ chối. Lý do: <b>{reason}</b>.</p>");
            UpsertNotification(migrationBuilder, "ONBOARDING_REQUEST_CREATED", "Hồ sơ onboarding mới: {employee_name}", "<p>Hệ thống vừa ghi nhận hồ sơ onboarding cho <b>{employee_name}</b> từ ứng viên <b>{candidate_name}</b>. Phòng ban: <b>{department}</b>. Vị trí: <b>{position}</b>.</p>");
            UpsertNotification(migrationBuilder, "PROFILE_UPDATE_SUBMITTED", "Yêu cầu cập nhật hồ sơ từ {name}", "<p>Nhân viên <b>{name}</b> vừa gửi yêu cầu cập nhật các trường: <b>{fields}</b>. Thời điểm gửi: <b>{submitted_at}</b>.</p>");
            UpsertNotification(migrationBuilder, "PROFILE_UPDATE_APPROVED", "Cập nhật hồ sơ của {name} đã được duyệt", "<p>Yêu cầu cập nhật hồ sơ của <b>{name}</b> đã được duyệt. Các trường cập nhật: <b>{fields}</b>.</p>");
            UpsertNotification(migrationBuilder, "PROFILE_UPDATE_REJECTED", "Cập nhật hồ sơ của {name} bị từ chối", "<p>Yêu cầu cập nhật hồ sơ của <b>{name}</b> đã bị từ chối. Lý do: <b>{reason}</b>.</p>");
            UpsertNotification(migrationBuilder, "CONTRACT_FLOW_REQUIRED", "Cần xử lý hợp đồng cho {name}", "<p>Hồ sơ hợp đồng của <b>{name}</b> cần được xử lý. Loại hợp đồng/phụ lục: <b>{contract_type}</b>. Hạn xử lý: <b>{deadline}</b>.</p>");
            UpsertNotification(migrationBuilder, "CONTRACT_SIGNED", "Hợp đồng {contract_number} đã hoàn tất", "<p>Hợp đồng <b>{contract_number}</b> của <b>{name}</b> đã hoàn tất ký/chấp thuận. Ngày hiệu lực: <b>{effective_date}</b>.</p>");
            UpsertNotification(migrationBuilder, "CONTRACT_REJECTED", "Hợp đồng {contract_number} bị từ chối", "<p>Hợp đồng <b>{contract_number}</b> của <b>{name}</b> bị từ chối. Lý do: <b>{reason}</b>.</p>");
            UpsertNotification(migrationBuilder, "OVERTIME_REQUEST_CREATED", "Yêu cầu tăng ca mới từ {name}", "<p><b>{name}</b> vừa tạo yêu cầu tăng ca ngày <b>{work_date}</b>, từ <b>{start_time}</b> đến <b>{end_time}</b>. Lý do: <b>{reason}</b>.</p>");
            UpsertNotification(migrationBuilder, "OVERTIME_APPROVED", "Yêu cầu tăng ca của {name} đã được duyệt", "<p>Yêu cầu tăng ca ngày <b>{work_date}</b> của <b>{name}</b> đã được duyệt. Số phút được duyệt: <b>{approved_minutes}</b>.</p>");
            UpsertNotification(migrationBuilder, "OVERTIME_REJECTED", "Yêu cầu tăng ca của {name} bị từ chối", "<p>Yêu cầu tăng ca ngày <b>{work_date}</b> của <b>{name}</b> đã bị từ chối. Lý do: <b>{reason}</b>.</p>");
            UpsertNotification(migrationBuilder, "PAYROLL_PUBLISHED", "Bảng lương kỳ {period} đã được công bố", "<p>Xin chào {name},</p><p>Bảng lương kỳ <b>{period}</b> đã được công bố. Lương thực nhận: <b>{net_salary}</b>. Trạng thái: <b>{status}</b>.</p>");
            UpsertNotification(migrationBuilder, "PAYROLL_ADJUSTMENT_APPROVED", "Điều chỉnh lương kỳ {period} đã được duyệt", "<p>Điều chỉnh lương của <b>{name}</b> trong kỳ <b>{period}</b> đã được duyệt. Số tiền: <b>{amount}</b>. Lý do: <b>{reason}</b>.</p>");
            UpsertNotification(migrationBuilder, "KPI_REVIEW_CREATED", "Phiếu đánh giá KPI kỳ {period}", "<p>Phiếu đánh giá KPI kỳ <b>{period}</b> của <b>{name}</b> đã được tạo. Hạn xử lý: <b>{deadline}</b>.</p>");
            UpsertNotification(migrationBuilder, "TRAINING_EVALUATION_DUE", "Cần đánh giá đào tạo: {course_name}", "<p>Khóa đào tạo <b>{course_name}</b> của <b>{name}</b> đang chờ đánh giá. Hạn xử lý: <b>{deadline}</b>.</p>");
            UpsertNotification(migrationBuilder, "PENALTY_CREATED", "Ghi nhận vi phạm: {rule_code}", "<p>Hệ thống ghi nhận vi phạm cho <b>{name}</b>. Mã luật: <b>{rule_code}</b>. Điểm trừ: <b>{point}</b>. Lý do: <b>{reason}</b>.</p>");
            UpsertNotification(migrationBuilder, "PERSONNEL_CHANGE_CREATED", "Hồ sơ biến động nhân sự mới: {change_type}", "<p>Hồ sơ biến động nhân sự của <b>{name}</b> vừa được tạo. Loại biến động: <b>{change_type}</b>. Ngày hiệu lực dự kiến: <b>{effective_date}</b>. Lý do: <b>{reason}</b>.</p>");
            UpsertNotification(migrationBuilder, "PERSONNEL_CHANGE_EMPLOYEE_CONSENT", "Cần phản hồi hồ sơ biến động nhân sự", "<p>Xin chào {name},</p><p>Hồ sơ <b>{change_type}</b> cần phản hồi từ bạn trước <b>{deadline}</b>.</p>");
            UpsertNotification(migrationBuilder, "PERSONNEL_CHANGE_DIRECTOR_APPROVAL", "Cần duyệt biến động nhân sự: {change_type}", "<p>Hồ sơ biến động nhân sự của <b>{name}</b> đang chờ duyệt. Loại: <b>{change_type}</b>. Ngày hiệu lực: <b>{effective_date}</b>.</p>");
            UpsertNotification(migrationBuilder, "PERSONNEL_CHANGE_APPROVED", "Hồ sơ biến động nhân sự đã được duyệt", "<p>Hồ sơ <b>{change_type}</b> của <b>{name}</b> đã được duyệt. Ngày hiệu lực: <b>{effective_date}</b>.</p>");
            UpsertNotification(migrationBuilder, "PERSONNEL_CHANGE_REJECTED", "Hồ sơ biến động nhân sự bị từ chối", "<p>Hồ sơ <b>{change_type}</b> của <b>{name}</b> đã bị từ chối. Lý do: <b>{reason}</b>.</p>");
            UpsertNotification(migrationBuilder, "PERSONNEL_CHANGE_EXECUTED", "Đã thực thi biến động nhân sự", "<p>Biến động nhân sự <b>{change_type}</b> của <b>{name}</b> đã hoàn tất thực thi. Ngày hiệu lực: <b>{effective_date}</b>.</p>");
        }

        private static void SeedDocumentTemplates(MigrationBuilder migrationBuilder)
        {
            UpsertDocument(migrationBuilder, "EXPORT_CONTRACT", "Contract", "Hợp đồng lao động",
                "<h2>HỢP ĐỒNG LAO ĐỘNG</h2><table><tr><td>Nhân viên</td><td><b>{employee_name}</b></td></tr><tr><td>Mã hợp đồng</td><td>{contract_number}</td></tr><tr><td>Loại hợp đồng</td><td>{contract_type}</td></tr><tr><td>Lương cơ bản</td><td>{basic_salary}</td></tr><tr><td>Ngày bắt đầu</td><td>{start_date}</td></tr><tr><td>Ngày kết thúc</td><td>{end_date}</td></tr><tr><td>Trạng thái</td><td>{status}</td></tr></table>");
            UpsertDocument(migrationBuilder, "EXPORT_CONTRACT_ADDENDUM", "ContractAddendum", "Phụ lục hợp đồng",
                "<h2>PHỤ LỤC HỢP ĐỒNG</h2><table><tr><td>Nhân viên</td><td>{employee_name}</td></tr><tr><td>Số phụ lục</td><td>{addendum_number}</td></tr><tr><td>Hợp đồng gốc</td><td>{contract_number}</td></tr><tr><td>Lương mới</td><td>{new_basic_salary}</td></tr><tr><td>Ngày hiệu lực</td><td>{effective_date}</td></tr><tr><td>Nội dung khác</td><td>{other_changes}</td></tr></table>");
            UpsertDocument(migrationBuilder, "EXPORT_LEAVE_REQUEST", "LeaveRequest", "Đơn nghỉ phép",
                "<h2>ĐƠN NGHỈ PHÉP</h2><table><tr><td>Nhân viên</td><td>{employee_name}</td></tr><tr><td>Loại nghỉ</td><td>{leave_type}</td></tr><tr><td>Từ ngày</td><td>{start_date}</td></tr><tr><td>Đến ngày</td><td>{end_date}</td></tr><tr><td>Số ngày</td><td>{days}</td></tr><tr><td>Lý do</td><td>{reason}</td></tr><tr><td>Trạng thái</td><td>{status}</td></tr></table>");
            UpsertDocument(migrationBuilder, "EXPORT_OVERTIME_REQUEST", "OvertimeRequest", "Phiếu đăng ký tăng ca",
                "<h2>PHIẾU ĐĂNG KÝ TĂNG CA</h2><table><tr><td>Nhân viên</td><td>{employee_name}</td></tr><tr><td>Ngày làm việc</td><td>{work_date}</td></tr><tr><td>Thời gian</td><td>{start_time} - {end_time}</td></tr><tr><td>Số phút duyệt</td><td>{approved_minutes}</td></tr><tr><td>Dự án</td><td>{project_code}</td></tr><tr><td>Lý do</td><td>{reason}</td></tr><tr><td>Trạng thái</td><td>{status}</td></tr></table>");
            UpsertDocument(migrationBuilder, "EXPORT_PROFILE_UPDATE_REQUEST", "ProfileUpdate", "Phiếu thay đổi hồ sơ",
                "<h2>PHIẾU THAY ĐỔI HỒ SƠ NHÂN SỰ</h2><table><tr><td>Nhân viên</td><td>{employee_name}</td></tr><tr><td>Trường thay đổi</td><td>{requested_fields}</td></tr><tr><td>Giá trị cũ</td><td>{old_values}</td></tr><tr><td>Giá trị mới</td><td>{new_values}</td></tr><tr><td>Trạng thái</td><td>{status}</td></tr><tr><td>Lý do từ chối</td><td>{reject_reason}</td></tr></table>");
            UpsertDocument(migrationBuilder, "EXPORT_ONBOARDING_PROFILE", "Onboarding", "Phiếu onboarding nhân sự",
                "<h2>PHIẾU ONBOARDING NHÂN SỰ</h2><table><tr><td>Ứng viên</td><td>{candidate_name}</td></tr><tr><td>Nhân viên</td><td>{employee_name}</td></tr><tr><td>Mã nhân viên</td><td>{employee_code}</td></tr><tr><td>Phòng ban</td><td>{department_name}</td></tr><tr><td>Vị trí</td><td>{position_name}</td></tr><tr><td>Loại nhân sự</td><td>{employee_type}</td></tr><tr><td>Trạng thái</td><td>{status}</td></tr></table>");
            UpsertDocument(migrationBuilder, "EXPORT_RECRUITMENT_REQUEST", "Recruitment", "Phiếu nhu cầu tuyển dụng",
                "<h2>PHIẾU NHU CẦU TUYỂN DỤNG</h2><table><tr><td>Mã yêu cầu</td><td>{request_code}</td></tr><tr><td>Phòng ban</td><td>{department_name}</td></tr><tr><td>Vị trí</td><td>{position_name}</td></tr><tr><td>Số lượng</td><td>{quantity}</td></tr><tr><td>Ngày cần nhân sự</td><td>{expected_start_date}</td></tr><tr><td>Lý do</td><td>{reason}</td></tr><tr><td>Trạng thái</td><td>{status}</td></tr></table>");
            UpsertDocument(migrationBuilder, "EXPORT_KPI_REVIEW", "Performance", "Phiếu đánh giá KPI",
                "<h2>PHIẾU ĐÁNH GIÁ KPI</h2><table><tr><td>Nhân viên</td><td>{employee_name}</td></tr><tr><td>Kỳ đánh giá</td><td>{period}</td></tr><tr><td>Tổng trọng số</td><td>{total_weight}</td></tr><tr><td>Tổng điểm trừ</td><td>{total_penalty_points}</td></tr><tr><td>Tổng điểm</td><td>{total_score}</td></tr><tr><td>Xếp loại</td><td>{final_rating}</td></tr></table><h3>Chi tiết KPI</h3><table border=\"1\" width=\"100%\"><tr><th>Mã</th><th>Tên KPI</th><th>Trọng số</th><th>Điểm trừ</th><th>Lý do</th><th>Điểm cuối</th></tr>{kpi_detail_rows}</table>");
            UpsertDocument(migrationBuilder, "EXPORT_PAYSLIP", "Payroll", "Phiếu lương nhân viên",
                "<h2>PHIẾU LƯƠNG</h2><table><tr><td>Nhân viên</td><td>{employee_name}</td></tr><tr><td>Kỳ lương</td><td>{period}</td></tr><tr><td>Lương cơ bản</td><td>{base_salary}</td></tr><tr><td>Tổng thu nhập</td><td>{gross_income}</td></tr><tr><td>Bảo hiểm NLĐ</td><td>{employee_insurance_amount}</td></tr><tr><td>Thuế TNCN</td><td>{pit_amount}</td></tr><tr><td>Khấu trừ khác</td><td>{other_deductions}</td></tr><tr><td>Lương thực nhận</td><td><b>{net_salary}</b></td></tr></table><h3>Chi tiết thành phần lương</h3><table border=\"1\" width=\"100%\"><tr><th>Mã</th><th>Thành phần</th><th>Số tiền</th><th>Chịu thuế</th><th>Tính BH</th><th>Ghi chú</th></tr>{payroll_detail_rows}</table>");
            UpsertDocument(migrationBuilder, "EXPORT_PERSONNEL_CHANGE_DECISION", "PersonnelChange", "Quyết định biến động nhân sự",
                "<h2>QUYẾT ĐỊNH BIẾN ĐỘNG NHÂN SỰ</h2><table><tr><td>Mã hồ sơ</td><td>{request_code}</td></tr><tr><td>Nhân viên</td><td>{employee_name}</td></tr><tr><td>Loại biến động</td><td>{change_type}</td></tr><tr><td>Số quyết định</td><td>{decision_number}</td></tr><tr><td>Ngày ban hành</td><td>{decision_issued_at}</td></tr><tr><td>Ngày hiệu lực</td><td>{effective_date}</td></tr><tr><td>Lý do</td><td>{reason}</td></tr></table><h3>Nội dung thay đổi</h3><table border=\"1\" width=\"100%\"><tr><th>Nội dung</th><th>Hiện tại</th><th>Sau thay đổi</th></tr>{personnel_change_rows}</table>");
        }

        private static void UpsertNotification(MigrationBuilder migrationBuilder, string key, string subject, string bodyHtml)
        {
            var json = JsonSerializer.Serialize(new { Subject = subject, BodyHtml = bodyHtml }, JsonOptions);
            UpsertConfiguration(migrationBuilder, "MAIL_TEMPLATE", $"TEMPLATE_{key}", json, "Mẫu email/thông báo hệ thống");
        }

        private static void UpsertDocument(MigrationBuilder migrationBuilder, string key, string documentType, string displayName, string bodyHtml)
        {
            var json = JsonSerializer.Serialize(new
            {
                TemplateKey = key,
                DocumentType = documentType,
                DisplayName = displayName,
                DefaultOutput = "HTML",
                ActiveLayoutVersion = "standard",
                AllowedOutputs = new[] { "HTML" },
                LayoutVersions = new[]
                {
                    new
                    {
                        Version = "standard",
                        Name = "Mẫu chuẩn",
                        IsActive = true,
                        Page = new { Size = "A4", Orientation = "portrait", Margin = "20mm" },
                        Theme = new { FontFamily = "Times New Roman", FontSize = "12pt", PrimaryColor = "#111827", AccentColor = "#f97316", LogoUrl = "" },
                        HeaderHtml = "<div style=\"text-align:center\"><p><b>{company_name}</b></p><p>{company_address}</p><hr/></div>",
                        BodyHtml = bodyHtml,
                        FooterHtml = "<div style=\"margin-top:32px;display:flex;justify-content:space-between;text-align:center\"><div><b>Người lập</b><br/><br/><br/>{created_by}</div><div><b>Người duyệt</b><br/><br/><br/>{director_name}</div></div>"
                    }
                }
            }, JsonOptions);

            UpsertConfiguration(migrationBuilder, "DOCUMENT_TEMPLATE", key, json, "Mẫu biểu mẫu/báo cáo trích xuất");
        }

        private static void UpsertConfiguration(MigrationBuilder migrationBuilder, string group, string key, string value, string description)
        {
            migrationBuilder.Sql($"""
                INSERT INTO configurations (ConfigGroup, ParamKey, ParamValue, Description, IsActive)
                VALUES ('{Escape(group)}', '{Escape(key)}', '{Escape(value)}', '{Escape(description)}', 1)
                ON DUPLICATE KEY UPDATE
                    Description = VALUES(Description);
                """);
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("'", "''");
        }
    }
}
