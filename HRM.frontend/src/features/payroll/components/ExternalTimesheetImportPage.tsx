import { useCallback, useEffect, useMemo, useState } from "react";
import { Download, Eye, FileSpreadsheet, RefreshCw, Send, UploadCloud, XCircle } from "lucide-react";
import { Button, Card, ConfirmDialog, DrawerForm } from "../../../components/ui";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { useNotification } from "../../../core/context/NotificationContext";
import { payrollApi } from "../api/payrollApi";
import type {
  ExternalTimesheetImportBatch,
  ExternalTimesheetImportPreview,
} from "../types/payroll";
import { formatMoney, formatNumber } from "../utils";
import { usePayrollPeriod } from "../hooks/usePayrollPeriod";
import { ExternalTimesheetPreviewTable } from "./ExternalTimesheetPreviewTable";

const cancellableStatuses = new Set(["Draft", "Imported", "Validated", "Rejected"]);

export const ExternalTimesheetImportPage = () => {
  const { month, year, period, setMonth, setYear } = usePayrollPeriod();
  const { triggerAlert } = useNotification();
  const [file, setFile] = useState<File | null>(null);
  const [sourceSystem, setSourceSystem] = useState("Timesheet cộng tác viên");
  const [overwrite, setOverwrite] = useState(false);
  const [note, setNote] = useState("");
  const [preview, setPreview] = useState<ExternalTimesheetImportPreview | null>(null);
  const [batches, setBatches] = useState<ExternalTimesheetImportBatch[]>([]);
  const [selectedBatch, setSelectedBatch] = useState<ExternalTimesheetImportBatch | null>(null);
  const [cancelTarget, setCancelTarget] = useState<ExternalTimesheetImportBatch | null>(null);
  const [loading, setLoading] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [submittingId, setSubmittingId] = useState<number | null>(null);
  const [cancellingId, setCancellingId] = useState<number | null>(null);

  const previewTotals = useMemo(
    () =>
      preview?.lines.reduce(
        (sum, line) => ({
          totalHours: sum.totalHours + (line.isValid ? line.approvedHours : 0),
          totalAmount: sum.totalAmount + (line.isValid ? line.amount : 0),
        }),
        { totalHours: 0, totalAmount: 0 },
      ) ?? { totalHours: 0, totalAmount: 0 },
    [preview],
  );

  const buildFormData = () => {
    if (!file) throw new Error("Vui lòng chọn file CSV giờ công cộng tác viên.");
    const formData = new FormData();
    formData.append("file", file);
    formData.append("importMonth", String(month));
    formData.append("importYear", String(year));
    formData.append("sourceSystem", sourceSystem);
    formData.append("overwrite", String(overwrite));
    formData.append("note", note);
    return formData;
  };

  const loadBatches = useCallback(async () => {
    try {
      const res = await payrollApi.getExternalTimesheetImports(month, year);
      setBatches(res.data ?? []);
    } catch {
      setBatches([]);
    }
  }, [month, year]);

  useEffect(() => {
    void loadBatches();
  }, [loadBatches]);

  const downloadTemplate = () => {
    const rows = [
      "MaNhanVien,HoTen,NgayLam,MaDuAn,MaCongViec,SoGioDuyet,DonGia,GhiChu,KyLuong",
      `NV0001,Nguyen Van A,${year}-${String(month).padStart(2, "0")}-01,HICAS-ERP,DEV-001,8,150000,Hoan thanh cong viec,${period}`,
    ];
    const blob = new Blob([`\uFEFF${rows.join("\n")}`], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `mau-gio-cong-cong-tac-vien-${period.replace("/", "-")}.csv`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  const handlePreview = async () => {
    try {
      setLoading(true);
      const res = await payrollApi.previewExternalTimesheetImport(buildFormData());
      setPreview(res.data);
      triggerAlert(
        res.data.canSave ? "success" : "warning",
        res.data.canSave ? "Đã đọc file giờ công" : "File còn dữ liệu cần kiểm tra",
        res.data.canSave
          ? "Dữ liệu hợp lệ và có thể lưu nháp."
          : "Vui lòng kiểm tra lỗi tổng quát và lỗi từng dòng trước khi lưu.",
      );
    } catch (error) {
      triggerAlert("error", "Không thể đọc file", getErrorMessage(error));
    } finally {
      setLoading(false);
    }
  };

  const handleImport = async () => {
    try {
      setLoading(true);
      const res = await payrollApi.importExternalTimesheet(buildFormData());
      triggerAlert("success", "Đã lưu nháp giờ công", `Batch #${res.data.id} đã sẵn sàng để gửi duyệt.`);
      setPreview(null);
      setFile(null);
      await loadBatches();
    } catch (error) {
      const maybePreview = extractPreview(error);
      if (maybePreview) setPreview(maybePreview);
      triggerAlert("error", "Chưa thể lưu batch", getErrorMessage(error));
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (id: number) => {
    try {
      setSubmittingId(id);
      await payrollApi.submitExternalTimesheetImport(id);
      triggerAlert("success", "Đã gửi duyệt", "Batch giờ công cộng tác viên đã chuyển sang bàn làm việc phê duyệt.");
      await loadBatches();
    } catch (error) {
      triggerAlert("error", "Không thể gửi duyệt", getErrorMessage(error));
    } finally {
      setSubmittingId(null);
    }
  };

  const handleOpenDetail = async (id: number) => {
    try {
      setDetailLoading(true);
      const res = await payrollApi.getExternalTimesheetImportDetail(id);
      setSelectedBatch(res.data);
    } catch (error) {
      triggerAlert("error", "Không thể tải chi tiết batch", getErrorMessage(error));
    } finally {
      setDetailLoading(false);
    }
  };

  const handleCancel = async () => {
    if (!cancelTarget) return;
    try {
      setCancellingId(cancelTarget.id);
      await payrollApi.cancelExternalTimesheetImport(cancelTarget.id, {
        note: "Hủy batch giờ công cộng tác viên từ trang quản lý.",
      });
      triggerAlert("success", "Đã hủy batch", "Batch giờ công cộng tác viên đã chuyển sang trạng thái đã hủy.");
      if (selectedBatch?.id === cancelTarget.id) setSelectedBatch(null);
      setCancelTarget(null);
      await loadBatches();
    } catch (error) {
      triggerAlert("error", "Không thể hủy batch", getErrorMessage(error));
    } finally {
      setCancellingId(null);
    }
  };

  return (
    <FeaturePage
      title="Giờ công cộng tác viên"
      description="Import giờ công đã được xác nhận và gửi duyệt trước khi tổng hợp lương."
      width="wide"
    >
      <Card
        title="Import giờ công"
        actions={
          <div className="flex flex-wrap gap-2">
            <Button type="button" variant="secondary" onClick={downloadTemplate}>
              <Download size={16} />
              Tải file mẫu
            </Button>
            <Button type="button" variant="secondary" onClick={() => void loadBatches()}>
              <RefreshCw size={16} />
              Làm mới
            </Button>
          </div>
        }
      >
        <div className="grid gap-4 lg:grid-cols-[140px_140px_1fr]">
          <label className="block">
            <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">Tháng</span>
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
            <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">Năm</span>
            <input
              type="number"
              min={2000}
              max={2100}
              value={year}
              onChange={(event) => setYear(Number(event.target.value))}
              className="hicas-input w-full"
            />
          </label>
          <label className="block">
            <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">Nguồn dữ liệu</span>
            <input
              value={sourceSystem}
              onChange={(event) => setSourceSystem(event.target.value)}
              className="hicas-input w-full"
              placeholder="Ví dụ: Timesheet cộng tác viên"
            />
          </label>
        </div>

        <div className="mt-4 grid gap-4 lg:grid-cols-[1fr_280px]">
          <label className="flex cursor-pointer flex-col items-center justify-center gap-2 rounded-[var(--radius-lg)] border border-dashed border-[var(--hicas-border)] bg-[var(--hicas-bg-soft)] px-6 py-8 text-center text-sm text-[var(--hicas-text-secondary)]">
            <FileSpreadsheet size={30} className="text-[var(--hicas-orange)]" />
            <span className="text-base font-semibold text-[var(--hicas-text-main)]">
              {file ? file.name : "Chọn file CSV giờ công cộng tác viên"}
            </span>
            <span>MaNhanVien, HoTen, NgayLam, MaDuAn, MaCongViec, SoGioDuyet, DonGia, GhiChu, KyLuong</span>
            <input
              type="file"
              accept=".csv,text/csv"
              className="hidden"
              onChange={(event) => {
                const selected = event.target.files?.[0] ?? null;
                setFile(selected);
                setPreview(null);
              }}
            />
          </label>

          <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-4">
            <label className="flex items-start gap-3 text-sm text-[var(--hicas-text-main)]">
              <input
                type="checkbox"
                checked={overwrite}
                onChange={(event) => setOverwrite(event.target.checked)}
                className="mt-1"
              />
              <span>
                <span className="block font-semibold">Ghi đè dòng trùng</span>
                <span className="text-[var(--hicas-text-secondary)]">
                  Chỉ thay thế dòng trùng ở batch chưa duyệt. Batch đã duyệt hoặc đã đưa vào payroll luôn được giữ nguyên.
                </span>
              </span>
            </label>
            <label className="mt-4 block">
              <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">Ghi chú</span>
              <textarea
                value={note}
                onChange={(event) => setNote(event.target.value)}
                className="hicas-input min-h-[88px] w-full"
                placeholder="Ví dụ: Giờ công đã đối chiếu với trưởng dự án"
              />
            </label>
            <div className="mt-4 grid gap-2">
              <Button type="button" variant="secondary" onClick={handlePreview} disabled={!file || loading}>
                <UploadCloud size={16} />
                Xem trước
              </Button>
              <Button
                type="button"
                onClick={handleImport}
                disabled={!file || loading || (preview ? !preview.canSave : false)}
              >
                <FileSpreadsheet size={16} />
                Lưu nháp
              </Button>
            </div>
          </div>
        </div>
      </Card>

      {preview ? (
        <Card title={`Dữ liệu xem trước ${preview.payrollPeriod}`}>
          <div className="mb-4 grid gap-3 md:grid-cols-4">
            <PreviewMetric label="Tổng dòng" value={preview.totalRows} />
            <PreviewMetric label="Hợp lệ" value={preview.validRows} />
            <PreviewMetric label="Cần sửa" value={preview.errorRows} danger={preview.errorRows > 0} />
            <PreviewMetric label="Tổng giờ" value={formatNumber(previewTotals.totalHours)} />
            <PreviewMetric label="Tổng tiền" value={formatMoney(previewTotals.totalAmount)} strong />
          </div>

          {preview.globalErrors.length > 0 ? (
            <div className="mb-4 rounded-[var(--radius-lg)] border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              {preview.globalErrors.map((message) => (
                <p key={message}>{message}</p>
              ))}
            </div>
          ) : null}

          <ExternalTimesheetPreviewTable lines={preview.lines} />
        </Card>
      ) : null}

      <Card title={`Batch giờ công ${period}`}>
        <div className="overflow-x-auto">
          <table className="min-w-full text-left text-sm">
            <thead className="border-b border-[var(--hicas-border)] text-xs uppercase text-[var(--hicas-text-secondary)]">
              <tr>
                <th className="px-3 py-3">File</th>
                <th className="px-3 py-3">Dòng</th>
                <th className="px-3 py-3">Tổng giờ</th>
                <th className="px-3 py-3">Tổng tiền</th>
                <th className="px-3 py-3">Trạng thái</th>
                <th className="px-3 py-3">Người import</th>
                <th className="px-3 py-3 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--hicas-border)]">
              {batches.map((batch) => {
                const status = String(batch.status);
                const canSubmit = status === "Draft";
                const canCancel = cancellableStatuses.has(status);
                return (
                  <tr key={batch.id} className="align-middle">
                    <td className="px-3 py-3 font-semibold text-[var(--hicas-text-main)]">
                      {batch.fileName || batch.sourceSystem}
                    </td>
                    <td className="px-3 py-3">
                      {batch.validRows}/{batch.totalRows}
                      {batch.errorRows > 0 ? <span className="ml-2 text-red-600">({batch.errorRows} lỗi)</span> : null}
                    </td>
                    <td className="px-3 py-3 font-semibold">{formatNumber(batch.totalHours)}</td>
                    <td className="px-3 py-3 font-semibold">{formatMoney(batch.totalAmount)}</td>
                    <td className="px-3 py-3">{batch.statusText || batch.status}</td>
                    <td className="px-3 py-3">{batch.importedByName || `#${batch.importedByAccountId}`}</td>
                    <td className="px-3 py-3">
                      <div className="flex flex-wrap justify-end gap-2">
                        <Button
                          type="button"
                          variant="secondary"
                          size="sm"
                          onClick={() => void handleOpenDetail(batch.id)}
                          disabled={detailLoading}
                        >
                          <Eye size={14} />
                          Chi tiết
                        </Button>
                        {canSubmit ? (
                          <Button
                            type="button"
                            size="sm"
                            onClick={() => void handleSubmit(batch.id)}
                            disabled={submittingId === batch.id}
                          >
                            <Send size={14} />
                            Gửi duyệt
                          </Button>
                        ) : null}
                        {canCancel ? (
                          <Button
                            type="button"
                            variant="danger"
                            size="sm"
                            onClick={() => setCancelTarget(batch)}
                            disabled={cancellingId === batch.id}
                          >
                            <XCircle size={14} />
                            Hủy
                          </Button>
                        ) : null}
                      </div>
                    </td>
                  </tr>
                );
              })}
              {batches.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-3 py-8 text-center text-[var(--hicas-text-secondary)]">
                    Chưa có batch giờ công cộng tác viên trong kỳ này.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
      </Card>

      <DrawerForm
        open={Boolean(selectedBatch) || detailLoading}
        title={selectedBatch ? `Chi tiết batch #${selectedBatch.id}` : "Đang tải chi tiết batch"}
        description={selectedBatch ? `${selectedBatch.fileName || selectedBatch.sourceSystem} - ${selectedBatch.payrollPeriod}` : undefined}
        width="xl"
        onClose={() => setSelectedBatch(null)}
        footer={
          <Button type="button" variant="secondary" onClick={() => setSelectedBatch(null)}>
            Đóng
          </Button>
        }
      >
        {detailLoading && !selectedBatch ? (
          <p className="text-sm text-[var(--hicas-text-secondary)]">Đang tải dữ liệu...</p>
        ) : selectedBatch ? (
          <div className="space-y-5">
            <div className="grid gap-3 md:grid-cols-4">
              <PreviewMetric label="Tổng dòng" value={selectedBatch.totalRows} />
              <PreviewMetric label="Hợp lệ" value={selectedBatch.validRows} />
              <PreviewMetric label="Cần sửa" value={selectedBatch.errorRows} danger={selectedBatch.errorRows > 0} />
              <PreviewMetric label="Tổng giờ" value={formatNumber(selectedBatch.totalHours)} />
              <PreviewMetric label="Tổng tiền" value={formatMoney(selectedBatch.totalAmount)} strong />
            </div>
            <ExternalTimesheetPreviewTable lines={selectedBatch.lines} />
          </div>
        ) : null}
      </DrawerForm>

      <ConfirmDialog
        open={Boolean(cancelTarget)}
        title="Hủy batch giờ công?"
        description="Batch đã hủy sẽ không được đưa vào phê duyệt hoặc tính lương. Batch đã duyệt không thể hủy để giữ an toàn dữ liệu."
        confirmLabel="Hủy batch"
        tone="danger"
        isLoading={cancelTarget ? cancellingId === cancelTarget.id : false}
        onClose={() => setCancelTarget(null)}
        onConfirm={() => void handleCancel()}
      />
    </FeaturePage>
  );
};

const PreviewMetric = ({
  label,
  value,
  strong,
  danger,
}: {
  label: string;
  value: string | number;
  strong?: boolean;
  danger?: boolean;
}) => (
  <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white px-4 py-3">
    <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">{label}</p>
    <p
      className={`mt-1 text-xl font-bold ${
        danger ? "text-red-600" : strong ? "text-[var(--hicas-orange)]" : "text-[var(--hicas-text-main)]"
      }`}
    >
      {value}
    </p>
  </div>
);

const getErrorMessage = (error: unknown) => {
  const maybe = error as { response?: { data?: { message?: string } }; message?: string };
  return maybe.response?.data?.message || maybe.message || (error instanceof Error ? error.message : "Vui lòng thử lại.");
};

const extractPreview = (error: unknown): ExternalTimesheetImportPreview | null => {
  const maybe = error as { response?: { data?: { data?: ExternalTimesheetImportPreview } } };
  return maybe.response?.data?.data ?? null;
};
