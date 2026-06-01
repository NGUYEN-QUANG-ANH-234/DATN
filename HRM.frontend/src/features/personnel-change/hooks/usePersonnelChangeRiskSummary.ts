import { useCallback, useState } from "react";
import { useNotification } from "../../../core/context/NotificationContext";
import { personnelChangeApi } from "../api/personnelChangeApi";
import type { PersonnelChangeRiskSummary } from "../types/personnelChange";

export const usePersonnelChangeRiskSummary = () => {
  const { triggerAlert } = useNotification();
  const [loading, setLoading] = useState(false);
  const [riskSummary, setRiskSummary] = useState<PersonnelChangeRiskSummary | null>(null);

  const loadRiskSummary = useCallback(async (id: number) => {
    setLoading(true);
    try {
      const response = await personnelChangeApi.getRiskSummary(id);
      setRiskSummary(response.data);
      return response.data;
    } catch (error) {
      console.error(error);
      triggerAlert("error", "Khong tai duoc risk summary", getErrorMessage(error));
      return null;
    } finally {
      setLoading(false);
    }
  }, [triggerAlert]);

  const clearRiskSummary = useCallback(() => setRiskSummary(null), []);

  return {
    loading,
    riskSummary,
    loadRiskSummary,
    clearRiskSummary,
  };
};

const getErrorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Da co loi xay ra.";
