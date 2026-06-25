import { useState, type ReactNode } from "react";
import { Card } from "../../../components/ui";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import { usePersonnelChanges } from "../hooks/usePersonnelChanges";
import {
  PersonnelChangeStatus,
  PersonnelChangeType,
  type PersonnelChangeDetail,
  type PersonnelChangeListItem,
  type PersonnelChangeStatus as PersonnelChangeStatusValue,
  type PersonnelChangeWorkflowKind,
} from "../types/personnelChange";
import { AppointmentConsentPanel } from "./AppointmentConsentPanel";
import { ContractFlowLinkPanel } from "./ContractFlowLinkPanel";
import { ConvertOfficialForm } from "./ConvertOfficialForm";
import { CurrentManagerOpinionPanel } from "./CurrentManagerOpinionPanel";
import { DirectorApprovalPanel } from "./DirectorApprovalPanel";
import { DismissalCreateForm } from "./DismissalCreateForm";
import { DismissalDirectorApprovalPanel } from "./DismissalDirectorApprovalPanel";
import { DismissalEvidencePanel } from "./DismissalEvidencePanel";
import { DismissalExplanationPanel } from "./DismissalExplanationPanel";
import { DismissalNotificationPanel } from "./DismissalNotificationPanel";
import { EmployeeConsentPanel } from "./EmployeeConsentPanel";
import { HrContractFlowPanel } from "./HrContractFlowPanel";
import { HrSelectEmployeePanel } from "./HrSelectEmployeePanel";
import { InternalTransferDemandForm } from "./InternalTransferDemandForm";
import { IssueAppointmentDecisionPanel } from "./IssueAppointmentDecisionPanel";
import { IssueTransferDecisionPanel } from "./IssueTransferDecisionPanel";
import { PersonnelChangeActionPanel } from "./PersonnelChangeActionPanel";
import { PersonnelChangeDetailDrawer } from "./PersonnelChangeDetailDrawer";
import { PersonnelChangeList } from "./PersonnelChangeList";
import { PromotionApprovalPanel } from "./PromotionApprovalPanel";
import { PromotionEligibilityPanel } from "./PromotionEligibilityPanel";
import { PromotionForm } from "./PromotionForm";
import { ResignationDirectorApprovalPanel } from "./ResignationDirectorApprovalPanel";
import { ResignationHrReviewPanel } from "./ResignationHrReviewPanel";
import { ResignationManagerReviewPanel } from "./ResignationManagerReviewPanel";
import { ResignationSettlementPanel } from "./ResignationSettlementPanel";
import { ResignationSubmitForm } from "./ResignationSubmitForm";
import { SeniorAppointmentForm } from "./SeniorAppointmentForm";

type PersonnelChangePageProps = {
  kind: PersonnelChangeWorkflowKind;
};

type PersonnelChangeController = ReturnType<typeof usePersonnelChanges>;

type PageConfig = {
  title: string;
  description: string;
  changeType: PersonnelChangeType;
  emptyTitle: string;
  emptyDescription: string;
  attentionLabel: string;
  attentionStatuses: PersonnelChangeStatusValue[];
  readyLabel: string;
  readyStatuses: PersonnelChangeStatusValue[];
};

