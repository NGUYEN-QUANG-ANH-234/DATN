export interface MenuItem {
  path?: string; // Khối cha có thể không cần path nếu chỉ dùng để mở dropdown
  label: string;
  roles: string[];
  icon?: string; // Tương lai bạn nên thêm icon
  children?: MenuItem[];
}

export const MENU_ITEMS: MenuItem[] = [
  {
    path: "/dashboard",
    label: "Tổng quan",
    roles: ["Admin", "HR", "Manager", "Employee", "Candidate"],
  },
  {
    path: "/organization",
    label: "Cơ cấu tổ chức",
    roles: ["Admin", "HR", "Manager", "Candidate"],
  },
  { path: "/recruitment", label: "Tuyển dụng", roles: ["Admin", "HR"] },
  {
    path: "/employees",
    label: "Hồ sơ nhân viên",
    roles: ["Admin", "HR", "Manager"],
  },
  {
    path: "/attendance",
    label: "Chấm công",
    roles: ["Admin", "HR", "Manager", "Employee"],
  },
  {
    path: "/tasks",
    label: "Công việc & Đào tạo",
    roles: ["Admin", "Manager", "Employee"],
  },
  { path: "/payroll", label: "Lương & Phụ cấp", roles: ["Admin", "HR"] },
  {
    path: "/requests",
    label: "Yêu cầu & Bàn giao",
    roles: ["Admin", "HR", "Manager", "Employee"],
  },
  {
    label: "Hệ thống",
    roles: ["Admin"],
    children: [
      {
        path: "/system",
        label: "Tổng quan Hệ thống",
        roles: ["Admin"],
      },
      {
        path: "/system/salary-variables",
        label: "Cấu hình Biến lương",
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
        label: "Cấu hình Chấm công",
        roles: ["Admin"],
      },
      {
        path: "/system/rbac",
        label: "Quản lý Quyền",
        roles: ["Admin"],
      },
      {
        path: "/system/audit-logs",
        label: "Xem nhật ký kiểm toán",
        roles: ["Admin"],
      },
      {
        path: "/system/account-management",
        label: "Quản lý Tài khoản",
        roles: ["Admin"],
      },
    ],
  },
];
