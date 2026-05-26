import axiosClient from "../../../core/api/axiosClient";

export interface CreateOvertimeRequestPayload {
  employeeId?: number | null;
  workDate: string;
  startTime: string;
  endTime: string;
  reason: string;
  projectCode?: string | null;
}

export interface CreateBulkOvertimeRequestPayload {
  employeeIds: number[];
  workDate: string;
  startTime: string;
  endTime: string;
  reason: string;
  projectCode?: string | null;
}

export interface ReviewOvertimeRequestPayload {
  isApproved: boolean;
  note?: string | null;
}

export interface OvertimeRequest {
  id: number;
  employeeId: number;
  employeeName: string;
  departmentName: string | null;
  requestedByAccountId: number;
  workDate: string;
  startTime: string;
  endTime: string;
  reason: string;
  projectCode: string | null;
  status: "PendingManager" | "PendingHR" | "PendingDirector" | "Approved" | "Rejected" | "Cancelled";
  managerNote: string | null;
  hrNote: string | null;
  approvedMinutes: number;
  actualOtMinutes: number;
  isPayrollLocked: boolean;
  payrollPeriod: string | null;
  createdAt: string;
  reconciledAt: string | null;
}

export interface OvertimeEmployeeOption {
  id: number;
  employeeCode: string;
  fullName: string;
  departmentName: string | null;
}

const ENDPOINT = "/overtime-requests";

export const overtimeApi = {
  create: async (
    payload: CreateOvertimeRequestPayload,
  ): Promise<{ success: boolean; data: number }> => {
    return await axiosClient.post(ENDPOINT, payload);
  },

  createBulk: async (
    payload: CreateBulkOvertimeRequestPayload,
  ): Promise<{ success: boolean; data: number[] }> => {
    return await axiosClient.post(`${ENDPOINT}/bulk`, payload);
  },

  getMy: async (): Promise<{ success: boolean; data: OvertimeRequest[] }> => {
    return await axiosClient.get(`${ENDPOINT}/my`);
  },

  getAssignableEmployees: async (): Promise<{ success: boolean; data: OvertimeEmployeeOption[] }> => {
    return await axiosClient.get(`${ENDPOINT}/assignable-employees`);
  },

  getPendingManager: async (): Promise<{ success: boolean; data: OvertimeRequest[] }> => {
    return await axiosClient.get(`${ENDPOINT}/pending-manager`);
  },

  getPendingHr: async (): Promise<{ success: boolean; data: OvertimeRequest[] }> => {
    return await axiosClient.get(`${ENDPOINT}/pending-hr`);
  },

  getPendingDirector: async (): Promise<{ success: boolean; data: OvertimeRequest[] }> => {
    return await axiosClient.get(`${ENDPOINT}/pending-director`);
  },

  getApproved: async (
    params?: { month?: number; year?: number },
  ): Promise<{ success: boolean; data: OvertimeRequest[] }> => {
    return await axiosClient.get(`${ENDPOINT}/approved`, { params });
  },

  managerReview: async (
    id: number,
    payload: ReviewOvertimeRequestPayload,
  ): Promise<{ success: boolean; message: string }> => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/manager-review`, payload);
  },

  hrConfirm: async (
    id: number,
    payload: ReviewOvertimeRequestPayload,
  ): Promise<{ success: boolean; message: string }> => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/hr-confirm`, payload);
  },

  directorReview: async (
    id: number,
    payload: ReviewOvertimeRequestPayload,
  ): Promise<{ success: boolean; message: string }> => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/director-review`, payload);
  },

  reconcile: async (
    id: number,
  ): Promise<{ success: boolean; data: OvertimeRequest; message: string }> => {
    return await axiosClient.post(`${ENDPOINT}/${id}/reconcile`);
  },
};
