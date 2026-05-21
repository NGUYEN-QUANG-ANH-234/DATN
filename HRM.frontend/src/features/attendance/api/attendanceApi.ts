import axiosClient from "../../../core/api/axiosClient";

export interface AttendanceGpsPayload {
  latitude: number;
  longitude: number;
}

export interface AttendanceLogResult {
  id: number;
  action: "CHECK_IN" | "CHECK_OUT";
  checkIn: string | null;
  checkOut: string | null;
  ipAddress: string;
  status: string;
  message: string;
}

export interface AttendanceNetworkInfo {
  clientIp: string;
  suggestedCidr: string;
  source: string;
}

export interface AttendanceTodayStatus {
  employeeName: string;
  shiftName: string | null;
  startTime: string | null;
  endTime: string | null;
  breakStartTime: string | null;
  breakEndTime: string | null;
  checkIn: string | null;
  checkOut: string | null;
  nextAction: "CHECK_IN" | "CHECK_OUT" | "DONE";
  message: string;
}

export const attendanceApi = {
  getTodayStatus: async (): Promise<{ success: boolean; data: AttendanceTodayStatus }> => {
    return await axiosClient.get("/attendance/me/today");
  },

  log: async (
    payload: AttendanceGpsPayload,
  ): Promise<{ success: boolean; data: AttendanceLogResult; message: string }> => {
    return await axiosClient.post("/attendance/log", payload);
  },

  getMyNetwork: async (): Promise<{ success: boolean; data: AttendanceNetworkInfo }> => {
    return await axiosClient.get("/attendance/my-network");
  },
};
