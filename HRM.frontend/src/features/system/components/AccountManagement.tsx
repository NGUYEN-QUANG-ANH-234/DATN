import React, { useState, useEffect } from "react";
import { useAccounts } from "../hooks/useAccounts";
import type { CreateAccountDto } from "../types/account";

export const AccountManagement: React.FC = () => {
  const {
    accounts,
    roles, // Lấy mảng Role động từ Backend
    loading,
    handleCreateAccount,
    handleToggleStatus,
    handleResetPassword,
    handleUpdateRole,
  } = useAccounts();

  const [formData, setFormData] = useState<CreateAccountDto>({
    email: "",
    fullName: "",
    roleId: 1,
  });

  // Tự động gán roleId mặc định bằng phần tử đầu tiên nếu roles đã được load
  useEffect(() => {
    if (roles.length > 0 && formData.roleId === 1) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setFormData((prev) => ({ ...prev, roleId: roles[0].id }));
    }
  }, [formData.roleId, roles]);

  const onSubmitCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    const success = await handleCreateAccount(formData);
    if (success) {
      setFormData({
        email: "",
        fullName: "",
        roleId: roles.length > 0 ? roles[0].id : 1,
      });
    }
  };

  return (
    <div className="rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
      <h2 className="text-xl font-bold mb-4">Quản trị Tài khoản Hệ thống</h2>

      {/* Form Tạo Tài Khoản */}
      <form
        onSubmit={onSubmitCreate}
        className="mb-8 p-4 bg-gray-50 rounded border flex flex-wrap gap-4 items-end"
      >
        <div>
          <label className="block text-sm font-medium mb-1">Email nội bộ</label>
          <input
            type="email"
            required
            value={formData.email}
            onChange={(e) =>
              setFormData({ ...formData, email: e.target.value })
            }
            placeholder="nguyenvana@hicas.vn"
            className="border p-2 rounded w-64 bg-white"
          />
        </div>
        <div>
          <label className="block text-sm font-medium mb-1">Họ và tên</label>
          <input
            type="text"
            required
            value={formData.fullName}
            onChange={(e) =>
              setFormData({ ...formData, fullName: e.target.value })
            }
            placeholder="Nguyễn Văn A"
            className="border p-2 rounded w-56 bg-white"
          />
        </div>

        {/* ĐÃ SỬA: Đổi từ input nhập số sang Thẻ Select Box động */}
        <div>
          <label className="block text-sm font-medium mb-1">
            Quyền hạn (Role)
          </label>
          <select
            required
            value={formData.roleId}
            onChange={(e) =>
              setFormData({ ...formData, roleId: Number(e.target.value) })
            }
            className="border p-2 rounded w-48 bg-white"
          >
            {roles.map((role) => (
              <option key={role.id} value={role.id}>
                {role.name}
              </option>
            ))}
          </select>
        </div>

        <button
          type="submit"
          className="bg-blue-600 text-white px-5 py-2 rounded hover:bg-blue-700 font-medium"
        >
          Tạo tài khoản & Gửi Mail
        </button>
      </form>

      {/* Bảng Danh sách Tài khoản */}
      <div className="overflow-x-auto">
        {loading ? (
          <p className="text-center p-4">Đang đồng bộ dữ liệu...</p>
        ) : (
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-gray-100 border-b">
                <th className="p-3 text-sm font-semibold">ID</th>
                <th className="p-3 text-sm font-semibold">Nhân sự</th>
                <th className="p-3 text-sm font-semibold">Quyền</th>
                <th className="p-3 text-sm font-semibold">Trạng thái</th>
                <th className="p-3 text-sm font-semibold">Bảo mật (MFA)</th>
                <th className="p-3 text-sm font-semibold text-center">
                  Hành động
                </th>
              </tr>
            </thead>
            <tbody>
              {accounts.map((acc) => (
                <tr key={acc.id} className="border-b hover:bg-gray-50">
                  <td className="p-3 text-sm text-gray-500">#{acc.id}</td>
                  <td className="p-3 text-sm">
                    <p className="font-bold text-gray-800">{acc.fullName}</p>
                    <p className="text-gray-500 text-xs">{acc.email}</p>
                  </td>

                  {/* ĐÃ SỬA: Cột cập nhật Quyền sử dụng Role List động và cảnh báo Admin */}
                  <td className="p-3 text-sm">
                    <select
                      value={acc.roleId}
                      onChange={(e) =>
                        handleUpdateRole(acc.id, Number(e.target.value))
                      }
                      className={`border rounded p-1 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-blue-500 transition-shadow ${
                        acc.roleId === 1
                          ? "border-red-300 font-bold text-red-600"
                          : "border-gray-300"
                      }`}
                    >
                      {roles.map((role) => (
                        <option key={role.id} value={role.id}>
                          {role.name}
                        </option>
                      ))}
                    </select>
                    {acc.roleId === 1 && (
                      <span className="block text-[10px] text-red-500 italic mt-1">
                        * Quản trị hệ thống
                      </span>
                    )}
                  </td>

                  <td className="p-3">
                    <span
                      className={`px-2 py-1 text-xs font-bold rounded ${acc.status === "Active" ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800"}`}
                    >
                      {acc.status}
                    </span>
                  </td>
                  <td className="p-3 text-sm">
                    {acc.isMfaEnabled ? (
                      <span className="text-blue-600 font-semibold">
                        Đã bật
                      </span>
                    ) : (
                      <span className="text-gray-400">Chưa bật</span>
                    )}
                  </td>
                  <td className="p-3 text-center space-x-2">
                    <button
                      onClick={() => handleToggleStatus(acc.id, acc.status)}
                      className={`px-3 py-1 text-xs font-semibold rounded text-white ${acc.status === "Active" ? "bg-orange-500 hover:bg-orange-600" : "bg-green-500 hover:bg-green-600"}`}
                    >
                      {acc.status === "Active" ? "Khóa User" : "Mở khóa"}
                    </button>
                    <button
                      onClick={() => handleResetPassword(acc.id)}
                      className="px-3 py-1 text-xs font-semibold rounded bg-gray-600 text-white hover:bg-gray-700"
                    >
                      Cấp lại mật khẩu
                    </button>
                  </td>
                </tr>
              ))}
              {accounts.length === 0 && (
                <tr>
                  <td colSpan={6} className="text-center p-4 text-gray-500">
                    Chưa có tài khoản nào.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};
