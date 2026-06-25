import axiosClient from "../../../core/api/axiosClient";

export interface AttendanceSummary {
  id: number;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  departmentName: string | null;
  month: number;
  year: number;
  workDays: number;
  workedMinutes: number;
  workedHours: number;
  payableWorkHours: number;
  lateMinutes: number;
  earlyLeaveMinutes: number;
  actualOtMinutes: number;
  approvalStatus: "Draft" | "PendingHRReview" | "Approved" | "Rejected" | "Locked";
  submittedByAccountId: number | null;
  submittedAt: string | null;
  approvedByAccountId: number | null;
  approvedAt: string | null;
  lockedByAccountId: number | null;
  lockedAt: string | null;
  periodNote: string | null;
  isPayrollLocked: boolean;
  generatedAt: string;
}

export interface AttendanceDailySummary {
  id: number;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  departmentName: string | null;
  workDate: string;
  firstCheckIn: string | null;
  lastCheckOut: string | null;
  workingMinutes: number;
  lateMinutes: number;
  earlyLeaveMinutes: number;
  overtimeMinutes: number;
  workdayValue: number;
  attendanceStatus:
    | "Present"
    | "HalfDay"
    | "PaidLeave"
    | "UnpaidLeave"
    | "Absence"
    | "Holiday"
    | "Weekend"
    | "MaternityLeave"
    | "SickLeave"
    | "ManualAdjusted";
  approvalStatus: "Draft" | "PendingHRReview" | "Approved" | "Rejected" | "Locked";
  isManualAdjusted: boolean;
  adjustmentReason: string | null;
  isPayrollLocked: boolean;
  generatedAt: string;
}

export interface AdjustAttendanceDailySummaryPayload {
  workingMinutes?: number | null;
  lateMinutes?: number | null;
  earlyLeaveMinutes?: number | null;
  overtimeMinutes?: number | null;
  workdayValue?: number | null;
  attendanceStatus?: AttendanceDailySummary["attendanceStatus"] | null;
  reason: string;
}

export interface AttendanceDailyImportResult {
  totalRows: number;
  updatedRows: number;
  createdRows: number;
  errorRows: number;
  errors: Array<{
    rowNumber: number;
    employeeCode: string;
    workDate: string;
    message: string;
  }>;
}

export interface AttendanceAdjustmentLog {
  id: number;
  attendanceDailySummaryId: number;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  departmentName: string | null;
  workDate: string;
  adjustedByAccountId: number;
  adjustedByName: string;
  adjustedAt: string;
  reason: string;
  oldValueJson: string | null;
  newValueJson: string | null;
}

export interface AttendancePeriodApproval {
  month: number;
  year: number;
  period: string;
  summaries: AttendanceSummary[];
}

const ENDPOINT = "/attendance-summaries";

export const attendanceSummaryApi = {
  getMonthly: async (
    month: number,
    year: number,
  ): Promise<{ success: boolean; data: AttendanceSummary[] }> => {
    return await axiosClient.get(ENDPOINT, { params: { month, year } });
  },

  getPendingApproval: async (): Promise<{ success: boolean; data: AttendancePeriodApproval[] }> => {
    return await axiosClient.get(`${ENDPOINT}/pending-approval`);
  },

  generateMonthly: async (
    month: number,
    year: number,
  ): Promise<{ success: boolean; data: AttendanceSummary[]; message: string }> => {
    return await axiosClient.post(`${ENDPOINT}/generate`, { month, year });
  },

  submitMonthly: async (
    month: number,
    year: number,
    note?: string,
  ): Promise<{ success: boolean; data: AttendanceSummary[]; message: string }> => {
    return await axiosClient.patch(`${ENDPOINT}/submit`, { month, year, note });
  },

  approveMonthly: async (
    month: number,
    year: number,
    note?: string,
  ): Promise<{ success: boolean; data: AttendanceSummary[]; message: string }> => {
    return await axiosClient.patch(`${ENDPOINT}/approve`, { month, year, note });
  },

  lockMonthly: async (
    month: number,
    year: number,
    note?: string,
  ): Promise<{ success: boolean; data: AttendanceSummary[]; message: string }> => {
    return await axiosClient.patch(`${ENDPOINT}/lock`, { month, year, note });
  },

  getDaily: async (
    month: number,
    year: number,
  ): Promise<{ success: boolean; data: AttendanceDailySummary[] }> => {
    return await axiosClient.get(`${ENDPOINT}/daily`, { params: { month, year } });
  },

  getAdjustmentLogs: async (
    month: number,
    year: number,
  ): Promise<{ success: boolean; data: AttendanceAdjustmentLog[] }> => {
    return await axiosClient.get(`${ENDPOINT}/daily/adjustment-logs`, { params: { month, year } });
  },

  adjustDaily: async (
    id: number,
    payload: AdjustAttendanceDailySummaryPayload,
  ): Promise<{ success: boolean; data: AttendanceDailySummary; message: string }> => {
    return await axiosClient.patch(`${ENDPOINT}/daily/${id}/adjust`, payload);
  },

  importDaily: async (
    formData: FormData,
  ): Promise<{ success: boolean; data: AttendanceDailyImportResult; message: string }> => {
    return await axiosClient.post(`${ENDPOINT}/daily/import`, formData);
  },

  approveDaily: async (
    id: number,
  ): Promise<{ success: boolean; data: AttendanceDailySummary; message: string }> => {
    return await axiosClient.patch(`${ENDPOINT}/daily/${id}/approve`);
  },
};
