import axiosClient from "../../../core/api/axiosClient";

export interface LeaveTypeOption {
  id: number;
  typeName: string;
}

export interface LeaveRequest {
  id: number;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  departmentName: string | null;
  leaveTypeId: number;
  leaveTypeName: string;
  isPaidLeave: boolean;
  startDate: string;
  endDate: string;
  requestedDays: number;
  reason: string;
  status: string;
  deadlineAt: string | null;
}

export interface CreateLeaveRequestPayload {
  leaveTypeId: number;
  startDate: string;
  endDate: string;
  reason: string;
}

const ENDPOINT = "/leave-requests";

export const leaveRequestApi = {
  getLeaveTypes: async (): Promise<{ success: boolean; data: LeaveTypeOption[] }> => {
    return await axiosClient.get("/system/leave-types");
  },

  create: async (
    payload: CreateLeaveRequestPayload,
  ): Promise<{ success: boolean; data: number; message: string }> => {
    return await axiosClient.post(ENDPOINT, payload);
  },

  getMy: async (): Promise<{ success: boolean; data: LeaveRequest[] }> => {
    return await axiosClient.get(`${ENDPOINT}/my`);
  },

  getPendingDept: async (): Promise<{ success: boolean; data: LeaveRequest[] }> => {
    return await axiosClient.get(`${ENDPOINT}/pending-dept`);
  },

  getPendingDirector: async (): Promise<{ success: boolean; data: LeaveRequest[] }> => {
    return await axiosClient.get(`${ENDPOINT}/pending-director`);
  },

  reviewByDept: async (id: number, isApproved: boolean) => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/dept-approve`, { isApproved });
  },

  finalApprove: async (id: number, isApproved: boolean) => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/final-approve`, { isApproved });
  },
};
