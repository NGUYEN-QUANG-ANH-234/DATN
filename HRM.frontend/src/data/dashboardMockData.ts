import {
  BarChart3,
  CalendarCheck,
  ClipboardList,
  FilePlus2,
  FileText,
  PieChart,
  UserPlus,
  UsersRound,
} from "lucide-react";

export const dashboardMetrics = [
  {
    label: "Tổng nhân sự",
    value: "256",
    change: "+12% so với tháng trước",
    tone: "orange",
    icon: UsersRound,
  },
  {
    label: "Chấm công hôm nay",
    value: "92.5%",
    change: "Có mặt: 237 / 256",
    tone: "success",
    icon: CalendarCheck,
  },
  {
    label: "Vị trí đang tuyển",
    value: "23",
    change: "+4 vị trí trong tuần",
    tone: "info",
    icon: UserPlus,
  },
  {
    label: "Trạng thái lương",
    value: "Đã duyệt",
    change: "Bảng lương tháng 05/2026",
    tone: "warning",
    icon: FileText,
  },
];

export const employeeTrendData = [
  { month: "12/2025", employees: 210, hires: 8 },
  { month: "01/2026", employees: 218, hires: 10 },
  { month: "02/2026", employees: 225, hires: 12 },
  { month: "03/2026", employees: 235, hires: 14 },
  { month: "04/2026", employees: 244, hires: 12 },
  { month: "05/2026", employees: 256, hires: 12 },
];

export const departmentData = [
  { name: "Engineering", value: 90, percent: "35%", color: "#FF7A00" },
  { name: "BIM", value: 51, percent: "20%", color: "#FF9F43" },
  { name: "Operations", value: 38, percent: "15%", color: "#FFC078" },
  { name: "HR & Admin", value: 26, percent: "10%", color: "#FFE0B8" },
  { name: "Finance", value: 20, percent: "8%", color: "#D1D5DB" },
  { name: "Khác", value: 31, percent: "12%", color: "#9CA3AF" },
];

export const quickActions = [
  { label: "Thêm nhân sự", path: "/employee-contract/profile-setup", icon: UserPlus },
  { label: "Tạo đơn nghỉ", path: "/attendance-leave/leave", icon: FilePlus2 },
  { label: "Nhu cầu tuyển", path: "/recruitment/demands/create", icon: ClipboardList },
  { label: "Chấm công", path: "/attendance-leave/attendance", icon: CalendarCheck },
  { label: "Tính lương", path: "/payroll/payroll-aggregation", icon: PieChart },
  { label: "Báo cáo", path: "/approvals/tracking", icon: BarChart3 },
];

export const recruitmentPipeline = [
  { stage: "Ứng tuyển", count: 48, delta: "+6", variant: "orange" },
  { stage: "Sàng lọc", count: 22, delta: "+4", variant: "info" },
  { stage: "Phỏng vấn", count: 12, delta: "+2", variant: "warning" },
  { stage: "Đánh giá", count: 7, delta: "-1", variant: "neutral" },
  { stage: "Offer", count: 5, delta: "+1", variant: "success" },
  { stage: "Đã tuyển", count: 3, delta: "+1", variant: "success" },
];

export const recentActivities = [
  {
    name: "Nguyễn Văn Minh",
    action: "Gửi đơn nghỉ phép năm",
    status: "Đã duyệt",
    variant: "success",
    time: "09:12",
  },
  {
    name: "Trần Thị Hạnh",
    action: "Check-in muộn 12 phút",
    status: "Đi muộn",
    variant: "warning",
    time: "08:42",
  },
  {
    name: "Lê Hoàng Nam",
    action: "Hoàn thành thử việc",
    status: "Hoàn tất",
    variant: "success",
    time: "Hôm qua",
  },
  {
    name: "Phạm Đức Đạt",
    action: "Cập nhật thông tin hồ sơ",
    status: "Chờ HR",
    variant: "orange",
    time: "Hôm qua",
  },
];

export const announcements = [
  {
    title: "Lịch nghỉ lễ 30/04 - 01/05",
    content: "Văn phòng tạm nghỉ theo lịch công ty. Các đơn OT cần gửi trước 17:00.",
    tag: "Thông báo",
  },
  {
    title: "Đánh giá hiệu suất Q2/2026",
    content: "Quản lý hoàn tất chốt KPI từ ngày 10/06 đến 24/06.",
    tag: "KPI",
  },
];
