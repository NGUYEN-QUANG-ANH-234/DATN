import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import {
  Building2,
  CalendarDays,
  CheckCircle2,
  Clock3,
  Eye,
  RefreshCw,
  UserRound,
  XCircle,
} from "lucide-react";
import {
  FeatureCard,
  FeaturePage,
  primaryButtonClass,
  secondaryButtonClass,
  dangerButtonClass,
  fieldClass,
  EmptyState,
} from "../../../core/components/FeatureShell";
import { DrawerForm } from "../../../components/ui";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import { useNotification } from "../../../core/context/NotificationContext";
import { recruitmentApi } from "../../recruitment/api/recruitmentApi";
import { hrProfileApi } from "../../employees/api/hrProfileApi";
import { dependentApi } from "../../employees/api/dependentApi";
import { onboardingApi } from "../../employees/api/onboardingApi";
import { contractApi } from "../../employees/api/contractApi";
import { contractAddendumApi } from "../../employees/api/contractAddendumApi";
import { overtimeApi } from "../../attendance/api/overtimeApi";
import { leaveRequestApi } from "../../attendance/api/leaveRequestApi";
import { accountApi } from "../../system/api/accountApi";
import { payrollApi } from "../../payroll/api/payrollApi";
import { formatMoney, formatNumber } from "../../payroll/utils";
import { personnelChangeApi } from "../../personnel-change/api/personnelChangeApi";
import { performanceApi, type PerformanceEvaluation } from "../../tasks/api/performanceApi";
import type { PendingProfileRequest } from "../../employees/types/profileRequest";
import type { PendingDependentRequest } from "../../employees/types/dependent";
import type { PendingOnboardingRequest } from "../../employees/types/onboarding";
import type { ContractDto } from "../../employees/api/contractApi";
import type { ContractAddendumDto } from "../../employees/api/contractAddendumApi";
import type { OvertimeRequest } from "../../attendance/api/overtimeApi";
import type { LeaveRequest } from "../../attendance/api/leaveRequestApi";
import type {
  ExternalTimesheetImportBatch,
  PayrollAdjustment,
  PayrollFormula,
  PayrollRunSummary,
  ProjectBonusImportBatch,
  SalarySlip,
} from "../../payroll/types/payroll";
import {
  getPersonnelChangeStatusLabel,
  PersonnelChangeStatus,
  PersonnelChangeType,
  type PersonnelChangeListItem,
  type PersonnelChangeStatus as PersonnelChangeStatusValue,
  type PersonnelChangeWorkflowKind,
} from "../../personnel-change/types/personnelChange";
import type {
  ApprovalItem,
  ApprovalModule,
  ApprovalAction,
  PendingApprovalDto,
  PendingApprovalActionDto,
  RoleOption,
  ApprovalWorkspaceFilters,
} from "../types";
import { APPROVAL_MODULES } from "../types";
import {
  formatDate,
  getRole,
  isApprovalRole,
  moduleTone,
  normalizeText,
  roleLabel,
  statusLabel,
  unwrapData,
} from "../utils";

const defaultFilters: ApprovalWorkspaceFilters = {
  module: "ALL",
  status: "ALL",
  owner: "",
  deadline: "ALL",
  query: "",
  fromDate: "",
  toDate: "",
};

const approvalModuleValues = new Set(
  APPROVAL_MODULES.map((item) => item.value).filter(
    (value): value is ApprovalModule => value !== "ALL",
  ),
);

const resolveModuleFilter = (
  value: string | null,
): ApprovalWorkspaceFilters["module"] =>
  value && approvalModuleValues.has(value as ApprovalModule)
    ? (value as ApprovalModule)
    : "ALL";

const isValidDate = (date: Date) => !Number.isNaN(date.getTime());

const startOfDay = (date: Date) => {
  const next = new Date(date);
  next.setHours(0, 0, 0, 0);
  return next;
};

const getDeadlineBucket = (
  value?: string | null,
): ApprovalWorkspaceFilters["deadline"] => {
  if (!value) return "NO_DEADLINE";

  const deadline = startOfDay(new Date(value));
  if (!isValidDate(deadline)) return "NO_DEADLINE";

  const today = startOfDay(new Date());
  const nextSevenDays = new Date(today);
  nextSevenDays.setDate(today.getDate() + 7);

  if (deadline < today) return "OVERDUE";
  if (deadline.getTime() === today.getTime()) return "TODAY";
  if (deadline <= nextSevenDays) return "NEXT_7_DAYS";
  return "ALL";
};

const deadlineLabel = (
  value: ApprovalWorkspaceFilters["deadline"],
) => {
  const map: Record<ApprovalWorkspaceFilters["deadline"], string> = {
    ALL: "Tất cả hạn xử lý",
    OVERDUE: "Quá hạn",
    TODAY: "Đến hạn hôm nay",
    NEXT_7_DAYS: "Trong 7 ngày tới",
    NO_DEADLINE: "Chưa có hạn",
  };

  return map[value];
};

const readableDeadlineLabel = (
  value: ApprovalWorkspaceFilters["deadline"],
) => {
  const map: Record<ApprovalWorkspaceFilters["deadline"], string> = {
    ALL: "Tất cả hạn xử lý",
    OVERDUE: "Quá hạn",
    TODAY: "Đến hạn hôm nay",
    NEXT_7_DAYS: "Trong 7 ngày tới",
    NO_DEADLINE: "Chưa có hạn",
  };

  return map[value];
};

const includeOvertimeReconcileInApprovalInbox = false;

const currentPayrollPeriod = () => {
  const now = new Date();
  return {
    month: now.getMonth() + 1,
    year: now.getFullYear(),
    period: `${String(now.getMonth() + 1).padStart(2, "0")}/${now.getFullYear()}`,
  };
};

type OnboardingDepartmentOption = {
  id: number;
  deptName: string;
  children?: OnboardingDepartmentOption[];
};

type OnboardingPositionOption = {
  id: number;
  title: string;
};

type OnboardingReviewSelection = {
  roleId?: number;
  departmentId?: number;
  positionId?: number;
};

const flattenDepartments = (
  nodes: OnboardingDepartmentOption[] = [],
): OnboardingDepartmentOption[] =>
  nodes.flatMap((node) => [
    { id: node.id, deptName: node.deptName },
    ...flattenDepartments(node.children || []),
  ]);

const unwrapLookupList = <T,>(value: unknown): T[] => {
  if (Array.isArray(value)) return value as T[];

  const response = value as { data?: T[]; Data?: T[] };
  return response?.data || response?.Data || [];
};

