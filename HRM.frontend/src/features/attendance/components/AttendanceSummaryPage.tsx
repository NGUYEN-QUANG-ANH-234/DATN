import { useCallback, useEffect, useMemo, useState } from "react";
import { Download, Lock, RefreshCcw, Send, ShieldCheck } from "lucide-react";
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

type PeriodStatus = "Draft" | "PendingHRReview" | "Approved" | "Locked" | "Empty";

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

  const periodStatus = useMemo(() => resolvePeriodStatus(summaries), [summaries]);
  const canGenerate = periodStatus === "Draft" || periodStatus === "Empty";
  const canSubmit = periodStatus === "Draft" && summaries.length > 0;
  const canApprove = periodStatus === "PendingHRReview";
  const canLock = periodStatus === "Approved";

  const runPeriodAction = async (
    action: "generate" | "submit" | "approve" | "lock",
  ) => {
    setLoading(true);
    try {
      const res =
        action === "generate"
          ? await attendanceSummaryApi.generateMonthly(month, year)
          : action === "submit"
            ? await attendanceSummaryApi.submitMonthly(month, year)
            : action === "approve"
              ? await attendanceSummaryApi.approveMonthly(month, year)
              : await attendanceSummaryApi.lockMonthly(month, year);

      setSummaries(res.data || []);
      triggerAlert("success", successTitle[action], res.message || successMessage[action]);
    } catch (error) {
      triggerAlert(
        "error",
        errorTitle[action],
        error instanceof Error ? error.message : "Vui lòng thử lại sau.",
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
    {
      key: "department",
      header: "Phòng ban",
      render: (item) => item.departmentName || "Chưa có phòng ban",
    },
    { key: "workDays", header: "Ngày công", render: (item) => item.workDays },
    { key: "workedHours", header: "Giờ làm", render: (item) => `${item.workedHours.toFixed(2)} giờ` },
    {
      key: "payableHours",
      header: "Giờ tính lương",
      render: (item) => (
        <div>
          <p>{item.payableWorkHours.toFixed(2)} giờ</p>
          <p className="text-xs text-[var(--hicas-text-secondary)]">Quy đổi từ ngày công</p>
        </div>
      ),
    },
    { key: "late", header: "Đi muộn", render: (item) => formatMinutes(item.lateMinutes) },
    { key: "early", header: "Về sớm", render: (item) => formatMinutes(item.earlyLeaveMinutes) },
    { key: "ot", header: "Làm thêm", render: (item) => formatMinutes(item.actualOtMinutes) },
    {
      key: "status",
      header: "Trạng thái",
      render: (item) => {
        const status = item.isPayrollLocked ? "Locked" : item.approvalStatus;
        return <StatusBadge status={status} label={getApprovalStatusLabel(status)} />;
      },
    },
  ];

  return (
    <FeaturePage
      title="Bảng công tháng"
      description="Tổng hợp, gửi duyệt và khóa kỳ công trước khi tính lương."
      width="wide"
    >
      <Card title="Kỳ công">
        <div className="grid gap-4 xl:grid-cols-[150px_150px_minmax(220px,1fr)_auto]">
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
          <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-[var(--hicas-surface-muted)] px-4 py-3">
            <span className="text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
              Trạng thái kỳ công
            </span>
            <div className="mt-2 flex flex-wrap items-center gap-3">
              <StatusBadge status={periodStatus} label={getPeriodStatusLabel(periodStatus)} />
              <span className="text-sm text-[var(--hicas-text-secondary)]">
                {summaries.length > 0
                  ? `${summaries.length} nhân sự trong kỳ ${String(month).padStart(2, "0")}/${year}`
                  : "Chưa có dữ liệu cho kỳ này"}
              </span>
            </div>
          </div>
          <div className="flex flex-wrap items-end justify-start gap-2 xl:justify-end">
            <Button
              type="button"
              variant="secondary"
              disabled={loading || !canGenerate}
              isLoading={loading && canGenerate}
              onClick={() => runPeriodAction("generate")}
              iconLeft={<RefreshCcw size={16} />}
            >
              Tổng hợp
            </Button>
            <Button
              type="button"
              variant="secondary"
              disabled={loading || !canSubmit}
              onClick={() => runPeriodAction("submit")}
              iconLeft={<Send size={16} />}
            >
              Gửi chốt
            </Button>
            <Button
              type="button"
              variant="secondary"
              disabled={loading || !canApprove}
              onClick={() => runPeriodAction("approve")}
              iconLeft={<ShieldCheck size={16} />}
            >
              Duyệt
            </Button>
            <Button
              type="button"
              disabled={loading || !canLock}
              onClick={() => runPeriodAction("lock")}
              iconLeft={<Lock size={16} />}
            >
              Khóa kỳ
            </Button>
            <Button
              type="button"
              variant="secondary"
              disabled={loading || summaries.length === 0}
              onClick={exportExcel}
              iconLeft={<Download size={16} />}
            >
              Xuất Excel
            </Button>
          </div>
        </div>
      </Card>

      <Card title="Chi tiết bảng công">
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

const successTitle = {
  generate: "Đã tổng hợp bảng công",
  submit: "Đã gửi chốt bảng công",
  approve: "Đã duyệt bảng công",
  lock: "Đã khóa kỳ công",
};

const successMessage = {
  generate: "Dữ liệu bảng công đã được cập nhật.",
  submit: "Kỳ công đã được gửi sang bước duyệt.",
  approve: "Kỳ công đã được duyệt và sẵn sàng khóa.",
  lock: "Dữ liệu công, OT và nghỉ phép liên quan đã được khóa cho payroll.",
};

const errorTitle = {
  generate: "Không thể tổng hợp bảng công",
  submit: "Không thể gửi chốt bảng công",
  approve: "Không thể duyệt bảng công",
  lock: "Không thể khóa kỳ công",
};

const resolvePeriodStatus = (summaries: AttendanceSummary[]): PeriodStatus => {
  if (summaries.length === 0) return "Empty";
  if (summaries.every((item) => item.isPayrollLocked || item.approvalStatus === "Locked")) return "Locked";
  if (summaries.every((item) => item.approvalStatus === "Approved")) return "Approved";
  if (summaries.some((item) => item.approvalStatus === "PendingHRReview")) return "PendingHRReview";
  return "Draft";
};

const getPeriodStatusLabel = (status: PeriodStatus) => {
  switch (status) {
    case "Draft":
      return "Bản nháp";
    case "PendingHRReview":
      return "Chờ duyệt";
    case "Approved":
      return "Đã duyệt";
    case "Locked":
      return "Đã khóa";
    default:
      return "Chưa có dữ liệu";
  }
};

const getApprovalStatusLabel = (status: string) => {
  switch (status) {
    case "Draft":
      return "Bản nháp";
    case "PendingHRReview":
      return "Chờ duyệt";
    case "Approved":
      return "Đã duyệt";
    case "Locked":
      return "Đã khóa";
    case "Rejected":
      return "Từ chối";
    default:
      return "Đang xử lý";
  }
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
          <td>${item.isPayrollLocked ? "Đã khóa" : getApprovalStatusLabel(item.approvalStatus)}</td>
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
          <tr><td class="title" colspan="16">Bảng công tháng ${String(month).padStart(2, "0")}/${year}</td></tr>
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
            <th>Làm thêm (phút)</th>
            <th>Làm thêm (giờ)</th>
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
