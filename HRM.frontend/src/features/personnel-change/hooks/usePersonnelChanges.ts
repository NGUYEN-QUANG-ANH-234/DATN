import { useCallback, useEffect, useState } from "react";
import { useNotification } from "../../../core/context/NotificationContext";
import { personnelChangeApi } from "../api/personnelChangeApi";
import {
  PersonnelChangeType,
  type ApprovePromotionRequest,
  type AppointmentConsentRequest,
  type CancelPersonnelChangeRequest,
  type CreateConvertOfficialRequest,
  type CreateDismissalRequest,
  type CreatePromotionRequest,
  type CreateSeniorAppointmentRequest,
  type CurrentManagerOpinionRequest,
  type DirectorApproveDismissalRequest,
  type DirectorApproveResignationRequest,
  type DirectorApproveTransferRequest,
  type DismissalEmployeeExplanationRequest,
  type EmployeeConsentRequest,
  type ExecutePersonnelChangeRequest,
  type HrContractFlowRequest,
  type HrReviewResignationRequest,
  type HrSelectEmployeeRequest,
  type InternalTransferDemandRequest,
  type IssueAppointmentDecisionRequest,
  type IssueTransferDecisionRequest,
  type ManagerReviewResignationRequest,
  type NotifyEmployeeDismissalRequest,
  type PersonnelChangeDetail,
  type PersonnelChangeListItem,
  type PersonnelChangeRiskSummary,
  type PersonnelChangeTimelineItem,
  type SubmitResignationRequest,
} from "../types/personnelChange";

