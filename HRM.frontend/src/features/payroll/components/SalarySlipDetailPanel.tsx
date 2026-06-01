import { Card, DataTable } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import type { SalarySlipDetail, SalarySlipDetailPanelProps } from "../types/payroll";
import { formatMoney, formatNumber } from "../utils";
import { PayrollMetricCard } from "./PayrollMetricCard";

const formatMinutes = (value?: number) => `${value ?? 0} phút`;

export const SalarySlipDetailPanel = ({ slip }: SalarySlipDetailPanelProps) => {
  const columns: Array<DataTableColumn<SalarySlipDetail>> = [
    {
      key: "code",
      header: "Mã khoản",
      render: (detail) => <span className="font-mono text-xs">{detail.componentCode}</span>,
    },
    { key: "name", header: "Tên khoản", render: (detail) => detail.componentName },
    { key: "amount", header: "Số tiền", render: (detail) => formatMoney(detail.amount) },
    {
      key: "taxable",
      header: "Chịu thuế",
      render: (detail) => formatMoney(detail.taxableAmount),
    },
    {
      key: "insurance",
      header: "Tính BH",
      render: (detail) => formatMoney(detail.insuranceBaseAmount),
    },
    { key: "note", header: "Ghi chú", render: (detail) => detail.note || "" },
  ];

  return (
    <Card
      title={`Chi tiết phiếu lương - ${slip.employeeName}`}
      description={`Kỳ ${slip.period}. Payroll chỉ đọc bảng công đã chốt; lỗi hiện diện được phản ánh qua giờ/công tính lương, không tạo khoản tiền trực tiếp.`}
    >
      <div className="mb-5 grid gap-3 text-sm md:grid-cols-4">
        <PayrollMetricCard label="Lương hợp đồng" value={formatMoney(slip.baseSalary)} />
        <PayrollMetricCard label="Gross" value={formatMoney(slip.grossIncome)} />
        <PayrollMetricCard label="Thu nhập tính thuế" value={formatMoney(slip.taxableIncome)} />
        <PayrollMetricCard label="Net" value={formatMoney(slip.netSalary)} strong />
      </div>

      <div className="mb-5 grid gap-3 text-sm md:grid-cols-4">
        <PayrollMetricCard label="Công chuẩn" value={formatNumber(slip.standardWorkDays)} />
        <PayrollMetricCard label="Công thực tế" value={formatNumber(slip.actualWorkDays)} />
        <PayrollMetricCard label="Giờ tính lương" value={formatNumber(slip.payableWorkHours)} />
        <PayrollMetricCard label="Giờ làm ghi nhận" value={formatNumber(slip.actualWorkHours)} />
        <PayrollMetricCard label="Đi muộn" value={formatMinutes(slip.lateMinutes)} />
        <PayrollMetricCard label="Về sớm" value={formatMinutes(slip.earlyLeaveMinutes)} />
        <PayrollMetricCard label="Nghỉ không lương" value={`${formatNumber(slip.unpaidLeaveWorkdays)} công`} />
        <PayrollMetricCard label="Nguồn ảnh hưởng" value="Bảng công đã chốt" />
      </div>

      <div className="mb-5 grid gap-3 text-sm md:grid-cols-4">
        <PayrollMetricCard label="Thâm niên" value={`${formatNumber(slip.serviceMonths)} tháng`} />
        <PayrollMetricCard label="Số năm thâm niên" value={formatNumber(slip.serviceYears)} />
        <PayrollMetricCard label="Tỷ lệ thâm niên" value={`${formatNumber(slip.seniorityRate)}%`} />
        <PayrollMetricCard label="Phụ cấp thâm niên" value={formatMoney(slip.seniorityAllowance)} />
      </div>

      <DataTable
        columns={columns}
        data={slip.details}
        rowKey={(row) => row.id}
        className="border-0 shadow-none"
        emptyTitle="Phiếu lương chưa có dòng chi tiết"
      />
    </Card>
  );
};
