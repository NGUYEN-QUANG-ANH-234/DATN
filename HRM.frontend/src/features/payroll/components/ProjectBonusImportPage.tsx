import { useCallback, useEffect, useMemo, useState } from "react";
import { Download, Eye, FileSpreadsheet, RefreshCw, Send, UploadCloud, XCircle } from "lucide-react";
import { Button, Card, ConfirmDialog, DrawerForm } from "../../../components/ui";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { useNotification } from "../../../core/context/NotificationContext";
import { payrollApi } from "../api/payrollApi";
import type { ProjectBonusImportBatch, ProjectBonusImportLine, ProjectBonusImportPreview } from "../types/payroll";
import { formatMoney } from "../utils";
import { usePayrollPeriod } from "../hooks/usePayrollPeriod";

const cancellableStatuses = new Set(["Draft", "PendingReview", "Rejected"]);

export const ProjectBonusImportPage = () => {
  const { month, year, period, setMonth, setYear } = usePayrollPeriod();
  const { triggerAlert } = useNotification();
  const [file, setFile] = useState<File | null>(null);
  const [overwrite, setOverwrite] = useState(false);
  const [note, setNote] = useState("");
  const [preview, setPreview] = useState<ProjectBonusImportPreview | null>(null);
  const [batches, setBatches] = useState<ProjectBonusImportBatch[]>([]);
  const [selectedBatch, setSelectedBatch] = useState<ProjectBonusImportBatch | null>(null);
  const [cancelTarget, setCancelTarget] = useState<ProjectBonusImportBatch | null>(null);
  const [loading, setLoading] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [submittingId, setSubmittingId] = useState<number | null>(null);
  const [cancellingId, setCancellingId] = useState<number | null>(null);

  const previewTotal = useMemo(
    () => preview?.lines.reduce((sum, line) => sum + (line.isValid ? line.bonusAmount : 0), 0) ?? 0,
    [preview],
  );

  const buildFormData = () => {
    if (!file) throw new Error("Vui lòng chọn file CSV thưởng dự án.");
    const formData = new FormData();
    formData.append("file", file);
    formData.append("periodMonth", String(month));
    formData.append("periodYear", String(year));
    formData.append("overwrite", String(overwrite));
    formData.append("note", note);
    return formData;
  };

  const loadBatches = useCallback(async () => {
    try {
      const res = await payrollApi.getProjectBonusImports(month, year);
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
      "MaNhanVien,KyLuong,MaDuAn,TenDuAn,SoTienThuong,LyDo,ChiuThueTNCN,TinhDongBaoHiem,GhiChu",
      `NV0001,${period},HICAS-ERP,Trien khai ERP,5000000,Thuong hoan thanh du an,Co,Khong,Ghi chu neu co`,
    ];
    const blob = new Blob([`\uFEFF${rows.join("\n")}`], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `mau-thuong-du-an-${period.replace("/", "-")}.csv`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  const handlePreview = async () => {
    try {
      setLoading(true);
      const res = await payrollApi.previewProjectBonusImport(buildFormData());
      setPreview(res.data);
      triggerAlert(
        res.data.canSave ? "success" : "warning",
        res.data.canSave ? "Đã đọc file thưởng dự án" : "File còn dữ liệu cần kiểm tra",
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
      const res = await payrollApi.importProjectBonus(buildFormData());
      triggerAlert("success", "Đã lưu nháp thưởng dự án", `Batch #${res.data.id} đã sẵn sàng để gửi duyệt.`);
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
      await payrollApi.submitProjectBonusImport(id);
      triggerAlert("success", "Đã gửi duyệt", "Batch thưởng dự án đã được chuyển sang bàn làm việc phê duyệt.");
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
      const res = await payrollApi.getProjectBonusImportDetail(id);
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
      await payrollApi.cancelProjectBonusImport(cancelTarget.id, {
        note: "Hủy batch thưởng dự án từ trang quản lý.",
      });
      triggerAlert("success", "Đã hủy batch", "Batch thưởng dự án đã được chuyển sang trạng thái đã hủy.");
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
      title="Thưởng dự án"
      description="Import khoản thưởng theo dự án và gửi Giám đốc duyệt trước khi tính lương."
      width="wide"
    >
      <Card
        title="Import thưởng dự án"
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
            <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">Ghi chú</span>
            <input
              value={note}
              onChange={(event) => setNote(event.target.value)}
              className="hicas-input w-full"
              placeholder="Ví dụ: Thưởng hoàn thành dự án trong kỳ"
            />
          </label>
        </div>

        <div className="mt-4 grid gap-4 lg:grid-cols-[1fr_280px]">
          <label className="flex cursor-pointer flex-col items-center justify-center gap-2 rounded-[var(--radius-lg)] border border-dashed border-[var(--hicas-border)] bg-[var(--hicas-bg-soft)] px-6 py-8 text-center text-sm text-[var(--hicas-text-secondary)]">
            <FileSpreadsheet size={30} className="text-[var(--hicas-orange)]" />
            <span className="text-base font-semibold text-[var(--hicas-text-main)]">
              {file ? file.name : "Chọn file CSV thưởng dự án"}
            </span>
            <span>MaNhanVien, KyLuong, MaDuAn, TenDuAn, SoTienThuong, LyDo, ChiuThueTNCN, TinhDongBaoHiem, GhiChu</span>
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
                  Chỉ thay thế dòng trùng ở batch chưa duyệt. Batch đã duyệt luôn được giữ nguyên.
                </span>
              </span>
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
            <PreviewMetric label="Tổng thưởng" value={formatMoney(previewTotal)} strong />
          </div>

          {preview.globalErrors.length > 0 ? (
            <div className="mb-4 rounded-[var(--radius-lg)] border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              {preview.globalErrors.map((message) => (
                <p key={message}>{message}</p>
              ))}
            </div>
          ) : null}

          <ProjectBonusLineTable lines={preview.lines} />
        </Card>
      ) : null}

      <Card title={`Batch thưởng dự án ${period}`}>
        <div className="overflow-x-auto">
          <table className="min-w-full text-left text-sm">
            <thead className="border-b border-[var(--hicas-border)] text-xs uppercase text-[var(--hicas-text-secondary)]">
              <tr>
                <th className="px-3 py-3">File</th>
                <th className="px-3 py-3">Dòng</th>
                <th className="px-3 py-3">Tổng thưởng</th>
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
                    <td className="px-3 py-3 font-semibold text-[var(--hicas-text-main)]">{batch.fileName}</td>
                    <td className="px-3 py-3">
                      {batch.validRows}/{batch.totalRows}
                      {batch.errorRows > 0 ? <span className="ml-2 text-red-600">({batch.errorRows} lỗi)</span> : null}
                    </td>
                    <td className="px-3 py-3 font-semibold">{formatMoney(batch.totalAmount)}</td>
                    <td className="px-3 py-3">{batch.statusText || batch.status}</td>
                    <td className="px-3 py-3">{batch.uploadedByName || `#${batch.uploadedByAccountId}`}</td>
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
                  <td colSpan={6} className="px-3 py-8 text-center text-[var(--hicas-text-secondary)]">
                    Chưa có batch thưởng dự án trong kỳ này.
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
        description={selectedBatch ? `${selectedBatch.fileName} - ${selectedBatch.payrollPeriod}` : undefined}
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
              <PreviewMetric label="Tổng thưởng" value={formatMoney(selectedBatch.totalAmount)} strong />
            </div>
            <ProjectBonusLineTable lines={selectedBatch.lines} />
          </div>
        ) : null}
      </DrawerForm>

      <ConfirmDialog
        open={Boolean(cancelTarget)}
        title="Hủy batch thưởng dự án?"
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

