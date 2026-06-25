import React, { useState } from "react";
import {
  Badge,
  Button,
  Card,
  DataTable,
  type BadgeVariant,
  type DataTableColumn,
} from "../../../components/ui";
import { formatDateTime } from "../../../utils/formatters";
import { useAuditLogs } from "../hooks/useAuditLogs";
import type { AuditLog, AuditLogFilter } from "../types/auditLog";

const MODULE_OPTIONS = [
  { value: "", label: "Tất cả hệ thống" },
  { value: "accounts", label: "Tài khoản & đăng nhập" },
  { value: "role_permissions", label: "Phân quyền (RBAC)" },
  { value: "configurations", label: "Cấu hình hệ thống" },
  { value: "employees", label: "Hồ sơ nhân sự" },
  { value: "payrolls", label: "Bảng lương" },
];

const ACTION_LABELS: Record<string, { label: string; variant: BadgeVariant }> = {
  Added: { label: "Thêm mới", variant: "success" },
  Modified: { label: "Cập nhật", variant: "orange" },
  Deleted: { label: "Xóa", variant: "danger" },
};

const getModuleName = (tableName: string) =>
  MODULE_OPTIONS.find((option) => option.value === tableName)?.label || tableName;

const getActionBadge = (action: string) => {
  const normalizedAction = action.toUpperCase();
  const actionMeta =
    ACTION_LABELS[action] ||
    (normalizedAction.includes("LOGIN") ||
    normalizedAction.includes("LOGOUT") ||
    normalizedAction.includes("TOKEN")
      ? { label: "Bảo mật", variant: "info" as BadgeVariant }
      : { label: action, variant: "neutral" as BadgeVariant });

  return <Badge variant={actionMeta.variant}>{actionMeta.label}</Badge>;
};

const renderBusinessEvidence = (log: AuditLog) => {
  const moduleName = getModuleName(log.tableName);
  const normalizedAction = log.actionType.toUpperCase();

  if (
    normalizedAction.includes("LOGIN") ||
    normalizedAction.includes("LOGOUT") ||
    normalizedAction.includes("TOKEN")
  ) {
    try {
      const message = JSON.parse(log.newValues || "{}").Message;
      return <span>{message || log.actionType}</span>;
    } catch {
      return <span>{log.actionType}</span>;
    }
  }

  try {
    const oldObj = log.oldValues ? (JSON.parse(log.oldValues) as Record<string, unknown>) : null;
    const newObj = log.newValues ? (JSON.parse(log.newValues) as Record<string, unknown>) : null;

    if (log.actionType === "Added") {
      return (
        <span>
          Đã tạo mới dữ liệu trong phân hệ{" "}
          <strong className="text-[var(--hicas-success)]">{moduleName}</strong>
        </span>
      );
    }

    if (log.actionType === "Deleted") {
      return (
        <span>
          Đã xóa dữ liệu khỏi phân hệ{" "}
          <strong className="text-[var(--hicas-danger)]">{moduleName}</strong>
        </span>
      );
    }

    if (log.actionType === "Modified") {
      if (!newObj) {
        return (
          <span>
            Đã cập nhật phân hệ{" "}
            <strong className="text-[var(--hicas-orange)]">{moduleName}</strong>
          </span>
        );
      }

      const changes = Object.keys(newObj).map((key) => {
        const oldValue =
          oldObj && oldObj[key] !== undefined && oldObj[key] !== null
            ? String(oldObj[key])
            : "Trống";
        const newValue = newObj[key] !== null ? String(newObj[key]) : "Trống";

        return (
          <li key={key} className="mt-1 text-xs">
            Đổi <strong>{key}</strong>: từ{" "}
            <span className="text-[var(--hicas-danger)] line-through">{oldValue}</span>{" "}
            thành{" "}
            <span className="font-semibold text-[var(--hicas-success)]">{newValue}</span>
          </li>
        );
      });

      return <ul className="list-disc pl-4">{changes}</ul>;
    }

    return (
      <span>
        Thao tác trên <strong className="text-[var(--hicas-orange)]">{moduleName}</strong>
      </span>
    );
  } catch {
    return (
      <span className="text-xs italic text-[var(--hicas-text-secondary)]">
        Dữ liệu cấu trúc phức tạp.
      </span>
    );
  }
};

