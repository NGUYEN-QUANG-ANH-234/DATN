export type PenaltyTab = "attendance" | "manual" | "history";

export type PenaltyRecordStatus =
  | "PendingEmployeeExplanation"
  | "PendingHRReview"
  | "PendingDirectorApproval"
  | "Approved"
  | "Applied"
  | "Rejected"
  | "Cancelled"
  | string;

export type PenaltySeverity = "Low" | "Medium" | "High" | "Critical" | string;

export interface PenaltyRecord {
  id: number;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  departmentName?: string | null;
  period: string;
  sourceType: string;
  referenceId?: number | null;
  ruleCode: string;
  penaltyPoint: number;
  reason?: string | null;
  status: PenaltyRecordStatus;
  occurredAt?: string | null;
  violationType: string;
  severity: PenaltySeverity;
  affectsAttendance: boolean;
  affectsPerformance: boolean;
  affectsPersonnelDecision: boolean;
  createdBySystem: boolean;
  createdByAccountId?: number | null;
  employeeExplanation?: string | null;
  managerNote?: string | null;
  hrNote?: string | null;
  evidenceFilePath?: string | null;
  approvedByAccountId?: number | null;
  attendanceAdjustmentLogId?: number | null;
  deductedMinutes?: number | null;
  deductedWorkday?: number | null;
  performanceReviewId?: number | null;
  reviewedAt?: string | null;
  appliedAt?: string | null;
  createdAt: string;
  requiresDirectorReview: boolean;
}

export interface CreateManualPenaltyRecordRequest {
  employeeId: number;
  occurredAt: string;
  period?: string | null;
  violationType: string;
  severity: string;
  description: string;
  penaltyPoint: number;
  requiresEmployeeExplanation: boolean;
  affectsAttendance: boolean;
  affectsPerformance: boolean;
  affectsPersonnelDecision: boolean;
  deductedMinutes?: number | null;
  deductedWorkday?: number | null;
  evidenceFilePath?: string | null;
  managerNote?: string | null;
  ruleCode?: string | null;
}

export interface ReviewPenaltyRecordRequest {
  isApproved: boolean;
  note?: string | null;
}

export interface PenaltyApiResponse<T> {
  success?: boolean;
  message?: string;
  data: T;
}
