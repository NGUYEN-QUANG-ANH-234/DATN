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
  | "Cancelled"
  | "RevisionRequired"
  | "Rejected";

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
  projectBonusSources?: ProjectBonusSource[];
  externalTimesheetSources?: ExternalTimesheetSource[];
}

export interface ProjectBonusSource {
  id: number;
  batchId: number;
  fileName?: string | null;
  payrollPeriod?: string | null;
  approvedAt?: string | null;
  employeeCode: string;
  employeeName?: string | null;
  projectCode: string;
  projectName: string;
  bonusAmount: number;
  taxable: boolean;
  insuranceContributable: boolean;
  reason?: string | null;
  note?: string | null;
}

export interface ExternalTimesheetSource {
  id: number;
  importId: number;
  fileName?: string | null;
  payrollPeriod?: string | null;
  approvedAt?: string | null;
  collaboratorEmployeeId?: number | null;
  collaboratorCode: string;
  collaboratorName?: string | null;
  workDate: string;
  projectCode: string;
  taskCode: string;
  approvedHours: number;
  hourlyRate: number;
  amount: number;
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
  submittedAt?: string | null;
  approvedAt?: string | null;
  lockedAt?: string | null;
  reviewNote?: string | null;
  details: SalarySlipDetail[];
}

export interface PayrollRunSummary {
  month: number;
  year: number;
  period: string;
  status: PayrollStatus;
  statusText: string;
  slipCount: number;
  grossIncome: number;
  netSalary: number;
  totalCompanyCost: number;
  calculatedAt?: string | null;
  submittedAt?: string | null;
  approvedAt?: string | null;
  lockedAt?: string | null;
  submittedByAccountId?: number | null;
  approvedByAccountId?: number | null;
  lockedByAccountId?: number | null;
  reviewNote?: string | null;
  slips: SalarySlip[];
}

export interface PayrollRunReviewRequest {
  month: number;
  year: number;
  isApproved: boolean;
  requestRevision?: boolean;
  note?: string | null;
}

export interface PayrollCalculationResult {
  month: number;
  year: number;
  createdCount: number;
  skippedCount: number;
  warnings: string[];
  payrolls: SalarySlip[];
}

export interface PayrollFeatureToggles {
  enableInsurance: boolean;
  enableOvertime: boolean;
  enableMealAllowance: boolean;
  enableExternalTimesheetPay: boolean;
}

export interface PayrollPreflightPolicy {
  area: string;
  code: string;
  name: string;
  version: number;
  versionCode?: string | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  status: string;
  isApplied: boolean;
  note?: string | null;
}

export interface PayrollDependencyImpact {
  key: string;
  name: string;
  enabled: boolean;
  impacts: string[];
}

