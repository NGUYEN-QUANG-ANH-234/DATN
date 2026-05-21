export interface ApplyJobPayload {
  recruitmentRequestId: number;
  fullName: string;
  email: string;
  cvFile: File;
}

export interface ApiResponse<T = unknown> {
  success: boolean;
  message: string;
  data?: T;
}

export interface ActiveJob {
  id: number;
  departmentName: string;
  positionName: string;
  quantity: number;
  description: string;
  deadline: string;
}

export interface CandidateHistoryDto {
  candidateId: number;
  recruitmentRequestId: number;
  fullName: string;
  cvFilePath: string;
  email: string;
  jobTitle: string;
  departmentName: string;
  status: string;
  appliedDate: string;
}
