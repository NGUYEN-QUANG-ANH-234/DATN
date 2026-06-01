import { Card } from "../../../components/ui";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { usePayrollPeriod } from "../hooks/usePayrollPeriod";
import { useSalarySlips } from "../hooks/useSalarySlips";
import { PayrollPeriodFilter } from "./PayrollPeriodFilter";
import { SalarySlipDetailPanel } from "./SalarySlipDetailPanel";
import { SalarySlipTable } from "./SalarySlipTable";

export const PayslipLookupPage = () => {
  const { month, year, period, setMonth, setYear } = usePayrollPeriod();
  const salarySlips = useSalarySlips(period);

  return (
    <FeaturePage
      title="Phân phối và tra cứu phiếu lương"
      description="Tra cứu phiếu lương theo phạm vi phân quyền và kết xuất các phiếu được phép truy cập."
      width="wide"
    >
      <Card title="Bộ lọc kỳ lương">
        <PayrollPeriodFilter
          month={month}
          year={year}
          loading={salarySlips.loading}
          canExport={salarySlips.selectedIds.length > 0}
          exportLabel="Kết xuất đã chọn"
          onMonthChange={setMonth}
          onYearChange={setYear}
          onRefresh={salarySlips.loadSlips}
          onExport={salarySlips.exportSelected}
        />
      </Card>

      <Card title="Danh sách phiếu lương">
        <SalarySlipTable
          slips={salarySlips.slips}
          selectedIds={salarySlips.selectedIds}
          loading={salarySlips.loading}
          onToggle={salarySlips.toggleSelected}
          onOpenDetail={salarySlips.openDetail}
          emptyText={`Chưa có phiếu lương cho kỳ ${period}.`}
        />
      </Card>

      {salarySlips.activeSlip && <SalarySlipDetailPanel slip={salarySlips.activeSlip} />}
    </FeaturePage>
  );
};
