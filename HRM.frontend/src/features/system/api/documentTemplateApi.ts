import axiosClient from "../../../core/api/axiosClient";
import type {
  ApiEnvelope,
  DocumentFieldCatalog,
  DocumentFormGenerateResult,
  DocumentFormPrepareResult,
  DocumentFormTemplateSummary,
  DocumentTemplateConfig,
  DocumentTemplateField,
  DocumentTemplatePreviewResult,
  DocumentTemplateValidationResult,
} from "../types/documentTemplate";

const ADMIN_ENDPOINT = "/system/document-templates";
const FORM_ENDPOINT = "/document-forms";

const unwrap = <T>(res: unknown, fallback: T): T => {
  if (res && typeof res === "object") {
    const env = res as ApiEnvelope<T>;
    if (env.data !== undefined) return env.data;
    if (env.Data !== undefined) return env.Data;
  }
  return (res as T) ?? fallback;
};

const normalizeArray = <T>(items: unknown, normalize: (item: T) => T): T[] => {
  if (!Array.isArray(items)) return [];
  return items.map((item) => normalize(item as T));
};

export const emptyDocumentTemplate = (templateKey = "NEW_TEMPLATE"): DocumentTemplateConfig => ({
  templateKey,
  displayName: "Mẫu biểu mới",
  category: "Đơn đề nghị",
  documentTitle: "MẪU BIỂU MỚI",
  status: "Active",
  numberPrefix: "DOC",
  signerTitle: "Người lập",
  dataScope: "SELF",
  allowedRoles: ["Employee", "HR", "Admin"],
  allowedOutputs: ["HTML", "DOC"],
  fields: [],
  headerHtml:
    '<header class="doc-header"><section class="doc-header-left"><div><strong>{company_name}</strong></div><div class="doc-small">{company_address}</div><div style="margin-top:18px">Số: {document_number}</div></section><section class="doc-header-right"><div>CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</div><div>Độc lập - Tự do - Hạnh phúc</div></section></header><div style="margin-top:12px;text-align:right;font-style:italic">{issued_place}, {issued_date_text}</div>',
  bodyHtml: "<p>Nội dung biểu mẫu dùng các placeholder đã khai báo.</p>",
  footerHtml:
    '<div class="doc-signatures"><div class="doc-signature"><strong>XÁC NHẬN CỦA CÔNG TY</strong><div style="margin-top:72px">{signer_name}</div></div><div class="doc-signature"><strong>{employee_name}</strong><div style="margin-top:72px">Người lập</div></div></div>',
  layout: {
    pageSize: "A4",
    orientation: "portrait",
    margin: "20mm",
    fontFamily: "Times New Roman",
    fontSize: "12pt",
  },
});

export const emptyTemplateField = (): DocumentTemplateField => ({
  code: "",
  label: "",
  bindingType: "Manual",
  sourcePath: null,
  resolverKey: null,
  dataType: "Text",
  required: false,
  defaultValue: "",
  options: [],
  sortOrder: 0,
  isActive: true,
});

const normalizeTemplate = (item: DocumentTemplateConfig): DocumentTemplateConfig => {
  const raw = item as DocumentTemplateConfig & Record<string, unknown>;
  const fields = (item.fields || (raw.Fields as DocumentTemplateField[]) || []).map(normalizeField);
  const layout = item.layout || (raw.Layout as DocumentTemplateConfig["layout"]) || emptyDocumentTemplate().layout;

  return {
    templateKey: item.templateKey || (raw.TemplateKey as string) || "",
    displayName: item.displayName || (raw.DisplayName as string) || "",
    category: item.category || (raw.Category as string) || "Biểu mẫu",
    documentTitle: item.documentTitle || (raw.DocumentTitle as string) || "",
    status: (item.status || (raw.Status as DocumentTemplateConfig["status"]) || "Active") as DocumentTemplateConfig["status"],
    numberPrefix: item.numberPrefix || (raw.NumberPrefix as string) || "DOC",
    signerTitle: item.signerTitle || (raw.SignerTitle as string) || "Người lập",
    dataScope: (item.dataScope || (raw.DataScope as DocumentTemplateConfig["dataScope"]) || "SELF") as DocumentTemplateConfig["dataScope"],
    allowedRoles: item.allowedRoles || (raw.AllowedRoles as string[]) || [],
    allowedOutputs: item.allowedOutputs || (raw.AllowedOutputs as string[]) || ["HTML", "DOC"],
    fields,
    headerHtml: item.headerHtml || (raw.HeaderHtml as string) || "",
    bodyHtml: item.bodyHtml || (raw.BodyHtml as string) || "",
    footerHtml: item.footerHtml || (raw.FooterHtml as string) || "",
    layout: {
      pageSize: layout.pageSize || (layout as unknown as Record<string, string>).PageSize || "A4",
      orientation: layout.orientation || (layout as unknown as Record<string, string>).Orientation || "portrait",
      margin: layout.margin || (layout as unknown as Record<string, string>).Margin || "20mm",
      fontFamily: layout.fontFamily || (layout as unknown as Record<string, string>).FontFamily || "Times New Roman",
      fontSize: layout.fontSize || (layout as unknown as Record<string, string>).FontSize || "12pt",
    },
  };
};

