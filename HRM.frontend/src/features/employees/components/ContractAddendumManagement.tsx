import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Check, Download, Eye, FilePenLine, FilePlus2, RefreshCw, Send, X } from "lucide-react";
import { contractApi } from "../api/contractApi";
import type { ContractDto } from "../api/contractApi";
import { contractAddendumApi } from "../api/contractAddendumApi";
import type { ContractAddendumDto, ContractDocumentPreviewDto, CreateContractAddendumPayload } from "../api/contractAddendumApi";
import { departmentApi } from "../../organization/api/departmentApi";
import type { DepartmentTree } from "../../organization/types/department";
import { recruitmentApi } from "../../recruitment/api/recruitmentApi";
import type { PositionOption } from "../../recruitment/types/recruitment";
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

type DepartmentOption = { id: number; deptName: string };

type AddendumForm = {
  contractId: string;
  addendumType: string;
  newBasicSalary: string;
  newInsuranceSalary: string;
  newEndDate: string;
  deptId: string;
  positionId: string;
  otherChangesJson: string;
  content: string;
  changedContentSummary: string;
  unchangedTerms: string;
  effectiveDate: string;
};

const defaultForm: AddendumForm = {
  contractId: "",
  addendumType: "Other",
  newBasicSalary: "",
  newInsuranceSalary: "",
  newEndDate: "",
  deptId: "",
  positionId: "",
  otherChangesJson: "",
  content: "",
  changedContentSummary: "",
  unchangedTerms: "Các điều khoản khác của hợp đồng lao động gốc không thay đổi và tiếp tục có hiệu lực.",
  effectiveDate: "",
};

const STATUS_MAP: Record<string, { label: string; cls: string }> = {
  Draft: { label: "Bản nháp", cls: "bg-gray-100 text-gray-700 border-gray-200" },
  PendingDept: { label: "Chờ Trưởng phòng xác nhận", cls: "bg-blue-50 text-blue-700 border-blue-200" },
  PendingHR: { label: "Chờ HR xác nhận chính sách", cls: "bg-indigo-50 text-indigo-700 border-indigo-200" },
  PendingEmployee: { label: "Chờ người lao động xác nhận", cls: "bg-amber-50 text-amber-700 border-amber-200" },
  PendingDirector: { label: "Chờ Giám đốc duyệt", cls: "bg-purple-50 text-purple-700 border-purple-200" },
  PendingHRRevision: { label: "Chờ HR chỉnh sửa", cls: "bg-amber-50 text-amber-700 border-amber-200" },
  ApprovedByDirector: { label: "Đã duyệt, chờ phát hành", cls: "bg-teal-50 text-teal-700 border-teal-200" },
  Active: { label: "Có hiệu lực", cls: "bg-green-50 text-green-700 border-green-200" },
  Rejected: { label: "Bị từ chối", cls: "bg-red-50 text-red-700 border-red-200" },
};

const ADDENDUM_TYPE_OPTIONS = [
  { value: "SalaryAdjustment", label: "Điều chỉnh lương" },
  { value: "InternalTransfer", label: "Điều chuyển nội bộ" },
  { value: "SeniorAppointment", label: "Bổ nhiệm/chức danh" },
  { value: "Other", label: "Nội dung khác" },
];

const ADDENDUM_TYPE_LABELS = new Map([
  ...ADDENDUM_TYPE_OPTIONS,
  { value: "Extension", label: "Gia hạn hợp đồng (cần tạo hợp đồng mới/tái ký)" },
].map(item => [item.value, item.label]));

const fmt = (value?: number | null) =>
  value == null ? "-" : new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(value);

const dateText = (value?: string | null) =>
  value ? new Date(value).toLocaleDateString("vi-VN") : "-";

const saveBlob = (blob: Blob, fileName: string) => {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
};

const unwrap = <T,>(res: unknown): T[] => {
  const raw = res as { data?: T[]; Data?: T[] };
  return raw.data || raw.Data || [];
};

const flattenDepartments = (nodes: DepartmentTree[], level = 0): DepartmentOption[] =>
  nodes.flatMap(node => [
    { id: node.id, deptName: `${"— ".repeat(level)}${node.deptName}` },
    ...flattenDepartments(node.children || [], level + 1),
  ]);

