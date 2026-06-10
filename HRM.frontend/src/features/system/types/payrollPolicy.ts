export const PayrollPolicyType = {
  Overtime: 0,
  PitTax: 1,
  Insurance: 2,
  Allowance: 3,
  Deduction: 4,
  Seniority: 5,
  MinimumWage: 6,
  KpiBonus: 7,
} as const;

export type PayrollPolicyType =
  (typeof PayrollPolicyType)[keyof typeof PayrollPolicyType];

export const PayrollPolicyValueType = {
  RatePercent: 0,
  Amount: 1,
  Bracket: 2,
  Formula: 3,
} as const;

export type PayrollPolicyValueType =
  (typeof PayrollPolicyValueType)[keyof typeof PayrollPolicyValueType];

export interface PayrollPolicy {
  id: number;
  policyType: PayrollPolicyType;
  code: string;
  name: string;
  valueType: PayrollPolicyValueType;
  ratePercent?: number | null;
  amount?: number | null;
  fromAmount?: number | null;
  toAmount?: number | null;
  quickDeduction?: number | null;
  formulaJson?: string | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  version: number;
  versionCode?: string | null;
  status?: number | string;
  sourceRef?: string | null;
  supersedesVersionId?: number | null;
  activatedAt?: string | null;
  lockedAfterUsed?: boolean;
  isActive: boolean;
  description?: string | null;
}

export type PayrollPolicyPayload = Omit<PayrollPolicy, "id">;

export interface PayrollPolicyResponse<T> {
  success: boolean;
  message?: string;
  data?: T;
}
