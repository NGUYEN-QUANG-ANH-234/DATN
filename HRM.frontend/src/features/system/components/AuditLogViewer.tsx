import React, { useState } from "react";
import { useAuditLogs } from "../hooks/useAuditLogs";
import type { AuditLog, AuditLogFilter } from "../types/auditLog";

// 1. MAPPING MODULE (BÌNH PHONG BẢO MẬT & DỄ ĐỌC)
const MODULE_OPTIONS = [
  { value: "", label: "-- Tất cả hệ thống --" },
  { value: "accounts", label: "Tài khoản & Đăng nhập" },
  { value: "role_permissions", label: "Phân quyền (RBAC)" },
  { value: "configurations", label: "Cấu hình hệ thống" },
  { value: "employees", label: "Hồ sơ nhân sự" },
  { value: "payrolls", label: "Bảng lương" },
];

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

  // 2. DỊCH HÀNH ĐỘNG & GẮN MÀU SẮC (UX)
  const translateAction = (action: string) => {
    if (action === "Added")
      return (
        <span className="px-2 py-1 bg-green-100 text-green-800 rounded text-xs font-bold">
          Thêm mới
        </span>
      );
    if (action === "Modified")
      return (
        <span className="px-2 py-1 bg-orange-100 text-orange-800 rounded text-xs font-bold">
          Cập nhật
        </span>
      );
    if (action === "Deleted")
      return (
        <span className="px-2 py-1 bg-red-100 text-red-800 rounded text-xs font-bold">
          Xóa
        </span>
      );

    // Bắt các action hệ thống (LOGIN, LOGOUT, REFRESH TOKEN...)
    if (
      action.includes("LOGIN") ||
      action.includes("LOGOUT") ||
      action.includes("TOKEN")
    ) {
      return (
        <span className="px-2 py-1 bg-blue-100 text-blue-800 rounded text-xs font-bold">
          Bảo mật
        </span>
      );
    }

    return (
      <span className="px-2 py-1 bg-gray-100 text-gray-800 rounded text-xs font-bold">
        {action}
      </span>
    );
  };

  // 3. DỊCH JSON THÀNH NGÔN NGỮ NGHIỆP VỤ
  const renderBusinessEvidence = (log: AuditLog) => {
    // Tìm tên tiếng Việt của module
    const moduleName =
      MODULE_OPTIONS.find((m) => m.value === log.tableName)?.label ||
      log.tableName;

    // Xử lý Event Hệ thống (Ví dụ: Đăng nhập sai, Gửi OTP)
    if (
      log.actionType.includes("LOGIN") ||
      log.actionType.includes("LOGOUT") ||
      log.actionType.includes("TOKEN")
    ) {
      try {
        const msg = JSON.parse(log.newValues || "{}").Message;
        return <span className="text-gray-700">{msg || log.actionType}</span>;
      } catch {
        return <span className="text-gray-700">{log.actionType}</span>;
      }
    }

    // Xử lý Data Thêm/Sửa/Xóa (CRUD)
    try {
      const oldObj = log.oldValues
        ? (JSON.parse(log.oldValues) as Record<string, unknown>)
        : null;
      const newObj = log.newValues
        ? (JSON.parse(log.newValues) as Record<string, unknown>)
        : null;

      if (log.actionType === "Added") {
        return (
          <span>
            Đã tạo mới dữ liệu trong phân hệ{" "}
            <strong className="text-green-600">{moduleName}</strong>
          </span>
        );
      }
      if (log.actionType === "Deleted") {
        return (
          <span>
            Đã xóa dữ liệu khỏi phân hệ{" "}
            <strong className="text-red-600">{moduleName}</strong>
          </span>
        );
      }
      if (log.actionType === "Modified") {
        if (!newObj)
          return (
            <span>
              Đã cập nhật phân hệ{" "}
              <strong className="text-orange-600">{moduleName}</strong>
            </span>
          );

        // Liệt kê chi tiết những trường bị thay đổi
        const changes = Object.keys(newObj).map((key) => {
          const oldVal =
            oldObj && oldObj[key] !== undefined && oldObj[key] !== null
              ? String(oldObj[key])
              : "Trống";
          const newVal = newObj[key] !== null ? String(newObj[key]) : "Trống";

          return (
            <li key={key} className="text-xs mt-1">
              Đổi <strong className="text-gray-700">{key}</strong>: từ{" "}
              <span className="line-through text-red-500">{oldVal}</span> thành{" "}
              <span className="text-green-600 font-semibold">{newVal}</span>
            </li>
          );
        });
        return <ul className="list-disc pl-4">{changes}</ul>;
      }

      return (
        <span>
          Thao tác trên{" "}
          <strong className="text-purple-600">{moduleName}</strong>
        </span>
      );
    } catch {
      return (
        <span className="text-xs text-gray-500 italic">
          Dữ liệu cấu trúc phức tạp.
        </span>
      );
    }
  };

  return (
    <div className="rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
      <h2 className="text-xl font-bold mb-4">Nhật ký Hoạt động Hệ thống</h2>

      {/* Vùng Lọc (Filter) */}
      <form
        onSubmit={handleSearch}
        className="mb-6 p-4 bg-gray-50 rounded border flex flex-wrap gap-4 items-end"
      >
        <div>
          <label className="block text-sm font-medium mb-1">Mã Tài Khoản</label>
          <input
            type="number"
            name="accountId"
            value={filter.accountId}
            onChange={handleFilterChange}
            placeholder="ID..."
            className="border p-2 rounded w-28 bg-white"
          />
        </div>

        {/* SỬ DỤNG SELECT BOX THAY CHO INPUT */}
        <div>
          <label className="block text-sm font-medium mb-1">
            Phân hệ (Module)
          </label>
          <select
            name="module"
            value={filter.module}
            onChange={handleFilterChange}
            className="border p-2 rounded w-56 bg-white"
          >
            {MODULE_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Từ ngày</label>
          <input
            type="date"
            name="startDate"
            value={filter.startDate}
            onChange={handleFilterChange}
            className="border p-2 rounded bg-white"
          />
        </div>
        <div>
          <label className="block text-sm font-medium mb-1">Đến ngày</label>
          <input
            type="date"
            name="endDate"
            value={filter.endDate}
            onChange={handleFilterChange}
            className="border p-2 rounded bg-white"
          />
        </div>
        <button
          type="submit"
          className="bg-purple-600 text-white px-5 py-2 rounded hover:bg-purple-700 font-medium transition-colors"
        >
          Lọc dữ liệu
        </button>
      </form>

      {/* Bảng Dữ liệu Đã Tối Ưu */}
      <div className="overflow-x-auto">
        {loading ? (
          <p className="text-center p-4 font-medium text-gray-600">
            Đang đồng bộ nhật ký...
          </p>
        ) : logs.length === 0 ? (
          <p className="text-center p-4 text-gray-500">
            Không có biến động nào trong khoảng thời gian này.
          </p>
        ) : (
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-gray-100 border-b">
                <th className="p-3 text-sm font-semibold text-gray-700">
                  Thời gian
                </th>
                <th className="p-3 text-sm font-semibold text-gray-700">
                  Tài khoản
                </th>
                <th className="p-3 text-sm font-semibold text-gray-700">
                  Thao tác
                </th>
                <th className="p-3 text-sm font-semibold text-gray-700">
                  Chi tiết Nghiệp vụ
                </th>
              </tr>
            </thead>
            <tbody>
              {logs.map((log) => (
                <tr
                  key={log.id}
                  className="border-b hover:bg-gray-50 align-top transition-colors"
                >
                  <td className="p-3 text-sm text-gray-600 whitespace-nowrap">
                    {new Date(log.timestamp).toLocaleString("vi-VN", {
                      hour: "2-digit",
                      minute: "2-digit",
                      day: "2-digit",
                      month: "2-digit",
                      year: "numeric",
                    })}
                  </td>
                  <td className="p-3 text-sm font-medium text-purple-700 whitespace-nowrap">
                    {log.accountId ? `User #${log.accountId}` : "Hệ thống"}
                  </td>
                  <td className="p-3 whitespace-nowrap">
                    {translateAction(log.actionType)}
                  </td>
                  <td className="p-3 text-sm text-gray-800">
                    {renderBusinessEvidence(log)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};
