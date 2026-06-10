import type { ChangeEvent, FormEvent } from "react";
import { useEffect, useMemo, useState } from "react";
import { Download, FileText, RefreshCw, ShieldCheck } from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card } from "../../../components/ui";
import { cn } from "../../../components/ui/classNames";
import { documentTemplateApi } from "../../system/api/documentTemplateApi";
import type {
  DocumentFormGenerateResult,
  DocumentFormPreparedField,
  DocumentFormPrepareResult,
  DocumentFormTemplateSummary,
} from "../../system/types/documentTemplate";

type MessageState = {
  type: "success" | "error";
  text: string;
};

const extractErrorMessage = (error: unknown) =>
  (error as { message?: string })?.message ||
  (error as { response?: { data?: { message?: string } } })?.response?.data?.message ||
  "Lỗi hệ thống";

const buildWordDocument = (innerHtml: string) => `
<!doctype html>
<html xmlns:o="urn:schemas-microsoft-com:office:office"
      xmlns:w="urn:schemas-microsoft-com:office:word"
      xmlns="http://www.w3.org/TR/REC-html40">
<head>
  <meta charset="utf-8" />
  <title>Biểu mẫu nhân sự</title>
  <!--[if Word]>
  <xml>
    <w:WordDocument>
      <w:View>Print</w:View>
      <w:Zoom>100</w:Zoom>
      <w:DoNotOptimizeForBrowser/>
    </w:WordDocument>
  </xml>
  <![endif]-->
</head>
<body>${innerHtml}</body>
</html>`;

const downloadDoc = (fileName: string, html: string) => {
  const blob = new Blob(["\ufeff", buildWordDocument(html)], {
    type: "application/msword;charset=utf-8",
  });
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName.endsWith(".doc") ? fileName : `${fileName}.doc`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
};

const isManualField = (field: DocumentFormPreparedField) => field.bindingType === "Manual";

