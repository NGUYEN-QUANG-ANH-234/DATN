export interface SlaConfig {
  code: string; // Tương ứng với ModuleCode trên Backend
  value: string;
  unit: string;
}

export interface SlaUpdateRequest {
  moduleCode: string;
  value: string;
  unit: string;
}
