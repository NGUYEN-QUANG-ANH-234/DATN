export type DashboardSeverity = "neutral" | "success" | "info" | "warning" | "danger";

export interface ApiResponse<T> {
  success?: boolean;
  Success?: boolean;
  data?: T;
  Data?: T;
  message?: string;
  Message?: string;
}

export interface DashboardResponse {
  role: string;
  scope: string;
  month: number;
  year: number;
  generatedAt: string;
  widgets: DashboardWidget[];
  sections: DashboardSection[];
  quickActions: DashboardAction[];
  risks: DashboardRiskItem[];
}

export interface DashboardWidget {
  id: string;
  title: string;
  value: string;
  subtitle?: string | null;
  severity: DashboardSeverity | string;
  scope: string;
  order: number;
  drilldown?: DashboardDrilldownRef | null;
  metrics: DashboardMetric[];
  actions: DashboardAction[];
}

export interface DashboardMetric {
  label: string;
  value: string;
  unit?: string | null;
  numericValue?: number | null;
  severity: DashboardSeverity | string;
}

export interface DashboardAction {
  label: string;
  route: string;
  actionType: string;
  icon?: string | null;
}

export interface DashboardDrilldownRef {
  type: DashboardDrilldownType | string;
  scope: string;
  filters: Record<string, string | null | undefined>;
}

export interface DashboardSection {
  id: string;
  title: string;
  subtitle?: string | null;
  type: string;
  order: number;
  table?: DashboardTable | null;
  widgets: DashboardWidget[];
}

export interface DashboardTable {
  columns: string[];
  rows: Array<Record<string, string | null | undefined>>;
}

export interface DashboardRiskItem {
  id: string;
  title: string;
  description?: string | null;
  severity: DashboardSeverity | string;
  route?: string | null;
}

export interface DashboardDrilldown {
  type: string;
  scope: string;
  title: string;
  metrics: DashboardMetric[];
  table: DashboardTable;
}

export type DashboardDrilldownType =
  | "payroll-slip"
  | "payroll-summary"
  | "payroll-preflight"
  | "approval-list"
  | "approval-detail"
  | "attendance-reconciliation"
  | "recruitment-pipeline"
  | "personnel-change-impact"
  | "contract-lifecycle"
  | "profile-completeness"
  | "system-health"
  | "audit-log";
