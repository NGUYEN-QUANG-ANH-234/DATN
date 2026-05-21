// Payload Trưởng phòng gửi lên
export interface CreateRecruitmentPayload {
  deptId: number;
  positionId: number;
  quantity: number;
  description?: string;
  deadline?: string;
  approverIds: number[]; // VD: [ID_HR, ID_GiamDoc]
}

// Payload HR/Giám đốc gửi lên khi duyệt
export interface ReviewRecruitmentPayload {
  isApproved: boolean;
  note?: string;
}

// Model hiển thị tin tuyển dụng Public
export interface ActiveJob {
  id: number;
  departmentName: string;
  positionName: string;
  quantity: number;
  description: string;
  deadline: string;
}

export interface DepartmentOption {
  id: number;
  deptName: string;
}

export interface PositionOption {
  id: number;
  title: string;
}

export interface CreateRecruitmentPayload {
  deptId: number;
  positionId: number;
  quantity: number;
  description?: string;
  deadline?: string;
}

export interface MyRequestRecord {
  id: number;
  quantity: number;
  status: string;
  createdAt: string;
  positionName?: string;
  departmentName?: string;
}
