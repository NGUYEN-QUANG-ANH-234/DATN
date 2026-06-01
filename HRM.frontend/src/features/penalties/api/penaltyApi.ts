import axiosClient from "../../../core/api/axiosClient";
import type {
  CreateManualPenaltyRecordRequest,
  PenaltyApiResponse,
  PenaltyRecord,
  ReviewPenaltyRecordRequest,
} from "../types/penalty";

export const penaltyApi = {
  getRecords: (status?: string) =>
    axiosClient.get<PenaltyApiResponse<PenaltyRecord[]>, PenaltyApiResponse<PenaltyRecord[]>>(
      "/penalties",
      { params: { status: status || undefined } },
    ),

  getMyRecords: () =>
    axiosClient.get<PenaltyApiResponse<PenaltyRecord[]>, PenaltyApiResponse<PenaltyRecord[]>>(
      "/penalties/my",
    ),

  getEmployeeHistory: (employeeId: number) =>
    axiosClient.get<PenaltyApiResponse<PenaltyRecord[]>, PenaltyApiResponse<PenaltyRecord[]>>(
      `/penalties/employees/${employeeId}/history`,
    ),

  createManual: (payload: CreateManualPenaltyRecordRequest) =>
    axiosClient.post<PenaltyApiResponse<PenaltyRecord>, PenaltyApiResponse<PenaltyRecord>>(
      "/penalties/manual",
      payload,
    ),

  hrReview: (id: number, payload: ReviewPenaltyRecordRequest) =>
    axiosClient.patch<PenaltyApiResponse<PenaltyRecord>, PenaltyApiResponse<PenaltyRecord>>(
      `/penalties/${id}/hr-review`,
      payload,
    ),

  directorReview: (id: number, payload: ReviewPenaltyRecordRequest) =>
    axiosClient.patch<PenaltyApiResponse<PenaltyRecord>, PenaltyApiResponse<PenaltyRecord>>(
      `/penalties/${id}/director-review`,
      payload,
    ),
};
