export interface ConfigureWorkScheduleDto {
  shiftName: string;
  startTime: string; // Định dạng "HH:mm" hoặc "HH:mm:ss"
  endTime: string;
  breakStartTime: string | null;
  breakEndTime: string | null;
  lateThresholdMins: number;
  earlyLeaveThresholdMins: number;
  deptId: number;
  leaveTypeId: number;
  year: number;
  totalDays: number;
}

export interface ConfiguredScheduleItem {
  deptId: number;
  deptName: string;
  shiftName: string;
  startTime: string;
  endTime: string;
  breakStartTime: string | null;
  breakEndTime: string | null;
  lateThresholdMins: number;
  earlyLeaveThresholdMins: number;
  leaveTypeName: string;
  year: number;
  totalDays: number;
}

export interface LeaveTypeSelect {
  id: number;
  typeName: string;
}
