import type { ChangeEvent, FormEvent } from "react";
import { useEffect, useMemo, useState } from "react";
import { Eye, FileText, Plus, RefreshCw, Save, Trash2 } from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card } from "../../../components/ui";
import { cn } from "../../../components/ui/classNames";
import {
  documentTemplateApi,
  emptyDocumentTemplate,
  emptyTemplateField,
} from "../api/documentTemplateApi";
import type {
  DocumentFieldCatalog,
  DocumentTemplateConfig,
  DocumentTemplateField,
  DocumentTemplatePreviewResult,
} from "../types/documentTemplate";

type MessageState = {
  type: "success" | "error";
  text: string;
};

const dataTypeLabels: Record<string, string> = {
  Text: "Văn bản ngắn",
  Textarea: "Văn bản dài",
  Number: "Số",
  Money: "Tiền",
  Date: "Ngày",
  DateTime: "Ngày giờ",
  Boolean: "Đúng/Sai",
  Select: "Lựa chọn",
  Time: "Giờ",
};

const bindingLabels: Record<string, string> = {
  System: "Tự lấy từ hệ thống",
  Manual: "Người dùng nhập",
  Computed: "Hệ thống tính",
};

const resolverOptions = [
  { value: "System.Today", label: "Ngày hiện tại" },
  { value: "Document.Number", label: "Số văn bản" },
  { value: "Leave.TotalDays", label: "Số ngày nghỉ" },
];

const roleOptions = ["Employee", "Manager", "HR", "Admin", "Director"];
const dataScopes = ["SELF", "TEAM", "ALL", "RECORD"];

const roleLabels: Record<string, string> = {
  Employee: "Nhân viên",
  Manager: "Quản lý",
  HR: "HR",
  Admin: "Quản trị",
  Director: "Giám đốc",
};

const dataScopeLabels: Record<string, string> = {
  SELF: "Cá nhân",
  TEAM: "Đội nhóm",
  ALL: "Toàn công ty",
  RECORD: "Theo hồ sơ",
};

const extractErrorMessage = (error: unknown) =>
  (error as { message?: string })?.message ||
  (error as { response?: { data?: { message?: string } } })?.response?.data?.message ||
  "Lỗi hệ thống";

