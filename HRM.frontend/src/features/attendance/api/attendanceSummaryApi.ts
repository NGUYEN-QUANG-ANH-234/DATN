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
  isPayrollLocked: boolean;
  generatedAt: string;
}

const ENDPOINT = "/attendance-summaries";

export const attendanceSummaryApi = {
  getMonthly: async (
    month: number,
    year: number,
  ): Promise<{ success: boolean; data: AttendanceSummary[] }> => {
    return await axiosClient.get(ENDPOINT, { params: { month, year } });
  },

  generateMonthly: async (
    month: number,
    year: number,
  ): Promise<{ success: boolean; data: AttendanceSummary[]; message: string }> => {
    return await axiosClient.post(`${ENDPOINT}/generate`, { month, year });
  },
};
