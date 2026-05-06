import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useGoogleLogin } from "@react-oauth/google";
import { useAuth } from "../hooks/useAuth";
import logo from "../../../assets/images/hicas-logo.jpg";

export const LoginPage = () => {
  const navigate = useNavigate();

  // Bổ sung thêm handleVerifyRecoveryCode từ hook useAuth
  const {
    step,
    error,
    handleGoogleLogin,
    handleVerifyMfa,
    handleVerifyRecoveryCode,
    setStep,
  } = useAuth();

  // State cho đăng nhập cơ bản
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  // State cho xác thực MFA
  const [otpCode, setOtpCode] = useState("");
  const [recoveryCode, setRecoveryCode] = useState("");

  // STATE MỚI: Quản lý chế độ nhập MFA (Mặc định là OTP)
  const [mfaMode, setMfaMode] = useState<"OTP" | "RECOVERY">("OTP");

  // Kiểm tra nếu đã login thì đá thẳng vào Dashboard
  useEffect(() => {
    if (localStorage.getItem("accessToken")) {
      navigate("/dashboard", { replace: true });
    }
  }, [navigate]);

  const loginWithGoogle = useGoogleLogin({
    flow: "auth-code",
    onSuccess: (codeResponse) => handleGoogleLogin(codeResponse.code),
    onError: () => console.error("Google Login Failed"),
  });

  const onSubmitBasicLogin = (e: React.FormEvent) => {
    e.preventDefault();
    console.log("Submit đăng nhập thường với:", email, password);
  };

  // CẬP NHẬT: Xử lý submit tùy theo chế độ đang chọn
  const onSubmitMfa = (e: React.FormEvent) => {
    e.preventDefault();
    if (mfaMode === "OTP") {
      handleVerifyMfa(otpCode);
    } else {
      handleVerifyRecoveryCode(recoveryCode);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50 px-4 py-12 sm:px-6 lg:px-8">
      <div className="w-full max-w-md space-y-8 rounded-xl bg-white p-8 shadow-lg border border-gray-100">
        {/* --- VÙNG DÀNH CHO LOGO --- */}
        <div className="text-center">
          <img
            className="mx-auto h-32 w-auto object-contain"
            src={logo}
            alt="HICAS Logo"
          />
          <h2 className="mt-4 text-center text-2xl font-bold tracking-tight text-gray-900">
            {step === "LOGIN"
              ? "Đăng nhập hệ thống"
              : mfaMode === "OTP"
                ? "Xác thực 2 lớp (MFA)"
                : "Khôi phục tài khoản"}
          </h2>
        </div>

        {/* Hiển thị lỗi dùng chung */}
        {error && (
          <div className="rounded-md bg-red-50 p-4 border border-red-200">
            <p className="text-sm text-red-700 text-center font-medium">
              {error}
            </p>
          </div>
        )}

        {/* --- XỬ LÝ ĐIỀU HƯỚNG BƯỚC ĐĂNG NHẬP --- */}
        {step === "LOGIN" ? (
          <div className="mt-8 space-y-6">
            {/* 1. Form đăng nhập tài khoản / mật khẩu */}
            <form onSubmit={onSubmitBasicLogin} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">
                  Email hoặc Tài khoản
                </label>
                <input
                  type="text"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-gray-900 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 sm:text-sm"
                  placeholder="admin@hicas.vn"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">
                  Mật khẩu
                </label>
                <input
                  type="password"
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-gray-900 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 sm:text-sm"
                  placeholder="••••••••"
                />
              </div>

              <button
                type="submit"
                className="flex w-full justify-center rounded-md border border-transparent bg-blue-600 py-2.5 px-4 text-sm font-medium text-white shadow-sm hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
              >
                Đăng nhập
              </button>
            </form>

            <div className="relative">
              <div className="absolute inset-0 flex items-center">
                <div className="w-full border-t border-gray-300" />
              </div>
              <div className="relative flex justify-center text-sm">
                <span className="bg-white px-2 text-gray-500">
                  Hoặc tiếp tục với
                </span>
              </div>
            </div>

            {/* 2. Nút đăng nhập Google riêng biệt */}
            <button
              onClick={() => loginWithGoogle()}
              type="button"
              className="flex w-full items-center justify-center gap-3 rounded-md border border-gray-300 bg-white py-2.5 px-4 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
            >
              {/* Icon Google SVG */}
              <svg className="h-5 w-5" viewBox="0 0 24 24">
                <path
                  d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
                  fill="#4285F4"
                />
                <path
                  d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                  fill="#34A853"
                />
                <path
                  d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                  fill="#FBBC05"
                />
                <path
                  d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                  fill="#EA4335"
                />
              </svg>
              Google
            </button>
          </div>
        ) : (
          /* --- FORM MFA / RECOVERY CODE --- */
          <form onSubmit={onSubmitMfa} className="mt-8 space-y-6">
            {/* Rẽ nhánh giao diện dựa vào mfaMode */}
            {mfaMode === "OTP" ? (
              <div>
                <p className="text-center text-sm text-gray-600 mb-4">
                  Vui lòng mở ứng dụng Google Authenticator và nhập mã 6 số của
                  bạn.
                </p>
                <input
                  type="text"
                  required
                  maxLength={6}
                  value={otpCode}
                  onChange={(e) =>
                    setOtpCode(e.target.value.replace(/\D/g, ""))
                  }
                  className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-3 text-center text-2xl tracking-[0.5em] text-gray-900 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                  placeholder="000000"
                />
              </div>
            ) : (
              <div>
                <p className="text-center text-sm text-gray-600 mb-4">
                  Nhập một trong các mã khôi phục bạn đã lưu khi thiết lập MFA.
                </p>
                <input
                  type="text"
                  required
                  value={recoveryCode}
                  onChange={(e) => setRecoveryCode(e.target.value)}
                  className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-3 text-center text-xl font-mono text-gray-900 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                  placeholder="Nhập mã khôi phục"
                />
              </div>
            )}

            {/* Nút Toggle giữa 2 chế độ */}
            <div className="text-center">
              <button
                type="button"
                onClick={() => {
                  setMfaMode(mfaMode === "OTP" ? "RECOVERY" : "OTP");
                  setOtpCode("");
                  setRecoveryCode("");
                }}
                className="text-sm font-medium text-blue-600 hover:text-blue-500"
              >
                {mfaMode === "OTP"
                  ? "Không có quyền truy cập ứng dụng? Dùng mã khôi phục"
                  : "Quay lại nhập mã Authenticator"}
              </button>
            </div>

            <div className="flex flex-col gap-3">
              <button
                type="submit"
                className="flex w-full justify-center rounded-md border border-transparent bg-blue-600 py-2.5 px-4 text-sm font-medium text-white shadow-sm hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
              >
                Xác nhận
              </button>
              <button
                type="button"
                onClick={() => {
                  setStep("LOGIN");
                  setMfaMode("OTP"); // Reset mode về mặc định
                }}
                className="flex w-full justify-center rounded-md border border-gray-300 bg-white py-2.5 px-4 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50"
              >
                Quay lại đăng nhập
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
};
