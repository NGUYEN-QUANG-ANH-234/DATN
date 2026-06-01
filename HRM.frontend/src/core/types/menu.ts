export interface MenuItem {
  path?: string;
  label: string;
  roles: string[];
  icon?: string;
  children?: MenuItem[];
}

const ALL_ROLES = [
  "Admin",
  "HR",
  "Manager",
  "Director",
  "Employee",
  "Intern",
  "Candidate",
  "Collaborator",
];

export const MENU_ITEMS: MenuItem[] = [
  {
    path: "/dashboard",
    label: "Tổng quan",
    roles: ALL_ROLES,
  },
  {
    label: "Module 0 - Cấu hình hệ thống",
    roles: ["Admin", "HR", "Director"],
    children: [
      {
        path: "/system-config/positions-departments",
        label: "F0.5 Vị trí & phòng ban",
        roles: ["Admin", "HR", "Director"],
      },
      {
        path: "/system-config/salary-variables",
        label: "F0.1 Biến lương",
        roles: ["Admin"],
      },
      {
        path: "/system-config/sla",
        label: "F0.2 Thời hạn xử lý SLA",
        roles: ["Admin"],
      },
      {
        path: "/system-config/notification-templates",
        label: "F0.3 Mẫu thông báo",
        roles: ["Admin"],
      },
      {
        path: "/system-config/attendance-parameters",
        label: "F0.4 Tham số chấm công",
        roles: ["Admin"],
      },
      {
        path: "/system-config/work-schedules",
        label: "F0.6 Lịch trình làm việc",
        roles: ["Admin"],
      },
      {
        path: "/system-config/payroll-policies",
        label: "F0.7 Chính sách lương",
        roles: ["Admin"],
      },
    ],
  },
  {
    label: "Module 1 - Quản trị hệ thống",
    roles: ["Admin"],
    children: [
      {
        path: "/admin/roles-permissions",
        label: "F1.1 Phân quyền",
        roles: ["Admin"],
      },
      {
        path: "/admin/audit-logs",
        label: "F1.2 Audit Log",
        roles: ["Admin"],
      },
      {
        path: "/admin/identity-auth",
        label: "F1.3 Xác thực & MFA",
        roles: ["Admin"],
      },
      {
        path: "/admin/accounts-access",
        label: "F1.4 Tài khoản & truy cập",
        roles: ["Admin"],
      },
    ],
  },
  {
    label: "Module 2 - Tuyển dụng",
    roles: ["Admin", "HR", "Manager", "Director", "Employee", "Candidate"],
    children: [
      {
        path: "/recruitment/jobs",
        label: "Vị trí tuyển dụng",
        roles: ["Admin", "HR", "Manager", "Director", "Employee", "Candidate"],
      },
      {
        path: "/recruitment/demands",
        label: "F2.1 Nhu cầu tuyển dụng",
        roles: ["Admin", "HR", "Manager", "Director"],
      },
      {
        path: "/recruitment/demands/create",
        label: "Tạo nhu cầu tuyển dụng",
        roles: ["Admin", "HR", "Manager"],
      },
      {
        path: "/recruitment/apply-cv",
        label: "F2.2 Gửi CV ứng tuyển",
        roles: ["Admin", "HR", "Manager", "Director", "Employee", "Candidate"],
      },
      {
        path: "/recruitment/candidate-review",
        label: "F2.3 Xét duyệt ứng viên",
        roles: ["Admin", "HR", "Manager", "Director"],
      },
      {
        path: "/recruitment/history",
        label: "Lịch sử ứng tuyển",
        roles: ["Admin", "HR", "Manager", "Director", "Employee", "Candidate"],
      },
    ],
  },
  {
    label: "Module 3 - Hồ sơ & Hợp đồng",
    roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern"],
    children: [
      {
        path: "/employee-contract/my-profile",
        label: "Hồ sơ cá nhân",
        roles: ["HR", "Manager", "Director", "Employee", "Intern"],
      },
      {
        path: "/employee-contract/profile-setup",
        label: "F3.2 Thiết lập hồ sơ",
        roles: ["Admin", "HR", "Director"],
      },
      {
        path: "/employee-contract/profile-change",
        label: "F3.3 Cập nhật hồ sơ",
        roles: ["HR", "Manager", "Director", "Employee", "Intern"],
      },
      {
        path: "/employee-contract/profile-review",
        label: "Duyệt thay đổi hồ sơ",
        roles: ["Admin", "HR"],
      },
      {
        path: "/employee-contract/contracts",
        label: "Hợp đồng của tôi",
        roles: ["HR", "Manager", "Director", "Employee", "Intern"],
      },
      {
        path: "/employee-contract/contract-requests",
        label: "F3.1 Ký/gia hạn hợp đồng",
        roles: ["Employee", "Manager", "Intern"],
      },
      {
        path: "/employee-contract/hr-contracts",
        label: "Soạn thảo hợp đồng",
        roles: ["Admin", "HR"],
      },
      {
        path: "/employee-contract/director-contract-approval",
        label: "Duyệt hợp đồng",
        roles: ["Director"],
      },
      {
        path: "/employee-contract/appendices",
        label: "F3.5 Phụ lục hợp đồng",
        roles: ["Admin", "HR", "Director"],
      },
      {
        path: "/employee-contract/history",
        label: "F3.6 Lịch sử biến động",
        roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern"],
      },
    ],
  },
  {
    label: "Module 4 - Chấm công & Nghỉ phép",
    roles: ["Admin", "HR", "Manager", "Director", "Employee"],
    children: [
      {
        path: "/attendance-leave/attendance",
        label: "F4.1 Chấm công",
        roles: ["Admin", "HR", "Manager", "Employee"],
      },
      {
        path: "/attendance-leave/overtime",
        label: "F4.2 Quản lý OT",
        roles: ["Admin", "HR", "Manager", "Employee"],
      },
      {
        path: "/attendance-leave/overtime-approvals",
        label: "Duyệt OT",
        roles: ["Admin", "HR", "Manager", "Director"],
      },
      {
        path: "/attendance-leave/timesheet-summary",
        label: "F4.3 Tổng hợp bảng công",
        roles: ["Admin", "HR"],
      },
      {
        path: "/attendance-leave/leave",
        label: "F4.4 Quản lý nghỉ phép",
        roles: ["Admin", "Manager", "Director", "Employee"],
      },
    ],
  },
  {
    label: "Module 5 - Hiệu suất & Đào tạo",
    roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern"],
    children: [
      {
        path: "/performance-training/criteria",
        label: "F5.1 Bộ chỉ tiêu KPI",
        roles: ["Admin", "HR", "Manager"],
      },
      {
        path: "/performance-training/result-update",
        label: "F5.2 Cập nhật kết quả",
        roles: ["Admin", "HR", "Manager", "Employee", "Intern"],
      },
      {
        path: "/performance-training/penalties",
        label: "F5.3 Vi phạm & điều chỉnh công",
        roles: ["Admin", "HR", "Manager", "Director"],
      },
      {
        path: "/performance-training/review-finalize",
        label: "F5.4 Chốt đánh giá",
        roles: ["Admin", "HR", "Manager"],
      },
      {
        path: "/performance-training/development-training",
        label: "F5.5 Đào tạo & phát triển",
        roles: ["Admin", "HR", "Manager"],
      },
    ],
  },
  {
    label: "Module 6 - Lương & Thưởng",
    roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern", "Collaborator"],
    children: [
      {
        path: "/payroll/salary-formula",
        label: "F6.1 Công thức lương",
        roles: ["Admin", "HR", "Director"],
      },
      {
        path: "/payroll/payroll-aggregation",
        label: "F6.2 Tổng hợp lương thưởng",
        roles: ["Admin", "HR", "Director"],
      },
      {
        path: "/payroll/payslip",
        label: "F6.3 Phiếu lương",
        roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern", "Collaborator"],
      },
      {
        path: "/payroll/adjustments",
        label: "Điều chỉnh nghiệp vụ lương",
        roles: ["Admin", "HR"],
      },
      {
        path: "/payroll/external-timesheets",
        label: "Giờ công CTV",
        roles: ["Admin", "HR"],
      },
    ],
  },
  {
    label: "Module 7 - Biến động nhân sự",
    roles: ["Admin", "HR", "Manager", "Director"],
    children: [
      {
        path: "/personnel-change/promotion",
        label: "F7.1 Đề xuất thăng tiến",
        roles: ["Admin", "HR", "Manager", "Director"],
      },
      {
        path: "/personnel-change/senior-appointment",
        label: "F7.2 Bổ nhiệm NSCC",
        roles: ["Admin", "HR", "Director"],
      },
      {
        path: "/personnel-change/termination",
        label: "F7.3 Chấm dứt chủ động",
        roles: ["Admin", "HR", "Manager", "Director"],
      },
      {
        path: "/personnel-change/dismissal",
        label: "F7.4 Sa thải bị động",
        roles: ["Admin", "HR", "Director"],
      },
      {
        path: "/personnel-change/internal-transfer",
        label: "F7.5 Thuyên chuyển nội bộ",
        roles: ["Admin", "HR", "Manager", "Director"],
      },
    ],
  },
  {
    label: "Phê duyệt & theo dõi",
    roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern", "Candidate"],
    children: [
      {
        path: "/approvals",
        label: "Phê duyệt của tôi",
        roles: ["Admin", "HR", "Manager", "Director"],
      },
      {
        path: "/approvals/tracking",
        label: "Theo dõi trạng thái",
        roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern", "Candidate"],
      },
    ],
  },
];
