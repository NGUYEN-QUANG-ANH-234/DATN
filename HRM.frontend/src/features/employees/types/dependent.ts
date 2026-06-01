export type DependentRelation = 0 | 1 | 2 | 3;

export interface DependentDto {
  id: number;
  employeeId: number;
  fullName: string;
  relationship: DependentRelation;
  idNumber: string | null;
  taxDependentCode: string | null;
  birthDate: string | null;
  validFrom: string;
  validTo: string | null;
  isActive: boolean;
  evidenceUrl: string | null;
  note: string | null;
}

export interface PendingDependentRequest {
  id: number;
  employeeId: number;
  employeeName: string;
  employeeCode: string;
  dependentId: number | null;
  actionType: "CREATE" | "UPDATE" | "DEACTIVATE" | string;
  requestedDataJson: string;
  evidenceUrl: string | null;
  status: string;
  createdAt: string;
}

export interface DependentFormState {
  fullName: string;
  relationship: DependentRelation;
  idNumber: string;
  taxDependentCode: string;
  birthDate: string;
  validFrom: string;
  validTo: string;
  note: string;
  evidenceFile: File | null;
}
