import { useCallback, useEffect, useState } from "react";
import { Download } from "lucide-react";
import { Button, Card, DataTable, StatusBadge } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { useNotification } from "../../../core/context/NotificationContext";
import { formatDateTime, formatMinutesAsHours } from "../../../utils";
import {
  attendanceSummaryApi,
  type AttendanceSummary,
} from "../api/attendanceSummaryApi";

const now = new Date();

export const AttendanceSummaryPage = () => {
  const { triggerAlert } = useNotification();
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [year, setYear] = useState(now.getFullYear());
  const [loading, setLoading] = useState(false);
  const [summaries, setSummaries] = useState<AttendanceSummary[]>([]);

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const res = await attendanceSummaryApi.getMonthly(month, year);
      setSummaries(res.data || []);
    } catch {
      setSummaries([]);
    } finally {
      setLoading(false);
    }
  }, [month, year]);

  useEffect(() => {
    void fetchData();
  }, [fetchData]);

  const generate = async () => {
    setLoading(true);
    try {
      const res = await attendanceSummaryApi.generateMonthly(month, year);
      setSummaries(res.data || []);
      triggerAlert(
        "success",
        "Đã tổng hợp bảng công",
        res.message || "Dữ liệu bảng công đã được cập nhật.",
      );
    } catch (error) {
      triggerAlert(
        "error",
        "Không thể tổng hợp bảng công",
        error instanceof Error ? error.message : "Đã có lỗi xảy ra.",
      );
    } finally {
      setLoading(false);
    }
  };

  const exportExcel = () => {
    if (summaries.length === 0) {
      triggerAlert(
        "warning",
        "Chưa có dữ liệu",
        "Vui lòng tổng hợp hoặc tải bảng công trước khi xuất Excel.",
      );
      return;
    }

    const html = buildExcelHtml(summaries, month, year);
    const blob = new Blob(["\ufeff", html], {
      type: "application/vnd.ms-excel;charset=utf-8;",
    });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `bang-cong-${String(month).padStart(2, "0")}-${year}.xls`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);

    triggerAlert("success", "Đã xuất Excel", "File bảng công đã được tạo thành công.");
  };

  const columns: Array<DataTableColumn<AttendanceSummary>> = [
    {
      key: "employee",
      header: "Nhân viên",
      render: (item) => (
        <div>
          <p className="font-semibold text-[var(--hicas-text-main)]">{item.employeeName}</p>
          <p className="text-xs text-[var(--hicas-text-secondary)]">{item.employeeCode}</p>
        </div>
      ),
    },
    { key: "department", header: "Phòng ban", render: (item) => item.departmentName || "Chưa có phòng ban" },
    { key: "workDays", header: "Ngày công", render: (item) => item.workDays },
    { key: "workedHours", header: "Giờ làm", render: (item) => `${item.workedHours.toFixed(2)} giờ` },
    {
      key: "payableHours",
      header: "Giờ tính lương",
      render: (item) => (
        <div>
          <p>{item.payableWorkHours.toFixed(2)} giờ</p>
          <p className="text-xs text-[var(--hicas-text-secondary)]">Quy đổi ngày công</p>
        </div>
      ),
    },
    { key: "late", header: "Đi muộn", render: (item) => formatMinutes(item.lateMinutes) },
    { key: "early", header: "Về sớm", render: (item) => formatMinutes(item.earlyLeaveMinutes) },
    { key: "ot", header: "Làm thêm hợp lệ", render: (item) => formatMinutes(item.actualOtMinutes) },
    {
      key: "status",
      header: "Trạng thái",
      render: (item) => (
        <StatusBadge
          status={item.isPayrollLocked ? "PayrollLocked" : "Open"}
          label={item.isPayrollLocked ? "Đã khóa lương" : "Có thể cập nhật"}
        />
      ),
    },
  ];

  return (
    <FeaturePage
      title="Tổng hợp bảng công"
      description="Tổng hợp ngày công, đi muộn, về sớm và giờ làm thêm hợp lệ."
      width="wide"
    >
      <Card title="Kỳ tổng hợp">
        <div className="grid gap-4 md:grid-cols-[160px_160px_auto_auto]">
          <label className="block">
            <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
              Tháng
            </span>
            <input
              type="number"
              min={1}
              max={12}
              value={month}
              onChange={(event) => setMonth(Number(event.target.value))}
              className="hicas-input w-full"
            />
          </label>
          <label className="block">
            <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
              Năm
            </span>
            <input
              type="number"
              min={2000}
              max={2100}
              value={year}
              onChange={(event) => setYear(Number(event.target.value))}
              className="hicas-input w-full"
            />
          </label>
          <div className="flex items-end">
            <Button type="button" disabled={loading} isLoading={loading} onClick={generate}>
              Tổng hợp lại
            </Button>
          </div>
          <div className="flex items-end">
            <Button
              type="button"
              variant="secondary"
              disabled={loading || summaries.length === 0}
              onClick={exportExcel}
            >
              <Download size={16} />
              Xuất Excel
            </Button>
          </div>
        </div>
      </Card>

      <Card title="Bảng công tổng hợp">
        <DataTable
          columns={columns}
          data={summaries}
          loading={loading}
          rowKey={(row) => row.id}
          emptyTitle="Chưa có bảng công cho kỳ này."
          className="border-0 shadow-none"
        />
      </Card>
    </FeaturePage>
  );
};

