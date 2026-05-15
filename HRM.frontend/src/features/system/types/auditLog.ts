export interface AuditLog {
  id: number;
  accountId: number | null;
  actionType: string;
  tableName: string;
  oldValues: string | null;
  newValues: string | null;
  timestamp: string;
}

export interface AuditLogFilter {
  accountId?: number | "";
  module?: string;
  startDate?: string;
  endDate?: string;
}
