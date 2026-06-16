import type { ReactNode } from "react";

export type ApprovalModule =
  | "RECRUITMENT"
  | "CANDIDATE"
  | "CONTRACT"
  | "PROFILE"
  | "ONBOARDING"
  | "ADDENDUM"
  | "OVERTIME"
  | "LEAVE"
  | "PAYROLL"
  | "PERSONNEL_CHANGE"
  | "PERFORMANCE";

export type ApprovalActionKind =
  | "approve"
  | "reject"
  | "revision"
  | "open"
  | "reconcile";

export interface ApprovalAction {
  kind: ApprovalActionKind;
  label: string;
  tone?: "primary" | "danger" | "secondary";
  requiresNote?: boolean;
  run: (note?: string) => Promise<unknown> | unknown;
}

export interface ApprovalItem {
  id: string;
  module: ApprovalModule;
  moduleLabel: string;
  source: string;
  title: string;
  subtitle?: string;
  owner?: string;
  department?: string | null;
  status: string;
  statusLabel: string;
  date?: string | null;
  deadline?: string | null;
  details?: ReactNode;
  actions: ApprovalAction[];
}

export interface TrackingItem {
  id: string;
  module: ApprovalModule;
  moduleLabel: string;
  title: string;
  owner?: string;
  department?: string | null;
  status: string;
  statusLabel: string;
  date?: string | null;
  scopeLabel: string;
}

export type PendingApprovalActionDto = {
  kind: ApprovalActionKind | string;
  label: string;
  tone?: "primary" | "danger" | "secondary" | string;
  requiresNote?: boolean;
  endpoint?: string;
  method?: string;
};

export type PendingApprovalDetailFieldDto = {
  label: string;
  value?: string | null;
};

export type PendingApprovalDto = {
  approvalRequestId?: number;
  moduleCode: string;
  referenceId: number;
  level: number;
  createdAt?: string;
  title?: string;
  description?: string;
  departmentName?: string;
  positionName?: string;
  quantity?: number;
  deadline?: string;
  cvFilePath?: string;
  status?: string;
  statusLabel?: string;
  detailRoute?: string;
  detailTitle?: string;
  actions?: PendingApprovalActionDto[];
  detailFields?: PendingApprovalDetailFieldDto[];
};

export type RoleOption = {
  id: number;
  name: string;
  roleName?: string;
};

export type ApprovalWorkspaceFilters = {
  module: "ALL" | ApprovalModule;
  status: "ALL" | string;
  owner: string;
  deadline: "ALL" | "OVERDUE" | "TODAY" | "NEXT_7_DAYS" | "NO_DEADLINE";
  query: string;
  fromDate: string;
  toDate: string;
};

export type ApprovalTrackingFilters = {
  module: "ALL" | ApprovalModule;
  status: string;
  query: string;
};

export const APPROVAL_MODULES: Array<{
  value: "ALL" | ApprovalModule;
  label: string;
}> = [
  { value: "ALL", label: "Tất cả" },
  { value: "RECRUITMENT", label: "Tuyển dụng" },
  { value: "CANDIDATE", label: "Ứng viên" },
  { value: "CONTRACT", label: "Hợp đồng" },
  { value: "PROFILE", label: "Hồ sơ" },
  { value: "ONBOARDING", label: "Tiếp nhận hồ sơ" },
  { value: "ADDENDUM", label: "Phụ lục" },
  { value: "OVERTIME", label: "Làm thêm giờ" },
  { value: "LEAVE", label: "Nghỉ phép" },
  { value: "PAYROLL", label: "Lương" },
  { value: "PERSONNEL_CHANGE", label: "Biến động nhân sự" },
  { value: "PERFORMANCE", label: "Hiệu suất" },
];
