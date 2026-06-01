import { useCallback, useState } from "react";
import { useNotification } from "../../../core/context/NotificationContext";
import { personnelChangeApi } from "../api/personnelChangeApi";
import type { PersonnelChangeDetail } from "../types/personnelChange";

export const usePersonnelChangeDetail = () => {
  const { triggerAlert } = useNotification();
  const [loading, setLoading] = useState(false);
  const [detail, setDetail] = useState<PersonnelChangeDetail | null>(null);

  const loadDetail = useCallback(async (id: number) => {
    setLoading(true);
    try {
      const response = await personnelChangeApi.getDetail(id);
      setDetail(response.data);
      return response.data;
    } catch (error) {
      console.error(error);
      triggerAlert("error", "Khong tai duoc chi tiet", getErrorMessage(error));
      return null;
    } finally {
      setLoading(false);
    }
  }, [triggerAlert]);

  const clearDetail = useCallback(() => setDetail(null), []);

  return {
    loading,
    detail,
    loadDetail,
    clearDetail,
  };
};

const getErrorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Da co loi xay ra.";
