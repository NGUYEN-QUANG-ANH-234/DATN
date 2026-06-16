import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Archive,
  CheckCircle2,
  Copy,
  GitBranch,
  Plus,
  RefreshCw,
  Save,
  Send,
  ShieldCheck,
  Trash2,
  Wand2,
} from "lucide-react";
import { Badge, Button, Card, DrawerForm, Input, Select } from "../../../components/ui";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { useNotification } from "../../../core/context/NotificationContext";
import { payrollApi } from "../api/payrollApi";
import type {
  PayrollFormula,
  PayrollFormulaLine,
  PayrollFormulaStatus,
  PayrollFormulaValidationResult,
  PayrollFormulaVariable,
  UpsertPayrollFormulaRequest,
} from "../types/payroll";

const statusOptions = [
  { value: "", label: "Tất cả trạng thái" },
  { value: "Draft", label: "Bản nháp" },
  { value: "PendingDirectorApproval", label: "Chờ giám đốc duyệt" },
  { value: "RevisionRequired", label: "Cần chỉnh sửa" },
  { value: "Approved", label: "Đã duyệt" },
  { value: "Active", label: "Đang áp dụng" },
  { value: "Archived", label: "Lưu trữ" },
  { value: "Rejected", label: "Từ chối" },
];

const contractTypeOptions = [
  { value: "", label: "Mọi loại hợp đồng" },
  { value: "Probation", label: "Thử việc" },
  { value: "Definite", label: "Xác định thời hạn" },
  { value: "Indefinite", label: "Không xác định thời hạn" },
  { value: "PartTime", label: "Bán thời gian" },
];

const payBasisOptions = [
  { value: "", label: "Mọi hình thức trả lương" },
  { value: "Monthly", label: "Theo tháng" },
  { value: "Daily", label: "Theo ngày" },
  { value: "Hourly", label: "Theo giờ" },
  { value: "FixedPackage", label: "Gói cố định" },
];

const employeeTypeOptions = [
  { value: "", label: "Mọi loại nhân sự" },
  { value: "Intern", label: "Thực tập sinh" },
  { value: "Official", label: "Chính thức" },
  { value: "Probation", label: "Thử việc" },
  { value: "PartTime", label: "Bán thời gian" },
  { value: "Contractual", label: "Hợp đồng thời vụ" },
];

const defaultLine = (order: number): PayrollFormulaLine => ({
  componentCode: "",
  expression: "",
  calculationOrder: order,
  isGrossComponent: true,
  isTaxable: true,
  isInsuranceBased: false,
  isDeduction: false,
  isSnapshotRequired: true,
  note: "",
});

const defaultForm = (): UpsertPayrollFormulaRequest => ({
  formulaCode: "DEFAULT_PAYROLL",
  formulaName: "",
  expression: "",
  contractType: null,
  payBasis: null,
  employeeType: null,
  deptId: null,
  positionId: null,
  jobLevelId: null,
  versionCode: "",
  effectiveFrom: toDateInput(new Date().toISOString()),
  effectiveTo: "",
  lines: [
    {
      componentCode: "BASE_SALARY_ACTUAL",
      expression: "contract_segment_salary_amount",
      calculationOrder: 10,
      isGrossComponent: true,
      isTaxable: true,
      isInsuranceBased: true,
      isDeduction: false,
      isSnapshotRequired: true,
      note: "Lương cơ bản theo công",
    },
  ],
});

const statusVariant = (status: string) => {
  if (status === "Active" || status === "Approved") return "success";
  if (status === "PendingDirectorApproval") return "warning";
  if (status === "RevisionRequired") return "info";
  if (status === "Rejected") return "danger";
  return "neutral";
};