export const DocumentFormsPage = () => {
  const [templates, setTemplates] = useState<DocumentFormTemplateSummary[]>([]);
  const [selectedKey, setSelectedKey] = useState("");
  const [prepared, setPrepared] = useState<DocumentFormPrepareResult | null>(null);
  const [manualValues, setManualValues] = useState<Record<string, string>>({});
  const [generated, setGenerated] = useState<DocumentFormGenerateResult | null>(null);
  const [message, setMessage] = useState<MessageState | null>(null);
  const [loading, setLoading] = useState(false);
  const [generating, setGenerating] = useState(false);

  const selectedTemplate = useMemo(
    () => templates.find((template) => template.templateKey === selectedKey),
    [selectedKey, templates],
  );

  const groupedTemplates = useMemo(
    () =>
      templates.reduce<Record<string, DocumentFormTemplateSummary[]>>((groups, template) => {
        const category = template.category || "Biểu mẫu";
        groups[category] = [...(groups[category] ?? []), template];
        return groups;
      }, {}),
    [templates],
  );

  const manualFields = useMemo(
    () => prepared?.fields.filter(isManualField) ?? [],
    [prepared],
  );

  const readonlyFields = useMemo(
    () => prepared?.fields.filter((field) => !isManualField(field)) ?? [],
    [prepared],
  );

  useEffect(() => {
    void loadTemplates();
  }, []);

  useEffect(() => {
    if (!selectedKey && templates.length > 0) {
      setSelectedKey(templates[0].templateKey);
    }
  }, [selectedKey, templates]);

  useEffect(() => {
    if (selectedKey) void prepareTemplate(selectedKey);
  }, [selectedKey]);

  const loadTemplates = async () => {
    setLoading(true);
    try {
      const data = await documentTemplateApi.getAvailableForms();
      setTemplates(data);
      if (data.length > 0) setSelectedKey(data[0].templateKey);
    } catch (error) {
      setMessage({ type: "error", text: extractErrorMessage(error) });
    } finally {
      setLoading(false);
    }
  };

  const prepareTemplate = async (templateKey: string) => {
    setLoading(true);
    try {
      const data = await documentTemplateApi.prepareForm(templateKey);
      setPrepared(data);
      setManualValues(
        data.fields
          .filter(isManualField)
          .reduce<Record<string, string>>((acc, field) => {
            acc[field.code] = field.value || "";
            return acc;
          }, {}),
      );
      setGenerated(null);
      setMessage(null);
    } catch (error) {
      setPrepared(null);
      setMessage({ type: "error", text: extractErrorMessage(error) });
    } finally {
      setLoading(false);
    }
  };

  const handleManualChange = (
    field: DocumentFormPreparedField,
    event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>,
  ) => {
    setManualValues((prev) => ({ ...prev, [field.code]: event.target.value }));
    setGenerated(null);
  };

  const validateManualValues = () => {
    const missing = manualFields.find((field) => field.required && !manualValues[field.code]?.trim());
    return missing ? `Vui lòng nhập ${missing.label.toLowerCase()}.` : "";
  };

  const handleGenerate = async (event: FormEvent) => {
    event.preventDefault();
    if (!selectedKey) return;

    const validationError = validateManualValues();
    if (validationError) {
      setMessage({ type: "error", text: validationError });
      return;
    }

    setGenerating(true);
    try {
      const data = await documentTemplateApi.generateForm(selectedKey, manualValues);
      setGenerated(data);
      setMessage({ type: "success", text: "Đã tạo biểu mẫu." });
    } catch (error) {
      setMessage({ type: "error", text: extractErrorMessage(error) });
    } finally {
      setGenerating(false);
    }
  };

  const handleDownload = () => {
    if (!generated?.content) return;
    downloadDoc(generated.fileName.replace(/\.html$/i, ".doc"), generated.content);
  };

  const previewHtml = generated?.content || prepared?.previewHtml || "";

  return (
    <div className="space-y-6">
      <PageHeader
        title="Xuất biểu mẫu"
        description="Chọn mẫu, kiểm tra dữ liệu có sẵn và nhập thông tin cần bổ sung."
        breadcrumb={[{ label: "Biểu mẫu" }, { label: "Xuất biểu mẫu" }]}
        actions={<Badge variant="orange">{templates.length} mẫu khả dụng</Badge>}
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

      <section className="grid gap-6 xl:grid-cols-[360px_minmax(0,1fr)]">
        <div className="space-y-6">
          <Card
            title="Loại biểu mẫu"
            description="Chọn mẫu phù hợp với nghiệp vụ cần xuất."
            actions={<FileText size={20} className="text-[var(--hicas-orange)]" />}
          >
            {loading && templates.length === 0 ? (
              <div className="py-8 text-center text-sm text-[var(--hicas-text-secondary)]">
                Đang tải dữ liệu...
              </div>
            ) : (
              <div className="space-y-5">
                {Object.entries(groupedTemplates).map(([category, items]) => (
                  <div key={category} className="space-y-2">
                    <p className="text-xs font-bold uppercase text-[var(--hicas-text-muted)]">
                      {category}
                    </p>
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
                        <div className="font-semibold">{template.displayName}</div>
                        {template.documentTitle ? (
                          <div className="mt-1 text-xs text-[var(--hicas-text-secondary)]">
                            {template.documentTitle}
                          </div>
                        ) : null}
                      </button>
                    ))}
                  </div>
                ))}
              </div>
            )}
          </Card>

          {prepared?.warnings.length ? (
            <Card title="Cảnh báo">
              <div className="space-y-2">
                {prepared.warnings.map((warning) => (
                  <Badge key={warning} variant="warning">{warning}</Badge>
                ))}
              </div>
            </Card>
          ) : null}
        </div>

        <div className="space-y-6">
          <Card
            title={selectedTemplate?.displayName || prepared?.displayName || "Biểu mẫu"}
            description="Kiểm tra dữ liệu có sẵn và nhập thông tin còn thiếu."
            actions={<ShieldCheck size={20} className="text-[var(--hicas-orange)]" />}
          >
            {!prepared ? (
              <div className="py-10 text-center text-sm text-[var(--hicas-text-secondary)]">
                Chọn một mẫu để bắt đầu.
              </div>
            ) : (
              <form onSubmit={handleGenerate} className="space-y-5">
                {readonlyFields.length > 0 && (
                  <div className="rounded-xl border border-[var(--hicas-border)] p-4">
                    <div className="mb-3 flex items-center justify-between">
                      <p className="text-sm font-semibold">Dữ liệu tự lấy từ hệ thống</p>
                      <Badge variant="info">{readonlyFields.length} trường</Badge>
                    </div>
                    <div className="grid gap-4 md:grid-cols-2">
                      {readonlyFields.map((field) => (
                        <label key={field.code} className="block">
                          <span className="mb-2 block text-sm font-semibold">{field.label}</span>
                          <input value={field.value || ""} readOnly className="hicas-input w-full bg-slate-50" />
                        </label>
                      ))}
                    </div>
                  </div>
                )}

                <div className="rounded-xl border border-[var(--hicas-border)] p-4">
                  <div className="mb-3 flex items-center justify-between">
                    <p className="text-sm font-semibold">Thông tin cần nhập</p>
                    <Badge variant="orange">{manualFields.length} trường</Badge>
                  </div>
                  {manualFields.length === 0 ? (
                    <p className="text-sm text-[var(--hicas-text-secondary)]">
                      Mẫu này không yêu cầu nhập thêm dữ liệu.
                    </p>
                  ) : (
                    <div className="grid gap-4 md:grid-cols-2">
                      {manualFields.map((field) => (
                        <label
                          key={field.code}
                          className={cn("block", field.dataType === "Textarea" && "md:col-span-2")}
                        >
                          <span className="mb-2 block text-sm font-semibold">
                            {field.label}
                            {field.required ? " *" : ""}
                          </span>
                          {field.dataType === "Textarea" ? (
                            <textarea
                              value={manualValues[field.code] || ""}
                              onChange={(event) => handleManualChange(field, event)}
                              required={field.required}
                              rows={4}
                              className="hicas-textarea w-full"
                            />
                          ) : field.dataType === "Select" ? (
                            <select
                              value={manualValues[field.code] || ""}
                              onChange={(event) => handleManualChange(field, event)}
                              required={field.required}
                              className="hicas-select w-full"
                            >
                              <option value="">Chọn</option>
                              {field.options.map((option) => (
                                <option key={option} value={option}>{option}</option>
                              ))}
                            </select>
                          ) : (
                            <input
                              type={
                                field.dataType === "Date"
                                  ? "date"
                                  : field.dataType === "Time"
                                    ? "time"
                                    : field.dataType === "Number" || field.dataType === "Money"
                                      ? "number"
                                      : "text"
                              }
                              value={manualValues[field.code] || ""}
                              onChange={(event) => handleManualChange(field, event)}
                              required={field.required}
                              className="hicas-input w-full"
                            />
                          )}
                        </label>
                      ))}
                    </div>
                  )}
                </div>

                <div className="flex flex-wrap gap-3">
                  <Button type="submit" iconLeft={<RefreshCw size={17} />} disabled={generating}>
                    {generating ? "Đang tạo..." : "Tạo biểu mẫu"}
                  </Button>
                  <Button
                    type="button"
                    variant="secondary"
                    iconLeft={<Download size={17} />}
                    onClick={handleDownload}
                    disabled={!generated?.content}
                  >
                    Tải DOC
                  </Button>
                </div>
              </form>
            )}
          </Card>

          <Card title="Bản xem trước" description="Bản xem trước dùng dữ liệu vừa nhập.">
            <div className="overflow-auto rounded-xl border border-[var(--hicas-border)] bg-slate-100 p-4">
              {previewHtml ? (
                <iframe
                  title="Bản xem trước biểu mẫu"
                  srcDoc={previewHtml}
                  className="h-[760px] w-full rounded-lg bg-white"
                />
              ) : (
                <div className="py-16 text-center text-sm text-[var(--hicas-text-secondary)]">
                  Chưa có bản xem trước.
                </div>
              )}
            </div>
          </Card>
        </div>
      </section>
    </div>
  );
};
