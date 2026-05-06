import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { authApi } from "../api/authApi";

export const useAuth = () => {
  const navigate = useNavigate();
  const [step, setStep] = useState<"LOGIN" | "MFA">("LOGIN");
  const [tempToken, setTempToken] = useState("");
  const [error, setError] = useState("");

  // --- BỔ SUNG TẠI ĐÂY: Hàm đăng xuất ---
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

      // Lúc này res chính là object { Status: "SUCCESS", Token: "..." }
      const res = (await authApi.verifyMfa(otpCode, tempToken)) as unknown as {
        status?: string;
        Status?: string;
        token?: string;
        Token?: string;
        accessToken?: string;
        refreshToken?: string;
        RefreshToken?: string;
        requireMfaSetup?: boolean; // Trường mới để biết có cần setup MFA không
        RequireMfaSetup?: boolean;
      };

      console.log("Dữ liệu MFA:", res);

      // Bắt mọi trường hợp tên biến C# trả về (camelCase hoặc PascalCase)
      const status = res.status || res.Status;
      const token = res.token || res.Token || res.accessToken;
      const refreshToken = res.refreshToken || res.RefreshToken;
      const requireMfaSetup = res.requireMfaSetup || res.RequireMfaSetup;

      if (status === "SUCCESS" && token && refreshToken) {
        // 1. Hiển thị Popup cảnh báo
        if (requireMfaSetup) {
          alert(
            "CẢNH BÁO: Tính năng Bảo mật 2 lớp (MFA) đã bị vô hiệu hóa!\n\nVì sự an toàn, vui lòng thiết lập lại MFA ngay sau khi vào bảng điều khiển.",
          );
          // Hoặc bạn có thể dispatch một state để show Modal UI đẹp hơn thay vì dùng alert mặc định
        }

        finalizeLogin(token, refreshToken);
      } else {
        console.error("Lỗi parse data:", res);
        setError("Không nhận được dữ liệu xác thực từ máy chủ.");
      }
    } catch (err: unknown) {
      setError(
        (err as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Mã OTP không hợp lệ",
      );
    }
  };

  // BỔ SUNG 1: Xác thực bằng mã khôi phục
  const handleVerifyRecoveryCode = async (recoveryCode: string) => {
    try {
      setError("");

      // Lúc này res chính là data đã được bóc từ Interceptor
      const res = (await authApi.verifyRecoveryCode(
        recoveryCode,
        tempToken,
      )) as unknown as {
        status?: string;
        Status?: string;
        token?: string;
        Token?: string;
        accessToken?: string;
        refreshToken?: string;
        RefreshToken?: string;
        requireMfaSetup?: boolean; // Trường mới để biết có cần setup MFA không
        RequireMfaSetup?: boolean;
      };

      // Bắt mọi trường hợp tên biến (camelCase hoặc PascalCase)
      const status = res.status || res.Status;
      const token = res.token || res.Token || res.accessToken;
      const refreshToken = res.refreshToken || res.RefreshToken;
      const requireMfaSetup = res.requireMfaSetup || res.RequireMfaSetup;

      if (status === "SUCCESS" && token && refreshToken) {
        // 1. Hiển thị Popup cảnh báo
        if (requireMfaSetup) {
          alert(
            "CẢNH BÁO: Tính năng Bảo mật 2 lớp (MFA) đã bị vô hiệu hóa!\n\nVì sự an toàn, vui lòng thiết lập lại MFA ngay sau khi vào bảng điều khiển.",
          );
          // Hoặc bạn có thể dispatch một state để show Modal UI đẹp hơn thay vì dùng alert mặc định
        }

        finalizeLogin(token, refreshToken);
      } else {
        setError("Không nhận được dữ liệu xác thực từ máy chủ.");
      }
    } catch (err: unknown) {
      // Xử lý lỗi gọn gàng hơn
      setError(
        (
          err as {
            response?: { data?: { message?: string } };
            message?: string;
          }
        )?.response?.data?.message ||
          (err as { message?: string })?.message ||
          "Mã khôi phục không chính xác hoặc đã được sử dụng.",
      );
    }
  };

  // BỔ SUNG 2: Gọi mã QR để cài đặt MFA
  const setupMfa = async () => {
    try {
      return await authApi.initiateMfaSetup();
    } catch (err: unknown) {
      throw new Error(
        (err as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Không thể khởi tạo MFA.",
      );
    }
  };

  // BỔ SUNG 3: Xác nhận OTP để hoàn tất cài MFA
  const confirmMfa = async (otpCode: string) => {
    try {
      return await authApi.confirmMfaSetup(otpCode);
    } catch (err: unknown) {
      throw new Error(
        (err as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Xác nhận MFA thất bại.",
      );
    }
  };

  return {
    step,
    error,
    handleGoogleLogin,
    handleVerifyMfa,
    setStep,
    logout,
    handleVerifyRecoveryCode,
    setupMfa,
    confirmMfa,
  };
};
