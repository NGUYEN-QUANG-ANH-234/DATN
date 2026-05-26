import { useCallback, useEffect, useState } from "react";
import { Download } from "lucide-react";
import {
  FeatureCard,
  FeaturePage,
  primaryButtonClass,
} from "../../../core/components/FeatureShell";
import { useNotification } from "../../../core/context/NotificationContext";
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

  return (
    <FeaturePage
      title="Tổng hợp bảng công"
      description="Dữ liệu tổng hợp tháng dùng làm cầu nối sang công thức lương: ngày công, đi muộn, về sớm và OT hợp lệ."
      width="wide"
    >
      <FeatureCard title="Kỳ tổng hợp">
        <div className="grid gap-4 md:grid-cols-[160px_160px_auto_auto]">
          <label className="block">
            <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
              Tháng
            </span>
            <input
              type="number"
              min={1}
              max={12}
              value={month}
              onChange={(event) => setMonth(Number(event.target.value))}
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            />
          </label>
          <label className="block">
            <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
              Năm
            </span>
            <input
              type="number"
              min={2000}
              max={2100}
              value={year}
              onChange={(event) => setYear(Number(event.target.value))}
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            />
          </label>
          <div className="flex items-end">
            <button
              type="button"
              disabled={loading}
              onClick={generate}
              className={primaryButtonClass}
            >
              Tổng hợp lại
            </button>
          </div>
          <div className="flex items-end">
            <button
              type="button"
              disabled={loading || summaries.length === 0}
              onClick={exportExcel}
              className={`${primaryButtonClass} inline-flex items-center gap-2`}
            >
              <Download size={16} />
              Xuất Excel
            </button>
          </div>
        </div>
      </FeatureCard>

      <FeatureCard title="Bảng công tổng hợp">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[920px] text-left text-sm">
            <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
              <tr>
                <th className="px-3 py-2">Nhân viên</th>
                <th className="px-3 py-2">Phòng ban</th>
                <th className="px-3 py-2">Ngày công</th>
                <th className="px-3 py-2">Đi muộn</th>
                <th className="px-3 py-2">Về sớm</th>
                <th className="px-3 py-2">OT hợp lệ</th>
                <th className="px-3 py-2">Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              {summaries.map((item) => (
                <tr key={item.id} className="border-b">
                  <td className="px-3 py-3">
                    <p className="font-semibold text-gray-900">{item.employeeName}</p>
                    <p className="text-xs text-gray-500">{item.employeeCode}</p>
                  </td>
                  <td className="px-3 py-3">
                    {item.departmentName || "Chưa có phòng ban"}
                  </td>
                  <td className="px-3 py-3">{item.workDays}</td>
                  <td className="px-3 py-3">{formatMinutes(item.lateMinutes)}</td>
                  <td className="px-3 py-3">
                    {formatMinutes(item.earlyLeaveMinutes)}
                  </td>
                  <td className="px-3 py-3">
                    {formatMinutes(item.actualOtMinutes)}
                  </td>
                  <td className="px-3 py-3">
                    {item.isPayrollLocked ? "Đã khóa lương" : "Có thể cập nhật"}
                  </td>
                </tr>
              ))}
              {summaries.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-3 py-6 text-center text-gray-500">
                    Chưa có bảng công cho kỳ này.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </FeatureCard>
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
          <td>${item.lateMinutes}</td>
          <td>${item.earlyLeaveMinutes}</td>
          <td>${item.actualOtMinutes}</td>
          <td>${(item.actualOtMinutes / 60).toFixed(2)}</td>
          <td>${item.isPayrollLocked ? "Đã khóa lương" : "Có thể cập nhật"}</td>
          <td>${formatExcelDate(item.generatedAt)}</td>
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
          <tr><td class="title" colspan="13">Bảng công tổng hợp tháng ${String(month).padStart(2, "0")}/${year}</td></tr>
          <tr><td colspan="13">Ngày xuất: ${new Date().toLocaleString("vi-VN")}</td></tr>
          <tr>
            <th>STT</th>
            <th>Mã nhân viên</th>
            <th>Nhân viên</th>
            <th>Phòng ban</th>
            <th>Tháng</th>
            <th>Năm</th>
            <th>Ngày công</th>
            <th>Đi muộn (phút)</th>
            <th>Về sớm (phút)</th>
            <th>OT hợp lệ (phút)</th>
            <th>OT hợp lệ (giờ)</th>
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

const formatExcelDate = (value: string) => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return date.toLocaleString("vi-VN");
};
