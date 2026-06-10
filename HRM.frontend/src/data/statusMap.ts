import type { BadgeVariant } from "../components/ui";

export type StatusConfig = {
  label: string;
  variant: BadgeVariant;
};

export const normalizeStatusKey = (status?: string | null) =>
  (status || "").replace(/[_\s-]/g, "").toLowerCase();

export const statusMap: Record<string, StatusConfig> = {
  active: { label: "Đang áp dụng", variant: "success" },
  inactive: { label: "Tạm tắt", variant: "neutral" },
  deactivated: { label: "Đã vô hiệu hóa", variant: "neutral" },
  disabled: { label: "Tạm tắt", variant: "neutral" },
  enabled: { label: "Đang áp dụng", variant: "success" },
  suspended: { label: "Tạm ngừng", variant: "warning" },

  draft: { label: "Nháp", variant: "neutral" },
  pending: { label: "Chờ xử lý", variant: "warning" },
  pendingapproval: { label: "Chờ duyệt", variant: "warning" },
  pendingreview: { label: "Chờ xem xét", variant: "warning" },
  pendingmanager: { label: "Chờ quản lý duyệt", variant: "warning" },
  pendinghr: { label: "Chờ HR xử lý", variant: "warning" },
  pendingdirector: { label: "Chờ giám đốc duyệt", variant: "warning" },
  pendingemployeeupdate: { label: "Chờ nhân viên cập nhật", variant: "warning" },
  pendingevaluation: { label: "Chờ đánh giá", variant: "warning" },

  approved: { label: "Đã duyệt", variant: "success" },
  approvedbydirector: { label: "Đã duyệt", variant: "success" },
  accepted: { label: "Đã chấp thuận", variant: "success" },
  signed: { label: "Đã ký", variant: "success" },
  rejected: { label: "Từ chối", variant: "danger" },
  rejectedbydept: { label: "Trưởng phòng từ chối", variant: "danger" },
  rejectedbydirector: { label: "Giám đốc từ chối", variant: "danger" },
  declined: { label: "Từ chối", variant: "danger" },
  cancelled: { label: "Đã hủy", variant: "neutral" },
  completed: { label: "Hoàn tất", variant: "success" },
  finalized: { label: "Đã chốt", variant: "success" },
  locked: { label: "Đã khóa", variant: "danger" },
  open: { label: "Đang mở", variant: "success" },
  closed: { label: "Đã đóng", variant: "neutral" },

  calculated: { label: "Đã tính", variant: "info" },
  hrreviewed: { label: "HR đã kiểm tra", variant: "info" },
  payrolllocked: { label: "Đã khóa lương", variant: "orange" },
  paid: { label: "Đã thanh toán", variant: "success" },

  reconciled: { label: "Đã đối chiếu", variant: "info" },
  autoreconciled: { label: "Tự động đối chiếu", variant: "info" },
  autoapproved: { label: "Tự động duyệt", variant: "info" },
  autoevaluated: { label: "Tự động đánh giá", variant: "info" },
  autocompleted: { label: "Tự động hoàn tất", variant: "info" },

  inprogress: { label: "Đang xử lý", variant: "info" },
  negotiating: { label: "Đang trao đổi", variant: "info" },
  assigned: { label: "Đã giao", variant: "info" },
  overdue: { label: "Quá hạn", variant: "danger" },
  expired: { label: "Hết hạn", variant: "danger" },
  needmoretraining: { label: "Cần đào tạo bổ sung", variant: "warning" },
  reworkrequired: { label: "Cần chỉnh sửa", variant: "warning" },
  extended: { label: "Gia hạn", variant: "warning" },

  present: { label: "Có mặt", variant: "success" },
  absent: { label: "Vắng mặt", variant: "danger" },
  late: { label: "Đi muộn", variant: "warning" },
  earlyleave: { label: "Về sớm", variant: "warning" },
  leave: { label: "Nghỉ phép", variant: "info" },

  success: { label: "Thành công", variant: "success" },
  failed: { label: "Thất bại", variant: "danger" },
  error: { label: "Lỗi", variant: "danger" },
  warning: { label: "Cảnh báo", variant: "warning" },
};

export const getStatusConfig = (
  status?: string | null,
  fallbackLabel?: string,
): StatusConfig => {
  const key = normalizeStatusKey(status);
  return (
    statusMap[key] ?? {
      label: fallbackLabel || status || "Không xác định",
      variant: "neutral",
    }
  );
};

export const getStatusLabel = (status?: string | null, fallbackLabel?: string) =>
  getStatusConfig(status, fallbackLabel).label;

export const getStatusVariant = (status?: string | null) => getStatusConfig(status).variant;
