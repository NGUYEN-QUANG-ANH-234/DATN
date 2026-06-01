import { FileSpreadsheet, RotateCcw } from "lucide-react";
import { Button, Card } from "../../../components/ui";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { useExternalTimesheetImport } from "../hooks/useExternalTimesheetImport";
import { usePayrollPeriod } from "../hooks/usePayrollPeriod";
import { formatMoney, formatNumber } from "../utils";
import { ExternalTimesheetPreviewTable } from "./ExternalTimesheetPreviewTable";
import { PayrollMetricCard } from "./PayrollMetricCard";

export const ExternalTimesheetImportPage = () => {
  const { month, year, period, setMonth, setYear } = usePayrollPeriod();
  const { sourceSystem, setSourceSystem, importState, parseFile, reset } =
    useExternalTimesheetImport(month, year);

  return (
    <FeaturePage
      title="Import giờ công CTV"
      description="Xem trước dữ liệu giờ công cộng tác viên/freelancer trước khi đưa vào payroll. Backend hiện đã có entity, phần API import/duyệt sẽ được nối ở bước tiếp theo."
      width="wide"
    >
      <Card
        title="Thông tin import"
        actions={
          <Button type="button" variant="secondary" onClick={reset}>
            <RotateCcw size={16} />
            Xóa xem trước
          </Button>
        }
      >
        <div className="grid gap-4 md:grid-cols-[160px_160px_1fr]">
          <label className="block">
            <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
              Tháng
            </span>
            <input
              type="number"
              min={1}
              max={12}
              value={month}
              onChange={(event) => setMonth(Number(event.target.value))}
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
              onChange={(event) => setYear(Number(event.target.value))}
              className="hicas-input w-full"
            />
          </label>
          <label className="block">
            <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
              Nguồn dữ liệu
            </span>
            <input
              value={sourceSystem}
              onChange={(event) => setSourceSystem(event.target.value)}
              className="hicas-input w-full"
            />
          </label>
        </div>

        <div className="mt-4 rounded-[var(--radius-lg)] border border-dashed border-[var(--hicas-border)] bg-[var(--hicas-bg-soft)] p-4">
          <label className="flex cursor-pointer flex-col items-center justify-center gap-2 rounded-[var(--radius-lg)] bg-white px-6 py-8 text-center text-sm text-[var(--hicas-text-secondary)]">
            <FileSpreadsheet size={28} className="text-[var(--hicas-orange)]" />
            <span className="font-semibold text-[var(--hicas-text-main)]">Chọn file CSV giờ công CTV</span>
            <span>
              Định dạng cột: collaboratorCode, collaboratorName, workDate, projectCode, taskCode,
              approvedHours, hourlyRate, note
            </span>
            <input
              type="file"
              accept=".csv,text/csv"
              className="hidden"
              onChange={(event) => {
                const file = event.target.files?.[0];
                if (file) void parseFile(file);
              }}
            />
          </label>
        </div>
      </Card>

      <div className="grid gap-4 md:grid-cols-4">
        <PayrollMetricCard label="Kỳ import" value={period} />
        <PayrollMetricCard label="File" value={importState.fileName || "Chưa chọn"} />
        <PayrollMetricCard label="Tổng giờ" value={formatNumber(importState.totalHours)} />
        <PayrollMetricCard label="Tổng tiền" value={formatMoney(importState.totalAmount)} strong />
      </div>

      <Card title="Dữ liệu xem trước">
        <div className="mb-4 rounded-[var(--radius-lg)] border border-[var(--hicas-warning-soft)] bg-[var(--hicas-warning-soft)] px-4 py-3 text-sm text-amber-800">
          Đây là trang frontend đã tách hook/type để chuẩn bị cho luồng CTV. Nút lưu chính thức sẽ được bật khi backend mở API import và phê duyệt ExternalTimesheetImport.
        </div>
        <ExternalTimesheetPreviewTable lines={importState.lines} />
      </Card>
    </FeaturePage>
  );
};
