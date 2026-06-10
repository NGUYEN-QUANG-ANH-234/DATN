import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useNotification } from "../../../core/context/NotificationContext";
import { departmentApi } from "../../organization/api/departmentApi";
import type { DepartmentTree } from "../../organization/types/department";
import {
  kpiApi,
  type KpiImportError,
  type KpiImportResult,
} from "../api/kpiApi";
import type { DepartmentOption } from "../types/kpi";

const current = new Date();
const defaultPeriod = `${String(current.getMonth() + 1).padStart(2, "0")}/${current.getFullYear()}`;

export const useKpiImport = () => {
  const { triggerAlert } = useNotification();
  const [file, setFile] = useState<File | null>(null);
  const [period, setPeriod] = useState(defaultPeriod);
  const [deptId, setDeptId] = useState("");
  const [departments, setDepartments] = useState<DepartmentOption[]>([]);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<KpiImportResult | null>(null);
  const [errors, setErrors] = useState<KpiImportError[]>([]);

  const fileName = useMemo(() => file?.name || "Chưa chọn file", [file]);

  useEffect(() => {
    let mounted = true;

    const loadDepartments = async () => {
      try {
        const response = await departmentApi.getTree();
        if (mounted) setDepartments(flattenDepartments(response.data || []));
      } catch (error) {
        console.error("Không thể tải danh sách phòng ban KPI:", error);
      }
    };

    void loadDepartments();
    return () => {
      mounted = false;
    };
  }, []);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();

    if (!file) {
      triggerAlert("warning", "Thiếu file KPI", "Vui lòng chọn file Excel hoặc CSV.");
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
        "Đã nhập KPI",
        response.message || "Dữ liệu KPI đầu kỳ đã được ghi nhận hoặc cập nhật.",
      );
    } catch (error) {
      const payload = (error as { response?: { data?: { errors?: KpiImportError[] } } })
        ?.response?.data;
      if (payload?.errors) setErrors(payload.errors);

      triggerAlert(
        "error",
        "Không thể nhập KPI",
        error instanceof Error ? error.message : "File KPI chưa hợp lệ.",
      );
    } finally {
      setLoading(false);
    }
  };

  return {
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
  };
};

const flattenDepartments = (
  nodes: DepartmentTree[],
  level = 0,
): DepartmentOption[] =>
  nodes.flatMap((node) => [
    { id: node.id, name: `${"-- ".repeat(level)}${node.deptName}` },
    ...flattenDepartments(node.children || [], level + 1),
  ]);

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
  link.download = "mau-nhap-kpi.csv";
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
};
