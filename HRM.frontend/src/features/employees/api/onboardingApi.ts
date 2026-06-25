import axiosClient from "../../../core/api/axiosClient";
import type {
  OnboardingCandidateLookup,
  PendingOnboardingRequest,
  ReviewOnboardingDto,
} from "../types/onboarding";

const ENDPOINT = "/onboarding-requests";

export const onboardingApi = {
  resolveCandidate: async (payload: {
    email: string;
    trackingCode: string;
  }): Promise<{ success: boolean; data: OnboardingCandidateLookup }> => {
    return await axiosClient.post(`${ENDPOINT}/resolve`, payload);
  },

  submitProfile: async (
    formData: FormData,
  ): Promise<{ success: boolean; message: string }> => {
    return await axiosClient.post(ENDPOINT, formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
  },

  getPendingRequests: async (): Promise<{
    success: boolean;
    data: PendingOnboardingRequest[];
  }> => {
    return await axiosClient.get(`${ENDPOINT}/pending`);
  },

  reviewRequest: async (
    id: number,
    payload: ReviewOnboardingDto,
  ): Promise<{ success: boolean; message: string }> => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/hr-review`, payload);
  },
};
