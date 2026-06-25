export interface ProfileUpdateFormState {
  // --- Cá nhân ---
  fullName: string;
  gender: string;
  birthDate: string;
  nationality: string;
  ethnicity: string;

  // --- Thông tin liên hệ (MỚI) ---
  phoneNumber: string;
  personalEmail: string;
  currentAddress: string;
  permanentAddress: string;

  // --- Định danh & Thuế & Bảo hiểm ---
  identityNumber: string;
  taxCode: string;
  socialInsCode: string;
  socialInsJoinDate: string; // MỚI
  insuranceHospital: string; // MỚI

  // --- Ngân hàng ---
  bankAccount: string;
  bankName: string;

  // --- Liên hệ khẩn cấp (MỚI) ---
  emergencyContactName: string;
  emergencyPhone: string;
  emergencyRelation: string;

  // --- Files ---
  avatarFile: File | null;
  identityFrontFile: File | null;
  identityBackFile: File | null;
  certificateFile: File | null;
}
