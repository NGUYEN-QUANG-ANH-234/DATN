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
            {request.employeeName || "Chưa có nhân sự"}
          </span>
        </div>
      ) : null}
    </div>

    {!request ? (
      <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4">
        <p className="text-sm text-[var(--hicas-text-secondary)]">Mở một hồ sơ trong bảng để thao tác.</p>
      </div>
    ) : (
      <div className="space-y-5">
        <p className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3 text-sm text-[var(--hicas-text-secondary)]">
          {request.reason || "Không có ghi chú."}
        </p>
        {children}
      </div>
    )}
  </section>
);

const getActionTitle = (kind: PersonnelChangeWorkflowKind) => {
  if (kind === "promotion") return "Thao tác thăng tiến";
  if (kind === "senior-appointment") return "Thao tác bổ nhiệm";
  if (kind === "termination") return "Thao tác nghỉ việc";
  if (kind === "dismissal") return "Thao tác kỷ luật";
  return "Thao tác thuyên chuyển";
};

const getActionDescription = (kind: PersonnelChangeWorkflowKind) => {
  if (kind === "promotion") return "HR rà soát, giám đốc phê duyệt và thực thi hồ sơ.";
  if (kind === "senior-appointment") return "Xác nhận, xử lý hợp đồng, ban hành quyết định và thực thi.";
  if (kind === "termination") return "Quản lý, HR và giám đốc cùng xử lý trước khi hoàn tất nghỉ việc.";
  if (kind === "dismissal") return "Thông báo, ghi nhận giải trình, trình giám đốc và thực thi quyết định.";
  return "HR chọn nhân sự, lấy ý kiến quản lý, xác nhận và ban hành quyết định.";
};
