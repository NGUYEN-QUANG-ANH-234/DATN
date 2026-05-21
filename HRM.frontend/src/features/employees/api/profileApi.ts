import axiosClient from "../../../core/api/axiosClient";

const PROFILE_ENDPOINT = "/employees/profile";

export const profileApi = {
  requestUpdate: async (
    formData: FormData,
  ): Promise<{ success: boolean; message: string }> => {
    return await axiosClient.patch(PROFILE_ENDPOINT, formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
    });
  },
};
