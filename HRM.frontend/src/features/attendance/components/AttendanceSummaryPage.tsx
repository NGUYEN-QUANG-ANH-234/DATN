import { useCallback, useEffect, useMemo, useState } from "react";
import { Download, Lock, Pencil, RefreshCcw, Send, ShieldCheck, UploadCloud } from "lucide-react";
import { Button, Card, DataTable, DrawerForm, StatusBadge } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import { useNotification } from "../../../core/context/NotificationContext";
import { formatDateTime, formatMinutesAsHours } from "../../../utils";
import {
  attendanceSummaryApi,
  type AdjustAttendanceDailySummaryPayload,
  type AttendanceAdjustmentLog,
  type AttendanceDailySummary,
  type AttendanceSummary,
} from "../api/attendanceSummaryApi";

const now = new Date();

type PeriodStatus = "Draft" | "PendingHRReview" | "Approved" | "Locked" | "Mixed" | "Empty";

const attendanceStatusOptions: Array<{ value: AttendanceDailySummary["attendanceStatus"]; label: string }> = [
  { value: "Present", label: "Có mặt" },
  { value: "HalfDay", label: "Nửa ngày" },
  { value: "PaidLeave", label: "Nghỉ hưởng lương" },
  { value: "UnpaidLeave", label: "Nghỉ không lương" },
  { value: "Absence", label: "Vắng mặt" },
  { value: "Holiday", label: "Ngày lễ" },
  { value: "Weekend", label: "Cuối tuần" },
  { value: "MaternityLeave", label: "Nghỉ thai sản" },
  { value: "SickLeave", label: "Nghỉ ốm" },
  { value: "ManualAdjusted", label: "Điều chỉnh thủ công" },
];

type AdjustForm = {
  workingMinutes: string;
  lateMinutes: string;
  earlyLeaveMinutes: string;
  overtimeMinutes: string;
  workdayValue: string;
  attendanceStatus: AttendanceDailySummary["attendanceStatus"];
  reason: string;
};

