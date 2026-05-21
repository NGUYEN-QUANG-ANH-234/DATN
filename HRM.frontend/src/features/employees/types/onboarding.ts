export interface SubmitOnboardingFormState {
  socialInsCode: string | number | readonly string[] | undefined;
  taxCode: string | number | readonly string[] | undefined;
  bankAccount: string | number | readonly string[] | undefined;
  bankName: string | number | readonly string[] | undefined;
  birthDate: string | number | readonly string[] | undefined;
  gender: string | number | readonly string[] | undefined;
  candidateId: number; // Thường lấy từ URL Params hoặc Context sau khi trúng tuyển
  fullName: string;
  email: string;
  phoneNumber: string;
  personalEmail: string;
  currentAddress: string;
  permanentAddress: string;
  identityNumber: string;
  emergencyContactName: string;
  emergencyPhone: string;
  emergencyRelation: string;
  identityFrontFile: File | null;
  identityBackFile: File | null;
  certificateFile: File | null;
}

export interface PendingOnboardingRequest {
  id: number;
  candidateId: number;
  requestedDataJson: string; // Chứa dữ liệu cá nhân dạng JSON
  status: string; // Pending_HR, Completed, Rejected
  createdAt: string;
}

export interface ReviewOnboardingDto {
  isApproved: boolean;
  rejectReason?: string;
  roleId?: number; // 3: HR, 4: Manager, 5: Employee, 6: Collaborator, 7: Intern
}
