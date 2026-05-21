import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Check, FilePenLine, FileText, RefreshCw, X } from "lucide-react";
import { contractApi } from "../api/contractApi";
import type { ContractDto, ReviewContractPayload, CreateDraftPayload } from "../api/contractApi";
import { useNotification } from "../../../core/context/NotificationContext";
import {
  dangerButtonClass,
  EmptyState,
  FeatureCard,
  FeaturePage,
  fieldClass,
  primaryButtonClass,
  secondaryButtonClass,
  textareaClass,
} from "../../../core/components/FeatureShell";

const CONTRACT_TYPES = ["Probation", "FixedTerm", "Indefinite", "PartTime"];

const CONTRACT_TYPE_LABELS: Record<string, string> = {
  Probation: "Thử việc",
  FixedTerm: "Có thời hạn",
  Definite: "Có thời hạn",
  Indefinite: "Không thời hạn",
  PartTime: "Bán thời gian",
};

const STATUS_LABELS: Record<string, string> = {
  PendingDept: "Chờ Trưởng phòng",
  PendingHR: "Chờ HR soạn thảo",
  Negotiating: "Đang thương lượng",
  Draft: "Chờ nhân viên xác nhận",
  PendingDirector: "Chờ Giám đốc duyệt",
  Active: "Có hiệu lực",
  Rejected: "Bị từ chối",
  Draft_Cancelled: "Hết hạn xác nhận",
};

type TabKey = "pending-dept" | "pending-hr" | "all";

type DraftForm = {
  contractType: string;
  basicSalary: string;
  salaryPercentage: string;
  insuranceSalary: string;
  startDate: string;
  endDate: string;
};

const defaultDraft: DraftForm = {
  contractType: "FixedTerm",
  basicSalary: "",
  salaryPercentage: "100",
  insuranceSalary: "",
  startDate: "",
  endDate: "",
};

const tabs: { key: TabKey; label: string }[] = [
  { key: "pending-dept", label: "Chờ Trưởng phòng" },
  { key: "pending-hr", label: "Chờ HR soạn thảo" },
  { key: "all", label: "Tất cả hợp đồng" },
];

const fmt = (v: number) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(v || 0);

const dateText = (value?: string | null) =>
  value ? new Date(value).toLocaleDateString("vi-VN") : "Chưa thiết lập";

const statusClass = (status: string) => {
  if (status === "Rejected" || status === "Draft_Cancelled") return "bg-red-50 text-red-700 border-red-200";
  if (status === "Active") return "bg-green-50 text-green-700 border-green-200";
  if (status === "Negotiating") return "bg-amber-50 text-amber-700 border-amber-200";
  if (status === "PendingDirector") return "bg-purple-50 text-purple-700 border-purple-200";
  return "bg-blue-50 text-blue-700 border-blue-200";
};

