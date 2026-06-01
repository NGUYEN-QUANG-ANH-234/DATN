import { useCallback, useState } from "react";
import { useNotification } from "../../../core/context/NotificationContext";
import { personnelChangeApi } from "../api/personnelChangeApi";
import type { PersonnelChangeTimelineItem } from "../types/personnelChange";

export const usePersonnelChangeTimeline = () => {
  const { triggerAlert } = useNotification();
  const [loading, setLoading] = useState(false);
  const [timeline, setTimeline] = useState<PersonnelChangeTimelineItem[]>([]);

  const loadTimeline = useCallback(async (id: number) => {
    setLoading(true);
    try {
      const response = await personnelChangeApi.getTimeline(id);
      setTimeline(response.data ?? []);
      return response.data ?? [];
    } catch (error) {
      console.error(error);
      triggerAlert("error", "Khong tai duoc timeline", getErrorMessage(error));
      return [];
    } finally {
      setLoading(false);
    }
  }, [triggerAlert]);

  const clearTimeline = useCallback(() => setTimeline([]), []);

  return {
    loading,
    timeline,
    loadTimeline,
    clearTimeline,
  };
};

const getErrorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Da co loi xay ra.";
