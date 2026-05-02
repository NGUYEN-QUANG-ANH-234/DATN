import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useGoogleLogin } from "@react-oauth/google";
import { useAuth } from "../hooks/useAuth";

export const LoginPage = () => {
  const navigate = useNavigate();
  const { step, error, handleGoogleLogin, handleVerifyMfa, setStep } =
    useAuth();
  const [otpCode, setOtpCode] = useState("");

  // Kiểm tra nếu đã login thì đá thẳng vào Dashboard
  useEffect(() => {
    if (localStorage.getItem("accessToken")) {
      navigate("/dashboard", { replace: true });
    }
  }, [navigate]);

  const login = useGoogleLogin({
    flow: "auth-code",
    onSuccess: (codeResponse) => handleGoogleLogin(codeResponse.code),
    onError: () => console.error("Google Login Failed"),
  });

  const onSubmitMfa = (e: React.FormEvent) => {
    e.preventDefault();
    handleVerifyMfa(otpCode);
  };

  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        height: "100vh",
      }}
    >
      <h1>HICAS HRM</h1>
      {error && <p style={{ color: "red" }}>{error}</p>}

      {step === "LOGIN" ? (
        <button onClick={() => login()}>Đăng nhập bằng Google</button>
      ) : (
        <form onSubmit={onSubmitMfa}>
          <h3>Xác thực 2 lớp (MFA)</h3>
          <p>Nhập mã 6 số từ ứng dụng Authenticator</p>
          <input
            type="text"
            value={otpCode}
            onChange={(e) => setOtpCode(e.target.value)}
            maxLength={6}
            required
          />
          <button type="submit">Xác nhận</button>
          <button type="button" onClick={() => setStep("LOGIN")}>
            Hủy
          </button>
        </form>
      )}
    </div>
  );
};