const normalizeField = (item: DocumentTemplateField): DocumentTemplateField => {
  const raw = item as DocumentTemplateField & Record<string, unknown>;
  return {
    code: item.code || (raw.Code as string) || "",
    label: item.label || (raw.Label as string) || "",
    bindingType: (item.bindingType || raw.BindingType || "Manual") as DocumentTemplateField["bindingType"],
    sourcePath: item.sourcePath ?? (raw.SourcePath as string | null) ?? null,
    resolverKey: item.resolverKey ?? (raw.ResolverKey as string | null) ?? null,
    dataType: (item.dataType || raw.DataType || "Text") as DocumentTemplateField["dataType"],
    required: item.required ?? (raw.Required as boolean) ?? false,
    defaultValue: item.defaultValue ?? (raw.DefaultValue as string | null) ?? "",
    options: item.options || (raw.Options as string[]) || [],
    sortOrder: item.sortOrder ?? (raw.SortOrder as number) ?? 0,
    isActive: item.isActive ?? (raw.IsActive as boolean) ?? true,
    placeholder: item.placeholder || (raw.Placeholder as string) || `{${item.code || raw.Code || ""}}`,
  };
};

const normalizeCatalog = (item: DocumentFieldCatalog): DocumentFieldCatalog => {
  const raw = item as DocumentFieldCatalog & Record<string, unknown>;
  return {
    code: item.code || (raw.Code as string) || "",
    label: item.label || (raw.Label as string) || "",
    sourcePath: item.sourcePath || (raw.SourcePath as string) || "",
    module: item.module || (raw.Module as string) || "",
    dataType: (item.dataType || raw.DataType || "Text") as DocumentFieldCatalog["dataType"],
    isActive: item.isActive ?? (raw.IsActive as boolean) ?? true,
  };
};

const normalizePreview = (item: DocumentTemplatePreviewResult): DocumentTemplatePreviewResult => {
  const raw = item as DocumentTemplatePreviewResult & Record<string, unknown>;
  return {
    html: item.html || (raw.Html as string) || "",
    resolvedValues: item.resolvedValues || (raw.ResolvedValues as Record<string, string>) || {},
    missingFields: item.missingFields || (raw.MissingFields as string[]) || [],
    invalidPlaceholders: item.invalidPlaceholders || (raw.InvalidPlaceholders as string[]) || [],
    warnings: item.warnings || (raw.Warnings as string[]) || [],
  };
};

export const documentTemplateApi = {
  getTemplates: async () => {
    const res = await axiosClient.get(ADMIN_ENDPOINT);
    return normalizeArray<DocumentTemplateConfig>(
      unwrap<DocumentTemplateConfig[]>(res, []),
      normalizeTemplate,
    );
  },

  getTemplate: async (templateKey: string) => {
    const res = await axiosClient.get(`${ADMIN_ENDPOINT}/${templateKey}`);
    return normalizeTemplate(unwrap<DocumentTemplateConfig>(res, emptyDocumentTemplate(templateKey)));
  },

  saveTemplate: async (template: DocumentTemplateConfig) => {
    const res = await axiosClient.put(`${ADMIN_ENDPOINT}/${template.templateKey}`, template);
    return {
      ...(res as unknown as ApiEnvelope<DocumentTemplateConfig>),
      data: normalizeTemplate(unwrap<DocumentTemplateConfig>(res, template)),
    };
  },

  validateTemplate: async (template: DocumentTemplateConfig) => {
    const res = await axiosClient.post(`${ADMIN_ENDPOINT}/${template.templateKey}/validate`, template);
    return unwrap<DocumentTemplateValidationResult>(res, {
      invalidPlaceholders: [],
      missingFields: [],
      unusedFields: [],
      warnings: [],
      isValid: true,
    });
  },

  previewTemplate: async (
    template: DocumentTemplateConfig,
    manualValues: Record<string, string>,
    previewMode = "Sample",
    employeeId?: number,
  ) => {
    const res = await axiosClient.post(`${ADMIN_ENDPOINT}/${template.templateKey}/preview`, {
      templateConfig: template,
      previewMode,
      employeeId,
      manualValues,
    });
    return normalizePreview(unwrap<DocumentTemplatePreviewResult>(res, {
      html: "",
      resolvedValues: {},
      missingFields: [],
      invalidPlaceholders: [],
      warnings: [],
    }));
  },

  getFieldCatalogs: async () => {
    const res = await axiosClient.get(`${ADMIN_ENDPOINT}/field-catalogs`);
    return normalizeArray<DocumentFieldCatalog>(
      unwrap<DocumentFieldCatalog[]>(res, []),
      normalizeCatalog,
    );
  },

  getAvailableForms: async () => {
    const res = await axiosClient.get(`${FORM_ENDPOINT}/available`);
    return unwrap<DocumentFormTemplateSummary[]>(res, []);
  },

  prepareForm: async (templateKey: string, employeeId?: number) => {
    const res = await axiosClient.get(`${FORM_ENDPOINT}/${templateKey}/prepare`, {
      params: employeeId ? { employeeId } : undefined,
    });
    return unwrap<DocumentFormPrepareResult>(res, {
      templateKey,
      displayName: templateKey,
      category: "",
      documentTitle: "",
      numberPrefix: "",
      fields: [],
      previewHtml: "",
      resolvedValues: {},
      warnings: [],
    });
  },

  generateForm: async (
    templateKey: string,
    manualValues: Record<string, string>,
    employeeId?: number,
  ) => {
    const res = await axiosClient.post(`${FORM_ENDPOINT}/${templateKey}/generate`, {
      employeeId,
      outputType: "HTML",
      manualValues,
    });
    return unwrap<DocumentFormGenerateResult>(res, {
      fileName: `${templateKey}.html`,
      contentType: "text/html; charset=utf-8",
      content: "",
      resolvedValues: {},
      warnings: [],
    });
  },
};
