import axiosClient from "../../../core/api/axiosClient";

export type DocumentTemplateSummary = {
  templateKey: string;
  documentType: string;
  displayName: string;
  activeLayoutVersion: string;
  allowedOutputs: string[];
  layoutVersions: Array<{
    version: string;
    name: string;
    isActive: boolean;
  }>;
};

const ENDPOINT = "/document-exports";

function buildFileName(templateKey: string, referenceId: number, layoutVersion?: string) {
  const suffix = layoutVersion ? `_${layoutVersion}` : "";
  return `${templateKey}_${referenceId}${suffix}.html`;
}

function downloadBlob(blob: Blob, fileName: string) {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}

export const documentExportApi = {
  getTemplates: async (): Promise<DocumentTemplateSummary[]> => {
    const res = (await axiosClient.get(`${ENDPOINT}/templates`)) as unknown;
    if (Array.isArray(res)) return normalizeTemplates(res);
    if (
      res &&
      typeof res === "object" &&
      Array.isArray((res as { data?: unknown }).data)
    ) {
      return normalizeTemplates((res as { data: DocumentTemplateSummary[] }).data);
    }
    if (
      res &&
      typeof res === "object" &&
      Array.isArray((res as { Data?: unknown }).Data)
    ) {
      return normalizeTemplates((res as { Data: DocumentTemplateSummary[] }).Data);
    }

    return [];
  },

  exportHtml: async (templateKey: string, referenceId: number, layoutVersion?: string) => {
    const blob = (await axiosClient.get(`${ENDPOINT}/${templateKey}/${referenceId}`, {
      params: layoutVersion ? { layoutVersion } : undefined,
      responseType: "blob",
    })) as Blob;

    downloadBlob(blob, buildFileName(templateKey, referenceId, layoutVersion));
  },

  exportContract: async (contractId: number, layoutVersion?: string) => {
    return documentExportApi.exportHtml("EXPORT_CONTRACT", contractId, layoutVersion);
  },

  exportContractAddendum: async (addendumId: number, layoutVersion?: string) => {
    return documentExportApi.exportHtml("EXPORT_CONTRACT_ADDENDUM", addendumId, layoutVersion);
  },

  exportLeaveRequest: async (requestId: number, layoutVersion?: string) => {
    return documentExportApi.exportHtml("EXPORT_LEAVE_REQUEST", requestId, layoutVersion);
  },

  exportOvertimeRequest: async (requestId: number, layoutVersion?: string) => {
    return documentExportApi.exportHtml("EXPORT_OVERTIME_REQUEST", requestId, layoutVersion);
  },

  exportProfileUpdateRequest: async (requestId: number, layoutVersion?: string) => {
    return documentExportApi.exportHtml("EXPORT_PROFILE_UPDATE_REQUEST", requestId, layoutVersion);
  },

  exportOnboardingProfile: async (requestId: number, layoutVersion?: string) => {
    return documentExportApi.exportHtml("EXPORT_ONBOARDING_PROFILE", requestId, layoutVersion);
  },

  exportRecruitmentRequest: async (requestId: number, layoutVersion?: string) => {
    return documentExportApi.exportHtml("EXPORT_RECRUITMENT_REQUEST", requestId, layoutVersion);
  },

  exportKpiReview: async (reviewId: number, layoutVersion?: string) => {
    return documentExportApi.exportHtml("EXPORT_KPI_REVIEW", reviewId, layoutVersion);
  },

  exportPayslip: async (payrollId: number, layoutVersion?: string) => {
    return documentExportApi.exportHtml("EXPORT_PAYSLIP", payrollId, layoutVersion);
  },

  exportPersonnelChangeDecision: async (requestId: number, layoutVersion?: string) => {
    return documentExportApi.exportHtml("EXPORT_PERSONNEL_CHANGE_DECISION", requestId, layoutVersion);
  },
};

const normalizeTemplates = (items: DocumentTemplateSummary[]): DocumentTemplateSummary[] =>
  items.map((item) => {
    const raw = item as DocumentTemplateSummary & {
      TemplateKey?: string;
      DocumentType?: string;
      DisplayName?: string;
      ActiveLayoutVersion?: string;
      AllowedOutputs?: string[];
      LayoutVersions?: DocumentTemplateSummary["layoutVersions"];
    };

    return {
      templateKey: item.templateKey || raw.TemplateKey || "",
      documentType: item.documentType || raw.DocumentType || "",
      displayName: item.displayName || raw.DisplayName || item.templateKey || raw.TemplateKey || "",
      activeLayoutVersion: item.activeLayoutVersion || raw.ActiveLayoutVersion || "",
      allowedOutputs: item.allowedOutputs || raw.AllowedOutputs || [],
      layoutVersions: item.layoutVersions || raw.LayoutVersions || [],
    };
  });
