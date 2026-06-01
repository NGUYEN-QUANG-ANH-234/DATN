import { formatMoney, formatNumber } from "../../../utils";
import type { SalarySlip } from "../types/payroll";

type SumKey = keyof Pick<
  SalarySlip,
  | "grossIncome"
  | "employeeInsuranceAmount"
  | "pitAmount"
  | "otherDeductions"
  | "netSalary"
  | "totalCompanyCost"
>;

export { formatMoney, formatNumber };

export const sumSalarySlips = (slips: SalarySlip[], key: SumKey) =>
  slips.reduce((total, slip) => total + (Number(slip[key]) || 0), 0);
