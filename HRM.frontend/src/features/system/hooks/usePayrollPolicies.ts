import { useCallback, useEffect, useState } from "react";
import { payrollPolicyApi } from "../api/payrollPolicyApi";
import type {
  OvertimeRateConfig,
  OvertimeRateConfigPayload,
  PayrollPolicy,
  PayrollPolicyPayload,
  PayrollPolicyType,
} from "../types/payrollPolicy";

export const usePayrollPolicies = () => {
  const [policies, setPolicies] = useState<PayrollPolicy[]>([]);
  const [overtimeRates, setOvertimeRates] = useState<OvertimeRateConfig[]>([]);
  const [loading, setLoading] = useState(false);
  const [overtimeLoading, setOvertimeLoading] = useState(false);

  const fetchPolicies = useCallback(
    async (policyType?: PayrollPolicyType | "", includeInactive = true) => {
      setLoading(true);
      try {
        const res = await payrollPolicyApi.getAll(policyType, includeInactive);
        if (Array.isArray(res)) {
          setPolicies(res);
        } else if (res && Array.isArray(res.data)) {
          setPolicies(res.data);
        } else {
          setPolicies([]);
        }
      } finally {
        setLoading(false);
      }
    },
    [],
  );

  const savePolicy = async (payload: PayrollPolicyPayload, id?: number) => {
    const res = id
      ? await payrollPolicyApi.update(id, payload)
      : await payrollPolicyApi.create(payload);
    await fetchPolicies("", true);
    return res;
  };

  const setStatus = async (id: number, isActive: boolean) => {
    const res = await payrollPolicyApi.setStatus(id, isActive);
    await fetchPolicies("", true);
    return res;
  };

  const deletePolicy = async (id: number) => {
    const res = await payrollPolicyApi.delete(id);
    await fetchPolicies("", true);
    return res;
  };

  const fetchOvertimeRates = useCallback(async (includeInactive = true) => {
    setOvertimeLoading(true);
    try {
      const res = await payrollPolicyApi.getOvertimeRates(includeInactive);
      if (Array.isArray(res)) {
        setOvertimeRates(res);
      } else if (res && Array.isArray(res.data)) {
        setOvertimeRates(res.data);
      } else {
        setOvertimeRates([]);
      }
    } finally {
      setOvertimeLoading(false);
    }
  }, []);

  const saveOvertimeRate = async (payload: OvertimeRateConfigPayload, id?: number) => {
    const res = id
      ? await payrollPolicyApi.createOvertimeRateVersion(id, payload)
      : await payrollPolicyApi.createOvertimeRate(payload);
    await fetchOvertimeRates(true);
    return res;
  };

  const setOvertimeRateStatus = async (id: number, isActive: boolean) => {
    const res = await payrollPolicyApi.setOvertimeRateStatus(id, isActive);
    await fetchOvertimeRates(true);
    return res;
  };

  useEffect(() => {
    fetchPolicies("", true);
    fetchOvertimeRates(true);
  }, [fetchPolicies, fetchOvertimeRates]);

  return {
    policies,
    overtimeRates,
    loading,
    overtimeLoading,
    fetchPolicies,
    fetchOvertimeRates,
    savePolicy,
    saveOvertimeRate,
    setStatus,
    setOvertimeRateStatus,
    deletePolicy,
  };
};
