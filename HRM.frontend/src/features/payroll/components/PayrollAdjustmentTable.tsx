import { Badge, DataTable, StatusBadge } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import type { PayrollAdjustment, PayrollAdjustmentTableProps } from "../types/payroll";
import { adjustmentTypes, formatMoney } from "../utils";

const adjustmentTypeLabel = (type: string) =>
  adjustmentTypes.find((item) => item.value === type)?.label || type;

export const PayrollAdjustmentTable = ({
  adjustments,
  loading = false,
  period,
}: PayrollAdjustmentTableProps) => {
  const columns: Array<DataTableColumn<PayrollAdjustment>> = [
    {
      key: "employee",
      header: "Nhân viên",
      render: (item) => (
        <div>
          <p className="font-semibold text-[var(--hicas-text-main)]">
            {item.employeeName || `Employee #${item.employeeId}`}
          </p>
          <p className="text-xs text-[var(--hicas-text-secondary)]">{item.employeeCode || ""}</p>
        </div>
      ),
    },
    { key: "type", header: "Loại", render: (item) => adjustmentTypeLabel(item.adjustmentType) },
    {
      key: "amount",
      header: "Số tiền",
      render: (item) => (
        <span className="font-semibold text-[var(--hicas-text-main)]">{formatMoney(item.amount)}</span>
      ),
    },
    { key: "tax", header: "Thuế", render: (item) => (item.isTaxable ? "Có" : "Không") },
    { key: "insurance", header: "BH", render: (item) => (item.isInsuranceBased ? "Có" : "Không") },
    {
      key: "nature",
      header: "Tính chất",
      render: (item) => (
        <Badge variant={item.isDeduction ? "warning" : "success"}>
          {item.isDeduction ? "Giảm lương hợp lệ" : "Thu nhập bổ sung"}
        </Badge>
      ),
    },
    { key: "status", header: "Trạng thái", render: (item) => <StatusBadge status={item.status} /> },
    {
      key: "reason",
      header: "Lý do",
      render: (item) => (
        <span className="line-clamp-2 text-[var(--hicas-text-secondary)]">{item.reason}</span>
      ),
    },
  ];

  return (
    <DataTable
      columns={columns}
      data={adjustments}
      loading={loading}
      rowKey={(row) => row.id}
      emptyTitle={`Chưa có khoản điều chỉnh nghiệp vụ lương trong kỳ ${period}.`}
    />
  );
};
