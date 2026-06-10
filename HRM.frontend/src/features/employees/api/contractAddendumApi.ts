import axiosClient from "../../../core/api/axiosClient";

export interface ContractAddendumDto {
  id: number;
  contractId: number;
  contractNumber: string;
  addendumNumber: string;
  addendumType: string;
  baseContractNumberSnapshot?: string | null;
  baseContractStartDateSnapshot?: string | null;
  baseContractEndDateSnapshot?: string | null;
  newBasicSalary?: number | null;
  newInsuranceSalary?: number | null;
  newEndDate?: string | null;
  otherChangesJson?: string | null;
  content?: string | null;
  changedContentSummary?: string | null;
  unchangedTerms?: string | null;
  legalDocumentNumber?: string | null;
  documentTemplateCode?: string | null;
  documentDocFilePath?: string | null;
  documentPdfFilePath?: string | null;
  issuedAt?: string | null;
  employeeSignedAt?: string | null;
  employerSignedAt?: string | null;
  effectiveDate: string;
  status: string;
  rejectReason?: string | null;
  createdAt: string;
  employeeId?: number | null;
  employeeName?: string | null;
}

export interface CreateContractAddendumPayload {
  addendumType?: string;
  newBasicSalary?: number;
  newInsuranceSalary?: number;
  newEndDate?: string;
  otherChangesJson?: string;
  content?: string;
  changedContentSummary?: string;
  unchangedTerms?: string;
  effectiveDate: string;
}

export interface ContractDocumentPreviewDto {
  referenceId: number;
  referenceType: string;
  templateCode: string;
  documentNumber: string;
  fileName: string;
  html: string;
  docFilePath?: string | null;
  pdfFilePath?: string | null;
  canDownloadPdf: boolean;
}

export interface IssueContractDocumentPayload {
  legalDocumentNumber?: string;
  documentTemplateCode?: string;
  issuedAt?: string;
  employeeSignedAt?: string;
  employerSignedAt?: string;
}

export interface ReviewContractAddendumPayload {
  isApproved: boolean;
  rejectReason?: string | null;
}

export interface RequestRevisionPayload {
  reason: string;
}

export const contractAddendumApi = {
  createDraft: async (contractId: number, payload: CreateContractAddendumPayload) => {
    return await axiosClient.post(`/contracts/${contractId}/addendums`, payload);
  },

  updateDraft: async (id: number, payload: CreateContractAddendumPayload) => {
    return await axiosClient.patch(`/addendums/${id}`, payload);
  },

  getByContract: async (contractId: number): Promise<{ success: boolean; data: ContractAddendumDto[] }> => {
    return await axiosClient.get(`/contracts/${contractId}/addendums`);
  },

  getAll: async (): Promise<{ success: boolean; data: ContractAddendumDto[] }> => {
    return await axiosClient.get("/addendums");
  },

  previewDocument: async (id: number): Promise<{ success: boolean; data: ContractDocumentPreviewDto }> => {
    return await axiosClient.get(`/addendums/${id}/document-preview`);
  },

  downloadDocumentDoc: async (id: number): Promise<Blob> => {
    return await axiosClient.get(`/addendums/${id}/document-doc`, { responseType: "blob" });
  },

  downloadDocumentPdf: async (id: number): Promise<Blob> => {
    return await axiosClient.get(`/addendums/${id}/document-pdf`, { responseType: "blob" });
  },

  issueDocument: async (id: number, payload: IssueContractDocumentPayload): Promise<{ success: boolean; data: ContractDocumentPreviewDto }> => {
    return await axiosClient.patch(`/addendums/${id}/issue-document`, payload);
  },

  getPendingDirector: async (): Promise<{ success: boolean; data: ContractAddendumDto[] }> => {
    return await axiosClient.get("/addendums/pending-director");
  },

  getPendingDept: async (): Promise<{ success: boolean; data: ContractAddendumDto[] }> => {
    return await axiosClient.get("/addendums/pending-dept");
  },

  getPendingHr: async (): Promise<{ success: boolean; data: ContractAddendumDto[] }> => {
    return await axiosClient.get("/addendums/pending-hr");
  },

  getMyPendingConfirmation: async (): Promise<{ success: boolean; data: ContractAddendumDto[] }> => {
    return await axiosClient.get("/addendums/my-pending-confirmation");
  },

  submit: async (id: number) => {
    return await axiosClient.patch(`/addendums/${id}/submit`);
  },

  approve: async (id: number) => {
    return await axiosClient.patch(`/addendums/${id}/approve`);
  },

  deptReview: async (id: number, payload: ReviewContractAddendumPayload) => {
    return await axiosClient.patch(`/addendums/${id}/dept-review`, payload);
  },

  hrConfirm: async (id: number, payload: ReviewContractAddendumPayload) => {
    return await axiosClient.patch(`/addendums/${id}/hr-confirm`, payload);
  },

  employeeConfirm: async (id: number, payload: ReviewContractAddendumPayload) => {
    return await axiosClient.patch(`/addendums/${id}/employee-confirm`, payload);
  },

  reject: async (id: number, rejectReason: string) => {
    return await axiosClient.patch(`/addendums/${id}/reject`, { rejectReason });
  },

  requestRevision: async (id: number, payload: RequestRevisionPayload) => {
    return await axiosClient.patch(`/addendums/${id}/request-revision`, payload);
  },
};
