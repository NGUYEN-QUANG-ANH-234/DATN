import React, { useState, useEffect } from "react";
import { useRbac } from "../hooks/useRbac";

type RolePermission = {
  roleId: string | number;
  roleName: string;
  permissions: string[];
};

type PermissionItem = {
  code: string;
  desc: string;
};

type PermissionModule = {
  group: string;
  codes: PermissionItem[];
};

export const RbacManager: React.FC = () => {
  // 1. Bổ sung availableModules từ hook
  const { roles, availableModules, loading, updatePermissions } = useRbac();
  const availableModulesTyped = availableModules as
    | PermissionModule[]
    | undefined;

  const [selectedRoleId, setSelectedRoleId] = useState<string | number | null>(
    null,
  );
  const [currentPermissions, setCurrentPermissions] = useState<string[]>([]);
  const [message, setMessage] = useState<string>("");

  useEffect(() => {
    if (selectedRoleId !== null) {
      const role = roles.find((r) => r.roleId === selectedRoleId);
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setCurrentPermissions(role ? [...role.permissions] : []);
      setMessage("");
    }
  }, [selectedRoleId, roles]);

  const handleCheckboxChange = (code: string) => {
    setCurrentPermissions((prev) =>
      prev.includes(code) ? prev.filter((p) => p !== code) : [...prev, code],
    );
  };

  // 2. Cập nhật hàm xử lý đầu vào là mảng Object thay vì mảng String
  const handleSelectAll = (moduleCodes: PermissionItem[]) => {
    const codesOnly = moduleCodes.map((c) => c.code); // Tách lấy mảng chuỗi code
    const isAllSelected = codesOnly.every((code) =>
      currentPermissions.includes(code),
    );

    if (isAllSelected) {
      setCurrentPermissions((prev) =>
        prev.filter((p) => !codesOnly.includes(p)),
      );
    } else {
      setCurrentPermissions((prev) =>
        Array.from(new Set([...prev, ...codesOnly])),
      );
    }
  };

  const handleSubmit = async () => {
    if (selectedRoleId === null) return;

    try {
      const res = await updatePermissions({
        roleId: Number(selectedRoleId),
        permissionCodes: currentPermissions,
      });

      const message =
        typeof res === "object" && res !== null && "message" in res
          ? String((res as { message?: unknown }).message || "")
          : "";

      setMessage(message || "Cập nhật quyền thành công!");
    } catch (error: unknown) {
      setMessage(`Lỗi: ${error}`);
    }
  };

  const selectedRole = roles.find((r) => r.roleId === selectedRoleId);

  return (
    <div className="p-4 bg-white rounded shadow">
      <h2 className="text-xl font-bold mb-4">Phân quyền Hệ thống (RBAC)</h2>

      {loading && roles.length === 0 ? (
        <p>Đang tải dữ liệu...</p>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
          {/* Cột trái: Danh sách Vai trò */}
          <div className="col-span-1 border-r pr-4">
            <h3 className="font-semibold mb-3">Chọn Vai trò</h3>
            <div className="space-y-2">
              {roles.map((role: RolePermission) => (
                <button
                  key={role.roleId}
                  onClick={() => setSelectedRoleId(role.roleId)}
                  className={`w-full text-left p-2 rounded border ${
                    selectedRoleId === role.roleId
                      ? "bg-purple-50 border-purple-400 text-purple-700 font-medium"
                      : "hover:bg-gray-50"
                  }`}
                >
                  {role.roleName}
                  {role.roleId === 1 && (
                    <span className="ml-2 text-xs text-red-500">(Root)</span>
                  )}
                </button>
              ))}
            </div>
          </div>

          {/* Cột phải: Ma trận Quyền Động */}
          <div className="col-span-3">
            {!selectedRole ? (
              <p className="text-gray-500 italic mt-4">
                Vui lòng chọn một vai trò bên trái để cấu hình.
              </p>
            ) : (
              <div>
                <div className="flex justify-between items-center mb-4 border-b pb-2">
                  <h3 className="font-semibold text-lg text-purple-800">
                    Quyền hạn của: {selectedRole.roleName}
                  </h3>
                  <button
                    onClick={handleSubmit}
                    disabled={selectedRole.roleId === 1} // Khóa SuperAdmin
                    className={`px-4 py-2 rounded text-white ${
                      selectedRole.roleId === 1
                        ? "bg-gray-400 cursor-not-allowed"
                        : "bg-purple-600 hover:bg-purple-700"
                    }`}
                  >
                    Lưu Thay Đổi
                  </button>
                </div>

                {selectedRole.roleId === 1 && (
                  <p className="text-sm text-red-600 mb-4 bg-red-50 p-2 rounded">
                    Bảo mật: Hệ thống không cho phép chỉnh sửa quyền của tài
                    khoản Root (Super Admin).
                  </p>
                )}

                {message && (
                  <p
                    className={`mb-4 text-sm font-medium ${
                      message.startsWith("Lỗi")
                        ? "text-red-600"
                        : "text-green-600"
                    }`}
                  >
                    {message}
                  </p>
                )}

                {/* 3. Render danh sách từ availableModules */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {availableModulesTyped?.map(
                    (module: PermissionModule, idx: number) => (
                      <div key={idx} className="border p-4 rounded bg-gray-50">
                        <div className="flex justify-between items-center mb-3">
                          <h4 className="font-medium text-gray-800">
                            {module.group}
                          </h4>
                          <button
                            type="button"
                            onClick={() => handleSelectAll(module.codes)}
                            disabled={selectedRole.roleId === 1}
                            className="text-xs text-blue-600 hover:underline"
                          >
                            Chọn/Bỏ chọn tất cả
                          </button>
                        </div>

                        <div className="space-y-2">
                          {module.codes.map((item: PermissionItem) => (
                            <label
                              key={item.code}
                              className="flex flex-col cursor-pointer"
                            >
                              <div className="flex items-center space-x-2">
                                <input
                                  type="checkbox"
                                  checked={currentPermissions.includes(
                                    item.code,
                                  )}
                                  onChange={() =>
                                    handleCheckboxChange(item.code)
                                  }
                                  disabled={selectedRole.roleId === 1}
                                  className="w-4 h-4 text-purple-600 rounded focus:ring-purple-500"
                                />
                                <span className="text-sm text-gray-700 font-bold">
                                  {item.code}
                                </span>
                              </div>
                              <span className="text-xs text-gray-500 ml-6">
                                {item.desc}
                              </span>
                            </label>
                          ))}
                        </div>
                      </div>
                    ),
                  )}
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