export const AuditLogViewer: React.FC = () => {
  const { logs, loading, fetchLogs } = useAuditLogs();

  const [filter, setFilter] = useState<AuditLogFilter>({
    accountId: "",
    module: "",
    startDate: "",
    endDate: "",
  });

  const handleFilterChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) => {
    const { name, value } = e.target;
    setFilter((prev) => ({ ...prev, [name]: value }));
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    fetchLogs(filter);
  };

  const columns: Array<DataTableColumn<AuditLog>> = [
    {
      key: "timestamp",
      header: "Thời gian",
      className: "whitespace-nowrap",
      render: (log) => (
        <span className="text-sm text-[var(--hicas-text-secondary)]">
          {formatDateTime(log.timestamp)}
        </span>
      ),
    },
    {
      key: "account",
      header: "Tài khoản",
      className: "whitespace-nowrap",
      render: (log) => (
        <span className="font-medium text-[var(--hicas-text-main)]">
          {log.accountId ? `User #${log.accountId}` : "Hệ thống"}
        </span>
      ),
    },
    {
      key: "action",
      header: "Thao tác",
      className: "whitespace-nowrap",
      render: (log) => getActionBadge(log.actionType),
    },
    {
      key: "detail",
      header: "Chi tiết nghiệp vụ",
      render: (log) => (
        <div className="max-w-3xl text-sm leading-6 text-[var(--hicas-text-main)]">
          {renderBusinessEvidence(log)}
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-5">
      <Card
        title="Nhật ký hoạt động hệ thống"
        description="Tra cứu thao tác quan trọng, biến động dữ liệu nhạy cảm và các sự kiện bảo mật."
      >
        <form onSubmit={handleSearch} className="grid gap-4 md:grid-cols-2 xl:grid-cols-[140px_260px_180px_180px_auto]">
          <label className="space-y-1 text-sm font-medium text-[var(--hicas-text-main)]">
            Mã tài khoản
            <input
              type="number"
              name="accountId"
              value={filter.accountId}
              onChange={handleFilterChange}
              placeholder="ID..."
              className="hicas-input w-full"
            />
          </label>

          <label className="space-y-1 text-sm font-medium text-[var(--hicas-text-main)]">
            Phân hệ
            <select
              name="module"
              value={filter.module}
              onChange={handleFilterChange}
              className="hicas-input w-full"
            >
              {MODULE_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>

          <label className="space-y-1 text-sm font-medium text-[var(--hicas-text-main)]">
            Từ ngày
            <input
              type="date"
              name="startDate"
              value={filter.startDate}
              onChange={handleFilterChange}
              className="hicas-input w-full"
            />
          </label>

          <label className="space-y-1 text-sm font-medium text-[var(--hicas-text-main)]">
            Đến ngày
            <input
              type="date"
              name="endDate"
              value={filter.endDate}
              onChange={handleFilterChange}
              className="hicas-input w-full"
            />
          </label>

          <div className="flex items-end">
            <Button type="submit" isLoading={loading} fullWidth>
              Lọc dữ liệu
            </Button>
          </div>
        </form>
      </Card>

      <div className="max-h-[640px] overflow-y-auto pr-1">
      <DataTable
        columns={columns}
        data={logs}
        rowKey={(log) => log.id}
        loading={loading}
        emptyTitle="Không có nhật ký phù hợp"
        emptyDescription="Thử mở rộng bộ lọc hoặc chọn khoảng thời gian khác."
      />
      </div>
    </div>
  );
};
