import {
  PersonnelChangeStatus,
  type PersonnelChangeContractFlowLink,
  type PersonnelChangeDetail,
} from "../types/personnelChange";

const acceptedContractFlowStatuses = new Set(["accepted", "signed"]);

export const isContractFlowAccepted = (status?: string | null) =>
  Boolean(status && acceptedContractFlowStatuses.has(status.trim().toLowerCase()));

export const isContractFlowInProgress = (request?: PersonnelChangeDetail | null) => {
  if (!request?.requiresContractFlow) return false;

  return (
    request.status === PersonnelChangeStatus.PendingContractFlow ||
    request.status === PersonnelChangeStatus.ContractNegotiating ||
    ["pending", "negotiating", "notstarted"].includes((request.contractFlowStatus || "").toLowerCase())
  );
};

export const canExecutePersonnelChange = (request?: PersonnelChangeDetail | null) => {
  if (!request) return false;
  if (!request.requiresContractFlow) return true;

  return (
    isContractFlowAccepted(request.contractFlowStatus) ||
    (request.contractLinks ?? []).some((link) => isContractFlowAccepted(link.status))
  );
};

export const getContractFlowExecutionBlockReason = (request?: PersonnelChangeDetail | null) => {
  if (!request) return null;
  if (canExecutePersonnelChange(request)) return null;

  return "Chua the execute vi luong hop dong tai Module 3 chua Accepted/Signed.";
};

export const getPrimaryContractLink = (request?: PersonnelChangeDetail | null) => {
  if (!request) return null;
  const [firstLink] = request.contractLinks ?? [];

  return {
    contractId: firstLink?.contractId ?? request.relatedContractId ?? null,
    contractRequestId: firstLink?.contractRequestId ?? request.relatedContractRequestId ?? null,
    contractAddendumId: firstLink?.contractAddendumId ?? request.relatedContractAddendumId ?? null,
  };
};

export const hasContractReference = (link: PersonnelChangeContractFlowLink) =>
  Boolean(link.contractId || link.contractRequestId || link.contractAddendumId);