export const DocumentTemplateManager = () => {
  const [templates, setTemplates] = useState<DocumentTemplateConfig[]>([]);
  const [catalogs, setCatalogs] = useState<DocumentFieldCatalog[]>([]);
  const [selectedKey, setSelectedKey] = useState("");
  const [form, setForm] = useState<DocumentTemplateConfig>(emptyDocumentTemplate());
  const [newField, setNewField] = useState<DocumentTemplateField>(emptyTemplateField());
  const [selectedCatalogPath, setSelectedCatalogPath] = useState("");
  const [preview, setPreview] = useState<DocumentTemplatePreviewResult | null>(null);
  const [message, setMessage] = useState<MessageState | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [previewLoading, setPreviewLoading] = useState(false);

  const selectedTemplate = useMemo(
    () => templates.find((template) => template.templateKey === selectedKey),
    [selectedKey, templates],
  );

  const groupedTemplates = useMemo(
    () =>
      templates.reduce<Record<string, DocumentTemplateConfig[]>>((groups, template) => {
        const key = template.category || "Biểu mẫu";
        groups[key] = [...(groups[key] ?? []), template];
        return groups;
      }, {}),
    [templates],
  );

  const activeCatalogs = useMemo(
    () => catalogs.filter((catalog) => catalog.isActive),
    [catalogs],
  );

  useEffect(() => {
    void loadData();
  }, []);

  useEffect(() => {
    if (!selectedKey && templates.length > 0) {
      setSelectedKey(templates[0].templateKey);
      return;
    }

    if (selectedTemplate) {
      setForm(selectedTemplate);
      setMessage(null);
    }
  }, [selectedKey, selectedTemplate, templates]);

  const renderPreview = async (template: DocumentTemplateConfig) => {
    if (!template.templateKey) return;

    setPreviewLoading(true);
    try {
      const nextPreview = await documentTemplateApi.previewTemplate(template, {}, "Sample");
      setPreview(nextPreview);
    } catch (error) {
      setPreview({
        html: "",
        resolvedValues: {},
        missingFields: [],
        invalidPlaceholders: [],
        warnings: [extractErrorMessage(error)],
      });
    } finally {
      setPreviewLoading(false);
    }
  };

  useEffect(() => {
    const handle = window.setTimeout(() => {
      void renderPreview(form);
    }, 450);

    return () => window.clearTimeout(handle);
  }, [form]);

  const loadData = async () => {
    setLoading(true);
    try {
      const [templateData, catalogData] = await Promise.all([
        documentTemplateApi.getTemplates(),
        documentTemplateApi.getFieldCatalogs(),
      ]);
      setTemplates(templateData);
      setCatalogs(catalogData);
      if (templateData.length > 0) setSelectedKey(templateData[0].templateKey);
    } catch (error) {
      setMessage({ type: "error", text: extractErrorMessage(error) });
    } finally {
      setLoading(false);
    }
  };

  const updateForm = <K extends keyof DocumentTemplateConfig>(
    key: K,
    value: DocumentTemplateConfig[K],
  ) => {
    setForm((prev) => ({ ...prev, [key]: value }));
  };

  const handleTextChange = (
    event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>,
  ) => {
    const { name, value } = event.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const toggleRole = (role: string) => {
    setForm((prev) => ({
      ...prev,
      allowedRoles: prev.allowedRoles.some((item) => item.toLowerCase() === role.toLowerCase())
        ? prev.allowedRoles.filter((item) => item.toLowerCase() !== role.toLowerCase())
        : [...prev.allowedRoles, role],
    }));
  };

  const addSystemField = () => {
    const catalog = activeCatalogs.find((item) => item.sourcePath === selectedCatalogPath);
    if (!catalog) return;

    if (form.fields.some((field) => field.code === catalog.code)) {
      setMessage({ type: "error", text: "Trường dữ liệu này đã có trong mẫu." });
      return;
    }

    setForm((prev) => ({
      ...prev,
      fields: [
        ...prev.fields,
        {
          ...emptyTemplateField(),
          code: catalog.code,
          label: catalog.label,
          bindingType: "System",
          sourcePath: catalog.sourcePath,
          dataType: catalog.dataType,
          required: false,
          sortOrder: prev.fields.length + 1,
        },
      ],
    }));
    setSelectedCatalogPath("");
    setMessage(null);
  };

  const addCustomField = () => {
    const code = newField.code.trim().toLowerCase().replace(/[^a-z0-9_]/g, "");
    if (!/^[a-z][a-z0-9_]{1,79}$/.test(code)) {
      setMessage({ type: "error", text: "Mã trường phải bắt đầu bằng chữ và chỉ dùng chữ thường, số, gạch dưới." });
      return;
    }

    if (form.fields.some((field) => field.code === code)) {
      setMessage({ type: "error", text: "Trường này đã tồn tại trong mẫu." });
      return;
    }

    setForm((prev) => ({
      ...prev,
      fields: [
        ...prev.fields,
        {
          ...newField,
          code,
          label: newField.label.trim() || code,
          sortOrder: prev.fields.length + 1,
          isActive: true,
        },
      ],
    }));
    setNewField(emptyTemplateField());
    setMessage(null);
  };

  const removeField = (code: string) => {
    setForm((prev) => ({
      ...prev,
      fields: prev.fields.filter((field) => field.code !== code),
    }));
  };

  const updateField = (
    code: string,
    key: keyof DocumentTemplateField,
    value: DocumentTemplateField[keyof DocumentTemplateField],
  ) => {
    setForm((prev) => ({
      ...prev,
      fields: prev.fields.map((field) =>
        field.code === code ? { ...field, [key]: value } : field,
      ),
    }));
  };

  const handleSave = async (event: FormEvent) => {
    event.preventDefault();
    setSaving(true);
    try {
      const res = await documentTemplateApi.saveTemplate(form);
      setMessage({ type: "success", text: res.message || "Đã lưu cấu hình biểu mẫu." });
      await loadData();
      setSelectedKey(res.data.templateKey);
    } catch (error) {
      setMessage({ type: "error", text: extractErrorMessage(error) });
    } finally {
      setSaving(false);
    }
  };

  const createNewTemplate = () => {
    const key = `CUSTOM_${Date.now()}`;
    const next = emptyDocumentTemplate(key);
    setSelectedKey(key);
    setForm(next);
    setMessage(null);
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Cấu hình biểu mẫu"
        description="Thiết lập mẫu biểu và trường dữ liệu cần dùng."
        breadcrumb={[
          { label: "Cấu hình hệ thống" },
          { label: "Biểu mẫu" },
        ]}
        actions={
          <Button type="button" variant="secondary" iconLeft={<Plus size={17} />} onClick={createNewTemplate}>
            Mẫu mới
          </Button>
        }
      />

      {message && (
        <div
          className={cn(
            "rounded-xl border px-4 py-3 text-sm font-medium",
            message.type === "error"
              ? "border-[var(--hicas-danger)] bg-[var(--hicas-danger-soft)] text-[var(--hicas-danger)]"
              : "border-[var(--hicas-success)] bg-[var(--hicas-success-soft)] text-[var(--hicas-success)]",
          )}
        >
          {message.text}
        </div>
      )}

      <section className="space-y-6">
        <Card
          title="Bản xem trước"
          description={
            previewLoading
              ? "Đang cập nhật bản xem trước..."
              : "Xem bố cục biểu mẫu trên vùng hiển thị rộng."
          }
          actions={
            <Button
              type="button"
              size="sm"
              variant="secondary"
              iconLeft={
                previewLoading ? (
                  <RefreshCw size={16} className="animate-spin" />
                ) : (
                  <Eye size={16} />
                )
              }
              onClick={() => void renderPreview(form)}
              disabled={previewLoading}
            >
              Cập nhật
            </Button>
          }
        >
          <div className="mb-4 flex flex-wrap gap-2">
            {preview?.invalidPlaceholders.map((item) => (
              <Badge key={item} variant="danger">
                Sai: {item}
              </Badge>
            ))}
            {preview?.missingFields.map((item) => (
              <Badge key={item} variant="warning">
                Thiếu: {item}
              </Badge>
            ))}
            {preview?.warnings.map((item) => (
              <Badge key={item} variant="neutral">
                {item}
              </Badge>
            ))}
            {preview &&
              !preview.invalidPlaceholders.length &&
              !preview.missingFields.length &&
              !preview.warnings.length && <Badge variant="success">Bản xem trước hợp lệ</Badge>}
          </div>
          <div className="overflow-auto rounded-xl border border-[var(--hicas-border)] bg-slate-100 p-4">
            {preview?.html ? (
              <iframe
                title="Bản xem trước biểu mẫu"
                srcDoc={preview.html}
                className="h-[calc(100vh-250px)] min-h-[760px] w-full rounded-lg bg-white shadow-sm"
              />
            ) : (
              <div className="py-20 text-center text-sm text-[var(--hicas-text-secondary)]">
                Chưa có bản xem trước.
              </div>
            )}
          </div>
        </Card>

        <div className="grid items-start gap-6 xl:grid-cols-[320px_minmax(0,1fr)]">
          <Card
            title="Danh sách mẫu"
            description="Chọn mẫu cần chỉnh sửa hoặc tạo mẫu mới."
            actions={<Badge variant="orange">{templates.length} mẫu</Badge>}
          >
            {loading ? (
              <div className="py-8 text-center text-sm text-[var(--hicas-text-secondary)]">Đang tải dữ liệu...</div>
            ) : (
              <div className="max-h-[720px] space-y-5 overflow-y-auto pr-1">
                {Object.entries(groupedTemplates).map(([category, items]) => (
                  <div key={category} className="space-y-2">
                    <p className="text-xs font-bold uppercase text-[var(--hicas-text-muted)]">{category}</p>
                    {items.map((template) => (
                      <button
                        key={template.templateKey}
                        type="button"
                        onClick={() => setSelectedKey(template.templateKey)}
                        className={cn(
                          "w-full rounded-xl border px-4 py-3 text-left transition",
                          selectedKey === template.templateKey
                            ? "border-[var(--hicas-orange)] bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange-dark)]"
                            : "border-[var(--hicas-border)] bg-white hover:border-[var(--hicas-orange)]",
                        )}
                      >
                        <div className="flex items-start justify-between gap-2">
                          <div>
                            <p className="text-sm font-semibold">{template.displayName}</p>
                            <p className="mt-1 font-mono text-xs text-[var(--hicas-text-secondary)]">
                              {template.templateKey}
                            </p>
                          </div>
                          <Badge variant={template.status === "Active" ? "success" : "neutral"}>
                            {template.status === "Active" ? "Đang áp dụng" : "Tạm tắt"}
                          </Badge>
                        </div>
                      </button>
                    ))}
                  </div>
                ))}
              </div>
            )}
          </Card>

          <Card title={form.displayName || "Biểu mẫu"} actions={<FileText size={20} className="text-[var(--hicas-orange)]" />}>
            <form onSubmit={handleSave} className="space-y-5">
            <div className="grid gap-4 md:grid-cols-2">
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Mã mẫu *</span>
                <input
                  name="templateKey"
                  value={form.templateKey}
                  onChange={(event) => updateForm("templateKey", event.target.value.toUpperCase().replace(/[^A-Z0-9_]/g, ""))}
                  className="hicas-input w-full font-mono"
                  required
                />
              </label>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Tên hiển thị *</span>
                <input name="displayName" value={form.displayName} onChange={handleTextChange} className="hicas-input w-full" required />
              </label>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Nhóm</span>
                <input name="category" value={form.category} onChange={handleTextChange} className="hicas-input w-full" />
              </label>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Trạng thái</span>
                <select name="status" value={form.status} onChange={handleTextChange} className="hicas-select w-full">
                  <option value="Active">Đang áp dụng</option>
                  <option value="Inactive">Tạm tắt</option>
                </select>
              </label>
              <label className="block md:col-span-2">
                <span className="mb-2 block text-sm font-semibold">Tiêu đề văn bản</span>
                <input name="documentTitle" value={form.documentTitle} onChange={handleTextChange} className="hicas-input w-full" />
              </label>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Tiền tố số</span>
                <input name="numberPrefix" value={form.numberPrefix} onChange={handleTextChange} className="hicas-input w-full uppercase" />
              </label>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Phạm vi dữ liệu</span>
                <select name="dataScope" value={form.dataScope} onChange={handleTextChange} className="hicas-select w-full">
                  {dataScopes.map((scope) => (
                    <option key={scope} value={scope}>{dataScopeLabels[scope] || scope}</option>
                  ))}
                </select>
              </label>
            </div>

            <div className="rounded-xl border border-[var(--hicas-border)] p-4">
              <div className="mb-3 flex items-center justify-between gap-3">
                <p className="text-sm font-semibold">Vai trò được dùng mẫu</p>
                <Badge variant="neutral">{form.allowedRoles.length} vai trò</Badge>
              </div>
              <div className="flex flex-wrap gap-2">
                {roleOptions.map((role) => {
                  const active = form.allowedRoles.some((item) => item.toLowerCase() === role.toLowerCase());
                  return (
                    <button
                      key={role}
                      type="button"
                      onClick={() => toggleRole(role)}
                      className={cn(
                        "rounded-lg border px-3 py-1.5 text-sm font-medium",
                        active
                          ? "border-[var(--hicas-orange)] bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange-dark)]"
                          : "border-[var(--hicas-border)] bg-white text-[var(--hicas-text-secondary)]",
                      )}
                    >
                      {roleLabels[role] || role}
                    </button>
                  );
                })}
              </div>
            </div>

            <div className="rounded-xl border border-[var(--hicas-border)] p-4">
              <div className="mb-4 flex items-center justify-between">
                <p className="text-sm font-semibold">Trường dữ liệu hệ thống</p>
                <Badge variant="info">{activeCatalogs.length} trường</Badge>
              </div>
              <div className="grid gap-3 md:grid-cols-[1fr_auto]">
                <select value={selectedCatalogPath} onChange={(event) => setSelectedCatalogPath(event.target.value)} className="hicas-select w-full">
                  <option value="">Chọn trường dữ liệu</option>
                  {activeCatalogs.map((catalog) => (
                    <option key={catalog.sourcePath} value={catalog.sourcePath}>
                      {catalog.label}
                    </option>
                  ))}
                </select>
                <Button type="button" variant="secondary" iconLeft={<Plus size={16} />} onClick={addSystemField}>
                  Thêm
                </Button>
              </div>
            </div>

            <div className="rounded-xl border border-[var(--hicas-border)] p-4">
              <div className="mb-4 flex items-center justify-between">
                <p className="text-sm font-semibold">Trường nhập tay và trường tự tính</p>
                <Badge variant="neutral">{form.fields.length} trường</Badge>
              </div>
              <div className="grid gap-3 md:grid-cols-[1fr_1fr_130px_150px_auto]">
                <input
                  value={newField.code}
                  onChange={(event) => setNewField((prev) => ({ ...prev, code: event.target.value.trim().toLowerCase().replace(/[^a-z0-9_]/g, "") }))}
                  className="hicas-input w-full"
                  placeholder="ly_do"
                />
                <input
                  value={newField.label}
                  onChange={(event) => setNewField((prev) => ({ ...prev, label: event.target.value }))}
                  className="hicas-input w-full"
                  placeholder="Lý do"
                />
                <select
                  value={newField.bindingType}
                  onChange={(event) => setNewField((prev) => ({ ...prev, bindingType: event.target.value as DocumentTemplateField["bindingType"] }))}
                  className="hicas-select w-full"
                >
                  <option value="Manual">Nhập tay</option>
                  <option value="Computed">Tự tính</option>
                </select>
                {newField.bindingType === "Computed" ? (
                  <select
                    value={newField.resolverKey || ""}
                    onChange={(event) => setNewField((prev) => ({ ...prev, resolverKey: event.target.value }))}
                    className="hicas-select w-full"
                  >
                    <option value="">Cách tính</option>
                    {resolverOptions.map((item) => (
                      <option key={item.value} value={item.value}>{item.label}</option>
                    ))}
                  </select>
                ) : (
                  <select
                    value={newField.dataType}
                    onChange={(event) => setNewField((prev) => ({ ...prev, dataType: event.target.value as DocumentTemplateField["dataType"] }))}
                    className="hicas-select w-full"
                  >
                    {Object.entries(dataTypeLabels).map(([value, label]) => (
                      <option key={value} value={value}>{label}</option>
                    ))}
                  </select>
                )}
                <Button type="button" variant="secondary" iconLeft={<Plus size={16} />} onClick={addCustomField}>
                  Thêm
                </Button>
              </div>
              <label className="mt-3 flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={newField.required}
                  onChange={(event) => setNewField((prev) => ({ ...prev, required: event.target.checked }))}
                />
                Bắt buộc nhập
              </label>
            </div>

            {form.fields.length > 0 && (
              <div className="max-h-[320px] space-y-2 overflow-y-auto pr-1">
                {form.fields.map((field) => (
                  <div key={field.code} className="rounded-xl border border-[var(--hicas-border)] bg-white px-3 py-3">
                    <div className="flex flex-wrap items-center justify-between gap-3">
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="font-semibold">{field.label}</span>
                        <span className="rounded-lg bg-[var(--hicas-bg-soft)] px-2 py-1 font-mono text-xs">{`{${field.code}}`}</span>
                        <Badge variant={field.bindingType === "System" ? "info" : field.bindingType === "Computed" ? "warning" : "neutral"}>
                          {bindingLabels[field.bindingType]}
                        </Badge>
                        <Badge variant="neutral">{dataTypeLabels[field.dataType] || field.dataType}</Badge>
                        {field.required && <Badge variant="warning">Bắt buộc</Badge>}
                      </div>
                      <div className="flex items-center gap-2">
                        <label className="flex items-center gap-1 text-xs">
                          <input
                            type="checkbox"
                            checked={field.required}
                            disabled={field.bindingType !== "Manual"}
                            onChange={(event) => updateField(field.code, "required", event.target.checked)}
                          />
                          Bắt buộc
                        </label>
                        <Button type="button" size="sm" variant="ghost" iconLeft={<Trash2 size={15} />} onClick={() => removeField(field.code)}>
                          Xóa
                        </Button>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}

            <div className="space-y-4">
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Phần đầu văn bản</span>
                <textarea name="headerHtml" value={form.headerHtml} onChange={handleTextChange} rows={4} className="hicas-textarea w-full font-mono text-xs" />
              </label>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Nội dung văn bản *</span>
                <textarea name="bodyHtml" value={form.bodyHtml} onChange={handleTextChange} rows={9} className="hicas-textarea w-full font-mono text-xs" required />
              </label>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Phần cuối văn bản</span>
                <textarea name="footerHtml" value={form.footerHtml} onChange={handleTextChange} rows={4} className="hicas-textarea w-full font-mono text-xs" />
              </label>
            </div>

            <Button type="submit" iconLeft={<Save size={17} />} disabled={saving}>
              {saving ? "Đang lưu..." : "Lưu cấu hình"}
            </Button>
            </form>
          </Card>
        </div>

      </section>
    </div>
  );
};
