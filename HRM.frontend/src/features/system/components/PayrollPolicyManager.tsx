import type { ChangeEvent, FormEvent } from "react";
import { useMemo, useState } from "react";
import { CopyPlus, Save } from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card, DataTable } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import { formatOptionalMoney } from "../../../utils";
import { usePayrollPolicies } from "../hooks/usePayrollPolicies";
import { PayrollFeatureTogglePanel } from "./PayrollFeatureTogglePanel";
import type { PayrollPolicy, PayrollPolicyPayload } from "../types/payrollPolicy";
import { PayrollPolicyType, PayrollPolicyValueType } from "../types/payrollPolicy";

const policyTypeOptions = [
  { value: PayrollPolicyType.Overtime, label: "Làm thêm giờ" },
  { value: PayrollPolicyType.PitTax, label: "Thuế TNCN" },
  { value: PayrollPolicyType.Insurance, label: "Bảo hiểm" },
  { value: PayrollPolicyType.Allowance, label: "Phụ cấp" },
  { value: PayrollPolicyType.Deduction, label: "Điều chỉnh giảm hợp lệ" },
  { value: PayrollPolicyType.Seniority, label: "Phụ cấp thâm niên" },
  { value: PayrollPolicyType.MinimumWage, label: "Lương tối thiểu vùng" },
  { value: PayrollPolicyType.KpiBonus, label: "Thưởng KPI" },
];

const valueTypeOptions = [
  { value: PayrollPolicyValueType.RatePercent, label: "Tỷ lệ %" },
  { value: PayrollPolicyValueType.Amount, label: "Số tiền" },
  { value: PayrollPolicyValueType.Bracket, label: "Bậc/khoảng" },
  { value: PayrollPolicyValueType.Formula, label: "Công thức JSON" },
];

const today = new Date().toISOString().slice(0, 10);

const emptyForm: PayrollPolicyPayload = {
  policyType: PayrollPolicyType.Overtime,
  code: "",
  name: "",
  valueType: PayrollPolicyValueType.RatePercent,
  ratePercent: 150,
  amount: null,
  fromAmount: null,
  toAmount: null,
  quickDeduction: null,
  formulaJson: "",
  effectiveFrom: today,
  effectiveTo: null,
  version: 1,
  isActive: true,
  description: "",
};

type MessageState = {
  type: "success" | "error";
  text: string;
};

const toDateInput = (value?: string | null) => (value ? value.slice(0, 10) : "");
const toNumberOrNull = (value: string) => (value === "" ? null : Number(value));

const getPolicyTypeLabel = (value: PayrollPolicy["policyType"]) =>
  policyTypeOptions.find((item) => item.value === value)?.label ?? "Khác";

const getRangeLabel = (policy: PayrollPolicy) => {
  const unlimited = policy.toAmount == null ? "Không giới hạn" : formatOptionalMoney(policy.toAmount, "--");
  if (policy.policyType === PayrollPolicyType.Seniority) {
    return `${policy.fromAmount ?? 0} - ${policy.toAmount ?? "Không giới hạn"} tháng`;
  }

  return `${formatOptionalMoney(policy.fromAmount, "--")} - ${unlimited}`;
};