export const SalaryFormulaPage = () => {
  const { triggerAlert } = useNotification();
  const [statusFilter, setStatusFilter] = useState("");
  const [formulas, setFormulas] = useState<PayrollFormula[]>([]);
  const [variables, setVariables] = useState<PayrollFormulaVariable[]>([]);
  const [selectedFormula, setSelectedFormula] = useState<PayrollFormula | null>(null);
  const [form, setForm] = useState<UpsertPayrollFormulaRequest>(defaultForm);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [activeLineIndex, setActiveLineIndex] = useState(0);
  const [validation, setValidation] = useState<PayrollFormulaValidationResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  const activeVariables = useMemo(
    () => variables.filter((item) => item.isActive),
    [variables],
  );

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      const [formulaRes, variableRes] = await Promise.all([
        payrollApi.getPayrollFormulas(statusFilter || undefined),
        payrollApi.getPayrollFormulaVariables(),
      ]);
      setFormulas(formulaRes.data ?? []);
      setVariables(variableRes.data ?? []);
    } catch (error) {
      triggerAlert("error", "Không thể tải công thức lương", getErrorMessage(error));
    } finally {
      setLoading(false);
    }
  }, [statusFilter, triggerAlert]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const openCreate = () => {
    setSelectedFormula(null);
    setForm(defaultForm());
    setValidation(null);
    setActiveLineIndex(0);
    setDrawerOpen(true);
  };

  const openEdit = (formula: PayrollFormula) => {
    setSelectedFormula(formula);
    setForm(toForm(formula));
    setValidation(null);
    setActiveLineIndex(0);
    setDrawerOpen(true);
  };

  const handleValidate = async () => {
    try {
      const res = await payrollApi.validatePayrollFormula(normalizePayload(form));
      setValidation(res.data);
      triggerAlert(
        res.data.isValid ? "success" : "warning",
        res.data.isValid ? "Công thức hợp lệ" : "Công thức cần kiểm tra",
        res.data.isValid
          ? "Có thể lưu bản nháp hoặc gửi duyệt."
          : res.data.errors.slice(0, 2).join(" "),
      );
      return res.data.isValid;
    } catch (error) {
      triggerAlert("error", "Không thể kiểm tra công thức", getErrorMessage(error));
      return false;
    }
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      const payload = normalizePayload(form);
      const res = selectedFormula
        ? await payrollApi.updatePayrollFormula(selectedFormula.id, payload)
        : await payrollApi.createPayrollFormula(payload);
      setSelectedFormula(res.data);
      setForm(toForm(res.data));
      triggerAlert("success", "Đã lưu công thức", "Bản nháp công thức lương đã được cập nhật.");
      await loadData();
    } catch (error) {
      triggerAlert("error", "Chưa thể lưu công thức", getErrorMessage(error));
    } finally {
      setSaving(false);
    }
  };

  const handleSubmit = async (formula: PayrollFormula) => {
    try {
      setSaving(true);
      const res = await payrollApi.submitPayrollFormula(formula.id);
      triggerAlert("success", "Đã gửi duyệt", "Công thức đã được chuyển sang bàn phê duyệt.");
      setSelectedFormula(res.data);
      setForm(toForm(res.data));
      await loadData();
    } catch (error) {
      triggerAlert("error", "Không thể gửi duyệt", getErrorMessage(error));
    } finally {
      setSaving(false);
    }
  };

  const handleClone = async (formula: PayrollFormula) => {
    try {
      setSaving(true);
      const res = await payrollApi.clonePayrollFormula(formula.id);
      triggerAlert("success", "Đã tạo version mới", "Version mới đang ở trạng thái bản nháp.");
      openEdit(res.data);
      await loadData();
    } catch (error) {
      triggerAlert("error", "Không thể clone công thức", getErrorMessage(error));
    } finally {
      setSaving(false);
    }
  };

  const handleActivate = async (formula: PayrollFormula) => {
    try {
      setSaving(true);
      const res = await payrollApi.activatePayrollFormula(formula.id);
      triggerAlert("success", "Đã kích hoạt công thức", "Payroll kỳ mới sẽ dùng version đang áp dụng.");
      setSelectedFormula(res.data);
      setForm(toForm(res.data));
      await loadData();
    } catch (error) {
      triggerAlert("error", "Không thể kích hoạt", getErrorMessage(error));
    } finally {
      setSaving(false);
    }
  };

  const handleArchive = async (formula: PayrollFormula) => {
    try {
      setSaving(true);
      const res = await payrollApi.archivePayrollFormula(formula.id, "Lưu trữ từ màn quản trị công thức lương.");
      triggerAlert("success", "Đã lưu trữ công thức", "Công thức sẽ không được dùng cho kỳ lương mới.");
      setSelectedFormula(res.data);
      setForm(toForm(res.data));
      await loadData();
    } catch (error) {
      triggerAlert("error", "Không thể lưu trữ", getErrorMessage(error));
    } finally {
      setSaving(false);
    }
  };

  const updateLine = (index: number, patch: Partial<PayrollFormulaLine>) => {
    setForm((current) => ({
      ...current,
      lines: current.lines.map((line, lineIndex) =>
        lineIndex === index ? { ...line, ...patch } : line,
      ),
    }));
  };

  const appendVariableToActiveLine = (variable: PayrollFormulaVariable) => {
    const source = variable.source;
    setForm((current) => ({
      ...current,
      lines: current.lines.map((line, index) => {
        if (index !== activeLineIndex) return line;
        const spacer = line.expression.trim() ? " " : "";
        return { ...line, expression: `${line.expression}${spacer}${source}` };
      }),
    }));
  };

  const addLine = () => {
    setForm((current) => ({
      ...current,
      lines: [...current.lines, defaultLine((current.lines.length + 1) * 10)],
    }));
    setActiveLineIndex(form.lines.length);
  };

  const removeLine = (index: number) => {
    setForm((current) => ({
      ...current,
      lines: current.lines.filter((_, lineIndex) => lineIndex !== index),
    }));
    setActiveLineIndex(0);
  };

  return (
    <FeaturePage
      title="Công thức lương"
      description="Quản lý version công thức, kiểm tra biến lương và gửi duyệt trước khi áp dụng."
      width="wide"
    >
      <div className="grid gap-4 lg:grid-cols-[1fr_320px]">
        <Card
          title="Danh sách công thức"
          actions={
            <div className="flex flex-wrap gap-2">
              <Select
                aria-label="Lọc trạng thái"
                className="min-w-[220px]"
                value={statusFilter}
                options={statusOptions}
                onChange={(event) => setStatusFilter(event.target.value)}
              />
              <Button variant="secondary" onClick={() => void loadData()} disabled={loading}>
                <RefreshCw size={16} />
                Làm mới
              </Button>
              <Button onClick={openCreate}>
                <Plus size={16} />
                Tạo công thức
              </Button>
            </div>
          }
        >
          <div className="overflow-x-auto">
            <table className="min-w-full text-left text-sm">
              <thead className="border-b border-[var(--hicas-border)] text-xs uppercase text-[var(--hicas-text-secondary)]">
                <tr>
                  <th className="px-3 py-3">Công thức</th>
                  <th className="px-3 py-3">Version</th>
                  <th className="px-3 py-3">Hiệu lực</th>
                  <th className="px-3 py-3">Dòng</th>
                  <th className="px-3 py-3">Trạng thái</th>
                  <th className="px-3 py-3 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--hicas-border)]">
                {loading ? (
                  <tr>
                    <td colSpan={6} className="px-3 py-8 text-center text-[var(--hicas-text-secondary)]">
                      Đang tải dữ liệu...
                    </td>
                  </tr>
                ) : formulas.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-3 py-8 text-center text-[var(--hicas-text-secondary)]">
                      Chưa có công thức lương phù hợp.
                    </td>
                  </tr>
                ) : (
                  formulas.map((formula) => (
                    <tr key={formula.id} className="align-top">
                      <td className="px-3 py-3">
                        <p className="font-semibold text-[var(--hicas-text-main)]">{formula.formulaName}</p>
                        <p className="text-xs text-[var(--hicas-text-secondary)]">{formula.formulaCode}</p>
                      </td>
                      <td className="px-3 py-3">
                        <p className="font-semibold">v{formula.version}</p>
                        <p className="text-xs text-[var(--hicas-text-secondary)]">{formula.versionCode || "Chưa đặt mã"}</p>
                      </td>
                      <td className="px-3 py-3">
                        <p>{formatDate(formula.effectiveFrom)}</p>
                        <p className="text-xs text-[var(--hicas-text-secondary)]">
                          {formula.effectiveTo ? `đến ${formatDate(formula.effectiveTo)}` : "Không giới hạn"}
                        </p>
                      </td>
                      <td className="px-3 py-3">{formula.lines?.length ?? 0}</td>
                      <td className="px-3 py-3">
                        <Badge variant={statusVariant(String(formula.status))}>
                          {formula.statusText || getFormulaStatusLabel(formula.status)}
                        </Badge>
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex flex-wrap justify-end gap-2">
                          <Button size="sm" variant="secondary" onClick={() => openEdit(formula)}>
                            <GitBranch size={14} />
                            Mở
                          </Button>
                          <Button size="sm" variant="secondary" onClick={() => void handleClone(formula)}>
                            <Copy size={14} />
                            Clone
                          </Button>
                          {formula.status === "Approved" ? (
                            <Button size="sm" onClick={() => void handleActivate(formula)}>
                              <CheckCircle2 size={14} />
                              Kích hoạt
                            </Button>
                          ) : null}
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </Card>

        <Card title="Biến lương đang dùng">
          <div className="space-y-3">
            <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-[var(--hicas-bg-soft)] p-4">
              <div className="flex gap-3">
                <ShieldCheck size={20} className="mt-0.5 text-[var(--hicas-orange)]" />
                <p className="text-sm leading-6 text-[var(--hicas-text-secondary)]">
                  Công thức chỉ nên dùng các biến đã được hệ thống cho phép. Khi tính lương, payroll sẽ lấy dữ liệu theo kỳ và snapshot lại kết quả.
                </p>
              </div>
            </div>
            <div className="max-h-[520px] space-y-2 overflow-y-auto pr-1">
              {activeVariables.slice(0, 36).map((variable) => (
                <button
                  key={`${variable.code}-${variable.source}`}
                  type="button"
                  className="w-full rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white px-3 py-2 text-left text-sm transition hover:border-[var(--hicas-orange)] hover:bg-orange-50"
                  onClick={() => appendVariableToActiveLine(variable)}
                >
                  <span className="block font-semibold text-[var(--hicas-text-main)]">{variable.source}</span>
                  <span className="block text-xs text-[var(--hicas-text-secondary)]">{variable.description}</span>
                </button>
              ))}
            </div>
          </div>
        </Card>
      </div>

      <DrawerForm
        open={drawerOpen}
        title={selectedFormula ? `Công thức ${selectedFormula.formulaCode}` : "Tạo công thức lương"}
        description="Sửa các dòng tính lương, kiểm tra biến rồi lưu bản nháp hoặc gửi duyệt."
        width="xl"
        onClose={() => setDrawerOpen(false)}
        footer={
          <div className="flex w-full flex-wrap justify-end gap-2">
            <Button variant="secondary" onClick={() => void handleValidate()} disabled={saving}>
              <Wand2 size={16} />
              Kiểm tra
            </Button>
            <Button variant="secondary" onClick={() => setDrawerOpen(false)} disabled={saving}>
              Đóng
            </Button>
            {selectedFormula && selectedFormula.status !== "Archived" ? (
              <Button variant="secondary" onClick={() => void handleArchive(selectedFormula)} disabled={saving}>
                <Archive size={16} />
                Lưu trữ
              </Button>
            ) : null}
            {selectedFormula && ["Draft", "RevisionRequired"].includes(String(selectedFormula.status)) ? (
              <Button variant="secondary" onClick={() => void handleSubmit(selectedFormula)} disabled={saving}>
                <Send size={16} />
                Gửi duyệt
              </Button>
            ) : null}
            {selectedFormula && selectedFormula.status === "Approved" ? (
              <Button onClick={() => void handleActivate(selectedFormula)} disabled={saving}>
                <CheckCircle2 size={16} />
                Kích hoạt
              </Button>
            ) : null}
            {canEdit(selectedFormula) ? (
              <Button onClick={() => void handleSave()} isLoading={saving}>
                <Save size={16} />
                Lưu bản nháp
              </Button>
            ) : null}
          </div>
        }
      >
        <div className="space-y-5">
          <div className="grid gap-4 md:grid-cols-2">
            <Input
              label="Mã công thức"
              value={form.formulaCode}
              onChange={(event) => setForm((current) => ({ ...current, formulaCode: event.target.value.toUpperCase() }))}
              disabled={Boolean(selectedFormula) || !canEdit(selectedFormula)}
            />
            <Input
              label="Tên công thức"
              value={form.formulaName}
              onChange={(event) => setForm((current) => ({ ...current, formulaName: event.target.value }))}
              disabled={!canEdit(selectedFormula)}
            />
            <Input
              label="Mã phiên bản"
              value={form.versionCode ?? ""}
              onChange={(event) => setForm((current) => ({ ...current, versionCode: event.target.value }))}
              disabled={!canEdit(selectedFormula)}
              placeholder="Ví dụ: KPI_PAYOUT_V2"
            />
            <div className="grid gap-3 sm:grid-cols-2">
              <Input
                label="Hiệu lực từ"
                type="date"
                value={toDateInput(form.effectiveFrom)}
                onChange={(event) => setForm((current) => ({ ...current, effectiveFrom: event.target.value }))}
                disabled={!canEdit(selectedFormula)}
              />
              <Input
                label="Hiệu lực đến"
                type="date"
                value={toDateInput(form.effectiveTo)}
                onChange={(event) => setForm((current) => ({ ...current, effectiveTo: event.target.value }))}
                disabled={!canEdit(selectedFormula)}
              />
            </div>
            <Select
              label="Loại hợp đồng"
              value={form.contractType ?? ""}
              options={contractTypeOptions}
              onChange={(event) => setForm((current) => ({ ...current, contractType: emptyToNull(event.target.value) }))}
              disabled={!canEdit(selectedFormula)}
            />
            <Select
              label="Hình thức trả lương"
              value={form.payBasis ?? ""}
              options={payBasisOptions}
              onChange={(event) => setForm((current) => ({ ...current, payBasis: emptyToNull(event.target.value) }))}
              disabled={!canEdit(selectedFormula)}
            />
            <Select
              label="Loại nhân sự"
              value={form.employeeType ?? ""}
              options={employeeTypeOptions}
              onChange={(event) => setForm((current) => ({ ...current, employeeType: emptyToNull(event.target.value) }))}
              disabled={!canEdit(selectedFormula)}
            />
            <Input
              label="Ghi chú chung"
              value={form.expression ?? ""}
              onChange={(event) => setForm((current) => ({ ...current, expression: event.target.value }))}
              disabled={!canEdit(selectedFormula)}
              placeholder="Ghi chú nội bộ cho version công thức"
            />
          </div>

          {validation ? (
            <ValidationPanel validation={validation} />
          ) : null}

          <Card
            title="Dòng công thức"
            actions={
              canEdit(selectedFormula) ? (
                <Button size="sm" variant="secondary" onClick={addLine}>
                  <Plus size={14} />
                  Thêm dòng
                </Button>
              ) : null
            }
          >
            <div className="space-y-4">
              {form.lines.map((line, index) => (
                <div
                  key={`${index}-${line.componentCode}`}
                  className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-4"
                >
                  <div className="grid gap-3 lg:grid-cols-[100px_1fr_2fr_auto]">
                    <Input
                      label="Thứ tự"
                      type="number"
                      value={line.calculationOrder}
                      onFocus={() => setActiveLineIndex(index)}
                      onChange={(event) => updateLine(index, { calculationOrder: Number(event.target.value) })}
                      disabled={!canEdit(selectedFormula)}
                    />
                    <Input
                      label="Mã khoản"
                      value={line.componentCode}
                      onFocus={() => setActiveLineIndex(index)}
                      onChange={(event) => updateLine(index, { componentCode: event.target.value.toUpperCase() })}
                      disabled={!canEdit(selectedFormula)}
                      placeholder="Ví dụ: PROJECT_BONUS"
                    />
                    <label className="block">
                      <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
                        Biểu thức
                      </span>
                      <textarea
                        value={line.expression}
                        onFocus={() => setActiveLineIndex(index)}
                        onChange={(event) => updateLine(index, { expression: event.target.value })}
                        disabled={!canEdit(selectedFormula)}
                        rows={2}
                        className="hicas-input min-h-[72px] w-full resize-y py-2"
                        placeholder="Ví dụ: kpi_bonus_amount * kpi_score / 100"
                      />
                    </label>
                    {canEdit(selectedFormula) ? (
                      <button
                        type="button"
                        className="mt-6 inline-flex h-11 w-11 items-center justify-center rounded-[var(--radius-md)] border border-red-200 text-red-600 hover:bg-red-50"
                        onClick={() => removeLine(index)}
                        aria-label="Xóa dòng"
                      >
                        <Trash2 size={16} />
                      </button>
                    ) : null}
                  </div>
                  <div className="mt-3 grid gap-3 md:grid-cols-5">
                    <ToggleLine
                      label="Khoản cộng"
                      checked={line.isGrossComponent}
                      disabled={!canEdit(selectedFormula)}
                      onChange={(checked) => updateLine(index, { isGrossComponent: checked })}
                    />
                    <ToggleLine
                      label="Chịu thuế"
                      checked={line.isTaxable}
                      disabled={!canEdit(selectedFormula)}
                      onChange={(checked) => updateLine(index, { isTaxable: checked })}
                    />
                    <ToggleLine
                      label="Tính bảo hiểm"
                      checked={line.isInsuranceBased}
                      disabled={!canEdit(selectedFormula)}
                      onChange={(checked) => updateLine(index, { isInsuranceBased: checked })}
                    />
                    <ToggleLine
                      label="Khoản trừ"
                      checked={line.isDeduction}
                      disabled={!canEdit(selectedFormula)}
                      onChange={(checked) => updateLine(index, { isDeduction: checked })}
                    />
                    <ToggleLine
                      label="Lưu snapshot"
                      checked={line.isSnapshotRequired}
                      disabled={!canEdit(selectedFormula)}
                      onChange={(checked) => updateLine(index, { isSnapshotRequired: checked })}
                    />
                  </div>
                  <Input
                    label="Ghi chú dòng"
                    value={line.note ?? ""}
                    onFocus={() => setActiveLineIndex(index)}
                    onChange={(event) => updateLine(index, { note: event.target.value })}
                    disabled={!canEdit(selectedFormula)}
                    className="mt-3"
                  />
                </div>
              ))}
            </div>
          </Card>
        </div>
      </DrawerForm>
    </FeaturePage>
  );
};

const ToggleLine = ({
  label,
  checked,
  disabled,
  onChange,
}: {
  label: string;
  checked: boolean;
  disabled?: boolean;
  onChange: (checked: boolean) => void;
}) => (
  <label className="flex items-center gap-2 rounded-[var(--radius-md)] border border-[var(--hicas-border)] px-3 py-2 text-sm">
    <input
      type="checkbox"
      checked={checked}
      disabled={disabled}
      onChange={(event) => onChange(event.target.checked)}
    />
    <span>{label}</span>
  </label>
);

const ValidationPanel = ({ validation }: { validation: PayrollFormulaValidationResult }) => (
  <div
    className={`rounded-[var(--radius-lg)] border px-4 py-3 text-sm ${
      validation.isValid
        ? "border-emerald-200 bg-emerald-50 text-emerald-800"
        : "border-amber-200 bg-amber-50 text-amber-800"
    }`}
  >
    <p className="font-semibold">
      {validation.isValid ? "Công thức hợp lệ" : "Công thức cần kiểm tra"}
    </p>
    {validation.errors.length > 0 ? (
      <ul className="mt-2 list-disc space-y-1 pl-5">
        {validation.errors.map((item) => (
          <li key={item}>{item}</li>
        ))}
      </ul>
    ) : null}
    {validation.warnings.length > 0 ? (
      <ul className="mt-2 list-disc space-y-1 pl-5">
        {validation.warnings.map((item) => (
          <li key={item}>{item}</li>
        ))}
      </ul>
    ) : null}
  </div>
);

function toForm(formula: PayrollFormula): UpsertPayrollFormulaRequest {
  return {
    formulaCode: formula.formulaCode,
    formulaName: formula.formulaName,
    expression: formula.expression ?? "",
    contractType: formula.contractType ?? null,
    payBasis: formula.payBasis ?? null,
    employeeType: formula.employeeType ?? null,
    deptId: formula.deptId ?? null,
    positionId: formula.positionId ?? null,
    jobLevelId: formula.jobLevelId ?? null,
    versionCode: formula.versionCode ?? "",
    effectiveFrom: toDateInput(formula.effectiveFrom),
    effectiveTo: toDateInput(formula.effectiveTo),
    lines: (formula.lines ?? []).map((line) => ({
      id: line.id,
      salaryComponentTypeId: line.salaryComponentTypeId ?? null,
      componentCode: line.componentCode,
      componentName: line.componentName ?? null,
      expression: line.expression,
      calculationOrder: line.calculationOrder,
      isGrossComponent: line.isGrossComponent,
      isTaxable: line.isTaxable,
      isInsuranceBased: line.isInsuranceBased,
      isDeduction: line.isDeduction,
      isSnapshotRequired: line.isSnapshotRequired,
      note: line.note ?? "",
    })),
  };
}

function normalizePayload(form: UpsertPayrollFormulaRequest): UpsertPayrollFormulaRequest {
  return {
    ...form,
    formulaCode: form.formulaCode.trim().toUpperCase(),
    formulaName: form.formulaName.trim(),
    versionCode: form.versionCode?.trim() || null,
    expression: form.expression?.trim() || null,
    contractType: emptyToNull(form.contractType),
    payBasis: emptyToNull(form.payBasis),
    employeeType: emptyToNull(form.employeeType),
    effectiveFrom: toDateInput(form.effectiveFrom) || toDateInput(new Date().toISOString()),
    effectiveTo: toDateInput(form.effectiveTo) || null,
    lines: form.lines.map((line, index) => ({
      ...line,
      componentCode: line.componentCode.trim().toUpperCase(),
      expression: line.expression.trim(),
      calculationOrder: Number(line.calculationOrder || (index + 1) * 10),
      note: line.note?.trim() || null,
    })),
  };
}

function toDateInput(value?: string | null) {
  if (!value) return "";
  return value.slice(0, 10);
}

function emptyToNull<T extends string | null | undefined>(value: T) {
  return value && String(value).trim() ? String(value).trim() : null;
}

function formatDate(value?: string | null) {
  if (!value) return "Chưa có";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleDateString("vi-VN");
}

function canEdit(formula: PayrollFormula | null) {
  if (!formula) return true;
  return ["Draft", "RevisionRequired"].includes(String(formula.status));
}

function getFormulaStatusLabel(status: PayrollFormulaStatus | string) {
  const map: Record<string, string> = {
    Pending: "Chờ duyệt",
    Approved: "Đã duyệt",
    Rejected: "Từ chối",
    Expired: "Hết hiệu lực",
    Draft: "Bản nháp",
    PendingDirectorApproval: "Chờ giám đốc duyệt",
    RevisionRequired: "Cần chỉnh sửa",
    Active: "Đang áp dụng",
    Archived: "Lưu trữ",
  };
  return map[String(status)] || String(status);
}

function getErrorMessage(error: unknown) {
  const maybe = error as { response?: { data?: { message?: string } }; message?: string };
  return maybe.response?.data?.message || maybe.message || "Vui lòng thử lại.";
}
