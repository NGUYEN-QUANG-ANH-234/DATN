import type { ReactNode } from "react";
import type { PersonnelChangeDetail, PersonnelChangeWorkflowKind } from "../types/personnelChange";

type Props = {
  kind: PersonnelChangeWorkflowKind;
  request?: PersonnelChangeDetail | null;
  children?: ReactNode;
};

export const PersonnelChangeActionPanel = ({ kind, request, children }: Props) => (
  <section className="space-y-4">
    <div className="flex flex-col gap-3 border-b border-[var(--hicas-border)] pb-3 lg:flex-row lg:items-end lg:justify-between">
      <div>
        <h2 className="text-lg font-semibold text-[var(--hicas-text-main)]">{getActionTitle(kind)}</h2>
        <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">{getActionDescription(kind)}</p>
      </div>
      {request ? (
        <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white px-3 py-2 text-sm">
          <span className="font-semibold text-[var(--hicas-text-main)]">
            #{String(request.id).padStart(5, "0")}
          </span>
          <span className="ml-2 text-[var(--hicas-text-secondary)]">
            {request.employeeName || "Chua co nhan su"}
          </span>
        </div>
      ) : null}
    </div>

    {!request ? (
      <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4">
        <p className="text-sm text-[var(--hicas-text-secondary)]">Mo mot ho so trong bang de thao tac.</p>
      </div>
    ) : (
      <div className="space-y-5">
        <p className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3 text-sm text-[var(--hicas-text-secondary)]">
          {request.reason || "Khong co ghi chu."}
        </p>
        {children}
      </div>
    )}
  </section>
);

const getActionTitle = (kind: PersonnelChangeWorkflowKind) => {
  if (kind === "promotion") return "Promotion actions";
  if (kind === "senior-appointment") return "Senior appointment actions";
  if (kind === "termination") return "Resignation actions";
  if (kind === "dismissal") return "Dismissal actions";
  return "Internal transfer actions";
};

const getActionDescription = (kind: PersonnelChangeWorkflowKind) => {
  if (kind === "promotion") return "HR review, Director approval va execute cho F7.1.";
  if (kind === "senior-appointment") return "Consent, contract flow, decision va execute cho F7.2.";
  if (kind === "termination") return "Manager, HR, Director, contract termination va execute cho F7.3.";
  if (kind === "dismissal") return "Notification, giai trinh, Director approval va execute cho F7.4.";
  return "HR select, manager opinion, consent, decision va execute cho F7.5.";
};
