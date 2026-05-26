import React, { useState, useEffect } from "react";
import { useNotificationTemplate } from "../hooks/useNotificationTemplate";
import type { NotificationTemplate } from "../types/notificationTemplate";

// Định nghĩa các biến cho phép hiển thị lên UI để hướng dẫn Admin
const PLACEHOLDERS: Record<string, string> = {
  PROMOTION: "{name}, {position}, {date}",
  NEW_TASK: "{name}, {task_name}, {deadline}",
  SLA_WARNING: "{name}, {module}, {hours_left}",
  LEAVE_REQUEST_CREATED:
    "{name}, {leave_type}, {start_date}, {end_date}, {days}, {status}",
  LEAVE_REQUEST_APPROVED:
    "{name}, {leave_type}, {start_date}, {end_date}, {days}, {status}",
  LEAVE_REQUEST_REJECTED:
    "{name}, {leave_type}, {start_date}, {end_date}, {days}, {status}, {reason}",
};

export const TemplateManager: React.FC = () => {
  const { templates, loading, updateTemplate } = useNotificationTemplate();
  const [selectedKey, setSelectedKey] = useState<string>("");
  const [formData, setFormData] = useState<NotificationTemplate>({
    templateKey: "",
    subject: "",
    bodyHtml: "",
  });
  const [message, setMessage] = useState<string>("");

  // Khi chọn mẫu khác, tự động điền dữ liệu cũ vào form
  useEffect(() => {
    const activeTemplate = templates.find((t) => t.templateKey === selectedKey);
    if (activeTemplate) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setFormData(activeTemplate);
      setMessage(""); // Xóa thông báo cũ
    } else {
      setFormData({ templateKey: selectedKey, subject: "", bodyHtml: "" });
    }
  }, [selectedKey, templates]);

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>,
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedKey) return;

    try {
      const res = await updateTemplate(selectedKey, formData);
      const message =
        typeof res === "object" && res !== null && "message" in res
          ? (res as { message?: string }).message
          : undefined;
      setMessage(message || "Cập nhật mẫu thành công!");
    } catch (error: unknown) {
      setMessage(`Lỗi: ${error}`);
    }
  };

  return (
    <div className="rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
      <h2 className="text-xl font-bold mb-4">
        Cấu hình Mẫu thông báo (Templates)
      </h2>

      {loading && templates.length === 0 ? (
        <p>Đang tải dữ liệu...</p>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {/* Cột trái: Danh sách chọn mẫu */}
          <div className="col-span-1 border-r pr-4">
            <h3 className="font-semibold mb-3">Chọn loại thông báo</h3>
            <div className="space-y-2">
              {Object.keys(PLACEHOLDERS).map((key) => (
                <button
                  key={key}
                  onClick={() => setSelectedKey(key)}
                  className={`w-full text-left p-2 rounded border ${
                    selectedKey === key
                      ? "bg-blue-50 border-blue-400 text-blue-700 font-medium"
                      : "hover:bg-gray-50"
                  }`}
                >
                  Mẫu: {key}
                </button>
              ))}
            </div>
          </div>

          {/* Cột phải: Form soạn thảo */}
          <div className="col-span-2">
            {!selectedKey ? (
              <p className="text-gray-500 italic mt-4">
                Vui lòng chọn một mẫu bên trái để chỉnh sửa.
              </p>
            ) : (
              <form onSubmit={handleSubmit} className="space-y-4">
                <div className="p-3 bg-yellow-50 border border-yellow-200 rounded text-sm text-yellow-800">
                  <strong>
                    Các biến hợp lệ (tự động thay thế tên người thực):
                  </strong>
                  <p className="font-mono mt-1">{PLACEHOLDERS[selectedKey]}</p>
                </div>

                <div>
                  <label className="block text-sm font-medium mb-1">
                    Tiêu đề (Subject) *
                  </label>
                  <input
                    required
                    name="subject"
                    value={formData.subject}
                    onChange={handleInputChange}
                    className="w-full border p-2 rounded"
                    placeholder="vd: Chúc mừng {name} thăng chức!"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium mb-1">
                    Nội dung (Body HTML) *
                  </label>
                  <textarea
                    required
                    name="bodyHtml"
                    value={formData.bodyHtml}
                    onChange={handleInputChange}
                    rows={6}
                    className="w-full border p-2 rounded"
                    placeholder="Nhập nội dung thông báo..."
                  />
                </div>

                <button
                  type="submit"
                  className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
                >
                  Lưu thay đổi
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
        </div>
      )}
    </div>
  );
};
