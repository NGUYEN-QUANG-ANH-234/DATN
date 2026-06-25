import type {
  CreatePayrollAdjustmentRequest,
  PayrollAdjustmentType,
  PayrollFormulaPreviewLine,
  PayrollStatus,
} from "../types/payroll";

const payrollStatusKeys = [
  "Draft",
  "Calculated",
  "HRReviewed",
  "PendingApproval",
  "Approved",
  "Locked",
  "Finalized",
  "Paid",
  "Cancelled",
  "RevisionRequired",
  "Rejected",
] as const;

export const payrollStatusLabels: Record<string, string> = {
  Draft: "Bản nháp",
  Calculated: "Đã tổng hợp",
  HRReviewed: "HR đã kiểm tra",
  PendingApproval: "Chờ duyệt",
  Approved: "Đã duyệt",
  Locked: "Đã khóa",
  Finalized: "Đã chốt",
  Paid: "Đã chi trả",
  Cancelled: "Đã hủy",
  RevisionRequired: "Cần bổ sung",
  Rejected: "Từ chối",
};

export const normalizePayrollStatus = (status?: PayrollStatus | string | number | null) => {
  if (typeof status === "number") {
    return payrollStatusKeys[status] ?? String(status);
  }

  const value = String(status ?? "");
  if (/^\d+$/.test(value)) {
    const numericStatus = Number(value);
    return payrollStatusKeys[numericStatus] ?? value;
  }

  return value;
};

export const getPayrollStatusLabel = (
  status?: PayrollStatus | string | number | null,
  fallbackLabel?: string | null,
) => {
  const normalized = normalizePayrollStatus(status);
  return fallbackLabel || payrollStatusLabels[normalized] || normalized || "Không xác định";
};

export const adjustmentTypes: Array<{ value: PayrollAdjustmentType; label: string }> = [
  { value: "RetroactiveSalaryIncrease", label: "Truy lĩnh tăng lương" },
  { value: "RetroactiveAllowance", label: "Truy lĩnh phụ cấp" },
  { value: "InsuranceArrears", label: "Điều chỉnh bảo hiểm" },
  { value: "TaxAdjustment", label: "Điều chỉnh thuế" },
  { value: "ManualCorrection", label: "Điều chỉnh nghiệp vụ lương" },
];

export const formulaPreviewLines: PayrollFormulaPreviewLine[] = [
  {
    componentCode: "BASE_SALARY_ACTUAL",
    componentName: "Lương cơ bản theo công",
    expression: "contract_segment_salary_amount",
    calculationOrder: 10,
    isGrossComponent: true,
    isTaxable: true,
    isInsuranceBased: true,
    isDeduction: false,
  },
  {
    componentCode: "POSITION_ALLOWANCE",
    componentName: "Phụ cấp chức vụ",
    expression: "position_allowance / standard_workdays * actual_workdays",
    calculationOrder: 20,
    isGrossComponent: true,
    isTaxable: true,
    isInsuranceBased: true,
    isDeduction: false,
  },
  {
    componentCode: "SENIORITY_ALLOWANCE",
    componentName: "Phụ cấp thâm niên",
    expression: "seniority_allowance_prorated",
    calculationOrder: 35,
    isGrossComponent: true,
    isTaxable: true,
    isInsuranceBased: true,
    isDeduction: false,
  },
  {
    componentCode: "KPI_BONUS",
    componentName: "Thưởng KPI thực nhận",
    expression: "kpi_bonus_amount * kpi_score / 100",
    calculationOrder: 80,
    isGrossComponent: true,
    isTaxable: true,
    isInsuranceBased: false,
    isDeduction: false,
  },
  {
    componentCode: "INTERN_ALLOWANCE",
    componentName: "Trợ cấp thực tập",
    expression: "intern_allowance_amount",
    calculationOrder: 75,
    isGrossComponent: true,
    isTaxable: true,
    isInsuranceBased: false,
    isDeduction: false,
  },
  {
    componentCode: "OT_BASE",
    componentName: "Làm thêm phần gốc 100%",
    expression: "overtime_base_amount",
    calculationOrder: 90,
    isGrossComponent: true,
    isTaxable: true,
    isInsuranceBased: false,
    isDeduction: false,
  },
  {
    componentCode: "OT_PREMIUM",
    componentName: "Làm thêm phần trả cao hơn",
    expression: "overtime_premium_amount",
    calculationOrder: 100,
    isGrossComponent: true,
    isTaxable: false,
    isInsuranceBased: false,
    isDeduction: false,
  },
  {
    componentCode: "EMPLOYEE_INSURANCE",
    componentName: "Bảo hiểm người lao động",
    expression: "insurance_salary * employee_insurance_rate",
    calculationOrder: 200,
    isGrossComponent: false,
    isTaxable: false,
    isInsuranceBased: false,
    isDeduction: true,
  },
  {
    componentCode: "PIT",
    componentName: "Thuế TNCN",
    expression: "pit(pit_tax_base)",
    calculationOrder: 210,
    isGrossComponent: false,
    isTaxable: false,
    isInsuranceBased: false,
    isDeduction: true,
  },
];

export const createEmptyPayrollAdjustmentForm = (
  month: number,
  year: number,
): CreatePayrollAdjustmentRequest => ({
  employeeId: 0,
  adjustmentType: "ManualCorrection",
  recognizedMonth: month,
  recognizedYear: year,
  effectiveFromMonth: "",
  effectiveToMonth: "",
  amount: 0,
  isTaxable: true,
  isInsuranceBased: false,
  isDeduction: false,
  reason: "",
});
