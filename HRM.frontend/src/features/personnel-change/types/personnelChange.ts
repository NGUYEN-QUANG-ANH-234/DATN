export type ApiResponse<T> = {
  success?: boolean;
  message?: string;
  data: T;
};

export type PersonnelChangeEmployeeOption = {
  id: number;
  employeeCode: string;
  fullName: string;
  departmentId?: number | null;
  departmentName?: string | null;
  positionId?: number | null;
  positionName?: string | null;
  jobLevelId?: number | null;
  jobLevelName?: string | null;
  managerId?: number | null;
  managerName?: string | null;
  status?: string | null;
  employeeType?: string | null;
};

export type PersonnelChangeDepartmentOption = {
  id: number;
  deptCode: string;
  deptName: string;
  parentDeptId?: number | null;
  managerId?: number | null;
  managerName?: string | null;
};

export type PersonnelChangePositionOption = {
  id: number;
  title: string;
  jobLevel: number;
};

export type PersonnelChangeJobLevelOption = {
  id: number;
  code: string;
  name: string;
  rankOrder: number;
  isManagementLevel: boolean;
};

export type PersonnelChangePenaltyOption = {
  id: number;
  period: string;
  ruleCode: string;
  reason?: string | null;
  penaltyPoint: number;
  severity: string;
  status: string;
  occurredAt?: string | null;
  affectsPersonnelDecision: boolean;
};

export type PersonnelChangePerformanceReviewOption = {
  id: number;
  period: string;
  totalScore: number;
  finalRating?: string | null;
  status: string;
  finalizedAt?: string | null;
  createdAt: string;
};

export type PersonnelChangeContractOption = {
  id: number;
  contractNumber: string;
  contractType: string;
  status: string;
  startDate: string;
  endDate?: string | null;
  basicSalary: number;
  insuranceSalary: number;
};

export type PersonnelChangeEvidenceUploadResult = {
  filePath: string;
  fileName: string;
  size: number;
};

export type PersonnelChangeWorkflowKind =
  | "promotion"
  | "senior-appointment"
  | "termination"
  | "dismissal"
  | "internal-transfer";

export const PersonnelChangeType = {
  ConvertToOfficial: 0,
  Promotion: 1,
  SeniorAppointment: 2,
  VoluntaryTermination: 3,
  Dismissal: 4,
  InternalTransfer: 5,
} as const;

export type PersonnelChangeType = (typeof PersonnelChangeType)[keyof typeof PersonnelChangeType];

export const PersonnelChangeStatus = {
  Draft: 0,
  PendingHRReview: 1,
  PendingEmployeeConsent: 2,
  EmployeeDeclined: 3,
  PendingDirectorApproval: 4,
  ApprovedByDirector: 5,
  PendingContractFlow: 6,
  ContractNegotiating: 7,
  ContractAccepted: 8,
  ContractRejected: 9,
  PendingDecisionIssuance: 10,
  ReadyToExecute: 11,
  Completed: 12,
  Rejected: 13,
  Cancelled: 14,
  Escalated: 15,
  PendingCurrentManagerOpinion: 16,
  PendingEmployeeNotification: 17,
  PendingEmployeeExplanation: 18,
  PendingManagerReview: 19,
} as const;

export type PersonnelChangeStatus = (typeof PersonnelChangeStatus)[keyof typeof PersonnelChangeStatus];

export const PersonnelChangeContractFlowType = {
  None: 0,
  NewContract: 1,
  ContractRenewal: 2,
  ContractAddendum: 3,
  ContractTermination: 4,
} as const;

export type PersonnelChangeContractFlowType =
  (typeof PersonnelChangeContractFlowType)[keyof typeof PersonnelChangeContractFlowType];

export const PersonnelChangePromotionType = {
  ConvertToOfficial: 0,
  PositionPromotion: 1,
  JobLevelPromotion: 2,
} as const;

export type PersonnelChangePromotionType =
  (typeof PersonnelChangePromotionType)[keyof typeof PersonnelChangePromotionType];

export const EmployeeType = {
  Intern: 0,
  Official: 1,
  Probation: 2,
  PartTime: 3,
  Contractual: 4,
} as const;

export type EmployeeType = (typeof EmployeeType)[keyof typeof EmployeeType];

