import React, { useState, useEffect } from "react";
import { hrProfileApi } from "../api/hrProfileApi";
import { dependentApi } from "../api/dependentApi";
import type { PendingProfileRequest } from "../types/profileRequest";
import type { PendingDependentRequest } from "../types/dependent";
import { BACKEND_URL } from "../../../core/api/config";
import { useNotification } from "../../../core/context/NotificationContext";

export const HRProfileReviewList: React.FC = () => {
  const [requests, setRequests] = useState<PendingProfileRequest[]>([]);
  const [dependentRequests, setDependentRequests] = useState<
    PendingDependentRequest[]
  >([]);
  const [loading, setLoading] = useState(false);
  const [processingId, setProcessingId] = useState<number | null>(null);
  const { triggerAlert } = useNotification();

  // States quản lý Input Từ chối ngay trên UI (Thay cho window.prompt)
  const [rejectingId, setRejectingId] = useState<number | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [dependentRejectingId, setDependentRejectingId] = useState<number | null>(null);
  const [dependentRejectReason, setDependentRejectReason] = useState("");

  useEffect(() => {
    const fetchRequests = async () => {
      setLoading(true);
      try {
        const [profileRes, dependentRes] = await Promise.all([
          hrProfileApi.getPendingRequests(),
          dependentApi.getPendingRequests(),
        ]);
        setRequests(profileRes.data || profileRes || []);
        setDependentRequests(dependentRes.data || dependentRes || []);
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

  const executeDependentReview = async (
    id: number,
    isApproved: boolean,
    reason?: string,
  ) => {
    setProcessingId(id);
    try {
      const response = await dependentApi.reviewRequest(id, {
        isApproved,
        rejectReason: reason,
      });
      triggerAlert(
        "success",
        "Thành công",
        response.message || "Đã xử lý yêu cầu người phụ thuộc.",
      );
      setDependentRequests((prev) => prev.filter((r) => r.id !== id));
      setDependentRejectingId(null);
      setDependentRejectReason("");
    } catch (error: unknown) {
      const msg =
        error instanceof Error
          ? error.message
          : "Không thể xử lý yêu cầu người phụ thuộc.";
      triggerAlert("error", "Lỗi xử lý", msg);
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

  const renderDependentChanges = (jsonString: string, evidenceUrl?: string | null) => {
    try {
      const data = JSON.parse(jsonString);
      return (
        <ul className="list-disc pl-5 text-sm text-gray-700 space-y-1">
          <li>
            Họ tên: <b className="text-blue-700">{data.FullName}</b>
          </li>
          <li>Quan hệ: {["Con", "Cha/Mẹ", "Vợ/Chồng", "Khác"][data.Relationship] ?? "Khác"}</li>
          {data.IdNumber && <li>CCCD: {data.IdNumber}</li>}
          {data.TaxDependentCode && <li>MST phụ thuộc: {data.TaxDependentCode}</li>}
          {data.ValidFrom && (
            <li>
              Hiệu lực từ: {new Date(data.ValidFrom).toLocaleDateString("vi-VN")}
            </li>
          )}
          {data.ValidTo && (
            <li>Hiệu lực đến: {new Date(data.ValidTo).toLocaleDateString("vi-VN")}</li>
          )}
          {data.Note && <li>Ghi chú: {data.Note}</li>}
          {evidenceUrl && (
            <li>
              <a
                href={`${BACKEND_URL}${evidenceUrl}`}
                target="_blank"
                rel="noreferrer"
                className="text-blue-500 hover:underline"
              >
                Xem minh chứng
              </a>
            </li>
          )}
        </ul>
      );
    } catch {
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

        <div className="mt-8 border-t pt-6">
          <h3 className="mb-4 text-lg font-bold text-gray-800">
            Yêu cầu người phụ thuộc
          </h3>
          {dependentRequests.length === 0 ? (
            <div className="rounded border border-dashed bg-gray-50 py-6 text-center text-gray-500">
              Không có yêu cầu người phụ thuộc đang chờ duyệt.
            </div>
          ) : (
            <div className="space-y-4">
              {dependentRequests.map((req) => (
                <div
                  key={req.id}
                  className="flex flex-col gap-4 rounded-lg border border-gray-200 bg-white p-5 transition hover:shadow-md md:flex-row md:justify-between"
                >
                  <div className="border-b border-gray-100 pb-4 md:w-1/3 md:border-b-0 md:border-r md:pb-0 md:pr-4">
                    <h4 className="font-bold text-gray-800">
                      {req.employeeName}
                    </h4>
                    <p className="mt-1 text-sm text-gray-500">
                      Mã NV: {req.employeeCode}
                    </p>
                    <span className="mt-2 inline-block rounded border border-blue-200 bg-blue-50 px-2 py-1 text-xs font-medium text-blue-700">
                      {req.actionType === "CREATE"
                        ? "Thêm mới"
                        : req.actionType === "UPDATE"
                          ? "Cập nhật"
                          : "Ngừng hiệu lực"}
                    </span>
                  </div>

                  <div className="md:w-1/2">
                    <h4 className="mb-2 text-sm font-semibold uppercase text-gray-700">
                      Nội dung đề xuất:
                    </h4>
                    <div className="rounded border border-gray-100 bg-gray-50 p-3">
                      {renderDependentChanges(
                        req.requestedDataJson,
                        req.evidenceUrl,
                      )}
                    </div>
                  </div>

                  <div className="flex flex-col justify-center gap-2 md:w-1/6">
                    {dependentRejectingId === req.id ? (
                      <div className="flex flex-col gap-2">
                        <input
                          type="text"
                          autoFocus
                          placeholder="Lý do từ chối..."
                          value={dependentRejectReason}
                          onChange={(e) =>
                            setDependentRejectReason(e.target.value)
                          }
                          className="w-full rounded border border-red-300 p-2 text-sm outline-none focus:ring-1 focus:ring-red-400"
                        />
                        <div className="flex gap-1">
                          <button
                            onClick={() =>
                              executeDependentReview(
                                req.id,
                                false,
                                dependentRejectReason.trim(),
                              )
                            }
                            disabled={
                              processingId === req.id ||
                              !dependentRejectReason.trim()
                            }
                            className="flex-1 rounded bg-red-600 py-1.5 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
                          >
                            Chốt
                          </button>
                          <button
                            onClick={() => {
                              setDependentRejectingId(null);
                              setDependentRejectReason("");
                            }}
                            disabled={processingId === req.id}
                            className="flex-1 rounded bg-gray-200 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-300 disabled:opacity-50"
                          >
                            Hủy
                          </button>
                        </div>
                      </div>
                    ) : (
                      <>
                        <button
                          onClick={() =>
                            executeDependentReview(req.id, true)
                          }
                          disabled={
                            processingId === req.id ||
                            dependentRejectingId !== null
                          }
                          className="w-full rounded bg-green-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-green-700 disabled:opacity-50"
                        >
                          {processingId === req.id
                            ? "Đang xử lý..."
                            : "Phê duyệt"}
                        </button>
                        <button
                          onClick={() => setDependentRejectingId(req.id)}
                          disabled={
                            processingId === req.id ||
                            dependentRejectingId !== null
                          }
                          className="w-full rounded border border-red-200 bg-red-50 px-4 py-2 text-sm font-medium text-red-600 transition-colors hover:bg-red-100 disabled:opacity-50"
                        >
                          Từ chối
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
    </div>
  );
};