export const ApprovalWorkspacePage = () => {
  const { user } = useCurrentUser();
  const role = getRole(user?.role);
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const { triggerAlert } = useNotification();
  const moduleFromQuery = useMemo(
    () => resolveModuleFilter(searchParams.get("module")),
    [searchParams],
  );

  const [items, setItems] = useState<ApprovalItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedItemId, setSelectedItemId] = useState<string | null>(null);
  const [pendingAction, setPendingAction] = useState<{
    item: ApprovalItem;
    action: ApprovalAction;
  } | null>(null);
  const [actionNote, setActionNote] = useState("");
  const [actionSubmitting, setActionSubmitting] = useState(false);
  const [filters, setFiltersState] = useState<ApprovalWorkspaceFilters>(() => ({
    ...defaultFilters,
    module: moduleFromQuery,
  }));
  const [roleOptions, setRoleOptions] = useState<RoleOption[]>([]);
  const onboardingReviewSelectionsRef = useRef<Record<number, OnboardingReviewSelection>>({});
  const [onboardingDepartments, setOnboardingDepartments] = useState<
    OnboardingDepartmentOption[]
  >([]);
  const [onboardingPositions, setOnboardingPositions] = useState<
    OnboardingPositionOption[]
  >([]);

  const setFilters = useCallback(
    (next: ApprovalWorkspaceFilters) => {
      setFiltersState(next);

      const nextParams = new URLSearchParams(searchParams);
      if (next.module === "ALL") {
        nextParams.delete("module");
      } else {
        nextParams.set("module", next.module);
      }

      setSearchParams(nextParams, { replace: true });
    },
    [searchParams, setSearchParams],
  );

  const fetchItems = useCallback(async () => {
    if (!isApprovalRole(role)) {
      setItems([]);
      return;
    }

    setLoading(true);
    try {
      const next: ApprovalItem[] = [];

      await Promise.all([
        loadCentralApprovals(next),
        loadProfileApprovals(next),
        loadDependentApprovals(next),
        loadOnboardingApprovals(next),
        loadContractWorkItems(next),
        loadAddendumApprovals(next),
        loadOvertimeApprovals(next),
        loadLeaveApprovals(next),
        loadPayrollWorkItems(next),
        loadPersonnelChangeApprovals(next),
        loadPerformanceApprovals(next),
      ]);

      setItems(next);
    } finally {
      setLoading(false);
    }
  // Helpers below are scoped to this render and all read the same role snapshot.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [role]);

  const loadCentralApprovals = async (target: ApprovalItem[]) => {
    if (!["Admin", "HR", "Manager", "Director"].includes(role)) return;

    try {
      const res = await recruitmentApi.getPendingApprovals();
      const approvals = unwrapData<PendingApprovalDto>(res);

      approvals.forEach((item) => {
        if (item.moduleCode.startsWith("CONTRACT")) return;

        const module = mapCentralModule(item.moduleCode);
        const detailFields: Array<[string, string | number | null | undefined]> =
          item.detailFields && item.detailFields.length > 0
            ? item.detailFields.map((field) => [field.label, field.value] as [string, string | null | undefined])
            : [
                ["Mã tham chiếu", `#${item.referenceId}`],
                ["Cấp duyệt", String(item.level)],
                ["Vị trí", item.positionName],
                ["Phòng ban", item.departmentName],
                ["Số lượng", item.quantity ? `${item.quantity} nhân sự` : undefined],
                ["Mô tả", item.description],
              ];
        const centralItem: ApprovalItem = {
          id: `central-${item.moduleCode}-${item.referenceId}`,
          module,
          moduleLabel: moduleLabel(module),
          source: item.moduleCode,
          title: centralTitle(item),
          subtitle: centralSubtitle(item),
          owner: item.title,
          department: item.departmentName,
          status: item.status || "Pending",
          statusLabel: `Cấp duyệt ${item.level}`,
          date: item.createdAt,
          deadline: item.deadline,
          details: (
            <DetailFieldGrid
              fields={[
                ["Mã tham chiếu", `#${item.referenceId}`],
                ["Cấp duyệt", String(item.level)],
                ["Vị trí", item.positionName],
                ["Phòng ban", item.departmentName],
                ["Số lượng", item.quantity ? `${item.quantity} nhân sự` : undefined],
                ["Mô tả", item.description],
              ]}
            />
          ),
          actions: [
            centralAction(item, true),
            centralAction(item, false),
          ],
        };

        centralItem.statusLabel = item.statusLabel || `Chờ duyệt cấp ${item.level}`;
        centralItem.details = <DetailFieldGrid fields={detailFields} />;
        centralItem.actions = buildCentralActions(item);
        target.push(centralItem);
      });
    } catch {
      // Một số role không có quyền đọc inbox chung. Bỏ qua để các luồng riêng vẫn hiện.
    }
  };

  const loadProfileApprovals = async (target: ApprovalItem[]) => {
    if (!["Admin", "HR", "Director"].includes(role)) return;

    try {
      const res = await hrProfileApi.getPendingRequests();
      unwrapData<PendingProfileRequest>(res).forEach((item) => {
        target.push({
          id: `profile-${item.id}`,
          module: "PROFILE",
          moduleLabel: "Hồ sơ",
          source: "PROFILE_UPDATE",
          title: `Cập nhật hồ sơ: ${item.employeeName}`,
          subtitle: item.employeeCode,
          owner: item.employeeName,
          status: item.status || "Pending_HR",
          statusLabel: statusLabel(item.status || "Pending_HR"),
          date: item.createdAt,
          deadline: item.deadlineSLA,
          details: <JsonPreview value={item.requestedDataJson} />,
          actions: [
            {
              kind: "approve",
              label: "Duyệt",
              tone: "primary",
              run: () => hrProfileApi.reviewRequest(item.id, { isApproved: true }),
            },
            {
              kind: "reject",
              label: "Từ chối",
              tone: "danger",
              run: () =>
                hrProfileApi.reviewRequest(item.id, {
                  isApproved: false,
                  rejectReason: "HR từ chối cập nhật hồ sơ.",
                }),
            },
          ],
        });
      });
    } catch {
      // Bỏ qua nếu API không khả dụng cho role hiện tại.
    }
  };

  const loadDependentApprovals = async (target: ApprovalItem[]) => {
    if (!["Admin", "HR", "Director"].includes(role)) return;

    try {
      const res = await dependentApi.getPendingRequests();
      unwrapData<PendingDependentRequest>(res).forEach((item) => {
        target.push({
          id: `dependent-${item.id}`,
          module: "PROFILE",
          moduleLabel: "Hồ sơ",
          source: "DEPENDENT_UPDATE",
          title: `Người phụ thuộc: ${item.employeeName}`,
          subtitle: `${item.employeeCode} - ${dependentActionLabel(item.actionType)}`,
          owner: item.employeeName,
          status: item.status || "Pending_HR",
          statusLabel: statusLabel(item.status || "Pending_HR"),
          date: item.createdAt,
          details: <JsonPreview value={item.requestedDataJson} />,
          actions: [
            {
              kind: "approve",
              label: "Duyệt",
              tone: "primary",
              run: () => dependentApi.reviewRequest(item.id, { isApproved: true }),
            },
            {
              kind: "reject",
              label: "Từ chối",
              tone: "danger",
              run: () =>
                dependentApi.reviewRequest(item.id, {
                  isApproved: false,
                  rejectReason: "Từ chối yêu cầu người phụ thuộc.",
                }),
            },
          ],
        });
      });
    } catch {
      // Bỏ qua nếu role hiện tại không có quyền xem yêu cầu người phụ thuộc.
    }
  };

  const loadOnboardingApprovals = async (target: ApprovalItem[]) => {
    if (!["Admin", "HR"].includes(role)) return;

    try {
      const res = await onboardingApi.getPendingRequests();
      unwrapData<PendingOnboardingRequest>(res).forEach((item) => {
        const selection = onboardingReviewSelectionsRef.current[item.id] || {};
        const selectedRoleId = selection.roleId || defaultEmployeeRoleId;
        const selectedDepartmentId = selection.departmentId || item.departmentId || "";
        const selectedPositionId = selection.positionId || item.positionId || "";

        target.push({
          id: `onboarding-${item.id}`,
          module: "ONBOARDING",
          moduleLabel: "Tiếp nhận hồ sơ",
          source: "ONBOARDING",
          title: `Thiết lập hồ sơ mới #${item.id}`,
          subtitle: [
            `Ứng viên #${item.candidateId}`,
            item.positionName,
            item.departmentName,
          ].filter(Boolean).join(" · "),
          owner: readOnboardingText(item.requestedDataJson, "FullName"),
          department: item.departmentName,
          status: item.status,
          statusLabel: statusLabel(item.status),
          date: item.createdAt,
          details: (
            <div className="space-y-3">
              <JsonPreview value={item.requestedDataJson} />
              <DetailFieldGrid
                fields={[
                  ["Phòng ban đề xuất", item.departmentName],
                  ["Vị trí đề xuất", item.positionName],
                  ["Yêu cầu tuyển dụng", item.recruitmentRequestId ? `#${item.recruitmentRequestId}` : undefined],
                ]}
              />
              <div className="grid gap-3 md:grid-cols-3">
                <label className="block">
                  <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
                    Vai trò khi kích hoạt
                  </span>
                  <select
                    className={fieldClass}
                    defaultValue={selectedRoleId}
                    onChange={(event) =>
                      setOnboardingReviewSelection(item.id, {
                        roleId: Number(event.target.value),
                      })
                    }
                  >
                    {normalizedRoleOptions.map((option) => (
                      <option key={option.id} value={option.id}>
                        {option.name}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="block">
                  <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
                    Phòng ban *
                  </span>
                  <select
                    className={fieldClass}
                    defaultValue={selectedDepartmentId}
                    onChange={(event) =>
                      setOnboardingReviewSelection(item.id, {
                        departmentId: toOptionalNumber(event.target.value),
                      })
                    }
                  >
                    <option value="">Chọn phòng ban</option>
                    {onboardingDepartments.map((department) => (
                      <option key={department.id} value={department.id}>
                        {department.deptName}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="block">
                  <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
                    Vị trí *
                  </span>
                  <select
                    className={fieldClass}
                    defaultValue={selectedPositionId}
                    onChange={(event) =>
                      setOnboardingReviewSelection(item.id, {
                        positionId: toOptionalNumber(event.target.value),
                      })
                    }
                  >
                    <option value="">Chọn vị trí</option>
                    {onboardingPositions.map((position) => (
                      <option key={position.id} value={position.id}>
                        {position.title}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
            </div>
          ),
          actions: [
            {
              kind: "approve",
              label: "Kích hoạt",
              tone: "primary",
              run: () => submitOnboardingApproval(item),
            },
            {
              kind: "reject",
              label: "Từ chối",
              tone: "danger",
              run: () =>
                onboardingApi.reviewRequest(item.id, {
                  isApproved: false,
                  rejectReason: "HR từ chối hồ sơ onboarding.",
                }),
            },
          ],
        });
      });
    } catch {
      // Bỏ qua nếu role không được phép.
    }
  };

  const loadContractWorkItems = async (target: ApprovalItem[]) => {
    if (["Admin", "Manager"].includes(role)) {
      try {
        const res = await contractApi.getPendingRequests();
        unwrapData<ContractDto>(res).forEach((item) => {
          target.push(mapContractItem(item, "dept"));
        });
      } catch {
        // Bỏ qua nếu không được phép.
      }
    }

    if (["Admin", "Director"].includes(role)) {
      try {
        const res = await contractApi.getDirectorPending();
        unwrapData<ContractDto>(res).forEach((item) => {
          target.push(mapContractItem(item, "director"));
        });
      } catch {
        // Bỏ qua nếu không được phép.
      }
    }

    if (!["Admin", "HR"].includes(role)) return;

    try {
      const res = await contractApi.getHrPendingRequests();
      unwrapData<ContractDto>(res).forEach((item) => {
        target.push({
          id: `contract-hr-${item.id}`,
          module: "CONTRACT",
          moduleLabel: "Hợp đồng",
          source: "CONTRACT_HR_DRAFT",
          title: `Soạn thảo hợp đồng: ${item.employeeName || `#${item.employeeId}`}`,
          subtitle: item.negotiationNote || "Chờ HR lập hoặc cập nhật bản nháp.",
          owner: item.employeeName,
          status: item.status,
          statusLabel: statusLabel(item.status),
          date: item.startDate,
          deadline: item.endDate,
          details: (
            <DetailFieldGrid
              fields={[
                ["Nhân sự", item.employeeName || `#${item.employeeId}`],
                ["Ngày bắt đầu", formatDate(item.startDate)],
                ["Ngày kết thúc", formatDate(item.endDate)],
                ["Ghi chú thương lượng", item.negotiationNote],
              ]}
            />
          ),
          actions: [
            {
              kind: "open",
              label: "Mở màn soạn",
              tone: "secondary",
              run: () => navigate("/employees/hr-contract-management"),
            },
          ],
        });
      });
    } catch {
      // Không phải mọi role đều đọc được hợp đồng HR.
    }
  };

  const loadAddendumApprovals = async (target: ApprovalItem[]) => {
    if (["Admin", "Manager"].includes(role)) {
      try {
        const res = await contractAddendumApi.getPendingDept();
        unwrapData<ContractAddendumDto>(res).forEach((item) =>
          target.push(mapAddendumItem(item, "dept")),
        );
      } catch {
        // Bỏ qua nếu không được phép.
      }
    }

    if (["Admin", "HR"].includes(role)) {
      try {
        const res = await contractAddendumApi.getPendingHr();
        unwrapData<ContractAddendumDto>(res).forEach((item) =>
          target.push(mapAddendumItem(item, "hr")),
        );
      } catch {
        // Bỏ qua nếu không được phép.
      }
    }

    if (!["Admin", "Director"].includes(role)) return;

    try {
      const res = await contractAddendumApi.getPendingDirector();
      unwrapData<ContractAddendumDto>(res).forEach((item) => {
        target.push(mapAddendumItem(item, "director"));
      });
    } catch {
      // Bỏ qua nếu không được phép.
    }
  };

  const loadOvertimeApprovals = async (target: ApprovalItem[]) => {
    if (["Admin", "Manager"].includes(role)) {
      try {
        const res = await overtimeApi.getPendingManager();
        unwrapData<OvertimeRequest>(res).forEach((item) =>
          target.push(mapOvertimeItem(item, "manager")),
        );
      } catch {
        // Bỏ qua.
      }
    }

    if (["Admin", "HR"].includes(role)) {
      try {
        const res = await overtimeApi.getPendingHr();
        unwrapData<OvertimeRequest>(res).forEach((item) =>
          target.push(mapOvertimeItem(item, "hr")),
        );
      } catch {
        // Bỏ qua.
      }

      if (includeOvertimeReconcileInApprovalInbox) {
        const res = { data: [] } as { data: OvertimeRequest[] };
        unwrapData<OvertimeRequest>(res)
          .filter((item) => !item.isPayrollLocked && !item.reconciledAt)
          .forEach((item) =>
            target.push({
              ...mapOvertimeBase(item),
              id: `ot-reconcile-${item.id}`,
              source: "OVERTIME_RECONCILE",
              statusLabel: "Đã duyệt, cần đối chiếu",
              actions: [
                {
                  kind: "reconcile",
                  label: "Đối chiếu",
                  tone: "secondary",
                  run: () => overtimeApi.reconcile(item.id),
                },
              ],
            }),
          );
      } else {
        // Bỏ qua.
      }
    }

    if (["Admin", "Director"].includes(role)) {
      try {
        const res = await overtimeApi.getPendingDirector();
        unwrapData<OvertimeRequest>(res).forEach((item) =>
          target.push(mapOvertimeItem(item, "director")),
        );
      } catch {
        // Bỏ qua.
      }
    }
  };

  const loadLeaveApprovals = async (target: ApprovalItem[]) => {
    if (["Admin", "Manager"].includes(role)) {
      try {
        const res = await leaveRequestApi.getPendingDept();
        unwrapData<LeaveRequest>(res).forEach((item) =>
          target.push(mapLeaveItem(item, "dept")),
        );
      } catch {
        // Bỏ qua.
      }
    }

    if (["Admin", "Director"].includes(role)) {
      try {
        const res = await leaveRequestApi.getPendingDirector();
        unwrapData<LeaveRequest>(res).forEach((item) =>
          target.push(mapLeaveItem(item, "director")),
        );
      } catch {
        // Bỏ qua.
      }
    }
  };

  const loadPayrollWorkItems = async (target: ApprovalItem[]) => {
    if (!["Admin", "HR", "Director"].includes(role)) return;

    const { month, year, period } = currentPayrollPeriod();

    if (["Admin", "Director"].includes(role)) {
      try {
        const payrollRunsRes = await payrollApi.getPendingPayrollRuns();
        (payrollRunsRes.data ?? []).forEach((item) => {
          target.push(mapPayrollRunItem(item));
        });
      } catch {
        // Skip payroll run approvals when the current role cannot approve payroll.
      }

      try {
        const formulasRes = await payrollApi.getPayrollFormulas("PendingDirectorApproval");
        (formulasRes.data ?? []).forEach((item) => {
          target.push(mapPayrollFormulaItem(item));
        });
      } catch {
        // Skip payroll formula approvals when the current role cannot approve payroll formulas.
      }

      try {
        const projectBonusRes = await payrollApi.getPendingProjectBonusImports();
        (projectBonusRes.data ?? []).forEach((item) => {
          target.push(mapProjectBonusImportItem(item));
        });
      } catch {
        // Skip project bonus imports when the current role cannot approve payroll inputs.
      }

      try {
        const timesheetRes = await payrollApi.getPendingExternalTimesheetImports();
        (timesheetRes.data ?? []).forEach((item) => {
          target.push(mapExternalTimesheetImportItem(item));
        });
      } catch {
        // Skip external timesheet imports when the current role cannot approve payroll inputs.
      }
    }

    if (!["Admin", "HR"].includes(role)) return;

    try {
      const adjustmentsRes = await payrollApi.getAdjustments(month, year);
      const adjustments = (adjustmentsRes.data ?? []).filter((item) =>
        ["Draft", "PendingApproval"].includes(item.status),
      );

      adjustments.forEach((item) => {
        target.push(mapPayrollAdjustmentItem(item, period));
      });
    } catch {
      // Some adjustment workflows do not require manual approval.
    }
  };

  const loadPersonnelChangeApprovals = async (target: ApprovalItem[]) => {
    const statuses = getPersonnelChangeStatusesForRole(role);
    if (statuses.length === 0) return;

    const seen = new Set<number>();

    await Promise.all(
      statuses.map(async (status) => {
        try {
          const res = await personnelChangeApi.getList({ status });
          (res.data ?? []).forEach((item) => {
            if (seen.has(item.id)) return;
            seen.add(item.id);
            target.push(mapPersonnelChangeItem(item));
          });
        } catch {
          // Some roles cannot read every personnel-change status; other adapters still load.
        }
      }),
    );
  };

  const loadPerformanceApprovals = async (target: ApprovalItem[]) => {
    if (!["Admin", "Manager", "HR", "Director"].includes(role)) return;

    try {
      const res = await performanceApi.getPending();
      (res.data ?? []).forEach((item) => {
        target.push(mapPerformanceEvaluationItem(item));
      });
    } catch {
      // Some roles do not have permission to evaluate KPI.
    }
  };

  const normalizedRoleOptions = useMemo(() => {
    if (roleOptions.length === 0) {
      return [{ id: 5, name: "Employee" }];
    }

    return roleOptions.map((role) => ({
      id: role.id,
      name: role.name || role.roleName || `Vai trò #${role.id}`,
    }));
  }, [roleOptions]);

  const setOnboardingReviewSelection = (
    requestId: number,
    patch: OnboardingReviewSelection,
  ) => {
    onboardingReviewSelectionsRef.current[requestId] = {
      ...(onboardingReviewSelectionsRef.current[requestId] || {}),
      ...patch,
    };
  };

  const submitOnboardingApproval = (item: PendingOnboardingRequest) => {
    const selection = onboardingReviewSelectionsRef.current[item.id] || {};
    const roleId = selection.roleId || defaultEmployeeRoleId;
    const departmentId = selection.departmentId || item.departmentId;
    const positionId = selection.positionId || item.positionId;

    if (!departmentId || !positionId) {
      throw new Error("Vui lòng chọn phòng ban và vị trí trước khi kích hoạt nhân viên.");
    }

    return onboardingApi.reviewRequest(item.id, {
      isApproved: true,
      roleId,
      departmentId,
      positionId,
    });
  };

  useEffect(() => {
    if (!["Admin", "HR"].includes(role)) return;

    void Promise.all([
      accountApi.getSystemRoles(),
      recruitmentApi.getDepartmentsTree(),
      recruitmentApi.getPositions(),
    ])
      .then(([rolesRes, departmentsRes, positionsRes]) => {
        setRoleOptions(unwrapData<RoleOption>(rolesRes));
        setOnboardingDepartments(flattenDepartments(unwrapLookupList<OnboardingDepartmentOption>(departmentsRes)));
        setOnboardingPositions(unwrapLookupList<OnboardingPositionOption>(positionsRes));
      })
      .catch(() => {
        setRoleOptions([]);
        setOnboardingDepartments([]);
        setOnboardingPositions([]);
      });
  }, [role]);

  useEffect(() => {
    void fetchItems();
  }, [fetchItems]);

  useEffect(() => {
    setFiltersState((prev) =>
      prev.module === moduleFromQuery ? prev : { ...prev, module: moduleFromQuery },
    );
  }, [moduleFromQuery]);

  const defaultEmployeeRoleId = useMemo(() => {
    const found = normalizedRoleOptions.find((item) =>
      ["Employee", "Nhân viên"].includes(item.name),
    );
    return found?.id || normalizedRoleOptions[0]?.id || 5;
  }, [normalizedRoleOptions]);

  const selectedItem = useMemo(
    () => items.find((item) => item.id === selectedItemId) || null,
    [items, selectedItemId],
  );

  const statusOptions = useMemo(() => {
    const map = new Map<string, string>();
    items.forEach((item) => {
      if (!item.status) return;
      map.set(item.status, item.statusLabel || statusLabel(item.status));
    });

    return Array.from(map.entries())
      .map(([value, label]) => ({ value, label }))
      .sort((a, b) => a.label.localeCompare(b.label, "vi"));
  }, [items]);

  const filteredItems = useMemo(() => {
    const query = normalizeText(filters.query);
    const owner = normalizeText(filters.owner);
    const from = filters.fromDate ? new Date(filters.fromDate) : null;
    const to = filters.toDate ? new Date(filters.toDate) : null;

    return items.filter((item) => {
      if (filters.module !== "ALL" && item.module !== filters.module) {
        return false;
      }

      if (filters.status !== "ALL" && item.status !== filters.status) {
        return false;
      }

      if (
        filters.deadline !== "ALL" &&
        getDeadlineBucket(item.deadline) !== filters.deadline
      ) {
        return false;
      }

      if (owner) {
        const ownerText = normalizeText(`${item.owner} ${item.title}`);
        if (!ownerText.includes(owner)) return false;
      }

      if (query) {
        const haystack = normalizeText(
          `${item.title} ${item.subtitle} ${item.owner} ${item.department} ${item.statusLabel}`,
        );
        if (!haystack.includes(query)) return false;
      }

      if (from || to) {
        const date = item.date ? new Date(item.date) : null;
        if (!date || !isValidDate(date)) return false;
        if (from && date < from) return false;
        if (to && date > to) return false;
      }

      return true;
    });
  }, [filters, items]);

  const metrics = useMemo(() => {
    const base =
      filters.module === "ALL"
        ? items
        : items.filter((item) => item.module === filters.module);

    return {
      total: base.length,
      overdue: base.filter((item) => getDeadlineBucket(item.deadline) === "OVERDUE").length,
      today: base.filter((item) => getDeadlineBucket(item.deadline) === "TODAY").length,
      visible: filteredItems.length,
    };
  }, [filteredItems.length, filters.module, items]);

  const executeAction = (item: ApprovalItem, action: ApprovalAction) => {
    if (action.kind === "open") {
      void action.run();
      return;
    }

    triggerAlert(
      "confirm",
      action.kind === "reject"
        ? "Xác nhận từ chối"
        : action.kind === "revision"
          ? "Yêu cầu bổ sung/chỉnh sửa"
          : "Xác nhận xử lý",
      `Bạn muốn ${action.label.toLowerCase()} yêu cầu "${item.title}"?`,
      async () => {
        try {
          await action.run();
          triggerAlert("success", "Đã xử lý", "Yêu cầu đã được cập nhật.");
          await fetchItems();
          setSelectedItemId(null);
        } catch (error) {
          triggerAlert("error", "Không thể xử lý", getErrorMessage(error));
        }
      },
    );
  };

  const handleApprovalAction = (item: ApprovalItem, action: ApprovalAction) => {
    if (action.kind === "open") {
      void action.run();
      return;
    }

    if (action.kind === "reconcile") {
      void Promise.resolve(action.run())
        .then(async () => {
          triggerAlert("success", "Đã cập nhật", "Yêu cầu đã được xử lý.");
          await fetchItems();
          setSelectedItemId(null);
        })
        .catch((error) => {
          triggerAlert("error", "Không thể xử lý", getErrorMessage(error));
        });
      return;
    }

    setPendingAction({ item, action });
    setActionNote("");
  };

  const submitPendingAction = async () => {
    if (!pendingAction) return;

    const note = actionNote.trim();
    const requiresNote =
      pendingAction.action.requiresNote ||
      pendingAction.action.kind === "reject" ||
      pendingAction.action.kind === "revision";

    if (requiresNote && !note) {
      triggerAlert(
        "error",
        "Thiếu ghi chú",
        pendingAction.action.kind === "revision"
          ? "Vui lòng nhập nội dung cần bổ sung hoặc chỉnh sửa trước khi gửi."
          : "Vui lòng nhập lý do từ chối trước khi gửi.",
      );
      return;
    }

    setActionSubmitting(true);
    try {
      await pendingAction.action.run(note);
      triggerAlert("success", "Đã xử lý", "Yêu cầu đã được cập nhật.");
      setPendingAction(null);
      setActionNote("");
      await fetchItems();
      setSelectedItemId(null);
    } catch (error) {
      triggerAlert("error", "Không thể xử lý", getErrorMessage(error));
    } finally {
      setActionSubmitting(false);
    }
  };

  void executeAction;

  return (
    <FeaturePage
      title="Phê duyệt"
      description={`Xử lý các yêu cầu đang chờ theo vai trò ${roleLabel(role)}.`}
      actions={
        <button className={secondaryButtonClass} onClick={() => fetchItems()}>
          <RefreshCw size={16} />
          Làm mới
        </button>
      }
      width="wide"
    >
      <CleanFilterPanel
        filters={filters}
        setFilters={setFilters}
        statusOptions={statusOptions}
      />
      <CleanApprovalSummaryStrip metrics={metrics} />

      {!isApprovalRole(role) ? (
        <FeatureCard>
          <EmptyState
            title="Vai trò hiện tại không có hàng đợi phê duyệt"
            description="Bạn vẫn có thể theo dõi trạng thái các yêu cầu liên quan ở trang Theo dõi trạng thái."
          />
        </FeatureCard>
      ) : (
        <FeatureCard
          title="Danh sách cần xử lý"
          description={`${filteredItems.length} yêu cầu đang hiển thị.`}
        >
          {loading ? (
            <div className="py-10 text-center text-sm text-gray-500">
              Đang tải dữ liệu...
            </div>
          ) : filteredItems.length === 0 ? (
            <EmptyState title="Chưa có yêu cầu phù hợp" />
          ) : (
            <div className="space-y-3">
              {filteredItems.map((item) => (
                <CleanApprovalRow
                  key={item.id}
                  item={item}
                  onAction={handleApprovalAction}
                  onOpenDetail={() => setSelectedItemId(item.id)}
                />
              ))}
            </div>
          )}
        </FeatureCard>
      )}

      <CleanApprovalDetailDrawer
        item={selectedItem}
        open={Boolean(selectedItem)}
        onClose={() => setSelectedItemId(null)}
        onAction={handleApprovalAction}
      />
      <ApprovalActionNoteDialog
        pendingAction={pendingAction}
        note={actionNote}
        submitting={actionSubmitting}
        onChangeNote={setActionNote}
        onCancel={() => {
          if (actionSubmitting) return;
          setPendingAction(null);
          setActionNote("");
        }}
        onSubmit={submitPendingAction}
      />
    </FeaturePage>
  );

  function centralAction(
    item: PendingApprovalDto,
    isApproved: boolean,
    action: "approve" | "reject" | "revision" = isApproved ? "approve" : "reject",
    label?: string,
  ): ApprovalAction {
    return {
      kind: action,
      label: label || (isApproved ? "Duyệt" : action === "revision" ? "Yêu cầu bổ sung" : "Từ chối"),
      tone: isApproved ? "primary" : action === "revision" ? "secondary" : "danger",
      requiresNote: action !== "approve",
      run: (note) =>
        recruitmentApi.reviewRequest({
          moduleCode: item.moduleCode,
          referenceId: item.referenceId,
          isApproved,
          action,
          note: note || "",
        }),
    };
  }

  function buildCentralActions(item: PendingApprovalDto): ApprovalAction[] {
    const backendActions = item.actions || [];
    if (backendActions.length === 0) {
      return [
        centralAction(item, true),
        centralAction(item, false),
      ];
    }

    return backendActions
      .map((action) => mapBackendApprovalAction(item, action))
      .filter((action): action is ApprovalAction => Boolean(action));
  }

  function mapBackendApprovalAction(
    item: PendingApprovalDto,
    action: PendingApprovalActionDto,
  ): ApprovalAction | null {
    if (action.kind === "open") {
      const route = action.endpoint || item.detailRoute;
      if (!route) return null;

      return {
        kind: "open",
        label: action.label || "Xem chi tiết",
        tone: "secondary",
        run: () => navigate(route),
      };
    }

    if (action.kind === "approve") {
      return centralAction(item, true, "approve", action.label);
    }

    if (action.kind === "reject" || action.kind === "revision") {
      return centralAction(item, false, action.kind, action.label);
    }

    return null;
  }

  function mapOvertimeItem(
    item: OvertimeRequest,
    scope: "manager" | "hr" | "director",
  ): ApprovalItem {
    return {
      ...mapOvertimeBase(item),
      id: `ot-${scope}-${item.id}`,
      source: scope === "manager" ? "OVERTIME_MANAGER" : scope === "hr" ? "OVERTIME_HR" : "OVERTIME_DIRECTOR",
      actions: [
        {
          kind: "approve",
          label: scope === "manager" ? "Duyệt nghiệp vụ" : "HR xác nhận",
          tone: "primary",
          run: (note) =>
            scope === "manager"
              ? overtimeApi.managerReview(item.id, { isApproved: true, note })
              : scope === "hr"
                ? overtimeApi.hrConfirm(item.id, { isApproved: true, note })
                : overtimeApi.directorReview(item.id, { isApproved: true, note }),
        },
        {
          kind: "reject",
          label: "Từ chối",
          tone: "danger",
          run: (note) =>
            scope === "manager"
              ? overtimeApi.managerReview(item.id, { isApproved: false, note })
              : scope === "hr"
                ? overtimeApi.hrConfirm(item.id, { isApproved: false, note })
                : overtimeApi.directorReview(item.id, { isApproved: false, note }),
        },
      ],
    };
  }

  function mapOvertimeBase(item: OvertimeRequest): ApprovalItem {
    return {
      id: `ot-${item.id}`,
      module: "OVERTIME",
      moduleLabel: "Làm thêm giờ",
      source: "OVERTIME",
      title: `${item.employeeName} - ${item.workDate}`,
      subtitle: `${item.startTime} - ${item.endTime}`,
      owner: item.employeeName,
      department: item.departmentName,
      status: item.status,
      statusLabel: statusLabel(item.status),
      date: item.workDate,
      details: (
        <DetailFieldGrid
          fields={[
            ["Nhân sự", item.employeeName],
            ["Phòng ban", item.departmentName],
            ["Ngày làm thêm", formatDate(item.workDate)],
            ["Khung giờ", `${item.startTime || "-"} - ${item.endTime || "-"}`],
            ["Lý do", item.reason],
          ]}
        />
      ),
      actions: [],
    };
  }

  function mapLeaveItem(
    item: LeaveRequest,
    scope: "dept" | "director",
  ): ApprovalItem {
    return {
      id: `leave-${scope}-${item.id}`,
      module: "LEAVE",
      moduleLabel: "Nghỉ phép",
      source: scope === "dept" ? "LEAVE_DEPT" : "LEAVE_DIRECTOR",
      title: `${item.employeeName} - ${item.leaveTypeName}`,
      subtitle: `${formatDate(item.startDate)} - ${formatDate(item.endDate)} (${item.requestedDays} ngày)`,
      owner: item.employeeName,
      department: item.departmentName,
      status: item.status,
      statusLabel: statusLabel(item.status),
      date: item.startDate,
      deadline: item.deadlineAt,
      details: (
        <DetailFieldGrid
          fields={[
            ["Nhân sự", item.employeeName],
            ["Phòng ban", item.departmentName],
            ["Loại nghỉ", item.leaveTypeName],
            ["Thời gian", `${formatDate(item.startDate)} - ${formatDate(item.endDate)}`],
            ["Số ngày", `${item.requestedDays} ngày`],
            ["Lý do", item.reason],
          ]}
        />
      ),
      actions: [
        {
          kind: "approve",
          label: scope === "dept" ? "Trưởng phòng duyệt" : "Giám đốc duyệt",
          tone: "primary",
          run: () =>
            scope === "dept"
              ? leaveRequestApi.reviewByDept(item.id, true)
              : leaveRequestApi.finalApprove(item.id, true),
        },
        {
          kind: "reject",
          label: "Từ chối",
          tone: "danger",
          run: () =>
            scope === "dept"
              ? leaveRequestApi.reviewByDept(item.id, false)
              : leaveRequestApi.finalApprove(item.id, false),
        },
      ],
    };
  }

  function mapAddendumItem(
    item: ContractAddendumDto,
    scope: "dept" | "hr" | "director",
  ): ApprovalItem {
    const sourceMap = {
      dept: "ADDENDUM_DEPT",
      hr: "ADDENDUM_HR",
      director: "ADDENDUM_DIRECTOR",
    };

    const approveLabel = {
      dept: "Trưởng phòng xác nhận",
      hr: "HR xác nhận",
      director: "Giám đốc duyệt",
    };

    return {
      id: `addendum-${scope}-${item.id}`,
      module: "ADDENDUM",
      moduleLabel: "Phụ lục",
      source: sourceMap[scope],
      title: item.addendumNumber,
      subtitle: item.content || item.contractNumber,
      owner: item.employeeName || undefined,
      status: item.status,
      statusLabel: statusLabel(item.status),
      date: item.createdAt,
      deadline: item.effectiveDate,
      details: (
        <DetailFieldGrid
          fields={[
            ["Số phụ lục", item.addendumNumber],
            ["Hợp đồng", item.contractNumber],
            ["Nhân sự", item.employeeName],
            ["Ngày hiệu lực", formatDate(item.effectiveDate)],
            ["Nội dung", item.content],
          ]}
        />
      ),
      actions: [
        {
          kind: "approve",
          label: approveLabel[scope],
          tone: "primary",
          run: () =>
            scope === "dept"
              ? contractAddendumApi.deptReview(item.id, { isApproved: true })
              : scope === "hr"
                ? contractAddendumApi.hrConfirm(item.id, { isApproved: true })
                : contractAddendumApi.approve(item.id),
        },
        {
          kind: "revision",
          label: "Yêu cầu chỉnh sửa",
          tone: "secondary",
          run: (note) =>
            contractAddendumApi.requestRevision(item.id, {
              reason: note || "Yêu cầu HR chỉnh sửa phụ lục hợp đồng.",
            }),
        },
      ],
    };
  }

  function mapContractItem(
    item: ContractDto,
    scope: "dept" | "director",
  ): ApprovalItem {
    const isDept = scope === "dept";
    const isRevisionFlow = !isDept || item.status === "PendingManagerContentReview";

    return {
      id: `contract-${scope}-${item.id}`,
      module: "CONTRACT",
      moduleLabel: "Hợp đồng",
      source: isDept ? "CONTRACT_DEPT" : "CONTRACT_DIRECTOR",
      title: isDept
        ? `Xác nhận yêu cầu hợp đồng: ${item.employeeName || `#${item.employeeId}`}`
        : `Duyệt hợp đồng: ${item.employeeName || `#${item.employeeId}`}`,
      subtitle: item.negotiationNote || item.contractNumber,
      owner: item.employeeName || undefined,
      status: item.status,
      statusLabel: statusLabel(item.status),
      date: item.startDate,
      deadline: item.endDate,
      details: (
        <DetailFieldGrid
          fields={[
            ["Nhân sự", item.employeeName || `#${item.employeeId}`],
            ["Số hợp đồng", item.contractNumber],
            ["Loại hợp đồng", item.contractType],
            ["Ngày bắt đầu", formatDate(item.startDate)],
            ["Ngày kết thúc", formatDate(item.endDate)],
            ["Lương cơ bản", formatMoney(item.basicSalary)],
            ["Lương bảo hiểm", formatMoney(item.insuranceSalary)],
            ["Ghi chú thương lượng", item.negotiationNote],
          ]}
        />
      ),
      actions: [
        {
          kind: "approve",
          label: isDept ? "Trưởng phòng xác nhận" : "Giám đốc duyệt",
          tone: "primary",
          run: () =>
            isDept
              ? contractApi.deptReview(item.id, { isApproved: true })
              : contractApi.directorApprove(item.id, { isApproved: true }),
        },
        {
          kind: isRevisionFlow ? "revision" : "reject",
          label: isRevisionFlow ? "Yêu cầu chỉnh sửa" : "Từ chối",
          tone: isRevisionFlow ? "secondary" : "danger",
          run: (note) =>
            isRevisionFlow
              ? contractApi.requestRevision(item.id, {
                  reason: note || "Yêu cầu HR chỉnh sửa hợp đồng.",
                })
              : contractApi.deptReview(item.id, {
                  isApproved: false,
                  rejectReason: note || "Trưởng phòng từ chối yêu cầu hợp đồng.",
                }),
        },
      ],
    };
  }

  function mapPayrollRunItem(item: PayrollRunSummary): ApprovalItem {
    return {
      id: `payroll-run-${item.month}-${item.year}`,
      module: "PAYROLL",
      moduleLabel: "Lương",
      source: "PAYROLL_RUN_APPROVAL",
      title: `Bảng lương ${item.period}`,
      subtitle: `${formatNumber(item.slipCount)} phiếu - ${formatMoney(item.netSalary)} thực nhận`,
      owner: item.submittedByAccountId ? `Tài khoản #${item.submittedByAccountId}` : "HR/Kế toán",
      status: item.status,
      statusLabel: item.statusText || statusLabel(item.status),
      date: item.submittedAt || item.calculatedAt,
      details: (
        <DetailFieldGrid
          fields={[
            ["Kỳ lương", item.period],
            ["Số phiếu", formatNumber(item.slipCount)],
            ["Tổng thu nhập", formatMoney(item.grossIncome)],
            ["Tổng thực nhận", formatMoney(item.netSalary)],
            ["Chi phí công ty", formatMoney(item.totalCompanyCost)],
            ["Ngày tổng hợp", formatDate(item.calculatedAt)],
            ["Ngày gửi duyệt", formatDate(item.submittedAt)],
            ["Ngày duyệt", formatDate(item.approvedAt)],
            ["Trạng thái", item.statusText || statusLabel(item.status)],
            ["Ghi chú duyệt", item.reviewNote],
          ]}
        />
      ),
      actions: [
        {
          kind: "approve",
          label: "Duyệt",
          tone: "primary",
          run: (note) =>
            payrollApi.reviewPayrollRun({
              month: item.month,
              year: item.year,
              isApproved: true,
              requestRevision: false,
              note: note || "",
            }),
        },
        {
          kind: "revision",
          label: "Yêu cầu bổ sung",
          tone: "secondary",
          requiresNote: true,
          run: (note) =>
            payrollApi.reviewPayrollRun({
              month: item.month,
              year: item.year,
              isApproved: false,
              requestRevision: true,
              note: note || "",
            }),
        },
        {
          kind: "reject",
          label: "Từ chối",
          tone: "danger",
          requiresNote: true,
          run: (note) =>
            payrollApi.reviewPayrollRun({
              month: item.month,
              year: item.year,
              isApproved: false,
              requestRevision: false,
              note: note || "",
            }),
        },
        {
          kind: "open",
          label: "Mở bảng lương",
          tone: "secondary",
          run: () => navigate("/payroll/payroll-aggregation"),
        },
      ],
    };
  }

  function mapPayrollSlipItem(item: SalarySlip, period: string): ApprovalItem {
    return {
      id: `payroll-slip-${item.id}`,
      module: "PAYROLL",
      moduleLabel: "Lương",
      source: "PAYROLL_RUN",
      title: `Bảng lương ${period}: ${item.employeeName}`,
      subtitle: `${item.employeeCode} - ${formatMoney(item.netSalary)} thực nhận`,
      owner: item.employeeName,
      department: item.departmentName,
      status: item.status,
      statusLabel: statusLabel(item.status),
      date: item.calculatedAt,
      details: (
        <DetailFieldGrid
          fields={[
            ["Kỳ lương", item.period],
            ["Nhân sự", item.employeeName],
            ["Phòng ban", item.departmentName],
            ["Chức danh", item.positionName],
            ["Tổng thu nhập", formatMoney(item.grossIncome)],
            ["Thực nhận", formatMoney(item.netSalary)],
            ["Trạng thái", statusLabel(item.status)],
          ]}
        />
      ),
      actions: [
        {
          kind: "open",
          label: "Mở bảng lương",
          tone: "secondary",
          run: () => navigate("/payroll/payroll-aggregation"),
        },
      ],
    };
  }

  void mapPayrollSlipItem;

  function mapPayrollAdjustmentItem(item: PayrollAdjustment, period: string): ApprovalItem {
    return {
      id: `payroll-adjustment-${item.id}`,
      module: "PAYROLL",
      moduleLabel: "Lương",
      source: "PAYROLL_ADJUSTMENT",
      title: `Điều chỉnh lương ${period}: ${item.employeeName || `#${item.employeeId}`}`,
      subtitle: `${formatMoney(item.amount)} - ${item.adjustmentType}`,
      owner: item.employeeName || undefined,
      status: item.status,
      statusLabel: statusLabel(item.status),
      date: item.createdAt,
      details: (
        <DetailFieldGrid
          fields={[
            ["Kỳ ghi nhận", `${item.recognizedMonth}/${item.recognizedYear}`],
            ["Nhân sự", item.employeeName || item.employeeCode],
            ["Loại điều chỉnh", item.adjustmentType],
            ["Số tiền", formatMoney(item.amount)],
            ["Tính thuế", item.isTaxable ? "Có" : "Không"],
            ["Tính bảo hiểm", item.isInsuranceBased ? "Có" : "Không"],
            ["Khoản khấu trừ", item.isDeduction ? "Có" : "Không"],
            ["Lý do", item.reason],
          ]}
        />
      ),
      actions: [
        {
          kind: "open",
          label: "Mở điều chỉnh",
          tone: "secondary",
          run: () => navigate("/payroll/adjustments"),
        },
      ],
    };
  }

  function mapPayrollFormulaItem(item: PayrollFormula): ApprovalItem {
    const previewLines = item.lines?.slice(0, 8) ?? [];

    return {
      id: `payroll-formula-${item.id}`,
      module: "PAYROLL",
      moduleLabel: "Lương",
      source: "PAYROLL_FORMULA_APPROVAL",
      title: `Công thức lương: ${item.formulaName}`,
      subtitle: `${item.formulaCode} - v${item.version}${item.versionCode ? ` - ${item.versionCode}` : ""}`,
      owner: item.submittedAt ? "HR/Kế toán" : undefined,
      status: item.status,
      statusLabel: item.statusText || statusLabel(item.status),
      date: item.submittedAt || item.createdAt,
      deadline: item.deadlineAt,
      details: (
        <div className="space-y-4">
          <DetailFieldGrid
            fields={[
              ["Mã công thức", item.formulaCode],
              ["Tên công thức", item.formulaName],
              ["Phiên bản", `v${item.version}`],
              ["Mã phiên bản", item.versionCode],
              ["Hiệu lực từ", formatDate(item.effectiveFrom)],
              ["Hiệu lực đến", item.effectiveTo ? formatDate(item.effectiveTo) : "Không giới hạn"],
              ["Số dòng", item.lines?.length ?? 0],
              ["Trạng thái", item.statusText || statusLabel(item.status)],
              ["Ghi chú duyệt", item.reviewNote],
            ]}
          />

          {previewLines.length > 0 ? (
            <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white">
              <div className="border-b border-[var(--hicas-border)] px-4 py-3 text-sm font-semibold text-[var(--hicas-text-main)]">
                Dòng công thức
              </div>
              <div className="divide-y divide-[var(--hicas-border)]">
                {previewLines.map((line) => (
                  <div
                    key={`${line.calculationOrder}-${line.componentCode}`}
                    className="grid gap-2 px-4 py-3 text-sm md:grid-cols-[80px_1fr_1.4fr]"
                  >
                    <span className="font-semibold text-[var(--hicas-text-secondary)]">
                      #{line.calculationOrder}
                    </span>
                    <span className="font-semibold text-[var(--hicas-text-main)]">
                      {line.componentName || line.componentCode}
                    </span>
                    <code className="break-all rounded bg-[var(--hicas-bg-soft)] px-2 py-1 text-xs text-[var(--hicas-text-main)]">
                      {line.expression}
                    </code>
                  </div>
                ))}
              </div>
            </div>
          ) : null}
        </div>
      ),
      actions: [
        {
          kind: "approve",
          label: "Duyệt",
          tone: "primary",
          run: (note) =>
            payrollApi.reviewPayrollFormula(item.id, {
              isApproved: true,
              requestRevision: false,
              note: note || "",
            }),
        },
        {
          kind: "revision",
          label: "Yêu cầu chỉnh sửa",
          tone: "secondary",
          requiresNote: true,
          run: (note) =>
            payrollApi.reviewPayrollFormula(item.id, {
              isApproved: false,
              requestRevision: true,
              note: note || "",
            }),
        },
        {
          kind: "open",
          label: "Mở công thức",
          tone: "secondary",
          run: () => navigate("/payroll/salary-formula"),
        },
      ],
    };
  }

  function mapProjectBonusImportItem(item: ProjectBonusImportBatch): ApprovalItem {
    const previewLines = item.lines?.slice(0, 6) ?? [];

    return {
      id: `project-bonus-import-${item.id}`,
      module: "PAYROLL",
      moduleLabel: "Lương",
      source: "PROJECT_BONUS_IMPORT",
      title: `Thưởng dự án ${item.payrollPeriod}`,
      subtitle: `${item.validRows} dòng hợp lệ - ${formatMoney(item.totalAmount)}`,
      owner: item.uploadedByName || `Tài khoản #${item.uploadedByAccountId}`,
      status: item.status,
      statusLabel: item.statusText || statusLabel(item.status),
      date: item.createdAt,
      details: (
        <div className="space-y-4">
          <DetailFieldGrid
            fields={[
              ["Kỳ lương", item.payrollPeriod],
              ["File import", item.fileName],
              ["Người import", item.uploadedByName || `#${item.uploadedByAccountId}`],
              ["Số dòng hợp lệ", item.validRows],
              ["Dòng lỗi", item.errorRows],
              ["Tổng thưởng", formatMoney(item.totalAmount)],
              ["Trạng thái", item.statusText || statusLabel(item.status)],
              ["Ghi chú", item.note],
            ]}
          />

          {previewLines.length > 0 ? (
            <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white">
              <div className="border-b border-[var(--hicas-border)] px-4 py-3 text-sm font-semibold text-[var(--hicas-text-main)]">
                Dòng thưởng trong batch
              </div>
              <div className="divide-y divide-[var(--hicas-border)]">
                {previewLines.map((line) => (
                  <div
                    key={`${line.rowNumber}-${line.employeeCode}-${line.projectCode}`}
                    className="grid gap-2 px-4 py-3 text-sm md:grid-cols-[1.2fr_1fr_auto]"
                  >
                    <div>
                      <p className="font-semibold text-[var(--hicas-text-main)]">
                        {line.employeeName || line.employeeCode}
                      </p>
                      <p className="text-[var(--hicas-text-secondary)]">{line.employeeCode}</p>
                    </div>
                    <div>
                      <p className="font-semibold text-[var(--hicas-text-main)]">{line.projectName}</p>
                      <p className="text-[var(--hicas-text-secondary)]">{line.projectCode}</p>
                    </div>
                    <div className="text-right font-semibold text-[var(--hicas-orange)]">
                      {formatMoney(line.bonusAmount)}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ) : null}
        </div>
      ),
      actions: [
        {
          kind: "approve",
          label: "Duyệt",
          tone: "primary",
          run: (note) =>
            payrollApi.reviewProjectBonusImport(item.id, {
              isApproved: true,
              note: note || "",
            }),
        },
        {
          kind: "reject",
          label: "Từ chối",
          tone: "danger",
          run: (note) =>
            payrollApi.reviewProjectBonusImport(item.id, {
              isApproved: false,
              note: note || "",
            }),
        },
        {
          kind: "open",
          label: "Mở thưởng dự án",
          tone: "secondary",
          run: () => navigate("/payroll/project-bonuses"),
        },
      ],
    };
  }

  function mapExternalTimesheetImportItem(item: ExternalTimesheetImportBatch): ApprovalItem {
    const previewLines = item.lines?.slice(0, 6) ?? [];

    return {
      id: `external-timesheet-import-${item.id}`,
      module: "PAYROLL",
      moduleLabel: "Lương",
      source: "EXTERNAL_TIMESHEET_IMPORT",
      title: `Giờ công cộng tác viên ${item.payrollPeriod}`,
      subtitle: `${item.validRows} dòng hợp lệ - ${formatNumber(item.totalHours)} giờ - ${formatMoney(item.totalAmount)}`,
      owner: item.importedByName || `Tài khoản #${item.importedByAccountId}`,
      status: item.status,
      statusLabel: item.statusText || statusLabel(item.status),
      date: item.importedAt,
      details: (
        <div className="space-y-4">
          <DetailFieldGrid
            fields={[
              ["Kỳ lương", item.payrollPeriod],
              ["Nguồn dữ liệu", item.sourceSystem],
              ["File import", item.fileName],
              ["Người import", item.importedByName || `#${item.importedByAccountId}`],
              ["Số dòng hợp lệ", item.validRows],
              ["Dòng lỗi", item.errorRows],
              ["Tổng giờ", formatNumber(item.totalHours)],
              ["Tổng tiền", formatMoney(item.totalAmount)],
              ["Trạng thái", item.statusText || statusLabel(item.status)],
              ["Ghi chú", item.note],
            ]}
          />

          {previewLines.length > 0 ? (
            <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white">
              <div className="border-b border-[var(--hicas-border)] px-4 py-3 text-sm font-semibold text-[var(--hicas-text-main)]">
                Dòng giờ công trong batch
              </div>
              <div className="divide-y divide-[var(--hicas-border)]">
                {previewLines.map((line) => (
                  <div
                    key={`${line.rowNumber}-${line.collaboratorCode}-${line.projectCode}-${line.taskCode}`}
                    className="grid gap-2 px-4 py-3 text-sm md:grid-cols-[1.2fr_1fr_auto]"
                  >
                    <div>
                      <p className="font-semibold text-[var(--hicas-text-main)]">
                        {line.collaboratorName || line.collaboratorCode}
                      </p>
                      <p className="text-[var(--hicas-text-secondary)]">
                        {line.collaboratorCode} - {line.workDateText || line.workDate}
                      </p>
                    </div>
                    <div>
                      <p className="font-semibold text-[var(--hicas-text-main)]">{line.projectCode}</p>
                      <p className="text-[var(--hicas-text-secondary)]">{line.taskCode || "Không có mã công việc"}</p>
                    </div>
                    <div className="text-right">
                      <p className="font-semibold text-[var(--hicas-orange)]">{formatMoney(line.amount)}</p>
                      <p className="text-xs text-[var(--hicas-text-secondary)]">
                        {formatNumber(line.approvedHours)} giờ
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ) : null}
        </div>
      ),
      actions: [
        {
          kind: "approve",
          label: "Duyệt",
          tone: "primary",
          run: (note) =>
            payrollApi.reviewExternalTimesheetImport(item.id, {
              isApproved: true,
              note: note || "",
            }),
        },
        {
          kind: "reject",
          label: "Từ chối",
          tone: "danger",
          run: (note) =>
            payrollApi.reviewExternalTimesheetImport(item.id, {
              isApproved: false,
              note: note || "",
            }),
        },
        {
          kind: "open",
          label: "Mở giờ công CTV",
          tone: "secondary",
          run: () => navigate("/payroll/external-timesheets"),
        },
      ],
    };
  }

  function mapPerformanceEvaluationItem(item: PerformanceEvaluation): ApprovalItem {
    return {
      id: `performance-${item.id}`,
      module: "PERFORMANCE",
      moduleLabel: "Hiệu suất",
      source: "PERFORMANCE_APPROVAL",
      title: `Chấm KPI: ${item.employeeName}`,
      subtitle: `Kỳ ${item.period} - ${item.details.length} chỉ tiêu`,
      owner: item.employeeName,
      department: item.departmentName,
      status: item.status,
      statusLabel: statusLabel(item.status),
      date: item.period,
      details: (
        <DetailFieldGrid
          fields={[
            ["Nhân sự", item.employeeName],
            ["Phòng ban", item.departmentName],
            ["Kỳ KPI", item.period],
            ["Số chỉ tiêu", item.details.length],
            ["Tổng trọng số", `${formatNumber(item.totalWeight)}%`],
            ["Điểm trừ hệ thống", formatNumber(item.systemPenaltyPoint)],
            ["Điểm hiện tại", formatNumber(item.totalScore)],
            ["Phiên bản chấm điểm", item.scoringVersion],
          ]}
        />
      ),
      actions: [
        {
          kind: "open",
          label: "Mở chấm KPI",
          tone: "secondary",
          run: () => navigate("/tasks/performance-evaluation"),
        },
      ],
    };
  }

  function mapPersonnelChangeItem(item: PersonnelChangeListItem): ApprovalItem {
    const workflow = resolvePersonnelChangeWorkflow(item);
    const typeLabel = personnelChangeTypeLabel(item.changeType);

    return {
      id: `personnel-change-${item.id}`,
      module: "PERSONNEL_CHANGE",
      moduleLabel: "Biến động nhân sự",
      source: `PERSONNEL_CHANGE_${getPersonnelChangeStatusLabel(item.status)}`,
      title: `${typeLabel}: ${item.employeeName || `#${item.employeeId}`}`,
      subtitle: item.reason || item.employeeCode || undefined,
      owner: item.employeeName || item.requestedByName || undefined,
      status: String(item.status),
      statusLabel: personnelChangeStatusLabel(item.status),
      date: item.requestedAt,
      deadline: item.effectiveDate,
      details: (
        <DetailFieldGrid
          fields={[
            ["Loại biến động", typeLabel],
            ["Nhân sự", item.employeeName || item.employeeCode],
            ["Người tạo", item.requestedByName],
            ["Ngày yêu cầu", formatDate(item.requestedAt)],
            ["Ngày hiệu lực", formatDate(item.effectiveDate)],
            ["Trạng thái", personnelChangeStatusLabel(item.status)],
            ["Cần nhân viên xác nhận", item.requiresEmployeeConsent ? "Có" : "Không"],
            ["Cần hợp đồng/phụ lục", item.requiresContractFlow ? "Có" : "Không"],
            ["Cần giám đốc duyệt", item.requiresDirectorApproval ? "Có" : "Không"],
            ["Lý do", item.reason],
          ]}
        />
      ),
      actions: personnelChangeActionsWithNotes(item, workflow),
    };
  }

  function personnelChangeActions(
    item: PersonnelChangeListItem,
    workflow: PersonnelChangeWorkflowKind,
  ): ApprovalAction[] {
    const openAction: ApprovalAction = {
      kind: "open",
      label: "Mở hồ sơ",
      tone: "secondary",
      run: () => navigate(personnelChangeRoute(workflow)),
    };

    if (item.status === PersonnelChangeStatus.PendingManagerReview && workflow === "termination") {
      return [
        personnelChangeDecision("Duyệt", true, () =>
          personnelChangeApi.managerReviewResignation(item.id, { isApproved: true, note: "" }),
        ),
        personnelChangeDecision("Từ chối", false, () =>
          personnelChangeApi.managerReviewResignation(item.id, { isApproved: false, note: "" }),
        ),
        openAction,
      ];
    }

    if (
      item.status === PersonnelChangeStatus.PendingCurrentManagerOpinion &&
      workflow === "internal-transfer"
    ) {
      return [
        personnelChangeDecision("Đồng ý", true, () =>
          personnelChangeApi.submitCurrentManagerOpinion(item.id, {
            isApproved: true,
            opinion: "",
          }),
        ),
        personnelChangeDecision("Từ chối", false, () =>
          personnelChangeApi.submitCurrentManagerOpinion(item.id, {
            isApproved: false,
            opinion: "",
          }),
        ),
        openAction,
      ];
    }

    if (item.status === PersonnelChangeStatus.PendingEmployeeConsent) {
      if (workflow === "internal-transfer" || workflow === "senior-appointment") {
        return [
          personnelChangeDecision("Đồng ý", true, () =>
            personnelChangeApi.employeeConsent(item.id, { isAccepted: true, note: "" }, workflow),
          ),
          personnelChangeDecision("Từ chối", false, () =>
            personnelChangeApi.employeeConsent(item.id, { isAccepted: false, note: "" }, workflow),
          ),
          openAction,
        ];
      }
    }

    if (item.status === PersonnelChangeStatus.PendingHRReview && workflow === "promotion") {
      return [
        personnelChangeDecision("HR duyệt", true, () =>
          personnelChangeApi.hrReviewPromotion(item.id, { isApproved: true, note: "" }),
        ),
        personnelChangeDecision("Từ chối", false, () =>
          personnelChangeApi.hrReviewPromotion(item.id, { isApproved: false, note: "" }),
        ),
        openAction,
      ];
    }

    if (item.status === PersonnelChangeStatus.PendingDirectorApproval) {
      const approve = () => personnelChangeDirectorAction(item, workflow, true);
      const reject = () => personnelChangeDirectorAction(item, workflow, false);
      return [
        personnelChangeDecision("Giám đốc duyệt", true, approve),
        personnelChangeDecision("Từ chối", false, reject),
        openAction,
      ];
    }

    return [openAction];
  }

  function personnelChangeDecision(
    label: string,
    isApproved: boolean,
    run: () => Promise<unknown> | unknown,
  ): ApprovalAction {
    return {
      kind: isApproved ? "approve" : "reject",
      label,
      tone: isApproved ? "primary" : "danger",
      run,
    };
  }

  function personnelChangeDirectorAction(
    item: PersonnelChangeListItem,
    workflow: PersonnelChangeWorkflowKind,
    isApproved: boolean,
  ) {
    if (workflow === "promotion") {
      return personnelChangeApi.directorApprovePromotion(item.id, { isApproved, note: "" });
    }
    if (workflow === "internal-transfer") {
      return personnelChangeApi.directorApproveTransfer(item.id, { isApproved, note: "" });
    }
    if (workflow === "dismissal") {
      return personnelChangeApi.directorApproveDismissal(item.id, { isApproved, note: "" });
    }
    if (workflow === "termination") {
      return personnelChangeApi.directorApproveResignation(item.id, { isApproved, note: "" });
    }

    return Promise.reject(new Error("Luồng này cần xử lý tại trang nghiệp vụ."));
  }
  function personnelChangeActionsWithNotes(
    item: PersonnelChangeListItem,
    workflow: PersonnelChangeWorkflowKind,
  ): ApprovalAction[] {
    const openAction: ApprovalAction = {
      kind: "open",
      label: "Mở hồ sơ",
      tone: "secondary",
      run: () => navigate(personnelChangeRoute(workflow)),
    };

    const decision = (
      label: string,
      isApproved: boolean,
      run: (note?: string) => Promise<unknown> | unknown,
    ): ApprovalAction => ({
      kind: isApproved ? "approve" : "reject",
      label,
      tone: isApproved ? "primary" : "danger",
      run,
    });

    if (item.status === PersonnelChangeStatus.PendingManagerReview && workflow === "termination") {
      return [
        decision("Duyệt", true, (note) =>
          personnelChangeApi.managerReviewResignation(item.id, {
            isApproved: true,
            note: note || "",
          }),
        ),
        decision("Từ chối", false, (note) =>
          personnelChangeApi.managerReviewResignation(item.id, {
            isApproved: false,
            note: note || "",
          }),
        ),
        openAction,
      ];
    }

    if (
      item.status === PersonnelChangeStatus.PendingCurrentManagerOpinion &&
      workflow === "internal-transfer"
    ) {
      return [
        decision("Đồng ý", true, (note) =>
          personnelChangeApi.submitCurrentManagerOpinion(item.id, {
            isApproved: true,
            opinion: note || "",
          }),
        ),
        decision("Từ chối", false, (note) =>
          personnelChangeApi.submitCurrentManagerOpinion(item.id, {
            isApproved: false,
            opinion: note || "",
          }),
        ),
        openAction,
      ];
    }

    if (
      item.status === PersonnelChangeStatus.PendingEmployeeConsent &&
      (workflow === "internal-transfer" || workflow === "senior-appointment")
    ) {
      return [
        decision("Đồng ý", true, (note) =>
          personnelChangeApi.employeeConsent(
            item.id,
            { isAccepted: true, note: note || "" },
            workflow,
          ),
        ),
        decision("Từ chối", false, (note) =>
          personnelChangeApi.employeeConsent(
            item.id,
            { isAccepted: false, note: note || "" },
            workflow,
          ),
        ),
        openAction,
      ];
    }

    if (item.status === PersonnelChangeStatus.PendingHRReview && workflow === "promotion") {
      return [
        decision("HR duyệt", true, (note) =>
          personnelChangeApi.hrReviewPromotion(item.id, {
            isApproved: true,
            note: note || "",
          }),
        ),
        decision("Từ chối", false, (note) =>
          personnelChangeApi.hrReviewPromotion(item.id, {
            isApproved: false,
            note: note || "",
          }),
        ),
        openAction,
      ];
    }

    if (item.status === PersonnelChangeStatus.PendingDirectorApproval) {
      return [
        decision("Giám đốc duyệt", true, (note) =>
          personnelChangeDirectorActionWithNote(item, workflow, true, note),
        ),
        decision("Từ chối", false, (note) =>
          personnelChangeDirectorActionWithNote(item, workflow, false, note),
        ),
        openAction,
      ];
    }

    return [openAction];
  }

  function personnelChangeDirectorActionWithNote(
    item: PersonnelChangeListItem,
    workflow: PersonnelChangeWorkflowKind,
    isApproved: boolean,
    note?: string,
  ) {
    const payload = { isApproved, note: note || "" };

    if (workflow === "promotion") {
      return personnelChangeApi.directorApprovePromotion(item.id, payload);
    }
    if (workflow === "internal-transfer") {
      return personnelChangeApi.directorApproveTransfer(item.id, payload);
    }
    if (workflow === "dismissal") {
      return personnelChangeApi.directorApproveDismissal(item.id, payload);
    }
    if (workflow === "termination") {
      return personnelChangeApi.directorApproveResignation(item.id, payload);
    }

    return Promise.reject(new Error("Luồng này cần xử lý tại trang nghiệp vụ."));
  }
  void personnelChangeActions;
};

const CleanFilterPanel = ({
  filters,
  setFilters,
  statusOptions,
}: {
  filters: ApprovalWorkspaceFilters;
  setFilters: (value: ApprovalWorkspaceFilters) => void;
  statusOptions: Array<{ value: string; label: string }>;
}) => (
  <FeatureCard
    title="Bộ lọc"
    description="Lọc theo phân hệ, trạng thái, người gửi, phòng ban hoặc hạn xử lý."
  >
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-6">
      <label>
        <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
          Phân hệ
        </span>
        <select
          className={fieldClass}
          value={filters.module}
          onChange={(event) =>
            setFilters({ ...filters, module: event.target.value as ApprovalWorkspaceFilters["module"] })
          }
        >
          {APPROVAL_MODULES.map((module) => (
            <option key={module.value} value={module.value}>
              {module.label}
            </option>
          ))}
        </select>
      </label>
      <label>
        <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
          Trạng thái
        </span>
        <select
          className={fieldClass}
          value={filters.status}
          onChange={(event) => setFilters({ ...filters, status: event.target.value })}
        >
          <option value="ALL">Tất cả trạng thái</option>
          {statusOptions.map((status) => (
            <option key={status.value} value={status.value}>
              {status.label}
            </option>
          ))}
        </select>
      </label>
      <label>
        <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
          Người gửi
        </span>
        <input
          className={fieldClass}
          value={filters.owner}
          onChange={(event) => setFilters({ ...filters, owner: event.target.value })}
          placeholder="Nhập tên người gửi"
        />
      </label>
      <label>
        <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
          Hạn xử lý
        </span>
        <select
          className={fieldClass}
          value={filters.deadline}
          onChange={(event) =>
            setFilters({
              ...filters,
              deadline: event.target.value as ApprovalWorkspaceFilters["deadline"],
            })
          }
        >
          {(["ALL", "OVERDUE", "TODAY", "NEXT_7_DAYS", "NO_DEADLINE"] as const).map(
            (value) => (
              <option key={value} value={value}>
                {readableDeadlineLabel(value)}
              </option>
            ),
          )}
        </select>
      </label>
      <label>
        <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
          Tìm kiếm
        </span>
        <input
          className={fieldClass}
          value={filters.query}
          onChange={(event) => setFilters({ ...filters, query: event.target.value })}
          placeholder="Tên nhân sự, phòng ban, nội dung"
        />
      </label>
      <label>
        <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
          Từ ngày tạo
        </span>
        <input
          type="date"
          className={fieldClass}
          value={filters.fromDate}
          onChange={(event) => setFilters({ ...filters, fromDate: event.target.value })}
        />
      </label>
      <label>
        <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
          Đến ngày tạo
        </span>
        <input
          type="date"
          className={fieldClass}
          value={filters.toDate}
          onChange={(event) => setFilters({ ...filters, toDate: event.target.value })}
        />
      </label>
    </div>
  </FeatureCard>
);

const CleanApprovalSummaryStrip = ({
  metrics,
}: {
  metrics: {
    total: number;
    visible: number;
    overdue: number;
    today: number;
  };
}) => (
  <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
    <SummaryTile
      label="Đang chờ"
      value={metrics.total}
      detail={`${metrics.visible} yêu cầu đang hiển thị`}
      icon={<Clock3 size={18} />}
    />
    <SummaryTile
      label="Quá hạn"
      value={metrics.overdue}
      detail="Cần ưu tiên xử lý"
      tone="danger"
      icon={<CalendarDays size={18} />}
    />
    <SummaryTile
      label="Đến hạn hôm nay"
      value={metrics.today}
      detail="Nên hoàn tất trong ngày"
      tone="warning"
      icon={<CalendarDays size={18} />}
    />
    <SummaryTile
      label="Kết quả lọc"
      value={metrics.visible}
      detail="Số yêu cầu phù hợp"
      tone="info"
      icon={<Eye size={18} />}
    />
  </div>
);

const CleanApprovalRow = ({
  item,
  onAction,
  onOpenDetail,
}: {
  item: ApprovalItem;
  onAction: (item: ApprovalItem, action: ApprovalAction) => void;
  onOpenDetail: () => void;
}) => (
  <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4 transition hover:border-[var(--hicas-primary)]/40 hover:shadow-sm">
    <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
      <div className="min-w-0 flex-1">
        <div className="mb-2 flex flex-wrap items-center gap-2">
          <span
            className={`rounded-md border px-2 py-1 text-xs font-semibold ${moduleTone(item.module)}`}
          >
            {item.moduleLabel}
          </span>
          <span className="rounded-md bg-gray-100 px-2 py-1 text-xs font-semibold text-gray-700">
            {item.statusLabel}
          </span>
        </div>
        <h3 className="text-base font-semibold text-gray-900">{item.title}</h3>
        {item.subtitle && (
          <p className="mt-1 text-sm text-gray-600">{item.subtitle}</p>
        )}
        <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-gray-500">
          {item.owner && (
            <span className="inline-flex items-center gap-1">
              <UserRound size={13} /> {item.owner}
            </span>
          )}
          {item.department && (
            <span className="inline-flex items-center gap-1">
              <Building2 size={13} /> {item.department}
            </span>
          )}
          <span className="inline-flex items-center gap-1">
            <CalendarDays size={13} /> Ngày tạo: {formatDate(item.date)}
          </span>
          <span className="inline-flex items-center gap-1">
            <Clock3 size={13} /> Hạn/SLA: {formatDate(item.deadline)}
          </span>
        </div>
      </div>
      <div className="flex flex-wrap justify-end gap-2">
        <button
          type="button"
          onClick={onOpenDetail}
          className={secondaryButtonClass}
        >
          <Eye size={16} />
          Chi tiết
        </button>
        {item.actions.map((action) => (
          <button
            key={`${item.id}-${action.label}`}
            type="button"
            onClick={() => onAction(item, action)}
            className={
              action.tone === "danger"
                ? dangerButtonClass
                : action.tone === "secondary"
                  ? secondaryButtonClass
                  : primaryButtonClass
            }
          >
            <ActionIcon kind={action.kind} />
            {action.label}
          </button>
        ))}
      </div>
    </div>
  </div>
);

const CleanApprovalDetailDrawer = ({
  item,
  open,
  onClose,
  onAction,
}: {
  item: ApprovalItem | null;
  open: boolean;
  onClose: () => void;
  onAction: (item: ApprovalItem, action: ApprovalAction) => void;
}) => (
  <DrawerForm
    open={open}
    title={item?.title || "Chi tiết phê duyệt"}
    description={item?.subtitle || "Kiểm tra thông tin trước khi xử lý yêu cầu."}
    width="xl"
    onClose={onClose}
    footer={
      item ? (
        <div className="flex w-full flex-col-reverse gap-2 sm:flex-row sm:items-center sm:justify-end">
          <button type="button" className={secondaryButtonClass} onClick={onClose}>
            Đóng
          </button>
          {item.actions.map((action) => (
            <button
              key={`${item.id}-drawer-${action.label}`}
              type="button"
              onClick={() => onAction(item, action)}
              className={
                action.tone === "danger"
                  ? dangerButtonClass
                  : action.tone === "secondary"
                    ? secondaryButtonClass
                    : primaryButtonClass
              }
            >
              <ActionIcon kind={action.kind} />
              {action.label}
            </button>
          ))}
        </div>
      ) : null
    }
  >
    {!item ? (
      <p className="text-sm text-[var(--hicas-text-secondary)]">
        Chưa có yêu cầu được chọn.
      </p>
    ) : (
      <div className="space-y-5">
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <DetailInfo label="Phân hệ" value={item.moduleLabel} />
          <DetailInfo label="Trạng thái" value={item.statusLabel} />
          <DetailInfo label="Người liên quan" value={item.owner || "-"} />
          <DetailInfo label="Phòng ban" value={item.department || "-"} />
          <DetailInfo label="Ngày tạo" value={formatDate(item.date)} />
          <DetailInfo label="Hạn/SLA" value={formatDate(item.deadline)} />
          <DetailInfo label="Nguồn xử lý" value={item.source || "-"} />
          <DetailInfo label="Mã yêu cầu" value={item.id} />
        </div>

        <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4">
          <p className="text-sm font-semibold text-[var(--hicas-text-main)]">
            Nội dung cần kiểm tra
          </p>
          {item.details ? (
            <div className="mt-3">{item.details}</div>
          ) : (
            <p className="mt-2 text-sm leading-6 text-[var(--hicas-text-secondary)]">
              Chưa có dữ liệu chi tiết bổ sung cho yêu cầu này.
            </p>
          )}
        </div>
      </div>
    )}
  </DrawerForm>
);

const FilterPanel = ({
  filters,
  setFilters,
  statusOptions,
}: {
  filters: ApprovalWorkspaceFilters;
  setFilters: (value: ApprovalWorkspaceFilters) => void;
  statusOptions: Array<{ value: string; label: string }>;
}) => (
  <FeatureCard title="Bộ lọc" description="Thu hẹp danh sách theo phân hệ, trạng thái, người gửi hoặc hạn xử lý.">
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-6">
      <label>
        <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
          Phân hệ
        </span>
        <select
          className={fieldClass}
          value={filters.module}
          onChange={(event) =>
            setFilters({ ...filters, module: event.target.value as ApprovalWorkspaceFilters["module"] })
          }
        >
          {APPROVAL_MODULES.map((module) => (
            <option key={module.value} value={module.value}>
              {module.label}
            </option>
          ))}
        </select>
      </label>
      <label>
        <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
          Trạng thái
        </span>
        <select
          className={fieldClass}
          value={filters.status}
          onChange={(event) =>
            setFilters({ ...filters, status: event.target.value })
          }
        >
          <option value="ALL">Tất cả trạng thái</option>
          {statusOptions.map((status) => (
            <option key={status.value} value={status.value}>
              {status.label}
            </option>
          ))}
        </select>
      </label>
      <label>
        <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
          Người gửi
        </span>
        <input
          className={fieldClass}
          value={filters.owner}
          onChange={(event) => setFilters({ ...filters, owner: event.target.value })}
          placeholder="Tên người gửi..."
        />
      </label>
      <label>
        <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
          Hạn xử lý
        </span>
        <select
          className={fieldClass}
          value={filters.deadline}
          onChange={(event) =>
            setFilters({
              ...filters,
              deadline: event.target.value as ApprovalWorkspaceFilters["deadline"],
            })
          }
        >
          {(["ALL", "OVERDUE", "TODAY", "NEXT_7_DAYS", "NO_DEADLINE"] as const).map(
            (value) => (
              <option key={value} value={value}>
                {deadlineLabel(value)}
              </option>
            ),
          )}
        </select>
      </label>
      <label>
        <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
          Tìm kiếm
        </span>
        <input
          className={fieldClass}
          value={filters.query}
          onChange={(event) => setFilters({ ...filters, query: event.target.value })}
          placeholder="Tên nhân viên, phòng ban, nội dung..."
        />
      </label>
      <label>
        <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
          Từ ngày tạo
        </span>
        <input
          type="date"
          className={fieldClass}
          value={filters.fromDate}
          onChange={(event) =>
            setFilters({ ...filters, fromDate: event.target.value })
          }
        />
      </label>
      <label>
        <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
          Đến ngày tạo
        </span>
        <input
          type="date"
          className={fieldClass}
          value={filters.toDate}
          onChange={(event) =>
            setFilters({ ...filters, toDate: event.target.value })
          }
        />
      </label>
    </div>
  </FeatureCard>
);

const ApprovalSummaryStrip = ({
  metrics,
}: {
  metrics: {
    total: number;
    visible: number;
    overdue: number;
    today: number;
  };
}) => (
  <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
    <SummaryTile
      label="Đang chờ"
      value={metrics.total}
      detail={`${metrics.visible} yêu cầu đang hiển thị`}
      icon={<Clock3 size={18} />}
    />
    <SummaryTile
      label="Quá hạn"
      value={metrics.overdue}
      detail="Cần ưu tiên xử lý"
      tone="danger"
      icon={<CalendarDays size={18} />}
    />
    <SummaryTile
      label="Đến hạn hôm nay"
      value={metrics.today}
      detail="Nên hoàn tất trong ngày"
      tone="warning"
      icon={<CalendarDays size={18} />}
    />
    <SummaryTile
      label="Bộ lọc hiện tại"
      value={metrics.visible}
      detail="Kết quả sau khi lọc"
      tone="info"
      icon={<Eye size={18} />}
    />
  </div>
);

const SummaryTile = ({
  label,
  value,
  detail,
  icon,
  tone = "default",
}: {
  label: string;
  value: number;
  detail: string;
  icon: ReactNode;
  tone?: "default" | "danger" | "warning" | "info";
}) => {
  const toneClass = {
    default: "bg-white text-[var(--hicas-text-main)]",
    danger: "bg-red-50 text-red-700",
    warning: "bg-amber-50 text-amber-700",
    info: "bg-blue-50 text-blue-700",
  }[tone];

  return (
    <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4 shadow-sm">
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm font-semibold text-[var(--hicas-text-secondary)]">
          {label}
        </p>
        <span className={`inline-flex h-9 w-9 items-center justify-center rounded-lg ${toneClass}`}>
          {icon}
        </span>
      </div>
      <p className="mt-3 text-2xl font-bold text-[var(--hicas-text-main)]">{value}</p>
      <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">{detail}</p>
    </div>
  );
};

const ApprovalRow = ({
  item,
  onAction,
  onOpenDetail,
}: {
  item: ApprovalItem;
  onAction: (item: ApprovalItem, action: ApprovalAction) => void;
  onOpenDetail: () => void;
}) => (
  <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4 transition hover:border-[var(--hicas-primary)]/40 hover:shadow-sm">
    <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
      <div className="min-w-0 flex-1">
        <div className="mb-2 flex flex-wrap items-center gap-2">
          <span
            className={`rounded-md border px-2 py-1 text-xs font-semibold ${moduleTone(item.module)}`}
          >
            {item.moduleLabel}
          </span>
          <span className="rounded-md bg-gray-100 px-2 py-1 text-xs font-semibold text-gray-700">
            {item.statusLabel}
          </span>
        </div>
        <h3 className="text-base font-semibold text-gray-900">{item.title}</h3>
        {item.subtitle && (
          <p className="mt-1 text-sm text-gray-600">{item.subtitle}</p>
        )}
        <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-gray-500">
          {item.owner && (
            <span className="inline-flex items-center gap-1">
              <UserRound size={13} /> {item.owner}
            </span>
          )}
          {item.department && (
            <span className="inline-flex items-center gap-1">
              <Building2 size={13} /> {item.department}
            </span>
          )}
          <span className="inline-flex items-center gap-1">
            <CalendarDays size={13} /> Ngày tạo: {formatDate(item.date)}
          </span>
          <span className="inline-flex items-center gap-1">
            <Clock3 size={13} /> Hạn/SLA: {formatDate(item.deadline)}
          </span>
        </div>
      </div>
      <div className="flex flex-wrap justify-end gap-2">
        <button
          type="button"
          onClick={onOpenDetail}
          className={secondaryButtonClass}
        >
          <Eye size={16} />
          Chi tiết
        </button>
        {item.actions.map((action) => (
          <button
            key={`${item.id}-${action.label}`}
            type="button"
            onClick={() => onAction(item, action)}
            className={
              action.tone === "danger"
                ? dangerButtonClass
                : action.tone === "secondary"
                  ? secondaryButtonClass
                  : primaryButtonClass
            }
          >
            <ActionIcon kind={action.kind} />
            {action.label}
          </button>
        ))}
      </div>
    </div>
  </div>
);

const ApprovalDetailDrawer = ({
  item,
  open,
  onClose,
  onAction,
}: {
  item: ApprovalItem | null;
  open: boolean;
  onClose: () => void;
  onAction: (item: ApprovalItem, action: ApprovalAction) => void;
}) => (
  <DrawerForm
    open={open}
    title={item?.title || "Chi tiết phê duyệt"}
    description={item?.subtitle || "Kiểm tra thông tin trước khi xử lý yêu cầu."}
    width="xl"
    onClose={onClose}
    footer={
      item ? (
        <div className="flex w-full flex-col-reverse gap-2 sm:flex-row sm:items-center sm:justify-end">
          <button type="button" className={secondaryButtonClass} onClick={onClose}>
            Đóng
          </button>
          {item.actions.map((action) => (
            <button
              key={`${item.id}-drawer-${action.label}`}
              type="button"
              onClick={() => onAction(item, action)}
              className={
                action.tone === "danger"
                  ? dangerButtonClass
                  : action.tone === "secondary"
                    ? secondaryButtonClass
                    : primaryButtonClass
              }
            >
              <ActionIcon kind={action.kind} />
              {action.label}
            </button>
          ))}
        </div>
      ) : null
    }
  >
    {!item ? (
      <p className="text-sm text-[var(--hicas-text-secondary)]">
        Chưa có yêu cầu được chọn.
      </p>
    ) : (
      <div className="space-y-5">
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <DetailInfo label="Phân hệ" value={item.moduleLabel} />
          <DetailInfo label="Trạng thái" value={item.statusLabel} />
          <DetailInfo label="Người liên quan" value={item.owner || "-"} />
          <DetailInfo label="Phòng ban" value={item.department || "-"} />
          <DetailInfo label="Ngày tạo" value={formatDate(item.date)} />
          <DetailInfo label="Hạn/SLA" value={formatDate(item.deadline)} />
          <DetailInfo label="Nguồn xử lý" value={item.source || "-"} />
          <DetailInfo label="Mã yêu cầu" value={item.id} />
        </div>

        <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4">
          <p className="text-sm font-semibold text-[var(--hicas-text-main)]">
            Nội dung cần kiểm tra
          </p>
          {item.details ? (
            <div className="mt-3">{item.details}</div>
          ) : (
            <p className="mt-2 text-sm leading-6 text-[var(--hicas-text-secondary)]">
              Chưa có dữ liệu chi tiết bổ sung cho yêu cầu này. Vui lòng kiểm tra
              thông tin tóm tắt trước khi xử lý.
            </p>
          )}
        </div>
      </div>
    )}
  </DrawerForm>
);

void FilterPanel;
void ApprovalSummaryStrip;
void ApprovalRow;
void ApprovalDetailDrawer;

const ApprovalActionNoteDialog = ({
  pendingAction,
  note,
  submitting,
  onChangeNote,
  onCancel,
  onSubmit,
}: {
  pendingAction: { item: ApprovalItem; action: ApprovalAction } | null;
  note: string;
  submitting: boolean;
  onChangeNote: (value: string) => void;
  onCancel: () => void;
  onSubmit: () => void;
}) => {
  if (!pendingAction) return null;

  const { item, action } = pendingAction;
  const isReject = action.kind === "reject";
  const isRevision = action.kind === "revision";
  const requiresNote = action.requiresNote || isReject || isRevision;
  const title = isReject ? "Từ chối yêu cầu" : isRevision ? "Yêu cầu bổ sung/chỉnh sửa" : "Xử lý phê duyệt";
  const helper = isReject
    ? "Nhập lý do từ chối để người gửi biết."
    : isRevision
      ? "Nhập nội dung cần bổ sung hoặc chỉnh sửa để người phụ trách cập nhật lại hồ sơ."
      : "Có thể ghi chú điều kiện hoặc nhận xét trước khi duyệt.";

  return (
    <div className="fixed inset-0 z-[70] flex items-center justify-center bg-black/40 p-4">
      <div className="w-full max-w-xl rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-5 shadow-xl">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-lg font-semibold text-[var(--hicas-text-main)]">
              {title}
            </p>
            <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
              {item.title}
            </p>
          </div>
          <span
            className={`rounded-md border px-2 py-1 text-xs font-semibold ${moduleTone(item.module)}`}
          >
            {item.moduleLabel}
          </span>
        </div>

        <label className="mt-5 block">
          <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">
            Ghi chú {requiresNote ? "*" : ""}
          </span>
          <textarea
            className={`${fieldClass} min-h-32 resize-y`}
            value={note}
            onChange={(event) => onChangeNote(event.target.value)}
            placeholder={helper}
            disabled={submitting}
          />
        </label>

        <div className="mt-5 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <button
            type="button"
            className={secondaryButtonClass}
            onClick={onCancel}
            disabled={submitting}
          >
            Hủy
          </button>
          <button
            type="button"
            className={isReject ? dangerButtonClass : isRevision ? secondaryButtonClass : primaryButtonClass}
            onClick={onSubmit}
            disabled={submitting || (requiresNote && !note.trim())}
          >
            <ActionIcon kind={action.kind} />
            {submitting ? "Đang xử lý..." : action.label}
          </button>
        </div>
      </div>
    </div>
  );
};

const DetailInfo = ({ label, value }: { label: string; value: string }) => (
  <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3">
    <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">
      {label}
    </p>
    <p className="mt-2 break-words text-sm font-semibold text-[var(--hicas-text-main)]">
      {value || "-"}
    </p>
  </div>
);

const DetailFieldGrid = ({
  fields,
}: {
  fields: Array<[string, string | number | null | undefined]>;
}) => {
  const visibleFields = fields.filter(([, value]) => value !== null && value !== undefined && value !== "");

  if (visibleFields.length === 0) {
    return (
      <p className="text-sm text-[var(--hicas-text-secondary)]">
        Chưa có dữ liệu chi tiết bổ sung.
      </p>
    );
  }

  return (
    <div className="grid gap-2 text-sm sm:grid-cols-2">
      {visibleFields.map(([label, value]) => (
        <div
          key={label}
          className="rounded-[var(--radius-sm)] border border-[var(--hicas-border-soft)] bg-[var(--hicas-bg-soft)] px-3 py-2"
        >
          <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">
            {label}
          </p>
          <p className="mt-1 break-words text-[var(--hicas-text-main)]">
            {String(value)}
          </p>
        </div>
      ))}
    </div>
  );
};

const ActionIcon = ({ kind }: { kind: ApprovalAction["kind"] }) => {
  if (kind === "approve") return <CheckCircle2 size={16} />;
  if (kind === "reject") return <XCircle size={16} />;
  if (kind === "revision") return <RefreshCw size={16} />;
  if (kind === "reconcile") return <RefreshCw size={16} />;
  return <Eye size={16} />;
};

const JsonPreview = ({ value }: { value: string }) => {
  let data: Record<string, unknown> | null = null;

  try {
    data = JSON.parse(value) as Record<string, unknown>;
  } catch {
    return <p className="text-sm text-gray-600">{value}</p>;
  }

  return (
    <div className="grid gap-2 text-sm sm:grid-cols-2">
      {Object.entries(data).slice(0, 8).map(([key, val]) => (
        <div
          key={key}
          className="rounded border border-gray-100 bg-gray-50 px-3 py-2"
        >
          <p className="text-xs font-semibold uppercase text-gray-500">{key}</p>
          <p className="mt-1 break-words text-gray-700">
            {String(val || "-")}
          </p>
        </div>
      ))}
    </div>
  );
};

const mapCentralModule = (moduleCode: string): ApprovalModule => {
  if (moduleCode === "CANDIDATE") return "CANDIDATE";
  if (moduleCode.startsWith("CONTRACT")) return "CONTRACT";
  if (moduleCode.startsWith("PAYROLL") || moduleCode.includes("BONUS") || moduleCode.includes("TIMESHEET")) return "PAYROLL";
  if (moduleCode.startsWith("PERSONNEL_CHANGE")) return "PERSONNEL_CHANGE";
  if (moduleCode.startsWith("PERFORMANCE") || moduleCode.startsWith("KPI")) return "PERFORMANCE";
  return "RECRUITMENT";
};

const moduleLabel = (module: ApprovalModule) =>
  APPROVAL_MODULES.find((item) => item.value === module)?.label || module;

const centralTitle = (item: PendingApprovalDto) => {
  if (item.title) return item.title;

  if (item.moduleCode === "RECRUITMENT") {
    return `Nhu cầu tuyển dụng${item.quantity ? ` ${item.quantity} nhân sự` : ""}`;
  }

  return item.title || `Yêu cầu #${item.referenceId}`;
};

const centralSubtitle = (item: PendingApprovalDto) =>
  [item.positionName, item.departmentName, item.description]
    .filter(Boolean)
    .join(" · ");

const dependentActionLabel = (actionType: string) => {
  if (actionType === "CREATE") return "Thêm mới";
  if (actionType === "UPDATE") return "Cập nhật";
  if (actionType === "DEACTIVATE") return "Ngừng hiệu lực";
  return actionType;
};

const toOptionalNumber = (value: string) => {
  const parsed = Number(value);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : undefined;
};

const readOnboardingText = (jsonString: string, key: string) => {
  try {
    const parsed = JSON.parse(jsonString) as Record<string, unknown>;
    const value = parsed?.[key];
    return typeof value === "string" && value.trim() ? value.trim() : undefined;
  } catch {
    return undefined;
  }
};

const getErrorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Đã có lỗi xảy ra.";

const getPersonnelChangeStatusesForRole = (
  role: string,
): PersonnelChangeStatusValue[] => {
  const statuses = new Set<PersonnelChangeStatusValue>();

  if (["Admin", "HR"].includes(role)) {
    statuses.add(PersonnelChangeStatus.PendingHRReview);
    statuses.add(PersonnelChangeStatus.PendingEmployeeNotification);
  }

  if (["Admin", "Manager"].includes(role)) {
    statuses.add(PersonnelChangeStatus.PendingManagerReview);
    statuses.add(PersonnelChangeStatus.PendingCurrentManagerOpinion);
  }

  if (["Admin", "Director"].includes(role)) {
    statuses.add(PersonnelChangeStatus.PendingDirectorApproval);
  }

  if (["Admin", "Employee"].includes(role)) {
    statuses.add(PersonnelChangeStatus.PendingEmployeeConsent);
    statuses.add(PersonnelChangeStatus.PendingEmployeeExplanation);
  }

  return Array.from(statuses);
};

const personnelChangeStatusLabel = (
  status?: PersonnelChangeStatusValue | null,
) => {
  const map: Partial<Record<PersonnelChangeStatusValue, string>> = {
    [PersonnelChangeStatus.PendingHRReview]: "Chờ HR xử lý",
    [PersonnelChangeStatus.PendingEmployeeConsent]: "Chờ nhân viên xác nhận",
    [PersonnelChangeStatus.PendingDirectorApproval]: "Chờ giám đốc duyệt",
    [PersonnelChangeStatus.PendingCurrentManagerOpinion]: "Chờ quản lý hiện tại",
    [PersonnelChangeStatus.PendingEmployeeNotification]: "Chờ thông báo nhân viên",
    [PersonnelChangeStatus.PendingEmployeeExplanation]: "Chờ nhân viên giải trình",
    [PersonnelChangeStatus.PendingManagerReview]: "Chờ quản lý duyệt",
  };

  if (status === null || status === undefined) return "Chưa có trạng thái";
  return map[status] || getPersonnelChangeStatusLabel(status);
};

const personnelChangeTypeLabel = (type: PersonnelChangeType) => {
  const map: Record<PersonnelChangeType, string> = {
    [PersonnelChangeType.ConvertToOfficial]: "Chuyển chính thức",
    [PersonnelChangeType.Promotion]: "Thăng tiến",
    [PersonnelChangeType.SeniorAppointment]: "Bổ nhiệm cấp cao",
    [PersonnelChangeType.VoluntaryTermination]: "Nghỉ việc chủ động",
    [PersonnelChangeType.Dismissal]: "Kỷ luật/sa thải",
    [PersonnelChangeType.InternalTransfer]: "Thuyên chuyển nội bộ",
  };

  return map[type] || "Biến động nhân sự";
};

const resolvePersonnelChangeWorkflow = (
  item: PersonnelChangeListItem,
): PersonnelChangeWorkflowKind => {
  if (
    item.changeType === PersonnelChangeType.Promotion ||
    item.changeType === PersonnelChangeType.ConvertToOfficial
  ) {
    return "promotion";
  }
  if (item.changeType === PersonnelChangeType.SeniorAppointment) {
    return "senior-appointment";
  }
  if (item.changeType === PersonnelChangeType.VoluntaryTermination) {
    return "termination";
  }
  if (item.changeType === PersonnelChangeType.Dismissal) {
    return "dismissal";
  }
  return "internal-transfer";
};

const personnelChangeRoute = (workflow: PersonnelChangeWorkflowKind) => {
  const map: Record<PersonnelChangeWorkflowKind, string> = {
    promotion: "/personnel-change/promotion",
    "senior-appointment": "/personnel-change/senior-appointment",
    termination: "/personnel-change/termination",
    dismissal: "/personnel-change/dismissal",
    "internal-transfer": "/personnel-change/internal-transfer",
  };

  return map[workflow];
};
