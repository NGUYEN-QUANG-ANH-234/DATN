import type { ChangeEvent, FormEvent } from "react";
import { useEffect, useMemo, useState } from "react";
import {
  Activity,
  AlertTriangle,
  Bell,
  CheckCircle2,
  Clock3,
  ListChecks,
  MailCheck,
  Plus,
  Save,
  Trash2,
} from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card } from "../../../components/ui";
import { cn } from "../../../components/ui/classNames";
import { useNotificationTemplate } from "../hooks/useNotificationTemplate";
import type { NotificationTemplate, TemplateVariable } from "../types/notificationTemplate";

type MessageState = {
  type: "success" | "error";
  text: string;
};

const emptyTemplate = (templateKey = ""): NotificationTemplate => ({
  templateKey,
  displayName: templateKey,
  category: "Mẫu hệ thống",
  allowedPlaceholders: [],
  systemPlaceholders: [],
  customVariables: [],
  subject: "",
  bodyHtml: "",
});

const emptyVariable = (): TemplateVariable => ({
  code: "",
  label: "",
  dataType: "Text",
  sourceType: "Manual",
  isRequired: false,
  description: "",
});

const dataTypeLabels: Record<TemplateVariable["dataType"], string> = {
  Text: "Văn bản ngắn",
  Textarea: "Văn bản dài",
  Number: "Số",
  Money: "Tiền tệ",
  Date: "Ngày",
  DateTime: "Ngày giờ",
  Boolean: "Đúng/Sai",
};

const hasConfiguredContent = (template: NotificationTemplate) =>
  Boolean(template.subject?.trim() && template.bodyHtml?.trim());

