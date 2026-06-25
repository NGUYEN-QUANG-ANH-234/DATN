import axiosClient from "../../../core/api/axiosClient";

export interface LeaveTypeOption {
  id: number;
  typeName: string;
  category: string;
  isPaid: boolean;
  countsAsUnpaidForInsurance: boolean;
  countsAsWorkday: boolean;
  deductAnnualLeave: boolean;
  affectsKpiPenalty: boolean;
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
  leaveCategory: string;
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

  getPendingHR: async (): Promise<{ success: boolean; data: LeaveRequest[] }> => {
    return await axiosClient.get(`${ENDPOINT}/pending-hr`);
  },

  reviewByDept: async (id: number, isApproved: boolean) => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/dept-approve`, { isApproved });
  },

  finalApprove: async (id: number, isApproved: boolean) => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/final-approve`, { isApproved });
  },

  hrConfirm: async (id: number, isApproved: boolean) => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/hr-confirm`, { isApproved });
  },
};
