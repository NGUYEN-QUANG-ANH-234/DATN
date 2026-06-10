import { useMemo, useState } from "react";
import type { FormEvent } from "react";
import { KeyRound, LockKeyhole, ShieldCheck } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { PageHeader } from "../../../components/layout";
import { Button, Card, Input } from "../../../components/ui";
import { authApi } from "../api/authApi";

type FormState = {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
};

const initialForm: FormState = {
  currentPassword: "",
  newPassword: "",
  confirmPassword: "",
};

const getApiMessage = (error: unknown, fallback: string) =>
  (error as { response?: { data?: { message?: string; Message?: string } } }).response?.data
    ?.message ||
  (error as { response?: { data?: { message?: string; Message?: string } } }).response?.data
    ?.Message ||
  (error as { message?: string }).message ||
  fallback;

const validatePassword = (password: string) => ({
  length: password.length >= 8,
  upper: /[A-Z]/.test(password),
  lower: /[a-z]/.test(password),
  digit: /\d/.test(password),
  special: /[^A-Za-z0-9]/.test(password),
});

export const AccountSecurityPage = () => {
  const navigate = useNavigate();
  const [form, setForm] = useState<FormState>(initialForm);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const rules = useMemo(() => validatePassword(form.newPassword), [form.newPassword]);
  const isStrongPassword = Object.values(rules).every(Boolean);

  const updateField = (field: keyof FormState, value: string) => {
    setForm((current) => ({ ...current, [field]: value }));
    setError("");
    setSuccess("");
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!form.currentPassword.trim()) {
      setError("Vui lòng nhập mật khẩu hiện tại.");
      return;
    }

    if (!isStrongPassword) {
      setError("Mật khẩu mới chưa đáp ứng đủ yêu cầu bảo mật.");
      return;
    }

    if (form.newPassword !== form.confirmPassword) {
      setError("Xác nhận mật khẩu mới không khớp.");
      return;
    }

    try {
      setIsSubmitting(true);
      const response = await authApi.changePassword(form);
      setSuccess(response.message || response.Message || "Đổi mật khẩu thành công.");
      setForm(initialForm);

      window.setTimeout(() => {
        localStorage.removeItem("accessToken");
        localStorage.removeItem("refreshToken");
        navigate("/", { replace: true });
      }, 1200);
    } catch (err) {
      setError(getApiMessage(err, "Không thể đổi mật khẩu. Vui lòng thử lại."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const ruleItems = [
    { key: "length", label: "Tối thiểu 8 ký tự", valid: rules.length },
    { key: "upper", label: "Có chữ hoa", valid: rules.upper },
    { key: "lower", label: "Có chữ thường", valid: rules.lower },
    { key: "digit", label: "Có số", valid: rules.digit },
    { key: "special", label: "Có ký tự đặc biệt", valid: rules.special },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="Bảo mật tài khoản"
        description="Đổi mật khẩu và kiểm tra trạng thái bảo mật tài khoản."
        breadcrumb={[
          { label: "Tài khoản" },
          { label: "Bảo mật" },
        ]}
      />

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1.15fr)_minmax(320px,0.85fr)]">
        <Card
          title="Đổi mật khẩu"
          description="Nhập mật khẩu hiện tại trước khi cập nhật mật khẩu mới."
        >
          <form onSubmit={handleSubmit} className="space-y-5">
            {error && (
              <div className="rounded-2xl border border-[var(--hicas-danger)] bg-[var(--hicas-danger-soft)] px-4 py-3 text-sm font-medium text-[var(--hicas-danger)]">
                {error}
              </div>
            )}

            {success && (
              <div className="rounded-2xl border border-[var(--hicas-success)] bg-[var(--hicas-success-soft)] px-4 py-3 text-sm font-medium text-[var(--hicas-success)]">
                {success}
              </div>
            )}

            <Input
              label="Mật khẩu hiện tại *"
              name="currentPassword"
              type="password"
              autoComplete="current-password"
              value={form.currentPassword}
              onChange={(event) => updateField("currentPassword", event.target.value)}
              iconLeft={<LockKeyhole size={17} />}
              placeholder="Nhập mật khẩu hiện tại"
            />

            <div className="grid gap-4 md:grid-cols-2">
              <Input
                label="Mật khẩu mới *"
                name="newPassword"
                type="password"
                autoComplete="new-password"
                value={form.newPassword}
                onChange={(event) => updateField("newPassword", event.target.value)}
                iconLeft={<KeyRound size={17} />}
                placeholder="Nhập mật khẩu mới"
              />

              <Input
                label="Xác nhận mật khẩu mới *"
                name="confirmPassword"
                type="password"
                autoComplete="new-password"
                value={form.confirmPassword}
                onChange={(event) => updateField("confirmPassword", event.target.value)}
                iconLeft={<KeyRound size={17} />}
                placeholder="Nhập lại mật khẩu mới"
                error={
                  form.confirmPassword && form.newPassword !== form.confirmPassword
                    ? "Mật khẩu xác nhận chưa khớp."
                    : undefined
                }
              />
            </div>

            <div className="flex flex-col-reverse gap-3 border-t border-[var(--hicas-border-soft)] pt-5 sm:flex-row sm:justify-end">
              <Button
                type="button"
                variant="secondary"
                onClick={() => setForm(initialForm)}
                disabled={isSubmitting}
              >
                Làm mới
              </Button>
              <Button type="submit" isLoading={isSubmitting}>
                Cập nhật mật khẩu
              </Button>
            </div>
          </form>
        </Card>

        <Card
          title="Yêu cầu bảo mật"
          description="Mật khẩu mạnh giúp giảm rủi ro truy cập trái phép."
        >
          <div className="space-y-3">
            {ruleItems.map((item) => (
              <div
                key={item.key}
                className="flex items-center gap-3 rounded-2xl border border-[var(--hicas-border-soft)] bg-[var(--hicas-bg)] px-4 py-3 text-sm"
              >
                <span
                  className={`inline-flex h-7 w-7 items-center justify-center rounded-full ${
                    item.valid
                      ? "bg-[var(--hicas-success-soft)] text-[var(--hicas-success)]"
                      : "bg-[var(--hicas-bg-soft)] text-[var(--hicas-text-muted)]"
                  }`}
                >
                  <ShieldCheck size={15} />
                </span>
                <span
                  className={
                    item.valid
                      ? "font-medium text-[var(--hicas-text-main)]"
                      : "text-[var(--hicas-text-secondary)]"
                  }
                >
                  {item.label}
                </span>
              </div>
            ))}
          </div>

          <div className="mt-5 rounded-2xl border border-[var(--hicas-border)] bg-[var(--hicas-orange-lighter)] p-4 text-sm leading-6 text-[var(--hicas-text-secondary)]">
            Sau khi đổi mật khẩu, refresh token hiện tại sẽ bị thu hồi. Người dùng cần đăng
            nhập lại bằng mật khẩu mới để tiếp tục sử dụng hệ thống.
          </div>
        </Card>
      </div>
    </div>
  );
};
