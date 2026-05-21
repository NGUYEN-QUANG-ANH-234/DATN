import { useState, useCallback, useEffect } from "react";
import { departmentApi } from "../api/departmentApi";
import type { DepartmentTree } from "../types/department";

export const useDepartments = () => {
  const [treeData, setTreeData] = useState<DepartmentTree[]>([]);
  const [loading, setLoading] = useState(false);

  const fetchTree = useCallback(async () => {
    setLoading(true);
    try {
      const res = await departmentApi.getTree();
      setTreeData(res.data || []);
    } catch (error) {
      console.error("Lỗi tải sơ đồ tổ chức:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  const handleUpdateParent = async (id: number, newParentId: number | null) => {
    try {
      await departmentApi.updateStructure(id, newParentId);
      await fetchTree();
      return true;
    } catch (error) {
      console.error("Lỗi cập nhật cấu trúc:", error);
      return false;
    }
  };

  const handleDeactivate = async (id: number): Promise<boolean> => {
    try {
      await departmentApi.deactivate(id);
      await fetchTree();
      return true;
    } catch (error) {
      console.error("Lỗi giải thể phòng ban:", error);
      return false;
    }
  };

  const handleCreate = async (data: {
    deptCode: string;
    deptName: string;
    parentDeptId: number | null;
  }) => {
    try {
      await departmentApi.create(data);
      await fetchTree();
      return true;
    } catch (error) {
      console.error("Lỗi tạo phòng ban:", error);
      return false;
    }
  };

  useEffect(() => {
    fetchTree();
  }, [fetchTree]);

  return {
    treeData,
    loading,
    handleUpdateParent,
    handleDeactivate,
    handleCreate,
  };
};
