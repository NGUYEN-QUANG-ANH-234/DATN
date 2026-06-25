export interface SubmitOnboardingFormState {
  candidateId: number;
  trackingCode: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  personalEmail: string;
  currentAddress: string;
  permanentAddress: string;
  identityNumber: string;
  nationality: string;
  ethnicity: string;
  emergencyContactName: string;
  emergencyPhone: string;
  emergencyRelation: string;
  gender: string;
  birthDate: string;
  taxCode: string;
  socialInsCode: string;
  bankAccount: string;
  bankName: string;
  identityFrontFile: File | null;
  identityBackFile: File | null;
  certificateFile: File | null;
}

export interface OnboardingCandidateLookup {
  candidateId: number;
  trackingCode: string;
  email: string;
  fullName: string;
  status: string;
  recruitmentRequestId?: number;
  departmentId?: number;
  departmentName?: string;
  positionId?: number;
  positionName?: string;
}

export interface PendingOnboardingRequest {
  id: number;
  candidateId: number;
  recruitmentRequestId?: number;
  departmentId?: number;
  departmentName?: string;
  positionId?: number;
  positionName?: string;
  requestedDataJson: string;
  status: string;
  createdAt: string;
}

export interface ReviewOnboardingDto {
  isApproved: boolean;
  rejectReason?: string;
  roleId?: number; // 3: HR, 4: Manager, 5: Employee, 6: Collaborator, 7: Intern
  departmentId?: number;
  positionId?: number;
}
