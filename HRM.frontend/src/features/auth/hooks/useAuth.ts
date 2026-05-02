import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { authApi } from "../api/authApi";

export const useAuth = () => {
  const navigate = useNavigate();
  const [step, setStep] = useState<"LOGIN" | "MFA">("LOGIN");
  const [tempToken, setTempToken] = useState("");
  const [error, setError] = useState("");

  const finalizeLogin = (token: string, refreshToken: string) => {
    localStorage.setItem("accessToken", token);
    localStorage.setItem("refreshToken", refreshToken);
    navigate("/dashboard", { replace: true });
  };

  const handleGoogleLogin = async (code: string) => {
    try {
      setError("");
      const res = await authApi.googleLogin(code);
      if (res.status === "SUCCESS" && res.token && res.refreshToken) {
        finalizeLogin(res.token, res.refreshToken);
      } else if (res.status === "MFA_REQUIRED" && res.token) {
        setTempToken(res.token);
        setStep("MFA");
      }
    } catch (err: unknown) {
      setError(
        (err as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Lỗi xác thực từ máy chủ",
      );
    }
  };

  const handleVerifyMfa = async (otpCode: string) => {
    try {
      setError("");
      const res = await authApi.verifyMfa(otpCode, tempToken);
      if (res.status === "SUCCESS" && res.token && res.refreshToken) {
        finalizeLogin(res.token, res.refreshToken);
      }
    } catch (err: unknown) {
      setError(
        (err as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Mã OTP không hợp lệ",
      );
    }
  };

  return { step, error, handleGoogleLogin, handleVerifyMfa, setStep };
};
