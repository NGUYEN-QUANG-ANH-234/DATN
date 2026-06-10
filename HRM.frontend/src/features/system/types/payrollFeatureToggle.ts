export type PayrollFeatureToggle = {
  enableInsurance: boolean;
  enableOvertime: boolean;
  enableMealAllowance: boolean;
  enableExternalTimesheetPay: boolean;
};

export type PayrollFeatureToggleResponse<T> = {
  success: boolean;
  data: T;
  message?: string;
};
