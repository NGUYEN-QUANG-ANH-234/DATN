import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Check, FilePenLine, FilePlus2, RefreshCw, Send, X } from "lucide-react";
import { contractApi } from "../api/contractApi";
import type { ContractDto } from "../api/contractApi";
import { contractAddendumApi } from "../api/contractAddendumApi";
import type { ContractAddendumDto, CreateContractAddendumPayload } from "../api/contractAddendumApi";
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
  newBasicSalary: string;
  newInsuranceSalary: string;
  newEndDate: string;
  deptId: string;
  positionId: string;
  otherChangesJson: string;
  content: string;
  effectiveDate: string;
};

const defaultForm: AddendumForm = {
  contractId: "",
  newBasicSalary: "",
  newInsuranceSalary: "",
  newEndDate: "",
  deptId: "",
  positionId: "",
  otherChangesJson: "",
  content: "",
  effectiveDate: "",
};

const STATUS_MAP: Record<string, { label: string; cls: string }> = {
  Draft: { label: "Bản nháp", cls: "bg-gray-100 text-gray-700 border-gray-200" },
  PendingDirector: { label: "Chờ Giám đốc duyệt", cls: "bg-purple-50 text-purple-700 border-purple-200" },
  Active: { label: "Có hiệu lực", cls: "bg-green-50 text-green-700 border-green-200" },
  Rejected: { label: "Bị từ chối", cls: "bg-red-50 text-red-700 border-red-200" },
};

const fmt = (value?: number | null) =>
  value == null ? "-" : new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(value);

const dateText = (value?: string | null) =>
  value ? new Date(value).toLocaleDateString("vi-VN") : "-";

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
      alertRef.current("error", "Lỗi tải dữ liệu", e?.message || "Không thể tải dữ liệu phụ lục.");
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

    if (form.otherChangesJson.trim()) {
      try {
        JSON.parse(form.otherChangesJson);
      } catch {
        alertRef.current("warning", "JSON chưa hợp lệ", "Thông tin điều chỉnh nâng cao cần là JSON hợp lệ.");
        return null;
      }
    }

    const otherChangesJson = buildOtherChangesJson();
    if (!form.newBasicSalary && !form.newInsuranceSalary && !form.newEndDate && !otherChangesJson) {
      alertRef.current("warning", "Thiếu nội dung", "Phụ lục cần có ít nhất một thông tin điều chỉnh.");
      return null;
    }

    return {
      newBasicSalary: form.newBasicSalary ? Number(form.newBasicSalary) : undefined,
      newInsuranceSalary: form.newInsuranceSalary ? Number(form.newInsuranceSalary) : undefined,
      newEndDate: form.newEndDate || undefined,
      otherChangesJson,
      content: form.content.trim() || undefined,
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
      newBasicSalary: addendum.newBasicSalary == null ? "" : String(addendum.newBasicSalary),
      newInsuranceSalary: addendum.newInsuranceSalary == null ? "" : String(addendum.newInsuranceSalary),
      newEndDate: addendum.newEndDate ? addendum.newEndDate.slice(0, 10) : "",
      deptId,
      positionId,
      otherChangesJson: Object.keys(parsed).length ? JSON.stringify(parsed, null, 2) : "",
      content: addendum.content || "",
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
      alertRef.current("warning", "Thiếu lý do", "Vui lòng nhập lý do từ chối.");
      return;
    }

    try {
      await contractAddendumApi.reject(rejectTarget.id, rejectReason.trim());
      alertRef.current("success", "Đã từ chối", "Phụ lục hợp đồng đã bị từ chối.");
      setRejectTarget(null);
      setRejectReason("");
      fetchData();
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Lỗi", e?.message || "Không thể từ chối phụ lục.");
    }
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
      description="Tạo phụ lục điều chỉnh lương, thời hạn hoặc thông tin điều chuyển gắn với hợp đồng gốc."
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
          <div className="py-10 text-center text-sm text-gray-500">Đang tải danh sách phụ lục...</div>
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
                    <p className="mt-1 text-sm text-gray-500">
                      HĐ gốc: {addendum.contractNumber || `#${addendum.contractId}`} · {addendum.employeeName || "Chưa có tên nhân viên"}
                    </p>
                  </div>

                  <div className="flex flex-wrap gap-2">
                    {addendum.status === "Draft" && (
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
                          Từ chối
                        </button>
                        <button className={primaryButtonClass} onClick={() => handleApprove(addendum.id)}>
                          <Check size={16} />
                          Phê duyệt
                        </button>
                      </>
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
                    <p className="text-xs font-medium text-gray-500">Ngày kết thúc mới</p>
                    <p className="mt-1 font-semibold text-gray-900">{dateText(addendum.newEndDate)}</p>
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
                  <p className="mt-3 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
                    {addendum.rejectReason}
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

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Lương cơ bản mới</span>
                <input className={fieldClass} type="number" min={0} value={form.newBasicSalary} onChange={e => setForm(prev => ({ ...prev, newBasicSalary: e.target.value }))} />
              </label>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Lương BHXH mới</span>
                <input className={fieldClass} type="number" min={0} value={form.newInsuranceSalary} onChange={e => setForm(prev => ({ ...prev, newInsuranceSalary: e.target.value }))} />
              </label>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Ngày kết thúc mới</span>
                <input className={fieldClass} type="date" value={form.newEndDate} onChange={e => setForm(prev => ({ ...prev, newEndDate: e.target.value }))} />
              </label>

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

      {rejectTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-2xl">
            <h2 className="text-lg font-semibold text-gray-900">Từ chối phụ lục</h2>
            <label className="mt-4 block">
              <span className="mb-1 block text-sm font-medium text-gray-700">Lý do từ chối</span>
              <textarea className={textareaClass} value={rejectReason} onChange={e => setRejectReason(e.target.value)} />
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
