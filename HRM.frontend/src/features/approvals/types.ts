import type { ReactNode } from "react";

export type ApprovalModule =
  | "RECRUITMENT"
  | "CANDIDATE"
  | "CONTRACT"
  | "PROFILE"
  | "ONBOARDING"
  | "ADDENDUM"
  | "OVERTIME"
  | "LEAVE";

export type ApprovalActionKind =
  | "approve"
  | "reject"
  | "open"
  | "reconcile";

export interface ApprovalAction {
  kind: ApprovalActionKind;
  label: string;
  tone?: "primary" | "danger" | "secondary";
  run: () => Promise<unknown> | unknown;
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

export type PendingApprovalDto = {
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
};

export type RoleOption = {
  id: number;
  name: string;
  roleName?: string;
};

export type ApprovalWorkspaceFilters = {
  module: "ALL" | ApprovalModule;
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
  { value: "ALL", label: "Tất cả module" },
  { value: "RECRUITMENT", label: "Tuyển dụng" },
  { value: "CANDIDATE", label: "Ứng viên" },
  { value: "CONTRACT", label: "Hợp đồng" },
  { value: "PROFILE", label: "Hồ sơ" },
  { value: "ONBOARDING", label: "Onboarding" },
  { value: "ADDENDUM", label: "Phụ lục" },
  { value: "OVERTIME", label: "OT" },
  { value: "LEAVE", label: "Nghỉ phép" },
];
