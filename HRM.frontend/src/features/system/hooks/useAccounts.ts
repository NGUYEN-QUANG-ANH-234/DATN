import { useState, useCallback, useEffect } from "react";
import { accountApi } from "../api/accountApi";
import type {
  Account,
  CreateAccountDto,
  AccountStatus,
} from "../types/account";

export const useAccounts = () => {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [roles, setRoles] = useState<{ id: number; name: string }[]>([]);
  const [loading, setLoading] = useState(false);

  // Gộp chung hàm lấy Data để luôn đồng bộ Account và Role
  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const [accRes, roleRes] = await Promise.all([
        accountApi.getAccounts(),
        accountApi.getSystemRoles(),
      ]);

      // Xử lý an toàn cho cả Object bọc data hoặc Array thuần
      const getResponseData = <T>(res: unknown): T[] => {
        if (Array.isArray(res)) return res;
        if (res && typeof res === "object" && "data" in res) {
          const data = (res as { data?: unknown }).data;
          return Array.isArray(data) ? (data as T[]) : [];
        }
        return [];
      };

      setAccounts(getResponseData<Account>(accRes));
      setRoles(getResponseData<{ id: number; name: string }>(roleRes));
    } catch (error) {
      console.error("Lỗi tải dữ liệu:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  const handleCreateAccount = async (data: CreateAccountDto) => {
    try {
      await accountApi.createAccount(data);
      alert("Khởi tạo tài khoản thành công! Mật khẩu đã được gửi qua email.");
      fetchData(); // Render lại bảng
      return true;
    } catch (error: unknown) {
      alert(
        (error as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Lỗi khi tạo tài khoản",
      );
      return false;
    }
  };

  const handleToggleStatus = async (
    id: number,
    currentStatus: AccountStatus,
  ) => {
    const newStatus = currentStatus === "Active" ? "Inactive" : "Active";
    if (
      !window.confirm(`Bạn có chắc muốn chuyển trạng thái thành ${newStatus}?`)
    )
      return;

    try {
      await accountApi.toggleStatus(id, newStatus);
      alert("Cập nhật trạng thái thành công!");
      fetchData();
    } catch (error: unknown) {
      alert(
        (error as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Lỗi khi đổi trạng thái",
      );
    }
  };

  const handleResetPassword = async (id: number) => {
    if (!window.confirm("Cấp lại mật khẩu mới cho nhân viên này?")) return;

    try {
      await accountApi.resetPassword(id);
      alert("Mật khẩu mới đã được tạo và gửi vào Email nhân viên.");
    } catch (error: unknown) {
      alert(
        (error as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Lỗi khi cấp lại mật khẩu",
      );
    }
  };

  const handleUpdateRole = async (id: number, roleId: number) => {
    try {
      await accountApi.updateRole(id, roleId);
      alert("Cập nhật quyền thành công!");
      fetchData();
    } catch (error: unknown) {
      alert(
        (error as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Lỗi cập nhật",
      );
    }
  };

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return {
    accounts,
    roles,
    loading,
    handleCreateAccount,
    handleToggleStatus,
    handleResetPassword,
    handleUpdateRole,
  };
};