const pageConfigs: Record<PersonnelChangeWorkflowKind, PageConfig> = {
  promotion: {
    title: "Thăng tiến & chuyển chính thức",
    description: "Quản lý đề xuất thăng tiến hoặc chuyển chính thức từ tạo hồ sơ đến thực thi.",
    changeType: PersonnelChangeType.Promotion,
    emptyTitle: "Chưa có hồ sơ",
    emptyDescription: "Tạo hồ sơ thăng tiến hoặc chuyển chính thức để bắt đầu xử lý.",
    attentionLabel: "Chờ HR",
    attentionStatuses: [PersonnelChangeStatus.PendingHRReview],
    readyLabel: "Sẵn sàng thực thi",
    readyStatuses: [PersonnelChangeStatus.ReadyToExecute, PersonnelChangeStatus.ContractAccepted],
  },
  "senior-appointment": {
    title: "Bổ nhiệm cấp cao",
    description: "Theo dõi hồ sơ bổ nhiệm từ xác nhận, hợp đồng đến ban hành quyết định.",
    changeType: PersonnelChangeType.SeniorAppointment,
    emptyTitle: "Chưa có hồ sơ bổ nhiệm",
    emptyDescription: "Tạo hồ sơ bổ nhiệm đầu tiên để bắt đầu xử lý.",
    attentionLabel: "Chờ xác nhận",
    attentionStatuses: [PersonnelChangeStatus.PendingEmployeeConsent],
    readyLabel: "Sẵn sàng thực thi",
    readyStatuses: [PersonnelChangeStatus.ReadyToExecute, PersonnelChangeStatus.ContractAccepted],
  },
  termination: {
    title: "Nghỉ việc chủ động",
    description: "Xử lý đơn nghỉ việc từ quản lý trực tiếp, HR, giám đốc đến hoàn tất hồ sơ.",
    changeType: PersonnelChangeType.VoluntaryTermination,
    emptyTitle: "Chưa có hồ sơ nghỉ việc",
    emptyDescription: "Tạo đơn nghỉ việc đầu tiên để bắt đầu xử lý.",
    attentionLabel: "Chờ quản lý",
    attentionStatuses: [PersonnelChangeStatus.PendingManagerReview],
    readyLabel: "Chờ hợp đồng",
    readyStatuses: [PersonnelChangeStatus.PendingContractFlow, PersonnelChangeStatus.ContractNegotiating],
  },
  dismissal: {
    title: "Kỷ luật & sa thải",
    description: "Theo dõi hồ sơ kỷ luật từ thông báo, giải trình đến phê duyệt và thực thi.",
    changeType: PersonnelChangeType.Dismissal,
    emptyTitle: "Chưa có hồ sơ kỷ luật",
    emptyDescription: "Tạo hồ sơ từ biên bản vi phạm để bắt đầu xử lý.",
    attentionLabel: "Chờ giải trình",
    attentionStatuses: [PersonnelChangeStatus.PendingEmployeeExplanation],
    readyLabel: "Chờ giám đốc",
    readyStatuses: [PersonnelChangeStatus.PendingDirectorApproval],
  },
  "internal-transfer": {
    title: "Thuyên chuyển nội bộ",
    description: "Quản lý nhu cầu thuyên chuyển, chọn nhân sự, xác nhận và ban hành quyết định.",
    changeType: PersonnelChangeType.InternalTransfer,
    emptyTitle: "Chưa có nhu cầu thuyên chuyển",
    emptyDescription: "Tạo nhu cầu thuyên chuyển đầu tiên để bắt đầu xử lý.",
    attentionLabel: "Chờ HR",
    attentionStatuses: [PersonnelChangeStatus.PendingHRReview],
    readyLabel: "Sẵn sàng thực thi",
    readyStatuses: [PersonnelChangeStatus.ReadyToExecute],
  },
};

export const PersonnelChangePage = ({ kind }: PersonnelChangePageProps) => {
  const config = pageConfigs[kind];
  const controller = usePersonnelChanges(config.changeType);
  const { user } = useCurrentUser();
  const [detailOpen, setDetailOpen] = useState(false);

  const openDetail = async (id: number) => {
    await controller.loadDetail(id);
    setDetailOpen(true);
  };

  return (
    <div className="space-y-5">
      <Card title={config.title} description={config.description}>
        <div className="grid gap-4 md:grid-cols-3">
          <Metric label="Hồ sơ" value={controller.records.length} />
          <Metric
            label={config.attentionLabel}
            value={countByStatus(controller.records, config.attentionStatuses)}
          />
          <Metric label={config.readyLabel} value={countByStatus(controller.records, config.readyStatuses)} />
        </div>
      </Card>

      <CreateSection kind={kind} controller={controller} role={user?.role || ""} />

      <PersonnelChangeList
        records={controller.records}
        kind={kind}
        loading={controller.loading}
        emptyTitle={config.emptyTitle}
        emptyDescription={config.emptyDescription}
        onOpen={(id) => void openDetail(id)}
      />

      <PersonnelChangeActionPanel kind={kind} request={controller.selected}>
        <ActionSection
          kind={kind}
          controller={controller}
          role={user?.role || ""}
          accountId={user?.accountId}
        />
      </PersonnelChangeActionPanel>

      <PersonnelChangeDetailDrawer
        open={detailOpen}
        request={controller.selected}
        riskSummary={controller.riskSummary}
        timeline={controller.timeline}
        onClose={() => setDetailOpen(false)}
      />
    </div>
  );
};

