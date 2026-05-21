import axiosClient from "../../../core/api/axiosClient";
import type {
  ReviewProfileUpdateDto,
  PendingProfileRequest,
} from "../types/profileRequest";

export const hrProfileApi = {
  // 1. API Lấy danh sách chờ duyệt
  getPendingRequests: async (): Promise<{
    success: boolean;
    data: PendingProfileRequest[];
  }> => {
    return await axiosClient.get("/employees/profile-requests/pending");
  },

  // 2. API HR Quyết định Duyệt/Từ chối (Endpoint ta vừa làm xong)
  reviewRequest: async (
    id: number,
    payload: ReviewProfileUpdateDto,
  ): Promise<{
    data: unknown;
    success: boolean;
    message: string;
  }> => {
    return await axiosClient.patch(
      `/employees/profile-requests/${id}/review`,
      payload,
    );
  },
};
