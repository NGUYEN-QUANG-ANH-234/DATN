export interface CreateRecruitmentPayload {
  deptId: number;
  positionId: number;
  quantity: number;
  description?: string;
  deadline?: string;
}

export interface ReviewRecruitmentPayload {
  isApproved: boolean;
  note?: string;
}

export interface ActiveJob {
  id: number;
  departmentName: string;
  positionName: string;
  quantity: number;
  filledSlots?: number;
  remainingSlots?: number;
  canApply?: boolean;
  description: string;
  deadline: string;
  status?: string;
}

export interface DepartmentOption {
  id: number;
  deptName: string;
}

export interface PositionOption {
  id: number;
  title: string;
}

export interface MyRequestRecord {
  id: number;
  quantity: number;
  status: string;
  createdAt: string;
  positionName?: string;
  departmentName?: string;
}

export interface RecruitmentRequestListItem {
  id: number;
  quantity: number;
  filledSlots: number;
  activeCandidateCount: number;
  remainingSlots: number;
  description?: string;
  deadline?: string;
  createdAt: string;
  status: string;
  departmentName?: string;
  positionName?: string;
  isClosed: boolean;
  isExpired: boolean;
  isFull: boolean;
  canApply: boolean;
}

export interface CloseRecruitmentPayload {
  reason?: string;
}
