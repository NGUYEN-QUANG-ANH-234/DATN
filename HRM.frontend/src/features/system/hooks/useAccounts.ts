import { useState, useCallback, useEffect } from "react";
import { accountApi } from "../api/accountApi";
import type {
  Account,
  CreateAccountDto,
  AccountStatus,
} from "../types/account";
import { useNotification } from "../../../core/context/NotificationContext";

export const useAccounts = () => {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [roles, setRoles] = useState<{ id: number; name: string }[]>([]);
  const [loading, setLoading] = useState(false);
  const { triggerAlert } = useNotification();

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const [accRes, roleRes] = await Promise.all([
        accountApi.getAccounts(),
        accountApi.getSystemRoles(),
      ]);

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
      triggerAlert("error", "Lỗi", "Không thể tải tài khoản và vai trò.");
    } finally {
      setLoading(false);
    }
  }, [triggerAlert]);

  const getErrorMessage = (error: unknown, fallback: string) =>
    (error as { response?: { data?: { message?: string } } }).response?.data
      ?.message || (error as { message?: string }).message || fallback;

  const handleCreateAccount = async (data: CreateAccountDto) => {
    try {
      const response = await accountApi.createAccount(data);
      const temporaryPassword = response.data?.temporaryPassword;
      triggerAlert(
        "success",
        "Đã tạo tài khoản",
        temporaryPassword
          ? `Mật khẩu tạm thời: ${temporaryPassword}`
          : "Tài khoản đã được tạo với mật khẩu bạn đã nhập.",
      );
      fetchData();
      return true;
    } catch (error: unknown) {
      triggerAlert("error", "Lỗi", getErrorMessage(error, "Lỗi khi tạo tài khoản."));
      return false;
    }
  };

  const handleToggleStatus = async (
    id: number,
    currentStatus: AccountStatus,
  ) => {
    const newStatus: AccountStatus =
      currentStatus === "Active" ? "Locked" : "Active";
    triggerAlert(
      "confirm",
      "Xác nhận đổi trạng thái",
      `Bạn có chắc muốn chuyển trạng thái thành ${newStatus}?`,
      async () => {
        try {
          await accountApi.toggleStatus(id, newStatus);
          triggerAlert(
            "success",
            "Đã cập nhật",
            "Cập nhật trạng thái thành công.",
          );
          fetchData();
        } catch (error: unknown) {
          triggerAlert(
            "error",
            "Lỗi",
            getErrorMessage(error, "Lỗi khi đổi trạng thái."),
          );
        }
      },
    );
  };

  const handleResetPassword = async (id: number) => {
    triggerAlert(
      "confirm",
      "Cấp lại mật khẩu",
      "Cấp lại mật khẩu mới cho nhân viên này?",
      async () => {
        try {
          const response = await accountApi.resetPassword(id);
          const temporaryPassword = response.data?.temporaryPassword;
          triggerAlert(
            "success",
            "Đã cấp lại mật khẩu",
            temporaryPassword
              ? `Mật khẩu mới: ${temporaryPassword}`
              : "Mật khẩu mới đã được tạo và gửi vào email nhân viên.",
          );
        } catch (error: unknown) {
          triggerAlert(
            "error",
            "Lỗi",
            getErrorMessage(error, "Lỗi khi cấp lại mật khẩu."),
          );
        }
      },
    );
  };

  const handleUpdateRole = async (id: number, roleId: number) => {
    try {
      await accountApi.updateRole(id, roleId);
      triggerAlert("success", "Đã cập nhật", "Cập nhật quyền thành công.");
      fetchData();
    } catch (error: unknown) {
      triggerAlert("error", "Lỗi", getErrorMessage(error, "Lỗi cập nhật quyền."));
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
