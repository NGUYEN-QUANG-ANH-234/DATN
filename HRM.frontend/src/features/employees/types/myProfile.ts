export interface MyProfileDto {
  employeeCode: string;
  fullName: string;
  gender: number | null;
  birthDate: string | null;

  // --- MỚI THÊM ---
  phoneNumber: string | null;
  personalEmail: string | null;
  currentAddress: string | null;
  permanentAddress: string | null;
  socialInsJoinDate: string | null;
  insuranceHospital: string | null;
  emergencyContactName: string | null;
  emergencyPhone: string | null;
  emergencyRelation: string | null;
  // ---------------

  identityNumber: string | null;
  taxCode: string | null;
  socialInsCode: string | null;
  bankAccount: string | null;
  bankName: string | null;
  joinedDate: string | null;
  avatarUrl: string | null;
  identityFrontUrl: string | null;
  identityBackUrl: string | null;
  certificateUrl: string | null;
  status: string;
}

export interface MyContractDto {
  id: number;
  contractNumber: string;
  contractType: string; // Enum chuyển thành chuỗi (Probation, Indefinite,...)
  basicSalary: number;
  salaryPercentage: number;
  insuranceSalary: number;
  startDate: string;
  endDate: string | null;
  status: string;
  version: number;
  negotiationNote: string | null;
}

export type HistoryEventType =
  | "ALL"
  | "PROFILE"
  | "CONTRACT"
  | "ADDENDUM"
  | "EMPLOYMENT";

export interface ConsolidatedHistoryItem {
  date: string;
  eventType: Exclude<HistoryEventType, "ALL">;
  title: string;
  description: string;
  refId: number | null;
  oldValue?: string | null;
  newValue?: string | null;
}

export interface PaginatedHistoryResponse {
  items: ConsolidatedHistoryItem[];
  totalCount: number;
  page: number;
  size: number;
  totalPages: number;
}
