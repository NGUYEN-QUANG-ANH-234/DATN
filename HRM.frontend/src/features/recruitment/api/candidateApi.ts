// Đảm bảo đường dẫn axiosClient khớp với dự án của bạn
import axiosClient from "../../../core/api/axiosClient";

import type {
  ApplyJobPayload,
  ApiResponse,
  ActiveJob,
  CandidateHistoryDto,
} from "../types/candidate";

export const candidateApi = {
  applyForJob: async (payload: ApplyJobPayload): Promise<ApiResponse> => {
    // Bắt buộc dùng FormData khi có đính kèm File
    const formData = new FormData();
    formData.append(
      "RecruitmentRequestId",
      payload.recruitmentRequestId.toString(),
    );
    formData.append("FullName", payload.fullName);
    formData.append("Email", payload.email);
    formData.append("CvFile", payload.cvFile);

    // Gửi POST request với header multipart/form-data
    return await axiosClient.post("/candidates/apply", formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
    });
  },

  getActiveJobs: async (): Promise<ApiResponse<ActiveJob[]>> => {
    // Gọi đến endpoint public đã viết trong RecruitmentController
    return await axiosClient.get("/recruitment/active-jobs");
  },

  getMyApplications: async (
    email: string,
    trackingCode: string,
  ): Promise<ApiResponse<CandidateHistoryDto[]>> => {
    return await axiosClient.get(
      `/candidates/my-applications?email=${encodeURIComponent(email)}&trackingCode=${encodeURIComponent(trackingCode)}`,
    );
  },

  getAllCandidates: async (): Promise<ApiResponse<CandidateHistoryDto[]>> => {
    return await axiosClient.get("/candidates");
  },

  hrApprove: async (id: number): Promise<ApiResponse> => {
    return await axiosClient.patch(`/candidates/${id}/hr-approve`);
  },

  deptConfirm: async (id: number): Promise<ApiResponse> => {
    return await axiosClient.patch(`/candidates/${id}/dept-confirm`);
  },

  finalApprove: async (id: number): Promise<ApiResponse> => {
    return await axiosClient.patch(`/candidates/${id}/final-approve`);
  },

  rejectCandidate: async (id: number): Promise<ApiResponse> => {
    return await axiosClient.patch(`/candidates/${id}/reject`);
  },
};
