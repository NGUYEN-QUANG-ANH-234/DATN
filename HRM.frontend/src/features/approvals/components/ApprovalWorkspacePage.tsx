import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  FeatureCard,
  FeaturePage,
  primaryButtonClass,
  secondaryButtonClass,
  dangerButtonClass,
  fieldClass,
  EmptyState,
} from "../../../core/components/FeatureShell";
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
import type { PendingProfileRequest } from "../../employees/types/profileRequest";
import type { PendingDependentRequest } from "../../employees/types/dependent";
import type { PendingOnboardingRequest } from "../../employees/types/onboarding";
import type { ContractDto } from "../../employees/api/contractApi";
import type { ContractAddendumDto } from "../../employees/api/contractAddendumApi";
import type { OvertimeRequest } from "../../attendance/api/overtimeApi";
import type { LeaveRequest } from "../../attendance/api/leaveRequestApi";
import type {
  ApprovalItem,
  ApprovalModule,
  ApprovalAction,
  PendingApprovalDto,
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
  statusLabel,
  unwrapData,
} from "../utils";

const defaultFilters: ApprovalWorkspaceFilters = {
  module: "ALL",
  query: "",
  fromDate: "",
  toDate: "",
};

const includeOvertimeReconcileInApprovalInbox = false;

export const ApprovalWorkspacePage = () => {
  const { user } = useCurrentUser();
  const role = getRole(user?.role);
  const navigate = useNavigate();
  const { triggerAlert } = useNotification();

  const [items, setItems] = useState<ApprovalItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState<ApprovalWorkspaceFilters>(defaultFilters);
  const [roleOptions, setRoleOptions] = useState<RoleOption[]>([]);
  const [onboardingRoleById, setOnboardingRoleById] = useState<
    Record<number, number>
  >({});

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
        const module = mapCentralModule(item.moduleCode);
        target.push({
          id: `central-${item.moduleCode}-${item.referenceId}`,
          module,
          moduleLabel: moduleLabel(module),
          source: item.moduleCode,
          title: centralTitle(item),
          subtitle: centralSubtitle(item),
          owner: item.title,
          department: item.departmentName,
          status: "Pending",
          statusLabel: `Cấp duyệt ${item.level}`,
          date: item.createdAt,
          deadline: item.deadline,
          actions: [
            centralAction(item, true),
            centralAction(item, false),
          ],
        });
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
        const selectedRoleId = onboardingRoleById[item.id] || defaultEmployeeRoleId;

        target.push({
          id: `onboarding-${item.id}`,
          module: "ONBOARDING",
          moduleLabel: "Onboarding",
          source: "ONBOARDING",
          title: `Thiết lập hồ sơ mới #${item.id}`,
          subtitle: `Ứng viên #${item.candidateId}`,
          status: item.status,
          statusLabel: statusLabel(item.status),
          date: item.createdAt,
          details: (
            <div className="space-y-3">
              <JsonPreview value={item.requestedDataJson} />
              <label className="block">
                <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
                  Vai trò khi kích hoạt
                </span>
                <select
                  className={fieldClass}
                  value={selectedRoleId}
                  onChange={(event) =>
                    setOnboardingRoleById((prev) => ({
                      ...prev,
                      [item.id]: Number(event.target.value),
                    }))
                  }
                >
                  {normalizedRoleOptions.map((option) => (
                    <option key={option.id} value={option.id}>
                      {option.name}
                    </option>
                  ))}
                </select>
              </label>
            </div>
          ),
          actions: [
            {
              kind: "approve",
              label: "Kích hoạt",
              tone: "primary",
              run: () =>
                onboardingApi.reviewRequest(item.id, {
                  isApproved: true,
                  roleId: onboardingRoleById[item.id] || defaultEmployeeRoleId,
                }),
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

  const normalizedRoleOptions = useMemo(() => {
    if (roleOptions.length === 0) {
      return [{ id: 5, name: "Employee" }];
    }

    return roleOptions.map((role) => ({
      id: role.id,
      name: role.name || role.roleName || `Role #${role.id}`,
    }));
  }, [roleOptions]);

  useEffect(() => {
    if (!["Admin", "HR"].includes(role)) return;

    accountApi
      .getSystemRoles()
      .then((res: unknown) => setRoleOptions(unwrapData<RoleOption>(res)))
      .catch(() => setRoleOptions([]));
  }, [role]);

  useEffect(() => {
    void fetchItems();
  }, [fetchItems]);

  const defaultEmployeeRoleId = useMemo(() => {
    const found = normalizedRoleOptions.find((item) =>
      ["Employee", "Nhân viên"].includes(item.name),
    );
    return found?.id || normalizedRoleOptions[0]?.id || 5;
  }, [normalizedRoleOptions]);

  const filteredItems = useMemo(() => {
    const query = normalizeText(filters.query);
    const from = filters.fromDate ? new Date(filters.fromDate) : null;
    const to = filters.toDate ? new Date(filters.toDate) : null;

    return items.filter((item) => {
      if (filters.module !== "ALL" && item.module !== filters.module) {
        return false;
      }

      if (query) {
        const haystack = normalizeText(
          `${item.title} ${item.subtitle} ${item.owner} ${item.department} ${item.statusLabel}`,
        );
        if (!haystack.includes(query)) return false;
      }

      if (from || to) {
        const date = item.date ? new Date(item.date) : null;
        if (!date || Number.isNaN(date.getTime())) return false;
        if (from && date < from) return false;
        if (to && date > to) return false;
      }

      return true;
    });
  }, [filters, items]);

  const executeAction = (item: ApprovalItem, action: ApprovalAction) => {
    triggerAlert(
      "confirm",
      action.kind === "reject" ? "Xác nhận từ chối" : "Xác nhận xử lý",
      `Bạn muốn ${action.label.toLowerCase()} yêu cầu "${item.title}"?`,
      async () => {
        try {
          await action.run();
          triggerAlert("success", "Đã xử lý", "Yêu cầu đã được cập nhật.");
          await fetchItems();
        } catch (error) {
          triggerAlert("error", "Không thể xử lý", getErrorMessage(error));
        }
      },
    );
  };

  return (
    <FeaturePage
      title={`Phê duyệt của ${role || "người dùng"}`}
      description="Một inbox chung theo vai trò, gom tất cả yêu cầu cần xử lý và cho phép lọc theo module, thời gian hoặc nội dung."
      actions={
        <button className={secondaryButtonClass} onClick={() => fetchItems()}>
          Làm mới
        </button>
      }
      width="wide"
    >
      <FilterPanel filters={filters} setFilters={setFilters} />

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
              Đang tải dữ liệu phê duyệt...
            </div>
          ) : filteredItems.length === 0 ? (
            <EmptyState title="Không có yêu cầu phù hợp bộ lọc" />
          ) : (
            <div className="space-y-3">
              {filteredItems.map((item) => (
                <ApprovalRow
                  key={item.id}
                  item={item}
                  onAction={executeAction}
                />
              ))}
            </div>
          )}
        </FeatureCard>
      )}
    </FeaturePage>
  );

  function centralAction(
    item: PendingApprovalDto,
    isApproved: boolean,
  ): ApprovalAction {
    return {
      kind: isApproved ? "approve" : "reject",
      label: isApproved ? "Duyệt" : "Từ chối",
      tone: isApproved ? "primary" : "danger",
      run: () =>
        recruitmentApi.reviewRequest({
          moduleCode: item.moduleCode,
          referenceId: item.referenceId,
          isApproved,
          note: "",
        }),
    };
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
          run: () =>
            scope === "manager"
              ? overtimeApi.managerReview(item.id, { isApproved: true })
              : scope === "hr"
                ? overtimeApi.hrConfirm(item.id, { isApproved: true })
                : overtimeApi.directorReview(item.id, { isApproved: true }),
        },
        {
          kind: "reject",
          label: "Từ chối",
          tone: "danger",
          run: () =>
            scope === "manager"
              ? overtimeApi.managerReview(item.id, { isApproved: false })
              : scope === "hr"
                ? overtimeApi.hrConfirm(item.id, { isApproved: false })
                : overtimeApi.directorReview(item.id, { isApproved: false }),
        },
      ],
    };
  }

  function mapOvertimeBase(item: OvertimeRequest): ApprovalItem {
    return {
      id: `ot-${item.id}`,
      module: "OVERTIME",
      moduleLabel: "OT",
      source: "OVERTIME",
      title: `${item.employeeName} - ${item.workDate}`,
      subtitle: `${item.startTime} - ${item.endTime}`,
      owner: item.employeeName,
      department: item.departmentName,
      status: item.status,
      statusLabel: statusLabel(item.status),
      date: item.workDate,
      details: <p className="text-sm text-gray-600">{item.reason}</p>,
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
      details: <p className="text-sm text-gray-600">{item.reason}</p>,
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
          kind: "reject",
          label: "Từ chối",
          tone: "danger",
          run: () =>
            scope === "dept"
              ? contractAddendumApi.deptReview(item.id, {
                  isApproved: false,
                  rejectReason: "Trưởng phòng từ chối phụ lục hợp đồng.",
                })
              : scope === "hr"
                ? contractAddendumApi.hrConfirm(item.id, {
                    isApproved: false,
                    rejectReason: "HR từ chối phụ lục hợp đồng.",
                  })
                : contractAddendumApi.reject(
                    item.id,
                    "Giám đốc từ chối phụ lục hợp đồng.",
                  ),
        },
      ],
    };
  }
};

