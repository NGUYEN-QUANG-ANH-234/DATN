import axiosClient from "../../../core/api/axiosClient";

export interface ContractDto {
  id: number;
  contractNumber: string;
  contractType: string;
  basicSalary: number;
  salaryPercentage: number;
  insuranceSalary: number;
  startDate: string;
  endDate: string | null;
  status: string;
  version: number;
  negotiationNote: string | null;
  employeeId?: number;
  employeeName?: string;
}

export interface ContractRequestPayload {
  note?: string;
}

export interface ReviewContractPayload {
  isApproved: boolean;
  rejectReason?: string;
}

export interface CreateDraftPayload {
  contractType: string;
  basicSalary: number;
  salaryPercentage: number;
  insuranceSalary: number;
  startDate: string;
  endDate?: string;
}

export interface NegotiatePayload {
  negotiationNote: string;
}

export const contractApi = {
  // 1. Nhân viên gửi yêu cầu ký kết/gia hạn
  createRequest: async (payload: ContractRequestPayload) => {
    return await axiosClient.post("/contracts/requests", payload);
  },

  // 2. Trưởng phòng duyệt/từ chối
  deptReview: async (id: number, payload: ReviewContractPayload) => {
    return await axiosClient.patch(`/contracts/requests/${id}/dept-review`, payload);
  },

  // 3. HR lập bản nháp hợp đồng
  hrCreateDraft: async (id: number, payload: CreateDraftPayload) => {
    return await axiosClient.post(`/contracts/requests/${id}/hr-draft`, payload);
  },

  // 3b. HR từ chối (không đáp ứng chính sách)
  hrReject: async (id: number, payload: ReviewContractPayload) => {
    return await axiosClient.patch(`/contracts/requests/${id}/hr-reject`, payload);
  },

  // 4. Nhân viên thương lượng
  negotiate: async (id: number, payload: NegotiatePayload) => {
    return await axiosClient.put(`/contracts/${id}/negotiate`, payload);
  },

  // 5. Nhân viên đồng ý điều khoản
  employeeAccept: async (id: number) => {
    return await axiosClient.patch(`/contracts/${id}/emp-accept`);
  },

  // 6. Giám đốc chốt phê duyệt
  directorApprove: async (id: number, payload: ReviewContractPayload) => {
    return await axiosClient.patch(`/contracts/${id}/director-approve`, payload);
  },

  // Lấy danh sách hợp đồng (HR/Manager/Director)
  getAllContracts: async (): Promise<{ success: boolean; data: ContractDto[] }> => {
    return await axiosClient.get("/contracts");
  },

  // Lấy hợp đồng của nhân viên hiện tại
  getMyContracts: async (): Promise<{ success: boolean; data: ContractDto[] }> => {
    return await axiosClient.get("/contracts/my-contracts");
  },

  // Lấy danh sách yêu cầu hợp đồng chờ duyệt (dành cho Trưởng phòng)
  getPendingRequests: async (): Promise<{ success: boolean; data: ContractDto[] }> => {
    return await axiosClient.get("/contracts/pending-dept");
  },

  // Lấy danh sách yêu cầu chờ HR duyệt
  getHrPendingRequests: async (): Promise<{ success: boolean; data: ContractDto[] }> => {
    return await axiosClient.get("/contracts/pending-hr");
  },

  // Lấy danh sách chờ Giám đốc duyệt
  getDirectorPending: async (): Promise<{ success: boolean; data: ContractDto[] }> => {
    return await axiosClient.get("/contracts/pending-director");
  },
};
