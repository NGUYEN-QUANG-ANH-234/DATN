import axiosClient from "../../../core/api/axiosClient";
import type {
  ActiveJob,
  CloseRecruitmentPayload,
  CreateRecruitmentPayload,
  RecruitmentRequestListItem,
} from "../types/recruitment";

const ENDPOINT = "/recruitment";

export const recruitmentApi = {
  // Tạo yêu cầu mới
  createRequest: async (payload: CreateRecruitmentPayload) => {
    return await axiosClient.post(`${ENDPOINT}/requests`, payload);
  },

  // Phê duyệt đa cấp
  reviewRequest: async (payload: {
    moduleCode: string;
    referenceId: number;
    isApproved: boolean;
    action?: "approve" | "reject" | "revision";
    note?: string;
  }) => {
    return await axiosClient.post("/approvals/process", payload);
  },

  // Lấy danh sách Job đã duyệt (Public)
  getActiveJobs: async (): Promise<{ success: boolean; data: ActiveJob[] }> => {
    return await axiosClient.get(`${ENDPOINT}/active-jobs`);
  },

  getRequests: async (): Promise<{ success: boolean; data: RecruitmentRequestListItem[] }> => {
    return await axiosClient.get(`${ENDPOINT}/requests`);
  },

  closeRequest: async (
    id: number,
    payload: CloseRecruitmentPayload = {},
  ): Promise<{ success: boolean; data: RecruitmentRequestListItem; message?: string }> => {
    return await axiosClient.patch(`${ENDPOINT}/requests/${id}/close`, payload);
  },

  getDepartmentsTree: async () => {
    return await axiosClient.get("/departments/tree");
  },

  // ĐỔI TỪ /system/roles SANG /positions
  getPositions: async () => {
    return await axiosClient.get("/positions");
  },

  getPendingApprovals: async () => {
    return await axiosClient.get("/approvals/pending");
  },

  getPendingRequests: async () => {
    return await axiosClient.get("/recruitment/requests/pending");
  },

  getMyRequests: async () => {
    return await axiosClient.get("/recruitment/requests/my-requests");
  },
};
