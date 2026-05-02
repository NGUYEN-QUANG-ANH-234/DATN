export interface AuthResponse {
  status: "SUCCESS" | "MFA_REQUIRED" | "FAILED";
  token?: string;
  refreshToken?: string;
  expiration?: string;
}
