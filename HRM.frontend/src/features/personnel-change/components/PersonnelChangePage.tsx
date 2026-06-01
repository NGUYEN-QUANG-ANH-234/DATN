import { useState, type ReactNode } from "react";
import { Card } from "../../../components/ui";
import { usePersonnelChanges } from "../hooks/usePersonnelChanges";
import {
  PersonnelChangeStatus,
  PersonnelChangeType,
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
    title: "Thang tien va chuyen chinh thuc",
    description: "Quan ly F7.1 theo flow HR review, Director approval, contract flow va execute.",
    changeType: PersonnelChangeType.Promotion,
    emptyTitle: "Chua co ho so F7.1",
    emptyDescription: "Tao ho so thang tien hoac chuyen chinh thuc de bat dau flow.",
    attentionLabel: "Cho HR",
    attentionStatuses: [PersonnelChangeStatus.PendingHRReview],
    readyLabel: "San sang execute",
    readyStatuses: [PersonnelChangeStatus.ReadyToExecute, PersonnelChangeStatus.ContractAccepted],
  },
  "senior-appointment": {
    title: "Bo nhiem nhan su cap cao",
    description: "Quan ly F7.2 theo flow consent, contract flow, quyet dinh va execute.",
    changeType: PersonnelChangeType.SeniorAppointment,
    emptyTitle: "Chua co ho so bo nhiem",
    emptyDescription: "Tao ho so bo nhiem dau tien de bat dau flow F7.2.",
    attentionLabel: "Cho consent",
    attentionStatuses: [PersonnelChangeStatus.PendingEmployeeConsent],
    readyLabel: "San sang execute",
    readyStatuses: [PersonnelChangeStatus.ReadyToExecute, PersonnelChangeStatus.ContractAccepted],
  },
  termination: {
    title: "Nghi viec chu dong",
    description: "Quan ly F7.3 theo flow Manager, HR, Director, contract termination va execute.",
    changeType: PersonnelChangeType.VoluntaryTermination,
    emptyTitle: "Chua co ho so nghi viec",
    emptyDescription: "Tao don nghi viec dau tien de bat dau flow F7.3.",
    attentionLabel: "Cho Manager",
    attentionStatuses: [PersonnelChangeStatus.PendingManagerReview],
    readyLabel: "Cho contract",
    readyStatuses: [PersonnelChangeStatus.PendingContractFlow, PersonnelChangeStatus.ContractNegotiating],
  },
  dismissal: {
    title: "Sa thai va ky luat",
    description: "Quan ly F7.4 theo flow notification, giai trinh, Director approval va execute.",
    changeType: PersonnelChangeType.Dismissal,
    emptyTitle: "Chua co ho so sa thai",
    emptyDescription: "Tao ho so tu penalty record de bat dau flow F7.4.",
    attentionLabel: "Cho giai trinh",
    attentionStatuses: [PersonnelChangeStatus.PendingEmployeeExplanation],
    readyLabel: "Cho Director",
    readyStatuses: [PersonnelChangeStatus.PendingDirectorApproval],
  },
  "internal-transfer": {
    title: "Thuyen chuyen noi bo",
    description: "Quan ly F7.5 theo flow demand, HR select, consent, decision va execute.",
    changeType: PersonnelChangeType.InternalTransfer,
    emptyTitle: "Chua co demand thuyen chuyen",
    emptyDescription: "Tao demand dau tien de bat dau flow F7.5.",
    attentionLabel: "Cho HR",
    attentionStatuses: [PersonnelChangeStatus.PendingHRReview],
    readyLabel: "San sang execute",
    readyStatuses: [PersonnelChangeStatus.ReadyToExecute],
  },
};

