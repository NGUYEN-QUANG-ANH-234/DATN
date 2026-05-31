import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useGoogleLogin } from "@react-oauth/google";
import { Eye, Globe2, LockKeyhole, Mail } from "lucide-react";
import { Button } from "../../../components/ui";
import logo from "../../../assets/images/hicas-logo.jpg";
import heroImage from "../../../assets/images/login-platform-hero-focused.png";
import { useAuth } from "../hooks/useAuth";

const GoogleIcon = () => (
  <svg className="h-5 w-5" viewBox="0 0 24 24" aria-hidden="true">
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
);

export const LoginPage = () => {
  const navigate = useNavigate();
  const {
    step,
    error,
    handleGoogleLogin,
    handleBasicLogin,
    handleVerifyMfa,
    handleVerifyRecoveryCode,
    setStep,
  } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [otpCode, setOtpCode] = useState("");
  const [recoveryCode, setRecoveryCode] = useState("");
  const [mfaMode, setMfaMode] = useState<"OTP" | "RECOVERY">("OTP");
  const [showPassword, setShowPassword] = useState(false);

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

  const onSubmitBasicLogin = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!email || !password) return;
    await handleBasicLogin(email, password);
  };

  const onSubmitMfa = (event: React.FormEvent) => {
    event.preventDefault();
    if (mfaMode === "OTP") {
      handleVerifyMfa(otpCode);
      return;
    }

    handleVerifyRecoveryCode(recoveryCode);
  };

  const title =
    step === "LOGIN"
      ? "HICAS WORKSPACE"
      : mfaMode === "OTP"
        ? "Xác thực hai lớp"
        : "Khôi phục truy cập";

  // const subtitle =
  //   step === "LOGIN"
  //     ? "Đăng nhập vào không gian làm việc nội bộ của HICAS."
  //     : "Tài khoản của bạn đang được bảo vệ bằng MFA.";

  return (
    <div className="min-h-screen bg-[#F6F7F9]">
      <div className="grid min-h-screen lg:grid-cols-[52fr_48fr]">
        <aside className="relative hidden min-h-screen overflow-hidden bg-[#101112] lg:block">
          <img
            src={heroImage}
            alt=""
            className="absolute inset-x-0 bottom-0 h-full w-full object-cover object-[center_76%] brightness-[1.18] contrast-[1.06] saturate-[1.08]"
          />
          <div className="absolute inset-0 bg-[linear-gradient(90deg,rgba(16,17,18,0.18)_0%,rgba(16,17,18,0.08)_42%,rgba(16,17,18,0.01)_100%)]" />
          <div className="absolute inset-0 bg-[linear-gradient(180deg,rgba(16,17,18,0.18)_0%,rgba(16,17,18,0.035)_30%,rgba(16,17,18,0)_66%,rgba(16,17,18,0.04)_100%)]" />
          <div className="absolute left-0 top-0 h-[46%] w-[88%] bg-[radial-gradient(ellipse_at_18%_18%,rgba(0,0,0,0.9)_0%,rgba(0,0,0,0.68)_34%,rgba(0,0,0,0.34)_62%,transparent_100%)] backdrop-blur-[1px]" />

          <div className="relative z-10 flex h-full flex-col px-8 py-7 text-white xl:px-10">
            <div className="flex items-center gap-4">
              <div className="flex h-16 w-16 items-center justify-center overflow-hidden rounded-2xl bg-white shadow-[0_18px_48px_rgba(0,0,0,0.28)]">
                <img
                  src={logo}
                  alt="HICAS"
                  className="h-full w-full object-contain"
                />
              </div>
              <div>
                <p className="text-sm font-semibold uppercase tracking-[0.18em] text-[var(--hicas-orange)] [text-shadow:0_3px_18px_rgba(0,0,0,0.9)]">
                  Engineering Workspace
                </p>
                <p className="mt-1 text-sm text-white/72 [text-shadow:0_3px_18px_rgba(0,0,0,0.9)]">
                  BIM / ERP / AI / Data Platform
                </p>
              </div>
            </div>

            <div className="mt-12 max-w-2xl xl:mt-14">
              {/* <div className="mb-4 inline-flex items-center rounded-full border border-[rgba(255,122,0,0.34)] bg-[#111111]/42 px-4 py-2 text-sm font-semibold text-[var(--hicas-orange-hover)] backdrop-blur">
                Engineering Software Platform
              </div> */}

              <h1 className="text-[clamp(1.95rem,3.15vw,3.65rem)] font-extrabold leading-[1.07] tracking-[-0.045em] [text-shadow:0_5px_28px_rgba(0,0,0,0.92)]">
                Engineering Software.
                <span className="block text-[var(--hicas-orange)]">
                  Connected Operations.
                </span>
              </h1>

              {/* <p className="mt-4 max-w-xl text-sm leading-7 text-white/76 xl:text-base">
                Không gian nội bộ kết nối con người, vận hành và dữ liệu trong
                hệ sinh thái BIM-ERP-AI của HICAS.
              </p> */}
            </div>
          </div>
        </aside>

        <main className="flex min-h-screen items-center justify-center px-5 py-8 sm:px-8 lg:px-10">
          <div className="w-full max-w-[480px]">
            <div className="mb-8 flex items-center justify-between lg:justify-end">
              <div className="flex items-center gap-3 lg:hidden">
                <div className="flex h-12 w-12 items-center justify-center overflow-hidden rounded-2xl bg-white shadow-[var(--shadow-card)]">
                  <img
                    src={logo}
                    alt="HICAS"
                    className="h-full w-full object-contain"
                  />
                </div>
                <div>
                  <p className="text-xs font-bold uppercase tracking-[0.2em] text-[var(--hicas-orange)]">
                    HICAS
                  </p>
                  <p className="text-sm font-semibold">Workspace</p>
                </div>
              </div>
              <div className="flex items-center gap-5 text-sm text-[var(--hicas-text-secondary)]">
                <button className="inline-flex items-center gap-2 font-semibold text-[var(--hicas-text-main)]">
                  <Globe2 size={16} />
                  Tiếng Việt
                </button>
                <button>Trợ giúp</button>
              </div>
            </div>

            <section className="rounded-[20px] border border-[var(--hicas-border)] bg-white p-6 shadow-[0_18px_58px_rgba(17,24,39,0.08)] sm:p-8">
              <div className="mb-7">
                <p className="text-sm font-semibold uppercase tracking-[0.18em] text-[var(--hicas-orange)]">
                  Welcome back
                </p>
                <h2 className="mt-3 text-3xl font-bold tracking-tight text-[var(--hicas-text-main)]">
                  {title}
                </h2>
                <p className="mt-3 text-sm leading-6 text-[var(--hicas-text-secondary)]">
                  {/* {subtitle} */}
                </p>
              </div>

              {error && (
                <div className="mb-5 rounded-2xl border border-[var(--hicas-danger)] bg-[var(--hicas-danger-soft)] px-4 py-3 text-sm font-medium text-[var(--hicas-danger)]">
                  {error}
                </div>
              )}

              {step === "LOGIN" ? (
                <div className="space-y-6">
                  <form onSubmit={onSubmitBasicLogin} className="space-y-4">
                    <label className="block">
                      <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">
                        Email hoặc tài khoản
                      </span>
                      <span className="relative block">
                        <Mail
                          size={18}
                          className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-[var(--hicas-text-muted)]"
                        />
                        <input
                          type="text"
                          required
                          value={email}
                          onChange={(event) => setEmail(event.target.value)}
                          className="hicas-input hicas-input-icon-left h-12 w-full rounded-xl"
                          placeholder="admin@hicas.vn"
                        />
                      </span>
                    </label>

                    <label className="block">
                      <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">
                        Mật khẩu
                      </span>
                      <span className="relative block">
                        <LockKeyhole
                          size={18}
                          className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-[var(--hicas-text-muted)]"
                        />
                        <input
                          type={showPassword ? "text" : "password"}
                          required
                          value={password}
                          onChange={(event) => setPassword(event.target.value)}
                          className="hicas-input hicas-input-icon-left hicas-input-icon-right h-12 w-full rounded-xl"
                          placeholder="••••••••"
                        />
                        <button
                          type="button"
                          onClick={() => setShowPassword((value) => !value)}
                          className="absolute right-4 top-1/2 -translate-y-1/2 text-[var(--hicas-text-muted)] transition hover:text-[var(--hicas-orange)]"
                          aria-label={
                            showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"
                          }
                        >
                          <Eye size={18} />
                        </button>
                      </span>
                    </label>

                    <div className="flex items-center justify-between gap-4 text-sm">
                      <label className="flex items-center gap-2 text-[var(--hicas-text-secondary)]">
                        <input
                          type="checkbox"
                          className="h-4 w-4 rounded border-[var(--hicas-border)] accent-[var(--hicas-orange)]"
                        />
                        Ghi nhớ đăng nhập
                      </label>
                      <button
                        type="button"
                        className="font-semibold text-[var(--hicas-orange)] hover:text-[var(--hicas-orange-dark)]"
                      >
                        Quên mật khẩu?
                      </button>
                    </div>

                    <Button type="submit" fullWidth size="lg">
                      Đăng nhập
                    </Button>
                  </form>

                  <div className="relative">
                    <div className="absolute inset-0 flex items-center">
                      <div className="w-full border-t border-[var(--hicas-border)]" />
                    </div>
                    <div className="relative flex justify-center text-sm">
                      <span className="bg-white px-3 text-[var(--hicas-text-muted)]">
                        hoặc tiếp tục với
                      </span>
                    </div>
                  </div>

                  <Button
                    type="button"
                    variant="secondary"
                    fullWidth
                    size="lg"
                    onClick={() => loginWithGoogle()}
                    iconLeft={<GoogleIcon />}
                  >
                    Đăng nhập bằng Google
                  </Button>

                  {/* <div className="flex items-center justify-center gap-3 rounded-2xl border border-[var(--hicas-border)] bg-[var(--hicas-orange-lighter)] px-4 py-3 text-sm font-semibold text-[var(--hicas-orange-dark)]">
                    <ShieldCheck size={18} />
                    Secure access with MFA enabled
                  </div> */}
                </div>
              ) : (
                <form onSubmit={onSubmitMfa} className="space-y-5">
                  {mfaMode === "OTP" ? (
                    <label className="block">
                      <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">
                        Mã Google Authenticator
                      </span>
                      <input
                        type="text"
                        required
                        maxLength={6}
                        value={otpCode}
                        onChange={(event) =>
                          setOtpCode(event.target.value.replace(/\D/g, ""))
                        }
                        className="hicas-input h-14 w-full text-center text-2xl font-semibold tracking-[0.48em]"
                        placeholder="000000"
                      />
                    </label>
                  ) : (
                    <label className="block">
                      <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">
                        Mã khôi phục
                      </span>
                      <input
                        type="text"
                        required
                        value={recoveryCode}
                        onChange={(event) =>
                          setRecoveryCode(event.target.value)
                        }
                        className="hicas-input h-14 w-full text-center font-mono text-lg"
                        placeholder="Nhập mã khôi phục"
                      />
                    </label>
                  )}

                  <button
                    type="button"
                    onClick={() => {
                      setMfaMode(mfaMode === "OTP" ? "RECOVERY" : "OTP");
                      setOtpCode("");
                      setRecoveryCode("");
                    }}
                    className="text-sm font-semibold text-[var(--hicas-orange)] hover:text-[var(--hicas-orange-dark)]"
                  >
                    {mfaMode === "OTP"
                      ? "Không có ứng dụng? Dùng mã khôi phục"
                      : "Quay lại nhập mã Authenticator"}
                  </button>

                  <div className="grid gap-3">
                    <Button type="submit" fullWidth size="lg">
                      Xác nhận
                    </Button>
                    <Button
                      type="button"
                      variant="secondary"
                      fullWidth
                      size="lg"
                      onClick={() => {
                        setStep("LOGIN");
                        setMfaMode("OTP");
                      }}
                    >
                      Quay lại đăng nhập
                    </Button>
                  </div>
                </form>
              )}
            </section>

            <p className="mt-8 text-center text-xs text-[var(--hicas-text-muted)]">
              © 2026 HICAS Group. All rights reserved.
            </p>
          </div>
        </main>
      </div>
    </div>
  );
};
