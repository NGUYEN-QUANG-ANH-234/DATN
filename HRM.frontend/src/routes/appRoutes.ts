export type AppRouteMeta = {
  path: string;
  label: string;
  module: string;
  roles: string[];
};

export const appRoutes: AppRouteMeta[] = [
  {
    path: "/dashboard",
    label: "Tổng quan",
    module: "Dashboard",
    roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern", "Candidate"],
  },
  {
    path: "/system-config/positions-departments",
    label: "F0.5 Vị trí & phòng ban",
    module: "Module 0",
    roles: ["Admin", "HR", "Director"],
  },
  {
    path: "/system-config/salary-variables",
    label: "F0.1 Cấu hình biến lương",
    module: "Module 0",
    roles: ["Admin"],
  },
  {
    path: "/system-config/payroll-policies",
    label: "F0.7 Cấu hình chính sách lương",
    module: "Module 0",
    roles: ["Admin"],
  },
  {
    path: "/attendance-leave/timesheet-summary",
    label: "F4.3 Tổng hợp bảng chấm công",
    module: "Module 4",
    roles: ["Admin", "HR"],
  },
  {
    path: "/attendance-leave/leave",
    label: "F4.4 Quản lý nghỉ phép",
    module: "Module 4",
    roles: ["Admin", "Manager", "Director", "Employee"],
  },
  {
    path: "/performance-training/criteria",
    label: "F5.1 Thiết lập bộ chỉ tiêu KPI",
    module: "Module 5",
    roles: ["Admin", "HR", "Manager"],
  },
  {
    path: "/performance-training/result-update",
    label: "F5.2 Nhân viên cập nhật kết quả",
    module: "Module 5",
    roles: ["Admin", "HR", "Manager", "Employee", "Intern"],
  },
  {
    path: "/performance-training/penalties",
    label: "F5.3 Vi phạm & điều chỉnh công",
    module: "Module 5",
    roles: ["Admin", "HR", "Manager", "Director"],
  },
  {
    path: "/performance-training/review-finalize",
    label: "F5.4 Đánh giá và chốt kết quả",
    module: "Module 5",
    roles: ["Admin", "HR", "Manager"],
  },
  {
    path: "/performance-training/development-training",
    label: "F5.5 Đánh giá phát triển và đào tạo",
    module: "Module 5",
    roles: ["Admin", "HR", "Manager"],
  },
  {
    path: "/payroll/salary-formula",
    label: "F6.1 Định nghĩa công thức lương",
    module: "Module 6",
    roles: ["Admin", "HR", "Director"],
  },
  {
    path: "/payroll/payroll-aggregation",
    label: "F6.2 Tổng hợp lương thưởng",
    module: "Module 6",
    roles: ["Admin", "HR", "Director"],
  },
  {
    path: "/payroll/payslip",
    label: "F6.3 Phân phối và tra cứu phiếu lương",
    module: "Module 6",
    roles: ["Admin", "HR", "Manager", "Director", "Employee", "Intern", "Collaborator"],
  },
  {
    path: "/payroll/adjustments",
    label: "Điều chỉnh nghiệp vụ lương",
    module: "Module 6",
    roles: ["Admin", "HR"],
  },
  {
    path: "/payroll/external-timesheets",
    label: "Giờ công CTV",
    module: "Module 6",
    roles: ["Admin", "HR"],
  },
  {
    path: "/personnel-change/promotion",
    label: "F7.1 Đề xuất thăng tiến",
    module: "Module 7",
    roles: ["Admin", "HR", "Manager", "Director"],
  },
  {
    path: "/personnel-change/senior-appointment",
    label: "F7.2 Bổ nhiệm nhân sự cấp cao",
    module: "Module 7",
    roles: ["Admin", "HR", "Director"],
  },
  {
    path: "/personnel-change/termination",
    label: "F7.3 Chấm dứt hợp đồng chủ động",
    module: "Module 7",
    roles: ["Admin", "HR", "Manager", "Director"],
  },
  {
    path: "/personnel-change/dismissal",
    label: "F7.4 Sa thải bị động",
    module: "Module 7",
    roles: ["Admin", "HR", "Director"],
  },
  {
    path: "/personnel-change/internal-transfer",
    label: "F7.5 Thuyên chuyển nội bộ",
    module: "Module 7",
    roles: ["Admin", "HR", "Manager", "Director"],
  },
];
