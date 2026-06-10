export interface SourceCatalogItem {
  id: number;
  displayName: string;
  sourcePath: string;
  module: string;
  dataType: string;
  aggregationType: string;
  isPeriodBased: boolean;
  isActive: boolean;
}

export interface SalaryVariable {
  code: string;
  source: string;
  description?: string;
  isActive: boolean;
}

export interface BaseResponse<T> {
  success: boolean;
  message?: string;
  data?: T;
}
