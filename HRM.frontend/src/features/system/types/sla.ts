export interface SlaConfig {
  code: string;
  moduleCode: string;
  displayName: string;
  moduleName: string;
  description: string;
  value: string;
  unit: string;
  isActive: boolean;
}

export interface SlaUpdateRequest {
  moduleCode: string;
  value: string;
  unit: string;
}

export interface SlaStatusRequest {
  isActive: boolean;
}
