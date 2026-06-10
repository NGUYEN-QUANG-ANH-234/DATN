import { useCallback, useEffect, useRef, useState } from "react";
import { Check, RefreshCw, X } from "lucide-react";
import { contractApi } from "../api/contractApi";
import type { ContractDto } from "../api/contractApi";
import { useNotification } from "../../../core/context/NotificationContext";
import {
  dangerButtonClass,
  EmptyState,
  FeatureCard,
  FeaturePage,
  primaryButtonClass,
  secondaryButtonClass,
  textareaClass,
} from "../../../core/components/FeatureShell";

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

export const DirectorContractApproval = () => {
  const [contracts, setContracts] = useState<ContractDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [rejectTarget, setRejectTarget] = useState<ContractDto | null>(null);
  const [rejectReason, setRejectReason] = useState("");

  const { triggerAlert } = useNotification();
  const alertRef = useRef(triggerAlert);

  useEffect(() => {
    alertRef.current = triggerAlert;
  }, [triggerAlert]);

  const fetchContracts = useCallback(async () => {
    setLoading(true);
    try {
      const res = await contractApi.getDirectorPending();
      const raw = res as unknown as { data?: ContractDto[]; Data?: ContractDto[] };
      setContracts(raw.data || raw.Data || []);
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

  const handleApprove = (id: number) => {
    alertRef.current(
      "confirm",
      "Phê duyệt hợp đồng",
      "Sau khi phê duyệt, hợp đồng sẽ có hiệu lực và hệ thống ghi nhận lịch sử lương cho nhân viên.",
      async () => {
        try {
          await contractApi.directorApprove(id, { isApproved: true });
          alertRef.current("success", "Đã phê duyệt", "Hợp đồng đã chính thức có hiệu lực.");
          fetchContracts();
        } catch (err: unknown) {
          const e = err as { message?: string };
          alertRef.current("error", "Lỗi", e?.message || "Không thể phê duyệt hợp đồng.");
        }
      },
    );
  };

  const handleReject = async () => {
    if (!rejectTarget) return;
    if (!rejectReason.trim()) {
      alertRef.current("warning", "Thiếu lý do", "Vui lòng nhập lý do yêu cầu chỉnh sửa.");
      return;
    }

    try {
      await contractApi.requestRevision(rejectTarget.id, {
        reason: rejectReason.trim(),
      });
      alertRef.current("success", "Đã gửi yêu cầu chỉnh sửa", "Hợp đồng đã được chuyển về HR chỉnh sửa.");
      setRejectTarget(null);
      setRejectReason("");
      fetchContracts();
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Không thể xử lý", e?.message || "Vui lòng thử lại.");
    }
  };

  return (
    <FeaturePage
      title="Phê duyệt hợp đồng"
      description="Danh sách hợp đồng đã được nhân viên đồng ý điều khoản và đang chờ Giám đốc phê duyệt cuối cùng."
      actions={
        <button className={secondaryButtonClass} onClick={fetchContracts} disabled={loading}>
          <RefreshCw size={16} />
          Làm mới
        </button>
      }
      width="normal"
    >
      {loading ? (
        <FeatureCard>
          <div className="py-10 text-center text-sm text-gray-500">Đang tải dữ liệu...</div>
        </FeatureCard>
      ) : contracts.length === 0 ? (
        <FeatureCard>
          <EmptyState title="Chưa có hợp đồng chờ phê duyệt" description="Các hợp đồng mới sẽ xuất hiện sau khi nhân viên đồng ý bản nháp." />
        </FeatureCard>
      ) : (
        <div className="space-y-3">
          {contracts.map(contract => (
            <FeatureCard key={contract.id} className="p-4">
              <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <span className="inline-flex rounded-md border border-purple-200 bg-purple-50 px-2.5 py-1 text-xs font-semibold text-purple-700">
                    Chờ phê duyệt
                  </span>
                  <h2 className="mt-2 text-base font-semibold text-gray-900">
                    {contract.contractNumber || `Hợp đồng #${contract.id}`}
                  </h2>
                  <p className="mt-1 text-sm text-gray-500">
                    {contract.employeeName || "Chưa có tên nhân viên"} · {CONTRACT_TYPE_LABELS[contract.contractType] ?? contract.contractType}
                  </p>
                </div>

                <div className="flex flex-wrap gap-2">
                  <button
                    className={dangerButtonClass}
                    onClick={() => {
                      setRejectTarget(contract);
                      setRejectReason("");
                    }}
                  >
                    <X size={16} />
                    Yêu cầu chỉnh sửa
                  </button>
                  <button className={primaryButtonClass} onClick={() => handleApprove(contract.id)}>
                    <Check size={16} />
                    Phê duyệt
                  </button>
                </div>
              </div>

              <div className="mt-4 grid gap-3 border-t border-gray-100 pt-4 text-sm sm:grid-cols-3">
                <div>
                  <p className="text-xs font-medium text-gray-500">Lương cơ bản</p>
                  <p className="mt-1 font-semibold text-gray-900">
                    {fmt(contract.basicSalary)} <span className="text-xs font-medium text-gray-500">({contract.salaryPercentage || 0}%)</span>
                  </p>
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
          ))}
        </div>
      )}

      {rejectTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-2xl">
            <h2 className="text-lg font-semibold text-gray-900">Yêu cầu chỉnh sửa hợp đồng</h2>
            <p className="mt-1 text-sm text-gray-500">Nội dung này sẽ được chuyển về HR để cập nhật bản nháp.</p>
            <label className="mt-4 block">
              <span className="mb-1 block text-sm font-medium text-gray-700">Lý do yêu cầu chỉnh sửa</span>
              <textarea
                className={textareaClass}
                value={rejectReason}
                onChange={e => setRejectReason(e.target.value)}
                placeholder="Nhập nội dung cần HR chỉnh sửa..."
              />
            </label>

            <div className="mt-5 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <button className={secondaryButtonClass} onClick={() => setRejectTarget(null)}>
                Hủy
              </button>
              <button className={dangerButtonClass} onClick={handleReject}>
                <X size={16} />
                Gửi yêu cầu chỉnh sửa
              </button>
            </div>
          </div>
        </div>
      )}
    </FeaturePage>
  );
};
