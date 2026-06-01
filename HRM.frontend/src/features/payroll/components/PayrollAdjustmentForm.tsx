import { Plus } from "lucide-react";
import { Button } from "../../../components/ui";
import type { PayrollAdjustmentFormProps, PayrollAdjustmentType } from "../types/payroll";
import { adjustmentTypes } from "../utils";

export const PayrollAdjustmentForm = ({
  form,
  period,
  saving = false,
  onChange,
  onSubmit,
}: PayrollAdjustmentFormProps) => (
  <form onSubmit={onSubmit} className="grid gap-4 md:grid-cols-2">
    <label className="block">
      <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
        EmployeeId
      </span>
      <input
        type="number"
        min={1}
        required
        value={form.employeeId || ""}
        onChange={(event) => onChange({ employeeId: Number(event.target.value) })}
        className="hicas-input w-full"
        placeholder="VD: 1"
      />
    </label>

    <label className="block">
      <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
        Loại điều chỉnh
      </span>
      <select
        value={form.adjustmentType}
        onChange={(event) => onChange({ adjustmentType: event.target.value as PayrollAdjustmentType })}
        className="hicas-input w-full"
      >
        {adjustmentTypes.map((type) => (
          <option key={type.value} value={type.value}>
            {type.label}
          </option>
        ))}
      </select>
    </label>

    <label className="block">
      <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
        Từ kỳ hiệu lực
      </span>
      <input
        type="text"
        value={form.effectiveFromMonth ?? ""}
        onChange={(event) => onChange({ effectiveFromMonth: event.target.value })}
        className="hicas-input w-full"
        placeholder="MM-yyyy"
      />
    </label>

    <label className="block">
      <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
        Đến kỳ hiệu lực
      </span>
      <input
        type="text"
        value={form.effectiveToMonth ?? ""}
        onChange={(event) => onChange({ effectiveToMonth: event.target.value })}
        className="hicas-input w-full"
        placeholder="MM-yyyy"
      />
    </label>

    <label className="block">
      <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
        Số tiền
      </span>
      <input
        type="number"
        required
        value={form.amount || ""}
        onChange={(event) => onChange({ amount: Number(event.target.value) })}
        className="hicas-input w-full"
        placeholder="VD: 1500000"
      />
    </label>

    <div className="grid grid-cols-3 gap-3 rounded-[var(--radius-lg)] border border-[var(--hicas-border)] p-3 text-sm">
      <label className="flex items-center gap-2">
        <input
          type="checkbox"
          checked={form.isTaxable}
          onChange={(event) => onChange({ isTaxable: event.target.checked })}
          className="h-4 w-4 accent-[var(--hicas-orange)]"
        />
        Chịu thuế
      </label>
      <label className="flex items-center gap-2">
        <input
          type="checkbox"
          checked={form.isInsuranceBased}
          onChange={(event) => onChange({ isInsuranceBased: event.target.checked })}
          className="h-4 w-4 accent-[var(--hicas-orange)]"
        />
        Tính BH
      </label>
      <label className="flex items-center gap-2">
        <input
          type="checkbox"
          checked={form.isDeduction}
          onChange={(event) => onChange({ isDeduction: event.target.checked })}
          className="h-4 w-4 accent-[var(--hicas-orange)]"
        />
        Khoản giảm hợp lệ
      </label>
    </div>

    <label className="block md:col-span-2">
      <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
        Lý do
      </span>
      <textarea
        required
        value={form.reason}
        onChange={(event) => onChange({ reason: event.target.value })}
        className="hicas-input min-h-[108px] w-full py-3"
        placeholder="VD: Truy lĩnh tăng lương theo phụ lục có hiệu lực từ 04-2026."
      />
      <p className="mt-2 text-xs text-[var(--hicas-text-secondary)]">
        Không dùng mục này cho đi muộn, về sớm, vắng mặt hoặc rời vị trí. Các lỗi hiện diện phải xử lý ở bảng công và biên bản vi phạm.
      </p>
    </label>

    <div className="md:col-span-2">
      <Button type="submit" isLoading={saving}>
        <Plus size={16} />
        Tạo khoản điều chỉnh cho kỳ {period}
      </Button>
    </div>
  </form>
);
