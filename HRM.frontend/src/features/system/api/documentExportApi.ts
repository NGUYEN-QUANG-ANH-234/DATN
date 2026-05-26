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
  getTemplates: async (): Promise<{ success: boolean; data: DocumentTemplateSummary[] }> => {
    return await axiosClient.get(`${ENDPOINT}/templates`);
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
};
