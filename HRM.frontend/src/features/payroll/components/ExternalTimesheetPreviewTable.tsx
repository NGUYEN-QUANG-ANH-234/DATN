import { DataTable } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import type { ExternalTimesheetImportLine, ExternalTimesheetPreviewTableProps } from "../types/payroll";
import { formatMoney, formatNumber } from "../utils";

export const ExternalTimesheetPreviewTable = ({ lines }: ExternalTimesheetPreviewTableProps) => {
  const columns: Array<DataTableColumn<ExternalTimesheetImportLine>> = [
    { key: "row", header: "Dòng", render: (line) => line.rowNumber },
    {
      key: "collaborator",
      header: "Cộng tác viên",
      render: (line) => (
        <div>
          <p className="font-semibold text-[var(--hicas-text-main)]">
            {line.collaboratorName || line.collaboratorCode}
          </p>
          <p className="text-xs text-[var(--hicas-text-secondary)]">{line.collaboratorCode}</p>
        </div>
      ),
    },
    { key: "workDate", header: "Ngày công", render: (line) => line.workDateText || line.workDate },
    { key: "project", header: "Dự án", render: (line) => line.projectCode },
    { key: "task", header: "Công việc", render: (line) => line.taskCode },
    { key: "hours", header: "Giờ duyệt", render: (line) => formatNumber(line.approvedHours) },
    { key: "rate", header: "Đơn giá", render: (line) => formatMoney(line.hourlyRate) },
    {
      key: "amount",
      header: "Thành tiền",
      render: (line) => (
        <span className="font-semibold text-[var(--hicas-text-main)]">{formatMoney(line.amount)}</span>
      ),
    },
    {
      key: "result",
      header: "Kết quả",
      render: (line) =>
        line.isValid ? (
          <span className="font-semibold text-emerald-700">Hợp lệ</span>
        ) : (
          <span className="text-red-700">{line.errorMessage || "Không hợp lệ"}</span>
        ),
    },
  ];

  return (
    <DataTable
      columns={columns}
      data={lines}
      rowKey={(row) => `${row.rowNumber}-${row.collaboratorCode}-${row.projectCode}-${row.taskCode}`}
      emptyTitle="Chưa có dữ liệu xem trước."
    />
  );
};
