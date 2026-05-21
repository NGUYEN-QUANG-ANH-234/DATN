import axiosClient from "../../../core/api/axiosClient";

export interface ContractAddendumDto {
  id: number;
  contractId: number;
  contractNumber: string;
  addendumNumber: string;
  newBasicSalary?: number | null;
  newInsuranceSalary?: number | null;
  newEndDate?: string | null;
  otherChangesJson?: string | null;
  content?: string | null;
  effectiveDate: string;
  status: string;
  rejectReason?: string | null;
  createdAt: string;
  employeeId?: number | null;
  employeeName?: string | null;
}

export interface CreateContractAddendumPayload {
  newBasicSalary?: number;
  newInsuranceSalary?: number;
  newEndDate?: string;
  otherChangesJson?: string;
  content?: string;
  effectiveDate: string;
}

export const contractAddendumApi = {
  createDraft: async (contractId: number, payload: CreateContractAddendumPayload) => {
    return await axiosClient.post(`/contracts/${contractId}/addendums`, payload);
  },

  updateDraft: async (id: number, payload: CreateContractAddendumPayload) => {
    return await axiosClient.patch(`/addendums/${id}`, payload);
  },

  getByContract: async (contractId: number): Promise<{ success: boolean; data: ContractAddendumDto[] }> => {
    return await axiosClient.get(`/contracts/${contractId}/addendums`);
  },

  getAll: async (): Promise<{ success: boolean; data: ContractAddendumDto[] }> => {
    return await axiosClient.get("/addendums");
  },

  getPendingDirector: async (): Promise<{ success: boolean; data: ContractAddendumDto[] }> => {
    return await axiosClient.get("/addendums/pending-director");
  },

  submit: async (id: number) => {
    return await axiosClient.patch(`/addendums/${id}/submit`);
  },

  approve: async (id: number) => {
    return await axiosClient.patch(`/addendums/${id}/approve`);
  },

  reject: async (id: number, rejectReason: string) => {
    return await axiosClient.patch(`/addendums/${id}/reject`, { rejectReason });
  },
};
