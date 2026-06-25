import { LockKeyhole, Send } from "lucide-react";
import { Button, Card } from "../../../components/ui";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { usePayrollAggregation } from "../hooks/usePayrollAggregation";
import { usePayrollPeriod } from "../hooks/usePayrollPeriod";
import { formatMoney, sumSalarySlips } from "../utils";
import { PayrollMetricCard } from "./PayrollMetricCard";
import { PayrollPeriodFilter } from "./PayrollPeriodFilter";
import { PayrollPreflightPanel } from "./PayrollPreflightPanel";
import { SalarySlipDetailPanel } from "./SalarySlipDetailPanel";
import { SalarySlipTable } from "./SalarySlipTable";

export const PayrollAggregationPage = () => {
  const { month, year, period, setMonth, setYear } = usePayrollPeriod();
  const payroll = usePayrollAggregation(month, year, period);

  const totalDeductions =
    sumSalarySlips(payroll.slips, "employeeInsuranceAmount") +
    sumSalarySlips(payroll.slips, "pitAmount") +
    sumSalarySlips(payroll.slips, "otherDeductions");

  return (
    <FeaturePage
      title="Tổng hợp lương thưởng"
      description="Tổng hợp bảng lương nháp từ bảng công, KPI và chính sách lương."
      width="wide"
    >
      <Card title="Kỳ lương">
        <PayrollPeriodFilter
          month={month}
          year={year}
          loading={payroll.loading}
          calculating={payroll.calculating}
          canCalculate={payroll.canCalculatePayroll}
          canExport={payroll.selectedIds.length > 0}
          showCalculate={payroll.canManagePayroll}
          onMonthChange={setMonth}
          onYearChange={setYear}
          onRefresh={payroll.loadSlips}
          onCalculate={payroll.calculatePayroll}
          onExport={payroll.exportSelected}
        />
        <div className="mt-4 rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-[var(--hicas-bg-soft)] p-4">
          <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
            <div className="grid flex-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
              <PayrollMetricCard
                label="Trạng thái kỳ"
                value={payroll.runSummary?.statusText || "Chưa có dữ liệu"}
                strong
              />
              <PayrollMetricCard
                label="Số phiếu"
                value={`${payroll.runSummary?.slipCount ?? payroll.slips.length} phiếu`}
              />
              <PayrollMetricCard
                label="Tổng thực nhận"
                value={formatMoney(payroll.runSummary?.netSalary ?? sumSalarySlips(payroll.slips, "netSalary"))}
              />
              <PayrollMetricCard
                label="Chi phí công ty"
                value={formatMoney(payroll.runSummary?.totalCompanyCost ?? sumSalarySlips(payroll.slips, "totalCompanyCost"))}
              />
            </div>
            {payroll.canManagePayroll && (
              <div className="flex flex-wrap gap-2 xl:justify-end">
                <Button
                  type="button"
                  variant="secondary"
                  disabled={!payroll.canSubmitPayroll || payroll.runActionLoading}
                  isLoading={payroll.runActionLoading && payroll.canSubmitPayroll}
                  onClick={payroll.submitPayrollRun}
                >
                  <Send size={16} />
                  Gửi duyệt
                </Button>
                <Button
                  type="button"
                  disabled={!payroll.canLockPayroll || payroll.runActionLoading}
                  isLoading={payroll.runActionLoading && payroll.canLockPayroll}
                  onClick={payroll.lockPayrollRun}
                >
                  <LockKeyhole size={16} />
                  Chốt bảng lương
                </Button>
              </div>
            )}
          </div>
          {payroll.runSummary?.reviewNote && (
            <p className="mt-3 rounded-[var(--radius-sm)] bg-white px-3 py-2 text-sm text-[var(--hicas-text-secondary)]">
              Ghi chú duyệt: {payroll.runSummary.reviewNote}
            </p>
          )}
        </div>
      </Card>

      {payroll.canManagePayroll && (
        <PayrollPreflightPanel preflight={payroll.preflight} loading={payroll.preflightLoading} />
      )}

      <div className="grid gap-4 md:grid-cols-4">
        <PayrollMetricCard label="Số phiếu" value={String(payroll.slips.length)} />
        <PayrollMetricCard label="Tổng thu nhập" value={formatMoney(sumSalarySlips(payroll.slips, "grossIncome"))} />
        <PayrollMetricCard label="BH, thuế và điều chỉnh hợp lệ" value={formatMoney(totalDeductions)} />
        <PayrollMetricCard label="Tổng thực nhận" value={formatMoney(sumSalarySlips(payroll.slips, "netSalary"))} strong />
      </div>

      {payroll.calculationResult && (
        <Card title="Kết quả tổng hợp gần nhất">
          <div className="grid gap-3 text-sm md:grid-cols-3">
            <PayrollMetricCard label="Tạo mới" value={`${payroll.calculationResult.createdCount} phiếu`} />
            <PayrollMetricCard label="Bỏ qua" value={`${payroll.calculationResult.skippedCount} hồ sơ`} />
            <PayrollMetricCard
              label="Cảnh báo"
              value={`${payroll.calculationResult.warnings?.length ?? 0} cảnh báo`}
            />
          </div>
        </Card>
      )}

      <Card title="Danh sách bảng lương nháp">
        <SalarySlipTable
          slips={payroll.slips}
          selectedIds={payroll.selectedIds}
          loading={payroll.loading}
          onToggle={payroll.toggleSelected}
          onSelectMany={payroll.selectSlips}
          onUnselectMany={payroll.unselectSlips}
          onClearSelected={payroll.clearSelected}
          onOpenDetail={payroll.openDetail}
          emptyText={`Chưa có phiếu lương cho kỳ ${period}.`}
        />
      </Card>

      {payroll.activeSlip && <SalarySlipDetailPanel slip={payroll.activeSlip} />}
    </FeaturePage>
  );
};
