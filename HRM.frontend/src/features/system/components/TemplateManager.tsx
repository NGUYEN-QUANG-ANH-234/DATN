import type { ChangeEvent, FormEvent } from "react";
import { useEffect, useState } from "react";
import { MailCheck, Save } from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card } from "../../../components/ui";
import { cn } from "../../../components/ui/classNames";
import { useNotificationTemplate } from "../hooks/useNotificationTemplate";
import type { NotificationTemplate } from "../types/notificationTemplate";

const PLACEHOLDERS: Record<string, string> = {
  PROMOTION: "{name}, {position}, {date}",
  NEW_TASK: "{name}, {task_name}, {deadline}",
  SLA_WARNING: "{name}, {module}, {hours_left}",
  LEAVE_REQUEST_CREATED: "{name}, {leave_type}, {start_date}, {end_date}, {days}, {status}",
  LEAVE_REQUEST_APPROVED: "{name}, {leave_type}, {start_date}, {end_date}, {days}, {status}",
  LEAVE_REQUEST_REJECTED:
    "{name}, {leave_type}, {start_date}, {end_date}, {days}, {status}, {reason}",
};

const templateName: Record<string, string> = {
  PROMOTION: "Thông báo thăng tiến",
  NEW_TASK: "Giao việc mới",
  SLA_WARNING: "Cảnh báo quá hạn SLA",
  LEAVE_REQUEST_CREATED: "Tạo đơn nghỉ phép",
  LEAVE_REQUEST_APPROVED: "Duyệt đơn nghỉ phép",
  LEAVE_REQUEST_REJECTED: "Từ chối đơn nghỉ phép",
};

type MessageState = {
  type: "success" | "error";
  text: string;
};

export const TemplateManager = () => {
  const { templates, loading, updateTemplate } = useNotificationTemplate();
  const [selectedKey, setSelectedKey] = useState<string>("PROMOTION");
  const [formData, setFormData] = useState<NotificationTemplate>({
    templateKey: "PROMOTION",
    subject: "",
    bodyHtml: "",
  });
  const [message, setMessage] = useState<MessageState | null>(null);

  useEffect(() => {
    const activeTemplate = templates.find((template) => template.templateKey === selectedKey);
    if (activeTemplate) {
      setFormData(activeTemplate);
      setMessage(null);
      return;
    }

    setFormData({ templateKey: selectedKey, subject: "", bodyHtml: "" });
  }, [selectedKey, templates]);

  const handleInputChange = (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = event.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    if (!selectedKey) return;

    try {
      const res = await updateTemplate(selectedKey, formData);
      const responseMessage =
        typeof res === "object" && res !== null && "message" in res
          ? (res as { message?: string }).message
          : undefined;
      setMessage({
        type: "success",
        text: responseMessage || "Đã cập nhật mẫu thông báo.",
      });
    } catch (error: unknown) {
      setMessage({ type: "error", text: String(error) });
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="F0.3 Cấu hình mẫu thông báo"
        description="Quản lý nội dung Email/In-app notification với biến động để hệ thống tự sinh nội dung theo từng sự kiện."
        breadcrumb={[
          { label: "Module 0" },
          { label: "Cấu hình hệ thống" },
          { label: "Mẫu thông báo" },
        ]}
      />

      {message && (
        <div
          className={`rounded-2xl border px-4 py-3 text-sm font-medium ${
            message.type === "error"
              ? "border-[var(--hicas-danger)] bg-[var(--hicas-danger-soft)] text-[var(--hicas-danger)]"
              : "border-[var(--hicas-success)] bg-[var(--hicas-success-soft)] text-[var(--hicas-success)]"
          }`}
        >
          {message.text}
        </div>
      )}

      <div className="grid gap-6 xl:grid-cols-[340px_minmax(0,1fr)]">
        <Card
          title="Danh sách mẫu"
          description="Chọn mẫu cần chỉnh sửa hoặc preview."
          actions={<Badge variant="orange">{templates.length} mẫu đã lưu</Badge>}
        >
          <div className="space-y-2">
            {Object.keys(PLACEHOLDERS).map((key) => (
              <button
                key={key}
                type="button"
                onClick={() => setSelectedKey(key)}
                className={cn(
                  "w-full rounded-2xl border px-4 py-3 text-left transition",
                  selectedKey === key
                    ? "border-[var(--hicas-orange)] bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange-dark)]"
                    : "border-[var(--hicas-border)] bg-white hover:border-[var(--hicas-orange)] hover:bg-[var(--hicas-orange-lighter)]",
                )}
              >
                <p className="text-sm font-semibold">{templateName[key] ?? key}</p>
                <p className="mt-1 font-mono text-xs text-[var(--hicas-text-secondary)]">
                  {key}
                </p>
              </button>
            ))}
          </div>
        </Card>

        <Card
          title={templateName[selectedKey] ?? selectedKey}
          description="Soạn tiêu đề và nội dung HTML. Hệ thống sẽ thay biến khi gửi thông báo."
          actions={<MailCheck size={20} className="text-[var(--hicas-orange)]" />}
        >
          {loading && templates.length === 0 ? (
            <div className="py-10 text-center text-sm text-[var(--hicas-text-secondary)]">
              Đang tải dữ liệu...
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-5">
              <div className="rounded-2xl border border-[var(--hicas-warning)] bg-[var(--hicas-warning-soft)] p-4 text-sm text-amber-800">
                <p className="font-semibold">Biến hợp lệ</p>
                <p className="mt-2 font-mono">{PLACEHOLDERS[selectedKey]}</p>
              </div>

              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Tiêu đề *</span>
                <input
                  required
                  name="subject"
                  value={formData.subject}
                  onChange={handleInputChange}
                  className="hicas-input w-full"
                  placeholder="Ví dụ: Đơn nghỉ phép của {name} đã được duyệt"
                />
              </label>

              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Nội dung HTML *</span>
                <textarea
                  required
                  name="bodyHtml"
                  value={formData.bodyHtml}
                  onChange={handleInputChange}
                  rows={8}
                  className="hicas-textarea w-full"
                  placeholder="Nhập nội dung thông báo..."
                />
              </label>

              <div className="rounded-2xl border border-[var(--hicas-border)] bg-[var(--hicas-bg-soft)] p-4">
                <div className="mb-2 flex items-center justify-between">
                  <p className="text-sm font-semibold">Preview</p>
                  <Badge variant="neutral">In-app / Email</Badge>
                </div>
                <p className="text-sm font-bold text-[var(--hicas-text-main)]">
                  {formData.subject || "Tiêu đề thông báo"}
                </p>
                <div
                  className="mt-3 text-sm leading-6 text-[var(--hicas-text-secondary)]"
                  dangerouslySetInnerHTML={{
                    __html: formData.bodyHtml || "Nội dung thông báo sẽ hiển thị tại đây.",
                  }}
                />
              </div>

              <Button type="submit" iconLeft={<Save size={17} />}>
                Lưu thay đổi
              </Button>
            </form>
          )}
        </Card>
      </div>
    </div>
  );
};
