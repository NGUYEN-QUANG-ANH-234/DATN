import type { ReactNode } from "react";
import { Button, Card, DataTable, StatusBadge } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import { formatDate, formatDateTime } from "../../../utils";
import type { OvertimeRequest } from "../api/overtimeApi";

type OvertimeTableProps = {
  title: string;
  data: OvertimeRequest[];
  emptyText: string;
  renderActions?: (item: OvertimeRequest) => ReactNode;
};

export const OvertimeTable = ({
  title,
  data,
  emptyText,
  renderActions,
}: OvertimeTableProps) => {
  const columns: Array<DataTableColumn<OvertimeRequest>> = [
    {
      key: "employee",
      header: "Nhân viên",
      render: (item) => (
        <div>
          <p className="font-semibold text-[var(--hicas-text-main)]">{item.employeeName}</p>
          <p className="text-xs text-[var(--hicas-text-secondary)]">
            {item.departmentName || "Chưa có phòng ban"}
          </p>
        </div>
      ),
    },
    { key: "workDate", header: "Ngày", render: (item) => formatDate(item.workDate) },
    {
      key: "time",
      header: "Khung OT / hệ số",
      render: (item) => (
        <div>
          <p>{formatDateTime(item.startAt)}</p>
          <p className="text-xs text-[var(--hicas-text-secondary)]">{formatDateTime(item.endAt)}</p>
          {item.segments?.length > 0 && (
            <p className="mt-1 text-xs text-[var(--hicas-info)]">
              {item.segments
                .map(
                  (segment) =>
                    `${segment.policyCode}: ${formatMinutes(segment.minutes)} x${segment.rateMultiplierSnapshot}`,
                )
                .join("; ")}
            </p>
          )}
        </div>
      ),
    },
    {
      key: "reason",
      header: "Lý do",
      render: (item) => (
        <span className="line-clamp-2 text-[var(--hicas-text-secondary)]">{item.reason}</span>
      ),
    },
    { key: "status", header: "Trạng thái", render: (item) => <StatusBadge status={item.status} /> },
    { key: "approved", header: "Được duyệt", render: (item) => formatMinutes(item.approvedMinutes) },
    { key: "actual", header: "Thực tính", render: (item) => formatMinutes(item.actualOtMinutes) },
    {
      key: "actions",
      header: "Thao tác",
      render: (item) => <div className="flex justify-end">{renderActions?.(item)}</div>,
      headerClassName: "text-right",
    },
  ];

  return (
    <Card title={title}>
      <DataTable
        columns={columns}
        data={data}
        rowKey={(row) => row.id}
        emptyTitle={emptyText}
        className="border-0 shadow-none"
      />
    </Card>
  );
};

export const OvertimeActionButtons = ({
  onApprove,
  onReject,
}: {
  onApprove: () => void;
  onReject: () => void;
}) => (
  <div className="flex flex-wrap justify-end gap-2">
    <Button type="button" size="sm" onClick={onApprove}>
      Duyệt
    </Button>
    <Button type="button" size="sm" variant="danger" onClick={onReject}>
      Từ chối
    </Button>
  </div>
);

const formatMinutes = (value: number) => {
  if (!value || value <= 0) return "0 phút";

  const hours = Math.floor(value / 60);
  const minutes = value % 60;

  if (hours === 0) return `${minutes} phút`;
  if (minutes === 0) return `${hours} giờ`;

  return `${hours} giờ ${minutes} phút`;
};