const FilterPanel = ({
  filters,
  setFilters,
}: {
  filters: ApprovalWorkspaceFilters;
  setFilters: (value: ApprovalWorkspaceFilters) => void;
}) => (
  <FeatureCard title="Bộ lọc">
    <div className="grid gap-4 md:grid-cols-4">
      <label>
        <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
          Module
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
          Từ ngày
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
          Đến ngày
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

const ApprovalRow = ({
  item,
  onAction,
}: {
  item: ApprovalItem;
  onAction: (item: ApprovalItem, action: ApprovalAction) => void;
}) => (
  <div className="rounded-lg border border-gray-200 bg-white p-4">
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
          {item.owner && <span>Người liên quan: {item.owner}</span>}
          {item.department && <span>Phòng ban: {item.department}</span>}
          <span>Ngày: {formatDate(item.date)}</span>
          {item.deadline && <span>Hạn/SLA: {formatDate(item.deadline)}</span>}
          <span>Nguồn: {item.source}</span>
        </div>
        {item.details && <div className="mt-3">{item.details}</div>}
      </div>
      <div className="flex flex-wrap justify-end gap-2">
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
            {action.label}
          </button>
        ))}
      </div>
    </div>
  </div>
);

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
  return "RECRUITMENT";
};

const moduleLabel = (module: ApprovalModule) =>
  APPROVAL_MODULES.find((item) => item.value === module)?.label || module;

const centralTitle = (item: PendingApprovalDto) => {
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

const getErrorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Đã có lỗi xảy ra.";
