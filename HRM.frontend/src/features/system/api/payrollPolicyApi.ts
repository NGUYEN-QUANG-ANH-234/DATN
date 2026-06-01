import axiosClient from "../../../core/api/axiosClient";
import type {
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
};
