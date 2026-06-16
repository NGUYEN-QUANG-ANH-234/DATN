import { APP_ROLES, ROLE_GROUPS, type RoleList } from "../auth/roleAccess";

export interface MenuItem {
  path?: string;
  label: string;
  roles: RoleList;
  icon?: string;
  module?: string;
  children?: MenuItem[];
}

export const MENU_ITEMS: MenuItem[] = [
  {
    path: "/dashboard",
    label: "Tổng quan",
    roles: APP_ROLES,
    icon: "dashboard",
    module: "Tổng quan",
  },
  {
    label: "Cấu hình hệ thống",
    roles: ROLE_GROUPS.systemConfig,
    icon: "settings",
    children: [
      {
        path: "/system-config/positions-departments",
        label: "Phòng ban",
        roles: ROLE_GROUPS.organization,
      },
      {
        path: "/system-config/salary-variables",
        label: "Biến lương",
        roles: ROLE_GROUPS.hrAdmin,
      },
      {
        path: "/system-config/sla",
        label: "Thời hạn xử lý",
        roles: ROLE_GROUPS.hrAdmin,
      },
      {
        path: "/system-config/notification-templates",
        label: "Mẫu thông báo",
        roles: ROLE_GROUPS.systemAdmin,
      },
      {
        path: "/system-config/document-templates",
        label: "Cấu hình biểu mẫu",
        roles: ROLE_GROUPS.systemAdmin,
      },
      {
        path: "/system-config/attendance-parameters",
        label: "Tham số chấm công",
        roles: ROLE_GROUPS.systemAdmin,
      },
      {
        path: "/system-config/work-schedules",
        label: "Ca làm việc",
        roles: ROLE_GROUPS.hrAdmin,
      },
      {
        path: "/system-config/company-calendar",
        label: "Lịch nghỉ công ty",
        roles: ROLE_GROUPS.hrAdmin,
      },
      {
        path: "/system-config/payroll-policies",
        label: "Chính sách lương",
        roles: ROLE_GROUPS.hrAdmin,
      },
    ],
  },
  {
    label: "Quản trị truy cập",
    roles: ROLE_GROUPS.systemAdmin,
    icon: "access",
    children: [
      {
        path: "/admin/roles-permissions",
        label: "Phân quyền",
        roles: ROLE_GROUPS.systemAdmin,
      },
      {
        path: "/admin/audit-logs",
        label: "Nhật ký hệ thống",
        roles: ROLE_GROUPS.systemAdmin,
      },
      {
        path: "/admin/identity-auth",
        label: "Xác thực & MFA",
        roles: ROLE_GROUPS.systemAdmin,
      },
      {
        path: "/admin/accounts-access",
        label: "Tài khoản & truy cập",
        roles: ROLE_GROUPS.systemAdmin,
      },
    ],
  },
  {
    label: "Tuyển dụng",
    roles: ROLE_GROUPS.recruitmentPublic,
    icon: "recruitment",
    children: [
      {
        path: "/recruitment/jobs",
        label: "Vị trí tuyển dụng",
        roles: ROLE_GROUPS.recruitmentPublic,
      },
      {
        path: "/recruitment/demands",
        label: "Nhu cầu tuyển dụng",
        roles: ROLE_GROUPS.recruitmentOps,
      },
      {
        path: "/recruitment/candidates",
        label: "Ứng viên",
        roles: ROLE_GROUPS.recruitmentOps,
      },
      {
        path: "/recruitment/history",
        label: "Lịch sử ứng tuyển",
        roles: ROLE_GROUPS.recruitmentPublic,
      },
    ],
  },
  {
    label: "Hồ sơ & hợp đồng",
    roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern"],
    icon: "profile",
    children: [
      {
        path: "/employee-contract/my-profile",
        label: "Hồ sơ cá nhân",
        roles: ROLE_GROUPS.employeeSelf,
      },
      {
        path: "/employee-contract/profile-change",
        label: "Cập nhật hồ sơ",
        roles: ROLE_GROUPS.employeeSelf,
      },
      {
        path: "/employee-contract/contracts",
        label: "Hợp đồng của tôi",
        roles: ROLE_GROUPS.employeeSelf,
      },
      {
        path: "/employee-contract/contract-requests",
        label: "Ký/gia hạn hợp đồng",
        roles: ["Employee", "Manager", "Intern"],
      },
      {
        path: "/employee-contract/profile-setup",
        label: "Thiết lập hồ sơ",
        roles: ROLE_GROUPS.employeeAdminDirector,
      },
      {
        path: "/employee-contract/hr-contracts",
        label: "Soạn thảo hợp đồng",
        roles: ROLE_GROUPS.employeeAdmin,
      },
      {
        path: "/employee-contract/appendices",
        label: "Phụ lục hợp đồng",
        roles: ROLE_GROUPS.employeeAdminDirector,
      },
      {
        path: "/employee-contract/history",
        label: "Lịch sử biến động",
        roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern"],
      },
    ],
  },
  {
    label: "Chấm công & nghỉ phép",
    roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern"],
    icon: "attendance",
    children: [
      {
        path: "/attendance-leave/attendance",
        label: "Chấm công",
        roles: ROLE_GROUPS.attendanceSelf,
      },
      {
        path: "/attendance-leave/overtime",
        label: "Làm thêm giờ",
        roles: ROLE_GROUPS.attendanceSelf,
      },
      {
        path: "/attendance-leave/timesheet-summary",
        label: "Tổng hợp bảng công",
        roles: ROLE_GROUPS.attendanceSummary,
      },
      {
        path: "/attendance-leave/leave",
        label: "Nghỉ phép",
        roles: ROLE_GROUPS.leave,
      },
    ],
  },
  {
    label: "Hiệu suất & đào tạo",
    roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern"],
    icon: "training",
    children: [
      {
        path: "/performance-training/criteria",
        label: "Bộ chỉ tiêu KPI",
        roles: ROLE_GROUPS.performanceManagers,
      },
      {
        path: "/performance-training/result-update",
        label: "Cập nhật kết quả",
        roles: ROLE_GROUPS.performanceContributors,
      },
      {
        path: "/performance-training/penalties",
        label: "Vi phạm & điều chỉnh công",
        roles: ROLE_GROUPS.performanceDiscipline,
      },
      {
        path: "/performance-training/review-finalize",
        label: "Chốt đánh giá",
        roles: ROLE_GROUPS.performanceManagers,
      },
      {
        path: "/performance-training/development-training",
        label: "Đào tạo & phát triển",
        roles: ROLE_GROUPS.performanceManagers,
      },
    ],
  },
  {
    label: "Lương",
    roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern", "Collaborator"],
    icon: "payroll",
    children: [
      {
        path: "/payroll/my-salary",
        label: "Lương của tôi",
        roles: ROLE_GROUPS.payrollSlips,
      },
      {
        path: "/payroll/salary-formula",
        label: "Công thức lương",
        roles: ROLE_GROUPS.payrollSensitive,
      },
      {
        path: "/payroll/payroll-aggregation",
        label: "Tổng hợp lương thưởng",
        roles: ROLE_GROUPS.payrollSensitive,
      },
      {
        path: "/payroll/payslip",
        label: "Tra cứu phiếu lương",
        roles: ["Admin", "HR", "Manager", "Director"],
      },
      {
        path: "/payroll/adjustments",
        label: "Điều chỉnh lương",
        roles: ROLE_GROUPS.payrollAdjustments,
      },
      {
        path: "/payroll/project-bonuses",
        label: "Thưởng dự án",
        roles: ROLE_GROUPS.payrollAdjustments,
      },
      {
        path: "/payroll/external-timesheets",
        label: "Giờ công cộng tác viên",
        roles: ROLE_GROUPS.payrollAdjustments,
      },
    ],
  },
  {
    label: "Biến động nhân sự",
    roles: ROLE_GROUPS.personnelChange,
    icon: "personnel",
    children: [
      {
        path: "/personnel-change/promotion",
        label: "Thăng tiến & chính thức",
        roles: ROLE_GROUPS.personnelChange,
      },
      {
        path: "/personnel-change/senior-appointment",
        label: "Bổ nhiệm cấp cao",
        roles: ROLE_GROUPS.personnelChangeExecutive,
      },
      {
        path: "/personnel-change/termination",
        label: "Nghỉ việc chủ động",
        roles: ROLE_GROUPS.personnelChange,
      },
      {
        path: "/personnel-change/dismissal",
        label: "Kỷ luật & sa thải",
        roles: ROLE_GROUPS.personnelChangeExecutive,
      },
      {
        path: "/personnel-change/internal-transfer",
        label: "Thuyên chuyển nội bộ",
        roles: ROLE_GROUPS.personnelChange,
      },
    ],
  },
  {
    path: "/document-forms/create",
    label: "Xuất biểu mẫu",
    roles: ROLE_GROUPS.documentForms,
    icon: "forms",
    module: "Biểu mẫu",
  },
  {
    label: "Phê duyệt",
    roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern", "Candidate"],
    icon: "approvals",
    children: [
      {
        path: "/approvals",
        label: "Phê duyệt của tôi",
        roles: ROLE_GROUPS.approvalInbox,
      },
      {
        path: "/approvals/tracking",
        label: "Theo dõi trạng thái",
        roles: ROLE_GROUPS.approvalTracking,
      },
    ],
  },
];
