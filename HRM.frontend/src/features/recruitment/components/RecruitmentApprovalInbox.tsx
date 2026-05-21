import React, { useState, useEffect, useCallback } from "react";
import { recruitmentApi } from "../api/recruitmentApi";
import { AxiosError } from "axios";
import { useNotification } from "../../../core/context/NotificationContext";
import { BACKEND_URL } from "../../../core/api/config";

// Cấu trúc dữ liệu trả về từ API /approvals/pending
export interface PendingApprovalDto {
  approvalRequestId: number;
  moduleCode: string;
  referenceId: number;
  level: number;
  createdAt: string;
  title: string;
  description?: string;
  departmentName?: string;
  positionName?: string;
  quantity?: number;
  deadline?: string;
  cvFilePath?: string;
}

export const RecruitmentApprovalInbox: React.FC = () => {
  const [pendingRequests, setPendingRequests] = useState<PendingApprovalDto[]>(
    [],
  );
  const [loading, setLoading] = useState(false);
  const { triggerAlert } = useNotification();

  const fetchPendingRequests = useCallback(async () => {
    setLoading(true);
    try {
      const res: unknown = await recruitmentApi.getPendingApprovals();
      const rawData = res as {
        data?: PendingApprovalDto[];
        Data?: PendingApprovalDto[];
      };
      setPendingRequests(rawData.data || rawData.Data || []);
    } catch (error: unknown) {
      console.error("Lỗi tải danh sách chờ duyệt", error);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchPendingRequests();
  }, [fetchPendingRequests]);

  const handleReview = (
    moduleCode: string,
    referenceId: number,
    isApproved: boolean,
  ) => {
    const actionName = isApproved ? "DUYỆT" : "TỪ CHỐI";

    triggerAlert(
      "confirm",
      "Xác nhận phê duyệt",
      `Bạn chắc chắn muốn ${actionName} yêu cầu này?`,
      async () => {
        try {
          await recruitmentApi.reviewRequest({
            moduleCode,
            referenceId,
            isApproved,
            note: "",
          });

          // Cập nhật giao diện ngay lập tức
          setPendingRequests((prev) =>
            prev.filter(
              (req) =>
                !(
                  req.moduleCode === moduleCode &&
                  req.referenceId === referenceId
                ),
            ),
          );

          triggerAlert(
            "success",
            "Thành công",
            `Đã ${actionName} yêu cầu thành công!`,
          );
        } catch (error: unknown) {
          const axiosError = error as AxiosError<{
            message?: string;
            Message?: string;
          }>;

          const errMsg =
            axiosError.response?.data?.message ||
            axiosError.response?.data?.Message ||
            "Lỗi xử lý";

          triggerAlert("error", "Thất bại", errMsg);
        }
      },
    );
  };

  const getFileUrl = (path?: string) => {
    if (!path) return "#";
    if (path.startsWith("http")) return path;
    return `${BACKEND_URL}${path.startsWith("/") ? "" : "/"}${path}`;
  };

  if (loading)
    return (
      <div className="p-8 text-center text-gray-500 animate-pulse">
        Đang tải hộp thư phê duyệt...
      </div>
    );

  return (
    <div className="rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
      <h2 className="text-xl font-bold text-gray-800 mb-6 border-b pb-3">
        Hộp Thư Phê Duyệt Hệ Thống
      </h2>

      {pendingRequests.length === 0 ? (
        <div className="text-center py-12 bg-gray-50 rounded-lg border border-dashed border-gray-200">
          <p className="text-gray-500">
            Tuyệt vời! Bạn không có yêu cầu nào đang chờ xử lý.
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {pendingRequests.map((req) => (
            <div
              key={`${req.moduleCode}-${req.referenceId}`}
              className="border border-gray-200 p-5 rounded-lg flex flex-col md:flex-row justify-between items-start md:items-center hover:border-blue-300 transition-colors bg-gray-50 gap-4"
            >
              <div className="flex-1">
                <div className="flex items-center gap-2 mb-2">
                  <span
                    className={`text-xs font-bold px-2 py-1 rounded ${req.moduleCode === "CANDIDATE" ? "bg-purple-100 text-purple-700" : req.moduleCode.startsWith("CONTRACT") ? "bg-green-100 text-green-700" : "bg-blue-100 text-blue-700"}`}
                  >
                    {req.moduleCode === "CANDIDATE"
                      ? "ỨNG VIÊN"
                      : req.moduleCode.startsWith("CONTRACT")
                      ? "HỢP ĐỒNG"
                      : "YÊU CẦU TUYỂN DỤNG"}
                  </span>
                  <span className="text-xs text-gray-500">
                    Cấp duyệt: {req.level}
                  </span>
                </div>

                <h3 className="font-bold text-lg text-gray-800">
                  {req.moduleCode === "CANDIDATE" || req.moduleCode.startsWith("CONTRACT")
                    ? req.title
                    : `Cần tuyển: ${req.quantity} nhân sự`}
                </h3>

                <p className="text-sm text-gray-600 mt-1">
                  <strong>Phòng ban:</strong> {req.departmentName || "Không rõ"}{" "}
                  | <strong>Vị trí:</strong> {req.positionName || "Không rõ"}
                </p>

                {req.moduleCode === "RECRUITMENT" && (
                  <>
                    <p className="text-sm text-gray-500 mt-1">
                      Hạn chót:{" "}
                      {req.deadline
                        ? new Date(req.deadline).toLocaleDateString("vi-VN")
                        : "Không có"}
                    </p>
                    {req.description && (
                      <p className="text-sm text-gray-500 mt-2 italic border-l-2 border-gray-300 pl-2">
                        "{req.description}"
                      </p>
                    )}
                  </>
                )}

                {req.moduleCode.startsWith("CONTRACT") && (
                  <p className="text-sm text-gray-500 mt-1">
                    Loại hợp đồng: <strong>{req.description}</strong>
                  </p>
                )}

                {req.moduleCode === "CANDIDATE" && req.cvFilePath && (
                  <div className="mt-3">
                    <a
                      href={getFileUrl(req.cvFilePath)}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-flex items-center text-sm font-medium text-blue-600 hover:text-blue-800 hover:underline"
                    >
                      <svg
                        className="w-4 h-4 mr-1"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth="2"
                          d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13"
                        ></path>
                      </svg>
                      Xem Hồ sơ/CV đính kèm
                    </a>
                  </div>
                )}
              </div>

              <div className="flex gap-3 shrink-0">
                <button
                  onClick={() =>
                    handleReview(req.moduleCode, req.referenceId, false)
                  }
                  className="px-4 py-2 bg-white text-red-600 border border-red-200 rounded hover:bg-red-50 transition-colors font-medium"
                >
                  Từ chối
                </button>
                <button
                  onClick={() =>
                    handleReview(req.moduleCode, req.referenceId, true)
                  }
                  className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 shadow-sm transition-colors font-medium"
                >
                  Phê duyệt
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
