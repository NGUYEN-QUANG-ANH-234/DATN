import { useState, useEffect, useCallback } from "react";
import { salaryVariableApi } from "../api/salaryVariableApi";
import type {
  SalaryVariable,
  SourceCatalogItem,
} from "../types/salaryVariable"; // Nhớ import thêm type này

export const useSalaryVariable = () => {
  const [variables, setVariables] = useState<SalaryVariable[]>([]);
  const [catalogs, setCatalogs] = useState<SourceCatalogItem[]>([]); // Thêm state catalogs
  const [loading, setLoading] = useState<boolean>(false);

  const fetchVariables = useCallback(async () => {
    setLoading(true);
    try {
      const res = (await salaryVariableApi.getAll()) as {
        success: boolean;
        data: SalaryVariable[];
      };

      if (Array.isArray(res)) {
        setVariables(res);
      } else if (res && Array.isArray(res.data)) {
        setVariables(res.data);
      } else {
        console.warn("Dữ liệu không đúng định dạng mảng:", res);
        setVariables([]);
      }
    } catch (error) {
      console.error("Lỗi khi tải danh sách biến lương:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  // THÊM MỚI: Hàm fetch Source Catalogs
  const fetchCatalogs = useCallback(async () => {
    try {
      // Nhớ thêm hàm getCatalogs vào file salaryVariableApi.ts của bạn
      const res = await salaryVariableApi.getCatalogs();

      if (Array.isArray(res)) {
        setCatalogs(res);
      } else if (
        res &&
        typeof res === "object" &&
        "data" in res &&
        Array.isArray((res as { data: SourceCatalogItem[] }).data)
      ) {
        setCatalogs((res as { data: SourceCatalogItem[] }).data);
      }
    } catch (error) {
      console.error("Lỗi khi tải danh mục nguồn:", error);
    }
  }, []);

  const defineVariable = async (payload: SalaryVariable) => {
    try {
      const res = await salaryVariableApi.define(payload);
      if (res.success) {
        await fetchVariables(); // Cập nhật lại danh sách sau khi thêm thành công
      }
      return res;
    } catch (error: unknown) {
      throw (
        (error as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Lỗi hệ thống"
      );
    }
  };

  useEffect(() => {
    fetchVariables();
    fetchCatalogs(); // Gọi thêm fetchCatalogs khi Component mount
  }, [fetchVariables, fetchCatalogs]);

  // Trả về thêm catalogs để UI sử dụng
  return { variables, catalogs, loading, defineVariable };
};
