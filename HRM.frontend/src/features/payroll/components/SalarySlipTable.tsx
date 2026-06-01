import { Eye } from "lucide-react";
import { Button, DataTable, StatusBadge } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import type { SalarySlip, SalarySlipTableProps } from "../types/payroll";
import { formatMoney, formatNumber } from "../utils";

const formatMinutes = (value?: number) => `${value ?? 0} phút`;

export const SalarySlipTable = ({
  slips,
  selectedIds,
  loading = false,
  emptyText,
  onToggle,
  onOpenDetail,
}: SalarySlipTableProps) => {
  const columns: Array<DataTableColumn<SalarySlip>> = [
    {
      key: "select",
      header: "Chọn",
      render: (slip) => (
        <input
          type="checkbox"
          checked={selectedIds.includes(slip.id)}
          onChange={() => onToggle(slip.id)}
          className="h-4 w-4 accent-[var(--hicas-orange)]"
        />
      ),
    },
    {
      key: "employee",
      header: "Nhân viên",
      render: (slip) => (
        <div>
          <p className="font-semibold text-[var(--hicas-text-main)]">{slip.employeeName}</p>
          <p className="text-xs text-[var(--hicas-text-secondary)]">{slip.employeeCode}</p>
        </div>
      ),
    },
    { key: "department", header: "Phòng ban", render: (slip) => slip.departmentName || "Chưa có" },
    { key: "standardWorkdays", header: "Công chuẩn", render: (slip) => formatNumber(slip.standardWorkDays) },
    { key: "actualWorkdays", header: "Công thực tế", render: (slip) => formatNumber(slip.actualWorkDays) },
    { key: "payableHours", header: "Giờ tính lương", render: (slip) => formatNumber(slip.payableWorkHours) },
    { key: "late", header: "Đi muộn", render: (slip) => formatMinutes(slip.lateMinutes) },
    { key: "early", header: "Về sớm", render: (slip) => formatMinutes(slip.earlyLeaveMinutes) },
    { key: "gross", header: "Gross", render: (slip) => formatMoney(slip.grossIncome) },
    { key: "insurance", header: "BH NLĐ", render: (slip) => formatMoney(slip.employeeInsuranceAmount) },
    { key: "pit", header: "PIT", render: (slip) => formatMoney(slip.pitAmount) },
    {
      key: "net",
      header: "Net",
      render: (slip) => (
        <span className="font-bold text-[var(--hicas-text-main)]">{formatMoney(slip.netSalary)}</span>
      ),
    },
    {
      key: "status",
      header: "Trạng thái",
      render: (slip) => <StatusBadge status={slip.status} />,
    },
    {
      key: "actions",
      header: "Thao tác",
      render: (slip) => (
        <Button size="sm" variant="ghost" onClick={() => onOpenDetail(slip.id)}>
          <Eye size={16} />
          Chi tiết
        </Button>
      ),
    },
  ];

  return (
    <DataTable
      columns={columns}
      data={slips}
      loading={loading}
      rowKey={(row) => row.id}
      emptyTitle={emptyText}
    />
  );
};
