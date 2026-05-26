import axiosClient from "../../../core/api/axiosClient";

export interface KpiImportError {
  rowNumber: number;
  message: string;
}

export interface KpiImportResult {
  importBatchId: number;
  period: string;
  deptId: number;
  totalRows: number;
  successRows: number;
  errorRows: number;
  createdOrUpdatedReviews: number;
  createdDetails: number;
  totalAssignedWeight: number;
  errors: KpiImportError[];
}

export const kpiApi = {
  importKpis: async (
    file: File,
    period?: string,
    deptId?: number,
  ): Promise<{ success: boolean; data: KpiImportResult; message: string }> => {
    const formData = new FormData();
    formData.append("file", file);
    if (period) formData.append("period", period);
    if (deptId) formData.append("deptId", String(deptId));

    return await axiosClient.post("/kpis/import", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
  },
};
