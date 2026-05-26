// Thêm mới Interface này
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

export interface CreateSourceCatalogPayload {
  displayName: string;
  sourcePath: string;
  module: string;
  dataType: string;
  aggregationType: string;
  isPeriodBased: boolean;
  isActive: boolean;
}

// Giữ nguyên các phần cũ
export interface SalaryVariable {
  code: string;
  source: string;
  description?: string;
}

export interface BaseResponse<T> {
  success: boolean;
  message?: string;
  data?: T;
}
