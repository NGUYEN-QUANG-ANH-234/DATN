import axiosClient from "../../../core/api/axiosClient";
import type {
  ApiResponse,
  CancelExternalTimesheetImportRequest,
  CancelProjectBonusImportRequest,
  CreatePayrollAdjustmentRequest,
  ExternalTimesheetImportBatch,
  ExternalTimesheetImportPreview,
  PayrollAdjustment,
  PayrollCalculationResult,
  PayrollFormula,
  PayrollFormulaReviewRequest,
  PayrollFormulaStatus,
  PayrollFormulaValidationResult,
  PayrollFormulaVariable,
  PayrollPreflight,
  PayrollRunReviewRequest,
  PayrollRunSummary,
  ProjectBonusImportBatch,
  ProjectBonusImportPreview,
  ReviewProjectBonusImportRequest,
  ReviewExternalTimesheetImportRequest,
  SalarySlip,
  UpsertPayrollFormulaRequest,
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

  getPayrollRunSummary: (month: number, year: number) =>
    axiosClient.get<ApiResponse<PayrollRunSummary>, ApiResponse<PayrollRunSummary>>(
      "/payroll/runs/summary",
      { params: { month, year } },
    ),

  getPendingPayrollRuns: () =>
    axiosClient.get<ApiResponse<PayrollRunSummary[]>, ApiResponse<PayrollRunSummary[]>>(
      "/payroll/runs/pending-approval",
    ),

  submitPayrollRun: (month: number, year: number) =>
    axiosClient.patch<ApiResponse<PayrollRunSummary>, ApiResponse<PayrollRunSummary>>(
      "/payroll/runs/submit",
      { month, year },
    ),

  reviewPayrollRun: (payload: PayrollRunReviewRequest) =>
    axiosClient.patch<ApiResponse<PayrollRunSummary>, ApiResponse<PayrollRunSummary>>(
      "/payroll/runs/director-review",
      payload,
    ),

  lockPayrollRun: (month: number, year: number) =>
    axiosClient.patch<ApiResponse<PayrollRunSummary>, ApiResponse<PayrollRunSummary>>(
      "/payroll/runs/lock",
      { month, year },
    ),

  getSalarySlips: (period: string) =>
    axiosClient.get<ApiResponse<SalarySlip[]>, ApiResponse<SalarySlip[]>>(
      "/salary-slips",
      { params: { period } },
    ),

  getMySalarySlips: (period: string) =>
    axiosClient.get<ApiResponse<SalarySlip[]>, ApiResponse<SalarySlip[]>>(
      "/salary-slips/my",
      { params: { period } },
    ),

  getSalarySlipDetail: (id: number) =>
    axiosClient.get<ApiResponse<SalarySlip>, ApiResponse<SalarySlip>>(
      `/salary-slips/${id}`,
    ),

  getMySalarySlipDetail: (id: number) =>
    axiosClient.get<ApiResponse<SalarySlip>, ApiResponse<SalarySlip>>(
      `/salary-slips/my/${id}`,
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

  getPayrollFormulas: (status?: PayrollFormulaStatus | string) =>
    axiosClient.get<ApiResponse<PayrollFormula[]>, ApiResponse<PayrollFormula[]>>(
      "/payroll/formulas",
      { params: status ? { status } : undefined },
    ),

  getPayrollFormulaDetail: (id: number) =>
    axiosClient.get<ApiResponse<PayrollFormula>, ApiResponse<PayrollFormula>>(
      `/payroll/formulas/${id}`,
    ),

  getPayrollFormulaVariables: () =>
    axiosClient.get<ApiResponse<PayrollFormulaVariable[]>, ApiResponse<PayrollFormulaVariable[]>>(
      "/payroll/formulas/variables",
    ),

  validatePayrollFormula: (payload: UpsertPayrollFormulaRequest) =>
    axiosClient.post<
      ApiResponse<PayrollFormulaValidationResult>,
      ApiResponse<PayrollFormulaValidationResult>
    >("/payroll/formulas/validate", payload),

  createPayrollFormula: (payload: UpsertPayrollFormulaRequest) =>
    axiosClient.post<ApiResponse<PayrollFormula>, ApiResponse<PayrollFormula>>(
      "/payroll/formulas",
      payload,
    ),

  updatePayrollFormula: (id: number, payload: UpsertPayrollFormulaRequest) =>
    axiosClient.put<ApiResponse<PayrollFormula>, ApiResponse<PayrollFormula>>(
      `/payroll/formulas/${id}`,
      payload,
    ),

  clonePayrollFormula: (id: number) =>
    axiosClient.post<ApiResponse<PayrollFormula>, ApiResponse<PayrollFormula>>(
      `/payroll/formulas/${id}/clone`,
    ),

  submitPayrollFormula: (id: number) =>
    axiosClient.patch<ApiResponse<PayrollFormula>, ApiResponse<PayrollFormula>>(
      `/payroll/formulas/${id}/submit`,
    ),

  reviewPayrollFormula: (id: number, payload: PayrollFormulaReviewRequest) =>
    axiosClient.patch<ApiResponse<PayrollFormula>, ApiResponse<PayrollFormula>>(
      `/payroll/formulas/${id}/director-review`,
      payload,
    ),

  activatePayrollFormula: (id: number) =>
    axiosClient.patch<ApiResponse<PayrollFormula>, ApiResponse<PayrollFormula>>(
      `/payroll/formulas/${id}/activate`,
    ),

  archivePayrollFormula: (id: number, note?: string) =>
    axiosClient.patch<ApiResponse<PayrollFormula>, ApiResponse<PayrollFormula>>(
      `/payroll/formulas/${id}/archive`,
      { note },
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

  getPendingExternalTimesheetImports: () =>
    axiosClient.get<ApiResponse<ExternalTimesheetImportBatch[]>, ApiResponse<ExternalTimesheetImportBatch[]>>(
      "/payroll/external-timesheet-imports/pending-director",
    ),

  getExternalTimesheetImports: (month: number, year: number) =>
    axiosClient.get<ApiResponse<ExternalTimesheetImportBatch[]>, ApiResponse<ExternalTimesheetImportBatch[]>>(
      "/payroll/external-timesheet-imports",
      { params: { month, year } },
    ),

  getExternalTimesheetImportDetail: (id: number) =>
    axiosClient.get<ApiResponse<ExternalTimesheetImportBatch>, ApiResponse<ExternalTimesheetImportBatch>>(
      `/payroll/external-timesheet-imports/${id}`,
    ),

  previewExternalTimesheetImport: (payload: FormData) =>
    axiosClient.post<ApiResponse<ExternalTimesheetImportPreview>, ApiResponse<ExternalTimesheetImportPreview>>(
      "/payroll/external-timesheet-imports/preview",
      payload,
      { headers: { "Content-Type": "multipart/form-data" } },
    ),

  importExternalTimesheet: (payload: FormData) =>
    axiosClient.post<ApiResponse<ExternalTimesheetImportBatch>, ApiResponse<ExternalTimesheetImportBatch>>(
      "/payroll/external-timesheet-imports",
      payload,
      { headers: { "Content-Type": "multipart/form-data" } },
    ),

  submitExternalTimesheetImport: (id: number) =>
    axiosClient.patch<ApiResponse<ExternalTimesheetImportBatch>, ApiResponse<ExternalTimesheetImportBatch>>(
      `/payroll/external-timesheet-imports/${id}/submit`,
    ),

  reviewExternalTimesheetImport: (id: number, payload: ReviewExternalTimesheetImportRequest) =>
    axiosClient.patch<ApiResponse<ExternalTimesheetImportBatch>, ApiResponse<ExternalTimesheetImportBatch>>(
      `/payroll/external-timesheet-imports/${id}/director-review`,
      payload,
    ),

  cancelExternalTimesheetImport: (id: number, payload: CancelExternalTimesheetImportRequest) =>
    axiosClient.patch<ApiResponse<ExternalTimesheetImportBatch>, ApiResponse<ExternalTimesheetImportBatch>>(
      `/payroll/external-timesheet-imports/${id}/cancel`,
      payload,
    ),
};
