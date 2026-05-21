import React, { useState } from "react";
import { useApplyJob } from "../hooks/useApplyJob.ts";
import type { ApplyJobPayload } from "../types/candidate";
import { useNotification } from "../../../core/context/NotificationContext";

const MAX_CV_SIZE = 5 * 1024 * 1024;

interface Props {
  recruitmentRequestId: number;
  jobTitle: string; // Tên vị trí để hiển thị cho đẹp
  onSuccess?: () => void; // Callback đóng Modal sau khi nộp xong (nếu cần)
}

export const CandidateApplyForm: React.FC<Props> = ({
  recruitmentRequestId,
  jobTitle,
  onSuccess,
}) => {
  const { loading, handleApply } = useApplyJob();
  const { triggerAlert } = useNotification();

  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [cvFile, setCvFile] = useState<File | null>(null);
  const [appliedCode, setAppliedCode] = useState<string | null>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > MAX_CV_SIZE || file.type !== "application/pdf") {
      triggerAlert(
        "warning",
        "CV không hợp lệ",
        "Vui lòng chọn file PDF có dung lượng tối đa 5MB.",
      );
      e.target.value = "";
      setCvFile(null);
      return;
    }

    setCvFile(file);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!cvFile) {
      triggerAlert("warning", "Thiếu CV", "Vui lòng đính kèm CV của bạn.");
      return;
    }

    const payload: ApplyJobPayload = {
      recruitmentRequestId,
      fullName,
      email,
      cvFile,
    };

    const result = await handleApply(payload);

    if (result && result.trackingCode) {
      // Save to local storage for auto-lookup later
      localStorage.setItem("candidate_email", email);
      localStorage.setItem("candidate_trackingCode", result.trackingCode);

      setAppliedCode(result.trackingCode);
      setFullName("");
      setCvFile(null);
      // Keep email for next use maybe, but we can clear it too
      setEmail("");

      // Delay close if needed or just show success UI
      setTimeout(() => {
        if (onSuccess) onSuccess();
      }, 3000);
    }
  };

  if (appliedCode) {
    return (
      <div className="mx-auto max-w-md rounded-lg border border-gray-200 bg-white p-8 text-center shadow-sm">
        <div className="w-16 h-16 bg-green-100 text-green-600 rounded-full flex items-center justify-center mx-auto mb-4 text-3xl">
          ✓
        </div>
        <h2 className="text-2xl font-bold text-gray-800 mb-2">Nộp thành công!</h2>
        <p className="text-gray-600 mb-6">
          Cảm ơn bạn đã ứng tuyển vị trí <span className="font-semibold">{jobTitle}</span>.
        </p>
        <div className="bg-gray-50 p-4 rounded-lg border border-dashed border-gray-300 mb-6">
          <p className="text-sm text-gray-500 mb-1">Mã tra cứu hồ sơ của bạn:</p>
          <p className="text-2xl font-mono font-bold text-blue-600 tracking-wider">{appliedCode}</p>
        </div>
        <p className="text-sm text-gray-500">
          Mã này đã được lưu tự động trên trình duyệt này để tra cứu sau.
        </p>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-md rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
      <h2 className="text-xl font-bold text-gray-800 mb-2">
        Nộp hồ sơ ứng tuyển
      </h2>
      <p className="text-sm text-gray-500 mb-6">
        Vị trí: <span className="font-semibold text-blue-600">{jobTitle}</span>
      </p>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Họ và tên *
          </label>
          <input
            type="text"
            required
            placeholder="Nhập họ và tên..."
            className="w-full border p-2.5 rounded focus:ring-2 focus:ring-blue-500 outline-none"
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Email liên hệ *
          </label>
          <input
            type="email"
            required
            placeholder="email@example.com"
            className="w-full border p-2.5 rounded focus:ring-2 focus:ring-blue-500 outline-none"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Đính kèm CV (PDF, &lt; 5MB) *
          </label>
          <input
            type="file"
            accept=".pdf"
            required
            className="w-full border p-2.5 rounded file:mr-4 file:py-2 file:px-4 file:rounded file:border-0 file:text-sm file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100 cursor-pointer"
            onChange={handleFileChange}
          />
        </div>

        <button
          type="submit"
          disabled={loading}
          className="w-full bg-blue-600 hover:bg-blue-700 text-white font-medium py-3 px-4 rounded-lg mt-2 disabled:opacity-60 transition-colors"
        >
          {loading ? "Đang xử lý hồ sơ..." : "Gửi CV Ứng Tuyển"}
        </button>
      </form>
    </div>
  );
};
