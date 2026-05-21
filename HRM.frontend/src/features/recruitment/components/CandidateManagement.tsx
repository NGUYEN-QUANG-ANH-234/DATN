import React, { useEffect, useState, useCallback, useRef } from "react";
import { candidateApi } from "../api/candidateApi";
import type { CandidateHistoryDto } from "../types/candidate";
import { useNotification } from "../../../core/context/NotificationContext";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import { BACKEND_URL } from "../../../core/api/config";

type AuthUser = {
  role?: string;
  roleName?: string;
  Role?: string;
};

export const CandidateManagement: React.FC = () => {
  const [candidates, setCandidates] = useState<CandidateHistoryDto[]>([]);
  const [loading, setLoading] = useState(false);

  const { triggerAlert } = useNotification();
  const { user } = useCurrentUser() as { user?: AuthUser };

  const alertRef = useRef(triggerAlert);
  useEffect(() => {
    alertRef.current = triggerAlert;
  }, [triggerAlert]);

  // =========================
  // Chuẩn hóa ROLE
  // =========================
  const userRole = String(user?.role || user?.roleName || user?.Role || "")
    .trim()
    .toUpperCase();

  // =========================
  // Load danh sách
  // =========================
  const fetchCandidates = useCallback(async () => {
    setLoading(true);
    try {
      const res = await candidateApi.getAllCandidates();
      setCandidates(res.data || []);
    } catch (error) {
      console.error(error);
      alertRef.current("error", "Lỗi", "Không thể lấy danh sách ứng viên.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchCandidates();
  }, [fetchCandidates]);

  // =========================
  // Check quyền duyệt
  // =========================
  const canApprove = (status: string) => {
    const s = String(status).trim().toUpperCase();

    // CHỈ cho phép HR/ADMIN duyệt ở vòng đầu (NEW).
    // Các vòng sau (INTERVIEW_PENDING, INTERVIEW_PASSED) nút sẽ tự động bị ẩn.
    if (s === "NEW" && (userRole === "HR" || userRole === "ADMIN")) {
      return true;
    }

    return false;
  };

  // =========================
  // Text nút
  // =========================
  const getActionText = (status: string) => {
    const s = String(status).trim().toUpperCase();

    if (s === "NEW") return "HR Duyệt sơ loại";

    return "";
  };

  // =========================
  // Approve
  // =========================
  const handleApprove = async (id: number, status: string) => {
    const s = String(status).trim().toUpperCase();

    try {
      if (s === "NEW" && (userRole === "HR" || userRole === "ADMIN")) {
        await candidateApi.hrApprove(id);

        alertRef.current(
          "success",
          "Thành công",
          "Đã duyệt sơ loại. Hồ sơ đã được đẩy vào Hộp thư phê duyệt của Trưởng phòng.",
        );
      } else {
        alertRef.current(
          "warning",
          "Không hợp lệ",
          "Các vòng duyệt tiếp theo vui lòng thao tác tại Hộp thư phê duyệt.",
        );
        return;
      }

      fetchCandidates();
    } catch (error: unknown) {
      const err = error as {
        response?: {
          data?: {
            message?: string;
          };
        };
      };

      triggerAlert(
        "error",
        "Lỗi phê duyệt",
        err.response?.data?.message || "Lỗi không xác định.",
      );
    }
  };

  // =========================
  // Reject
  // =========================
  const handleReject = async (id: number) => {
    triggerAlert(
      "confirm",
      "Xác nhận",
      "Bạn có chắc muốn từ chối ứng viên này?",
      async () => {
        try {
          await candidateApi.rejectCandidate(id);

          triggerAlert("success", "Thành công", "Đã từ chối ứng viên.");

          fetchCandidates();
        } catch (error: unknown) {
          const err = error as {
            response?: {
              data?: {
                message?: string;
              };
            };
          };

          triggerAlert(
            "error",
            "Lỗi",
            err.response?.data?.message || "Không thể từ chối.",
          );
        }
      },
    );
  };

  // =========================
  // Link CV
  // =========================
  const getFileUrl = (path?: string) => {
    if (!path) return "#";
    if (path.startsWith("http")) return path;

    return `${BACKEND_URL}${path.startsWith("/") ? "" : "/"}${path}`;
  };

  // =========================
  // Badge
  // =========================
  const getStatusBadge = (status: string) => {
    const s = String(status).trim().toUpperCase();

    const styles: Record<string, string> = {
      NEW: "bg-blue-100 text-blue-800",
      INTERVIEW_PENDING: "bg-yellow-100 text-yellow-800",
      INTERVIEW_PASSED: "bg-indigo-100 text-indigo-800",
      OFFER: "bg-green-100 text-green-800",
      REJECTED: "bg-red-100 text-red-800",
      SLA_EXPIRED: "bg-gray-100 text-gray-800",
    };

    return (
      <span
        className={`text-xs font-semibold px-2.5 py-0.5 rounded-full ${
          styles[s] || "bg-gray-100 text-gray-800"
        }`}
      >
        {status}
      </span>
    );
  };

  return (
    <div className="min-h-full bg-gray-50 px-4 py-6 sm:px-6">
      <div className="flex justify-between mb-6 items-center">
        <h1 className="text-2xl font-bold">Quản lý ứng viên</h1>

        <span className="text-sm bg-gray-100 px-3 py-1 rounded">
          Quyền: <b>{userRole || "N/A"}</b>
        </span>
      </div>

      {loading ? (
        <p>Đang tải dữ liệu...</p>
      ) : (
        <div className="overflow-x-auto rounded-lg border border-gray-200 bg-white shadow-sm">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left">Ứng viên</th>
                <th className="px-6 py-3 text-left">Vị trí</th>
                <th className="px-6 py-3 text-left">Ngày nộp</th>
                <th className="px-6 py-3 text-left">Trạng thái</th>
                <th className="px-6 py-3 text-right">Thao tác</th>
              </tr>
            </thead>

            <tbody>
              {candidates.map((c) => (
                <tr key={c.candidateId} className="hover:bg-gray-50">
                  <td className="px-6 py-4">
                    <div>{c.fullName}</div>
                    <div className="text-sm text-gray-500">{c.email}</div>
                  </td>

                  <td className="px-6 py-4">
                    <div>{c.jobTitle}</div>
                    <div className="text-sm text-gray-500">
                      {c.departmentName}
                    </div>
                  </td>

                  <td className="px-6 py-4">
                    {new Date(c.appliedDate).toLocaleDateString("vi-VN")}
                  </td>

                  <td className="px-6 py-4">{getStatusBadge(c.status)}</td>

                  <td className="px-6 py-4 flex justify-end gap-2">
                    {c.cvFilePath && (
                      <a
                        href={getFileUrl(c.cvFilePath)}
                        target="_blank"
                        rel="noreferrer"
                        className="bg-gray-100 px-3 py-1 rounded"
                      >
                        Xem CV
                      </a>
                    )}

                    {canApprove(c.status) && (
                      <>
                        <button
                          onClick={() => handleReject(c.candidateId)}
                          className="bg-red-100 text-red-700 px-3 py-1 rounded"
                        >
                          Từ chối
                        </button>

                        <button
                          onClick={() => handleApprove(c.candidateId, c.status)}
                          className="bg-blue-100 text-blue-700 px-3 py-1 rounded"
                        >
                          {getActionText(c.status)}
                        </button>
                      </>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
