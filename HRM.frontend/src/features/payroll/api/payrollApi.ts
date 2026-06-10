import axiosClient from "../../../core/api/axiosClient";
import type {
  ApiResponse,
  CancelProjectBonusImportRequest,
  CreatePayrollAdjustmentRequest,
  PayrollAdjustment,
  PayrollCalculationResult,
  PayrollPreflight,
  ProjectBonusImportBatch,
  ProjectBonusImportPreview,
  ReviewProjectBonusImportRequest,
  SalarySlip,
} from "../types/payroll";

export const payrollApi = {
  preflight: (month: number, year: number) =>
    axiosClient.get<ApiResponse<PayrollPreflight>, ApiResponse<PayrollPreflight>>(
      "/payroll/preflight",
      { params: { month, year } },
    ),

  calculate: (month: number, year: number) =>
    axiosClient.post<
      ApiResponse<PayrollCalculationResult>,
      ApiResponse<PayrollCalculationResult>
    >("/payroll/calculate", { month, year }),

  getSalarySlips: (period: string) =>
    axiosClient.get<ApiResponse<SalarySlip[]>, ApiResponse<SalarySlip[]>>(
      "/salary-slips",
      { params: { period } },
    ),

  getSalarySlipDetail: (id: number) =>
    axiosClient.get<ApiResponse<SalarySlip>, ApiResponse<SalarySlip>>(
      `/salary-slips/${id}`,
    ),

  exportSalarySlips: async (slipIds: number[]): Promise<Blob> => {
    return await axiosClient.post(
      "/salary-slips/export",
      { slipIds, format: "CSV" },
      { responseType: "blob" },
    );
  },

  getAdjustments: (month: number, year: number) =>
    axiosClient.get<ApiResponse<PayrollAdjustment[]>, ApiResponse<PayrollAdjustment[]>>(
      "/payroll/adjustments",
      { params: { month, year } },
    ),

  createAdjustment: (payload: CreatePayrollAdjustmentRequest) =>
    axiosClient.post<ApiResponse<PayrollAdjustment>, ApiResponse<PayrollAdjustment>>(
      "/payroll/adjustments",
      payload,
    ),

  getPendingProjectBonusImports: () =>
    axiosClient.get<ApiResponse<ProjectBonusImportBatch[]>, ApiResponse<ProjectBonusImportBatch[]>>(
      "/payroll/project-bonus-imports/pending-director",
    ),

  getProjectBonusImports: (month: number, year: number) =>
    axiosClient.get<ApiResponse<ProjectBonusImportBatch[]>, ApiResponse<ProjectBonusImportBatch[]>>(
      "/payroll/project-bonus-imports",
      { params: { month, year } },
    ),

  getProjectBonusImportDetail: (id: number) =>
    axiosClient.get<ApiResponse<ProjectBonusImportBatch>, ApiResponse<ProjectBonusImportBatch>>(
      `/payroll/project-bonus-imports/${id}`,
    ),

  previewProjectBonusImport: (payload: FormData) =>
    axiosClient.post<ApiResponse<ProjectBonusImportPreview>, ApiResponse<ProjectBonusImportPreview>>(
      "/payroll/project-bonus-imports/preview",
      payload,
      { headers: { "Content-Type": "multipart/form-data" } },
    ),

  importProjectBonus: (payload: FormData) =>
    axiosClient.post<ApiResponse<ProjectBonusImportBatch>, ApiResponse<ProjectBonusImportBatch>>(
      "/payroll/project-bonus-imports",
      payload,
      { headers: { "Content-Type": "multipart/form-data" } },
    ),

  submitProjectBonusImport: (id: number) =>
    axiosClient.patch<ApiResponse<ProjectBonusImportBatch>, ApiResponse<ProjectBonusImportBatch>>(
      `/payroll/project-bonus-imports/${id}/submit`,
    ),

  reviewProjectBonusImport: (id: number, payload: ReviewProjectBonusImportRequest) =>
    axiosClient.patch<ApiResponse<ProjectBonusImportBatch>, ApiResponse<ProjectBonusImportBatch>>(
      `/payroll/project-bonus-imports/${id}/director-review`,
      payload,
    ),

  cancelProjectBonusImport: (id: number, payload: CancelProjectBonusImportRequest) =>
    axiosClient.patch<ApiResponse<ProjectBonusImportBatch>, ApiResponse<ProjectBonusImportBatch>>(
      `/payroll/project-bonus-imports/${id}/cancel`,
      payload,
    ),
};
