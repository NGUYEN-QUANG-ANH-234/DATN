import axiosClient from "../../../core/api/axiosClient";
import type {
  ApiResponse,
  AppointmentConsentRequest,
  ApprovePromotionRequest,
  CancelPersonnelChangeRequest,
  CreateConvertOfficialRequest,
  CreateSeniorAppointmentRequest,
  CreateDismissalRequest,
  CreatePromotionRequest,
  CurrentManagerOpinionRequest,
  DirectorApproveDismissalRequest,
  DirectorApproveResignationRequest,
  DirectorApproveTransferRequest,
  DismissalEmployeeExplanationRequest,
  EmployeeConsentRequest,
  ExecutePersonnelChangeRequest,
  HrContractFlowRequest,
  HrReviewResignationRequest,
  HrSelectEmployeeRequest,
  InternalTransferDemandRequest,
  IssueAppointmentDecisionRequest,
  IssueTransferDecisionRequest,
  ManagerReviewResignationRequest,
  NotifyEmployeeDismissalRequest,
  PersonnelChangeDetail,
  PersonnelChangeListItem,
  PersonnelChangeRiskSummary,
  PersonnelChangeTimelineItem,
  PersonnelChangeType,
  PersonnelChangeWorkflowKind,
  SubmitResignationRequest,
} from "../types/personnelChange";

const ENDPOINT = "/personnel-changes";
const INTERNAL_TRANSFER_ENDPOINT = `${ENDPOINT}/internal-transfers`;
const SENIOR_APPOINTMENT_ENDPOINT = `${ENDPOINT}/senior-appointments`;
const DISMISSAL_ENDPOINT = `${ENDPOINT}/dismissals`;
const PROMOTION_ENDPOINT = `${ENDPOINT}/promotions`;
const VOLUNTARY_TERMINATION_ENDPOINT = `${ENDPOINT}/voluntary-terminations`;

