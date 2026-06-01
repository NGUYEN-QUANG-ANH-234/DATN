import { useCallback, useEffect, useState } from "react";
import { payrollPolicyApi } from "../api/payrollPolicyApi";
import type {
  PayrollPolicy,
  PayrollPolicyPayload,
  PayrollPolicyType,
} from "../types/payrollPolicy";

export const usePayrollPolicies = () => {
  const [policies, setPolicies] = useState<PayrollPolicy[]>([]);
  const [loading, setLoading] = useState(false);

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

  useEffect(() => {
    fetchPolicies("", true);
  }, [fetchPolicies]);

  return { policies, loading, fetchPolicies, savePolicy, setStatus };
};
