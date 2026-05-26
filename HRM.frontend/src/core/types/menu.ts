export interface MenuItem {
  path?: string;
  label: string;
  roles: string[];
  icon?: string;
  children?: MenuItem[];
}

export const MENU_ITEMS: MenuItem[] = [
  {
    path: "/dashboard",
    label: "Tổng quan",
    roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern", "Candidate"],
  },
  {
    label: "Cơ cấu tổ chức",
    roles: ["Admin", "HR", "Director"],
    children: [
      {
        path: "/organization/department-management",
        label: "Sơ đồ tổ chức",
        roles: ["Admin", "HR", "Director"],
      },
    ],
  },
  {
    label: "Tuyển dụng",
    roles: ["Admin", "HR", "Manager", "Director", "Employee", "Candidate"],
    children: [
      {
        path: "/recruitment/all",
        label: "Vị trí tuyển dụng",
        roles: ["Admin", "HR", "Manager", "Director", "Employee", "Candidate"],
      },
      {
        path: "/recruitment/create",
        label: "Tạo nhu cầu tuyển dụng",
        roles: ["Admin", "HR", "Manager"],
      },
      {
        path: "/recruitment/history",
        label: "Lịch sử ứng tuyển",
        roles: ["Admin", "HR", "Manager", "Director", "Employee", "Candidate"],
      },
      {
        path: "/recruitment/candidates-management",
        label: "Theo dõi ứng viên",
        roles: ["Admin", "HR", "Manager", "Director"],
      },
    ],
  },
  {
    label: "Hồ sơ & hợp đồng",
    roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern"],
    children: [
      {
        path: "/employees/my-profile",
        label: "Hồ sơ cá nhân",
        roles: ["HR", "Manager", "Director", "Employee", "Intern"],
      },
      {
        path: "/employees/profile-update",
        label: "Cập nhật hồ sơ",
        roles: ["HR", "Manager", "Director", "Employee", "Intern"],
      },
      {
        path: "/employees/my-contracts",
        label: "Hợp đồng của tôi",
        roles: ["HR", "Manager", "Director", "Employee", "Intern"],
      },
      {
        path: "/employees/contract-management",
        label: "Ký kết / gia hạn hợp đồng",
        roles: ["Employee", "Manager", "Intern"],
      },
      {
        path: "/employees/hr-contract-management",
        label: "Soạn thảo hợp đồng",
        roles: ["Admin", "HR"],
      },
      {
        path: "/employees/contract-addendums",
        label: "Phụ lục hợp đồng",
        roles: ["HR", "Director", "Admin"],
      },
      {
        path: "/employees/history",
        label: "Lịch sử biến động",
        roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern"],
      },
      {
        path: "/employees/onboarding",
        label: "Onboarding",
        roles: ["Admin", "HR", "Director"],
      },
    ],
  },
  {
    label: "Chấm công & nghỉ phép",
    roles: ["Admin", "HR", "Manager", "Director", "Employee"],
    children: [
      {
        path: "/attendance",
        label: "Chấm công cá nhân",
        roles: ["Admin", "HR", "Manager", "Employee"],
      },
      {
        path: "/attendance/overtime",
        label: "Làm thêm giờ (OT)",
        roles: ["Admin", "HR", "Manager", "Employee"],
      },
      {
        path: "/attendance/leaves",
        label: "Nghỉ phép",
        roles: ["Admin", "Manager", "Director", "Employee"],
      },
      {
        path: "/attendance/summary",
        label: "Tổng hợp bảng công",
        roles: ["Admin", "HR"],
      },
    ],
  },
  {
    label: "Phê duyệt",
    roles: ["Admin", "HR", "Manager", "Director"],
    children: [
      {
        path: "/approvals",
        label: "Phê duyệt của tôi",
        roles: ["Admin", "HR", "Manager", "Director"],
      },
      {
        path: "/approvals/tracking",
        label: "Theo dõi trạng thái",
        roles: ["Admin", "HR", "Manager", "Director"],
      },
    ],
  },
  {
    label: "Theo dõi yêu cầu",
    roles: ["Employee", "Intern", "Candidate"],
    children: [
      {
        path: "/approvals/tracking",
        label: "Trạng thái của tôi",
        roles: ["Employee", "Intern", "Candidate"],
      },
    ],
  },
  {
    label: "Công việc & đào tạo",
    roles: ["Admin", "HR", "Manager", "Employee", "Intern"],
    children: [
      {
        path: "/tasks/workspace",
        label: "Cập nhật tiến độ / duyệt task",
        roles: ["Admin", "HR", "Manager", "Employee", "Intern"],
      },
      {
        path: "/tasks/kpi-import",
        label: "Import KPI đầu kỳ",
        roles: ["Admin", "HR", "Manager"],
      },
      {
        path: "/tasks/performance-evaluation",
        label: "Đánh giá KPI",
        roles: ["Admin", "HR", "Manager"],
      },
      {
        path: "/tasks/training-evaluation",
        label: "Theo dõi & đánh giá đào tạo",
        roles: ["Admin", "HR", "Manager"],
      },
    ],
  },
  {
    path: "/payroll",
    label: "Lương & phụ cấp",
    roles: ["Admin", "HR"],
  },
  {
    path: "/requests",
    label: "Yêu cầu nhân sự",
    roles: ["Admin", "HR", "Manager", "Employee"],
  },
  {
    label: "Hệ thống",
    roles: ["Admin"],
    children: [
      {
        path: "/system",
        label: "Tổng quan hệ thống",
        roles: ["Admin"],
      },
      {
        path: "/system/salary-variables",
        label: "Cấu hình biến lương",
        roles: ["Admin"],
      },
      {
        path: "/system/sla",
        label: "Cấu hình SLA",
        roles: ["Admin"],
      },
      {
        path: "/system/templates",
        label: "Mẫu thông báo",
        roles: ["Admin"],
      },
      {
        path: "/system/attendance-config",
        label: "Cấu hình chấm công",
        roles: ["Admin"],
      },
      {
        path: "/system/schedule-configuration",
        label: "Cấu hình lịch làm việc",
        roles: ["Admin"],
      },
      {
        path: "/system/rbac",
        label: "Quản lý quyền",
        roles: ["Admin"],
      },
      {
        path: "/system/audit-logs",
        label: "Nhật ký kiểm toán",
        roles: ["Admin"],
      },
      {
        path: "/system/account-management",
        label: "Quản lý tài khoản",
        roles: ["Admin"],
      },
    ],
  },
];
