import axiosClient from "../../api/axiosClient";
import type { AuthResponse } from "../types";

export const authApi = {
  googleLogin: (code: string): Promise<AuthResponse> => {
    return axiosClient.post("/v1/auth/google", { code });
  },

  verifyRecoveryCode: async (recoveryCode: string, tempToken: string) => {
    return await axiosClient.post("/v1/auth/verify-recovery-code", {
      recoveryCode,
      tempToken,
    });
  },

  verifyMfa: async (otpCode: string, tempToken: string) => {
    return await axiosClient.post("/v1/auth/verify-mfa", {
      otpCode,
      tempToken,
    });
  },

  initiateMfaSetup: async () => {
    return await axiosClient.post("/v1/auth/mfa/setup");
  },

  confirmMfaSetup: async (otpCode: string) => {
    return await axiosClient.post("/v1/auth/mfa/confirm", { otpCode });
  },

  logout: async () => {
    return await axiosClient.post("/v1/auth/logout");
  },
};
