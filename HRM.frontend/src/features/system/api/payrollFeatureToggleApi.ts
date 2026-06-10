import axiosClient from "../../../core/api/axiosClient";
import type {
  PayrollFeatureToggle,
  PayrollFeatureToggleResponse,
} from "../types/payrollFeatureToggle";

const ENDPOINT = "/system/payroll-feature-toggles";

export const payrollFeatureToggleApi = {
  get: async (): Promise<PayrollFeatureToggleResponse<PayrollFeatureToggle>> => {
    return await axiosClient.get(ENDPOINT);
  },

  update: async (
    payload: PayrollFeatureToggle,
  ): Promise<PayrollFeatureToggleResponse<PayrollFeatureToggle>> => {
    return await axiosClient.put(ENDPOINT, payload);
  },
};
