export type PolicyVersionStatus = 0 | 1 | 2 | "Draft" | "Active" | "Archived" | number;

export type CompanyCalendarDayType =
  | 0
  | 1
  | 2
  | 3
  | 4
  | 5
  | "PublicHoliday"
  | "CompanyHoliday"
  | "CompensatoryWorkingDay"
  | "CompensatoryDayOff"
  | "SpecialPaidLeave"
  | "UnpaidCompanyClosure";

export type CompanyCalendarDay = {
  id: number;
  date: string;
  dayType: CompanyCalendarDayType;
  name: string;
  isPaid: boolean;
  isOvertimeHoliday: boolean;
  isWorkingDayOverride: boolean;
  description?: string | null;
};

export type CompanyCalendar = {
  id: number;
  year: number;
  versionCode: string;
  effectiveFrom: string;
  effectiveTo?: string | null;
  status: PolicyVersionStatus;
  sourceRef?: string | null;
  lockedAfterUsed: boolean;
  note?: string | null;
  days: CompanyCalendarDay[];
};

export type SaveCompanyCalendarPayload = {
  id?: number | null;
  versionCode?: string | null;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  status: PolicyVersionStatus;
  sourceRef?: string | null;
  note?: string | null;
  days: Omit<CompanyCalendarDay, "id">[];
};

export type CompanyCalendarResponse<T> = {
  success: boolean;
  data: T;
  message?: string;
};
