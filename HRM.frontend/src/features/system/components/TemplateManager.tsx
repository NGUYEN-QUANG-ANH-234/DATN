import type { ChangeEvent, FormEvent } from "react";
import { useEffect, useMemo, useState } from "react";
import {
  Activity,
  AlertTriangle,
  CheckCircle2,
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
                <div className="mt-3 flex flex-wrap gap-2">
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
                  <div className="mt-4 space-y-2">
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

              <div className="rounded-xl border border-[var(--hicas-border)] bg-[var(--hicas-bg-soft)] p-4">
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
        <div className="overflow-x-auto">
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
