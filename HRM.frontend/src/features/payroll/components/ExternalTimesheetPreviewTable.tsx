import { DataTable } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import type { ExternalTimesheetLinePreview, ExternalTimesheetPreviewTableProps } from "../types/payroll";
import { formatMoney, formatNumber } from "../utils";

export const ExternalTimesheetPreviewTable = ({ lines }: ExternalTimesheetPreviewTableProps) => {
  const columns: Array<DataTableColumn<ExternalTimesheetLinePreview>> = [
    { key: "row", header: "Dòng", render: (line) => line.rowNumber },
    {
      key: "collaborator",
      header: "CTV",
      render: (line) => (
        <div>
          <p className="font-semibold text-[var(--hicas-text-main)]">{line.collaboratorName}</p>
          <p className="text-xs text-[var(--hicas-text-secondary)]">{line.collaboratorCode}</p>
        </div>
      ),
    },
    { key: "workDate", header: "Ngày công", render: (line) => line.workDate },
    { key: "project", header: "Dự án", render: (line) => line.projectCode },
    { key: "task", header: "Task", render: (line) => line.taskCode },
    { key: "hours", header: "Giờ duyệt", render: (line) => formatNumber(line.approvedHours) },
    { key: "rate", header: "Đơn giá", render: (line) => formatMoney(line.hourlyRate) },
    {
      key: "amount",
      header: "Thành tiền",
      render: (line) => (
        <span className="font-semibold text-[var(--hicas-text-main)]">{formatMoney(line.amount)}</span>
      ),
    },
  ];

  return (
    <DataTable
      columns={columns}
      data={lines}
      rowKey={(row) => `${row.rowNumber}-${row.collaboratorCode}`}
      emptyTitle="Chưa có dữ liệu xem trước."
    />
  );
};
