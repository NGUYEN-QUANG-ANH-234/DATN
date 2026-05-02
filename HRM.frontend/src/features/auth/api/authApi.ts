import axiosClient from "../../../core/api/axiosClient";
import type { AuthResponse } from "../types";

export const authApi = {
  googleLogin: (code: string): Promise<AuthResponse> => {
    return axiosClient.post("/auth/google", { code });
  },
  verifyMfa: (otpCode: string, tempToken: string): Promise<AuthResponse> => {
    return axiosClient.post("/auth/verify-mfa", { otpCode, tempToken });
  },
};
