export type DocumentBindingType = "System" | "Manual" | "Computed";
export type DocumentFieldDataType =
  | "Text"
  | "Textarea"
  | "Number"
  | "Money"
  | "Date"
  | "DateTime"
  | "Boolean"
  | "Select"
  | "Time";

export type DocumentTemplateLayout = {
  pageSize: string;
  orientation: string;
  margin: string;
  fontFamily: string;
  fontSize: string;
};

export type DocumentTemplateField = {
  code: string;
  label: string;
  bindingType: DocumentBindingType;
  sourcePath?: string | null;
  resolverKey?: string | null;
  dataType: DocumentFieldDataType;
  required: boolean;
  defaultValue?: string | null;
  options: string[];
  sortOrder: number;
  isActive: boolean;
  placeholder?: string;
};

export type DocumentTemplateConfig = {
  templateKey: string;
  displayName: string;
  category: string;
  documentTitle: string;
  status: "Active" | "Inactive";
  numberPrefix: string;
  signerTitle: string;
  dataScope: "SELF" | "TEAM" | "ALL" | "RECORD";
  allowedRoles: string[];
  allowedOutputs: string[];
  fields: DocumentTemplateField[];
  headerHtml: string;
  bodyHtml: string;
  footerHtml: string;
  layout: DocumentTemplateLayout;
};

export type DocumentFieldCatalog = {
  code: string;
  label: string;
  sourcePath: string;
  module: string;
  dataType: DocumentFieldDataType;
  isActive: boolean;
};

export type DocumentTemplateValidationResult = {
  invalidPlaceholders: string[];
  missingFields: string[];
  unusedFields: string[];
  warnings: string[];
  isValid: boolean;
};

export type DocumentTemplatePreviewResult = {
  html: string;
  resolvedValues: Record<string, string>;
  missingFields: string[];
  invalidPlaceholders: string[];
  warnings: string[];
};

export type DocumentFormTemplateSummary = {
  templateKey: string;
  displayName: string;
  category: string;
  documentTitle: string;
  numberPrefix: string;
  dataScope: string;
};

export type DocumentFormPreparedField = {
  code: string;
  label: string;
  bindingType: DocumentBindingType;
  dataType: DocumentFieldDataType;
  required: boolean;
  value: string;
  readOnly: boolean;
  options: string[];
  sortOrder: number;
};

export type DocumentFormPrepareResult = {
  templateKey: string;
  displayName: string;
  category: string;
  documentTitle: string;
  numberPrefix: string;
  fields: DocumentFormPreparedField[];
  previewHtml: string;
  resolvedValues: Record<string, string>;
  warnings: string[];
};

export type DocumentFormGenerateResult = {
  fileName: string;
  contentType: string;
  content: string;
  resolvedValues: Record<string, string>;
  warnings: string[];
};

export type ApiEnvelope<T> = {
  success: boolean;
  message?: string;
  data?: T;
  Data?: T;
};
