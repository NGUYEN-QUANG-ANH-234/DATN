import axiosClient from "../../../core/api/axiosClient";
import type {
  ApiResponse,
  CreatePayrollAdjustmentRequest,
  PayrollAdjustment,
  PayrollCalculationResult,
  SalarySlip,
} from "../types/payroll";

export const payrollApi = {
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
};
