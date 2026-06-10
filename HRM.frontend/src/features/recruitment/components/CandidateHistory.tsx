import React, { useState } from "react";
import { candidateApi } from "../api/candidateApi";
import type { CandidateHistoryDto } from "../types/candidate";
import { useNotification } from "../../../core/context/NotificationContext";
import { CandidateApplyForm } from "./CandidateApplyForm";

export const CandidateHistory: React.FC = () => {
  const [email, setEmail] = useState("");
  const [trackingCode, setTrackingCode] = useState("");
  const [history, setHistory] = useState<CandidateHistoryDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [hasSearched, setHasSearched] = useState(false);
  const [selectedJobToUpdate, setSelectedJobToUpdate] = useState<{
    id: number;
    title: string;
  } | null>(null);

  const { triggerAlert } = useNotification();

  React.useEffect(() => {
    const storedEmail = localStorage.getItem("candidate_email");
    const storedTrackingCode = localStorage.getItem("candidate_trackingCode");

    if (storedEmail && storedTrackingCode) {
      setEmail(storedEmail);
      setTrackingCode(storedTrackingCode);
      fetchHistory(storedEmail, storedTrackingCode);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const fetchHistory = async (searchEmail: string, searchCode: string) => {
    setLoading(true);
    setHasSearched(true);
    try {
      const res = await candidateApi.getMyApplications(searchEmail, searchCode);
      setHistory(res.data || []);
    } catch (error) {
      console.error(error);
      triggerAlert(
        "error",
        "Lỗi",
        "Không thể lấy lịch sử ứng tuyển hoặc sai mã tra cứu.",
      );
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email || !trackingCode) {
      triggerAlert(
        "warning",
        "Thiếu thông tin",
        "Vui lòng nhập email và mã tra cứu.",
      );
      return;
    }
    await fetchHistory(email, trackingCode);
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "New":
        return (
          <span className="bg-blue-100 text-blue-800 text-xs font-semibold px-2.5 py-0.5 rounded-full">
            Chờ xử lý
          </span>
        );
      case "Interview_Pending":
        return (
          <span className="bg-yellow-100 text-yellow-800 text-xs font-semibold px-2.5 py-0.5 rounded-full">
            Chờ phỏng vấn
          </span>
        );
      case "Interview_Passed":
        return (
          <span className="bg-indigo-100 text-indigo-800 text-xs font-semibold px-2.5 py-0.5 rounded-full">
            Đạt phỏng vấn (Chờ GĐ duyệt)
          </span>
        );
      case "Offer":
        return (
          <span className="bg-green-100 text-green-800 text-xs font-semibold px-2.5 py-0.5 rounded-full">
            Đã gửi Offer
          </span>
        );
      case "Hired":
        return (
          <span className="bg-emerald-100 text-emerald-800 text-xs font-semibold px-2.5 py-0.5 rounded-full">
            Đã tuyển
          </span>
        );
      case "Rejected":
        return (
          <span className="bg-red-100 text-red-800 text-xs font-semibold px-2.5 py-0.5 rounded-full">
            Từ chối
          </span>
        );
      case "SLA_Expired":
        return (
          <span className="bg-gray-100 text-gray-800 text-xs font-semibold px-2.5 py-0.5 rounded-full">
            Hết hạn xử lý
          </span>
        );
      default:
        return (
          <span className="bg-gray-100 text-gray-800 text-xs font-semibold px-2.5 py-0.5 rounded-full">
            {status}
          </span>
        );
    }
  };

  return (
    <div className="mt-8 rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
      <h2 className="text-2xl font-bold text-gray-900 mb-6">
        Tra cứu kết quả ứng tuyển
      </h2>

      <form
        onSubmit={handleSearch}
        className="flex flex-col sm:flex-row gap-4 mb-8"
      >
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="Nhập email ứng tuyển..."
          className="flex-1 px-4 py-2.5 rounded-lg border border-gray-300 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none transition-colors"
          required
        />
        <input
          type="text"
          value={trackingCode}
          onChange={(e) => setTrackingCode(e.target.value.toUpperCase())}
          placeholder="Nhập mã tra cứu (VD: CAND-5F3B2A1C)"
          className="flex-1 px-4 py-2.5 rounded-lg border border-gray-300 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none transition-colors font-mono"
          required
        />
        <button
          type="submit"
          disabled={loading}
          className="px-6 py-2.5 bg-gray-900 text-white rounded-lg font-medium hover:bg-black transition-colors disabled:bg-gray-400"
        >
          {loading ? "Đang tìm..." : "Tra cứu"}
        </button>
      </form>

      {hasSearched && !loading && history.length === 0 && (
        <div className="text-center py-8 text-gray-500 bg-gray-50 rounded-xl border border-dashed border-gray-300">
          Không tìm thấy hồ sơ nào ứng với email này.
        </div>
      )}

      {history.length > 0 && (
        <div className="space-y-4">
          {history.map((item) => (
            <div
              key={item.candidateId}
              className="flex flex-col sm:flex-row sm:items-center justify-between p-4 border border-gray-200 rounded-xl hover:border-blue-300 transition-colors bg-gray-50"
            >
              <div className="mb-4 sm:mb-0">
                <h3 className="text-lg font-bold text-gray-900">
                  {item.jobTitle}
                </h3>
                <p className="text-sm text-gray-600 mb-2">
                  Phòng ban: {item.departmentName}
                </p>
                <div className="flex items-center gap-3 text-sm">
                  <span className="text-gray-500">
                    Ngày nộp:{" "}
                    {new Date(item.appliedDate).toLocaleDateString("vi-VN")}
                  </span>
                  {getStatusBadge(item.status)}
                </div>
              </div>

              {/* Chỉ cho phép cập nhật khi Status là New */}
              {item.status === "New" ? (
                <button
                  onClick={() =>
                    setSelectedJobToUpdate({
                      id: item.recruitmentRequestId,
                      title: item.jobTitle,
                    })
                  }
                  className="px-4 py-2 bg-white border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 hover:text-blue-600 transition-colors font-medium text-sm whitespace-nowrap"
                >
                  Cập nhật hồ sơ
                </button>
              ) : (
                <span className="text-xs text-gray-500 italic">
                  Hồ sơ đang được xử lý
                </span>
              )}
            </div>
          ))}
        </div>
      )}

      {selectedJobToUpdate && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-[100] p-4 overflow-y-auto">
          <div className="relative bg-white rounded-2xl w-full max-w-md my-8 shadow-2xl">
            <button
              onClick={() => setSelectedJobToUpdate(null)}
              className="absolute top-4 right-4 text-gray-400 hover:text-gray-800 bg-gray-100 hover:bg-gray-200 rounded-full w-8 h-8 flex items-center justify-center transition-colors"
            >
              ✕
            </button>
            <div className="p-2">
              <CandidateApplyForm
                recruitmentRequestId={selectedJobToUpdate.id}
                jobTitle={selectedJobToUpdate.title}
                onSuccess={() => {
                  setSelectedJobToUpdate(null);
                  handleSearch({ preventDefault: () => {} } as React.FormEvent); // reload list
                }}
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
