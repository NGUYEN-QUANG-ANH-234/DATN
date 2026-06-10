import { Download, FileSpreadsheet, Upload } from "lucide-react";
import {
  FeatureCard,
  FeaturePage,
  fieldClass,
  primaryButtonClass,
  secondaryButtonClass,
} from "../../../core/components/FeatureShell";
import { useKpiImport } from "../hooks/useKpiImport";

export const KpiImportPage = () => {
  const {
    fileName,
    period,
    setPeriod,
    deptId,
    setDeptId,
    departments,
    loading,
    result,
    errors,
    setFile,
    handleSubmit,
    downloadTemplate,
  } = useKpiImport();

  return (
    <FeaturePage
      title="Thiết lập KPI đầu kỳ"
      description="Nhập hoặc cập nhật KPI đầu kỳ cho nhân viên theo phòng ban."
      width="wide"
      actions={
        <button
          type="button"
          onClick={downloadTemplate}
          className={secondaryButtonClass}
        >
          <Download size={16} />
          Tải mẫu CSV
        </button>
      }
    >
      <FeatureCard
        title="Chọn file KPI"
        description="Mỗi nhân viên phải có tổng trọng số đúng 100%. Import lại cùng kỳ sẽ thay thế KPI cũ nếu chưa đồng bộ lương."
      >
        <form onSubmit={handleSubmit} className="grid gap-4 lg:grid-cols-[180px_220px_1fr_auto] lg:items-end">
          <label className="block">
            <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
              Kỳ đánh giá
            </span>
            <input
              value={period}
              onChange={(event) => setPeriod(event.target.value)}
              placeholder="MM/yyyy"
              className={fieldClass}
            />
          </label>

          <label className="block">
            <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
              Phòng ban
            </span>
            <select
              value={deptId}
              onChange={(event) => setDeptId(event.target.value)}
              className={fieldClass}
            >
              <option value="">Tự nhận theo tài khoản</option>
              {departments.map((dept) => (
                <option key={dept.id} value={dept.id}>
                  {dept.name}
                </option>
              ))}
            </select>
          </label>

          <label className="block">
            <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
              File KPI
            </span>
            <div className="flex min-h-10 items-center gap-3 rounded-lg border border-gray-300 bg-white px-3 py-2">
              <FileSpreadsheet size={18} className="text-blue-600" />
              <span className="min-w-0 flex-1 truncate text-sm text-gray-700">
                {fileName}
              </span>
              <input
                type="file"
                accept=".xlsx,.csv"
                onChange={(event) => setFile(event.target.files?.[0] || null)}
                className="max-w-48 text-sm"
              />
            </div>
          </label>

          <button type="submit" disabled={loading} className={primaryButtonClass}>
            <Upload size={16} />
            {loading ? "Đang nhập..." : "Nhập KPI"}
          </button>
        </form>
      </FeatureCard>

      {result && (
        <FeatureCard title="Kết quả nhập file">
          <div className="grid gap-3 text-sm md:grid-cols-3 lg:grid-cols-5">
            <SummaryItem label="Kỳ KPI" value={result.period} />
            <SummaryItem label="Số dòng" value={result.totalRows} />
            <SummaryItem label="Nhân viên" value={result.createdOrUpdatedReviews} />
            <SummaryItem label="Chỉ tiêu KPI" value={result.createdDetails} />
            <SummaryItem label="Tổng trọng số" value={result.totalAssignedWeight} />
          </div>
        </FeatureCard>
      )}

      {errors.length > 0 && (
        <FeatureCard title="Dòng lỗi cần sửa">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[640px] text-left text-sm">
              <thead className="border-b bg-red-50 text-xs uppercase text-red-700">
                <tr>
                  <th className="px-3 py-2">Dòng</th>
                  <th className="px-3 py-2">Nội dung lỗi</th>
                </tr>
              </thead>
              <tbody>
                {errors.map((item, index) => (
                  <tr key={`${item.rowNumber}-${index}`} className="border-b">
                    <td className="px-3 py-2 font-mono">{item.rowNumber || "-"}</td>
                    <td className="px-3 py-2">{item.message}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </FeatureCard>
      )}
    </FeaturePage>
  );
};

const SummaryItem = ({ label, value }: { label: string; value: string | number }) => (
  <div className="rounded-lg border border-gray-200 bg-gray-50 p-4">
    <p className="text-xs font-semibold uppercase text-gray-500">{label}</p>
    <p className="mt-1 text-xl font-bold text-gray-900">{value}</p>
  </div>
);
