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
    label: "Cơ cấu tổ chức",
    roles: ["Admin"],
    children: [
      {
        path: "/organization/department-management",
        label: "Tổng quan Cơ cấu tổ chức",
        roles: ["HR", "Director", "Admin"],
      },
    ],
  },
  {
    label: "Tuyển dụng",
    roles: ["Admin", "Candidate", "HR", "Manager", "Director", "Employee"],
    children: [
      {
        path: "/recruitment/all",
        label: "Tất cả vị trí tuyển dụng",
        roles: ["Admin", "HR", "Manager", "Director", "Candidate"],
      },
      {
        path: "/recruitment/create",
        label: "Tạo việc làm mới",
        roles: ["Admin", "HR", "Manager", "Director"],
      },
      {
        path: "/recruitment/approval-inbox",
        label: "Hộp thư phê duyệt",
        roles: ["Admin", "HR", "Director", "Manager"],
      },
      {
        path: "/recruitment/candidates-management", // Đã đồng bộ với App.tsx
        label: "Quản lý Ứng viên",
        roles: ["Admin", "HR", "Manager", "Director"],
      },
      {
        path: "/recruitment/history", // Đã bổ sung trang tra cứu lịch sử ứng tuyển
        label: "Lịch sử ứng tuyển",
        roles: ["Admin", "HR", "Manager", "Director", "Candidate", "Employee"],
      },
    ],
  },
  {
    label: "Hồ sơ nhân viên",
    roles: [
      "Admin",
      "HR",
      "Manager",
      "Candidate",
      "Employee",
      "Director",
      "Intern",
    ],
    children: [
      {
        path: "/employees/my-profile",
        label: "Tổng quan Hồ sơ nhân viên",
        roles: ["HR", "Director", "Admin", "Employee"],
      },
      {
        path: "/employees/my-contracts",
        label: "Tổng quan Hợp đồng nhân viên",
        roles: ["HR", "Director", "Admin", "Employee"],
      },
      {
        path: "/employees/history",
        label: "Lịch sử biến động",
        roles: ["HR", "Director", "Admin", "Employee", "Manager", "Intern"],
      },
      {
        path: "/employees/profile-update",
        label: "Thay đổi thông tin cá nhân",
        roles: ["HR", "Director", "Admin", "Employee"],
      },
      {
        path: "/employees/onboarding",
        label: "Onboarding",
        roles: ["HR", "Director", "Admin"],
      },
      {
        path: "/employees/hr-profile-review",
        label: "Duyệt hồ sơ nhân viên",
        roles: ["HR", "Director", "Admin"],
      },
      {
        path: "/employees/contract-management",
        label: "Ký kết / Gia hạn hợp đồng",
        roles: ["Employee", "Manager", "Intern"],
      },
      {
        path: "/employees/hr-contract-management",
        label: "Quản lý Hợp đồng (HR)",
        roles: ["HR", "Admin"],
      },
      {
        path: "/employees/director-contract-approval",
        label: "Phê duyệt Hợp đồng (GĐ)",
        roles: ["Director", "Admin"],
      },
      {
        path: "/employees/contract-addendums",
        label: "Phụ lục hợp đồng",
        roles: ["HR", "Director", "Admin"],
      },
    ],
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
    label: "Yêu cầu",
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
      {
        path: "/system/schedule-configuration",
        label: "Cấu hình Lịch làm việc",
        roles: ["HR", "Director", "Admin"],
      },
    ],
  },
];
