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
        console.error("Lỗi lấy danh sách việc làm:", error);
      } finally {
        setLoading(false);
      }
    };
    fetchJobs();
  }, []);

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col items-center py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-4xl w-full">
        <div className="text-center mb-12">
          <h1 className="text-4xl font-extrabold text-gray-900 tracking-tight">
            Cổng Thông Tin Tuyển Dụng
          </h1>
          <p className="mt-3 text-lg text-gray-500">
            Tham gia cùng chúng tôi để kiến tạo những giá trị tốt đẹp nhất.
          </p>
        </div>

        <div className="flex justify-center mb-8">
          <div className="flex gap-1 rounded-lg bg-gray-200 p-1">
            <button
              onClick={() => setActiveTab("jobs")}
              className={`px-6 py-2.5 rounded-lg font-medium text-sm transition-all ${
                activeTab === "jobs"
                  ? "bg-white text-gray-900 shadow-sm"
                  : "text-gray-600 hover:text-gray-900 hover:bg-gray-300"
              }`}
            >
              Cơ hội việc làm
            </button>
            <button
              onClick={() => setActiveTab("history")}
              className={`px-6 py-2.5 rounded-lg font-medium text-sm transition-all ${
                activeTab === "history"
                  ? "bg-white text-gray-900 shadow-sm"
                  : "text-gray-600 hover:text-gray-900 hover:bg-gray-300"
              }`}
            >
              Tra cứu hồ sơ
            </button>
          </div>
        </div>

        {activeTab === "jobs" ? (
          <>
            {loading ? (
          <div className="text-center py-12">
            <div className="inline-block animate-spin w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full"></div>
            <p className="mt-4 text-gray-500 font-medium">Đang tải danh sách việc làm...</p>
          </div>
        ) : jobs.length === 0 ? (
          <div className="bg-white p-10 rounded-xl shadow-sm text-center border border-gray-100">
            <span className="text-4xl">📭</span>
            <h3 className="mt-4 text-xl font-bold text-gray-800">Hiện chưa có vị trí nào đang tuyển</h3>
            <p className="text-gray-500 mt-2">Vui lòng quay lại sau.</p>
          </div>
        ) : (
          <div className="grid gap-6 md:grid-cols-2">
            {jobs.map((job) => (
              <div
                key={job.id}
                 className="flex flex-col justify-between rounded-lg border border-gray-200 bg-white p-6 shadow-sm transition-shadow hover:border-blue-200 hover:shadow-md"
              >
                <div>
                  <div className="flex justify-between items-start mb-4">
                    <h2 className="text-xl font-bold text-blue-900">{job.positionName || "Chưa cập nhật"}</h2>
                    <span className="bg-blue-50 text-blue-700 text-xs font-bold px-2.5 py-1 rounded-full whitespace-nowrap">
                      {job.quantity} người
                    </span>
                  </div>
                  <div className="space-y-2 mb-6">
                    <div className="flex items-center text-sm text-gray-600">
                      <span className="w-5 text-center mr-2">🏢</span>
                      <span>{job.departmentName || "Chưa cập nhật"}</span>
                    </div>
                    {job.deadline && (
                      <div className="flex items-center text-sm text-gray-600">
                        <span className="w-5 text-center mr-2">⏳</span>
                        <span>Hạn nộp: <strong className="text-red-500">{new Date(job.deadline).toLocaleDateString("vi-VN")}</strong></span>
                      </div>
                    )}
                  </div>
                  {job.description && (
                    <p className="text-gray-600 text-sm line-clamp-3 mb-6 bg-gray-50 p-3 rounded-lg italic">
                      {job.description}
                    </p>
                  )}
                </div>
                <button
                  onClick={() => setSelectedJob(job)}
                  className="w-full py-2.5 px-4 bg-gray-900 hover:bg-black text-white rounded-lg font-medium transition-colors shadow-sm"
                >
                  Ứng tuyển ngay
                </button>
              </div>
            ))}
          </div>
        )}
          </>
        ) : (
          <CandidateHistory />
        )}
      </div>

      {/* Modal Ứng tuyển */}
      {selectedJob && activeTab === "jobs" && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-[100] p-4 overflow-y-auto">
          <div className="relative bg-white rounded-2xl w-full max-w-md my-8 shadow-2xl">
            <button
              onClick={() => setSelectedJob(null)}
              className="absolute top-4 right-4 text-gray-400 hover:text-gray-800 bg-gray-100 hover:bg-gray-200 rounded-full w-8 h-8 flex items-center justify-center transition-colors"
            >
              ✕
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
