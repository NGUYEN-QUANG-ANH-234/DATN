import React, { useRef, useState } from "react";
import { useApplyJob } from "../hooks/useApplyJob";
import { useActiveJobs } from "../hooks/useActiveJobs";
import type { ApplyJobPayload } from "../types/candidate";
import { useNotification } from "../../../core/context/NotificationContext";
import {
  fieldClass,
  primaryButtonClass,
} from "../../../core/components/FeatureShell";

const MAX_CV_SIZE = 5 * 1024 * 1024;

export const CandidateJobBoard: React.FC = () => {
  const { jobs, loadingJobs } = useActiveJobs();
  const { loading, handleApply } = useApplyJob();
  const { triggerAlert } = useNotification();

  const [selectedJobId, setSelectedJobId] = useState<number | "">("");
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [cvFile, setCvFile] = useState<File | null>(null);

  const fileInputRef = useRef<HTMLInputElement>(null);

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

    if (!selectedJobId) {
      triggerAlert("warning", "Thiếu vị trí", "Vui lòng chọn vị trí tuyển dụng.");
      return;
    }

    if (!cvFile) {
      triggerAlert("warning", "Thiếu CV", "Vui lòng đính kèm CV của bạn.");
      return;
    }

    const payload: ApplyJobPayload = {
      recruitmentRequestId: Number(selectedJobId),
      fullName,
      email,
      cvFile,
    };

    const result = await handleApply(payload);
    if (result) {
      setSelectedJobId("");
      setFullName("");
      setEmail("");
      setCvFile(null);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  };

  if (loadingJobs) {
    return <div className="p-10 text-center text-gray-500">Đang tải danh sách việc làm...</div>;
  }

  return (
    <div className="mx-auto mt-10 max-w-xl rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
      <h2 className="text-2xl font-bold text-gray-900">Cổng ứng viên HICAS</h2>
      <p className="mt-1 text-sm text-gray-500">
        Chọn vị trí bạn muốn ứng tuyển và đính kèm CV.
      </p>

      <form onSubmit={handleSubmit} className="mt-6 space-y-5">
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            Vị trí ứng tuyển *
          </label>
          <select
            required
            className={fieldClass}
            value={selectedJobId}
            onChange={(e) =>
              setSelectedJobId(e.target.value ? Number(e.target.value) : "")
            }
          >
            <option value="">Chọn vị trí đang tuyển</option>
            {jobs.map((job) => (
              <option key={job.id} value={job.id}>
                {job.positionName} - {job.departmentName} (Cần: {job.quantity})
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            Họ và tên *
          </label>
          <input
            type="text"
            required
            placeholder="Nhập họ và tên..."
            className={fieldClass}
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            Email liên hệ *
          </label>
          <input
            type="email"
            required
            placeholder="email@example.com"
            className={fieldClass}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            Đính kèm CV (PDF, tối đa 5MB) *
          </label>
          <input
            type="file"
            accept=".pdf"
            required
            ref={fileInputRef}
            className={fieldClass}
            onChange={handleFileChange}
          />
        </div>

        <button
          type="submit"
          disabled={loading || jobs.length === 0}
          className={`w-full ${primaryButtonClass}`}
        >
          {loading ? "Đang xử lý hồ sơ..." : "Nộp hồ sơ ứng tuyển"}
        </button>
      </form>
    </div>
  );
};
