import { Card } from "../../../components/ui";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { usePayrollAggregation } from "../hooks/usePayrollAggregation";
import { usePayrollPeriod } from "../hooks/usePayrollPeriod";
import { formatMoney, sumSalarySlips } from "../utils";
import { PayrollMetricCard } from "./PayrollMetricCard";
import { PayrollPeriodFilter } from "./PayrollPeriodFilter";
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
      description="Tổng hợp bảng lương nháp từ hợp đồng, bảng công đã chốt, OT, KPI, phụ cấp thâm niên, truy lĩnh/truy thu và snapshot chính sách lương."
      width="wide"
    >
      <Card title="Kỳ lương">
        <PayrollPeriodFilter
          month={month}
          year={year}
          loading={payroll.loading}
          calculating={payroll.calculating}
          canCalculate={payroll.canManagePayroll}
          canExport={payroll.selectedIds.length > 0}
          showCalculate={payroll.canManagePayroll}
          onMonthChange={setMonth}
          onYearChange={setYear}
          onRefresh={payroll.loadSlips}
          onCalculate={payroll.calculatePayroll}
          onExport={payroll.exportSelected}
        />
      </Card>

      <div className="grid gap-4 md:grid-cols-4">
        <PayrollMetricCard label="Số phiếu" value={String(payroll.slips.length)} />
        <PayrollMetricCard label="Tổng Gross" value={formatMoney(sumSalarySlips(payroll.slips, "grossIncome"))} />
        <PayrollMetricCard label="BH, thuế và điều chỉnh hợp lệ" value={formatMoney(totalDeductions)} />
        <PayrollMetricCard label="Tổng Net" value={formatMoney(sumSalarySlips(payroll.slips, "netSalary"))} strong />
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
          onOpenDetail={payroll.openDetail}
          emptyText={`Chưa có phiếu lương cho kỳ ${period}.`}
        />
      </Card>

      {payroll.activeSlip && <SalarySlipDetailPanel slip={payroll.activeSlip} />}
    </FeaturePage>
  );
};
