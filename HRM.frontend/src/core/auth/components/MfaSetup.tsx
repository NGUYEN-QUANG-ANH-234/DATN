import React, { useState } from "react";
import { QRCodeSVG } from "qrcode.react";
import { AxiosError } from "axios";
import { CheckCircle2, Copy, KeyRound, LockKeyhole, QrCode, ShieldCheck } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card } from "../../../components/ui";
import { cn } from "../../../components/ui/classNames";
import { authApi } from "../api/authApi";
import type { MfaSetupResponse } from "../types";
import { useCurrentUser } from "../hooks/useCurrentUser";

const steps = [
  { id: 1, label: "Khởi tạo" },
  { id: 2, label: "Quét QR" },
  { id: 3, label: "Mã khôi phục" },
] as const;

const StepIndicator = ({ currentStep }: { currentStep: 1 | 2 | 3 }) => (
  <div className="grid gap-3 sm:grid-cols-3">
    {steps.map((item) => {
      const active = item.id === currentStep;
      const done = item.id < currentStep;

      return (
        <div
          key={item.id}
          className={cn(
            "flex items-center gap-3 rounded-[var(--radius-lg)] border px-4 py-3",
            active
              ? "border-[var(--hicas-orange)] bg-[var(--hicas-orange-soft)]"
              : done
                ? "border-[var(--hicas-success)] bg-[var(--hicas-success-soft)]"
                : "border-[var(--hicas-border)] bg-white",
          )}
        >
          <span
            className={cn(
              "flex h-8 w-8 items-center justify-center rounded-full text-sm font-bold",
              active
                ? "bg-[var(--hicas-orange)] text-white"
                : done
                  ? "bg-[var(--hicas-success)] text-white"
                  : "bg-[var(--hicas-bg-soft)] text-[var(--hicas-text-secondary)]",
            )}
          >
            {item.id}
          </span>
          <span className="text-sm font-semibold text-[var(--hicas-text-main)]">
            {item.label}
          </span>
        </div>
      );
    })}
  </div>
);

