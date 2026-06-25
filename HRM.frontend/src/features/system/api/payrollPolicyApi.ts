import axiosClient from "../../../core/api/axiosClient";
import type {
  OvertimeRateConfig,
  OvertimeRateConfigPayload,
  PayrollPolicy,
  PayrollPolicyPayload,
  PayrollPolicyResponse,
  PayrollPolicyType,
} from "../types/payrollPolicy";

const ENDPOINT = "/system/payroll-policies";

export const payrollPolicyApi = {
  getAll: async (
    policyType?: PayrollPolicyType | "",
    includeInactive = true,
  ): Promise<PayrollPolicyResponse<PayrollPolicy[]>> => {
    const params: Record<string, string | number | boolean> = {
      includeInactive,
    };
    if (policyType !== "" && policyType !== undefined) {
      params.policyType = policyType;
    }
    return await axiosClient.get(ENDPOINT, { params });
  },

  create: async (
    payload: PayrollPolicyPayload,
  ): Promise<PayrollPolicyResponse<PayrollPolicy>> => {
    return await axiosClient.post(ENDPOINT, payload);
  },

  update: async (
    id: number,
    payload: PayrollPolicyPayload,
  ): Promise<PayrollPolicyResponse<PayrollPolicy>> => {
    return await axiosClient.put(`${ENDPOINT}/${id}`, payload);
  },

  setStatus: async (
    id: number,
    isActive: boolean,
  ): Promise<PayrollPolicyResponse<null>> => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/status`, null, {
      params: { isActive },
    });
  },

  delete: async (id: number): Promise<PayrollPolicyResponse<null>> => {
    return await axiosClient.delete(`${ENDPOINT}/${id}`);
  },

  getOvertimeRates: async (
    includeInactive = true,
  ): Promise<PayrollPolicyResponse<OvertimeRateConfig[]>> => {
    return await axiosClient.get(`${ENDPOINT}/overtime-rates`, {
      params: { includeInactive },
    });
  },

  createOvertimeRate: async (
    payload: OvertimeRateConfigPayload,
  ): Promise<PayrollPolicyResponse<OvertimeRateConfig>> => {
    return await axiosClient.post(`${ENDPOINT}/overtime-rates`, payload);
  },

  createOvertimeRateVersion: async (
    id: number,
    payload: OvertimeRateConfigPayload,
  ): Promise<PayrollPolicyResponse<OvertimeRateConfig>> => {
    return await axiosClient.put(`${ENDPOINT}/overtime-rates/${id}`, payload);
  },

  setOvertimeRateStatus: async (
    id: number,
    isActive: boolean,
  ): Promise<PayrollPolicyResponse<null>> => {
    return await axiosClient.patch(`${ENDPOINT}/overtime-rates/${id}/status`, null, {
      params: { isActive },
    });
  },
};
