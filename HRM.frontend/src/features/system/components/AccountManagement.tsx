import React, { useMemo, useState } from "react";
import { useAccounts } from "../hooks/useAccounts";
import type { CreateAccountDto } from "../types/account";

export const AccountManagement: React.FC = () => {
  const {
    accounts,
    roles,
    loading,
    handleCreateAccount,
    handleToggleStatus,
    handleResetPassword,
    handleUpdateRole,
  } = useAccounts();

  const candidateRoleId = useMemo(() => {
    const candidateRole = roles.find((role) => {
      const roleName = role.name.toLowerCase();
      return (
        roleName.includes("candidate") ||
        roleName.includes("ung vien") ||
        roleName.includes("ứng viên")
      );
    });

    return candidateRole?.id ?? 8;
  }, [roles]);

  const [formData, setFormData] = useState<CreateAccountDto>({
    email: "",
    fullName: "",
    roleId: candidateRoleId,
    password: "",
  });

  const selectedRoleId = roles.some((role) => role.id === formData.roleId)
    ? formData.roleId
    : candidateRoleId;

  const onSubmitCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    const success = await handleCreateAccount({
      ...formData,
      roleId: selectedRoleId,
      password: formData.password?.trim() || undefined,
    });

    if (success) {
      setFormData({
        email: "",
        fullName: "",
        roleId: candidateRoleId,
        password: "",
      });
    }
  };

  return (
    <div className="rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
      <h2 className="mb-4 text-xl font-bold text-gray-900">
        Quan tri tai khoan he thong
      </h2>

      <form
        onSubmit={onSubmitCreate}
        className="mb-8 flex flex-wrap items-end gap-4 rounded border bg-gray-50 p-4"
      >
        <div>
          <label className="mb-1 block text-sm font-medium">Email noi bo</label>
          <input
            type="email"
            required
            value={formData.email}
            onChange={(e) =>
              setFormData({ ...formData, email: e.target.value })
            }
            placeholder="nguyenvana@hicas.vn"
            className="w-64 rounded border bg-white p-2"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Ho va ten</label>
          <input
            type="text"
            required
            value={formData.fullName}
            onChange={(e) =>
              setFormData({ ...formData, fullName: e.target.value })
            }
            placeholder="Nguyen Van A"
            className="w-56 rounded border bg-white p-2"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">
            Mat khau khoi tao
          </label>
          <input
            type="password"
            minLength={8}
            value={formData.password || ""}
            onChange={(e) =>
              setFormData({ ...formData, password: e.target.value })
            }
            placeholder="De trong de sinh tu dong"
            className="w-56 rounded border bg-white p-2"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">
            Quyen han mac dinh
          </label>
          <select
            required
            value={selectedRoleId}
            onChange={(e) =>
              setFormData({ ...formData, roleId: Number(e.target.value) })
            }
            className="w-48 rounded border bg-white p-2"
          >
            {roles.map((role) => (
              <option key={role.id} value={role.id}>
                {role.name}
              </option>
            ))}
          </select>
          <p className="mt-1 text-xs text-gray-500">
            Mac dinh la Candidate neu khong chon role.
          </p>
        </div>

        <button
          type="submit"
          className="rounded bg-blue-600 px-5 py-2 font-medium text-white hover:bg-blue-700"
        >
          Tao tai khoan
        </button>
      </form>

      <div className="overflow-x-auto">
        {loading ? (
          <p className="p-4 text-center">Dang dong bo du lieu...</p>
        ) : (
          <table className="w-full border-collapse text-left">
            <thead>
              <tr className="border-b bg-gray-100">
                <th className="p-3 text-sm font-semibold">ID</th>
                <th className="p-3 text-sm font-semibold">Nhan su</th>
                <th className="p-3 text-sm font-semibold">Quyen</th>
                <th className="p-3 text-sm font-semibold">Trang thai</th>
                <th className="p-3 text-sm font-semibold">MFA</th>
                <th className="p-3 text-center text-sm font-semibold">
                  Hanh dong
                </th>
              </tr>
            </thead>
            <tbody>
              {accounts.map((acc) => (
                <tr key={acc.id} className="border-b hover:bg-gray-50">
                  <td className="p-3 text-sm text-gray-500">#{acc.id}</td>
                  <td className="p-3 text-sm">
                    <p className="font-bold text-gray-800">{acc.fullName}</p>
                    <p className="text-xs text-gray-500">{acc.email}</p>
                  </td>
                  <td className="p-3 text-sm">
                    <select
                      value={acc.roleId}
                      onChange={(e) =>
                        handleUpdateRole(acc.id, Number(e.target.value))
                      }
                      className={`rounded border bg-white p-1 text-sm transition-shadow focus:outline-none focus:ring-2 focus:ring-blue-500 ${
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
                      <span className="mt-1 block text-[10px] italic text-red-500">
                        * Quan tri he thong
                      </span>
                    )}
                  </td>
                  <td className="p-3">
                    <span
                      className={`rounded px-2 py-1 text-xs font-bold ${
                        acc.status === "Active"
                          ? "bg-green-100 text-green-800"
                          : "bg-red-100 text-red-800"
                      }`}
                    >
                      {acc.status}
                    </span>
                  </td>
                  <td className="p-3 text-sm">
                    {acc.isMfaEnabled ? (
                      <span className="font-semibold text-blue-600">Da bat</span>
                    ) : (
                      <span className="text-gray-400">Chua bat</span>
                    )}
                  </td>
                  <td className="space-x-2 p-3 text-center">
                    <button
                      onClick={() => handleToggleStatus(acc.id, acc.status)}
                      className={`rounded px-3 py-1 text-xs font-semibold text-white ${
                        acc.status === "Active"
                          ? "bg-orange-500 hover:bg-orange-600"
                          : "bg-green-500 hover:bg-green-600"
                      }`}
                    >
                      {acc.status === "Active" ? "Khoa" : "Mo khoa"}
                    </button>
                    <button
                      onClick={() => handleResetPassword(acc.id)}
                      className="rounded bg-gray-600 px-3 py-1 text-xs font-semibold text-white hover:bg-gray-700"
                    >
                      Cap lai mat khau
                    </button>
                  </td>
                </tr>
              ))}
              {accounts.length === 0 && (
                <tr>
                  <td colSpan={6} className="p-4 text-center text-gray-500">
                    Chua co tai khoan nao.
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