export const PayrollPolicyManager = () => {
  const { policies, loading, savePolicy, setStatus } = usePayrollPolicies();
  const [form, setForm] = useState<PayrollPolicyPayload>(emptyForm);
  const [editingId, setEditingId] = useState<number | undefined>();
  const [message, setMessage] = useState<MessageState | null>(null);

  const sortedPolicies = useMemo(
    () =>
      [...policies].sort((a, b) =>
        `${a.policyType}-${a.code}`.localeCompare(`${b.policyType}-${b.code}`),
      ),
    [policies],
  );

  const handleChange = (
    event: ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>,
  ) => {
    const { name, value, type } = event.target;
    const checked = type === "checkbox" ? (event.target as HTMLInputElement).checked : undefined;

    setForm((prev) => {
      if (name === "policyType" || name === "valueType" || name === "version") {
        return { ...prev, [name]: Number(value) };
      }
      if (["ratePercent", "amount", "fromAmount", "toAmount", "quickDeduction"].includes(name)) {
        return { ...prev, [name]: toNumberOrNull(value) };
      }
      if (name === "isActive") {
        return { ...prev, isActive: Boolean(checked) };
      }
      if (name === "effectiveTo") {
        return { ...prev, effectiveTo: value || null };
      }
      return { ...prev, [name]: value };
    });
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    try {
      const payload = {
        ...form,
        code: form.code.trim().toUpperCase(),
        effectiveTo: form.effectiveTo || null,
      };
      const res = await savePolicy(payload, editingId);
      setMessage({
        type: "success",
        text: res.message || "Đã lưu chính sách lương.",
      });
      setForm(emptyForm);
      setEditingId(undefined);
    } catch (error: unknown) {
      setMessage({
        type: "error",
        text:
          (error as { response?: { data?: { message?: string } } }).response?.data?.message ||
          String(error),
      });
    }
  };

  const editPolicy = (policy: PayrollPolicy) => {
    setEditingId(policy.id);
    setForm({
      policyType: policy.policyType,
      code: policy.code,
      name: policy.name,
      valueType: policy.valueType,
      ratePercent: policy.ratePercent ?? null,
      amount: policy.amount ?? null,
      fromAmount: policy.fromAmount ?? null,
      toAmount: policy.toAmount ?? null,
      quickDeduction: policy.quickDeduction ?? null,
      formulaJson: policy.formulaJson ?? "",
      effectiveFrom: today,
      effectiveTo: null,
      version: policy.version + 1,
      isActive: policy.isActive,
      description: policy.description ?? "",
    });
  };

  const renderValue = (policy: PayrollPolicy) => {
    if (policy.valueType === PayrollPolicyValueType.RatePercent) {
      return `${policy.ratePercent ?? 0}%`;
    }
    if (policy.valueType === PayrollPolicyValueType.Amount) {
      return formatOptionalMoney(policy.amount, "--");
    }
    if (policy.valueType === PayrollPolicyValueType.Bracket) {
      const value = policy.amount != null
        ? formatOptionalMoney(policy.amount, "--")
        : `${policy.ratePercent ?? 0}%`;
      return `${getRangeLabel(policy)} | ${value}`;
    }
    return "JSON";
  };

  const columns: Array<DataTableColumn<PayrollPolicy>> = [
    {
      key: "policyType",
      header: "Loại",
      render: (row) => <Badge variant="orange">{getPolicyTypeLabel(row.policyType)}</Badge>,
    },
    {
      key: "code",
      header: "Mã",
      render: (row) => (
        <span className="font-mono font-semibold text-[var(--hicas-text-main)]">{row.code}</span>
      ),
    },
    {
      key: "name",
      header: "Tên chính sách",
      render: (row) => (
        <div>
          <p className="font-semibold text-[var(--hicas-text-main)]">{row.name}</p>
          {row.description && (
            <p className="mt-1 text-xs text-[var(--hicas-text-secondary)]">{row.description}</p>
          )}
        </div>
      ),
    },
    { key: "value", header: "Giá trị", render: renderValue },
    {
      key: "effective",
      header: "Hiệu lực",
      render: (row) => (
        <span className="text-sm">
          {toDateInput(row.effectiveFrom)} - {toDateInput(row.effectiveTo) || "Không giới hạn"}
        </span>
      ),
    },
    { key: "version", header: "Version", render: (row) => `v${row.version}` },
    {
      key: "status",
      header: "Trạng thái",
      render: (row) => (
        <Badge variant={row.isActive ? "success" : "neutral"}>
          {row.isActive ? "Đang dùng" : "Tạm tắt"}
        </Badge>
      ),
    },
    {
      key: "actions",
      header: "Thao tác",
      render: (row) => (
        <div className="flex flex-wrap gap-2">
          <Button size="sm" variant="secondary" onClick={() => editPolicy(row)}>
            Tạo version
          </Button>
          <Button size="sm" variant="ghost" onClick={() => setStatus(row.id, !row.isActive)}>
            {row.isActive ? "Tắt" : "Bật"}
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="Chính sách lương"
        description="Quản lý các chính sách lương đang áp dụng."
        breadcrumb={[
          { label: "Cấu hình hệ thống" },
          { label: "Chính sách lương" },
        ]}
      />

      <PayrollFeatureTogglePanel />

      {message && (
        <div
          className={`rounded-2xl border px-4 py-3 text-sm font-medium ${
            message.type === "error"
              ? "border-[var(--hicas-danger)] bg-[var(--hicas-danger-soft)] text-[var(--hicas-danger)]"
              : "border-[var(--hicas-success)] bg-[var(--hicas-success-soft)] text-[var(--hicas-success)]"
          }`}
        >
          {message.text}
        </div>
      )}

      <Card
        title={editingId ? "Tạo phiên bản chính sách mới" : "Thêm chính sách lương"}
        description="Nhập giá trị và thời gian áp dụng cho chính sách."
        actions={<CopyPlus size={20} className="text-[var(--hicas-orange)]" />}
      >
        <form onSubmit={handleSubmit} className="grid gap-4 md:grid-cols-4">
          <label className="block">
            <span className="mb-2 block text-sm font-semibold">Loại chính sách</span>
            <select
              name="policyType"
              value={form.policyType}
              onChange={handleChange}
              className="hicas-select w-full"
            >
              {policyTypeOptions.map((item) => (
                <option key={item.value} value={item.value}>
                  {item.label}
                </option>
              ))}
            </select>
          </label>

          <label className="block">
            <span className="mb-2 block text-sm font-semibold">Mã chính sách *</span>
            <input
              required
              name="code"
              value={form.code}
              onChange={handleChange}
              className="hicas-input w-full font-mono uppercase"
              placeholder="SENIORITY_12M"
            />
          </label>

          <label className="block">
            <span className="mb-2 block text-sm font-semibold">Tên chính sách *</span>
            <input
              required
              name="name"
              value={form.name}
              onChange={handleChange}
              className="hicas-input w-full"
              placeholder="Phụ cấp thâm niên từ 12 tháng"
            />
          </label>

          <label className="block">
            <span className="mb-2 block text-sm font-semibold">Kiểu giá trị</span>
            <select
              name="valueType"
              value={form.valueType}
              onChange={handleChange}
              className="hicas-select w-full"
            >
              {valueTypeOptions.map((item) => (
                <option key={item.value} value={item.value}>
                  {item.label}
                </option>
              ))}
            </select>
          </label>

          <label className="block">
            <span className="mb-2 block text-sm font-semibold">Tỷ lệ %</span>
            <input
              type="number"
              step="0.01"
              name="ratePercent"
              value={form.ratePercent ?? ""}
              onChange={handleChange}
              className="hicas-input w-full"
            />
          </label>

          <label className="block">
            <span className="mb-2 block text-sm font-semibold">Số tiền</span>
            <input
              type="number"
              step="1000"
              name="amount"
              value={form.amount ?? ""}
              onChange={handleChange}
              className="hicas-input w-full"
            />
          </label>

          <label className="block">
            <span className="mb-2 block text-sm font-semibold">Từ mức</span>
            <input
              type="number"
              step={form.policyType === PayrollPolicyType.Seniority ? "1" : "1000"}
              name="fromAmount"
              value={form.fromAmount ?? ""}
              onChange={handleChange}
              className="hicas-input w-full"
              placeholder={form.policyType === PayrollPolicyType.Seniority ? "Số tháng" : undefined}
            />
          </label>

          <label className="block">
            <span className="mb-2 block text-sm font-semibold">Đến mức</span>
            <input
              type="number"
              step={form.policyType === PayrollPolicyType.Seniority ? "1" : "1000"}
              name="toAmount"
              value={form.toAmount ?? ""}
              onChange={handleChange}
              className="hicas-input w-full"
              placeholder="Để trống nếu không giới hạn"
            />
          </label>

          <label className="block">
            <span className="mb-2 block text-sm font-semibold">Giảm trừ nhanh</span>
            <input
              type="number"
              step="1000"
              name="quickDeduction"
              value={form.quickDeduction ?? ""}
              onChange={handleChange}
              className="hicas-input w-full"
            />
          </label>

          <label className="block">
            <span className="mb-2 block text-sm font-semibold">Hiệu lực từ</span>
            <input
              required
              type="date"
              name="effectiveFrom"
              value={form.effectiveFrom}
              onChange={handleChange}
              className="hicas-input w-full"
            />
          </label>

          <label className="block">
            <span className="mb-2 block text-sm font-semibold">Hiệu lực đến</span>
            <input
              type="date"
              name="effectiveTo"
              value={form.effectiveTo ?? ""}
              onChange={handleChange}
              className="hicas-input w-full"
            />
          </label>

          <label className="block">
            <span className="mb-2 block text-sm font-semibold">Version</span>
            <input
              type="number"
              min="1"
              name="version"
              value={form.version}
              onChange={handleChange}
              className="hicas-input w-full"
            />
          </label>

          {form.valueType === PayrollPolicyValueType.Formula && (
            <label className="block md:col-span-2">
              <span className="mb-2 block text-sm font-semibold">Công thức JSON</span>
              <textarea
                name="formulaJson"
                value={form.formulaJson ?? ""}
                onChange={handleChange}
                className="hicas-textarea min-h-[96px] w-full font-mono"
                placeholder='{"ratePerYear":1,"maxRate":10}'
              />
            </label>
          )}

          <label className="block md:col-span-2">
            <span className="mb-2 block text-sm font-semibold">Mô tả</span>
            <textarea
              name="description"
              value={form.description ?? ""}
              onChange={handleChange}
              className="hicas-textarea min-h-[96px] w-full"
            />
          </label>

          <label className="flex items-center gap-2 rounded-2xl border border-[var(--hicas-border)] px-4 py-3 text-sm font-medium">
            <input
              type="checkbox"
              name="isActive"
              checked={form.isActive}
              onChange={handleChange}
              className="accent-[var(--hicas-orange)]"
            />
            Đang áp dụng
          </label>

          <div className="flex flex-wrap gap-3 md:col-span-3">
            <Button type="submit" iconLeft={<Save size={17} />}>
              {editingId ? "Tạo phiên bản mới" : "Thêm chính sách"}
            </Button>
            {editingId && (
              <Button
                type="button"
                variant="secondary"
                onClick={() => {
                  setEditingId(undefined);
                  setForm(emptyForm);
                }}
              >
                Hủy sửa
              </Button>
            )}
          </div>
        </form>
      </Card>

      <DataTable
        columns={columns}
        data={sortedPolicies}
        loading={loading}
        rowKey={(row) => row.id}
        emptyTitle="Chưa có chính sách lương"
        emptyDescription="Hãy thêm chính sách làm thêm, thuế, bảo hiểm, phụ cấp hoặc phụ cấp thâm niên đầu tiên."
      />
    </div>
  );
};