export const MfaSetup: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useCurrentUser();

  const [step, setStep] = useState<1 | 2 | 3>(1);
  const [qrData, setQrData] = useState<MfaSetupResponse | null>(null);
  const [otpCode, setOtpCode] = useState("");
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [isAlreadySetup, setIsAlreadySetup] = useState<boolean>(
    (user as { isMfaEnabled?: boolean })?.isMfaEnabled || false,
  );

  React.useEffect(() => {
    if ((user as { isMfaEnabled?: boolean })?.isMfaEnabled) {
      setIsAlreadySetup(true);
    }
  }, [user]);

  const handleStartSetup = async () => {
    try {
      setSubmitting(true);
      setError("");
      const response = await authApi.initiateMfaSetup();
      const data = ("data" in response ? response.data : response) as MfaSetupResponse;

      if (data.qrCodeUri && data.secretKey) {
        setQrData({
          qrCodeUri: data.qrCodeUri,
          secretKey: data.secretKey,
        });
        setStep(2);
      } else {
        setError("Không nhận được dữ liệu cấu hình MFA từ máy chủ.");
      }
    } catch (err) {
      if (err instanceof AxiosError) {
        const errorData = err.response?.data;
        const errorMessage = String(
          errorData?.message || errorData?.Message || errorData?.detail || errorData || "",
        ).toLowerCase();

        if (errorMessage.includes("mfa đã được bật") || errorMessage.includes("already enabled")) {
          setIsAlreadySetup(true);
        } else {
          setError(
            errorData?.message ||
              errorData?.detail ||
              "Lỗi khi khởi tạo cấu hình MFA. Vui lòng thử lại.",
          );
        }
      } else {
        setError("Đã xảy ra lỗi không xác định.");
      }
    } finally {
      setSubmitting(false);
    }
  };

  const handleConfirm = async () => {
    if (otpCode.length < 6) {
      setError("Vui lòng nhập đủ 6 số OTP.");
      return;
    }

    try {
      setSubmitting(true);
      setError("");
      const response = await authApi.confirmMfaSetup(otpCode);
      const data = ("data" in response ? response.data : response) as {
        recoveryCodes: string[];
      };

      setRecoveryCodes(data.recoveryCodes);
      setStep(3);
    } catch (err) {
      if (err instanceof AxiosError) {
        setError(err.response?.data?.message || "Mã OTP không hợp lệ hoặc đã hết hạn.");
      } else {
        setError("Đã xảy ra lỗi khi xác nhận OTP.");
      }
    } finally {
      setSubmitting(false);
    }
  };

  const copyRecoveryCodes = async () => {
    if (recoveryCodes.length === 0) return;
    await navigator.clipboard.writeText(recoveryCodes.join("\n"));
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Xác thực đa yếu tố"
        description="Bật mã OTP để bảo vệ tài khoản khi đăng nhập."
        breadcrumb={[
          { label: "Tài khoản" },
          { label: "MFA" },
        ]}
        actions={
          <Badge variant={isAlreadySetup ? "success" : "warning"}>
            {isAlreadySetup ? "Đã kích hoạt" : "Cần thiết lập"}
          </Badge>
        }
      />

      <div className="grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_360px]">
        <Card
          title="Luồng thiết lập MFA"
          description="Hoàn tất từng bước để kích hoạt xác thực OTP."
        >
          <div className="space-y-6">
            <StepIndicator currentStep={step} />

            {error && (
              <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-danger)] bg-[var(--hicas-danger-soft)] px-4 py-3 text-sm font-medium text-[var(--hicas-danger)]">
                {error}
              </div>
            )}

            {isAlreadySetup ? (
              <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-success)] bg-[var(--hicas-success-soft)] p-6">
                <div className="flex flex-col gap-4 sm:flex-row sm:items-center">
                  <span className="flex h-14 w-14 items-center justify-center rounded-full bg-white text-[var(--hicas-success)] shadow-sm">
                    <CheckCircle2 size={28} />
                  </span>
                  <div>
                    <h3 className="text-lg font-semibold text-[var(--hicas-text-main)]">
                      MFA đã được kích hoạt
                    </h3>
                    <p className="mt-1 text-sm leading-6 text-[var(--hicas-text-secondary)]">
                      Tài khoản của bạn đang được bảo vệ bằng xác thực hai lớp. Bạn không cần thiết
                      lập lại.
                    </p>
                  </div>
                </div>
                <div className="mt-5">
                  <Button type="button" variant="secondary" onClick={() => navigate("/dashboard")}>
                    Quay về bảng điều khiển
                  </Button>
                </div>
              </div>
            ) : (
              <>
                {step === 1 && (
                  <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-[var(--hicas-bg)] p-5">
                    <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                      <div className="flex gap-4">
                        <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-[var(--radius-lg)] bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange-dark)]">
                          <ShieldCheck size={22} />
                        </span>
                        <div>
                          <h3 className="font-semibold text-[var(--hicas-text-main)]">
                            Bắt đầu bảo vệ tài khoản
                          </h3>
                          <p className="mt-1 text-sm leading-6 text-[var(--hicas-text-secondary)]">
                            Hệ thống sẽ tạo secret key và QR code để bạn thêm tài khoản HICAS vào
                            ứng dụng Authenticator.
                          </p>
                        </div>
                      </div>
                      <Button
                        type="button"
                        iconLeft={<QrCode size={16} />}
                        isLoading={submitting}
                        onClick={handleStartSetup}
                      >
                        Bắt đầu thiết lập
                      </Button>
                    </div>
                  </div>
                )}

                {step === 2 && qrData && (
                  <div className="grid gap-6 lg:grid-cols-[320px_minmax(0,1fr)]">
                    <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-5 text-center">
                      <div className="mx-auto inline-flex rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-4 shadow-sm">
                        <QRCodeSVG value={qrData.qrCodeUri} size={196} />
                      </div>
                      <p className="mt-4 text-sm font-semibold text-[var(--hicas-text-main)]">
                        Quét QR bằng Google Authenticator
                      </p>
                      <p className="mt-1 text-xs leading-5 text-[var(--hicas-text-secondary)]">
                        Nếu không quét được QR, nhập secret key thủ công.
                      </p>
                    </div>

                    <div className="space-y-5">
                      <div>
                        <label className="block">
                          <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">
                            Secret key
                          </span>
                          <input
                            readOnly
                            value={qrData.secretKey}
                            className="hicas-input w-full bg-[var(--hicas-bg)] font-mono"
                          />
                        </label>
                      </div>

                      <label className="block">
                        <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">
                          Mã OTP 6 số
                        </span>
                        <input
                          type="text"
                          maxLength={6}
                          value={otpCode}
                          onChange={(event) =>
                            setOtpCode(event.target.value.replace(/\D/g, ""))
                          }
                          className="hicas-input h-14 w-full text-center font-mono text-2xl font-semibold tracking-[0.45em]"
                          placeholder="000000"
                        />
                      </label>

                      <Button
                        type="button"
                        fullWidth
                        iconLeft={<LockKeyhole size={16} />}
                        isLoading={submitting}
                        disabled={otpCode.length !== 6}
                        onClick={handleConfirm}
                      >
                        Xác nhận thiết lập
                      </Button>
                    </div>
                  </div>
                )}

                {step === 3 && (
                  <div className="space-y-5">
                    <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-success)] bg-[var(--hicas-success-soft)] px-4 py-3 text-sm font-medium text-[var(--hicas-success)]">
                      MFA đã được kích hoạt. Hãy lưu lại mã khôi phục trước khi rời khỏi trang.
                    </div>

                    <div className="grid gap-3 sm:grid-cols-2">
                      {recoveryCodes.map((code) => (
                        <div
                          key={code}
                          className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-[var(--hicas-bg)] px-4 py-3 text-center font-mono text-sm font-semibold text-[var(--hicas-text-main)]"
                        >
                          {code}
                        </div>
                      ))}
                    </div>

                    <div className="flex flex-col gap-3 sm:flex-row sm:justify-end">
                      <Button
                        type="button"
                        variant="secondary"
                        iconLeft={<Copy size={16} />}
                        onClick={copyRecoveryCodes}
                      >
                        Sao chép mã
                      </Button>
                      <Button type="button" onClick={() => navigate("/dashboard")}>
                        Hoàn tất
                      </Button>
                    </div>
                  </div>
                )}
              </>
            )}
          </div>
        </Card>

        <Card
          title="Ghi chú bảo mật"
          description="Các lưu ý giúp bảo vệ tài khoản của bạn."
          actions={<KeyRound size={20} className="text-[var(--hicas-orange)]" />}
        >
          <div className="space-y-3 text-sm leading-6 text-[var(--hicas-text-secondary)]">
            <p>
              Mã OTP thay đổi liên tục theo thời gian, chỉ sử dụng khi đăng nhập hoặc xác nhận
              hành động bảo mật.
            </p>
            <p>
              Mã khôi phục chỉ hiển thị một lần. Hãy lưu ở nơi an toàn và không chia sẻ cho người
              khác.
            </p>
            <p>
              Nếu mất thiết bị xác thực, liên hệ Admin để kiểm tra danh tính và cấp lại quyền truy
              cập.
            </p>
          </div>
        </Card>
      </div>
    </div>
  );
};
