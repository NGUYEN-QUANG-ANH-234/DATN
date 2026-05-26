import { useEffect, useMemo, useState } from "react";
import { Download, FileSpreadsheet, Upload } from "lucide-react";
import {
  FeatureCard,
  FeaturePage,
  fieldClass,
  primaryButtonClass,
  secondaryButtonClass,
} from "../../../core/components/FeatureShell";
import { useNotification } from "../../../core/context/NotificationContext";
import { departmentApi } from "../../organization/api/departmentApi";
import type { DepartmentTree } from "../../organization/types/department";
import { kpiApi, type KpiImportError, type KpiImportResult } from "../api/kpiApi";

const current = new Date();
const defaultPeriod = `${String(current.getMonth() + 1).padStart(2, "0")}/${current.getFullYear()}`;

type DepartmentOption = { id: number; name: string };

const flattenDepartments = (nodes: DepartmentTree[], level = 0): DepartmentOption[] =>
  nodes.flatMap((node) => [
    { id: node.id, name: `${"-- ".repeat(level)}${node.deptName}` },
    ...flattenDepartments(node.children || [], level + 1),
  ]);

export const KpiImportPage = () => {
  const { triggerAlert } = useNotification();
  const [file, setFile] = useState<File | null>(null);
  const [period, setPeriod] = useState(defaultPeriod);
  const [deptId, setDeptId] = useState("");
  const [departments, setDepartments] = useState<DepartmentOption[]>([]);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<KpiImportResult | null>(null);
  const [errors, setErrors] = useState<KpiImportError[]>([]);

  const fileName = useMemo(() => file?.name || "Chua chon file", [file]);

  useEffect(() => {
    let mounted = true;

    const loadDepartments = async () => {
      try {
        const response = await departmentApi.getTree();
        if (mounted) setDepartments(flattenDepartments(response.data || []));
      } catch (error) {
        console.error("Khong the tai danh sach phong ban KPI:", error);
      }
    };

    void loadDepartments();
    return () => {
      mounted = false;
    };
  }, []);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!file) {
      triggerAlert("warning", "Thieu file KPI", "Vui long chon file Excel hoac CSV.");
      return;
    }

    setLoading(true);
    setErrors([]);
    setResult(null);

    try {
      const response = await kpiApi.importKpis(
        file,
        period,
        deptId ? Number(deptId) : undefined,
      );
      setResult(response.data);
      triggerAlert(
        "success",
        "Import KPI thanh cong",
        response.message || "Du lieu KPI dau ky da duoc ghi nhan.",
      );
    } catch (error) {
      const payload = (error as { response?: { data?: { errors?: KpiImportError[] } } })
        ?.response?.data;
      if (payload?.errors) setErrors(payload.errors);

      triggerAlert(
        "error",
        "Import KPI that bai",
        error instanceof Error ? error.message : "File KPI chua hop le.",
      );
    } finally {
      setLoading(false);
    }
  };

  const downloadTemplate = () => {
    const csv = [
      "MaNV,MaKPI,TenChiTieu,TrongSo,MucTieu,DonVi,MoTa",
      "NV0001,KPI-001,Hoan thanh ke hoach cong viec,40,100,%,Hoan thanh cac dau viec duoc giao",
      "NV0001,KPI-002,Chat luong cong viec,35,95,%,Dam bao chat luong dau ra",
      "NV0001,KPI-003,Tuan thu quy dinh,25,100,%,Dung quy trinh va ky luat",
    ].join("\n");

    const blob = new Blob(["\ufeff", csv], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "mau-import-kpi.csv";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  return (
    <FeaturePage
      title="Thiet lap KPI dau ky"
      description="Import chi tieu KPI cho nhan vien trong phong ban. Diem tru khong nhap o buoc giao KPI ma se phat sinh tu he thong hoac buoc truong phong danh gia."
      width="wide"
      actions={
        <button
          type="button"
          onClick={downloadTemplate}
          className={secondaryButtonClass}
        >
          <Download size={16} />
          Tai mau CSV
        </button>
      }
    >
      <FeatureCard title="Import file KPI" description="File can co cac cot: MaNV, TenChiTieu, TrongSo. Cac cot MaKPI, MucTieu, DonVi, MoTa la tuy chon.">
        <form onSubmit={handleSubmit} className="grid gap-4 lg:grid-cols-[180px_220px_1fr_auto] lg:items-end">
          <label className="block">
            <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
              Ky danh gia
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
              Phong ban
            </span>
            <select
              value={deptId}
              onChange={(event) => setDeptId(event.target.value)}
              className={fieldClass}
            >
              <option value="">Tu nhan theo tai khoan</option>
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
            {loading ? "Dang import..." : "Import KPI"}
          </button>
        </form>
      </FeatureCard>

      {result && (
        <FeatureCard title="Ket qua import">
          <div className="grid gap-3 text-sm md:grid-cols-3 lg:grid-cols-5">
            <SummaryItem label="Ky KPI" value={result.period} />
            <SummaryItem label="So dong" value={result.totalRows} />
            <SummaryItem label="Nhan vien" value={result.createdOrUpdatedReviews} />
            <SummaryItem label="Chi tieu KPI" value={result.createdDetails} />
            <SummaryItem label="Tong trong so" value={result.totalAssignedWeight} />
          </div>
        </FeatureCard>
      )}

      {errors.length > 0 && (
        <FeatureCard title="Dong loi can sua">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[640px] text-left text-sm">
              <thead className="border-b bg-red-50 text-xs uppercase text-red-700">
                <tr>
                  <th className="px-3 py-2">Dong</th>
                  <th className="px-3 py-2">Noi dung loi</th>
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
