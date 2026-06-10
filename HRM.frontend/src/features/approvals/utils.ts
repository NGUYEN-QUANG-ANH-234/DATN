import type { ApprovalModule } from "./types";

export const unwrapData = <T,>(value: unknown): T[] => {
  const raw = value as { data?: T[]; Data?: T[] };
  return raw?.data || raw?.Data || [];
};

export const getRole = (role?: string | null) => String(role || "").trim();

export const roleLabel = (role?: string | null) => {
  const map: Record<string, string> = {
    Admin: "Quản trị",
    HR: "HR",
    Manager: "Quản lý",
    Director: "Giám đốc",
    Employee: "Nhân viên",
    Intern: "Thực tập sinh",
    Candidate: "Ứng viên",
    Collaborator: "Cộng tác viên",
  };

  const normalized = getRole(role);
  return map[normalized] || normalized || "Người dùng";
};

export const isApprovalRole = (role: string) =>
  ["Admin", "HR", "Manager", "Director", "Employee"].includes(role);

export const formatDate = (value?: string | null) => {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return date.toLocaleDateString("vi-VN");
};

export const normalizeText = (value: unknown) =>
  String(value || "").toLowerCase().trim();

export const moduleTone = (module: ApprovalModule) => {
  const map: Record<ApprovalModule, string> = {
    RECRUITMENT: "bg-blue-50 text-blue-700 border-blue-200",
    CANDIDATE: "bg-violet-50 text-violet-700 border-violet-200",
    CONTRACT: "bg-emerald-50 text-emerald-700 border-emerald-200",
    PROFILE: "bg-cyan-50 text-cyan-700 border-cyan-200",
    ONBOARDING: "bg-indigo-50 text-indigo-700 border-indigo-200",
    ADDENDUM: "bg-amber-50 text-amber-700 border-amber-200",
    OVERTIME: "bg-orange-50 text-orange-700 border-orange-200",
    LEAVE: "bg-rose-50 text-rose-700 border-rose-200",
    PAYROLL: "bg-lime-50 text-lime-700 border-lime-200",
    PERSONNEL_CHANGE: "bg-sky-50 text-sky-700 border-sky-200",
  };

  return map[module];
};

export const statusLabel = (status: string) => {
  const map: Record<string, string> = {
    Pending: "Đang chờ duyệt",
    PendingHR: "Chờ HR",
    Pending_HR: "Chờ HR",
    PendingDept: "Chờ Trưởng phòng",
    PendingManager: "Chờ Trưởng phòng",
    PendingEmployee: "Chờ người lao động xác nhận",
    PendingDirector: "Chờ Giám đốc",
    PendingApproval: "Chờ phê duyệt",
    PendingCurrentManagerOpinion: "Chờ quản lý hiện tại",
    PendingEmployeeNotification: "Chờ thông báo nhân viên",
    PendingEmployeeExplanation: "Chờ nhân viên giải trình",
    PendingManagerReview: "Chờ quản lý duyệt",
    Draft: "Bản nháp",
    Calculated: "Đã tính",
    HRReviewed: "HR đã kiểm tra",
    Approved: "Đã duyệt",
    ApprovedByDirector: "Giám đốc đã duyệt",
    Active: "Có hiệu lực",
    Rejected: "Từ chối",
    EmployeeDeclined: "Nhân viên từ chối",
    RejectedByDept: "Trưởng phòng từ chối",
    RejectedByDirector: "Giám đốc từ chối",
    Locked: "Đã khóa",
    Finalized: "Đã chốt",
    Paid: "Đã chi trả",
    Completed: "Hoàn tất",
    ReadyToExecute: "Sẵn sàng thực thi",
    PendingContractFlow: "Chờ luồng hợp đồng",
    ContractNegotiating: "Đang thương lượng hợp đồng",
    ContractAccepted: "Hợp đồng đã chấp thuận",
    ContractRejected: "Hợp đồng bị từ chối",
    New: "Mới nộp",
    Interview_Pending: "Chờ Trưởng phòng",
    Interview_Passed: "Chờ Giám đốc",
    Offer: "Đã chốt offer",
    Cancelled: "Đã hủy",
    SLA_Expired: "Quá hạn SLA",
  };

  return map[status] || status;
};
