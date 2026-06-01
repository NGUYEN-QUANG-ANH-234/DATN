import type { FormEvent } from "react";

export type PayrollStatus =
  | "Draft"
  | "Calculated"
  | "HRReviewed"
  | "PendingApproval"
  | "Approved"
  | "Locked"
  | "Finalized"
  | "Paid"
  | "Cancelled";

export interface SalarySlipDetail {
  id: number;
  componentCode: string;
  componentName: string;
  amount: number;
  taxableAmount: number;
  insuranceBaseAmount: number;
  isIncome: boolean;
  isDeduction: boolean;
  isTaxable: boolean;
  isInsuranceBased: boolean;
  note?: string | null;
}

export interface SalarySlip {
  id: number;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  departmentName?: string | null;
  positionName?: string | null;
  month: number;
  year: number;
  period: string;
  baseSalary: number;
  baseSalaryActual: number;
  standardWorkDays: number;
  standardWorkHours: number;
  actualWorkDays: number;
  actualWorkHours: number;
  payableWorkHours: number;
  workedMinutes: number;
  lateMinutes: number;
  earlyLeaveMinutes: number;
  unpaidLeaveWorkdays: number;
  serviceMonths: number;
  serviceYears: number;
  seniorityAllowance: number;
  seniorityRate: number;
  actualOtMinutes: number;
  grossIncome: number;
  insuranceSalary: number;
  employeeInsuranceAmount: number;
  employerContributionAmount: number;
  taxableGrossIncome: number;
  taxableIncome: number;
  pitAmount: number;
  otherDeductions: number;
  netSalary: number;
  totalCompanyCost: number;
  status: PayrollStatus;
  calculatedAt?: string | null;
  lockedAt?: string | null;
  details: SalarySlipDetail[];
}

export interface PayrollCalculationResult {
  month: number;
  year: number;
  createdCount: number;
  skippedCount: number;
  warnings: string[];
  payrolls: SalarySlip[];
}

export type PayrollAdjustmentType =
  | "RetroactiveSalaryIncrease"
  | "RetroactiveAllowance"
  | "InsuranceArrears"
  | "TaxAdjustment"
  | "ManualCorrection";

export interface PayrollAdjustment {
  id: number;
  employeeId: number;
  employeeCode?: string | null;
  employeeName?: string | null;
  relatedPayrollId?: number | null;
  adjustmentType: PayrollAdjustmentType | string;
  recognizedMonth: number;
  recognizedYear: number;
  effectiveFromMonth?: string | null;
  effectiveToMonth?: string | null;
  amount: number;
  isTaxable: boolean;
  isInsuranceBased: boolean;
  isDeduction: boolean;
  reason: string;
  status: string;
  createdAt?: string | null;
}

export interface CreatePayrollAdjustmentRequest {
  employeeId: number;
  adjustmentType: PayrollAdjustmentType;
  recognizedMonth: number;
  recognizedYear: number;
  effectiveFromMonth?: string | null;
  effectiveToMonth?: string | null;
  amount: number;
  isTaxable: boolean;
  isInsuranceBased: boolean;
  isDeduction: boolean;
  reason: string;
}

export interface ExternalTimesheetLinePreview {
  rowNumber: number;
  collaboratorCode: string;
  collaboratorName: string;
  workDate: string;
  projectCode: string;
  taskCode: string;
  approvedHours: number;
  hourlyRate: number;
  amount: number;
  note?: string;
}

export interface ExternalTimesheetImportState {
  fileName: string;
  sourceSystem: string;
  importMonth: number;
  importYear: number;
  lines: ExternalTimesheetLinePreview[];
  totalHours: number;
  totalAmount: number;
}

export interface PayrollFormulaPreviewLine {
  componentCode: string;
  componentName: string;
  expression: string;
  calculationOrder: number;
  isGrossComponent: boolean;
  isTaxable: boolean;
  isInsuranceBased: boolean;
  isDeduction: boolean;
}

export interface ApiResponse<T> {
  success?: boolean;
  message?: string;
  data: T;
}

export interface PayrollMetricCardProps {
  label: string;
  value: string;
  strong?: boolean;
}

export interface PayrollPeriodFilterProps {
  month: number;
  year: number;
  loading?: boolean;
  calculating?: boolean;
  canCalculate?: boolean;
  canExport?: boolean;
  showCalculate?: boolean;
  showExport?: boolean;
  exportLabel?: string;
  onMonthChange: (month: number) => void;
  onYearChange: (year: number) => void;
  onRefresh: () => void;
  onCalculate?: () => void;
  onExport?: () => void;
}

export interface SalarySlipTableProps {
  slips: SalarySlip[];
  selectedIds: number[];
  loading?: boolean;
  emptyText: string;
  onToggle: (id: number) => void;
  onOpenDetail: (id: number) => void;
}

export interface SalarySlipDetailPanelProps {
  slip: SalarySlip;
}

export interface SalaryFormulaPreviewTableProps {
  lines: PayrollFormulaPreviewLine[];
}

export interface PayrollAdjustmentFormProps {
  form: CreatePayrollAdjustmentRequest;
  period: string;
  saving?: boolean;
  onChange: (patch: Partial<CreatePayrollAdjustmentRequest>) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
}

export interface PayrollAdjustmentTableProps {
  adjustments: PayrollAdjustment[];
  loading?: boolean;
  period: string;
}

export interface ExternalTimesheetPreviewTableProps {
  lines: ExternalTimesheetLinePreview[];
}
