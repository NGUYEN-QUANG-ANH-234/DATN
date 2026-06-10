import React, { useEffect, useState } from "react";
import { candidateApi } from "../api/candidateApi";
import type { ActiveJob } from "../types/candidate";
import { CandidateApplyForm } from "./CandidateApplyForm";
import { CandidateHistory } from "./CandidateHistory";

export const PublicCareersPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<"jobs" | "history">("jobs");
  const [jobs, setJobs] = useState<ActiveJob[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedJob, setSelectedJob] = useState<ActiveJob | null>(null);

  useEffect(() => {
    const fetchJobs = async () => {
      setLoading(true);
      try {
        const response = await candidateApi.getActiveJobs();
        setJobs(response.data || []);
      } catch (error) {
        console.error("Không thể tải vị trí tuyển dụng:", error);
      } finally {
        setLoading(false);
      }
    };

    void fetchJobs();
  }, []);

  return (
    <div className="flex min-h-screen flex-col items-center bg-gray-50 px-4 py-12 sm:px-6 lg:px-8">
      <div className="w-full max-w-4xl">
        <div className="mb-10 text-center">
          <h1 className="text-4xl font-extrabold tracking-tight text-gray-900">
            Cổng tuyển dụng HICAS
          </h1>
          <p className="mt-3 text-lg text-gray-500">
            Chọn vị trí phù hợp và gửi hồ sơ ứng tuyển của bạn.
          </p>
        </div>

        <div className="mb-8 flex justify-center">
          <div className="flex gap-1 rounded-lg bg-gray-200 p-1">
            <button
              onClick={() => setActiveTab("jobs")}
              className={`rounded-lg px-6 py-2.5 text-sm font-medium transition-all ${
                activeTab === "jobs"
                  ? "bg-white text-gray-900 shadow-sm"
                  : "text-gray-600 hover:bg-gray-300 hover:text-gray-900"
              }`}
            >
              Cơ hội việc làm
            </button>
            <button
              onClick={() => setActiveTab("history")}
              className={`rounded-lg px-6 py-2.5 text-sm font-medium transition-all ${
                activeTab === "history"
                  ? "bg-white text-gray-900 shadow-sm"
                  : "text-gray-600 hover:bg-gray-300 hover:text-gray-900"
              }`}
            >
              Tra cứu hồ sơ
            </button>
          </div>
        </div>

        {activeTab === "jobs" ? (
          <>
            {loading ? (
              <div className="py-12 text-center">
                <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-t-transparent" />
                <p className="mt-4 font-medium text-gray-500">Đang tải dữ liệu...</p>
              </div>
            ) : jobs.length === 0 ? (
              <div className="rounded-xl border border-gray-100 bg-white p-10 text-center shadow-sm">
                <h3 className="text-xl font-bold text-gray-800">
                  Hiện chưa có vị trí nào đang tuyển
                </h3>
                <p className="mt-2 text-gray-500">Vui lòng quay lại sau.</p>
              </div>
            ) : (
              <div className="grid gap-6 md:grid-cols-2">
                {jobs.map((job) => {
                  const remainingSlots = job.remainingSlots ?? job.quantity;
                  const canApply = job.canApply !== false && remainingSlots > 0;

                  return (
                    <div
                      key={job.id}
                      className="flex flex-col justify-between rounded-lg border border-gray-200 bg-white p-6 shadow-sm transition-shadow hover:border-blue-200 hover:shadow-md"
                    >
                      <div>
                        <div className="mb-4 flex items-start justify-between gap-3">
                          <h2 className="text-xl font-bold text-blue-900">
                            {job.positionName || "Chưa cập nhật"}
                          </h2>
                          <span className="whitespace-nowrap rounded-full bg-blue-50 px-2.5 py-1 text-xs font-bold text-blue-700">
                            Còn {remainingSlots} vị trí
                          </span>
                        </div>
                        <div className="mb-6 space-y-2">
                          <div className="text-sm text-gray-600">
                            Phòng ban: {job.departmentName || "Chưa cập nhật"}
                          </div>
                          {job.deadline && (
                            <div className="text-sm text-gray-600">
                              Hạn nộp:{" "}
                              <strong className="text-red-500">
                                {new Date(job.deadline).toLocaleDateString("vi-VN")}
                              </strong>
                            </div>
                          )}
                          <div className="text-sm text-gray-600">
                            Chỉ tiêu: {job.filledSlots ?? 0}/{job.quantity} đã chốt
                          </div>
                        </div>
                        {job.description && (
                          <p className="mb-6 line-clamp-3 rounded-lg bg-gray-50 p-3 text-sm text-gray-600">
                            {job.description}
                          </p>
                        )}
                      </div>
                      <button
                        onClick={() => canApply && setSelectedJob(job)}
                        disabled={!canApply}
                        className="w-full rounded-lg bg-gray-900 px-4 py-2.5 font-medium text-white shadow-sm transition-colors hover:bg-black disabled:cursor-not-allowed disabled:bg-gray-300"
                      >
                        {canApply ? "Ứng tuyển ngay" : "Đã ngừng nhận hồ sơ"}
                      </button>
                    </div>
                  );
                })}
              </div>
            )}
          </>
        ) : (
          <CandidateHistory />
        )}
      </div>

      {selectedJob && activeTab === "jobs" && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center overflow-y-auto bg-black/60 p-4 backdrop-blur-sm">
          <div className="relative my-8 w-full max-w-md rounded-2xl bg-white shadow-2xl">
            <button
              onClick={() => setSelectedJob(null)}
              className="absolute right-4 top-4 flex h-8 w-8 items-center justify-center rounded-full bg-gray-100 text-gray-400 transition-colors hover:bg-gray-200 hover:text-gray-800"
            >
              x
            </button>
            <div className="p-2">
              <CandidateApplyForm
                recruitmentRequestId={selectedJob.id}
                jobTitle={selectedJob.positionName}
                onSuccess={() => setSelectedJob(null)}
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