export const personnelChangeApi = {
  getList: async (params?: {
    changeType?: PersonnelChangeType;
    status?: number;
    employeeId?: number;
  }) =>
    axiosClient.get<ApiResponse<PersonnelChangeListItem[]>, ApiResponse<PersonnelChangeListItem[]>>(
      ENDPOINT,
      { params },
    ),

  getDetail: async (id: number) =>
    axiosClient.get<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${ENDPOINT}/${id}`,
    ),

  getRiskSummary: async (id: number) =>
    axiosClient.get<ApiResponse<PersonnelChangeRiskSummary>, ApiResponse<PersonnelChangeRiskSummary>>(
      `${ENDPOINT}/${id}/risk-summary`,
    ),

  getTimeline: async (id: number) =>
    axiosClient.get<ApiResponse<PersonnelChangeTimelineItem[]>, ApiResponse<PersonnelChangeTimelineItem[]>>(
      `${ENDPOINT}/${id}/timeline`,
    ),

  cancel: async (id: number, payload: CancelPersonnelChangeRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${ENDPOINT}/${id}/cancel`,
      payload,
    ),

  createInternalTransferDemand: async (payload: InternalTransferDemandRequest) =>
    axiosClient.post<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${INTERNAL_TRANSFER_ENDPOINT}/demands`,
      payload,
    ),

  hrSelectEmployee: async (id: number, payload: HrSelectEmployeeRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${INTERNAL_TRANSFER_ENDPOINT}/${id}/hr-select-employee`,
      payload,
    ),

  submitCurrentManagerOpinion: async (id: number, payload: CurrentManagerOpinionRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${INTERNAL_TRANSFER_ENDPOINT}/${id}/current-manager-opinion`,
      payload,
    ),

  submitEmployeeConsent: async (id: number, payload: EmployeeConsentRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${INTERNAL_TRANSFER_ENDPOINT}/${id}/employee-consent`,
      payload,
    ),

  directorApproveTransfer: async (id: number, payload: DirectorApproveTransferRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${INTERNAL_TRANSFER_ENDPOINT}/${id}/director-approve-transfer`,
      payload,
    ),

  issueTransferDecision: async (id: number, payload: IssueTransferDecisionRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${INTERNAL_TRANSFER_ENDPOINT}/${id}/issue-transfer-decision`,
      payload,
    ),

  execute: async (
    id: number,
    payload: ExecutePersonnelChangeRequest,
    workflow: PersonnelChangeWorkflowKind = "internal-transfer",
  ) => {
    if (workflow === "promotion") return personnelChangeApi.executePromotion(id, payload);
    if (workflow === "senior-appointment") return personnelChangeApi.executeSeniorAppointment(id, payload);
    if (workflow === "termination") return personnelChangeApi.executeResignation(id, payload);
    if (workflow === "dismissal") return personnelChangeApi.executeDismissal(id, payload);

    return axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${INTERNAL_TRANSFER_ENDPOINT}/${id}/execute`,
      payload,
    );
  },

  createSeniorAppointment: async (payload: CreateSeniorAppointmentRequest) =>
    axiosClient.post<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      SENIOR_APPOINTMENT_ENDPOINT,
      payload,
    ),

  submitAppointmentConsent: async (id: number, payload: AppointmentConsentRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${SENIOR_APPOINTMENT_ENDPOINT}/${id}/appointment-consent`,
      payload,
    ),

  startHrContractFlow: async (id: number, payload: HrContractFlowRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${SENIOR_APPOINTMENT_ENDPOINT}/${id}/hr-contract-flow`,
      payload,
    ),

  issueAppointmentDecision: async (id: number, payload: IssueAppointmentDecisionRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${SENIOR_APPOINTMENT_ENDPOINT}/${id}/issue-appointment-decision`,
      payload,
    ),

  executeSeniorAppointment: async (id: number, payload: ExecutePersonnelChangeRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${SENIOR_APPOINTMENT_ENDPOINT}/${id}/execute`,
      payload,
    ),

  createDismissal: async (payload: CreateDismissalRequest) =>
    axiosClient.post<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      DISMISSAL_ENDPOINT,
      payload,
    ),

  notifyDismissalEmployee: async (id: number, payload: NotifyEmployeeDismissalRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${DISMISSAL_ENDPOINT}/${id}/notify-employee`,
      payload,
    ),

  submitDismissalExplanation: async (id: number, payload: DismissalEmployeeExplanationRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${DISMISSAL_ENDPOINT}/${id}/employee-explanation`,
      payload,
    ),

  directorApproveDismissal: async (id: number, payload: DirectorApproveDismissalRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${DISMISSAL_ENDPOINT}/${id}/director-approve-dismissal`,
      payload,
    ),

  executeDismissal: async (id: number, payload: ExecutePersonnelChangeRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${DISMISSAL_ENDPOINT}/${id}/execute`,
      payload,
    ),

  createPromotion: async (payload: CreatePromotionRequest) =>
    axiosClient.post<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      PROMOTION_ENDPOINT,
      payload,
    ),

  createConvertOfficial: async (payload: CreateConvertOfficialRequest) =>
    axiosClient.post<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${PROMOTION_ENDPOINT}/convert-official`,
      payload,
    ),

  hrReviewPromotion: async (id: number, payload: ApprovePromotionRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${PROMOTION_ENDPOINT}/${id}/hr-review`,
      payload,
    ),

  directorApprovePromotion: async (id: number, payload: ApprovePromotionRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${PROMOTION_ENDPOINT}/${id}/director-approve`,
      payload,
    ),

  executePromotion: async (id: number, payload: ExecutePersonnelChangeRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${PROMOTION_ENDPOINT}/${id}/execute`,
      payload,
    ),

  submitResignation: async (payload: SubmitResignationRequest) =>
    axiosClient.post<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      VOLUNTARY_TERMINATION_ENDPOINT,
      payload,
    ),

  managerReviewResignation: async (id: number, payload: ManagerReviewResignationRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${VOLUNTARY_TERMINATION_ENDPOINT}/${id}/manager-review`,
      payload,
    ),

  hrReviewResignation: async (id: number, payload: HrReviewResignationRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${VOLUNTARY_TERMINATION_ENDPOINT}/${id}/hr-review`,
      payload,
    ),

  directorApproveResignation: async (id: number, payload: DirectorApproveResignationRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${VOLUNTARY_TERMINATION_ENDPOINT}/${id}/director-approve`,
      payload,
    ),

  executeResignation: async (id: number, payload: ExecutePersonnelChangeRequest) =>
    axiosClient.patch<ApiResponse<PersonnelChangeDetail>, ApiResponse<PersonnelChangeDetail>>(
      `${VOLUNTARY_TERMINATION_ENDPOINT}/${id}/execute`,
      payload,
    ),

  hrReview: async (
    id: number,
    payload: ApprovePromotionRequest | HrReviewResignationRequest | HrSelectEmployeeRequest,
    workflow: PersonnelChangeWorkflowKind,
  ) => {
    if (workflow === "promotion") return personnelChangeApi.hrReviewPromotion(id, payload as ApprovePromotionRequest);
    if (workflow === "termination") return personnelChangeApi.hrReviewResignation(id, payload as HrReviewResignationRequest);
    if (workflow === "internal-transfer") return personnelChangeApi.hrSelectEmployee(id, payload as HrSelectEmployeeRequest);
    throw new Error(`HR review is not configured for ${workflow}.`);
  },

  directorReview: async (
    id: number,
    payload:
      | ApprovePromotionRequest
      | DirectorApproveTransferRequest
      | DirectorApproveDismissalRequest
      | DirectorApproveResignationRequest,
    workflow: PersonnelChangeWorkflowKind,
  ) => {
    if (workflow === "promotion") return personnelChangeApi.directorApprovePromotion(id, payload as ApprovePromotionRequest);
    if (workflow === "internal-transfer") return personnelChangeApi.directorApproveTransfer(id, payload as DirectorApproveTransferRequest);
    if (workflow === "dismissal") return personnelChangeApi.directorApproveDismissal(id, payload as DirectorApproveDismissalRequest);
    if (workflow === "termination") return personnelChangeApi.directorApproveResignation(id, payload as DirectorApproveResignationRequest);
    throw new Error(`Director review is not configured for ${workflow}.`);
  },

  employeeConsent: async (
    id: number,
    payload: EmployeeConsentRequest | AppointmentConsentRequest,
    workflow: PersonnelChangeWorkflowKind,
  ) => {
    if (workflow === "internal-transfer") return personnelChangeApi.submitEmployeeConsent(id, payload as EmployeeConsentRequest);
    if (workflow === "senior-appointment") return personnelChangeApi.submitAppointmentConsent(id, payload as AppointmentConsentRequest);
    throw new Error(`Employee consent is not configured for ${workflow}.`);
  },

  employeeExplanation: async (
    id: number,
    payload: DismissalEmployeeExplanationRequest,
    workflow: PersonnelChangeWorkflowKind,
  ) => {
    if (workflow === "dismissal") return personnelChangeApi.submitDismissalExplanation(id, payload);
    throw new Error(`Employee explanation is not configured for ${workflow}.`);
  },

  issueDecision: async (
    id: number,
    payload: IssueTransferDecisionRequest | IssueAppointmentDecisionRequest,
    workflow: PersonnelChangeWorkflowKind,
  ) => {
    if (workflow === "internal-transfer") return personnelChangeApi.issueTransferDecision(id, payload as IssueTransferDecisionRequest);
    if (workflow === "senior-appointment") return personnelChangeApi.issueAppointmentDecision(id, payload as IssueAppointmentDecisionRequest);
    throw new Error(`Issue decision is not configured for ${workflow}.`);
  },

  executePersonnelChange: async (
    id: number,
    payload: ExecutePersonnelChangeRequest,
    workflow: PersonnelChangeWorkflowKind,
  ) => personnelChangeApi.execute(id, payload, workflow),
};
