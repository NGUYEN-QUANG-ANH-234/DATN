import React, { useEffect, useState } from "react";
import { Button, Card, EmptyState } from "../../../components/ui";
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
  const { roles, availableModules, loading, updatePermissions } = useRbac();
  const availableModulesTyped = availableModules as PermissionModule[] | undefined;

  const [selectedRoleId, setSelectedRoleId] = useState<string | number | null>(null);
  const [currentPermissions, setCurrentPermissions] = useState<string[]>([]);
  const [message, setMessage] = useState<string>("");

  useEffect(() => {
    if (selectedRoleId !== null) {
      const role = roles.find((item) => item.roleId === selectedRoleId);
      setCurrentPermissions(role ? [...role.permissions] : []);
      setMessage("");
    }
  }, [selectedRoleId, roles]);

  const handleCheckboxChange = (code: string) => {
    setCurrentPermissions((prev) =>
      prev.includes(code) ? prev.filter((permission) => permission !== code) : [...prev, code],
    );
  };

  const handleSelectAll = (moduleCodes: PermissionItem[]) => {
    const codesOnly = moduleCodes.map((item) => item.code);
    const isAllSelected = codesOnly.every((code) => currentPermissions.includes(code));

    if (isAllSelected) {
      setCurrentPermissions((prev) =>
        prev.filter((permission) => !codesOnly.includes(permission)),
      );
      return;
    }

    setCurrentPermissions((prev) => Array.from(new Set([...prev, ...codesOnly])));
  };

  const handleSubmit = async () => {
    if (selectedRoleId === null) return;

    try {
      const res = await updatePermissions({
        roleId: Number(selectedRoleId),
        permissionCodes: currentPermissions,
      });

      const responseMessage =
        typeof res === "object" && res !== null && "message" in res
          ? String((res as { message?: unknown }).message || "")
          : "";

      setMessage(responseMessage || "Cập nhật quyền thành công!");
    } catch (error: unknown) {
      setMessage(`Lỗi: ${error}`);
    }
  };

  const selectedRole = roles.find((role) => role.roleId === selectedRoleId);

  return (
    <Card
      title="Phân quyền hệ thống (RBAC)"
      description="Quản lý ma trận quyền truy cập theo vai trò và chức năng trong từng phân hệ."
    >
      {loading && roles.length === 0 ? (
        <p className="text-sm text-[var(--hicas-text-secondary)]">Đang tải dữ liệu...</p>
      ) : (
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[260px_1fr]">
          <aside className="border-b border-[var(--hicas-border-soft)] pb-4 lg:border-b-0 lg:border-r lg:pr-4">
            <h3 className="mb-3 text-sm font-semibold text-[var(--hicas-text-main)]">
              Chọn vai trò
            </h3>
            <div className="space-y-2">
              {roles.map((role: RolePermission) => (
                <button
                  key={role.roleId}
                  type="button"
                  onClick={() => setSelectedRoleId(role.roleId)}
                  className={`min-h-10 w-full rounded-[var(--radius-md)] border px-3 text-left text-sm transition ${
                    selectedRoleId === role.roleId
                      ? "border-[var(--hicas-orange)] bg-[var(--hicas-orange-soft)] font-semibold text-[var(--hicas-orange-dark)]"
                      : "border-[var(--hicas-border)] text-[var(--hicas-text-main)] hover:border-[var(--hicas-orange)] hover:bg-[var(--hicas-orange-lighter)]"
                  }`}
                >
                  {role.roleName}
                  {role.roleId === 1 && (
                    <span className="ml-2 text-xs text-[var(--hicas-danger)]">(Root)</span>
                  )}
                </button>
              ))}
            </div>
          </aside>

          <section>
            {!selectedRole ? (
              <EmptyState
                title="Chưa chọn vai trò"
                description="Chọn một vai trò ở danh sách bên trái để cấu hình quyền."
              />
            ) : (
              <div className="space-y-5">
                <div className="flex flex-col gap-3 border-b border-[var(--hicas-border-soft)] pb-4 sm:flex-row sm:items-center sm:justify-between">
                  <h3 className="text-lg font-semibold text-[var(--hicas-text-main)]">
                    Quyền hạn của: {selectedRole.roleName}
                  </h3>
                  <Button
                    onClick={handleSubmit}
                    disabled={selectedRole.roleId === 1}
                    variant={selectedRole.roleId === 1 ? "secondary" : "primary"}
                  >
                    Lưu thay đổi
                  </Button>
                </div>

                {selectedRole.roleId === 1 && (
                  <div className="rounded-[var(--radius-md)] border border-[var(--hicas-danger)] bg-[var(--hicas-danger-soft)] px-4 py-3 text-sm text-[var(--hicas-danger)]">
                    Bảo mật: hệ thống không cho phép chỉnh sửa quyền của tài khoản Root (Super Admin).
                  </div>
                )}

                {message && (
                  <p
                    className={`text-sm font-medium ${
                      message.startsWith("Lỗi")
                        ? "text-[var(--hicas-danger)]"
                        : "text-[var(--hicas-success)]"
                    }`}
                  >
                    {message}
                  </p>
                )}

                <div className="grid grid-cols-1 gap-4 xl:grid-cols-2">
                  {availableModulesTyped?.map((module: PermissionModule) => (
                    <div
                      key={module.group}
                      className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-[var(--hicas-bg)] p-4"
                    >
                      <div className="mb-3 flex items-center justify-between gap-3">
                        <h4 className="font-semibold text-[var(--hicas-text-main)]">
                          {module.group}
                        </h4>
                        <Button
                          type="button"
                          size="sm"
                          variant="ghost"
                          onClick={() => handleSelectAll(module.codes)}
                          disabled={selectedRole.roleId === 1}
                        >
                          Chọn/Bỏ chọn tất cả
                        </Button>
                      </div>

                      <div className="space-y-3">
                        {module.codes.map((item: PermissionItem) => (
                          <label key={item.code} className="flex cursor-pointer flex-col">
                            <div className="flex items-center gap-2">
                              <input
                                type="checkbox"
                                checked={currentPermissions.includes(item.code)}
                                onChange={() => handleCheckboxChange(item.code)}
                                disabled={selectedRole.roleId === 1}
                                className="h-4 w-4 rounded border-[var(--hicas-border)] accent-[var(--hicas-orange)]"
                              />
                              <span className="text-sm font-semibold text-[var(--hicas-text-main)]">
                                {item.code}
                              </span>
                            </div>
                            <span className="ml-6 text-xs leading-5 text-[var(--hicas-text-secondary)]">
                              {item.desc}
                            </span>
                          </label>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </section>
        </div>
      )}
    </Card>
  );
};
