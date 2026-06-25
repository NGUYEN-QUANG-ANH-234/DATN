import axiosClient from "../../../core/api/axiosClient";
import type { TaskItem } from "./taskApi";

export interface TrainingSummary {
  id: number;
  employeeId: number;
  employeeName: string;
  departmentName?: string | null;
  courseName?: string | null;
  trainingType?: string | null;
  status: string;
  finalScore?: number | null;
  managerEvaluation?: string | null;
  isPassed: boolean;
  evaluationDeadline?: string | null;
  tasks: TaskItem[];
}

export const trainingApi = {
  getMyLearning: async (): Promise<{ success: boolean; data: TrainingSummary[] }> =>
    await axiosClient.get("/training/my-learning"),

  getPending: async (): Promise<{ success: boolean; data: TrainingSummary[] }> =>
    await axiosClient.get("/training/pending-evaluation"),

  getSummary: async (id: number): Promise<{ success: boolean; data: TrainingSummary }> =>
    await axiosClient.get(`/training/summary/${id}`),

  evaluate: async (payload: {
    trainingId: number;
    isApproved: boolean;
    finalScore?: number;
    managerEvaluation?: string;
    createPromotionRequest?: boolean;
  }) => await axiosClient.post("/training/evaluate", payload),
};
