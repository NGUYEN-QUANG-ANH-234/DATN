import axiosClient from "../../api/axiosClient";
import type { AuthResponse, ChangePasswordRequest, ChangePasswordResponse } from "../types";

export const authApi = {
  googleLogin: (code: string): Promise<AuthResponse> => {
    return axiosClient.post("/auth/google", { code });
  },

  verifyRecoveryCode: async (recoveryCode: string, tempToken: string) => {
    return await axiosClient.post("/auth/verify-recovery-code", {
      recoveryCode,
      tempToken,
    });
  },

  verifyMfa: async (otpCode: string, tempToken: string) => {
    return await axiosClient.post("/auth/verify-mfa", {
      otpCode,
      tempToken,
    });
  },

  initiateMfaSetup: async () => {
    return await axiosClient.post("/auth/mfa/setup");
  },

  confirmMfaSetup: async (otpCode: string) => {
    return await axiosClient.post("/auth/mfa/confirm", { otpCode });
  },

  logout: async () => {
    return await axiosClient.post("/auth/logout");
  },

  changePassword: async (payload: ChangePasswordRequest): Promise<ChangePasswordResponse> => {
    return await axiosClient.post("/auth/change-password", payload);
  },

  basicLogin: (email: string, password: string): Promise<AuthResponse> => {
    return axiosClient.post("/auth/login", { email, password });
  },
};