export const TemplateManager = () => {
  const { templates, loading, updateTemplate } = useNotificationTemplate();
  const [selectedKey, setSelectedKey] = useState<string>("");
  const [formData, setFormData] = useState<NotificationTemplate>(emptyTemplate());
  const [newVariable, setNewVariable] = useState<TemplateVariable>(emptyVariable());
  const [message, setMessage] = useState<MessageState | null>(null);

  const groupedTemplates = useMemo(() => {
    return templates.reduce<Record<string, NotificationTemplate[]>>((groups, template) => {
      const category = template.category || "Mẫu hệ thống";
      groups[category] = [...(groups[category] ?? []), template];
      return groups;
    }, {});
  }, [templates]);

  const selectedTemplate = useMemo(
    () => templates.find((template) => template.templateKey === selectedKey),
    [selectedKey, templates],
  );

  const monitoringSummary = useMemo(() => {
    const configuredCount = templates.filter(hasConfiguredContent).length;
    const reviewCount = templates.length - configuredCount;
    const categoryCount = Object.keys(groupedTemplates).length;

    return { configuredCount, reviewCount, categoryCount };
  }, [groupedTemplates, templates]);

  const selectedTemplateReady = hasConfiguredContent(formData);
  const preview = useMemo(() => buildNotificationPreview(formData), [formData]);

  useEffect(() => {
    if (!selectedKey && templates.length > 0) {
      setSelectedKey(templates[0].templateKey);
      return;
    }

    if (selectedTemplate) {
      setFormData(selectedTemplate);
      setNewVariable(emptyVariable());
      setMessage(null);
      return;
    }

    setFormData(emptyTemplate(selectedKey));
    setNewVariable(emptyVariable());
  }, [selectedKey, selectedTemplate, templates]);

  const handleInputChange = (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = event.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleVariableChange = (
    event: ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>,
  ) => {
    const { name, value } = event.target;
    setNewVariable((prev) => ({
      ...prev,
      [name]:
        name === "code"
          ? value.trim().toLowerCase().replace(/[^a-z0-9_]/g, "")
          : value,
    }));
  };

  const addCustomVariable = () => {
    const code = newVariable.code.trim().toLowerCase();
    if (!/^[a-z][a-z0-9_]{1,49}$/.test(code)) {
      setMessage({
        type: "error",
        text: "Mã biến phải dùng chữ thường, số, gạch dưới và bắt đầu bằng chữ.",
      });
      return;
    }

    const placeholder = `{${code}}`;
    if (
      formData.systemPlaceholders.some((item) => item.toLowerCase() === placeholder) ||
      formData.customVariables.some((item) => item.code.toLowerCase() === code)
    ) {
      setMessage({ type: "error", text: "Biến này đã tồn tại trong mẫu." });
      return;
    }

    const variable: TemplateVariable = {
      ...newVariable,
      code,
      label: newVariable.label.trim() || code,
      sourceType: "Manual",
      placeholder,
      description: newVariable.description?.trim() || null,
    };

    setFormData((prev) => ({
      ...prev,
      customVariables: [...prev.customVariables, variable],
      allowedPlaceholders: [...prev.allowedPlaceholders, placeholder],
    }));
    setNewVariable(emptyVariable());
    setMessage(null);
  };

  const removeCustomVariable = (code: string) => {
    const placeholder = `{${code}}`.toLowerCase();
    setFormData((prev) => ({
      ...prev,
      customVariables: prev.customVariables.filter((item) => item.code !== code),
      allowedPlaceholders: prev.allowedPlaceholders.filter(
        (item) => item.toLowerCase() !== placeholder,
      ),
    }));
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
        title="Mẫu thông báo"
        description="Cập nhật nội dung email và thông báo dùng cho các sự kiện nghiệp vụ."
        breadcrumb={[
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

      <div className="grid gap-4 md:grid-cols-3">
        <Card className="min-h-[124px]">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="text-sm font-medium text-[var(--hicas-text-secondary)]">
                Mẫu đang áp dụng
              </p>
              <p className="mt-2 text-3xl font-bold text-[var(--hicas-text-main)]">
                {monitoringSummary.configuredCount}
              </p>
              <p className="mt-1 text-xs text-[var(--hicas-text-muted)]">
                Đã có tiêu đề và nội dung
              </p>
            </div>
            <CheckCircle2 size={24} className="text-[var(--hicas-success)]" />
          </div>
        </Card>

        <Card className="min-h-[124px]">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="text-sm font-medium text-[var(--hicas-text-secondary)]">
                Cần rà lại
              </p>
              <p className="mt-2 text-3xl font-bold text-[var(--hicas-text-main)]">
                {monitoringSummary.reviewCount}
              </p>
              <p className="mt-1 text-xs text-[var(--hicas-text-muted)]">
                Thiếu tiêu đề hoặc nội dung
              </p>
            </div>
            <AlertTriangle size={24} className="text-[var(--hicas-warning)]" />
          </div>
        </Card>

        <Card className="min-h-[124px]">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="text-sm font-medium text-[var(--hicas-text-secondary)]">
                Nhóm nghiệp vụ
              </p>
              <p className="mt-2 text-3xl font-bold text-[var(--hicas-text-main)]">
                {monitoringSummary.categoryCount}
              </p>
              <p className="mt-1 text-xs text-[var(--hicas-text-muted)]">
                Nhóm theo nghiệp vụ
              </p>
            </div>
            <Activity size={24} className="text-[var(--hicas-orange)]" />
          </div>
        </Card>
      </div>

      <div className="grid gap-6 xl:grid-cols-[380px_minmax(0,1fr)]">
        <Card
          title="Danh sách mẫu"
          description="Chọn mẫu cần chỉnh sửa hoặc kiểm tra trạng thái nội dung."
          actions={<Badge variant="orange">{templates.length} mẫu</Badge>}
        >
          {loading && templates.length === 0 ? (
            <div className="py-10 text-center text-sm text-[var(--hicas-text-secondary)]">
              Đang tải dữ liệu...
            </div>
          ) : (
            <div className="max-h-[680px] space-y-5 overflow-y-auto pr-1">
              {Object.entries(groupedTemplates).map(([category, items]) => (
                <div key={category} className="space-y-2">
                  <div className="text-xs font-bold uppercase text-[var(--hicas-text-muted)]">
                    {category}
                  </div>
                  {items.map((template) => (
                    <button
                      key={template.templateKey}
                      type="button"
                      onClick={() => setSelectedKey(template.templateKey)}
                      className={cn(
                        "w-full rounded-xl border px-4 py-3 text-left transition",
                        selectedKey === template.templateKey
                          ? "border-[var(--hicas-orange)] bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange-dark)]"
                          : "border-[var(--hicas-border)] bg-white hover:border-[var(--hicas-orange)] hover:bg-[var(--hicas-orange-lighter)]",
                      )}
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-sm font-semibold">{template.displayName}</p>
                          <p className="mt-1 font-mono text-xs text-[var(--hicas-text-secondary)]">
                            {template.templateKey}
                          </p>
                        </div>
                        <Badge variant={hasConfiguredContent(template) ? "success" : "warning"}>
                          {hasConfiguredContent(template) ? "Đang áp dụng" : "Cần rà"}
                        </Badge>
                      </div>
                    </button>
                  ))}
                </div>
              ))}
            </div>
          )}
        </Card>

        <Card
          title={formData.displayName || selectedKey || "Mẫu thông báo"}
          description="Soạn tiêu đề, nội dung và sử dụng các biến đã được cho phép."
          actions={<MailCheck size={20} className="text-[var(--hicas-orange)]" />}
        >
          {loading && templates.length === 0 ? (
            <div className="py-10 text-center text-sm text-[var(--hicas-text-secondary)]">
              Đang tải dữ liệu...
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-5">
              <div className="grid gap-3 md:grid-cols-3">
                <div className="rounded-xl border border-[var(--hicas-border)] bg-white p-4">
                  <p className="text-xs font-semibold uppercase text-[var(--hicas-text-muted)]">
                    Trạng thái
                  </p>
                  <div className="mt-2">
                    <Badge variant={selectedTemplateReady ? "success" : "warning"}>
                      {selectedTemplateReady ? "Đang áp dụng" : "Cần cấu hình"}
                    </Badge>
                  </div>
                </div>
                <div className="rounded-xl border border-[var(--hicas-border)] bg-white p-4">
                  <p className="text-xs font-semibold uppercase text-[var(--hicas-text-muted)]">
                    Nhóm
                  </p>
                  <p className="mt-2 text-sm font-semibold">{formData.category}</p>
                </div>
                <div className="rounded-xl border border-[var(--hicas-border)] bg-white p-4">
                  <p className="text-xs font-semibold uppercase text-[var(--hicas-text-muted)]">
                    Biến hợp lệ
                  </p>
                  <p className="mt-2 text-sm font-semibold">
                    {formData.allowedPlaceholders.length} biến
                  </p>
                </div>
              </div>

              <div className="rounded-xl border border-[var(--hicas-warning)] bg-[var(--hicas-warning-soft)] p-4 text-sm text-amber-800">
                <div className="flex flex-wrap items-center gap-2">
                  <p className="font-semibold">Biến hợp lệ</p>
                  <Badge variant="neutral">{formData.allowedPlaceholders.length} biến</Badge>
                </div>
                <div className="mt-3 flex max-h-[150px] flex-wrap gap-2 overflow-y-auto pr-1">
                  {formData.allowedPlaceholders.length > 0 ? (
                    formData.allowedPlaceholders.map((placeholder) => (
                      <span
                        key={placeholder}
                        className="rounded-lg bg-white px-2 py-1 font-mono text-xs"
                      >
                        {placeholder}
                      </span>
                    ))
                  ) : (
                    <span className="text-sm">Mẫu này chưa khai báo biến động.</span>
                  )}
                </div>
              </div>

              <div className="rounded-xl border border-[var(--hicas-border)] bg-white p-4">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <p className="text-sm font-semibold">Biến bổ sung do Admin khai báo</p>
                    <p className="mt-1 text-xs text-[var(--hicas-text-secondary)]">
                      Chỉ tạo biến nhập tay có mục đích rõ ràng và phạm vi sử dụng cụ thể.
                    </p>
                  </div>
                  <Badge variant="neutral">{formData.customVariables.length} biến</Badge>
                </div>

                <div className="mt-4 grid gap-3 md:grid-cols-[1fr_1fr_150px_auto]">
                  <label className="block">
                    <span className="mb-2 block text-xs font-semibold">Mã biến</span>
                    <input
                      name="code"
                      value={newVariable.code}
                      onChange={handleVariableChange}
                      className="hicas-input w-full"
                      placeholder="legal_basis"
                    />
                  </label>
                  <label className="block">
                    <span className="mb-2 block text-xs font-semibold">Tên hiển thị</span>
                    <input
                      name="label"
                      value={newVariable.label}
                      onChange={handleVariableChange}
                      className="hicas-input w-full"
                      placeholder="Căn cứ pháp lý"
                    />
                  </label>
                  <label className="block">
                    <span className="mb-2 block text-xs font-semibold">Kiểu dữ liệu</span>
                    <select
                      name="dataType"
                      value={newVariable.dataType}
                      onChange={handleVariableChange}
                      className="hicas-select w-full"
                    >
                      {Object.entries(dataTypeLabels).map(([value, label]) => (
                        <option key={value} value={value}>
                          {label}
                        </option>
                      ))}
                    </select>
                  </label>
                  <div className="flex items-end">
                    <Button
                      type="button"
                      variant="secondary"
                      iconLeft={<Plus size={16} />}
                      onClick={addCustomVariable}
                    >
                      Thêm
                    </Button>
                  </div>
                </div>

                <label className="mt-3 flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={newVariable.isRequired}
                    onChange={(event) =>
                      setNewVariable((prev) => ({ ...prev, isRequired: event.target.checked }))
                    }
                  />
                  Bắt buộc nhập khi dùng mẫu thủ công
                </label>

                {formData.customVariables.length > 0 && (
                  <div className="mt-4 max-h-[260px] space-y-2 overflow-y-auto pr-1">
                    {formData.customVariables.map((variable) => (
                      <div
                        key={variable.code}
                        className="flex flex-col gap-2 rounded-xl border border-[var(--hicas-border)] px-3 py-2 sm:flex-row sm:items-center sm:justify-between"
                      >
                        <div className="flex flex-wrap items-center gap-2">
                          <span className="font-semibold">{variable.label}</span>
                          <span className="rounded-lg bg-[var(--hicas-bg-soft)] px-2 py-1 font-mono text-xs">
                            {variable.placeholder || `{${variable.code}}`}
                          </span>
                          <Badge variant="info">{dataTypeLabels[variable.dataType]}</Badge>
                          {variable.isRequired && <Badge variant="warning">Bắt buộc</Badge>}
                        </div>
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          iconLeft={<Trash2 size={15} />}
                          onClick={() => removeCustomVariable(variable.code)}
                        >
                          Xóa
                        </Button>
                      </div>
                    ))}
                  </div>
                )}
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
                <span className="mb-2 block text-sm font-semibold">Nội dung thông báo *</span>
                <textarea
                  required
                  name="bodyHtml"
                  value={formData.bodyHtml}
                  onChange={handleInputChange}
                  rows={10}
                  className="hicas-textarea w-full"
                  placeholder="Nhập nội dung thông báo..."
                />
              </label>

              <NotificationTemplatePreview
                template={formData}
                preview={preview}
                ready={selectedTemplateReady}
              />

              <div className="hidden">
                <div className="mb-2 flex items-center justify-between">
                  <p className="text-sm font-semibold">Bản xem trước</p>
                  <Badge variant="neutral">Trong hệ thống / Email</Badge>
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

              <Button type="submit" iconLeft={<Save size={17} />} disabled={!selectedKey}>
                Lưu thay đổi
              </Button>
            </form>
          )}
        </Card>
      </div>

      <Card
        title="Theo dõi chi tiết"
        description="Theo dõi mẫu đã sẵn sàng dùng và mẫu cần bổ sung nội dung."
        actions={<ListChecks size={20} className="text-[var(--hicas-orange)]" />}
      >
        <div className="max-h-[520px] overflow-auto pr-1">
          <table className="w-full min-w-[760px] text-left text-sm">
            <thead>
              <tr className="border-b border-[var(--hicas-border)] text-xs uppercase text-[var(--hicas-text-muted)]">
                <th className="px-3 py-3">Mẫu thông báo</th>
                <th className="px-3 py-3">Nhóm</th>
                <th className="px-3 py-3">Biến</th>
                <th className="px-3 py-3">Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              {templates.map((template) => (
                <tr
                  key={template.templateKey}
                  className="border-b border-[var(--hicas-border-soft)]"
                >
                  <td className="px-3 py-3">
                    <div className="font-semibold">{template.displayName}</div>
                    <div className="font-mono text-xs text-[var(--hicas-text-muted)]">
                      {template.templateKey}
                    </div>
                  </td>
                  <td className="px-3 py-3">{template.category}</td>
                  <td className="px-3 py-3">{template.allowedPlaceholders.length}</td>
                  <td className="px-3 py-3">
                    <Badge variant={hasConfiguredContent(template) ? "success" : "warning"}>
                      {hasConfiguredContent(template) ? "Sẵn sàng" : "Cần cấu hình"}
                    </Badge>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
};

type NotificationPreviewModel = {
  subject: string;
  bodyHtml: string;
  recipient: string;
};

const NotificationTemplatePreview = ({
  template,
  preview,
  ready,
}: {
  template: NotificationTemplate;
  preview: NotificationPreviewModel;
  ready: boolean;
}) => (
  <div className="rounded-xl border border-[var(--hicas-border)] bg-[var(--hicas-bg-soft)] p-4">
    <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
      <div>
        <p className="text-sm font-semibold">Bản xem trước</p>
        <p className="mt-1 text-xs font-medium text-[var(--hicas-text-secondary)]">
          Hiển thị theo giao diện người dùng nhận được trong hệ thống và qua email.
        </p>
      </div>
      <div className="flex flex-wrap gap-2">
        <Badge variant="orange">Trong hệ thống</Badge>
        <Badge variant="neutral">Email</Badge>
      </div>
    </div>

    <div className="grid gap-4 xl:grid-cols-[minmax(0,0.9fr)_minmax(0,1.1fr)]">
      <div className="overflow-hidden rounded-2xl border border-[var(--hicas-border)] bg-white shadow-sm">
        <div className="flex items-start justify-between gap-3 border-b border-[var(--hicas-border-soft)] px-4 py-3">
          <div className="flex items-center gap-3">
            <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange)]">
              <Bell size={19} />
            </span>
            <div>
              <p className="text-sm font-bold text-[var(--hicas-text-main)]">Trung tâm thông báo</p>
              <p className="text-xs font-semibold text-[var(--hicas-text-muted)]">HICAS HRM</p>
            </div>
          </div>
          <Badge variant="success">Mới</Badge>
        </div>

        <div className="p-4">
          <div className="rounded-2xl border border-[var(--hicas-orange-soft)] bg-[linear-gradient(180deg,#fff7ed_0%,#ffffff_100%)] p-4">
            <div className="flex items-start gap-3">
              <span className="mt-1 h-2.5 w-2.5 shrink-0 rounded-full bg-[var(--hicas-orange)]" />
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <Badge variant={ready ? "success" : "warning"}>
                    {ready ? "Sẵn sàng gửi" : "Đang soạn"}
                  </Badge>
                  <span className="inline-flex items-center gap-1 text-xs font-semibold text-[var(--hicas-text-muted)]">
                    <Clock3 size={13} />
                    Vừa xong
                  </span>
                </div>
                <p className="mt-3 text-base font-bold leading-6 text-[var(--hicas-text-main)]">
                  {preview.subject}
                </p>
                <div
                  className="mt-2 text-sm font-medium leading-6 text-[var(--hicas-text-secondary)] [&_a]:font-semibold [&_a]:text-[var(--hicas-orange-dark)] [&_p]:mb-2 [&_strong]:text-[var(--hicas-text-main)] [&_ul]:ml-5 [&_ul]:list-disc"
                  dangerouslySetInnerHTML={{ __html: preview.bodyHtml }}
                />
              </div>
            </div>
          </div>

          <div className="mt-3 flex flex-wrap gap-2 text-xs font-semibold text-[var(--hicas-text-secondary)]">
            <span className="rounded-full border border-[var(--hicas-border)] bg-white px-3 py-1">
              {template.category || "Mẫu hệ thống"}
            </span>
            <span className="rounded-full border border-[var(--hicas-border)] bg-white px-3 py-1">
              {template.templateKey || "TEMPLATE_KEY"}
            </span>
          </div>
        </div>
      </div>

      <div className="overflow-hidden rounded-2xl border border-[var(--hicas-border)] bg-white shadow-sm">
        <div className="border-b border-[var(--hicas-border-soft)] bg-[var(--hicas-bg-soft)] px-4 py-3">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="flex items-center gap-2 text-sm font-bold text-[var(--hicas-text-main)]">
              <MailCheck size={18} className="text-[var(--hicas-orange)]" />
              Email preview
            </div>
            <Badge variant="neutral">no-reply@hicas.vn</Badge>
          </div>
        </div>

        <div className="space-y-3 px-4 py-4">
          <div className="rounded-xl border border-[var(--hicas-border)] bg-white px-3 py-2">
            <p className="text-xs font-semibold uppercase text-[var(--hicas-text-muted)]">Tiêu đề</p>
            <p className="mt-1 text-sm font-bold text-[var(--hicas-text-main)]">{preview.subject}</p>
          </div>

          <div className="grid gap-2 sm:grid-cols-2">
            <div className="rounded-xl border border-[var(--hicas-border)] bg-white px-3 py-2">
              <p className="text-xs font-semibold uppercase text-[var(--hicas-text-muted)]">Người gửi</p>
              <p className="mt-1 truncate text-sm font-semibold text-[var(--hicas-text-main)]">
                HICAS HRM
              </p>
            </div>
            <div className="rounded-xl border border-[var(--hicas-border)] bg-white px-3 py-2">
              <p className="text-xs font-semibold uppercase text-[var(--hicas-text-muted)]">Người nhận</p>
              <p className="mt-1 truncate text-sm font-semibold text-[var(--hicas-text-main)]">
                {preview.recipient}
              </p>
            </div>
          </div>

          <div className="rounded-2xl border border-[var(--hicas-border)] bg-white p-5">
            <div className="border-b border-[var(--hicas-border-soft)] pb-4">
              <p className="text-lg font-bold text-[var(--hicas-text-main)]">HICAS</p>
              <p className="mt-1 text-sm font-medium text-[var(--hicas-text-secondary)]">
                Nền tảng quản trị nhân sự
              </p>
            </div>
            <div
              className="mt-4 text-sm leading-7 text-[var(--hicas-text-secondary)] [&_a]:font-semibold [&_a]:text-[var(--hicas-orange-dark)] [&_p]:mb-3 [&_strong]:text-[var(--hicas-text-main)] [&_ul]:ml-5 [&_ul]:list-disc"
              dangerouslySetInnerHTML={{ __html: preview.bodyHtml }}
            />
            <div className="mt-5 rounded-xl bg-[var(--hicas-bg-soft)] px-4 py-3 text-xs font-semibold text-[var(--hicas-text-secondary)]">
              Đây là email tự động từ hệ thống HICAS HRM.
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
);

const buildNotificationPreview = (template: NotificationTemplate): NotificationPreviewModel => {
  const sampleValues = buildSampleValues(template);
  const subject = replaceTemplatePlaceholders(
    template.subject || "Tiêu đề thông báo",
    sampleValues,
  );
  const bodySource = template.bodyHtml || "Nội dung thông báo sẽ hiển thị tại đây.";
  const replacedBody = replaceTemplatePlaceholders(bodySource, sampleValues);
  const bodyHtml = looksLikeHtml(replacedBody)
    ? replacedBody
    : escapeHtml(replacedBody).replace(/\n/g, "<br />");

  return {
    subject,
    bodyHtml,
    recipient:
      sampleValues.get("email") ||
      sampleValues.get("employee_email") ||
      sampleValues.get("candidate_email") ||
      "nguyenvana@hicas.vn",
  };
};

const buildSampleValues = (template: NotificationTemplate) => {
  const values = new Map<string, string>();
  const placeholders = [
    ...template.allowedPlaceholders,
    ...template.systemPlaceholders,
    ...template.customVariables.map((variable) => variable.placeholder || `{${variable.code}}`),
  ];

  placeholders.forEach((placeholder) => {
    const code = placeholder.replace(/[{}]/g, "").trim();
    if (code) values.set(code, sampleValueForCode(code));
  });

  return values;
};

const replaceTemplatePlaceholders = (value: string, samples: Map<string, string>) =>
  value.replace(/\{([a-zA-Z0-9_]+)\}/g, (match, code: string) =>
    samples.get(code) ?? sampleValueForCode(code) ?? match,
  );

const sampleValueForCode = (code: string) => {
  const normalized = code.toLowerCase();
  if (normalized.includes("email")) return "nguyenvana@hicas.vn";
  if (normalized.includes("employee") && normalized.includes("name")) return "Nguyễn Văn A";
  if (normalized.includes("candidate") && normalized.includes("name")) return "Trần Minh Anh";
  if (normalized === "name" || normalized.endsWith("_name")) return "Nguyễn Văn A";
  if (normalized.includes("manager")) return "Lê Quang Minh";
  if (normalized.includes("department")) return "Phòng Công nghệ";
  if (normalized.includes("position")) return "Chuyên viên Nhân sự";
  if (normalized.includes("tracking") || normalized.includes("code")) return "HICAS-2026-001";
  if (normalized.includes("date")) return "19/06/2026";
  if (normalized.includes("time")) return "09:30";
  if (normalized.includes("amount") || normalized.includes("salary") || normalized.includes("money")) {
    return "2.000.000 VND";
  }
  if (normalized.includes("reason")) return "Bổ sung hồ sơ theo yêu cầu hệ thống";
  if (normalized.includes("status")) return "Đã được phê duyệt";
  if (normalized.includes("url") || normalized.includes("link")) return "https://hrm.hicas.vn/approvals";
  return "Dữ liệu mẫu";
};

const looksLikeHtml = (value: string) => /<\/?[a-z][\s\S]*>/i.test(value);

const escapeHtml = (value: string) =>
  value
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
