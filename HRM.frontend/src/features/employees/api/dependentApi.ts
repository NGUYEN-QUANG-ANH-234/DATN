import axiosClient from "../../../core/api/axiosClient";
import type {
  DependentDto,
  PendingDependentRequest,
} from "../types/dependent";
import type { ReviewProfileUpdateDto } from "../types/profileRequest";

export const dependentApi = {
  getMyDependents: async (): Promise<{
    success: boolean;
    data: DependentDto[];
  }> => {
    return await axiosClient.get("/employees/me/dependents");
  },

  requestCreate: async (
    formData: FormData,
  ): Promise<{ success: boolean; message: string; data: number }> => {
    return await axiosClient.post("/employees/dependents/requests", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
  },

  requestUpdate: async (
    dependentId: number,
    formData: FormData,
  ): Promise<{ success: boolean; message: string; data: number }> => {
    return await axiosClient.put(
      `/employees/dependents/${dependentId}/requests`,
      formData,
      {
        headers: { "Content-Type": "multipart/form-data" },
      },
    );
  },

  requestDeactivate: async (
    dependentId: number,
  ): Promise<{ success: boolean; message: string; data: number }> => {
    return await axiosClient.patch(
      `/employees/dependents/${dependentId}/deactivate-request`,
    );
  },

  getPendingRequests: async (): Promise<{
    success: boolean;
    data: PendingDependentRequest[];
  }> => {
    return await axiosClient.get("/employees/dependent-requests/pending");
  },

  reviewRequest: async (
    id: number,
    payload: ReviewProfileUpdateDto,
  ): Promise<{ success: boolean; message: string }> => {
    return await axiosClient.patch(
      `/employees/dependent-requests/${id}/review`,
      payload,
    );
  },
};
