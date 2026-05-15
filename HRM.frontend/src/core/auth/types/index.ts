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
  // Thay vì email, .NET dùng chuẩn URL cho các claim mặc định.
  // Tuy nhiên, đối với Name và Role ta đã custom lại tên ngắn gọn ở Backend
  email?: string;
  name?: string; // <-- Thêm name (Do ta dùng ClaimTypes.Name ở Backend)
  role?: string; // <-- Thêm role (Do ta dùng claim "role" ở Backend)
  avatar?: string; // <-- Thêm avatar
  IsMfaEnabled?: string | boolean;
  sub?: string;
  exp?: number;
  [key: string]: unknown;
}

// Định nghĩa kiểu dữ liệu User để hiển thị trên UI
export interface UserState {
  name: string;
  role: string;
  avatar: string;
}
