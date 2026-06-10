import axiosClient from "../../../core/api/axiosClient";

export interface ContractDto {
  id: number;
  contractNumber: string;
  contractType: string;
  basicSalary: number;
  salaryPercentage: number;
  insuranceSalary: number;
  startDate: string | null;
  endDate: string | null;
  status: string;
  version: number;
  negotiationNote: string | null;
  employeeId?: number;
  employeeName?: string;
  legalDocumentType?: string | null;
  employerLegalName?: string | null;
  employerTaxCode?: string | null;
  employerAddress?: string | null;
  employerRepresentativeName?: string | null;
  employerRepresentativeTitle?: string | null;
  employerRepresentativeAuthorization?: string | null;
  signingLocation?: string | null;
  employeeFullNameSnapshot?: string | null;
  employeeBirthDateSnapshot?: string | null;
  employeeGenderSnapshot?: string | null;
  employeeIdentityNumberSnapshot?: string | null;
  employeeIdentityIssueDate?: string | null;
  employeeIdentityIssuePlace?: string | null;
  employeeResidenceAddressSnapshot?: string | null;
  employeeDepartmentSnapshot?: string | null;
  employeePositionSnapshot?: string | null;
  employeeJobLevelSnapshot?: string | null;
  jobTitle?: string | null;
  jobDescription?: string | null;
  workLocation?: string | null;
  workingMode?: string | null;
  workingHours?: string | null;
  restTime?: string | null;
  directManagerSnapshot?: string | null;
  salaryPaymentMethod?: string | null;
  salaryPaymentDate?: string | null;
  allowanceDescription?: string | null;
  additionalBenefits?: string | null;
  salaryReviewPolicy?: string | null;
  bonusPolicy?: string | null;
  kpiBonusTargetAmount?: number | null;
  kpiBonusPolicyCode?: string | null;
  kpiBonusPolicyVersionCode?: string | null;
  kpiScoreFormula?: string | null;
  kpiPayoutFormula?: string | null;
  kpiBonusEligibilityRule?: string | null;
  kpiBonusPaymentPeriod?: string | null;
  kpiBonusApproverRole?: string | null;
  insurancePolicy?: string | null;
  laborProtectionPolicy?: string | null;
  trainingPolicy?: string | null;
  employeeObligations?: string | null;
  employerObligations?: string | null;
  confidentialityClause?: string | null;
  intellectualPropertyClause?: string | null;
  terminationClause?: string | null;
  disputeResolutionClause?: string | null;
  legalDocumentNumber?: string | null;
  documentTemplateCode?: string | null;
  documentDocFilePath?: string | null;
  documentPdfFilePath?: string | null;
  issuedAt?: string | null;
  employeeSignedAt?: string | null;
  employerSignedAt?: string | null;
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

export interface ContractRequestPayload {
  note?: string;
}

export interface ReviewContractPayload {
  isApproved: boolean;
  rejectReason?: string;
}

export interface CreateDraftPayload {
  contractType: string;
  basicSalary: number;
  salaryPercentage: number;
  insuranceSalary: number;
  startDate: string;
  endDate?: string;
  employer?: {
    legalName?: string;
    taxCode?: string;
    address?: string;
    representativeName?: string;
    representativeTitle?: string;
    representativeAuthorization?: string;
    signingLocation?: string;
  };
  employee?: {
    fullName?: string;
    birthDate?: string;
    gender?: string;
    identityNumber?: string;
    identityIssueDate?: string;
    identityIssuePlace?: string;
    residenceAddress?: string;
    department?: string;
    position?: string;
    jobLevel?: string;
  };
  work?: {
    jobTitle?: string;
    jobDescription?: string;
    workLocation?: string;
    workingMode?: string;
    workingHours?: string;
    restTime?: string;
    directManager?: string;
  };
  compensation?: {
    salaryPaymentMethod?: string;
    salaryPaymentDate?: string;
    allowanceDescription?: string;
    additionalBenefits?: string;
    salaryReviewPolicy?: string;
    bonusPolicy?: string;
    insurancePolicy?: string;
    laborProtectionPolicy?: string;
    trainingPolicy?: string;
  };
  clauses?: {
    employeeObligations?: string;
    employerObligations?: string;
    confidentialityClause?: string;
    intellectualPropertyClause?: string;
    terminationClause?: string;
    disputeResolutionClause?: string;
  };
  issuance?: {
    legalDocumentNumber?: string;
    documentTemplateCode?: string;
    issuedAt?: string;
  };
}

export interface NegotiatePayload {
  negotiationNote: string;
}

export interface RequestRevisionPayload {
  reason: string;
}

export const contractApi = {
  // 1. Nhân viên gửi yêu cầu ký kết/gia hạn
  createRequest: async (payload: ContractRequestPayload) => {
    return await axiosClient.post("/contracts/requests", payload);
  },

  // 2. Trưởng phòng duyệt/từ chối
  deptReview: async (id: number, payload: ReviewContractPayload) => {
    return await axiosClient.patch(`/contracts/requests/${id}/dept-review`, payload);
  },

  // 3. HR lập bản nháp hợp đồng
  hrCreateDraft: async (id: number, payload: CreateDraftPayload) => {
    return await axiosClient.post(`/contracts/requests/${id}/hr-draft`, payload);
  },

  // 3b. HR từ chối (không đáp ứng chính sách)
  hrReject: async (id: number, payload: ReviewContractPayload) => {
    return await axiosClient.patch(`/contracts/requests/${id}/hr-reject`, payload);
  },

  // 4. Nhân viên thương lượng
  negotiate: async (id: number, payload: NegotiatePayload) => {
    return await axiosClient.put(`/contracts/${id}/negotiate`, payload);
  },

  // 5. Nhân viên đồng ý điều khoản
  employeeAccept: async (id: number) => {
    return await axiosClient.patch(`/contracts/${id}/emp-accept`);
  },

  // 6. Giám đốc chốt phê duyệt
  directorApprove: async (id: number, payload: ReviewContractPayload) => {
    return await axiosClient.patch(`/contracts/${id}/director-approve`, payload);
  },

  requestRevision: async (id: number, payload: RequestRevisionPayload) => {
    return await axiosClient.patch(`/contracts/${id}/request-revision`, payload);
  },

  // Lấy danh sách hợp đồng (HR/Manager/Director)
  getAllContracts: async (): Promise<{ success: boolean; data: ContractDto[] }> => {
    return await axiosClient.get("/contracts");
  },

  getDraftDefaults: async (id: number): Promise<{ success: boolean; data: ContractDto }> => {
    return await axiosClient.get(`/contracts/${id}/draft-defaults`);
  },

  previewDocument: async (id: number): Promise<{ success: boolean; data: ContractDocumentPreviewDto }> => {
    return await axiosClient.get(`/contracts/${id}/document-preview`);
  },

  downloadDocumentDoc: async (id: number): Promise<Blob> => {
    return await axiosClient.get(`/contracts/${id}/document-doc`, { responseType: "blob" });
  },

  downloadDocumentPdf: async (id: number): Promise<Blob> => {
    return await axiosClient.get(`/contracts/${id}/document-pdf`, { responseType: "blob" });
  },

  issueDocument: async (id: number, payload: IssueContractDocumentPayload): Promise<{ success: boolean; data: ContractDocumentPreviewDto }> => {
    return await axiosClient.patch(`/contracts/${id}/issue-document`, payload);
  },

  // Lấy hợp đồng của nhân viên hiện tại
  getMyContracts: async (): Promise<{ success: boolean; data: ContractDto[] }> => {
    return await axiosClient.get("/contracts/my-contracts");
  },

  // Lấy danh sách yêu cầu hợp đồng chờ duyệt (dành cho Trưởng phòng)
  getPendingRequests: async (): Promise<{ success: boolean; data: ContractDto[] }> => {
    return await axiosClient.get("/contracts/pending-dept");
  },

  // Lấy danh sách yêu cầu chờ HR duyệt
  getHrPendingRequests: async (): Promise<{ success: boolean; data: ContractDto[] }> => {
    return await axiosClient.get("/contracts/pending-hr");
  },

  // Lấy danh sách chờ Giám đốc duyệt
  getDirectorPending: async (): Promise<{ success: boolean; data: ContractDto[] }> => {
    return await axiosClient.get("/contracts/pending-director");
  },
};
