import { useState, useEffect, useCallback } from "react";
import { recruitmentApi } from "../api/recruitmentApi";
import type {
  CreateRecruitmentPayload,
  DepartmentOption,
  PositionOption,
} from "../types/recruitment";
import { AxiosError } from "axios";
import { useNotification } from "../../../core/context/NotificationContext";

// 1. Định nghĩa Interface cục bộ cho dữ liệu thô từ API trả về để tránh dùng unknown/any sai cách
interface RawDepartmentNode {
  id: number;
  deptName: string;
  children?: RawDepartmentNode[];
}

interface RawPosition {
  id: number;
  title: string;
}

export const useCreateRecruitment = () => {
  const [loading, setLoading] = useState(false);
  const [departments, setDepartments] = useState<DepartmentOption[]>([]);
  const [positions, setPositions] = useState<PositionOption[]>([]);
  const { triggerAlert } = useNotification();

  const fetchMasterData = useCallback(async () => {
    try {
      // Lấy dữ liệu qua API layer
      const [deptRes, posRes] = await Promise.all([
        recruitmentApi.getDepartmentsTree(),
        recruitmentApi.getPositions(),
      ]);

      // 2. Định nghĩa kiểu đầu vào là RawDepartmentNode thay vì unknown
      const flattenTree = (nodes: RawDepartmentNode[]): DepartmentOption[] => {
        return nodes.reduce(
          (acc: DepartmentOption[], curr: RawDepartmentNode) => {
            return [
              ...acc,
              { id: curr.id, deptName: curr.deptName },
              ...flattenTree(curr.children || []),
            ];
          },
          [],
        );
      };

      // Ép kiểu (Type Assertion) mảng dữ liệu lấy được về đúng cấu trúc Interface
      const rawDeptData = (deptRes.data ||
        deptRes.data ||
        []) as RawDepartmentNode[];
      setDepartments(flattenTree(rawDeptData));

      const rawPosData = (posRes.data || []) as RawPosition[];
      setPositions(
        rawPosData.map((p: RawPosition) => ({ id: p.id, title: p.title })),
      );
    } catch (error: unknown) {
      console.error("Lỗi tải danh mục:", error);
    }
  }, []);

  useEffect(() => {
    fetchMasterData();
  }, [fetchMasterData]);

  const handleCreateRequest = async (
    payload: CreateRecruitmentPayload,
  ): Promise<boolean> => {
    setLoading(true);
    try {
      await recruitmentApi.createRequest(payload);
      triggerAlert("success", "Thành công", "Đã gửi đề xuất tuyển dụng! Hệ thống sẽ tự động chuyển đến HR.");
      return true;
    } catch (error: unknown) {
      // 3. Sử dụng AxiosError chuẩn xác
      const axiosError = error as AxiosError<{
        message?: string;
        Message?: string;
      }>;
      const errMsg =
        axiosError.response?.data?.message ||
        axiosError.response?.data?.Message ||
        "Lỗi khi tạo đề xuất";

      triggerAlert("error", "Lỗi", errMsg);
      return false;
    } finally {
      setLoading(false);
    }
  };

  return { loading, departments, positions, handleCreateRequest };
};
