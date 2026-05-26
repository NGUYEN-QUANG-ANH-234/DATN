import axiosClient from "../../../core/api/axiosClient";

export interface PerformanceDetail {
  id: number;
  kpiCode: string;
  kpiName: string;
  weightPercent: number;
  targetValue?: number | null;
  actualValue?: number | null;
  unit?: string | null;
  employeeSelfPercent: number;
  achievedPercent: number;
  managerScore: number;
  systemPenaltyPoint: number;
  systemPenaltyReason?: string | null;
  manualPenaltyPoint: number;
  manualPenaltyReason?: string | null;
  penaltyPoint?: number;
  penaltyReason?: string | null;
  finalPoint: number;
  employeeComment?: string | null;
  managerComment?: string | null;
  evidencePath?: string | null;
}

export interface PerformanceEvaluation {
  id: number;
  employeeId: number;
  employeeName: string;
  departmentName?: string | null;
  period: string;
  totalWeight: number;
  systemPenaltyPoint: number;
  totalScore: number;
  finalRating?: string | null;
  finalComment?: string | null;
  status: string;
  details: PerformanceDetail[];
}

export interface FinalizePerformancePayload {
  isApproved: boolean;
  finalRating?: string;
  finalComment?: string;
  details: Array<{
    detailId: number;
    managerScore: number;
    manualPenaltyPoint: number;
    manualPenaltyReason?: string;
    managerComment?: string;
  }>;
}

export interface UpdatePerformanceProgressPayload {
  details: Array<{
    detailId: number;
    employeeSelfPercent: number;
    actualValue?: number | null;
    employeeComment?: string;
  }>;
}

export const performanceApi = {
  getMy: async (): Promise<{ success: boolean; data: PerformanceEvaluation[] }> =>
    await axiosClient.get("/performance/my"),

  getPending: async (): Promise<{ success: boolean; data: PerformanceEvaluation[] }> =>
    await axiosClient.get("/performance/pending-evaluation"),

  getDetail: async (id: number): Promise<{ success: boolean; data: PerformanceEvaluation }> =>
    await axiosClient.get(`/performance/${id}`),

  finalizeScore: async (id: number, payload: FinalizePerformancePayload) =>
    await axiosClient.patch(`/performance/${id}/finalize-score`, payload),

  updateProgress: async (id: number, payload: UpdatePerformanceProgressPayload) =>
    await axiosClient.patch(`/performance/${id}/progress`, payload),
};