const CreateSection = ({
  kind,
  controller,
  role,
}: {
  kind: PersonnelChangeWorkflowKind;
  controller: PersonnelChangeController;
  role: string;
}) => {
  const permissions = getActionPermissions(role);

  if (kind === "promotion") {
    if (!permissions.hr) return null;
    return (
      <div className="grid gap-5 xl:grid-cols-2">
        <PromotionForm saving={controller.saving} onSubmit={controller.createPromotion} />
        <ConvertOfficialForm saving={controller.saving} onSubmit={controller.createConvertOfficial} />
      </div>
    );
  }

  if (kind === "senior-appointment") {
    if (!permissions.hr) return null;
    return <SeniorAppointmentForm saving={controller.saving} onSubmit={controller.createSeniorAppointment} />;
  }

  if (kind === "termination") {
    if (!permissions.employee && !permissions.hr) return null;
    return <ResignationSubmitForm saving={controller.saving} onSubmit={controller.submitResignation} />;
  }

  if (kind === "dismissal") {
    if (!permissions.hr) return null;
    return <DismissalCreateForm saving={controller.saving} onSubmit={controller.createDismissal} />;
  }

  if (!permissions.hr && !permissions.manager) return null;
  return (
    <InternalTransferDemandForm
      saving={controller.saving}
      onSubmit={controller.createInternalTransferDemand}
    />
  );
};

