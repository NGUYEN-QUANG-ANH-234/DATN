import { useCallback, useEffect, useState } from "react";
import { salaryVariableApi } from "../api/salaryVariableApi";
import type {
  SalaryVariable,
  SourceCatalogItem,
} from "../types/salaryVariable";

const extractErrorMessage = (error: unknown) =>
  (error as { response?: { data?: { message?: string } } }).response?.data
    ?.message || "Lỗi hệ thống";

export const useSalaryVariable = () => {
  const [variables, setVariables] = useState<SalaryVariable[]>([]);
  const [catalogs, setCatalogs] = useState<SourceCatalogItem[]>([]);
  const [loading, setLoading] = useState<boolean>(false);

  const fetchVariables = useCallback(async () => {
    setLoading(true);
    try {
      const res = await salaryVariableApi.getAll();

      if (Array.isArray(res)) {
        setVariables(res.map((item) => ({ ...item, isActive: item.isActive ?? true })));
      } else if (res && Array.isArray(res.data)) {
        setVariables(res.data.map((item) => ({ ...item, isActive: item.isActive ?? true })));
      } else {
        setVariables([]);
      }
    } catch (error) {
      console.error("Error loading salary variables:", error);
      setVariables([]);
    } finally {
      setLoading(false);
    }
  }, []);

  const fetchCatalogs = useCallback(async () => {
    try {
      const res = await salaryVariableApi.getCatalogs();

      if (Array.isArray(res)) {
        setCatalogs(res);
      } else if (res && Array.isArray(res.data)) {
        setCatalogs(res.data);
      } else {
        setCatalogs([]);
      }
    } catch (error) {
      console.error("Error loading source catalogs:", error);
      setCatalogs([]);
    }
  }, []);

  const defineVariable = async (payload: SalaryVariable) => {
    try {
      const res = await salaryVariableApi.define(payload);
      if (res.success) {
        await fetchVariables();
      }
      return res;
    } catch (error: unknown) {
      throw extractErrorMessage(error);
    }
  };

  const setVariableActive = async (code: string, isActive: boolean) => {
    try {
      const res = await salaryVariableApi.setActive(code, isActive);
      if (res.success) {
        await fetchVariables();
      }
      return res;
    } catch (error: unknown) {
      throw extractErrorMessage(error);
    }
  };

  const setCatalogActive = async (id: number, isActive: boolean) => {
    try {
      const res = await salaryVariableApi.setCatalogActive(id, isActive);
      if (res.success) {
        await fetchCatalogs();
      }
      return res;
    } catch (error: unknown) {
      throw extractErrorMessage(error);
    }
  };

  useEffect(() => {
    fetchVariables();
    fetchCatalogs();
  }, [fetchVariables, fetchCatalogs]);

  return {
    variables,
    catalogs,
    loading,
    defineVariable,
    setVariableActive,
    setCatalogActive,
  };
};