export const HRContractManagement = () => {
  const [tab, setTab] = useState<TabKey>("pending-dept");
  const [contracts, setContracts] = useState<ContractDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [draftTarget, setDraftTarget] = useState<ContractDto | null>(null);
  const [draft, setDraft] = useState<DraftForm>(defaultDraft);
  const [submittingDraft, setSubmittingDraft] = useState(false);
  const [rejectTarget, setRejectTarget] = useState<ContractDto | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [rejectCtx, setRejectCtx] = useState<"dept" | "hr">("hr");

  const { triggerAlert } = useNotification();
  const alertRef = useRef(triggerAlert);

  useEffect(() => {
    alertRef.current = triggerAlert;
  }, [triggerAlert]);

  const fetchContracts = useCallback(async () => {
    setLoading(true);
    try {
      const res =
        tab === "pending-dept"
          ? await contractApi.getPendingRequests()
          : tab === "pending-hr"
            ? await contractApi.getHrPendingRequests()
            : await contractApi.getAllContracts();

      const raw = res as unknown as { data?: ContractDto[]; Data?: ContractDto[] };
      setContracts(raw.data || raw.Data || []);
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Lỗi tải dữ liệu", e?.message || "Không thể tải danh sách hợp đồng.");
    } finally {
      setLoading(false);
    }
  }, [tab]);

  useEffect(() => {
    fetchContracts();
  }, [fetchContracts]);

  const counts = useMemo(
    () => ({
      pendingDept: contracts.filter(c => c.status === "PendingDept").length,
      pendingHR: contracts.filter(c => c.status === "PendingHR" || c.status === "Negotiating").length,
      all: contracts.length,
    }),
    [contracts],
  );

  const openDraft = (contract: ContractDto) => {
    setDraftTarget(contract);
    setDraft({
      contractType: contract.contractType || defaultDraft.contractType,
      basicSalary: contract.basicSalary ? String(contract.basicSalary) : "",
      salaryPercentage: contract.salaryPercentage ? String(contract.salaryPercentage) : "100",
      insuranceSalary: contract.insuranceSalary ? String(contract.insuranceSalary) : "",
      startDate: contract.startDate ? contract.startDate.slice(0, 10) : "",
      endDate: contract.endDate ? contract.endDate.slice(0, 10) : "",
    });
  };

  const validateDraft = () => {
    const basicSalary = Number(draft.basicSalary);
    const salaryPercentage = Number(draft.salaryPercentage || 100);
    const insuranceSalary = Number(draft.insuranceSalary || 0);

    if (!basicSalary || basicSalary <= 0 || !draft.startDate) {
      alertRef.current("warning", "Thiếu thông tin", "Vui lòng nhập lương cơ bản hợp lệ và ngày bắt đầu.");
      return null;
    }

    if (salaryPercentage <= 0 || salaryPercentage > 100) {
      alertRef.current("warning", "Tỷ lệ chưa hợp lệ", "Tỷ lệ thực lĩnh phải nằm trong khoảng 1-100%.");
      return null;
    }

    if (insuranceSalary < 0) {
      alertRef.current("warning", "Lương BHXH chưa hợp lệ", "Lương đóng BHXH không được âm.");
      return null;
    }

    if ((draft.contractType === "Probation" || draft.contractType === "FixedTerm") && !draft.endDate) {
      alertRef.current("warning", "Thiếu ngày kết thúc", "Hợp đồng thử việc/có thời hạn cần có ngày kết thúc.");
      return null;
    }

    if (draft.endDate && new Date(draft.endDate) < new Date(draft.startDate)) {
      alertRef.current("warning", "Thời hạn chưa hợp lệ", "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
      return null;
    }

    return {
      contractType: draft.contractType,
      basicSalary,
      salaryPercentage,
      insuranceSalary,
      startDate: draft.startDate,
      endDate: draft.endDate || undefined,
    } satisfies CreateDraftPayload;
  };

  const handleCreateDraft = async () => {
    if (!draftTarget) return;
    const payload = validateDraft();
    if (!payload) return;

    setSubmittingDraft(true);
    try {
      await contractApi.hrCreateDraft(draftTarget.id, payload);
      alertRef.current(
        "success",
        "Đã lưu bản nháp",
        draftTarget.status === "Negotiating"
          ? "Bản nháp mới đã được gửi lại cho nhân viên."
          : "Bản nháp đã được gửi cho nhân viên xác nhận.",
      );
      setDraftTarget(null);
      setDraft(defaultDraft);
      fetchContracts();
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Lỗi", e?.message || "Không thể lưu bản nháp.");
    } finally {
      setSubmittingDraft(false);
    }
  };

  const handleDeptApprove = (id: number) => {
    alertRef.current(
      "confirm",
      "Xác nhận chuyển HR",
      "Bạn muốn xác nhận đề xuất này và chuyển cho HR soạn thảo hợp đồng?",
      async () => {
        try {
          await contractApi.deptReview(id, { isApproved: true });
          alertRef.current("success", "Đã chuyển HR", "Yêu cầu hợp đồng đã được chuyển sang bộ phận HR.");
          fetchContracts();
        } catch (err: unknown) {
          const e = err as { message?: string };
          alertRef.current("error", "Lỗi", e?.message || "Không thể xác nhận yêu cầu.");
        }
      },
    );
  };

  const openReject = (contract: ContractDto, ctx: "dept" | "hr") => {
    setRejectTarget(contract);
    setRejectCtx(ctx);
    setRejectReason("");
  };

  const handleReject = async () => {
    if (!rejectTarget) return;
    if (!rejectReason.trim()) {
      alertRef.current("warning", "Thiếu lý do", "Vui lòng nhập lý do từ chối.");
      return;
    }

    try {
      const payload: ReviewContractPayload = { isApproved: false, rejectReason: rejectReason.trim() };
      if (rejectCtx === "dept") {
        await contractApi.deptReview(rejectTarget.id, payload);
      } else {
        await contractApi.hrReject(rejectTarget.id, payload);
      }
      alertRef.current("success", "Đã từ chối", "Yêu cầu hợp đồng đã được cập nhật.");
      setRejectTarget(null);
      setRejectReason("");
      fetchContracts();
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Lỗi", e?.message || "Thao tác thất bại.");
    }
  };

  return (
    <FeaturePage
      title="Quản lý hợp đồng"
      description="Xử lý đề xuất từ phòng ban, soạn thảo bản nháp và theo dõi trạng thái phê duyệt hợp đồng."
      actions={
        <button className={secondaryButtonClass} onClick={fetchContracts} disabled={loading}>
          <RefreshCw size={16} />
          Làm mới
        </button>
      }
    >
      <FeatureCard>
        <div className="flex flex-wrap gap-2">
          {tabs.map(item => {
            const active = tab === item.key;
            const badge =
              item.key === "pending-dept"
                ? counts.pendingDept
                : item.key === "pending-hr"
                  ? counts.pendingHR
                  : counts.all;

            return (
              <button
                key={item.key}
                onClick={() => setTab(item.key)}
                className={`inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-sm font-semibold transition ${
                  active
                    ? "border-blue-600 bg-blue-600 text-white"
                    : "border-gray-200 bg-white text-gray-700 hover:bg-gray-50"
                }`}
              >
                {item.label}
                <span className={`rounded-full px-2 py-0.5 text-xs ${active ? "bg-white/20" : "bg-gray-100 text-gray-600"}`}>
                  {badge}
                </span>
              </button>
            );
          })}
        </div>
      </FeatureCard>

      {loading ? (
        <FeatureCard>
          <div className="py-10 text-center text-sm text-gray-500">Đang tải danh sách hợp đồng...</div>
        </FeatureCard>
      ) : contracts.length === 0 ? (
        <FeatureCard>
          <EmptyState title="Không có hợp đồng cần xử lý" description="Các mục mới sẽ xuất hiện tại đây khi có yêu cầu phù hợp." />
        </FeatureCard>
      ) : (
        <div className="space-y-3">
          {contracts.map(contract => (
            <FeatureCard key={contract.id} className="p-4">
              <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className={`rounded-md border px-2.5 py-1 text-xs font-semibold ${statusClass(contract.status)}`}>
                      {STATUS_LABELS[contract.status] ?? contract.status}
                    </span>
                    <span className="text-xs font-medium text-gray-500">Phiên bản v{contract.version || 0}</span>
                  </div>
                  <h2 className="mt-2 text-base font-semibold text-gray-900">
                    {contract.contractNumber || `Hợp đồng #${contract.id}`}
                  </h2>
                  <p className="mt-1 text-sm text-gray-500">
                    {contract.employeeName || "Chưa có tên nhân viên"} · {(CONTRACT_TYPE_LABELS[contract.contractType] ?? contract.contractType) || "Chưa chọn loại"}
                  </p>
                </div>

                <div className="flex flex-wrap gap-2">
                  {tab === "pending-dept" && (
                    <>
                      <button className={dangerButtonClass} onClick={() => openReject(contract, "dept")}>
                        <X size={16} />
                        Từ chối
                      </button>
                      <button className={primaryButtonClass} onClick={() => handleDeptApprove(contract.id)}>
                        <Check size={16} />
                        Chuyển HR
                      </button>
                    </>
                  )}

                  {tab === "pending-hr" && (
                    <>
                      <button className={dangerButtonClass} onClick={() => openReject(contract, "hr")}>
                        <X size={16} />
                        Từ chối
                      </button>
                      <button className={primaryButtonClass} onClick={() => openDraft(contract)}>
                        <FilePenLine size={16} />
                        {contract.status === "Negotiating" ? "Cập nhật bản nháp" : "Lập bản nháp"}
                      </button>
                    </>
                  )}
                </div>
              </div>

              <div className="mt-4 grid gap-3 border-t border-gray-100 pt-4 text-sm sm:grid-cols-2 lg:grid-cols-4">
                <div>
                  <p className="text-xs font-medium text-gray-500">Lương cơ bản</p>
                  <p className="mt-1 font-semibold text-gray-900">{fmt(contract.basicSalary)}</p>
                </div>
                <div>
                  <p className="text-xs font-medium text-gray-500">Tỷ lệ thực lĩnh</p>
                  <p className="mt-1 font-semibold text-gray-900">{contract.salaryPercentage || 0}%</p>
                </div>
                <div>
                  <p className="text-xs font-medium text-gray-500">BHXH</p>
                  <p className="mt-1 font-semibold text-gray-900">{fmt(contract.insuranceSalary)}</p>
                </div>
                <div>
                  <p className="text-xs font-medium text-gray-500">Hiệu lực</p>
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

      {draftTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="w-full max-w-2xl rounded-lg bg-white p-6 shadow-2xl">
            <div className="mb-5 flex items-start justify-between gap-4 border-b border-gray-100 pb-4">
              <div>
                <h2 className="text-lg font-semibold text-gray-900">
                  {draftTarget.status === "Negotiating" ? "Cập nhật bản nháp hợp đồng" : "Lập bản nháp hợp đồng"}
                </h2>
                <p className="mt-1 text-sm text-gray-500">
                  SLA phản hồi của nhân viên sẽ bắt đầu sau khi bản nháp được gửi.
                </p>
              </div>
              <FileText className="text-blue-600" size={22} />
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <label className="sm:col-span-2">
                <span className="mb-1 block text-sm font-medium text-gray-700">Loại hợp đồng</span>
                <select
                  className={fieldClass}
                  value={draft.contractType}
                  onChange={e => setDraft(prev => ({ ...prev, contractType: e.target.value }))}
                >
                  {CONTRACT_TYPES.map(type => (
                    <option key={type} value={type}>
                      {CONTRACT_TYPE_LABELS[type] ?? type}
                    </option>
                  ))}
                </select>
              </label>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Lương cơ bản</span>
                <input
                  className={fieldClass}
                  type="number"
                  min={0}
                  value={draft.basicSalary}
                  onChange={e => setDraft(prev => ({ ...prev, basicSalary: e.target.value }))}
                />
              </label>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Tỷ lệ thực lĩnh (%)</span>
                <input
                  className={fieldClass}
                  type="number"
                  min={1}
                  max={100}
                  value={draft.salaryPercentage}
                  onChange={e => setDraft(prev => ({ ...prev, salaryPercentage: e.target.value }))}
                />
              </label>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Lương đóng BHXH</span>
                <input
                  className={fieldClass}
                  type="number"
                  min={0}
                  value={draft.insuranceSalary}
                  onChange={e => setDraft(prev => ({ ...prev, insuranceSalary: e.target.value }))}
                />
              </label>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Ngày bắt đầu</span>
                <input
                  className={fieldClass}
                  type="date"
                  value={draft.startDate}
                  onChange={e => setDraft(prev => ({ ...prev, startDate: e.target.value }))}
                />
              </label>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Ngày kết thúc</span>
                <input
                  className={fieldClass}
                  type="date"
                  value={draft.endDate}
                  onChange={e => setDraft(prev => ({ ...prev, endDate: e.target.value }))}
                />
              </label>
            </div>

            <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <button
                className={secondaryButtonClass}
                onClick={() => {
                  setDraftTarget(null);
                  setDraft(defaultDraft);
                }}
              >
                Hủy
              </button>
              <button className={primaryButtonClass} onClick={handleCreateDraft} disabled={submittingDraft}>
                <Check size={16} />
                {submittingDraft ? "Đang lưu..." : "Lưu và gửi"}
              </button>
            </div>
          </div>
        </div>
      )}

      {rejectTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-2xl">
            <h2 className="text-lg font-semibold text-gray-900">Từ chối yêu cầu hợp đồng</h2>
            <p className="mt-1 text-sm text-gray-500">
              Lý do sẽ được lưu vào ghi chú hợp đồng để các bên liên quan theo dõi.
            </p>

            <label className="mt-4 block">
              <span className="mb-1 block text-sm font-medium text-gray-700">Lý do từ chối</span>
              <textarea
                className={textareaClass}
                value={rejectReason}
                onChange={e => setRejectReason(e.target.value)}
                placeholder="Nhập lý do từ chối..."
              />
            </label>

            <div className="mt-5 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <button className={secondaryButtonClass} onClick={() => setRejectTarget(null)}>
                Hủy
              </button>
              <button className={dangerButtonClass} onClick={handleReject}>
                <X size={16} />
                Xác nhận từ chối
              </button>
            </div>
          </div>
        </div>
      )}
    </FeaturePage>
  );
};