const ActionSection = ({
  kind,
  controller,
  role,
  accountId,
}: {
  kind: PersonnelChangeWorkflowKind;
  controller: PersonnelChangeController;
  role: string;
  accountId?: number;
}) => {
  if (!controller.selected) return null;
  const request = controller.selected;
  const permissions = getActionPermissions(role, request, accountId);
  const status = request.status;
  const actionItems: ReactNode[] = [];

  const addAction = (key: string, canShow: boolean, node: ReactNode) => {
    if (canShow) actionItems.push(<div key={key}>{node}</div>);
  };

  if (kind === "promotion") {
    const canHrReview =
      permissions.hr && status === PersonnelChangeStatus.PendingHRReview;
    const canDirectorApprove =
      permissions.director && status === PersonnelChangeStatus.PendingDirectorApproval;
    const canExecute =
      permissions.hr &&
      isStatusOneOf(status, [
        PersonnelChangeStatus.ApprovedByDirector,
        PersonnelChangeStatus.ContractAccepted,
        PersonnelChangeStatus.ReadyToExecute,
      ]);

    addAction("promotion-eligibility", true, (
      <PromotionEligibilityPanel request={request} summary={controller.riskSummary} />
    ));
    addAction("promotion-approval", canHrReview || canDirectorApprove || canExecute, (
        <PromotionApprovalPanel
          request={request}
          saving={controller.saving}
          canHrReview={canHrReview}
          canDirectorApprove={canDirectorApprove}
          canExecute={canExecute}
          onHrReview={controller.hrReviewPromotion}
          onDirectorApprove={controller.directorApprovePromotion}
          onExecute={controller.executePromotion}
        />
    ));
  }

  if (kind === "senior-appointment") {
    addAction(
      "appointment-consent",
      permissions.employee && status === PersonnelChangeStatus.PendingEmployeeConsent,
      (
        <AppointmentConsentPanel
          request={request}
          saving={controller.saving}
          onSubmit={controller.submitAppointmentConsent}
        />
      ),
    );
    addAction(
      "appointment-contract-flow",
      permissions.hr && status === PersonnelChangeStatus.PendingContractFlow,
      (
        <HrContractFlowPanel
          request={request}
          saving={controller.saving}
          onSubmit={controller.startHrContractFlow}
        />
      ),
    );
    addAction(
      "appointment-decision",
      permissions.hr &&
        isStatusOneOf(status, [
          PersonnelChangeStatus.ContractAccepted,
          PersonnelChangeStatus.PendingDecisionIssuance,
          PersonnelChangeStatus.ReadyToExecute,
        ]),
      (
        <IssueAppointmentDecisionPanel
          request={request}
          saving={controller.saving}
          onIssue={controller.issueAppointmentDecision}
          onExecute={controller.executeSeniorAppointment}
        />
      ),
    );
  }

  if (kind === "termination") {
    addAction("resignation-settlement", true, <ResignationSettlementPanel request={request} />);
    addAction(
      "resignation-manager",
      permissions.manager && status === PersonnelChangeStatus.PendingManagerReview,
      (
        <ResignationManagerReviewPanel
          request={request}
          saving={controller.saving}
          onSubmit={controller.managerReviewResignation}
        />
      ),
    );
    addAction(
      "resignation-hr",
      permissions.hr && status === PersonnelChangeStatus.PendingHRReview,
      (
        <ResignationHrReviewPanel
          request={request}
          saving={controller.saving}
          onSubmit={controller.hrReviewResignation}
        />
      ),
    );

    const canApprove = permissions.director && status === PersonnelChangeStatus.PendingDirectorApproval;
    const canExecute =
      permissions.hr &&
      isStatusOneOf(status, [PersonnelChangeStatus.ContractAccepted, PersonnelChangeStatus.ReadyToExecute]);
    addAction(
      "resignation-director-execute",
      canApprove || canExecute,
      (
        <ResignationDirectorApprovalPanel
          request={request}
          saving={controller.saving}
          canApprove={canApprove}
          canExecute={canExecute}
          onApprove={controller.directorApproveResignation}
          onExecute={controller.executeResignation}
        />
      ),
    );
  }

  if (kind === "dismissal") {
    addAction("dismissal-evidence", true, <DismissalEvidencePanel request={request} />);
    addAction(
      "dismissal-notify",
      permissions.hr &&
        isStatusOneOf(status, [
          PersonnelChangeStatus.PendingHRReview,
          PersonnelChangeStatus.PendingEmployeeNotification,
        ]),
      (
        <DismissalNotificationPanel
          request={request}
          saving={controller.saving}
          onSubmit={controller.notifyDismissalEmployee}
        />
      ),
    );
    addAction(
      "dismissal-explanation",
      permissions.employee && status === PersonnelChangeStatus.PendingEmployeeExplanation,
      (
        <DismissalExplanationPanel
          request={request}
          saving={controller.saving}
          onSubmit={controller.submitDismissalExplanation}
        />
      ),
    );
    const canApprove = permissions.director && status === PersonnelChangeStatus.PendingDirectorApproval;
    const canExecute =
      permissions.hr &&
      isStatusOneOf(status, [
        PersonnelChangeStatus.ApprovedByDirector,
        PersonnelChangeStatus.ContractAccepted,
        PersonnelChangeStatus.ReadyToExecute,
      ]);
    addAction(
      "dismissal-approve-execute",
      canApprove || canExecute,
      (
        <DismissalDirectorApprovalPanel
          request={request}
          saving={controller.saving}
          canApprove={canApprove}
          canExecute={canExecute}
          onApprove={controller.directorApproveDismissal}
          onExecute={controller.executeDismissal}
        />
      ),
    );
  }

  if (kind === "internal-transfer") {
    addAction(
      "transfer-hr-select",
      permissions.hr && status === PersonnelChangeStatus.PendingHRReview,
      (
        <HrSelectEmployeePanel
          request={request}
          saving={controller.saving}
          onSubmit={controller.hrSelectEmployee}
        />
      ),
    );
    addAction(
      "transfer-manager",
      permissions.manager && status === PersonnelChangeStatus.PendingCurrentManagerOpinion,
      (
        <CurrentManagerOpinionPanel
          request={request}
          saving={controller.saving}
          onSubmit={controller.submitCurrentManagerOpinion}
        />
      ),
    );
    addAction(
      "transfer-employee",
      permissions.employee && status === PersonnelChangeStatus.PendingEmployeeConsent,
      (
        <EmployeeConsentPanel
          request={request}
          saving={controller.saving}
          onSubmit={controller.submitEmployeeConsent}
        />
      ),
    );
    addAction(
      "transfer-director",
      permissions.director && status === PersonnelChangeStatus.PendingDirectorApproval,
      (
        <DirectorApprovalPanel
          request={request}
          saving={controller.saving}
          onSubmit={controller.directorApproveTransfer}
        />
      ),
    );
    addAction(
      "transfer-issue-execute",
      permissions.hr &&
        isStatusOneOf(status, [
          PersonnelChangeStatus.ApprovedByDirector,
          PersonnelChangeStatus.ContractAccepted,
          PersonnelChangeStatus.PendingDecisionIssuance,
          PersonnelChangeStatus.ReadyToExecute,
        ]),
      (
        <IssueTransferDecisionPanel
          request={request}
          saving={controller.saving}
          onIssue={controller.issueTransferDecision}
          onExecute={controller.executeInternalTransfer}
        />
      ),
    );
  }

  return (
    <div className="grid gap-5 xl:grid-cols-2">
      {request.requiresContractFlow ? (
        <ContractFlowLinkPanel request={request} />
      ) : null}
      {actionItems.length > 0 ? actionItems : <NoAvailableAction />}
    </div>
  );
};

