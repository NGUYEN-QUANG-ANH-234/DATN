import React, { useState, useEffect } from "react";
import { hrProfileApi } from "../api/hrProfileApi";
import type { PendingProfileRequest } from "../types/profileRequest";
import { BACKEND_URL } from "../../../core/api/config";
import { useNotification } from "../../../core/context/NotificationContext";

export const HRProfileReviewList: React.FC = () => {
  const [requests, setRequests] = useState<PendingProfileRequest[]>([]);
  const [loading, setLoading] = useState(false);
  const [processingId, setProcessingId] = useState<number | null>(null);
  const { triggerAlert } = useNotification();

  // States quản lý Input Từ chối ngay trên UI (Thay cho window.prompt)
  const [rejectingId, setRejectingId] = useState<number | null>(null);
  const [rejectReason, setRejectReason] = useState("");

  useEffect(() => {
    const fetchRequests = async () => {
      setLoading(true);
      try {
        const res = await hrProfileApi.getPendingRequests();
        setRequests(res.data || res || []);
      } catch (error) {
        console.error("Lỗi tải danh sách:", error);
      } finally {
        setLoading(false);
      }
    };
    fetchRequests();
  }, []);

  // Hàm xử lý API chung
  const executeReview = async (
    id: number,
    isApproved: boolean,
    reason?: string,
  ) => {
    setProcessingId(id);
    try {
      const response: unknown = await hrProfileApi.reviewRequest(id, {
        isApproved,
        rejectReason: reason,
      });

      // Bọc thép: Chỉ cần không văng lỗi Axios (HTTP 4xx, 5xx) thì coi như thành công
      const msg =
        (response as { message?: string })?.message ||
        (response as { Message?: string })?.Message ||
        "Thao tác thành công!";
      triggerAlert("success", "Thành công", msg);

      // Xóa item khỏi UI và reset trạng thái
      setRequests((prev) => prev.filter((r) => r.id !== id));
      setRejectingId(null);
      setRejectReason("");
    } catch (error: unknown) {
      console.error(error); // In ra console để biết Axios phàn nàn gì
      triggerAlert(
        "error",
        "Lỗi xử lý",
        "Giao diện gặp lỗi, nhưng backend có thể đã xử lý. Vui lòng tải lại trang để kiểm tra dữ liệu.",
      );
    } finally {
      setProcessingId(null);
    }
  };

  // Hành động bấm "Phê duyệt"
  const handleApprove = (id: number) => {
    triggerAlert(
      "confirm",
      "Xác nhận phê duyệt",
      "Xác nhận hồ sơ hợp lệ và ghi đè dữ liệu gốc?",
      () => executeReview(id, true),
    );
  };

  // Hành động Submit nút "Từ chối"
  const handleConfirmReject = (id: number) => {
    if (!rejectReason.trim()) {
      triggerAlert("warning", "Thiếu lý do", "Bạn phải nhập lý do từ chối.");
      return;
    }
    executeReview(id, false, rejectReason.trim());
  };

  const renderRequestedChanges = (jsonString: string) => {
    try {
      const data = JSON.parse(jsonString);
      const items = [];

      if (data.FullName)
        items.push(
          <li key="name">
            Tên mới: <b className="text-blue-700">{data.FullName}</b>
          </li>,
        );
      if (data.IdentityNumber)
        items.push(
          <li key="id">
            CCCD mới: <b className="text-blue-700">{data.IdentityNumber}</b>
          </li>,
        );
      if (data.BankAccount)
        items.push(
          <li key="bank">
            TK Ngân hàng:{" "}
            <b className="text-blue-700">
              {data.BankAccount} ({data.BankName})
            </b>
          </li>,
        );

      if (data.IdentityFrontUrl) {
        items.push(
          <li key="f_front">
            <a
              href={`${BACKEND_URL}${data.IdentityFrontUrl}`}
              target="_blank"
              rel="noreferrer"
              className="text-blue-500 hover:underline"
            >
              📎 Xem CCCD Mặt trước
            </a>
          </li>,
        );
      }
      if (data.IdentityBackUrl) {
        items.push(
          <li key="f_back">
            <a
              href={`${BACKEND_URL}${data.IdentityBackUrl}`}
              target="_blank"
              rel="noreferrer"
              className="text-blue-500 hover:underline"
            >
              📎 Xem CCCD Mặt sau
            </a>
          </li>,
        );
      }
      if (data.CertificateUrl) {
        items.push(
          <li key="f_cert">
            <a
              href={`${BACKEND_URL}${data.CertificateUrl}`}
              target="_blank"
              rel="noreferrer"
              className="text-blue-500 hover:underline"
            >
              📎 Xem Bằng cấp/Chứng chỉ
            </a>
          </li>,
        );
      }

      return (
        <ul className="list-disc pl-5 text-sm text-gray-700 space-y-1">
          {items}
        </ul>
      );
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
    } catch (e) {
      return <span className="text-red-500 text-sm">Lỗi giải mã dữ liệu</span>;
    }
  };

  if (loading)
    return (
      <div className="p-8 text-center text-gray-500">
        Đang tải danh sách chờ duyệt...
      </div>
    );

  return (
    <div className="min-h-full bg-gray-50 px-4 py-6 sm:px-6">
      <div className="mx-auto max-w-6xl rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
        <h2 className="text-2xl font-bold text-gray-800 mb-6 border-b pb-3">
          Phê duyệt cập nhật hồ sơ (Dành cho HR)
        </h2>

        {requests.length === 0 ? (
          <div className="text-center py-10 bg-gray-50 rounded border border-dashed">
            <p className="text-gray-500">
              Tuyệt vời! Hiện không có hồ sơ nào bị tồn đọng cần duyệt.
            </p>
          </div>
        ) : (
          <div className="space-y-4">
            {requests.map((req) => (
              <div
                key={req.id}
                className="border border-gray-200 rounded-lg p-5 flex flex-col md:flex-row justify-between gap-4 hover:shadow-md transition bg-white"
              >
                {/* Thông tin */}
                <div className="md:w-1/3 border-b md:border-b-0 md:border-r border-gray-100 pr-4 pb-4 md:pb-0">
                  <h3 className="font-bold text-gray-800">
                    {req.employeeName}
                  </h3>
                  <p className="text-sm text-gray-500 mt-1">
                    Mã NV: {req.employeeCode}
                  </p>
                  <div className="mt-2 inline-block px-2 py-1 bg-yellow-50 text-yellow-700 border border-yellow-200 text-xs rounded font-medium">
                    Hạn xử lý SLA:{" "}
                    {new Date(req.deadlineSLA).toLocaleString("vi-VN")}
                  </div>
                </div>

                {/* Nội dung thay đổi */}
                <div className="md:w-1/2">
                  <h4 className="text-sm font-semibold text-gray-700 mb-2 uppercase">
                    Nội dung đề xuất:
                  </h4>
                  <div className="bg-gray-50 p-3 rounded border border-gray-100">
                    {renderRequestedChanges(req.requestedDataJson)}
                  </div>
                </div>

                {/* Các nút Action - Bố cục thông minh chống Spam */}
                <div className="md:w-1/6 flex flex-col justify-center gap-2">
                  {rejectingId === req.id ? (
                    /* CHẾ ĐỘ NHẬP LÝ DO TỪ CHỐI (Inline) */
                    <div className="flex flex-col gap-2 animate-fade-in">
                      <input
                        type="text"
                        autoFocus
                        placeholder="Lý do từ chối..."
                        value={rejectReason}
                        onChange={(e) => setRejectReason(e.target.value)}
                        className="w-full border border-red-300 p-2 rounded text-sm outline-none focus:ring-1 focus:ring-red-400"
                      />
                      <div className="flex gap-1">
                        <button
                          onClick={() => handleConfirmReject(req.id)}
                          disabled={processingId === req.id}
                          className="flex-1 bg-red-600 text-white py-1.5 rounded text-sm font-medium hover:bg-red-700 disabled:opacity-50"
                        >
                          Chốt
                        </button>
                        <button
                          onClick={() => {
                            setRejectingId(null);
                            setRejectReason("");
                          }}
                          disabled={processingId === req.id}
                          className="flex-1 bg-gray-200 text-gray-700 py-1.5 rounded text-sm font-medium hover:bg-gray-300 disabled:opacity-50"
                        >
                          Hủy
                        </button>
                      </div>
                    </div>
                  ) : (
                    /* CHẾ ĐỘ NÚT BẤM BÌNH THƯỜNG */
                    <>
                      <button
                        onClick={() => handleApprove(req.id)}
                        disabled={
                          processingId === req.id || rejectingId !== null
                        }
                        className="w-full bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-4 rounded text-sm transition-colors shadow-sm disabled:opacity-50"
                      >
                        {processingId === req.id
                          ? "Đang xử lý..."
                          : "✓ Phê duyệt"}
                      </button>
                      <button
                        onClick={() => setRejectingId(req.id)}
                        disabled={
                          processingId === req.id || rejectingId !== null
                        }
                        className="w-full bg-red-50 hover:bg-red-100 text-red-600 font-medium py-2 px-4 rounded text-sm transition-colors border border-red-200 disabled:opacity-50"
                      >
                        ✕ Từ chối
                      </button>
                    </>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
