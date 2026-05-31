import React, { useMemo, useState } from "react";
import { Button, Card, DataTable, StatusBadge, type DataTableColumn } from "../../../components/ui";
import { useAccounts } from "../hooks/useAccounts";
import type { Account, CreateAccountDto } from "../types/account";

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
      return roleName.includes("candidate") || roleName.includes("ứng viên");
    });

    return candidateRole?.id ?? roles[0]?.id ?? 8;
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

  const columns: Array<DataTableColumn<Account>> = [
    {
      key: "id",
      header: "ID",
      render: (account) => (
        <span className="text-sm text-[var(--hicas-text-secondary)]">#{account.id}</span>
      ),
    },
    {
      key: "employee",
      header: "Nhân sự",
      render: (account) => (
        <div>
          <p className="font-semibold text-[var(--hicas-text-main)]">{account.fullName}</p>
          <p className="text-xs text-[var(--hicas-text-secondary)]">{account.email}</p>
        </div>
      ),
    },
    {
      key: "role",
      header: "Vai trò",
      render: (account) => (
        <div className="space-y-1">
          <select
            value={account.roleId}
            onChange={(e) => handleUpdateRole(account.id, Number(e.target.value))}
            className="hicas-input min-w-40 text-sm"
          >
            {roles.map((role) => (
              <option key={role.id} value={role.id}>
                {role.name}
              </option>
            ))}
          </select>
          {account.roleId === 1 && (
            <span className="block text-[11px] font-medium text-[var(--hicas-danger)]">
              Quản trị hệ thống
            </span>
          )}
        </div>
      ),
    },
    {
      key: "status",
      header: "Trạng thái",
      render: (account) => <StatusBadge status={account.status} />,
    },
    {
      key: "mfa",
      header: "MFA",
      render: (account) => (
        <StatusBadge status={account.isMfaEnabled ? "Active" : "Inactive"} />
      ),
    },
    {
      key: "actions",
      header: "Hành động",
      className: "min-w-52",
      render: (account) => (
        <div className="flex flex-wrap gap-2">
          <Button
            size="sm"
            variant={account.status === "Active" ? "danger" : "secondary"}
            onClick={() => handleToggleStatus(account.id, account.status)}
          >
            {account.status === "Active" ? "Khóa" : "Mở khóa"}
          </Button>
          <Button
            size="sm"
            variant="secondary"
            onClick={() => handleResetPassword(account.id)}
          >
            Cấp lại mật khẩu
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-5">
      <Card
        title="Quản trị tài khoản hệ thống"
        description="Khởi tạo tài khoản, gán vai trò, khóa/mở khóa truy cập và cấp lại mật khẩu khi cần."
      >
        <form onSubmit={onSubmitCreate} className="grid gap-4 lg:grid-cols-[1fr_1fr_1fr_220px_auto]">
          <label className="space-y-1 text-sm font-medium text-[var(--hicas-text-main)]">
            Email nội bộ
            <input
              type="email"
              required
              value={formData.email}
              onChange={(e) => setFormData({ ...formData, email: e.target.value })}
              placeholder="nguyenvana@hicas.vn"
              className="hicas-input w-full"
            />
          </label>

          <label className="space-y-1 text-sm font-medium text-[var(--hicas-text-main)]">
            Họ và tên
            <input
              type="text"
              required
              value={formData.fullName}
              onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
              placeholder="Nguyễn Văn A"
              className="hicas-input w-full"
            />
          </label>

          <label className="space-y-1 text-sm font-medium text-[var(--hicas-text-main)]">
            Mật khẩu khởi tạo
            <input
              type="password"
              minLength={8}
              value={formData.password || ""}
              onChange={(e) => setFormData({ ...formData, password: e.target.value })}
              placeholder="Để trống để sinh tự động"
              className="hicas-input w-full"
            />
          </label>

          <label className="space-y-1 text-sm font-medium text-[var(--hicas-text-main)]">
            Vai trò mặc định
            <select
              required
              value={selectedRoleId}
              onChange={(e) => setFormData({ ...formData, roleId: Number(e.target.value) })}
              className="hicas-input w-full"
            >
              {roles.map((role) => (
                <option key={role.id} value={role.id}>
                  {role.name}
                </option>
              ))}
            </select>
          </label>

          <div className="flex items-end">
            <Button type="submit" fullWidth>
              Tạo tài khoản
            </Button>
          </div>
        </form>
      </Card>

      <DataTable
        columns={columns}
        data={accounts}
        rowKey={(account) => account.id}
        loading={loading}
        emptyTitle="Chưa có tài khoản"
        emptyDescription="Tài khoản mới sẽ xuất hiện tại đây sau khi được khởi tạo."
      />
    </div>
  );
};
