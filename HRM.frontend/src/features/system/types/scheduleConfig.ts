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
  month: number;
  standardWorkDays: number;
  standardHoursPerDay: number;
  includePaidLeaveInWorkDays: boolean;
  workingDaysOfWeek?: string | null;
  holidayDatesJson?: string | null;
  lockWorkCalendar: boolean;
  calendarNote?: string | null;
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
  month?: number | null;
  standardWorkDays?: number | null;
  standardHoursPerDay?: number | null;
  includePaidLeaveInWorkDays: boolean;
  workingDaysOfWeek?: string | null;
  holidayDatesJson?: string | null;
  isWorkCalendarLocked: boolean;
  calendarNote?: string | null;
}

export interface LeaveTypeSelect {
  id: number;
  typeName: string;
  category: string;
  isPaid: boolean;
  countsAsUnpaidForInsurance: boolean;
  countsAsWorkday: boolean;
  deductAnnualLeave: boolean;
  affectsKpiPenalty: boolean;
}

export interface ScheduleChangeHistoryItem {
  id: number;
  actionType: string;
  actorName?: string | null;
  message?: string | null;
  timestamp: string;
}