export const PersonnelChangePage = ({ kind }: PersonnelChangePageProps) => {
  const config = pageConfigs[kind];
  const controller = usePersonnelChanges(config.changeType);
  const [detailOpen, setDetailOpen] = useState(false);

  const openDetail = async (id: number) => {
    await controller.loadDetail(id);
    setDetailOpen(true);
  };

  return (
    <div className="space-y-5">
      <Card title={config.title} description={config.description}>
        <div className="grid gap-4 md:grid-cols-3">
          <Metric label="Ho so" value={controller.records.length} />
          <Metric
            label={config.attentionLabel}
            value={countByStatus(controller.records, config.attentionStatuses)}
          />
          <Metric label={config.readyLabel} value={countByStatus(controller.records, config.readyStatuses)} />
        </div>
      </Card>

      <CreateSection kind={kind} controller={controller} />

      <PersonnelChangeList
        records={controller.records}
        kind={kind}
        loading={controller.loading}
        emptyTitle={config.emptyTitle}
        emptyDescription={config.emptyDescription}
        onOpen={(id) => void openDetail(id)}
      />

      <PersonnelChangeActionPanel kind={kind} request={controller.selected}>
        <ActionSection kind={kind} controller={controller} />
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
}: {
  kind: PersonnelChangeWorkflowKind;
  controller: PersonnelChangeController;
}) => {
  if (kind === "promotion") {
    return (
      <div className="grid gap-5 xl:grid-cols-2">
        <PromotionForm saving={controller.saving} onSubmit={controller.createPromotion} />
        <ConvertOfficialForm saving={controller.saving} onSubmit={controller.createConvertOfficial} />
      </div>
    );
  }

  if (kind === "senior-appointment") {
    return <SeniorAppointmentForm saving={controller.saving} onSubmit={controller.createSeniorAppointment} />;
  }

  if (kind === "termination") {
    return <ResignationSubmitForm saving={controller.saving} onSubmit={controller.submitResignation} />;
  }

  if (kind === "dismissal") {
    return <DismissalCreateForm saving={controller.saving} onSubmit={controller.createDismissal} />;
  }

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
}: {
  kind: PersonnelChangeWorkflowKind;
  controller: PersonnelChangeController;
}) => {
  if (!controller.selected) return null;

  const contentByKind: Record<PersonnelChangeWorkflowKind, ReactNode> = {
    promotion: (
      <>
        <PromotionEligibilityPanel request={controller.selected} summary={controller.riskSummary} />
        <PromotionApprovalPanel
          request={controller.selected}
          saving={controller.saving}
          onHrReview={controller.hrReviewPromotion}
          onDirectorApprove={controller.directorApprovePromotion}
          onExecute={controller.executePromotion}
        />
      </>
    ),
    "senior-appointment": (
      <>
        <AppointmentConsentPanel
          request={controller.selected}
          saving={controller.saving}
          onSubmit={controller.submitAppointmentConsent}
        />
        <HrContractFlowPanel
          request={controller.selected}
          saving={controller.saving}
          onSubmit={controller.startHrContractFlow}
        />
        <IssueAppointmentDecisionPanel
          request={controller.selected}
          saving={controller.saving}
          onIssue={controller.issueAppointmentDecision}
          onExecute={controller.executeSeniorAppointment}
        />
      </>
    ),
    termination: (
      <>
        <ResignationSettlementPanel request={controller.selected} />
        <ResignationManagerReviewPanel
          request={controller.selected}
          saving={controller.saving}
          onSubmit={controller.managerReviewResignation}
        />
        <ResignationHrReviewPanel
          request={controller.selected}
          saving={controller.saving}
          onSubmit={controller.hrReviewResignation}
        />
        <ResignationDirectorApprovalPanel
          request={controller.selected}
          saving={controller.saving}
          onApprove={controller.directorApproveResignation}
          onExecute={controller.executeResignation}
        />
      </>
    ),
    dismissal: (
      <>
        <DismissalEvidencePanel request={controller.selected} />
        <DismissalNotificationPanel
          request={controller.selected}
          saving={controller.saving}
          onSubmit={controller.notifyDismissalEmployee}
        />
        <DismissalExplanationPanel
          request={controller.selected}
          saving={controller.saving}
          onSubmit={controller.submitDismissalExplanation}
        />
        <DismissalDirectorApprovalPanel
          request={controller.selected}
          saving={controller.saving}
          onApprove={controller.directorApproveDismissal}
          onExecute={controller.executeDismissal}
        />
      </>
    ),
    "internal-transfer": (
      <>
        <HrSelectEmployeePanel
          request={controller.selected}
          saving={controller.saving}
          onSubmit={controller.hrSelectEmployee}
        />
        <CurrentManagerOpinionPanel
          request={controller.selected}
          saving={controller.saving}
          onSubmit={controller.submitCurrentManagerOpinion}
        />
        <EmployeeConsentPanel
          request={controller.selected}
          saving={controller.saving}
          onSubmit={controller.submitEmployeeConsent}
        />
        <DirectorApprovalPanel
          request={controller.selected}
          saving={controller.saving}
          onSubmit={controller.directorApproveTransfer}
        />
        <IssueTransferDecisionPanel
          request={controller.selected}
          saving={controller.saving}
          onIssue={controller.issueTransferDecision}
          onExecute={controller.executeInternalTransfer}
        />
      </>
    ),
  };

  return (
    <div className="grid gap-5 xl:grid-cols-2">
      {controller.selected.requiresContractFlow ? (
        <ContractFlowLinkPanel request={controller.selected} />
      ) : null}
      {contentByKind[kind]}
    </div>
  );
};

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