const ProjectBonusLineTable = ({ lines }: { lines: ProjectBonusImportLine[] }) => (
  <div className="overflow-x-auto">
    <table className="min-w-full text-left text-sm">
      <thead className="border-b border-[var(--hicas-border)] text-xs uppercase text-[var(--hicas-text-secondary)]">
        <tr>
          <th className="px-3 py-3">Dòng</th>
          <th className="px-3 py-3">Nhân viên</th>
          <th className="px-3 py-3">Dự án</th>
          <th className="px-3 py-3">Số tiền</th>
          <th className="px-3 py-3">Thuế</th>
          <th className="px-3 py-3">Bảo hiểm</th>
          <th className="px-3 py-3">Kết quả</th>
        </tr>
      </thead>
      <tbody className="divide-y divide-[var(--hicas-border)]">
        {lines.map((line) => (
          <tr key={`${line.rowNumber}-${line.employeeCode}-${line.projectCode}`} className={line.isValid ? "" : "bg-red-50/60"}>
            <td className="px-3 py-3">{line.rowNumber}</td>
            <td className="px-3 py-3">
              <p className="font-semibold">{line.employeeName || line.employeeCode}</p>
              <p className="text-xs text-[var(--hicas-text-secondary)]">{line.employeeCode}</p>
            </td>
            <td className="px-3 py-3">
              <p className="font-semibold">{line.projectName}</p>
              <p className="text-xs text-[var(--hicas-text-secondary)]">{line.projectCode}</p>
            </td>
            <td className="px-3 py-3 font-semibold">{formatMoney(line.bonusAmount)}</td>
            <td className="px-3 py-3">{line.taxable ? "Có" : "Không"}</td>
            <td className="px-3 py-3">{line.insuranceContributable ? "Có" : "Không"}</td>
            <td className="px-3 py-3">
              {line.isValid ? (
                <span className="font-semibold text-emerald-700">Hợp lệ</span>
              ) : (
                <span className="text-red-700">{line.errorMessage || "Không hợp lệ"}</span>
              )}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  </div>
);

const getErrorMessage = (error: unknown) => {
  const maybe = error as { response?: { data?: { message?: string } }; message?: string };
  return maybe.response?.data?.message || maybe.message || (error instanceof Error ? error.message : "Vui lòng thử lại.");
};

const extractPreview = (error: unknown): ProjectBonusImportPreview | null => {
  const maybe = error as { response?: { data?: { data?: ProjectBonusImportPreview } } };
  return maybe.response?.data?.data ?? null;
};