export type PersonnelChangeListItem = {
  id: number;
  employeeId?: number | null;
  employeeCode?: string | null;
  employeeName?: string | null;
  changeType: PersonnelChangeType;
  promotionType?: PersonnelChangePromotionType | null;
  status: PersonnelChangeStatus;
  requestedAt: string;
  requestedByAccountId: number;
  requestedByName?: string | null;
  effectiveDate?: string | null;
  reason?: string | null;
  requiresEmployeeConsent: boolean;
  requiresContractFlow: boolean;
  contractFlowType?: PersonnelChangeContractFlowType | null;
  requiresDirectorApproval: boolean;
};

export type PersonnelChangeContractFlowLink = {
  id: number;
  personnelChangeRequestId: number;
  contractId?: number | null;
  contractRequestId?: number | null;
  contractAddendumId?: number | null;
  contractFlowType: PersonnelChangeContractFlowType;
  status?: string | null;
  createdAt: string;
  completedAt?: string | null;
};

export type PersonnelChangeDetail = PersonnelChangeListItem & {
  currentDepartmentId?: number | null;
  currentDepartmentName?: string | null;
  currentPositionId?: number | null;
  currentPositionName?: string | null;
  currentManagerId?: number | null;
  currentManagerName?: string | null;
  currentJobLevelId?: number | null;
  currentJobLevelName?: string | null;
  currentEmployeeType?: EmployeeType | null;
  newDepartmentId?: number | null;
  newDepartmentName?: string | null;
  newPositionId?: number | null;
  newPositionName?: string | null;
  newManagerId?: number | null;
  newManagerName?: string | null;
  newJobLevelId?: number | null;
  newJobLevelName?: string | null;
  newEmployeeType?: EmployeeType | null;
  hrNote?: string | null;
  directorNote?: string | null;
  relatedContractId?: number | null;
  relatedContractRequestId?: number | null;
  relatedContractAddendumId?: number | null;
  sourcePenaltyRecordId?: number | null;
  sourcePerformanceReviewId?: number | null;
  employeeConsentNote?: string | null;
  employeeNotifiedAt?: string | null;
  responseDeadlineAt?: string | null;
  evidenceFilePath?: string | null;
  managerNote?: string | null;
  employeeExplanation?: string | null;
  employeeExplanationAt?: string | null;
  lockAccountOnExecution?: boolean;
  accountLockedAt?: string | null;
  requiresFinalSettlement?: boolean;
  relatedFinalSettlementId?: number | null;
  contractFlowStatus?: string | null;
  decisionNumber?: string | null;
  decisionFilePath?: string | null;
  decisionIssuedAt?: string | null;
  completedAt?: string | null;
  rejectedReason?: string | null;
  histories?: PersonnelChangeTimelineItem[];
  contractLinks?: PersonnelChangeContractFlowLink[];
};

export type PersonnelChangeRequest = PersonnelChangeDetail;

export type PersonnelChangeTimelineItem = {
  id: number;
  requestId: number;
  action: string;
  oldStatus?: PersonnelChangeStatus | null;
  newStatus?: PersonnelChangeStatus | null;
  actorAccountId?: number | null;
  actorName?: string | null;
  note?: string | null;
  createdAt: string;
};

export type PersonnelChangeRiskSummary = {
  requestId: number;
  employee?: {
    id: number;
    employeeCode?: string | null;
    fullName?: string | null;
    departmentName?: string | null;
    positionName?: string | null;
    jobLevelName?: string | null;
    employeeType?: string | null;
    status?: string | null;
    joinedDate?: string | null;
  } | null;
  currentContract?: {
    id: number;
    contractNumber?: string | null;
    contractType?: string | null;
    status?: string | null;
    startDate: string;
    endDate?: string | null;
    basicSalary: number;
    insuranceSalary: number;
  } | null;
  latestPerformance?: {
    id: number;
    period: string;
    totalScore: number;
    finalRating?: string | null;
    status?: string | null;
  } | null;
  penaltySummary?: {
    totalRecords: number;
    personnelImpactRecords: number;
    totalPenaltyPoint: number;
  };
  seniority?: {
    joinedDate?: string | null;
    totalMonths: number;
    totalYears: number;
  };
  generatedAt: string;
};

export type InternalTransferDemandRequest = {
  requestedDepartmentId: number;
  requestedPositionId?: number | null;
  requestedManagerId?: number | null;
  reason?: string | null;
  urgencyLevel?: string | null;
  expectedEffectiveDate?: string | null;
  requiredSkills?: string | null;
};

export type HrSelectEmployeeRequest = {
  employeeId: number;
  newDepartmentId?: number | null;
  newPositionId?: number | null;
  newManagerId?: number | null;
  newJobLevelId?: number | null;
  requiresContractAddendum: boolean;
  note?: string | null;
};

