import { Calculator, Download, RefreshCw } from "lucide-react";
import { Button } from "../../../components/ui";
import type { PayrollPeriodFilterProps } from "../types/payroll";

export const PayrollPeriodFilter = ({
  month,
  year,
  loading = false,
  calculating = false,
  canCalculate = true,
  canExport = false,
  showCalculate = false,
  showExport = true,
  exportLabel = "Kết xuất",
  onMonthChange,
  onYearChange,
  onRefresh,
  onCalculate,
  onExport,
}: PayrollPeriodFilterProps) => (
  <div className="grid gap-4 md:grid-cols-[160px_160px_auto_auto_auto]">
    <label className="block">
      <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
        Tháng
      </span>
      <input
        type="number"
        min={1}
        max={12}
        value={month}
        onChange={(event) => onMonthChange(Number(event.target.value))}
        className="hicas-input w-full"
      />
    </label>
    <label className="block">
      <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
        Năm
      </span>
      <input
        type="number"
        min={2000}
        max={2100}
        value={year}
        onChange={(event) => onYearChange(Number(event.target.value))}
        className="hicas-input w-full"
      />
    </label>
    <div className="flex items-end">
      <Button type="button" variant="secondary" disabled={loading} onClick={onRefresh}>
        <RefreshCw size={16} />
        Tải lại
      </Button>
    </div>
    {showCalculate && (
      <div className="flex items-end">
        <Button
          type="button"
          disabled={loading || calculating || !canCalculate}
          onClick={onCalculate}
          isLoading={calculating}
        >
          <Calculator size={16} />
          Tổng hợp lương
        </Button>
      </div>
    )}
    {showExport && (
      <div className="flex items-end">
        <Button type="button" variant="secondary" disabled={loading || !canExport} onClick={onExport}>
          <Download size={16} />
          {exportLabel}
        </Button>
      </div>
    )}
  </div>
);
