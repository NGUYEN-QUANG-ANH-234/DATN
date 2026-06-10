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
      triggerAlert("error", "Không tải được hồ sơ biến động", getErrorMessage(error));
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
      triggerAlert("error", "Không tải được hồ sơ thăng tiến", getErrorMessage(error));
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
      triggerAlert("error", "Không tải được chi tiết hồ sơ", getErrorMessage(error));
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
        "Đã tạo nhu cầu",
        "Nhu cầu thuyên chuyển nội bộ đã được ghi nhận.",
      ),
    hrSelectEmployee: (id: number, payload: HrSelectEmployeeRequest) =>
      runAction(
        () => personnelChangeApi.hrSelectEmployee(id, payload),
        "Đã chọn nhân sự",
        "Hồ sơ đã chuyển sang bước lấy ý kiến quản lý hiện tại.",
      ),
    submitCurrentManagerOpinion: (id: number, payload: CurrentManagerOpinionRequest) =>
      runAction(
        () => personnelChangeApi.submitCurrentManagerOpinion(id, payload),
        "Đã gửi ý kiến quản lý",
        "Hồ sơ đã được cập nhật theo ý kiến quản lý hiện tại.",
      ),
    submitEmployeeConsent: (id: number, payload: EmployeeConsentRequest) =>
      runAction(
        () => personnelChangeApi.submitEmployeeConsent(id, payload),
        "Đã gửi phản hồi nhân viên",
        "Phản hồi của nhân viên đã được ghi nhận.",
      ),
    directorApproveTransfer: (id: number, payload: DirectorApproveTransferRequest) =>
      runAction(
        () => personnelChangeApi.directorApproveTransfer(id, payload),
        "Đã xử lý phê duyệt",
        "Quyết định phê duyệt đã được cập nhật.",
      ),
    issueTransferDecision: (id: number, payload: IssueTransferDecisionRequest) =>
      runAction(
        () => personnelChangeApi.issueTransferDecision(id, payload),
        "Đã ban hành quyết định",
        "Hồ sơ thuyên chuyển đã sẵn sàng thực hiện.",
      ),
    executeInternalTransfer: (id: number, payload: ExecutePersonnelChangeRequest) =>
      runAction(
        () => personnelChangeApi.execute(id, payload),
        "Đã thực hiện thuyên chuyển",
        "Thông tin tổ chức của nhân sự đã được cập nhật.",
      ),
    createSeniorAppointment: (payload: CreateSeniorAppointmentRequest) =>
      runAction(
        () => personnelChangeApi.createSeniorAppointment(payload),
        "Đã tạo bổ nhiệm",
        "Hồ sơ bổ nhiệm nhân sự cấp cao đã được ghi nhận.",
      ),
    submitAppointmentConsent: (id: number, payload: AppointmentConsentRequest) =>
      runAction(
        () => personnelChangeApi.submitAppointmentConsent(id, payload),
        "Đã gửi phản hồi",
        "Phản hồi bổ nhiệm của nhân viên đã được ghi nhận.",
      ),
    startHrContractFlow: (id: number, payload: HrContractFlowRequest) =>
      runAction(
        () => personnelChangeApi.startHrContractFlow(id, payload),
        "Đã tạo xử lý hợp đồng",
        "Hồ sơ bổ nhiệm đã chuyển sang bước xử lý hợp đồng hoặc phụ lục.",
      ),
    issueAppointmentDecision: (id: number, payload: IssueAppointmentDecisionRequest) =>
      runAction(
        () => personnelChangeApi.issueAppointmentDecision(id, payload),
        "Đã ban hành quyết định",
        "Hồ sơ bổ nhiệm đã sẵn sàng thực hiện.",
      ),
    executeSeniorAppointment: (id: number, payload: ExecutePersonnelChangeRequest) =>
      runAction(
        () => personnelChangeApi.executeSeniorAppointment(id, payload),
        "Đã thực hiện bổ nhiệm",
        "Chức danh, cấp bậc và vai trò phòng ban của nhân sự đã được cập nhật.",
      ),
    createDismissal: (payload: CreateDismissalRequest) =>
      runAction(
        () => personnelChangeApi.createDismissal(payload),
        "Đã tạo hồ sơ kỷ luật",
        "Hồ sơ kỷ luật hoặc sa thải đã được ghi nhận.",
      ),
    notifyDismissalEmployee: (id: number, payload: NotifyEmployeeDismissalRequest) =>
      runAction(
        () => personnelChangeApi.notifyDismissalEmployee(id, payload),
        "Đã thông báo nhân viên",
        "Hồ sơ đã chuyển sang bước nhận giải trình.",
      ),
    submitDismissalExplanation: (id: number, payload: DismissalEmployeeExplanationRequest) =>
      runAction(
        () => personnelChangeApi.submitDismissalExplanation(id, payload),
        "Đã gửi giải trình",
        "Giải trình của nhân viên đã được ghi nhận.",
      ),
    directorApproveDismissal: (id: number, payload: DirectorApproveDismissalRequest) =>
      runAction(
        () => personnelChangeApi.directorApproveDismissal(id, payload),
        "Đã xử lý phê duyệt",
        "Quyết định phê duyệt đã được cập nhật.",
      ),
    executeDismissal: (id: number, payload: ExecutePersonnelChangeRequest) =>
      runAction(
        () => personnelChangeApi.executeDismissal(id, payload),
        "Đã thực hiện kỷ luật",
        "Trạng thái nhân sự, tài khoản và quyết toán cuối cùng đã được cập nhật khi cần.",
      ),
    createPromotion: (payload: CreatePromotionRequest) =>
      runAction(
        () => personnelChangeApi.createPromotion(payload),
        "Đã tạo hồ sơ thăng tiến",
        "Hồ sơ thăng tiến đã được ghi nhận.",
        loadPromotionOfficial,
      ),
    createConvertOfficial: (payload: CreateConvertOfficialRequest) =>
      runAction(
        () => personnelChangeApi.createConvertOfficial(payload),
        "Đã tạo hồ sơ chuyển chính thức",
        "Hồ sơ chuyển chính thức đã được ghi nhận.",
        loadPromotionOfficial,
      ),
    hrReviewPromotion: (id: number, payload: ApprovePromotionRequest) =>
      runAction(
        () => personnelChangeApi.hrReviewPromotion(id, payload),
        "Đã duyệt bước HR",
        "Hồ sơ đã chuyển sang bước phê duyệt tiếp theo hoặc bị từ chối.",
        loadPromotionOfficial,
      ),
    directorApprovePromotion: (id: number, payload: ApprovePromotionRequest) =>
      runAction(
        () => personnelChangeApi.directorApprovePromotion(id, payload),
        "Đã xử lý phê duyệt",
        "Quyết định phê duyệt đã được cập nhật.",
        loadPromotionOfficial,
      ),
    executePromotion: (id: number, payload: ExecutePersonnelChangeRequest) =>
      runAction(
        () => personnelChangeApi.executePromotion(id, payload),
        "Đã thực hiện thăng tiến",
        "Thông tin chức danh, cấp bậc và loại nhân sự đã được cập nhật.",
        loadPromotionOfficial,
      ),
    submitResignation: (payload: SubmitResignationRequest) =>
      runAction(
        () => personnelChangeApi.submitResignation(payload),
        "Đã gửi đơn nghỉ việc",
        "Hồ sơ nghỉ việc chủ động đã được ghi nhận.",
      ),
    managerReviewResignation: (id: number, payload: ManagerReviewResignationRequest) =>
      runAction(
        () => personnelChangeApi.managerReviewResignation(id, payload),
        "Đã duyệt bước quản lý",
        "Hồ sơ đã chuyển sang HR xử lý hoặc bị từ chối.",
      ),
    hrReviewResignation: (id: number, payload: HrReviewResignationRequest) =>
      runAction(
        () => personnelChangeApi.hrReviewResignation(id, payload),
        "Đã duyệt bước HR",
        "Hồ sơ đã chuyển sang phê duyệt tiếp theo hoặc bị từ chối.",
      ),
    directorApproveResignation: (id: number, payload: DirectorApproveResignationRequest) =>
      runAction(
        () => personnelChangeApi.directorApproveResignation(id, payload),
        "Đã xử lý phê duyệt",
        "Quyết định phê duyệt đã được cập nhật.",
      ),
    executeResignation: (id: number, payload: ExecutePersonnelChangeRequest) =>
      runAction(
        () => personnelChangeApi.executeResignation(id, payload),
        "Đã thực hiện nghỉ việc",
        "Trạng thái nhân sự, quá trình làm việc và quyết toán cuối cùng đã được cập nhật khi cần.",
      ),
    cancel: (id: number, payload: CancelPersonnelChangeRequest) =>
      runAction(
        () => personnelChangeApi.cancel(id, payload),
        "Đã hủy hồ sơ",
        "Hồ sơ biến động đã được hủy.",
      ),
  };
};

const getErrorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Đã có lỗi xảy ra.";
