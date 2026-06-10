import { useCallback, useEffect, useRef, useState } from "react";
import { Check, MessageSquareText, Plus, RefreshCw, Send, X } from "lucide-react";
import { contractApi } from "../api/contractApi";
import type { ContractDto } from "../api/contractApi";
import { contractAddendumApi } from "../api/contractAddendumApi";
import type { ContractAddendumDto } from "../api/contractAddendumApi";
import { useNotification } from "../../../core/context/NotificationContext";
import {
  EmptyState,
  FeatureCard,
  FeaturePage,
  primaryButtonClass,
  secondaryButtonClass,
  textareaClass,
} from "../../../core/components/FeatureShell";

const STATUS_MAP: Record<string, { label: string; cls: string }> = {
  PendingDept: { label: "Chờ Trưởng phòng", cls: "bg-blue-50 text-blue-700 border-blue-200" },
  PendingHR: { label: "Chờ HR soạn thảo", cls: "bg-indigo-50 text-indigo-700 border-indigo-200" },
  PendingManagerContentReview: { label: "Chờ Trưởng phòng duyệt nội dung", cls: "bg-blue-50 text-blue-700 border-blue-200" },
  PendingEmployee: { label: "Chờ người lao động xác nhận", cls: "bg-amber-50 text-amber-700 border-amber-200" },
  PendingHRRevision: { label: "Chờ HR chỉnh sửa", cls: "bg-amber-50 text-amber-700 border-amber-200" },
  Draft: { label: "Chờ nhân viên xác nhận", cls: "bg-amber-50 text-amber-700 border-amber-200" },
  Negotiating: { label: "Đang thương lượng", cls: "bg-orange-50 text-orange-700 border-orange-200" },
  PendingDirector: { label: "Chờ Giám đốc duyệt", cls: "bg-purple-50 text-purple-700 border-purple-200" },
  ApprovedByDirector: { label: "Đã duyệt, chờ phát hành", cls: "bg-teal-50 text-teal-700 border-teal-200" },
  Active: { label: "Có hiệu lực", cls: "bg-green-50 text-green-700 border-green-200" },
  Rejected: { label: "Bị từ chối", cls: "bg-red-50 text-red-700 border-red-200" },
  Draft_Cancelled: { label: "Hết hạn xác nhận", cls: "bg-gray-100 text-gray-600 border-gray-200" },
};

const CONTRACT_TYPE_LABELS: Record<string, string> = {
  Probation: "Thử việc",
  FixedTerm: "Có thời hạn",
  Definite: "Có thời hạn",
  Indefinite: "Không thời hạn",
  PartTime: "Bán thời gian",
};

const fmt = (v: number) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(v || 0);

const dateText = (value?: string | null) =>
  value ? new Date(value).toLocaleDateString("vi-VN") : "Chưa thiết lập";

