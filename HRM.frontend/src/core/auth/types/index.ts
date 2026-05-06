export interface AuthResponse {
  status: "SUCCESS" | "MFA_REQUIRED" | "FAILED";
  token?: string;
  refreshToken?: string;
  expiration?: string;
}

export interface MfaSetupResponse {
  qrCodeUri: string;
  secretKey: string;
}

export interface MfaConfirmRequest {
  otpCode: string;
}

export interface MfaConfirmResponse {
  message: string;
  recoveryCodes: string[];
}

// Định nghĩa kiểu dữ liệu của Token trả về từ C#
export interface JwtPayload {
  IsMfaEnabled(IsMfaEnabled: unknown): unknown;
  email?: string;
  RoleId?: string;
  sub?: string;
  exp?: number;
}

// Định nghĩa kiểu dữ liệu User để hiển thị trên UI
export interface UserState {
  name: string;
  role: string;
  avatar: string;
}
