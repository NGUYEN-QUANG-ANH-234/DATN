import React, { useState } from "react";
import { useSla } from "../hooks/useSla";
import type { SlaUpdateRequest } from "../types/sla";
import { useSalaryVariable } from "../hooks/useSalaryVariable";

export const SlaManager: React.FC = () => {
  const { catalogs } = useSalaryVariable();
  const { slas, loading, updateSla } = useSla();
  const [formData, setFormData] = useState<SlaUpdateRequest>({
    moduleCode: "",
    value: "",
    unit: "HOURS",
  });

  const availableModules = Array.from(new Set(catalogs.map((c) => c.module)));
  const [message, setMessage] = useState<string>("");

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await updateSla(formData);
      const message =
        typeof res === "object" && res !== null && "message" in res
          ? (res as { message?: string }).message
          : undefined;
      setMessage(message || "Cập nhật thành công!");
      setFormData({ moduleCode: "", value: "", unit: "HOURS" });
    } catch (error: unknown) {
      setMessage(`Lỗi: ${String(error)}`);
    }
  };

  return (
    <div className="p-4 bg-white rounded shadow">
      <h2 className="text-xl font-bold mb-4">
        Cấu hình Thời hạn Phê duyệt (SLA)
      </h2>

      <form
        onSubmit={handleSubmit}
        className="mb-8 grid grid-cols-1 md:grid-cols-4 gap-4 items-end"
      >
        <div>
          <label className="block text-sm font-medium mb-1">
            Phân hệ (Module) *
          </label>
          <select
            required
            name="moduleCode"
            value={formData.moduleCode}
            onChange={handleInputChange}
            className="w-full border p-2 rounded"
          >
            <option value="">-- Chọn phân hệ --</option>
            {availableModules.map((module) => (
              <option key={module} value={module.toUpperCase()}>
                Phê duyệt {module}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Thời hạn *</label>
          <input
            required
            type="number"
            min="1"
            name="value"
            value={formData.value}
            onChange={handleInputChange}
            className="w-full border p-2 rounded"
            placeholder="vd: 48"
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Đơn vị *</label>
          <select
            required
            name="unit"
            value={formData.unit}
            onChange={handleInputChange}
            className="w-full border p-2 rounded"
          >
            <option value="HOURS">Giờ (HOURS)</option>
            <option value="DAYS">Ngày (DAYS)</option>
          </select>
        </div>

        <button
          type="submit"
          className="bg-green-600 text-white p-2 rounded hover:bg-green-700"
        >
          Cập nhật SLA
        </button>
      </form>

      {message && (
        <p className="mb-4 text-sm font-medium text-green-600">{message}</p>
      )}

      {/* Bảng hiển thị SLA hiện tại */}
      {loading ? (
        <p className="text-center py-4">Đang tải dữ liệu...</p>
      ) : (
        <table className="w-full text-left border-collapse border">
          <thead className="bg-gray-100">
            <tr>
              <th className="border p-2">Mã Phân hệ</th>
              <th className="border p-2">Thời hạn quy định</th>
              <th className="border p-2">Đơn vị</th>
            </tr>
          </thead>
          <tbody>
            {slas && slas.length > 0 ? (
              slas.map((s, index) => {
                const slaItem = s as {
                  code?: string;
                  moduleCode?: string;
                  ModuleCode?: string;
                  value?: string | number;
                  Value?: string | number;
                  unit?: string;
                  Unit?: string;
                };

                return (
                  <tr key={index} className="hover:bg-gray-50">
                    {/* CẬP NHẬT: Lấy s.code hoặc s.moduleCode để phòng hờ chênh lệch chữ hoa/thường */}
                    <td className="border p-2 font-bold text-gray-700">
                      {slaItem.code || slaItem.moduleCode || slaItem.ModuleCode}
                    </td>
                    <td className="border p-2 text-blue-600 font-bold">
                      {slaItem.value || slaItem.Value}
                    </td>
                    <td className="border p-2">
                      {/* Bắt cả Unit hoặc unit */}
                      {(slaItem.unit || slaItem.Unit) === "HOURS"
                        ? "Giờ"
                        : "Ngày"}
                    </td>
                  </tr>
                );
              })
            ) : (
              <tr>
                <td colSpan={3} className="text-center p-4">
                  Chưa có cấu hình SLA nào.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      )}
    </div>
  );
};
