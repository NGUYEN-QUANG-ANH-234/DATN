import React, { useEffect, useMemo, useState } from "react";
import { CheckSquare, LockKeyhole, Save, ShieldCheck, Users } from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card, EmptyState } from "../../../components/ui";
import { cn } from "../../../components/ui/classNames";
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
  const availableModulesTyped = (availableModules as PermissionModule[] | undefined) ?? [];

  const [selectedRoleId, setSelectedRoleId] = useState<string | number | null>(null);
  const [currentPermissions, setCurrentPermissions] = useState<string[]>([]);
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(
    null,
  );

  useEffect(() => {
    if (selectedRoleId === null && roles.length > 0) {
      setSelectedRoleId(roles[0].roleId);
    }
  }, [roles, selectedRoleId]);

  useEffect(() => {
    if (selectedRoleId !== null) {
      const role = roles.find((item) => item.roleId === selectedRoleId);
      setCurrentPermissions(role ? [...role.permissions] : []);
      setMessage(null);
    }
  }, [selectedRoleId, roles]);

  const totalPermissions = useMemo(
    () => availableModulesTyped.reduce((sum, module) => sum + module.codes.length, 0),
    [availableModulesTyped],
  );

  const selectedRole = roles.find((role: RolePermission) => role.roleId === selectedRoleId);
  const isRootRole = Number(selectedRole?.roleId) === 1;

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
    if (selectedRoleId === null || isRootRole) return;

    try {
      const res = await updatePermissions({
        roleId: Number(selectedRoleId),
        permissionCodes: currentPermissions,
      });

      const responseMessage =
        typeof res === "object" && res !== null && "message" in res
          ? String((res as { message?: unknown }).message || "")
          : "";

      setMessage({
        type: "success",
        text: responseMessage || "Đã cập nhật quyền cho vai trò.",
      });
    } catch (error: unknown) {
      setMessage({
        type: "error",
        text: error instanceof Error ? error.message : "Không thể cập nhật quyền.",
      });
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Phân quyền hệ thống"
        description="Quản lý quyền truy cập theo vai trò."
        breadcrumb={[
          { label: "Quản trị" },
          { label: "Vai trò & phân quyền" },
        ]}
        actions={
          <Button
            type="button"
            iconLeft={<Save size={16} />}
            disabled={!selectedRole || isRootRole}
            onClick={handleSubmit}
          >
            Lưu phân quyền
          </Button>
        }
      />

      <div className="grid gap-4 md:grid-cols-3">
        <Card className="p-4" padded={false}>
          <div className="flex items-center justify-between gap-4">
            <div>
              <p className="text-sm font-medium text-[var(--hicas-text-secondary)]">Vai trò</p>
              <p className="mt-1 text-2xl font-bold text-[var(--hicas-text-main)]">
                {roles.length}
              </p>
            </div>
            <span className="flex h-11 w-11 items-center justify-center rounded-[var(--radius-lg)] bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange-dark)]">
              <Users size={20} />
            </span>
          </div>
        </Card>
        <Card className="p-4" padded={false}>
          <div className="flex items-center justify-between gap-4">
            <div>
              <p className="text-sm font-medium text-[var(--hicas-text-secondary)]">Nhóm quyền</p>
              <p className="mt-1 text-2xl font-bold text-[var(--hicas-text-main)]">
                {availableModulesTyped.length}
              </p>
            </div>
            <span className="flex h-11 w-11 items-center justify-center rounded-[var(--radius-lg)] bg-[var(--hicas-info-soft)] text-[var(--hicas-info)]">
              <ShieldCheck size={20} />
            </span>
          </div>
        </Card>
        <Card className="p-4" padded={false}>
          <div className="flex items-center justify-between gap-4">
            <div>
              <p className="text-sm font-medium text-[var(--hicas-text-secondary)]">
                Quyền đang chọn
              </p>
              <p className="mt-1 text-2xl font-bold text-[var(--hicas-text-main)]">
                {currentPermissions.length}/{totalPermissions}
              </p>
            </div>
            <span className="flex h-11 w-11 items-center justify-center rounded-[var(--radius-lg)] bg-[var(--hicas-success-soft)] text-[var(--hicas-success)]">
              <CheckSquare size={20} />
            </span>
          </div>
        </Card>
      </div>

      {message && (
        <div
          className={cn(
            "rounded-[var(--radius-lg)] border px-4 py-3 text-sm font-medium",
            message.type === "error"
              ? "border-[var(--hicas-danger)] bg-[var(--hicas-danger-soft)] text-[var(--hicas-danger)]"
              : "border-[var(--hicas-success)] bg-[var(--hicas-success-soft)] text-[var(--hicas-success)]",
          )}
        >
          {message.text}
        </div>
      )}

      <section className="grid items-start gap-6 xl:grid-cols-[300px_minmax(0,1fr)]">
        <Card
          title="Vai trò"
          description="Chọn một vai trò để xem và cập nhật quyền."
          actions={<Badge variant="orange">{roles.length} vai trò</Badge>}
        >
          {loading && roles.length === 0 ? (
            <p className="py-8 text-center text-sm text-[var(--hicas-text-secondary)]">
              Đang tải dữ liệu...
            </p>
          ) : (
            <div className="max-h-[640px] space-y-2 overflow-y-auto pr-1">
              {roles.map((role: RolePermission) => {
                const selected = selectedRoleId === role.roleId;
                const root = Number(role.roleId) === 1;

                return (
                  <button
                    key={role.roleId}
                    type="button"
                    onClick={() => setSelectedRoleId(role.roleId)}
                    className={cn(
                      "w-full rounded-[var(--radius-lg)] border px-4 py-3 text-left transition",
                      selected
                        ? "border-[var(--hicas-orange)] bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange-dark)]"
                        : "border-[var(--hicas-border)] bg-white text-[var(--hicas-text-main)] hover:border-[var(--hicas-orange)] hover:bg-[var(--hicas-orange-lighter)]",
                    )}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-sm font-semibold">{role.roleName}</p>
                        <p className="mt-1 text-xs text-[var(--hicas-text-secondary)]">
                          {role.permissions.length} quyền đang gán
                        </p>
                      </div>
                      <Badge variant={root ? "danger" : selected ? "orange" : "neutral"}>
                        {root ? "Quản trị gốc" : "Vai trò"}
                      </Badge>
                    </div>
                  </button>
                );
              })}
            </div>
          )}
        </Card>

        <Card
          title={selectedRole ? `Ma trận quyền: ${selectedRole.roleName}` : "Ma trận quyền"}
          description="Bật hoặc tắt quyền theo từng phân hệ."
          actions={
            selectedRole ? (
              <Badge variant={isRootRole ? "danger" : "info"}>
                {isRootRole ? "Không chỉnh sửa" : `${currentPermissions.length} quyền`}
              </Badge>
            ) : null
          }
        >
          {!selectedRole ? (
            <EmptyState
              title="Chưa chọn vai trò"
              description="Chọn một vai trò ở danh sách bên trái để cấu hình quyền."
            />
          ) : (
            <div className="space-y-5">
              {isRootRole && (
                <div className="flex gap-3 rounded-[var(--radius-lg)] border border-[var(--hicas-danger)] bg-[var(--hicas-danger-soft)] px-4 py-3 text-sm text-[var(--hicas-danger)]">
                  <LockKeyhole size={18} className="mt-0.5 shrink-0" />
                  <span>
                    Vai trò quản trị gốc là vai trò hệ thống, không cho phép chỉnh sửa từ giao diện để
                    tránh mất quyền quản trị cao nhất.
                  </span>
                </div>
              )}

              <div className="grid gap-4 2xl:grid-cols-2">
                {availableModulesTyped.map((module: PermissionModule) => {
                  const selectedCount = module.codes.filter((item) =>
                    currentPermissions.includes(item.code),
                  ).length;
                  const allSelected =
                    module.codes.length > 0 && selectedCount === module.codes.length;

                  return (
                    <section
                      key={module.group}
                      className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-4"
                    >
                      <div className="mb-4 flex items-start justify-between gap-3">
                        <div>
                          <h3 className="font-semibold text-[var(--hicas-text-main)]">
                            {module.group}
                          </h3>
                          <p className="mt-1 text-xs text-[var(--hicas-text-secondary)]">
                            {selectedCount}/{module.codes.length} quyền đã chọn
                          </p>
                        </div>
                        <Button
                          type="button"
                          size="sm"
                          variant={allSelected ? "secondary" : "ghost"}
                          onClick={() => handleSelectAll(module.codes)}
                          disabled={isRootRole}
                        >
                          {allSelected ? "Bỏ chọn" : "Chọn hết"}
                        </Button>
                      </div>

                      <div className="space-y-2">
                        {module.codes.map((item: PermissionItem) => {
                          const checked = currentPermissions.includes(item.code);

                          return (
                            <label
                              key={item.code}
                              className={cn(
                                "flex cursor-pointer items-start gap-3 rounded-[var(--radius-md)] border px-3 py-3 transition",
                                checked
                                  ? "border-[var(--hicas-orange)] bg-[var(--hicas-orange-lighter)]"
                                  : "border-[var(--hicas-border-soft)] bg-[var(--hicas-bg)] hover:border-[var(--hicas-orange)]",
                                isRootRole && "cursor-not-allowed opacity-75",
                              )}
                            >
                              <input
                                type="checkbox"
                                checked={checked}
                                onChange={() => handleCheckboxChange(item.code)}
                                disabled={isRootRole}
                                className="mt-1 h-4 w-4 rounded border-[var(--hicas-border)] accent-[var(--hicas-orange)]"
                              />
                              <span>
                                <span className="block font-mono text-xs font-semibold text-[var(--hicas-text-main)]">
                                  {item.code}
                                </span>
                                <span className="mt-1 block text-xs leading-5 text-[var(--hicas-text-secondary)]">
                                  {item.desc}
                                </span>
                              </span>
                            </label>
                          );
                        })}
                      </div>
                    </section>
                  );
                })}
              </div>
            </div>
          )}
        </Card>
      </section>
    </div>
  );
};
