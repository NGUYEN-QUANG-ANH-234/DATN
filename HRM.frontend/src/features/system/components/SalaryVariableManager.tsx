import React, { useState } from "react";
import { useSalaryVariable } from "../hooks/useSalaryVariable";
import type { SalaryVariable } from "../types/salaryVariable";

export const SalaryVariableManager: React.FC = () => {
  // Lấy thêm catalogs từ hook
  const { variables, catalogs, loading, defineVariable } = useSalaryVariable();
  const [formData, setFormData] = useState<SalaryVariable>({
    code: "",
    source: "",
    description: "",
  });
  const [message, setMessage] = useState<string>("");

  // SỬA ĐIỂM 1: Cập nhật Type để nhận cả Input và Select
  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await defineVariable(formData);
      setMessage(res.message || "Lưu thành công!");
      setFormData({ code: "", source: "", description: "" });
    } catch (error: unknown) {
      setMessage(`Lỗi: ${(error as Error).message}`);
    }
  };

  return (
    <div className="p-4 bg-white rounded shadow">
      <h2 className="text-xl font-bold mb-4">
        Quản lý Biến Lương Hệ Thống (F0.1)
      </h2>

      <form
        onSubmit={handleSubmit}
        className="mb-8 grid grid-cols-1 md:grid-cols-4 gap-4 items-end"
      >
        {/* Input Code (Giữ nguyên) */}
        <div>
          <label className="block text-sm font-medium mb-1">
            Mã biến (Code) *
          </label>
          <input
            required
            name="code"
            value={formData.code}
            onChange={handleInputChange}
            pattern="^[a-zA-Z0-9_]+$"
            title="Không chứa ký tự đặc biệt"
            className="w-full border p-2 rounded"
            placeholder="vd: ot_hours"
          />
        </div>

        {/* SỬA ĐIỂM 2: Thay Input thành Select cho Nguồn dữ liệu */}
        <div>
          <label className="block text-sm font-medium mb-1">
            Nguồn dữ liệu (Source) *
          </label>
          <select
            required
            name="source"
            value={formData.source}
            onChange={handleInputChange}
            className="w-full border p-2 rounded bg-white"
          >
            <option value="" disabled>
              -- Chọn mỏ dữ liệu chuẩn --
            </option>
            {catalogs.map((item) => (
              <option key={item.id} value={item.sourcePath}>
                [{item.module}] {item.displayName}
              </option>
            ))}
          </select>
        </div>

        {/* Input Description (Giữ nguyên) */}
        <div>
          <label className="block text-sm font-medium mb-1">Mô tả</label>
          <input
            name="description"
            value={formData.description}
            onChange={handleInputChange}
            className="w-full border p-2 rounded"
            placeholder="vd: Giờ tăng ca"
          />
        </div>

        <button
          type="submit"
          className="bg-blue-600 text-white p-2 rounded hover:bg-blue-700"
        >
          Lưu Biến Lương
        </button>
      </form>

      {message && (
        <p className="mb-4 text-sm font-medium text-blue-600">{message}</p>
      )}

      {/* Bảng danh sách (Giữ nguyên) */}
      {loading ? (
        <p className="text-center py-4">Đang tải dữ liệu...</p>
      ) : (
        <table className="w-full text-left border-collapse border">
          <thead className="bg-gray-100">
            <tr>
              <th className="border p-2">Mã biến (Code)</th>
              <th className="border p-2">Nguồn dữ liệu (Mapping)</th>
              <th className="border p-2">Mô tả</th>
            </tr>
          </thead>
          <tbody>
            {variables && variables.length > 0 ? (
              variables.map((v, index) => (
                <tr key={`${v.code}-${index}`} className="hover:bg-gray-50">
                  <td className="border p-2 font-mono text-blue-600">
                    {v.code}
                  </td>
                  <td className="border p-2 font-mono">{v.source}</td>
                  <td className="border p-2">{v.description}</td>
                </tr>
              ))
            ) : (
              <tr>
                <td colSpan={3} className="text-center p-4 text-gray-500">
                  Chưa có biến lương nào được định nghĩa hoặc lỗi tải dữ liệu.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      )}
    </div>
  );
};
