import axiosClient from "../../../core/api/axiosClient";

export interface TaskItem {
  id: number;
  title: string;
  description?: string | null;
  taskType: string;
  employeeId?: number | null;
  employeeName?: string | null;
  departmentName?: string | null;
  progressPercent: number;
  status: string;
  evidencePath?: string | null;
  deadline?: string | null;
  reviewDeadline?: string | null;
  submittedAt?: string | null;
  approvedAt?: string | null;
}

export const taskApi = {
  getMy: async (): Promise<{ success: boolean; data: TaskItem[] }> =>
    await axiosClient.get("/tasks/my"),

  getPendingReview: async (): Promise<{ success: boolean; data: TaskItem[] }> =>
    await axiosClient.get("/tasks/pending-review"),

  updateProgress: async (
    id: number,
    payload: { progressPercent: number; note?: string; evidenceFile?: File | null },
  ) => {
    const formData = new FormData();
    formData.append("progressPercent", String(payload.progressPercent));
    if (payload.note) formData.append("note", payload.note);
    if (payload.evidenceFile) formData.append("evidenceFile", payload.evidenceFile);

    return await axiosClient.patch(`/tasks/${id}/progress`, formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
  },

  provideFeedback: async (id: number, content: string) =>
    await axiosClient.patch(`/tasks/${id}/feedback`, { content }),

  approve: async (id: number) => await axiosClient.patch(`/tasks/${id}/approve`),
};