export interface PayrollPreflight {
  month: number;
  year: number;
  period: string;
  periodStart: string;
  periodEnd: string;
  canCalculate: boolean;
  featureToggles: PayrollFeatureToggles;
  policies: PayrollPreflightPolicy[];
  dependencyImpacts: PayrollDependencyImpact[];
  errors: string[];
  warnings: string[];
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

export type ProjectBonusImportStatus =
  | "Draft"
  | "PendingReview"
  | "Approved"
  | "Rejected"
  | "Cancelled";

export interface ProjectBonusImportLine {
  id: number;
  rowNumber: number;
  employeeId?: number | null;
  employeeCode: string;
  employeeName?: string | null;
  projectCode: string;
  projectName: string;
  bonusAmount: number;
  taxable: boolean;
  insuranceContributable: boolean;
  reason?: string | null;
  note?: string | null;
  validationStatus: string;
  isValid: boolean;
  errorMessage?: string | null;
}

export interface ProjectBonusImportBatch {
  id: number;
  periodMonth: number;
  periodYear: number;
  payrollPeriod: string;
  fileName: string;
  status: ProjectBonusImportStatus | string;
  statusText: string;
  totalRows: number;
  validRows: number;
  errorRows: number;
  totalAmount: number;
  uploadedByAccountId: number;
  uploadedByName?: string | null;
  createdAt: string;
  approvedByAccountId?: number | null;
  approvedByName?: string | null;
  approvedAt?: string | null;
  note?: string | null;
  lines: ProjectBonusImportLine[];
}

export interface ProjectBonusImportPreview {
  periodMonth: number;
  periodYear: number;
  payrollPeriod: string;
  fileName: string;
  overwrite: boolean;
  canSave: boolean;
  totalRows: number;
  validRows: number;
  errorRows: number;
  totalAmount: number;
  globalErrors: string[];
  lines: ProjectBonusImportLine[];
}

export interface ReviewProjectBonusImportRequest {
  isApproved: boolean;
  note?: string | null;
}

export interface CancelProjectBonusImportRequest {
  note?: string | null;
}

export type ExternalTimesheetImportStatus =
  | "Draft"
  | "Imported"
  | "Validated"
  | "Approved"
  | "Rejected"
  | "PayrollImported"
  | "Cancelled";

export interface ExternalTimesheetImportLine {
  id: number;
  rowNumber: number;
  collaboratorEmployeeId?: number | null;
  collaboratorCode: string;
  collaboratorName?: string | null;
  workDate?: string | null;
  workDateText: string;
  projectCode: string;
  taskCode: string;
  approvedHours: number;
  hourlyRate: number;
  amount: number;
  note?: string | null;
  validationStatus: string;
  isValid: boolean;
  errorMessage?: string | null;
}

export interface ExternalTimesheetImportBatch {
  id: number;
  importMonth: number;
  importYear: number;
  payrollPeriod: string;
  sourceSystem: string;
  fileName: string;
  status: ExternalTimesheetImportStatus | string;
  statusText: string;
  totalRows: number;
  validRows: number;
  errorRows: number;
  totalHours: number;
  totalAmount: number;
  importedByAccountId: number;
  importedByName?: string | null;
  importedAt: string;
  approvedByAccountId?: number | null;
  approvedByName?: string | null;
  approvedAt?: string | null;
  note?: string | null;
  lines: ExternalTimesheetImportLine[];
}

export interface ExternalTimesheetImportPreview {
  importMonth: number;
  importYear: number;
  payrollPeriod: string;
  sourceSystem: string;
  fileName: string;
  overwrite: boolean;
  canSave: boolean;
  totalRows: number;
  validRows: number;
  errorRows: number;
  totalHours: number;
  totalAmount: number;
  globalErrors: string[];
  lines: ExternalTimesheetImportLine[];
}

export interface ReviewExternalTimesheetImportRequest {
  isApproved: boolean;
  note?: string | null;
}

export interface CancelExternalTimesheetImportRequest {
  note?: string | null;
}

export type PayrollFormulaStatus =
  | "Pending"
  | "Approved"
  | "Rejected"
  | "Expired"
  | "Draft"
  | "PendingDirectorApproval"
  | "RevisionRequired"
  | "Active"
  | "Archived";

export interface PayrollFormulaLine {
  id?: number;
  salaryComponentTypeId?: number | null;
  componentCode: string;
  componentName?: string | null;
  expression: string;
  calculationOrder: number;
  isGrossComponent: boolean;
  isTaxable: boolean;
  isInsuranceBased: boolean;
  isDeduction: boolean;
  isSnapshotRequired: boolean;
  note?: string | null;
}

export interface PayrollFormula {
  id: number;
  formulaCode: string;
  formulaName: string;
  expression?: string | null;
  isActive: boolean;
  contractType?: string | null;
  payBasis?: string | null;
  employeeType?: string | null;
  deptId?: number | null;
  positionId?: number | null;
  jobLevelId?: number | null;
  version: number;
  versionCode?: string | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  status: PayrollFormulaStatus | string;
  statusText: string;
  deadlineAt?: string | null;
  submittedAt?: string | null;
  approvedAt?: string | null;
  activatedAt?: string | null;
  archivedAt?: string | null;
  rejectReason?: string | null;
  reviewNote?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  lines: PayrollFormulaLine[];
}

export interface UpsertPayrollFormulaRequest {
  formulaCode: string;
  formulaName: string;
  expression?: string | null;
  contractType?: string | null;
  payBasis?: string | null;
  employeeType?: string | null;
  deptId?: number | null;
  positionId?: number | null;
  jobLevelId?: number | null;
  versionCode?: string | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  lines: PayrollFormulaLine[];
}

export interface PayrollFormulaReviewRequest {
  isApproved: boolean;
  requestRevision?: boolean;
  note?: string | null;
}

export interface PayrollFormulaValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
}

export interface PayrollFormulaVariable {
  code: string;
  source: string;
  description: string;
  module?: string | null;
  dataType?: string | null;
  aggregationType?: string | null;
  isPeriodBased: boolean;
  isActive: boolean;
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
  lines: ExternalTimesheetImportLine[];
}
