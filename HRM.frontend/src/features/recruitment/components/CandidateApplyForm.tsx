import type { ChangeEvent, FormEvent } from "react";
import { useState } from "react";
import { useApplyJob } from "../hooks/useApplyJob.ts";
import type { ApplyJobPayload } from "../types/candidate";
import { useNotification } from "../../../core/context/NotificationContext";

const MAX_CV_SIZE = 5 * 1024 * 1024;

interface Props {
  recruitmentRequestId: number;
  jobTitle: string;
  onSuccess?: () => void;
}

export const CandidateApplyForm = ({
  recruitmentRequestId,
  jobTitle,
  onSuccess,
}: Props) => {
  const { loading, handleApply } = useApplyJob();
  const { triggerAlert } = useNotification();

  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [cvFile, setCvFile] = useState<File | null>(null);
  const [appliedCode, setAppliedCode] = useState<string | null>(null);
  const [receiptEmail, setReceiptEmail] = useState<string | null>(null);

  const handleFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    if (file.size > MAX_CV_SIZE || file.type !== "application/pdf") {
      triggerAlert(
        "warning",
        "CV không hợp lệ",
        "Vui lòng chọn file PDF có dung lượng tối đa 5MB.",
      );
      event.target.value = "";
      setCvFile(null);
      return;
    }

    setCvFile(file);
  };

  const copyTrackingCode = async () => {
    if (!appliedCode) return;

    try {
      await navigator.clipboard.writeText(appliedCode);
      triggerAlert("success", "Đã sao chép mã tra cứu", appliedCode);
    } catch {
      triggerAlert(
        "warning",
        "Chưa thể sao chép tự động",
        "Bạn có thể bôi đen mã tra cứu và sao chép thủ công.",
      );
    }
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();

    if (!cvFile) {
      triggerAlert("warning", "Thiếu CV", "Vui lòng đính kèm CV của bạn.");
      return;
    }

    const normalizedEmail = email.trim();
    const payload: ApplyJobPayload = {
      recruitmentRequestId,
      fullName: fullName.trim(),
      email: normalizedEmail,
      cvFile,
    };

    const result = await handleApply(payload);

    if (result?.trackingCode) {
      localStorage.setItem("candidate_email", normalizedEmail);
      localStorage.setItem("candidate_trackingCode", result.trackingCode);

      setAppliedCode(result.trackingCode);
      setReceiptEmail(normalizedEmail);
      setFullName("");
      setEmail("");
      setCvFile(null);
    }
  };

  if (appliedCode) {
    return (
      <div className="mx-auto max-w-md rounded-2xl border border-[var(--hicas-border)] bg-white p-6 text-center shadow-[var(--shadow-card)] sm:p-8">
        <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-[var(--hicas-success-soft)] text-3xl font-bold text-[var(--hicas-success)]">
          ✓
        </div>
        <h2 className="mb-2 text-2xl font-extrabold text-[var(--hicas-text-main)]">
          HICAS đã nhận hồ sơ
        </h2>
        <p className="mb-5 text-sm leading-6 text-[var(--hicas-text-secondary)]">
          Hồ sơ ứng tuyển vị trí <span className="font-semibold text-[var(--hicas-text-main)]">{jobTitle}</span> đã được ghi nhận.
        </p>

        <div className="mb-5 rounded-xl border border-dashed border-[var(--hicas-orange)] bg-[var(--hicas-orange-soft)] p-4">
          <p className="mb-2 text-sm font-semibold text-[var(--hicas-text-secondary)]">
            Mã tra cứu hồ sơ
          </p>
          <p className="select-all break-all font-mono text-3xl font-extrabold tracking-wider text-[var(--hicas-orange)]">
            {appliedCode}
          </p>
        </div>

        <p className="mb-5 text-sm leading-6 text-[var(--hicas-text-secondary)]">
          Mã này đã được gửi về email <span className="font-semibold text-[var(--hicas-text-main)]">{receiptEmail}</span>. Bạn nên lưu lại mã để tra cứu trạng thái hồ sơ sau này.
        </p>

        <div className="grid gap-3 sm:grid-cols-2">
          <button
            type="button"
            onClick={copyTrackingCode}
            className="rounded-xl border border-[var(--hicas-border)] px-4 py-3 text-sm font-bold text-[var(--hicas-text-main)] transition hover:border-[var(--hicas-orange)] hover:text-[var(--hicas-orange)]"
          >
            Sao chép mã
          </button>
          {onSuccess ? (
            <button
              type="button"
              onClick={onSuccess}
              className="rounded-xl bg-[var(--hicas-orange)] px-4 py-3 text-sm font-bold text-white shadow-[var(--shadow-card)] transition hover:bg-[var(--hicas-orange-hover)]"
            >
              Tôi đã lưu mã
            </button>
          ) : null}
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-md rounded-2xl border border-[var(--hicas-border)] bg-white p-5 shadow-[var(--shadow-card)] sm:p-6">
      <h2 className="mb-2 text-xl font-extrabold text-[var(--hicas-text-main)]">
        Nộp hồ sơ ứng tuyển
      </h2>
      <p className="mb-6 text-sm text-[var(--hicas-text-secondary)]">
        Vị trí: <span className="font-semibold text-[var(--hicas-orange)]">{jobTitle}</span>
      </p>

      <form onSubmit={handleSubmit} className="space-y-4">
        <label className="block text-sm font-semibold text-[var(--hicas-text-main)]">
          Họ và tên *
          <input
            type="text"
            required
            placeholder="Nhập họ và tên"
            className="hicas-input mt-1"
            value={fullName}
            onChange={(event) => setFullName(event.target.value)}
          />
        </label>

        <label className="block text-sm font-semibold text-[var(--hicas-text-main)]">
          Email liên hệ *
          <input
            type="email"
            required
            placeholder="email@example.com"
            className="hicas-input mt-1"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
        </label>

        <label className="block text-sm font-semibold text-[var(--hicas-text-main)]">
          CV ứng tuyển *
          <input
            type="file"
            accept=".pdf"
            required
            className="mt-1 w-full cursor-pointer rounded-xl border border-[var(--hicas-border)] bg-white p-2.5 text-sm file:mr-4 file:rounded-lg file:border-0 file:bg-[var(--hicas-orange-soft)] file:px-4 file:py-2 file:text-sm file:font-bold file:text-[var(--hicas-orange)] hover:file:bg-orange-100"
            onChange={handleFileChange}
          />
          <span className="mt-1 block text-xs font-normal text-[var(--hicas-text-muted)]">
            Chỉ nhận file PDF, tối đa 5MB.
          </span>
        </label>

        <button
          type="submit"
          disabled={loading}
          className="mt-2 w-full rounded-xl bg-[var(--hicas-orange)] px-4 py-3 font-bold text-white shadow-[var(--shadow-card)] transition hover:bg-[var(--hicas-orange-hover)] disabled:cursor-not-allowed disabled:opacity-60"
        >
          {loading ? "Đang gửi hồ sơ..." : "Gửi hồ sơ"}
        </button>
      </form>
    </div>
  );
};