export const ContractManagement = () => {
  const [contracts, setContracts] = useState<ContractDto[]>([]);
  const [pendingAddendums, setPendingAddendums] = useState<ContractAddendumDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [showRequest, setShowRequest] = useState(false);
  const [requestNote, setRequestNote] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [negotiateTarget, setNegotiateTarget] = useState<ContractDto | null>(null);
  const [negotiateNote, setNegotiateNote] = useState("");

  const { triggerAlert } = useNotification();
  const alertRef = useRef(triggerAlert);

  useEffect(() => {
    alertRef.current = triggerAlert;
  }, [triggerAlert]);

  const fetchContracts = useCallback(async () => {
    setLoading(true);
    try {
      const [res, addendumRes] = await Promise.all([
        contractApi.getMyContracts(),
        contractAddendumApi.getMyPendingConfirmation(),
      ]);
      const raw = res as unknown as { data?: ContractDto[]; Data?: ContractDto[] };
      const addendumRaw = addendumRes as unknown as { data?: ContractAddendumDto[]; Data?: ContractAddendumDto[] };
      setContracts(raw.data || raw.Data || []);
      setPendingAddendums(addendumRaw.data || addendumRaw.Data || []);
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Không thể tải dữ liệu", e?.message || "Vui lòng thử lại.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchContracts();
  }, [fetchContracts]);

  const handleSendRequest = async () => {
    setSubmitting(true);
    try {
      await contractApi.createRequest({ note: requestNote.trim() || undefined });
      alertRef.current("success", "Đã gửi yêu cầu", "Yêu cầu ký kết/gia hạn hợp đồng đã được gửi tới Trưởng phòng.");
      setShowRequest(false);
      setRequestNote("");
      fetchContracts();
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Lỗi", e?.message || "Không thể gửi yêu cầu.");
    } finally {
      setSubmitting(false);
    }
  };

  const handleAddendumConfirm = (addendum: ContractAddendumDto, isApproved: boolean) => {
    alertRef.current(
      "confirm",
      isApproved ? "Xác nhận phụ lục" : "Yêu cầu chỉnh sửa phụ lục",
      isApproved
        ? "Bạn đồng ý với toàn bộ điều khoản của phụ lục hợp đồng này?"
        : "Bạn muốn gửi yêu cầu chỉnh sửa phụ lục này về HR?",
      async () => {
        try {
          if (isApproved) {
            await contractAddendumApi.employeeConfirm(addendum.id, { isApproved: true });
          } else {
            await contractAddendumApi.requestRevision(addendum.id, {
              reason: "Người lao động yêu cầu HR chỉnh sửa điều khoản phụ lục.",
            });
          }
          alertRef.current(
            "success",
            isApproved ? "Đã xác nhận phụ lục" : "Đã gửi yêu cầu chỉnh sửa",
            isApproved
              ? "Phụ lục đã được chuyển sang Giám đốc phê duyệt cuối."
              : "Phụ lục đã được chuyển về HR chỉnh sửa.",
          );
          fetchContracts();
        } catch (err: unknown) {
          const e = err as { message?: string };
          alertRef.current("error", "Lỗi", e?.message || "Không thể xử lý phụ lục.");
        }
      },
    );
  };

  const handleAccept = (id: number) => {
    alertRef.current(
      "confirm",
      "Xác nhận điều khoản",
      "Bạn đồng ý toàn bộ điều khoản của bản nháp hợp đồng này?",
      async () => {
        try {
          await contractApi.employeeAccept(id);
          alertRef.current("success", "Đã xác nhận", "Hợp đồng đã được chuyển sang Giám đốc phê duyệt.");
          fetchContracts();
        } catch (err: unknown) {
          const e = err as { message?: string };
          alertRef.current("error", "Lỗi", e?.message || "Không thể xác nhận hợp đồng.");
        }
      },
    );
  };

  const handleNegotiate = async () => {
    if (!negotiateTarget) return;
    if (!negotiateNote.trim()) {
      alertRef.current("warning", "Thiếu nội dung", "Vui lòng nhập nội dung thương lượng.");
      return;
    }

    try {
      await contractApi.negotiate(negotiateTarget.id, { negotiationNote: negotiateNote.trim() });
      alertRef.current("success", "Đã gửi phản hồi", "Ý kiến thương lượng đã được chuyển tới HR.");
      setNegotiateTarget(null);
      setNegotiateNote("");
      fetchContracts();
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Lỗi", e?.message || "Không thể gửi thương lượng.");
    }
  };

  const getStatus = (status: string) =>
    STATUS_MAP[status] ?? { label: status, cls: "bg-gray-100 text-gray-600 border-gray-200" };

  return (
    <FeaturePage
      title="Hợp đồng lao động"
      description="Theo dõi yêu cầu, xem bản nháp và phản hồi điều khoản hợp đồng của bạn."
      actions={
        <div className="flex flex-wrap gap-2">
          <button className={secondaryButtonClass} onClick={fetchContracts} disabled={loading}>
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button className={primaryButtonClass} onClick={() => setShowRequest(true)}>
            <Plus size={16} />
            Gửi yêu cầu
          </button>
        </div>
      }
      width="normal"
    >
      {pendingAddendums.length > 0 && (
        <div className="mb-4 space-y-3">
          {pendingAddendums.map(addendum => (
            <FeatureCard key={addendum.id} className="border-amber-200 bg-amber-50 p-4">
              <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <span className="inline-flex rounded-md border border-amber-200 bg-white px-2.5 py-1 text-xs font-semibold text-amber-700">
                    Chức năng sửa đổi: Chờ người lao động xác nhận phụ lục
                  </span>
                  <h2 className="mt-2 text-base font-semibold text-gray-900">{addendum.addendumNumber}</h2>
                  <p className="mt-1 text-sm text-gray-600">
                    HĐ gốc: {addendum.contractNumber || `#${addendum.contractId}`} · Hiệu lực: {dateText(addendum.effectiveDate)}
                  </p>
                  {addendum.content && <p className="mt-2 text-sm text-gray-700">{addendum.content}</p>}
                </div>
                <div className="flex flex-wrap gap-2">
                  <button className={secondaryButtonClass} onClick={() => handleAddendumConfirm(addendum, false)}>
                    <X size={16} />
                    Yêu cầu chỉnh sửa
                  </button>
                  <button className={primaryButtonClass} onClick={() => handleAddendumConfirm(addendum, true)}>
                    <Check size={16} />
                    Đồng ý phụ lục
                  </button>
                </div>
              </div>
            </FeatureCard>
          ))}
        </div>
      )}

      {loading ? (
        <FeatureCard>
          <div className="py-10 text-center text-sm text-gray-500">Đang tải dữ liệu...</div>
        </FeatureCard>
      ) : contracts.length === 0 ? (
        <FeatureCard>
          <EmptyState title="Bạn chưa có hợp đồng nào" description="Gửi yêu cầu mới khi cần ký kết hoặc gia hạn hợp đồng." />
        </FeatureCard>
      ) : (
        <div className="space-y-3">
          {contracts.map(contract => {
            const status = getStatus(contract.status);
            return (
              <FeatureCard key={contract.id} className="p-4">
                <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                  <div>
                    <span className={`inline-flex rounded-md border px-2.5 py-1 text-xs font-semibold ${status.cls}`}>
                      {status.label}
                    </span>
                    <h2 className="mt-2 text-base font-semibold text-gray-900">
                      {contract.contractNumber || `Hợp đồng #${contract.id}`}
                    </h2>
                    <p className="mt-1 text-sm text-gray-500">
                      {(CONTRACT_TYPE_LABELS[contract.contractType] ?? contract.contractType) || "Chưa chọn loại"} · Phiên bản v{contract.version || 0}
                    </p>
                  </div>

                  {contract.status === "Draft" && (
                    <div className="flex flex-wrap gap-2">
                      <button
                        className={secondaryButtonClass}
                        onClick={() => {
                          setNegotiateTarget(contract);
                          setNegotiateNote("");
                        }}
                      >
                        <MessageSquareText size={16} />
                        Yêu cầu điều chỉnh
                      </button>
                      <button className={primaryButtonClass} onClick={() => handleAccept(contract.id)}>
                        <Check size={16} />
                        Đồng ý điều khoản
                      </button>
                    </div>
                  )}
                </div>

                <div className="mt-4 grid gap-3 border-t border-gray-100 pt-4 text-sm sm:grid-cols-3">
                  <div>
                    <p className="text-xs font-medium text-gray-500">Lương cơ bản</p>
                    <p className="mt-1 font-semibold text-gray-900">{fmt(contract.basicSalary)}</p>
                  </div>
                  <div>
                    <p className="text-xs font-medium text-gray-500">Lương đóng BHXH</p>
                    <p className="mt-1 font-semibold text-gray-900">{fmt(contract.insuranceSalary)}</p>
                  </div>
                  <div>
                    <p className="text-xs font-medium text-gray-500">Thời hạn</p>
                    <p className="mt-1 font-semibold text-gray-900">
                      {dateText(contract.startDate)} - {contract.endDate ? dateText(contract.endDate) : "Không thời hạn"}
                    </p>
                  </div>
                </div>

                {contract.negotiationNote && (
                  <div className="mt-4 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800">
                    <span className="font-semibold">Ghi chú: </span>
                    {contract.negotiationNote}
                  </div>
                )}
              </FeatureCard>
            );
          })}
        </div>
      )}

      {showRequest && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-2xl">
            <h2 className="text-lg font-semibold text-gray-900">Gửi yêu cầu hợp đồng</h2>
            <p className="mt-1 text-sm text-gray-500">Bạn có thể ghi chú mong muốn ký mới, gia hạn hoặc điều chỉnh điều khoản.</p>
            <label className="mt-4 block">
              <span className="mb-1 block text-sm font-medium text-gray-700">Ghi chú</span>
              <textarea
                className={textareaClass}
                value={requestNote}
                onChange={e => setRequestNote(e.target.value)}
                placeholder="Nhập đề xuất của bạn..."
              />
            </label>

            <div className="mt-5 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <button
                className={secondaryButtonClass}
                onClick={() => {
                  setShowRequest(false);
                  setRequestNote("");
                }}
              >
                Hủy
              </button>
              <button className={primaryButtonClass} onClick={handleSendRequest} disabled={submitting}>
                <Send size={16} />
                {submitting ? "Đang gửi..." : "Gửi yêu cầu"}
              </button>
            </div>
          </div>
        </div>
      )}

      {negotiateTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-2xl">
            <h2 className="text-lg font-semibold text-gray-900">Yêu cầu điều chỉnh</h2>
            <p className="mt-1 text-sm text-gray-500">HR sẽ nhận nội dung này và gửi lại bản nháp mới nếu điều chỉnh được chấp thuận.</p>
            <label className="mt-4 block">
              <span className="mb-1 block text-sm font-medium text-gray-700">Nội dung thương lượng</span>
              <textarea
                className={textareaClass}
                value={negotiateNote}
                onChange={e => setNegotiateNote(e.target.value)}
                placeholder="Nhập nội dung cần điều chỉnh..."
              />
            </label>

            <div className="mt-5 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <button
                className={secondaryButtonClass}
                onClick={() => {
                  setNegotiateTarget(null);
                  setNegotiateNote("");
                }}
              >
                Hủy
              </button>
              <button className={primaryButtonClass} onClick={handleNegotiate}>
                <Send size={16} />
                Gửi phản hồi
              </button>
            </div>
          </div>
        </div>
      )}
    </FeaturePage>
  );
};