export const usePersonnelChanges = (
  defaultChangeType: PersonnelChangeType = PersonnelChangeType.InternalTransfer,
) => {
  const { triggerAlert } = useNotification();
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [records, setRecords] = useState<PersonnelChangeListItem[]>([]);
  const [selected, setSelected] = useState<PersonnelChangeDetail | null>(null);
  const [riskSummary, setRiskSummary] = useState<PersonnelChangeRiskSummary | null>(null);
  const [timeline, setTimeline] = useState<PersonnelChangeTimelineItem[]>([]);

  const loadByType = useCallback(async (changeType: PersonnelChangeType = defaultChangeType) => {
    setLoading(true);
    try {
      const response = await personnelChangeApi.getList({
        changeType,
      });
      setRecords(response.data ?? []);
    } catch (error) {
      console.error(error);
      triggerAlert("error", "Khong tai duoc ho so bien dong", getErrorMessage(error));
    } finally {
      setLoading(false);
    }
  }, [defaultChangeType, triggerAlert]);

  const loadPromotionOfficial = useCallback(async () => {
    setLoading(true);
    try {
      const [promotionRes, convertOfficialRes] = await Promise.all([
        personnelChangeApi.getList({ changeType: PersonnelChangeType.Promotion }),
        personnelChangeApi.getList({ changeType: PersonnelChangeType.ConvertToOfficial }),
      ]);
      const combined = [...(promotionRes.data ?? []), ...(convertOfficialRes.data ?? [])].sort(
        (left, right) =>
          new Date(right.requestedAt).getTime() - new Date(left.requestedAt).getTime(),
      );
      setRecords(combined);
    } catch (error) {
      console.error(error);
      triggerAlert("error", "Khong tai duoc ho so thang tien", getErrorMessage(error));
    } finally {
      setLoading(false);
    }
  }, [triggerAlert]);

  const loadDetail = useCallback(async (id: number) => {
    setLoading(true);
    try {
      const [detailRes, riskRes, timelineRes] = await Promise.all([
        personnelChangeApi.getDetail(id),
        personnelChangeApi.getRiskSummary(id),
        personnelChangeApi.getTimeline(id),
      ]);
      setSelected(detailRes.data);
      setRiskSummary(riskRes.data);
      setTimeline(timelineRes.data ?? []);
    } catch (error) {
      console.error(error);
      triggerAlert("error", "Khong tai duoc chi tiet", getErrorMessage(error));
    } finally {
      setLoading(false);
    }
  }, [triggerAlert]);

  useEffect(() => {
    if (defaultChangeType === PersonnelChangeType.Promotion) {
      void loadPromotionOfficial();
      return;
    }

    void loadByType(defaultChangeType);
  }, [defaultChangeType, loadByType, loadPromotionOfficial]);

  const runAction = async (
    action: () => Promise<unknown>,
    successTitle: string,
    successMessage: string,
    reloadRecords: () => Promise<void> = () => loadByType(defaultChangeType),
  ) => {
    setSaving(true);
    try {
      await action();
      triggerAlert("success", successTitle, successMessage);
      await reloadRecords();
      if (selected?.id) await loadDetail(selected.id);
      return true;
    } catch (error) {
      console.error(error);
      triggerAlert("error", successTitle, getErrorMessage(error));
      return false;
    } finally {
      setSaving(false);
    }
  };

  return {
    loading,
    saving,
    records,
    selected,
    riskSummary,
    timeline,
    loadInternalTransfers: () => loadByType(PersonnelChangeType.InternalTransfer),
    loadSeniorAppointments: () => loadByType(PersonnelChangeType.SeniorAppointment),
    loadDismissals: () => loadByType(PersonnelChangeType.Dismissal),
    loadTerminations: () => loadByType(PersonnelChangeType.VoluntaryTermination),
    loadPromotionOfficial,
    loadByType,
    loadDetail,
    createInternalTransferDemand: (payload: InternalTransferDemandRequest) =>
      runAction(
        () => personnelChangeApi.createInternalTransferDemand(payload),
        "Da tao demand",
        "Demand thuyen chuyen noi bo da duoc ghi nhan.",
      ),
    hrSelectEmployee: (id: number, payload: HrSelectEmployeeRequest) =>
      runAction(
        () => personnelChangeApi.hrSelectEmployee(id, payload),
        "Da chon nhan su",
        "Ho so da chuyen sang buoc lay y kien quan ly hien tai.",
      ),
    submitCurrentManagerOpinion: (id: number, payload: CurrentManagerOpinionRequest) =>
      runAction(
        () => personnelChangeApi.submitCurrentManagerOpinion(id, payload),
        "Da gui y kien quan ly",
        "Ho so da duoc cap nhat theo y kien quan ly hien tai.",
      ),
    submitEmployeeConsent: (id: number, payload: EmployeeConsentRequest) =>
      runAction(
        () => personnelChangeApi.submitEmployeeConsent(id, payload),
        "Da gui phan hoi nhan vien",
        "Phan hoi cua nhan vien da duoc ghi nhan.",
      ),
    directorApproveTransfer: (id: number, payload: DirectorApproveTransferRequest) =>
      runAction(
        () => personnelChangeApi.directorApproveTransfer(id, payload),
        "Da xu ly phe duyet",
        "Quyet dinh cua Director da duoc cap nhat.",
      ),
    issueTransferDecision: (id: number, payload: IssueTransferDecisionRequest) =>
      runAction(
        () => personnelChangeApi.issueTransferDecision(id, payload),
        "Da ban hanh quyet dinh",
        "Ho so thuyen chuyen da san sang thuc thi.",
      ),
    executeInternalTransfer: (id: number, payload: ExecutePersonnelChangeRequest) =>
      runAction(
        () => personnelChangeApi.execute(id, payload),
        "Da thuc thi thuyen chuyen",
        "Thong tin to chuc cua nhan su da duoc cap nhat.",
      ),
    createSeniorAppointment: (payload: CreateSeniorAppointmentRequest) =>
      runAction(
        () => personnelChangeApi.createSeniorAppointment(payload),
        "Da tao bo nhiem",
        "Ho so bo nhiem nhan su cap cao da duoc ghi nhan.",
      ),
    submitAppointmentConsent: (id: number, payload: AppointmentConsentRequest) =>
      runAction(
        () => personnelChangeApi.submitAppointmentConsent(id, payload),
        "Da gui phan hoi",
        "Phan hoi bo nhiem cua nhan vien da duoc ghi nhan.",
      ),
    startHrContractFlow: (id: number, payload: HrContractFlowRequest) =>
      runAction(
        () => personnelChangeApi.startHrContractFlow(id, payload),
        "Da tao contract flow",
        "Ho so bo nhiem da duoc chuyen sang Module 3 xu ly hop dong/phu luc.",
      ),
    issueAppointmentDecision: (id: number, payload: IssueAppointmentDecisionRequest) =>
      runAction(
        () => personnelChangeApi.issueAppointmentDecision(id, payload),
        "Da ban hanh quyet dinh",
        "Ho so bo nhiem da san sang thuc thi.",
      ),
    executeSeniorAppointment: (id: number, payload: ExecutePersonnelChangeRequest) =>
      runAction(
        () => personnelChangeApi.executeSeniorAppointment(id, payload),
        "Da thuc thi bo nhiem",
        "Chuc danh, cap bac va vai tro phong ban cua nhan su da duoc cap nhat.",
      ),
    createDismissal: (payload: CreateDismissalRequest) =>
      runAction(
        () => personnelChangeApi.createDismissal(payload),
        "Da tao ho so sa thai",
        "Ho so sa thai/ky luat da duoc ghi nhan.",
      ),
    notifyDismissalEmployee: (id: number, payload: NotifyEmployeeDismissalRequest) =>
      runAction(
        () => personnelChangeApi.notifyDismissalEmployee(id, payload),
        "Da thong bao nhan vien",
        "Ho so da chuyen sang buoc nhan giai trinh.",
      ),
    submitDismissalExplanation: (id: number, payload: DismissalEmployeeExplanationRequest) =>
      runAction(
        () => personnelChangeApi.submitDismissalExplanation(id, payload),
        "Da gui giai trinh",
        "Giai trinh cua nhan vien da duoc ghi nhan.",
      ),
    directorApproveDismissal: (id: number, payload: DirectorApproveDismissalRequest) =>
      runAction(
        () => personnelChangeApi.directorApproveDismissal(id, payload),
        "Da xu ly phe duyet",
        "Quyet dinh cua Director da duoc cap nhat.",
      ),
    executeDismissal: (id: number, payload: ExecutePersonnelChangeRequest) =>
      runAction(
        () => personnelChangeApi.executeDismissal(id, payload),
        "Da thuc thi sa thai",
        "Trang thai nhan su, tai khoan va final settlement da duoc cap nhat neu can.",
      ),
    createPromotion: (payload: CreatePromotionRequest) =>
      runAction(
        () => personnelChangeApi.createPromotion(payload),
        "Da tao ho so thang tien",
        "Ho so thang tien da duoc ghi nhan.",
        loadPromotionOfficial,
      ),
    createConvertOfficial: (payload: CreateConvertOfficialRequest) =>
      runAction(
        () => personnelChangeApi.createConvertOfficial(payload),
        "Da tao ho so chuyen chinh thuc",
        "Ho so chuyen chinh thuc da duoc ghi nhan.",
        loadPromotionOfficial,
      ),
    hrReviewPromotion: (id: number, payload: ApprovePromotionRequest) =>
      runAction(
        () => personnelChangeApi.hrReviewPromotion(id, payload),
        "Da HR review",
        "Ho so da chuyen sang buoc Director phe duyet hoac bi tu choi.",
        loadPromotionOfficial,
      ),
    directorApprovePromotion: (id: number, payload: ApprovePromotionRequest) =>
      runAction(
        () => personnelChangeApi.directorApprovePromotion(id, payload),
        "Da xu ly phe duyet",
        "Quyet dinh cua Director da duoc cap nhat.",
        loadPromotionOfficial,
      ),
    executePromotion: (id: number, payload: ExecutePersonnelChangeRequest) =>
      runAction(
        () => personnelChangeApi.executePromotion(id, payload),
        "Da thuc thi thang tien",
        "Thong tin chuc danh, cap bac va loai nhan su da duoc cap nhat.",
        loadPromotionOfficial,
      ),
    submitResignation: (payload: SubmitResignationRequest) =>
      runAction(
        () => personnelChangeApi.submitResignation(payload),
        "Da gui don nghi viec",
        "Ho so nghi viec chu dong da duoc ghi nhan.",
      ),
    managerReviewResignation: (id: number, payload: ManagerReviewResignationRequest) =>
      runAction(
        () => personnelChangeApi.managerReviewResignation(id, payload),
        "Da quan ly review",
        "Ho so da chuyen sang HR review hoac bi tu choi.",
      ),
    hrReviewResignation: (id: number, payload: HrReviewResignationRequest) =>
      runAction(
        () => personnelChangeApi.hrReviewResignation(id, payload),
        "Da HR review",
        "Ho so da chuyen sang Director phe duyet hoac bi tu choi.",
      ),
    directorApproveResignation: (id: number, payload: DirectorApproveResignationRequest) =>
      runAction(
        () => personnelChangeApi.directorApproveResignation(id, payload),
        "Da xu ly phe duyet",
        "Quyet dinh cua Director da duoc cap nhat.",
      ),
    executeResignation: (id: number, payload: ExecutePersonnelChangeRequest) =>
      runAction(
        () => personnelChangeApi.executeResignation(id, payload),
        "Da thuc thi nghi viec",
        "Trang thai nhan su, service period va final settlement da duoc cap nhat neu can.",
      ),
    cancel: (id: number, payload: CancelPersonnelChangeRequest) =>
      runAction(
        () => personnelChangeApi.cancel(id, payload),
        "Da huy ho so",
        "Ho so bien dong da duoc huy.",
      ),
  };
};

const getErrorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Da co loi xay ra.";
