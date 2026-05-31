import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { authApi } from "../api/authApi";

type AuthResponseLike = {
  status?: string;
  Status?: string;
  token?: string;
  Token?: string;
  accessToken?: string;
  refreshToken?: string;
  RefreshToken?: string;
  requireMfaSetup?: boolean;
  RequireMfaSetup?: boolean;
};

const getApiMessage = (error: unknown, fallback: string) =>
  (error as { response?: { data?: { message?: string; Message?: string } } }).response?.data
    ?.message ||
  (error as { response?: { data?: { message?: string; Message?: string } } }).response?.data
    ?.Message ||
  (error as { message?: string }).message ||
  fallback;

export const useAuth = () => {
  const navigate = useNavigate();
  const [step, setStep] = useState<"LOGIN" | "MFA">("LOGIN");
  const [tempToken, setTempToken] = useState("");
  const [error, setError] = useState("");

  const logout = async () => {
    try {
      await authApi.logout();
    } catch (err) {
      console.error("Lỗi đăng xuất API", err);
    } finally {
      localStorage.removeItem("accessToken");
      localStorage.removeItem("refreshToken");
      navigate("/", { replace: true });
    }
  };

  const finalizeLogin = (token: string, refreshToken?: string) => {
    localStorage.setItem("accessToken", token);
    if (refreshToken) {
      localStorage.setItem("refreshToken", refreshToken);
    }
    navigate("/dashboard", { replace: true });
  };

  const handleGoogleLogin = async (code: string) => {
    try {
      setError("");
      const res = await authApi.googleLogin(code);
      const status = res.status;
      const token = res.token;

      if (status === "SUCCESS" && token) {
        finalizeLogin(token);
      } else if (status === "MFA_REQUIRED" && token) {
        setTempToken(token);
        setStep("MFA");
      }
    } catch (err: unknown) {
      setError(getApiMessage(err, "Lỗi xác thực từ máy chủ."));
    }
  };

  const handleBasicLogin = async (email: string, password: string) => {
    try {
      setError("");
      const res = await authApi.basicLogin(email, password);
      const status = res.status;
      const token = res.token;

      if (status === "SUCCESS" && token) {
        finalizeLogin(token);
      } else if (status === "MFA_REQUIRED" && token) {
        setTempToken(token);
        setStep("MFA");
      }
    } catch (err: unknown) {
      setError(getApiMessage(err, "Tài khoản hoặc mật khẩu không chính xác."));
    }
  };

  const handleVerifyMfa = async (otpCode: string) => {
    try {
      setError("");

      const res = (await authApi.verifyMfa(otpCode, tempToken)) as unknown as AuthResponseLike;
      const status = res.status || res.Status;
      const token = res.token || res.Token || res.accessToken;
      const refreshToken = res.refreshToken || res.RefreshToken;
      const requireMfaSetup = res.requireMfaSetup || res.RequireMfaSetup;

      if (status === "SUCCESS" && token) {
        if (requireMfaSetup) {
          alert(
            "Cảnh báo: Tính năng bảo mật hai lớp (MFA) đang bị vô hiệu hóa. Vui lòng thiết lập lại MFA sau khi đăng nhập để bảo vệ tài khoản.",
          );
        }

        finalizeLogin(token, refreshToken);
      } else {
        console.error("Lỗi parse dữ liệu MFA:", res);
        setError("Không nhận được dữ liệu xác thực từ máy chủ.");
      }
    } catch (err: unknown) {
      setError(getApiMessage(err, "Mã OTP không hợp lệ."));
    }
  };

  const handleVerifyRecoveryCode = async (recoveryCode: string) => {
    try {
      setError("");

      const res = (await authApi.verifyRecoveryCode(
        recoveryCode,
        tempToken,
      )) as unknown as AuthResponseLike;
      const status = res.status || res.Status;
      const token = res.token || res.Token || res.accessToken;
      const refreshToken = res.refreshToken || res.RefreshToken;
      const requireMfaSetup = res.requireMfaSetup || res.RequireMfaSetup;

      if (status === "SUCCESS" && token) {
        if (requireMfaSetup) {
          alert(
            "Cảnh báo: Tính năng bảo mật hai lớp (MFA) đang bị vô hiệu hóa. Vui lòng thiết lập lại MFA sau khi đăng nhập để bảo vệ tài khoản.",
          );
        }

        finalizeLogin(token, refreshToken);
      } else {
        setError("Không nhận được dữ liệu xác thực từ máy chủ.");
      }
    } catch (err: unknown) {
      setError(
        getApiMessage(err, "Mã khôi phục không chính xác hoặc đã được sử dụng."),
      );
    }
  };

  const setupMfa = async () => {
    try {
      return await authApi.initiateMfaSetup();
    } catch (err: unknown) {
      throw new Error(getApiMessage(err, "Không thể khởi tạo MFA."));
    }
  };

  const confirmMfa = async (otpCode: string) => {
    try {
      return await authApi.confirmMfaSetup(otpCode);
    } catch (err: unknown) {
      throw new Error(getApiMessage(err, "Xác nhận MFA thất bại."));
    }
  };

  return {
    step,
    error,
    handleGoogleLogin,
    handleBasicLogin,
    handleVerifyMfa,
    setStep,
    logout,
    handleVerifyRecoveryCode,
    setupMfa,
    confirmMfa,
  };
};
