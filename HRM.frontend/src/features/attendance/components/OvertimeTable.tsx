import type { ReactNode } from "react";
import { FeatureCard } from "../../../core/components/FeatureShell";
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
}: OvertimeTableProps) => (
  <FeatureCard title={title}>
    <div className="overflow-x-auto">
      <table className="w-full min-w-[860px] text-left text-sm">
        <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
          <tr>
            <th className="px-3 py-2">Nhân viên</th>
            <th className="px-3 py-2">Ngày</th>
            <th className="px-3 py-2">Khung OT</th>
            <th className="px-3 py-2">Lý do</th>
            <th className="px-3 py-2">Trạng thái</th>
            <th className="px-3 py-2">Được duyệt</th>
            <th className="px-3 py-2">Thực tính</th>
            <th className="px-3 py-2 text-right">Thao tác</th>
          </tr>
        </thead>
        <tbody>
          {data.map((item) => (
            <tr key={item.id} className="border-b">
              <td className="px-3 py-3">
                <p className="font-semibold text-gray-900">
                  {item.employeeName}
                </p>
                <p className="text-xs text-gray-500">
                  {item.departmentName || "Chưa có phòng ban"}
                </p>
              </td>
              <td className="px-3 py-3">{formatDate(item.workDate)}</td>
              <td className="px-3 py-3">
                {item.startTime} - {item.endTime}
              </td>
              <td className="max-w-[260px] px-3 py-3 text-gray-600">
                {item.reason}
              </td>
              <td className="px-3 py-3">{formatStatus(item.status)}</td>
              <td className="px-3 py-3">
                {formatMinutes(item.approvedMinutes)}
              </td>
              <td className="px-3 py-3">
                {formatMinutes(item.actualOtMinutes)}
              </td>
              <td className="px-3 py-3 text-right">
                {renderActions?.(item)}
              </td>
            </tr>
          ))}
          {data.length === 0 && (
            <tr>
              <td colSpan={8} className="px-3 py-6 text-center text-gray-500">
                {emptyText}
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  </FeatureCard>
);

export const OvertimeActionButtons = ({
  onApprove,
  onReject,
}: {
  onApprove: () => void;
  onReject: () => void;
}) => (
  <div className="flex justify-end gap-2">
    <button
      type="button"
      onClick={onApprove}
      className="rounded bg-emerald-600 px-3 py-1 text-xs font-semibold text-white hover:bg-emerald-700"
    >
      Duyệt
    </button>
    <button
      type="button"
      onClick={onReject}
      className="rounded bg-rose-600 px-3 py-1 text-xs font-semibold text-white hover:bg-rose-700"
    >
      Từ chối
    </button>
  </div>
);

const formatStatus = (status: OvertimeRequest["status"]) => {
  const map: Record<OvertimeRequest["status"], string> = {
    PendingManager: "Chờ Trưởng phòng",
    PendingHR: "Chờ HR",
    PendingDirector: "Chờ Giám đốc",
    Approved: "Đã duyệt",
    Rejected: "Từ chối",
    Cancelled: "Đã hủy",
  };

  return map[status] || status;
};

const formatDate = (value: string) =>
  new Date(value).toLocaleDateString("vi-VN");

const formatMinutes = (value: number) => {
  if (!value || value <= 0) return "0 phút";

  const hours = Math.floor(value / 60);
  const minutes = value % 60;

  if (hours === 0) return `${minutes} phút`;
  if (minutes === 0) return `${hours} giờ`;

  return `${hours} giờ ${minutes} phút`;
};