export type CurrentManagerOpinionRequest = {
  isApproved: boolean;
  opinion?: string | null;
};

export type EmployeeConsentRequest = {
  isAccepted: boolean;
  note?: string | null;
};

export type DirectorApproveTransferRequest = {
  isApproved: boolean;
  note?: string | null;
};

export type IssueTransferDecisionRequest = {
  decisionNumber: string;
  decisionFilePath?: string | null;
  decisionIssuedAt?: string | null;
  note?: string | null;
};

export type ExecutePersonnelChangeRequest = {
  completedAt?: string | null;
  note?: string | null;
};

export type CancelPersonnelChangeRequest = {
  reason?: string | null;
};

export type CreateSeniorAppointmentRequest = {
  employeeId: number;
  newDepartmentId?: number | null;
  newPositionId: number;
  newJobLevelId?: number | null;
  reportsToManagerId?: number | null;
  isDepartmentManager: boolean;
  reason?: string | null;
  effectiveDate?: string | null;
  relatedContractId?: number | null;
  contractFlowType: PersonnelChangeContractFlowType;
};

export type AppointmentConsentRequest = {
  isAccepted: boolean;
  note?: string | null;
};

export type HrContractFlowRequest = {
  contractFlowType: PersonnelChangeContractFlowType;
  relatedContractId?: number | null;
  note?: string | null;
};

export type IssueAppointmentDecisionRequest = {
  decisionNumber: string;
  decisionFilePath?: string | null;
  decisionIssuedAt?: string | null;
  note?: string | null;
};

export type CreateDismissalRequest = {
  employeeId: number;
  sourcePenaltyRecordId: number;
  reason?: string | null;
  evidenceFilePath?: string | null;
  hrNote?: string | null;
  managerNote?: string | null;
  responseDeadlineAt?: string | null;
  effectiveDate?: string | null;
  relatedContractId?: number | null;
  lockAccountOnExecution: boolean;
  requiresFinalSettlement: boolean;
};

export type NotifyEmployeeDismissalRequest = {
  hrNote?: string | null;
  evidenceFilePath?: string | null;
  employeeNotifiedAt?: string | null;
  responseDeadlineAt?: string | null;
  note?: string | null;
};

export type DismissalEmployeeExplanationRequest = {
  explanation: string;
  evidenceFilePath?: string | null;
};

export type DirectorApproveDismissalRequest = {
  isApproved: boolean;
  note?: string | null;
};

export type SubmitResignationRequest = {
  employeeId: number;
  expectedLastWorkingDate: string;
  reason?: string | null;
  employeeNote?: string | null;
};

export type ManagerReviewResignationRequest = {
  isApproved: boolean;
  note?: string | null;
};

export type HrReviewResignationRequest = {
  isApproved: boolean;
  note?: string | null;
  relatedContractId?: number | null;
  requiresFinalSettlement: boolean;
  lockAccountAfterEffectiveDate: boolean;
};

export type DirectorApproveResignationRequest = {
  isApproved: boolean;
  note?: string | null;
};

export type CreatePromotionRequest = {
  employeeId: number;
  promotionType: PersonnelChangePromotionType;
  newPositionId?: number | null;
  newJobLevelId?: number | null;
  newEmployeeType?: EmployeeType | null;
  effectiveDate?: string | null;
  reason?: string | null;
  sourcePerformanceReviewId?: number | null;
  requiresContractFlow: boolean;
  contractFlowType: PersonnelChangeContractFlowType;
  relatedContractId?: number | null;
};

export type CreateConvertOfficialRequest = {
  employeeId: number;
  newPositionId?: number | null;
  newJobLevelId?: number | null;
  newEmployeeType: EmployeeType;
  effectiveDate?: string | null;
  reason?: string | null;
  sourcePerformanceReviewId?: number | null;
  requiresContractFlow: boolean;
  contractFlowType: PersonnelChangeContractFlowType;
  relatedContractId?: number | null;
};

export type ApprovePromotionRequest = {
  isApproved: boolean;
  note?: string | null;
  hrAssignedAccountId?: number | null;
  requiresContractFlow?: boolean | null;
  contractFlowType?: PersonnelChangeContractFlowType | null;
  relatedContractId?: number | null;
};

const statusLabels: Record<number, string> = Object.fromEntries(
  Object.entries(PersonnelChangeStatus).map(([key, value]) => [value, key]),
) as Record<number, string>;

export const getPersonnelChangeStatusLabel = (status?: PersonnelChangeStatus | null) => {
  if (status === null || status === undefined) return "Unknown";
  return statusLabels[status] ?? String(status);
};