const formatMinutes = (value: number) => {
  if (!value || value <= 0) return "0 phút";
  const hours = Math.floor(value / 60);
  const minutes = value % 60;
  if (hours === 0) return `${minutes} phút`;
  if (minutes === 0) return `${hours} giờ`;
  return `${hours} giờ ${minutes} phút`;
};

const buildExcelHtml = (
  summaries: AttendanceSummary[],
  month: number,
  year: number,
) => {
  const rows = summaries
    .map(
      (item, index) => `
        <tr>
          <td>${index + 1}</td>
          <td>${escapeHtml(item.employeeCode)}</td>
          <td>${escapeHtml(item.employeeName)}</td>
          <td>${escapeHtml(item.departmentName || "Chưa có phòng ban")}</td>
          <td>${item.month}</td>
          <td>${item.year}</td>
          <td>${item.workDays}</td>
          <td>${item.workedMinutes}</td>
          <td>${item.workedHours.toFixed(2)}</td>
          <td>${item.payableWorkHours.toFixed(2)}</td>
          <td>${item.lateMinutes}</td>
          <td>${item.earlyLeaveMinutes}</td>
          <td>${item.actualOtMinutes}</td>
          <td>${formatMinutesAsHours(item.actualOtMinutes)}</td>
          <td>${item.isPayrollLocked ? "Đã khóa lương" : "Có thể cập nhật"}</td>
          <td>${formatDateTime(item.generatedAt, "")}</td>
        </tr>`,
    )
    .join("");

  return `
    <html>
      <head>
        <meta charset="UTF-8" />
        <style>
          table { border-collapse: collapse; font-family: Arial, sans-serif; font-size: 12px; }
          th, td { border: 1px solid #d1d5db; padding: 6px 8px; }
          th { background: #f3f4f6; font-weight: 700; }
          .title { font-size: 18px; font-weight: 700; }
        </style>
      </head>
      <body>
        <table>
          <tr><td class="title" colspan="16">Bảng công tổng hợp tháng ${String(month).padStart(2, "0")}/${year}</td></tr>
          <tr><td colspan="16">Ngày xuất: ${formatDateTime(new Date())}</td></tr>
          <tr>
            <th>STT</th>
            <th>Mã nhân viên</th>
            <th>Nhân viên</th>
            <th>Phòng ban</th>
            <th>Tháng</th>
            <th>Năm</th>
            <th>Ngày công</th>
            <th>Giờ làm (phút)</th>
            <th>Giờ làm</th>
            <th>Giờ tính lương</th>
            <th>Đi muộn (phút)</th>
            <th>Về sớm (phút)</th>
            <th>Làm thêm hợp lệ (phút)</th>
            <th>Làm thêm hợp lệ (giờ)</th>
            <th>Trạng thái</th>
            <th>Ngày tổng hợp</th>
          </tr>
          ${rows}
        </table>
      </body>
    </html>`;
};

const escapeHtml = (value: string) =>
  value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
