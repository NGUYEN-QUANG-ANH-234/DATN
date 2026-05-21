import axiosClient from "../../../core/api/axiosClient";
import type {
  PendingOnboardingRequest,
  ReviewOnboardingDto,
} from "../types/onboarding";

const ENDPOINT = "/onboarding-requests";

export const onboardingApi = {
  // Dành cho Nhân viên mới / Ứng viên nộp hồ sơ
  submitProfile: async (
    formData: FormData,
  ): Promise<{ success: boolean; message: string }> => {
    return await axiosClient.post(ENDPOINT, formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
  },

  // Dành cho HR lấy danh sách chờ duyệt (Giả định Backend có endpoint GET /onboarding-requests/pending)
  getPendingRequests: async (): Promise<{
    success: boolean;
    data: PendingOnboardingRequest[];
  }> => {
    return await axiosClient.get(`${ENDPOINT}/pending`);
  },

  // Dành cho HR duyệt hồ sơ và cấp Role
  reviewRequest: async (
    id: number,
    payload: ReviewOnboardingDto,
  ): Promise<{ success: boolean; message: string }> => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/hr-review`, payload);
  },
};