const getActionPermissions = (
  role: string,
  request?: PersonnelChangeDetail | null,
  accountId?: number,
) => {
  const normalized = role.trim().toLowerCase();
  const admin = normalized === "admin";
  const isHr = normalized === "hr";
  const isManagerRole = normalized === "manager";
  const isDirector = normalized === "director";
  const isEmployeeRole =
    normalized === "employee" ||
    normalized === "intern" ||
    normalized === "collaborator";
  const hasAccount = typeof accountId === "number" && Number.isFinite(accountId);
  const isSelectedEmployee =
    hasAccount && request?.employeeAccountId === accountId;
  const isCurrentManager =
    hasAccount && request?.currentManagerAccountId === accountId;

  return {
    hr: admin || isHr,
    manager: admin || (request ? isCurrentManager : isManagerRole),
    director: admin || isDirector,
    employee:
      admin ||
      isSelectedEmployee ||
      (!request && isEmployeeRole) ||
      (isEmployeeRole && !request?.employeeAccountId),
  };
};

const NoAvailableAction = () => (
  <Card
    title="Không có thao tác phù hợp"
    description="Hồ sơ hiện chưa đến bước xử lý của vai trò đang đăng nhập."
  >
    <p className="text-sm text-[var(--hicas-text-secondary)]">
      Bạn vẫn có thể theo dõi chi tiết, lịch sử và thông tin tham chiếu của hồ sơ nếu có quyền xem.
    </p>
  </Card>
);

const isStatusOneOf = (
  status: PersonnelChangeStatusValue,
  allowed: PersonnelChangeStatusValue[],
) => allowed.includes(status);

const Metric = ({ label, value }: { label: string; value: number }) => (
  <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4">
    <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">{label}</p>
    <p className="mt-2 text-2xl font-bold text-[var(--hicas-text-main)]">{value}</p>
  </div>
);

const countByStatus = (
  records: PersonnelChangeListItem[],
  statuses: PersonnelChangeStatusValue[],
) => records.filter((item) => statuses.includes(item.status)).length;
