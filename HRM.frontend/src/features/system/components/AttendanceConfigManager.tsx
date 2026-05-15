import React, { useState, useEffect } from "react";
import { useAttendanceConfig } from "../hooks/useAttendanceConfig";
import type { AttendanceConfig } from "../types/attendanceConfig";

export const AttendanceConfigManager: React.FC = () => {
  const { config, loading, updateConfig } = useAttendanceConfig();
  const [message, setMessage] = useState<string>("");

  // Sử dụng state phẳng cho form, Gom mảng IP thành chuỗi để hiển thị trên Textarea
  const [formData, setFormData] = useState({
    latitude: 0,
    longitude: 0,
    radiusInMeters: 0,
    ipRangesString: "",
  });

  // Khi có dữ liệu từ DB, fill vào form
  useEffect(() => {
    if (config) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setFormData({
        latitude: config.latitude,
        longitude: config.longitude,
        radiusInMeters: config.radiusInMeters,
        ipRangesString: config.allowedIpRanges
          ? config.allowedIpRanges.join("\n")
          : "",
      });
      setMessage(""); // Reset message
    }
  }, [config]);

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>,
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      // Ép kiểu chuẩn bị Payload trước khi gửi
      const payload: AttendanceConfig = {
        latitude: Number(formData.latitude),
        longitude: Number(formData.longitude),
        radiusInMeters: Number(formData.radiusInMeters),
        // Cắt chuỗi theo dòng mới (\n), loại bỏ khoảng trắng và dòng trống
        allowedIpRanges: formData.ipRangesString
          .split("\n")
          .map((ip) => ip.trim())
          .filter((ip) => ip !== ""),
      };

      const res = (await updateConfig(payload)) as { message?: string };
      setMessage(res.message || "Lưu cấu hình thành công!");
    } catch (error: unknown) {
      setMessage(`Lỗi: ${(error as Error).message}`);
    }
  };

  return (
    <div className="p-4 bg-white rounded shadow">
      <h2 className="text-xl font-bold mb-4">
        Cấu hình Tham số Chấm công (GPS & IP)
      </h2>

      {loading && !config ? (
        <p>Đang tải dữ liệu...</p>
      ) : (
        <form onSubmit={handleSubmit} className="space-y-4 max-w-3xl">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1">
                Vĩ độ (Latitude) *
              </label>
              <input
                required
                type="number"
                step="any"
                name="latitude"
                value={formData.latitude}
                onChange={handleInputChange}
                className="w-full border p-2 rounded"
                placeholder="VD: 21.028511"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">
                Kinh độ (Longitude) *
              </label>
              <input
                required
                type="number"
                step="any"
                name="longitude"
                value={formData.longitude}
                onChange={handleInputChange}
                className="w-full border p-2 rounded"
                placeholder="VD: 105.804817"
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">
              Bán kính cho phép (Meters) *
            </label>
            <input
              required
              type="number"
              min="1"
              name="radiusInMeters"
              value={formData.radiusInMeters}
              onChange={handleInputChange}
              className="w-full border p-2 rounded md:w-1/2"
              placeholder="VD: 50"
            />
            <p className="text-xs text-gray-500 mt-1">
              Khoảng cách tối đa từ vị trí trên để được phép check-in.
            </p>
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">
              Dải IP Public hợp lệ (Mỗi IP một dòng)
            </label>
            <textarea
              name="ipRangesString"
              value={formData.ipRangesString}
              onChange={handleInputChange}
              rows={4}
              className="w-full border p-2 rounded font-mono text-sm"
              placeholder="VD:&#10;192.168.1.1&#10;14.232.11.0/24"
            />
            <p className="text-xs text-gray-500 mt-1">
              Nhân viên kết nối Wifi công ty sẽ thuộc dải IP này.
            </p>
          </div>

          <button
            type="submit"
            className="bg-purple-600 text-white px-4 py-2 rounded hover:bg-purple-700"
          >
            Lưu tham số chấm công
          </button>

          {message && (
            <p
              className={`mt-2 text-sm font-medium ${message.startsWith("Lỗi") ? "text-red-600" : "text-green-600"}`}
            >
              {message}
            </p>
          )}
        </form>
      )}
    </div>
  );
};