export const AttendanceSummaryPage = () => {
  const { triggerAlert } = useNotification();
  const { user } = useCurrentUser();
  const role = user?.role || "";
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [year, setYear] = useState(now.getFullYear());
  const [loading, setLoading] = useState(false);
  const [dailyLoading, setDailyLoading] = useState(false);
  const [adjusting, setAdjusting] = useState(false);
  const [importing, setImporting] = useState(false);
  const [summaries, setSummaries] = useState<AttendanceSummary[]>([]);
  const [dailySummaries, setDailySummaries] = useState<AttendanceDailySummary[]>([]);
  const [adjustmentLogs, setAdjustmentLogs] = useState<AttendanceAdjustmentLog[]>([]);
  const [adjustTarget, setAdjustTarget] = useState<AttendanceDailySummary | null>(null);
  const [adjustForm, setAdjustForm] = useState<AdjustForm>(() => emptyAdjustForm());
  const [importFile, setImportFile] = useState<File | null>(null);
  const [importReason, setImportReason] = useState("Import ghi đè bảng công ngày");
  const [importErrors, setImportErrors] = useState<Array<{ rowNumber: number; employeeCode: string; workDate: string; message: string }>>([]);

  const fetchData = useCallback(async () => {
    setLoading(true);
    setDailyLoading(true);
    try {
      const [monthlyRes, dailyRes, adjustmentRes] = await Promise.all([
        attendanceSummaryApi.getMonthly(month, year),
        attendanceSummaryApi.getDaily(month, year),
        attendanceSummaryApi.getAdjustmentLogs(month, year),
      ]);
      setSummaries(monthlyRes.data || []);
      setDailySummaries(dailyRes.data || []);
      setAdjustmentLogs(adjustmentRes.data || []);
    } catch {
      setSummaries([]);
      setDailySummaries([]);
      setAdjustmentLogs([]);
    } finally {
      setLoading(false);
      setDailyLoading(false);
    }
  }, [month, year]);

  useEffect(() => {
    void fetchData();
  }, [fetchData]);

  const periodStatus = useMemo(() => resolvePeriodStatus(summaries), [summaries]);
  const isHrOrAdmin = ["HR", "Admin"].includes(role);
  const isDirectorOrAdmin = ["Director", "Admin"].includes(role);
  const canGenerate = isHrOrAdmin && canGeneratePeriod(summaries, periodStatus);
  const canSubmit = isHrOrAdmin && canSubmitPeriod(summaries);
  const canApprove = isDirectorOrAdmin && canApprovePeriod(summaries);
  const canLock = isHrOrAdmin && canLockPeriod(summaries);

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
      const [dailyRes, adjustmentRes] = await Promise.all([
        attendanceSummaryApi.getDaily(month, year),
        attendanceSummaryApi.getAdjustmentLogs(month, year),
      ]);
      setDailySummaries(dailyRes.data || []);
      setAdjustmentLogs(adjustmentRes.data || []);
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

  const downloadCurrentDailyData = () => {
    if (dailySummaries.length === 0) {
      triggerAlert(
        "warning",
        "Chưa có dữ liệu ngày",
        "Vui lòng tổng hợp kỳ công trước khi tải file điều chỉnh.",
      );
      return;
    }

    const rows = [
      ["MaNhanVien", "NgayCong", "GioLam", "DiMuon", "VeSom", "OT", "Cong", "TrangThai", "LyDo"],
      ...dailySummaries.map((item) => [
        item.employeeCode,
        formatDateForImport(item.workDate),
        (Number(item.workingMinutes || 0) / 60).toFixed(2),
        String(item.lateMinutes || 0),
        String(item.earlyLeaveMinutes || 0),
        String(item.overtimeMinutes || 0),
        String(item.workdayValue || 0),
        getAttendanceStatusLabel(item.attendanceStatus),
        item.adjustmentReason || "",
      ]),
    ];
    const csv = rows.map((row) => row.map((cell) => `"${String(cell).replace(/"/g, '""')}"`).join(",")).join("\r\n");
    const blob = new Blob(["\ufeff", csv], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `bang-cong-ngay-${String(month).padStart(2, "0")}-${year}.csv`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  const importDailyFile = async () => {
    if (!importFile) {
      triggerAlert("warning", "Chưa chọn file", "Vui lòng chọn file .xlsx hoặc .csv để import bảng công ngày.");
      return;
    }

    const formData = new FormData();
    formData.append("month", String(month));
    formData.append("year", String(year));
    formData.append("reason", importReason.trim() || "Import ghi đè bảng công ngày");
    formData.append("file", importFile);

    setImporting(true);
    try {
      const res = await attendanceSummaryApi.importDaily(formData);
      setImportErrors(res.data.errors || []);
      await fetchData();
      if (res.data.errorRows > 0) {
        triggerAlert(
          "warning",
          "Import hoàn tất một phần",
          `Cập nhật ${res.data.updatedRows}, tạo mới ${res.data.createdRows}, lỗi ${res.data.errorRows} dòng.`,
        );
      } else {
        triggerAlert("success", "Đã import bảng công ngày", res.message || "Dữ liệu bảng công ngày đã được cập nhật.");
      }
    } catch (error) {
      triggerAlert(
        "error",
        "Không thể import bảng công ngày",
        error instanceof Error ? error.message : "Vui lòng kiểm tra lại file import.",
      );
    } finally {
      setImporting(false);
    }
  };

  const openAdjustDrawer = (item: AttendanceDailySummary) => {
    setAdjustTarget(item);
    setAdjustForm({
      workingMinutes: String(item.workingMinutes ?? 0),
      lateMinutes: String(item.lateMinutes ?? 0),
      earlyLeaveMinutes: String(item.earlyLeaveMinutes ?? 0),
      overtimeMinutes: String(item.overtimeMinutes ?? 0),
      workdayValue: String(item.workdayValue ?? 0),
      attendanceStatus: item.attendanceStatus,
      reason: item.adjustmentReason || "",
    });
  };

  const closeAdjustDrawer = () => {
    if (adjusting) return;
    setAdjustTarget(null);
    setAdjustForm(emptyAdjustForm());
  };

  const submitDailyAdjustment = async () => {
    if (!adjustTarget) return;
    if (!adjustForm.reason.trim()) {
      triggerAlert("warning", "Thiếu lý do điều chỉnh", "Vui lòng nhập lý do để lưu lịch sử bảng công.");
      return;
    }

    setAdjusting(true);
    try {
      const payload: AdjustAttendanceDailySummaryPayload = {
        workingMinutes: toNullableNumber(adjustForm.workingMinutes),
        lateMinutes: toNullableNumber(adjustForm.lateMinutes),
        earlyLeaveMinutes: toNullableNumber(adjustForm.earlyLeaveMinutes),
        overtimeMinutes: toNullableNumber(adjustForm.overtimeMinutes),
        workdayValue: toNullableNumber(adjustForm.workdayValue),
        attendanceStatus: adjustForm.attendanceStatus,
        reason: adjustForm.reason.trim(),
      };
      const res = await attendanceSummaryApi.adjustDaily(adjustTarget.id, payload);
      setDailySummaries((items) => items.map((item) => (item.id === adjustTarget.id ? res.data : item)));
      const monthlyRes = await attendanceSummaryApi.getMonthly(month, year);
      setSummaries(monthlyRes.data || []);
      triggerAlert("success", "Đã điều chỉnh bảng công", res.message || "Dòng bảng công đã được cập nhật.");
      setAdjustTarget(null);
      setAdjustForm(emptyAdjustForm());
    } catch (error) {
      triggerAlert(
        "error",
        "Không thể điều chỉnh bảng công",
        error instanceof Error ? error.message : "Vui lòng kiểm tra dữ liệu nhập.",
      );
    } finally {
      setAdjusting(false);
    }
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

  const dailyColumns: Array<DataTableColumn<AttendanceDailySummary>> = [
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
      key: "workDate",
      header: "Ngày",
      render: (item) => formatDateTime(item.workDate, ""),
    },
    {
      key: "checkTime",
      header: "Vào / ra",
      render: (item) => (
        <div className="text-sm">
          <p>{formatDateTime(item.firstCheckIn, "Chưa check-in")}</p>
          <p className="text-xs text-[var(--hicas-text-secondary)]">
            {formatDateTime(item.lastCheckOut, "Chưa check-out")}
          </p>
        </div>
      ),
    },
    { key: "working", header: "Giờ làm", render: (item) => formatMinutesAsHours(item.workingMinutes) },
    { key: "workday", header: "Công", render: (item) => item.workdayValue },
    { key: "late", header: "Đi muộn", render: (item) => formatMinutes(item.lateMinutes) },
    { key: "early", header: "Về sớm", render: (item) => formatMinutes(item.earlyLeaveMinutes) },
    { key: "ot", header: "OT", render: (item) => formatMinutes(item.overtimeMinutes) },
    {
      key: "dailyStatus",
      header: "Trạng thái",
      render: (item) => (
        <div className="space-y-1">
          <StatusBadge
            status={item.isPayrollLocked ? "Locked" : item.approvalStatus}
            label={item.isPayrollLocked ? "Đã khóa" : getApprovalStatusLabel(item.approvalStatus)}
          />
          <p className="text-xs text-[var(--hicas-text-secondary)]">
            {getAttendanceStatusLabel(item.attendanceStatus)}
            {item.isManualAdjusted ? " · Đã chỉnh" : ""}
          </p>
        </div>
      ),
    },
    {
      key: "actions",
      header: "Thao tác",
      render: (item) => (
        <Button
          type="button"
          variant="secondary"
          size="sm"
          disabled={item.isPayrollLocked || item.approvalStatus === "Locked"}
          onClick={() => openAdjustDrawer(item)}
          iconLeft={<Pencil size={14} />}
        >
          Điều chỉnh
        </Button>
      ),
    },
  ];

  void dailyColumns;

  const adjustmentLogColumns: Array<DataTableColumn<AttendanceAdjustmentLog>> = [
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
      key: "workDate",
      header: "Ngày công",
      render: (item) => formatDateTime(item.workDate, ""),
    },
    {
      key: "changes",
      header: "Nội dung thay đổi",
      render: (item) => (
        <div className="max-w-[420px] whitespace-pre-line text-sm text-[var(--hicas-text-main)]">
          {buildChangeSummary(item)}
        </div>
      ),
    },
    {
      key: "reason",
      header: "Lý do",
      render: (item) => item.reason || "Không có ghi chú",
    },
    {
      key: "actor",
      header: "Người thực hiện",
      render: (item) => (
        <div>
          <p className="font-medium text-[var(--hicas-text-main)]">{item.adjustedByName}</p>
          <p className="text-xs text-[var(--hicas-text-secondary)]">{formatDateTime(item.adjustedAt, "")}</p>
        </div>
      ),
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
                {getPeriodActionHint(periodStatus, role, summaries)}
              </span>
              <span className="text-sm text-[var(--hicas-text-secondary)]">
                {summaries.length > 0
                  ? `${summaries.length} nhân sự trong kỳ ${String(month).padStart(2, "0")}/${year}`
                  : "Chưa có dữ liệu cho kỳ này"}
              </span>
            </div>
          </div>
          <div className="flex flex-wrap items-end justify-start gap-2 xl:justify-end">
            {isHrOrAdmin && (
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
            )}
            {isHrOrAdmin && (
              <Button
              type="button"
              variant="secondary"
              disabled={loading || !canSubmit}
              onClick={() => runPeriodAction("submit")}
              iconLeft={<Send size={16} />}
            >
              Gửi chốt
              </Button>
            )}
            {isDirectorOrAdmin && (
              <Button
                type="button"
                variant="secondary"
                disabled={loading || !canApprove}
                onClick={() => runPeriodAction("approve")}
                iconLeft={<ShieldCheck size={16} />}
              >
              Duyệt
              </Button>
            )}
            {isHrOrAdmin && (
              <Button
                type="button"
                disabled={loading || !canLock}
                onClick={() => runPeriodAction("lock")}
                iconLeft={<Lock size={16} />}
              >
              Khóa kỳ
              </Button>
            )}
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
        <div className="mb-5 rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-[var(--hicas-surface-muted)] p-4">
          <div className="mb-3">
            <h3 className="text-base font-semibold text-[var(--hicas-text-main)]">Import bảng công ngày</h3>
            <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
              Dùng file Excel/CSV để ghi đè hàng loạt dữ liệu công ngày trước khi gửi chốt kỳ.
            </p>
          </div>
          <div className="grid gap-4 xl:grid-cols-[minmax(260px,1fr)_minmax(260px,1fr)_auto]">
            <label className="block">
              <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
                File bảng công
              </span>
              <input
                type="file"
                accept=".xlsx,.csv"
                className="hicas-input w-full"
                onChange={(event) => setImportFile(event.target.files?.[0] ?? null)}
              />
            </label>
            <label className="block">
              <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
                Lý do ghi đè
              </span>
              <input
                className="hicas-input w-full"
                value={importReason}
                onChange={(event) => setImportReason(event.target.value)}
                placeholder="Ví dụ: Import bảng công đã rà soát từ HR"
              />
            </label>
            <div className="flex flex-wrap items-end gap-2">
              <Button type="button" variant="secondary" onClick={downloadCurrentDailyData} iconLeft={<Download size={16} />}>
                Tải dữ liệu hiện tại
              </Button>
              <Button type="button" onClick={importDailyFile} isLoading={importing} iconLeft={<UploadCloud size={16} />}>
                Import ghi đè
              </Button>
            </div>
          </div>
          <p className="mt-3 text-sm text-[var(--hicas-text-secondary)]">
            Cột hỗ trợ: MaNhanVien, NgayCong, GioLam hoặc PhutLamViec, DiMuon, VeSom, OT, Cong, TrangThai, LyDo.
            Dòng đã khóa lương hoặc kỳ công đã gửi chốt sẽ không được ghi đè.
          </p>
          {importErrors.length > 0 && (
            <div className="mt-4 max-h-[180px] overflow-auto rounded-[var(--radius-md)] border border-[var(--hicas-warning)]/40 bg-[var(--hicas-warning-soft)] p-3">
              <p className="text-sm font-semibold text-amber-800">Một số dòng chưa được import</p>
              <div className="mt-2 space-y-2 text-sm text-amber-900">
                {importErrors.map((error) => (
                  <div key={`${error.rowNumber}-${error.employeeCode}-${error.workDate}`}>
                    Dòng {error.rowNumber} · {error.employeeCode || "Chưa có mã"} · {error.workDate || "Chưa có ngày"}: {error.message}
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        <DataTable
          columns={columns}
          data={summaries}
          loading={loading}
          rowKey={(row) => row.id}
          emptyTitle="Chưa có bảng công cho kỳ này."
          className="border-0 shadow-none"
          tableClassName="min-w-[1080px]"
          scrollContainerClassName="max-h-[420px] overflow-auto"
          stickyHeader
        />
      </Card>

      <Card
        title="Theo dõi thay đổi ngày công"
        description="Ghi nhận các lần import hoặc điều chỉnh làm thay đổi dữ liệu ngày công trong kỳ."
      >
        <DataTable
          columns={adjustmentLogColumns}
          data={adjustmentLogs}
          loading={dailyLoading}
          rowKey={(row) => row.id}
          emptyTitle="Chưa có thay đổi ngày công trong kỳ này."
          className="border-0 shadow-none"
          tableClassName="min-w-[1180px]"
          scrollContainerClassName="max-h-[540px] overflow-auto"
          stickyHeader
        />
      </Card>

      <DrawerForm
        open={Boolean(adjustTarget)}
        title="Điều chỉnh bảng công ngày"
        description={
          adjustTarget
            ? `${adjustTarget.employeeName} - ${formatDateTime(adjustTarget.workDate, "")}`
            : undefined
        }
        width="md"
        submitLabel="Lưu điều chỉnh"
        isSubmitting={adjusting}
        onClose={closeAdjustDrawer}
        onSubmit={submitDailyAdjustment}
      >
        <div className="space-y-4">
          <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-[var(--hicas-surface-muted)] p-4 text-sm text-[var(--hicas-text-secondary)]">
            Điều chỉnh bảng công sẽ được ghi nhận lịch sử và cập nhật lại dữ liệu tổng hợp tháng.
            Không thể điều chỉnh khi kỳ công hoặc payroll đã khóa.
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <NumberField
              label="Phút làm việc"
              value={adjustForm.workingMinutes}
              onChange={(value) => setAdjustForm((prev) => ({ ...prev, workingMinutes: value }))}
            />
            <NumberField
              label="Công quy đổi"
              value={adjustForm.workdayValue}
              step="0.5"
              onChange={(value) => setAdjustForm((prev) => ({ ...prev, workdayValue: value }))}
            />
            <NumberField
              label="Phút đi muộn"
              value={adjustForm.lateMinutes}
              onChange={(value) => setAdjustForm((prev) => ({ ...prev, lateMinutes: value }))}
            />
            <NumberField
              label="Phút về sớm"
              value={adjustForm.earlyLeaveMinutes}
              onChange={(value) => setAdjustForm((prev) => ({ ...prev, earlyLeaveMinutes: value }))}
            />
            <NumberField
              label="Phút làm thêm"
              value={adjustForm.overtimeMinutes}
              onChange={(value) => setAdjustForm((prev) => ({ ...prev, overtimeMinutes: value }))}
            />
            <label className="block">
              <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">
                Trạng thái ngày công
              </span>
              <select
                className="hicas-input w-full"
                value={adjustForm.attendanceStatus}
                onChange={(event) =>
                  setAdjustForm((prev) => ({
                    ...prev,
                    attendanceStatus: event.target.value as AttendanceDailySummary["attendanceStatus"],
                  }))
                }
              >
                {attendanceStatusOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
          </div>
          <label className="block">
            <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">
              Lý do điều chỉnh *
            </span>
            <textarea
              className="hicas-textarea min-h-[120px] w-full"
              value={adjustForm.reason}
              onChange={(event) => setAdjustForm((prev) => ({ ...prev, reason: event.target.value }))}
              placeholder="Ví dụ: Bổ sung checkout do nhân viên quên bấm công, có xác nhận của trưởng phòng."
            />
          </label>
        </div>
      </DrawerForm>
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
  if (summaries.every((item) => item.approvalStatus === "PendingHRReview")) return "PendingHRReview";
  if (summaries.every((item) => item.approvalStatus === "Draft")) return "Draft";
  return "Mixed";
};

const isLockedSummary = (item: AttendanceSummary) =>
  item.isPayrollLocked || item.approvalStatus === "Locked";

const canGeneratePeriod = (summaries: AttendanceSummary[], status: PeriodStatus) =>
  status === "Empty" ||
  (summaries.length > 0 &&
    summaries.every((item) => !isLockedSummary(item) && item.approvalStatus === "Draft"));

const canSubmitPeriod = (summaries: AttendanceSummary[]) =>
  summaries.some((item) => item.approvalStatus === "Draft") &&
  summaries.every(
    (item) =>
      !isLockedSummary(item) &&
      ["Draft", "PendingHRReview"].includes(item.approvalStatus),
  );

const canApprovePeriod = (summaries: AttendanceSummary[]) =>
  summaries.some((item) => item.approvalStatus === "PendingHRReview") &&
  summaries.every(
    (item) =>
      !isLockedSummary(item) &&
      ["PendingHRReview", "Approved"].includes(item.approvalStatus),
  );

const canLockPeriod = (summaries: AttendanceSummary[]) =>
  summaries.some((item) => item.approvalStatus === "Approved" && !item.isPayrollLocked) &&
  summaries.every(
    (item) =>
      isLockedSummary(item) ||
      item.approvalStatus === "Approved",
  );

const getPeriodStatusLabel = (status: PeriodStatus) => {
  switch (status) {
    case "Draft":
      return "Bản nháp";
    case "PendingHRReview":
      return "Chờ giám đốc duyệt";
    case "Approved":
      return "Đã duyệt";
    case "Locked":
      return "Đã khóa";
    case "Mixed":
      return "Cần đồng bộ";
    default:
      return "Chưa có dữ liệu";
  }
};

const getPeriodActionHint = (status: PeriodStatus, role: string, summaries: AttendanceSummary[]) => {
  const isHrOrAdmin = ["HR", "Admin"].includes(role);
  const isDirectorOrAdmin = ["Director", "Admin"].includes(role);

  if (status === "Empty") {
    return isHrOrAdmin
      ? "Bắt đầu bằng nút Tổng hợp để tạo bảng công tháng."
      : "Chưa có bảng công cho kỳ này.";
  }

  if (status === "Draft") {
    return isHrOrAdmin
      ? "HR có thể import/chỉnh bảng công ngày, sau đó gửi chốt sang Giám đốc."
      : "Bảng công đang ở bản nháp, chưa gửi phê duyệt.";
  }

  if (status === "PendingHRReview") {
    return isDirectorOrAdmin
      ? "Kỳ công đang chờ Giám đốc duyệt. Có thể xử lý tại đây hoặc trong trang Phê duyệt."
      : "Kỳ công đã gửi chốt và đang chờ Giám đốc duyệt.";
  }

  if (status === "Approved") {
    return isHrOrAdmin
      ? "Kỳ công đã được duyệt. HR có thể khóa kỳ để chuyển sang tính lương."
      : "Kỳ công đã được duyệt, chờ HR khóa kỳ.";
  }

  if (status === "Locked") {
    return "Kỳ công đã khóa, không thể chỉnh sửa hoặc import thêm.";
  }

  if (canSubmitPeriod(summaries)) {
    return isHrOrAdmin
      ? "Một phần kỳ công đã gửi chốt. Bấm Gửi chốt để đồng bộ các dòng còn lại."
      : "Kỳ công chưa đồng bộ, chờ HR gửi chốt các dòng còn lại.";
  }

  if (canApprovePeriod(summaries)) {
    return isDirectorOrAdmin
      ? "Một phần kỳ công đã duyệt. Bấm Duyệt để đồng bộ các dòng còn lại."
      : "Kỳ công chưa đồng bộ, chờ Giám đốc duyệt các dòng còn lại.";
  }

  if (canLockPeriod(summaries)) {
    return isHrOrAdmin
      ? "Một phần kỳ công đã khóa. Bấm Khóa kỳ để khóa các dòng còn lại."
      : "Kỳ công chưa đồng bộ, chờ HR khóa các dòng còn lại.";
  }

  return "Dữ liệu trong kỳ chưa đồng nhất. Vui lòng kiểm tra trạng thái từng dòng.";
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

const getAttendanceStatusLabel = (status: AttendanceDailySummary["attendanceStatus"]) =>
  attendanceStatusOptions.find((item) => item.value === status)?.label || status;

const formatDateForImport = (value: string) => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
};

const buildChangeSummary = (log: AttendanceAdjustmentLog) => {
  const oldValue = parseSnapshot(log.oldValueJson);
  const newValue = parseSnapshot(log.newValueJson);
  if (Object.keys(oldValue).length === 0) {
    return `Tạo mới dữ liệu ngày công: ${describeSnapshot(newValue)}`;
  }

  const fields: Array<[string, string, (value: unknown) => string]> = [
    ["WorkingMinutes", "Giờ làm", (value) => formatMinutesAsHours(toNumber(value))],
    ["LateMinutes", "Đi muộn", (value) => formatMinutes(toNumber(value))],
    ["EarlyLeaveMinutes", "Về sớm", (value) => formatMinutes(toNumber(value))],
    ["OvertimeMinutes", "OT", (value) => formatMinutes(toNumber(value))],
    ["WorkdayValue", "Công", (value) => String(value ?? 0)],
    ["AttendanceStatus", "Trạng thái", (value) => getAttendanceStatusLabel(String(value) as AttendanceDailySummary["attendanceStatus"])],
  ];

  const changes = fields
    .filter(([key]) => String(oldValue[key] ?? "") !== String(newValue[key] ?? ""))
    .map(([key, label, formatter]) => `${label}: ${formatter(oldValue[key])} → ${formatter(newValue[key])}`);

  return changes.length > 0 ? changes.join("\n") : "Đã ghi nhận điều chỉnh";
};

const parseSnapshot = (value?: string | null): Record<string, unknown> => {
  if (!value) return {};
  try {
    const parsed = JSON.parse(value);
    return parsed && typeof parsed === "object" ? parsed : {};
  } catch {
    return {};
  }
};

const describeSnapshot = (snapshot: Record<string, unknown>) => {
  const parts = [
    `Giờ làm ${formatMinutesAsHours(toNumber(snapshot.WorkingMinutes))}`,
    `Công ${snapshot.WorkdayValue ?? 0}`,
    `OT ${formatMinutes(toNumber(snapshot.OvertimeMinutes))}`,
  ];
  return parts.join(", ");
};

const toNumber = (value: unknown) => {
  const number = Number(value);
  return Number.isFinite(number) ? number : 0;
};

const formatMinutes = (value: number) => {
  if (!value || value <= 0) return "0 phút";
  const hours = Math.floor(value / 60);
  const minutes = value % 60;
  if (hours === 0) return `${minutes} phút`;
  if (minutes === 0) return `${hours} giờ`;
  return `${hours} giờ ${minutes} phút`;
};

const emptyAdjustForm = (): AdjustForm => ({
  workingMinutes: "0",
  lateMinutes: "0",
  earlyLeaveMinutes: "0",
  overtimeMinutes: "0",
  workdayValue: "0",
  attendanceStatus: "Present",
  reason: "",
});

const toNullableNumber = (value: string) => {
  const normalized = value.trim();
  if (normalized === "") return null;
  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : null;
};

type NumberFieldProps = {
  label: string;
  value: string;
  step?: string;
  onChange: (value: string) => void;
};

const NumberField = ({ label, value, step = "1", onChange }: NumberFieldProps) => (
  <label className="block">
    <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">{label}</span>
    <input
      type="number"
      min={0}
      step={step}
      value={value}
      onChange={(event) => onChange(event.target.value)}
      className="hicas-input w-full"
    />
  </label>
);

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