const parseOtherChanges = (json?: string | null): Record<string, unknown> => {
  if (!json) return {};
  try {
    const parsed = JSON.parse(json);
    return parsed && typeof parsed === "object" && !Array.isArray(parsed) ? parsed : {};
  } catch {
    return {};
  }
};

export const ContractAddendumManagement = () => {
  const [contracts, setContracts] = useState<ContractDto[]>([]);
  const [addendums, setAddendums] = useState<ContractAddendumDto[]>([]);
  const [departments, setDepartments] = useState<DepartmentOption[]>([]);
  const [positions, setPositions] = useState<PositionOption[]>([]);
  const [loading, setLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [editingTarget, setEditingTarget] = useState<ContractAddendumDto | null>(null);
  const [form, setForm] = useState<AddendumForm>(defaultForm);
  const [rejectTarget, setRejectTarget] = useState<ContractAddendumDto | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [documentPreview, setDocumentPreview] = useState<ContractDocumentPreviewDto | null>(null);
  const [documentTarget, setDocumentTarget] = useState<ContractAddendumDto | null>(null);
  const [documentLoading, setDocumentLoading] = useState(false);

  const { triggerAlert } = useNotification();
  const alertRef = useRef(triggerAlert);

  useEffect(() => {
    alertRef.current = triggerAlert;
  }, [triggerAlert]);

  const activeContracts = useMemo(
    () => contracts.filter(contract => contract.status === "Active"),
    [contracts],
  );

  const departmentNames = useMemo(
    () => new Map(departments.map(dept => [dept.id, dept.deptName.replace(/^—\s*/g, "")])),
    [departments],
  );

  const positionNames = useMemo(
    () => new Map(positions.map(position => [position.id, position.title])),
    [positions],
  );

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const [contractRes, addendumRes, deptRes, posRes] = await Promise.all([
        contractApi.getAllContracts(),
        contractAddendumApi.getAll(),
        departmentApi.getTree(),
        recruitmentApi.getPositions(),
      ]);
      setContracts(unwrap<ContractDto>(contractRes));
      setAddendums(unwrap<ContractAddendumDto>(addendumRes));
      setDepartments(flattenDepartments(unwrap<DepartmentTree>(deptRes)));
      setPositions(unwrap<PositionOption>(posRes));
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Không thể tải dữ liệu", e?.message || "Vui lòng thử lại.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const buildOtherChangesJson = (): string | undefined => {
    const advanced = parseOtherChanges(form.otherChangesJson);
    const merged: Record<string, unknown> = { ...advanced };

    if (form.deptId) merged.deptId = Number(form.deptId);
    else delete merged.deptId;

    if (form.positionId) merged.positionId = Number(form.positionId);
    else delete merged.positionId;

    return Object.keys(merged).length ? JSON.stringify(merged) : undefined;
  };

  const validateForm = (): CreateContractAddendumPayload | null => {
    if (!editingTarget && !Number(form.contractId)) {
      alertRef.current("warning", "Thiếu hợp đồng gốc", "Vui lòng chọn hợp đồng gốc.");
      return null;
    }

    if (!form.effectiveDate) {
      alertRef.current("warning", "Thiếu ngày hiệu lực", "Vui lòng chọn ngày hiệu lực phụ lục.");
      return null;
    }

    if (form.addendumType === "Extension" || form.newEndDate) {
      alertRef.current(
        "warning",
        "Cần tạo hợp đồng mới/tái ký",
        "Phụ lục không dùng để thay đổi thời hạn hợp đồng. Vui lòng tạo hợp đồng mới hoặc luồng tái ký.",
      );
      return null;
    }

    if (form.otherChangesJson.trim()) {
      try {
        JSON.parse(form.otherChangesJson);
      } catch {
        alertRef.current("warning", "JSON chưa hợp lệ", "Thông tin điều chỉnh nâng cao cần là JSON hợp lệ.");
        return null;
      }
    }

    const otherChangesJson = buildOtherChangesJson();
    if (!form.newBasicSalary && !form.newInsuranceSalary && !otherChangesJson) {
      alertRef.current("warning", "Thiếu nội dung", "Phụ lục cần có ít nhất một thông tin điều chỉnh.");
      return null;
    }

    return {
      addendumType: form.addendumType || "Other",
      newBasicSalary: form.newBasicSalary ? Number(form.newBasicSalary) : undefined,
      newInsuranceSalary: form.newInsuranceSalary ? Number(form.newInsuranceSalary) : undefined,
      otherChangesJson,
      content: form.content.trim() || undefined,
      changedContentSummary: form.changedContentSummary.trim() || undefined,
      unchangedTerms: form.unchangedTerms.trim() || undefined,
      effectiveDate: form.effectiveDate,
    };
  };

  const resetForm = () => {
    setShowForm(false);
    setEditingTarget(null);
    setForm(defaultForm);
  };

  const openCreate = () => {
    setEditingTarget(null);
    setForm(defaultForm);
    setShowForm(true);
  };

  const openEdit = (addendum: ContractAddendumDto) => {
    const parsed = parseOtherChanges(addendum.otherChangesJson);
    const deptId = parsed.deptId == null ? "" : String(parsed.deptId);
    const positionId = parsed.positionId == null ? "" : String(parsed.positionId);
    delete parsed.deptId;
    delete parsed.positionId;

    setEditingTarget(addendum);
    setForm({
      contractId: String(addendum.contractId),
      addendumType: addendum.addendumType || "Other",
      newBasicSalary: addendum.newBasicSalary == null ? "" : String(addendum.newBasicSalary),
      newInsuranceSalary: addendum.newInsuranceSalary == null ? "" : String(addendum.newInsuranceSalary),
      newEndDate: addendum.newEndDate ? addendum.newEndDate.slice(0, 10) : "",
      deptId,
      positionId,
      otherChangesJson: Object.keys(parsed).length ? JSON.stringify(parsed, null, 2) : "",
      content: addendum.content || "",
      changedContentSummary: addendum.changedContentSummary || "",
      unchangedTerms: addendum.unchangedTerms || defaultForm.unchangedTerms,
      effectiveDate: addendum.effectiveDate ? addendum.effectiveDate.slice(0, 10) : "",
    });
    setShowForm(true);
  };

  const handleSave = async () => {
    const payload = validateForm();
    if (!payload) return;

    try {
      if (editingTarget) {
        await contractAddendumApi.updateDraft(editingTarget.id, payload);
        alertRef.current("success", "Đã cập nhật", "Bản nháp phụ lục đã được cập nhật.");
      } else {
        await contractAddendumApi.createDraft(Number(form.contractId), payload);
        alertRef.current("success", "Đã tạo bản nháp", "Phụ lục hợp đồng đã được lưu ở trạng thái bản nháp.");
      }

      resetForm();
      fetchData();
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Lỗi", e?.message || "Không thể lưu phụ lục hợp đồng.");
    }
  };

  const handleSubmit = (id: number) => {
    alertRef.current("confirm", "Gửi duyệt phụ lục", "Bạn muốn gửi phụ lục này cho Giám đốc phê duyệt?", async () => {
      try {
        await contractAddendumApi.submit(id);
        alertRef.current("success", "Đã gửi duyệt", "Phụ lục đã chuyển sang trạng thái chờ Giám đốc.");
        fetchData();
      } catch (err: unknown) {
        const e = err as { message?: string };
        alertRef.current("error", "Lỗi", e?.message || "Không thể gửi duyệt phụ lục.");
      }
    });
  };

  const handleApprove = (id: number) => {
    alertRef.current(
      "confirm",
      "Phê duyệt phụ lục",
      "Phụ lục sẽ có hiệu lực, cập nhật hợp đồng gốc và ghi lịch sử biến động nhân sự.",
      async () => {
        try {
          await contractAddendumApi.approve(id);
          alertRef.current("success", "Đã phê duyệt", "Phụ lục đã có hiệu lực.");
          fetchData();
        } catch (err: unknown) {
          const e = err as { message?: string };
          alertRef.current("error", "Lỗi", e?.message || "Không thể phê duyệt phụ lục.");
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
      await contractAddendumApi.requestRevision(rejectTarget.id, { reason: rejectReason.trim() });
      alertRef.current("success", "Đã gửi yêu cầu chỉnh sửa", "Phụ lục hợp đồng đã được chuyển về HR chỉnh sửa.");
      setRejectTarget(null);
      setRejectReason("");
      fetchData();
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Lỗi", e?.message || "Không thể gửi yêu cầu chỉnh sửa phụ lục.");
    }
  };

  const openDocumentPreview = async (addendum: ContractAddendumDto) => {
    setDocumentTarget(addendum);
    setDocumentLoading(true);
    try {
      const res = await contractAddendumApi.previewDocument(addendum.id);
      const raw = res as unknown as { data?: ContractDocumentPreviewDto; Data?: ContractDocumentPreviewDto };
      setDocumentPreview(raw.data || raw.Data || null);
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Không thể xem trước", e?.message || "Phụ lục chưa có đủ dữ liệu văn bản.");
      setDocumentTarget(null);
    } finally {
      setDocumentLoading(false);
    }
  };

  const downloadDocument = async (addendum: ContractAddendumDto, type: "doc" | "pdf") => {
    try {
      const blob = type === "doc"
        ? await contractAddendumApi.downloadDocumentDoc(addendum.id)
        : await contractAddendumApi.downloadDocumentPdf(addendum.id);
      saveBlob(blob, `${addendum.legalDocumentNumber || addendum.addendumNumber || `addendum-${addendum.id}`}.${type}`);
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Không thể tải văn bản", e?.message || "Vui lòng thử lại.");
    }
  };

  const issueDocument = async (addendum: ContractAddendumDto) => {
    alertRef.current("confirm", "Phát hành phụ lục", "Văn bản phụ lục sẽ được đánh dấu đã phát hành và sẵn sàng tải DOC.", async () => {
      try {
        const res = await contractAddendumApi.issueDocument(addendum.id, {
          legalDocumentNumber: addendum.legalDocumentNumber || addendum.addendumNumber,
          documentTemplateCode: addendum.documentTemplateCode || "CONTRACT_ADDENDUM",
          issuedAt: new Date().toISOString(),
          employerSignedAt: new Date().toISOString(),
        });
        const raw = res as unknown as { data?: ContractDocumentPreviewDto; Data?: ContractDocumentPreviewDto };
        setDocumentTarget(addendum);
        setDocumentPreview(raw.data || raw.Data || null);
        alertRef.current("success", "Đã phát hành", "Văn bản phụ lục đã sẵn sàng để tải.");
        fetchData();
      } catch (err: unknown) {
        const e = err as { message?: string };
        alertRef.current("error", "Không thể phát hành", e?.message || "Vui lòng thử lại.");
      }
    });
  };

  const describeOtherChanges = (json?: string | null) => {
    const parsed = parseOtherChanges(json);
    const labels: string[] = [];
    const deptId = Number(parsed.deptId);
    const positionId = Number(parsed.positionId);

    if (deptId) labels.push(`Phòng ban mới: ${departmentNames.get(deptId) || `#${deptId}`}`);
    if (positionId) labels.push(`Vị trí mới: ${positionNames.get(positionId) || `#${positionId}`}`);

    const rest = { ...parsed };
    delete rest.deptId;
    delete rest.positionId;
    if (Object.keys(rest).length) labels.push(`Khác: ${JSON.stringify(rest)}`);

    return labels;
  };

  return (
      <FeaturePage
        title="Phụ lục hợp đồng"
      description="Tạo phụ lục điều chỉnh lương, chức danh, phòng ban hoặc điều khoản bổ sung của hợp đồng."
      actions={
        <div className="flex flex-wrap gap-2">
          <button className={secondaryButtonClass} onClick={fetchData} disabled={loading}>
            <RefreshCw size={16} />
            Làm mới
          </button>
          <button className={primaryButtonClass} onClick={openCreate}>
            <FilePlus2 size={16} />
            Tạo phụ lục
          </button>
        </div>
      }
    >
      {loading ? (
        <FeatureCard>
          <div className="py-10 text-center text-sm text-gray-500">Đang tải dữ liệu...</div>
        </FeatureCard>
      ) : addendums.length === 0 ? (
        <FeatureCard>
          <EmptyState title="Chưa có phụ lục hợp đồng" description="Tạo phụ lục từ một hợp đồng đang có hiệu lực để bắt đầu luồng phê duyệt." />
        </FeatureCard>
      ) : (
        <div className="space-y-3">
          {addendums.map(addendum => {
            const status = STATUS_MAP[addendum.status] ?? { label: addendum.status, cls: "bg-gray-100 text-gray-700 border-gray-200" };
            const otherLabels = describeOtherChanges(addendum.otherChangesJson);

            return (
              <FeatureCard key={addendum.id} className="p-4">
                <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                  <div>
                    <span className={`inline-flex rounded-md border px-2.5 py-1 text-xs font-semibold ${status.cls}`}>
                      {status.label}
                    </span>
                    <h2 className="mt-2 text-base font-semibold text-gray-900">{addendum.addendumNumber}</h2>
                    <p className="mt-1 text-xs font-medium text-gray-500">
                      {ADDENDUM_TYPE_LABELS.get(addendum.addendumType) || addendum.addendumType || "Nội dung khác"}
                    </p>
                    <p className="mt-1 text-sm text-gray-500">
                      HĐ gốc: {addendum.contractNumber || `#${addendum.contractId}`} · {addendum.employeeName || "Chưa có tên nhân viên"}
                    </p>
                  </div>

                  <div className="flex flex-wrap gap-2">
                    {(addendum.status === "Draft" || addendum.status === "PendingHRRevision") && (
                      <>
                        <button className={secondaryButtonClass} onClick={() => openEdit(addendum)}>
                          <FilePenLine size={16} />
                          Sửa
                        </button>
                        <button className={primaryButtonClass} onClick={() => handleSubmit(addendum.id)}>
                          <Send size={16} />
                          Gửi duyệt
                        </button>
                      </>
                    )}
                    {addendum.status === "PendingDirector" && (
                      <>
                        <button className={dangerButtonClass} onClick={() => { setRejectTarget(addendum); setRejectReason(""); }}>
                          <X size={16} />
                          Yêu cầu chỉnh sửa
                        </button>
                        <button className={primaryButtonClass} onClick={() => handleApprove(addendum.id)}>
                          <Check size={16} />
                          Phê duyệt
                        </button>
                      </>
                    )}
                    <button className={secondaryButtonClass} onClick={() => openDocumentPreview(addendum)}>
                      <Eye size={16} />
                      Xem trước
                    </button>
                    <button className={secondaryButtonClass} onClick={() => downloadDocument(addendum, "doc")}>
                      <Download size={16} />
                      Tải DOC
                    </button>
                    {addendum.documentPdfFilePath && (
                      <button className={secondaryButtonClass} onClick={() => downloadDocument(addendum, "pdf")}>
                        <Download size={16} />
                        PDF
                      </button>
                    )}
                    {!addendum.issuedAt && (addendum.status === "ApprovedByDirector" || addendum.status === "Active") && (
                      <button className={primaryButtonClass} onClick={() => issueDocument(addendum)}>
                        <Send size={16} />
                        Phát hành
                      </button>
                    )}
                  </div>
                </div>

                <div className="mt-4 grid gap-3 border-t border-gray-100 pt-4 text-sm sm:grid-cols-2 lg:grid-cols-4">
                  <div>
                    <p className="text-xs font-medium text-gray-500">Lương cơ bản mới</p>
                    <p className="mt-1 font-semibold text-gray-900">{fmt(addendum.newBasicSalary)}</p>
                  </div>
                  <div>
                    <p className="text-xs font-medium text-gray-500">BHXH mới</p>
                    <p className="mt-1 font-semibold text-gray-900">{fmt(addendum.newInsuranceSalary)}</p>
                  </div>
                  <div>
                    <p className="text-xs font-medium text-gray-500">Ngày hiệu lực</p>
                    <p className="mt-1 font-semibold text-gray-900">{dateText(addendum.effectiveDate)}</p>
                  </div>
                </div>

                {addendum.content && (
                  <p className="mt-4 rounded-lg border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-700">
                    {addendum.content}
                  </p>
                )}
                {otherLabels.length > 0 && (
                  <div className="mt-3 rounded-lg border border-blue-100 bg-blue-50 px-3 py-2 text-sm text-blue-800">
                    {otherLabels.map(label => <p key={label}>{label}</p>)}
                  </div>
                )}
                {addendum.rejectReason && (
                  <p className="mt-3 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800">
                    <span className="font-semibold">Yêu cầu chỉnh sửa: </span>{addendum.rejectReason}
                  </p>
                )}
              </FeatureCard>
            );
          })}
        </div>
      )}

      {showForm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-lg bg-white p-6 shadow-2xl">
            <h2 className="text-lg font-semibold text-gray-900">
              {editingTarget ? "Sửa bản nháp phụ lục" : "Tạo phụ lục hợp đồng"}
            </h2>
            <p className="mt-1 text-sm text-gray-500">Chỉ cần nhập những trường thực sự thay đổi.</p>

            <div className="mt-5 grid gap-4 sm:grid-cols-2">
              <label className="sm:col-span-2">
                <span className="mb-1 block text-sm font-medium text-gray-700">Hợp đồng gốc</span>
                <select
                  className={fieldClass}
                  value={form.contractId}
                  disabled={Boolean(editingTarget)}
                  onChange={e => setForm(prev => ({ ...prev, contractId: e.target.value }))}
                >
                  <option value="">Chọn hợp đồng đang hiệu lực</option>
                  {activeContracts.map(contract => (
                    <option key={contract.id} value={contract.id}>
                      {contract.contractNumber} - {contract.employeeName || `NV #${contract.employeeId}`}
                    </option>
                  ))}
                </select>
              </label>

              <label className="sm:col-span-2">
                <span className="mb-1 block text-sm font-medium text-gray-700">Loại phụ lục</span>
                <select
                  className={fieldClass}
                  value={form.addendumType}
                  onChange={e => setForm(prev => ({ ...prev, addendumType: e.target.value }))}
                >
                  {ADDENDUM_TYPE_OPTIONS.map(option => (
                    <option key={option.value} value={option.value}>{option.label}</option>
                  ))}
                </select>
              </label>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Lương cơ bản mới</span>
                <input className={fieldClass} type="number" min={0} value={form.newBasicSalary} onChange={e => setForm(prev => ({ ...prev, newBasicSalary: e.target.value }))} />
              </label>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Lương BHXH mới</span>
                <input className={fieldClass} type="number" min={0} value={form.newInsuranceSalary} onChange={e => setForm(prev => ({ ...prev, newInsuranceSalary: e.target.value }))} />
              </label>

              <div className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800">
                Thay đổi thời hạn hoặc loại hợp đồng cần tạo hợp đồng mới/tái ký, không xử lý bằng phụ lục.
              </div>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Ngày hiệu lực</span>
                <input className={fieldClass} type="date" value={form.effectiveDate} onChange={e => setForm(prev => ({ ...prev, effectiveDate: e.target.value }))} />
              </label>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Phòng ban mới</span>
                <select className={fieldClass} value={form.deptId} onChange={e => setForm(prev => ({ ...prev, deptId: e.target.value }))}>
                  <option value="">Không thay đổi</option>
                  {departments.map(dept => <option key={dept.id} value={dept.id}>{dept.deptName}</option>)}
                </select>
              </label>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Vị trí mới</span>
                <select className={fieldClass} value={form.positionId} onChange={e => setForm(prev => ({ ...prev, positionId: e.target.value }))}>
                  <option value="">Không thay đổi</option>
                  {positions.map(position => <option key={position.id} value={position.id}>{position.title}</option>)}
                </select>
              </label>

              <label className="sm:col-span-2">
                <span className="mb-1 block text-sm font-medium text-gray-700">Nội dung tóm tắt</span>
                <textarea className={textareaClass} value={form.content} onChange={e => setForm(prev => ({ ...prev, content: e.target.value }))} placeholder="Ví dụ: Điều chỉnh lương định kỳ năm 2026" />
              </label>

              <label className="sm:col-span-2">
                <span className="mb-1 block text-sm font-medium text-gray-700">Nội dung thay đổi trên phụ lục</span>
                <textarea className={textareaClass} value={form.changedContentSummary} onChange={e => setForm(prev => ({ ...prev, changedContentSummary: e.target.value }))} placeholder="Để trống nếu muốn hệ thống tự tóm tắt từ các trường thay đổi." />
              </label>

              <label className="sm:col-span-2">
                <span className="mb-1 block text-sm font-medium text-gray-700">Điều khoản giữ nguyên</span>
                <textarea className={textareaClass} value={form.unchangedTerms} onChange={e => setForm(prev => ({ ...prev, unchangedTerms: e.target.value }))} />
              </label>

              <label className="sm:col-span-2">
                <span className="mb-1 block text-sm font-medium text-gray-700">Điều chỉnh nâng cao dạng JSON</span>
                <textarea className={textareaClass} value={form.otherChangesJson} onChange={e => setForm(prev => ({ ...prev, otherChangesJson: e.target.value }))} placeholder='Ví dụ: {"workShiftId": 2}' />
              </label>
            </div>

            <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <button className={secondaryButtonClass} onClick={resetForm}>
                Hủy
              </button>
              <button className={primaryButtonClass} onClick={handleSave}>
                <FilePlus2 size={16} />
                {editingTarget ? "Cập nhật bản nháp" : "Lưu bản nháp"}
              </button>
            </div>
          </div>
        </div>
      )}

      {documentTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="flex max-h-[92vh] w-full max-w-5xl flex-col rounded-lg bg-white shadow-2xl">
            <div className="flex items-start justify-between gap-4 border-b border-gray-100 p-5">
              <div>
                <h2 className="text-lg font-semibold text-gray-900">Xem trước phụ lục hợp đồng</h2>
                <p className="mt-1 text-sm text-gray-500">
                  {documentPreview?.documentNumber || documentTarget.addendumNumber} · {documentPreview?.templateCode || "CONTRACT_ADDENDUM"}
                </p>
              </div>
              <button className={secondaryButtonClass} onClick={() => { setDocumentTarget(null); setDocumentPreview(null); }}>
                Đóng
              </button>
            </div>
            <div className="flex-1 overflow-y-auto bg-gray-100 p-4">
              {documentLoading ? (
                <div className="rounded-lg bg-white p-8 text-center text-sm text-gray-500">Đang tải bản xem trước...</div>
              ) : documentPreview?.html ? (
                <div className="rounded-lg bg-white shadow-sm" dangerouslySetInnerHTML={{ __html: documentPreview.html }} />
              ) : (
                <div className="rounded-lg bg-white p-8 text-center text-sm text-gray-500">Chưa có nội dung xem trước.</div>
              )}
            </div>
            <div className="flex flex-col-reverse gap-2 border-t border-gray-100 p-4 sm:flex-row sm:justify-end">
              <button className={secondaryButtonClass} onClick={() => downloadDocument(documentTarget, "doc")}>
                <Download size={16} />
                Tải DOC
              </button>
              {documentPreview?.canDownloadPdf && (
                <button className={secondaryButtonClass} onClick={() => downloadDocument(documentTarget, "pdf")}>
                  <Download size={16} />
                  Tải PDF
                </button>
              )}
              {!documentTarget.issuedAt && (documentTarget.status === "ApprovedByDirector" || documentTarget.status === "Active") && (
                <button className={primaryButtonClass} onClick={() => issueDocument(documentTarget)}>
                  <Send size={16} />
                  Phát hành
                </button>
              )}
            </div>
          </div>
        </div>
      )}

      {rejectTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-2xl">
            <h2 className="text-lg font-semibold text-gray-900">Yêu cầu chỉnh sửa phụ lục</h2>
            <p className="mt-1 text-sm text-gray-500">Nội dung này sẽ được chuyển về HR để cập nhật bản nháp phụ lục.</p>
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
