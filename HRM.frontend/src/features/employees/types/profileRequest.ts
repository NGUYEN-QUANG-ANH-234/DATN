export interface ReviewProfileUpdateDto {
  isApproved: boolean;
  rejectReason?: string;
}

// Model đại diện cho 1 dòng Yêu cầu từ Backend trả về (Giả định bạn đã có API GET list)
export interface PendingProfileRequest {
  id: number;
  employeeId: number;
  employeeName: string;
  employeeCode: string;
  requestedDataJson: string; // Chuỗi JSON chứa dữ liệu thay đổi
  status?: string;
  createdAt: string;
  deadlineSLA: string;
}
