import type { ApprovalModule } from "./types";

export type ApprovalWorkflowModule = ApprovalModule;

export type ApprovalWorkflowScope =
  | "central-inbox"
  | "module-adapter"
  | "domain-action"
  | "planned";

export type ApprovalWorkflowStatus = "active" | "partial" | "planned";

export type ApprovalWorkflowMapItem = {
  workflowKey: string;
  workflowName: string;
  module: ApprovalWorkflowModule;
  moduleLabel: string;
  ownerArea: string;
  creators: string[];
  approvers: string[];
  pendingStatuses: string[];
  approveRoute: string;
  legacyRoutes: string[];
  scope: ApprovalWorkflowScope;
  status: ApprovalWorkflowStatus;
  note?: string;
};

export const getApprovalRedirect = (module: ApprovalModule) =>
  `/approvals?module=${module}`;

export const LEGACY_APPROVAL_REDIRECTS: Array<{
  from: string;
  to: string;
  reason: string;
}> = [
  {
    from: "/recruitment/approval-inbox",
    to: getApprovalRedirect("RECRUITMENT"),
    reason: "Các yêu cầu tuyển dụng cần duyệt được xử lý tại trang Phê duyệt.",
  },
  {
    from: "/employee-contract/profile-review",
    to: getApprovalRedirect("PROFILE"),
    reason: "Các thay đổi hồ sơ cần duyệt được xử lý tại trang Phê duyệt.",
  },
  {
    from: "/employee-contract/director-contract-approval",
    to: getApprovalRedirect("CONTRACT"),
    reason: "Hợp đồng cần duyệt được xử lý tại trang Phê duyệt.",
  },
  {
    from: "/attendance-leave/overtime-approvals",
    to: getApprovalRedirect("OVERTIME"),
    reason: "Yêu cầu làm thêm giờ cần duyệt được xử lý tại trang Phê duyệt.",
  },
];

export const APPROVAL_WORKFLOW_MAP: ApprovalWorkflowMapItem[] = [
  {
    workflowKey: "RECRUITMENT_REQUEST",
    workflowName: "Duyệt nhu cầu tuyển dụng",
    module: "RECRUITMENT",
    moduleLabel: "Tuyển dụng",
    ownerArea: "Tuyển dụng",
    creators: ["HR", "Manager"],
    approvers: ["HR", "Director"],
    pendingStatuses: ["Pending", "PendingHR", "PendingDirector"],
    approveRoute: getApprovalRedirect("RECRUITMENT"),
    legacyRoutes: ["/recruitment/approval-inbox"],
    scope: "central-inbox",
    status: "active",
  },
  {
    workflowKey: "CANDIDATE_APPROVAL",
    workflowName: "Duyệt ứng viên",
    module: "CANDIDATE",
    moduleLabel: "Ứng viên",
    ownerArea: "Tuyển dụng",
    creators: ["HR"],
    approvers: ["Manager", "Director"],
    pendingStatuses: ["Interview_Pending", "Interview_Passed", "Pending"],
    approveRoute: getApprovalRedirect("CANDIDATE"),
    legacyRoutes: [],
    scope: "central-inbox",
    status: "active",
    note: "HR tiếp tục sàng lọc và theo dõi ứng viên tại trang Ứng viên.",
  },
  {
    workflowKey: "PROFILE_UPDATE",
    workflowName: "Duyệt thay đổi hồ sơ",
    module: "PROFILE",
    moduleLabel: "Hồ sơ",
    ownerArea: "Hồ sơ và hợp đồng",
    creators: ["Employee", "Intern", "HR"],
    approvers: ["HR", "Director"],
    pendingStatuses: ["Pending_HR", "PendingHR", "PendingDirector"],
    approveRoute: getApprovalRedirect("PROFILE"),
    legacyRoutes: ["/employee-contract/profile-review", "/employees/hr-profile-review"],
    scope: "module-adapter",
    status: "active",
  },
  {
    workflowKey: "DEPENDENT_UPDATE",
    workflowName: "Duyệt người phụ thuộc",
    module: "PROFILE",
    moduleLabel: "Hồ sơ",
    ownerArea: "Hồ sơ và hợp đồng",
    creators: ["Employee", "HR"],
    approvers: ["HR", "Director"],
    pendingStatuses: ["Pending_HR", "PendingHR"],
    approveRoute: getApprovalRedirect("PROFILE"),
    legacyRoutes: [],
    scope: "module-adapter",
    status: "active",
  },
  {
    workflowKey: "ONBOARDING",
    workflowName: "Tiếp nhận hồ sơ nhân viên",
    module: "ONBOARDING",
    moduleLabel: "Tiếp nhận hồ sơ",
    ownerArea: "Hồ sơ và hợp đồng",
    creators: ["HR"],
    approvers: ["HR"],
    pendingStatuses: ["Pending", "Pending_HR"],
    approveRoute: getApprovalRedirect("ONBOARDING"),
    legacyRoutes: [],
    scope: "module-adapter",
    status: "active",
  },
  {
    workflowKey: "CONTRACT_FLOW",
    workflowName: "Duyệt hợp đồng",
    module: "CONTRACT",
    moduleLabel: "Hợp đồng",
    ownerArea: "Hồ sơ và hợp đồng",
    creators: ["Employee", "Manager", "HR"],
    approvers: ["Manager", "Employee", "Director"],
    pendingStatuses: [
      "PendingManagerContentReview",
      "PendingEmployee",
      "PendingDirector",
      "PendingHRRevision",
    ],
    approveRoute: getApprovalRedirect("CONTRACT"),
    legacyRoutes: [
      "/employee-contract/director-contract-approval",
      "/employees/director-contract-approval",
    ],
    scope: "module-adapter",
    status: "active",
    note: "HR soạn và phát hành hợp đồng tại trang Hợp đồng.",
  },
  {
    workflowKey: "ADDENDUM_FLOW",
    workflowName: "Duyệt phụ lục hợp đồng",
    module: "ADDENDUM",
    moduleLabel: "Phụ lục",
    ownerArea: "Hồ sơ và hợp đồng",
    creators: ["HR"],
    approvers: ["Manager", "Employee", "Director"],
    pendingStatuses: ["PendingDept", "PendingEmployee", "PendingDirector", "PendingHRRevision"],
    approveRoute: getApprovalRedirect("ADDENDUM"),
    legacyRoutes: [],
    scope: "module-adapter",
    status: "active",
  },
  {
    workflowKey: "OVERTIME_APPROVAL",
    workflowName: "Duyệt làm thêm giờ",
    module: "OVERTIME",
    moduleLabel: "Làm thêm giờ",
    ownerArea: "Chấm công và nghỉ phép",
    creators: ["Employee", "Manager"],
    approvers: ["Manager", "HR", "Director"],
    pendingStatuses: ["PendingManager", "PendingHR", "PendingDirector"],
    approveRoute: getApprovalRedirect("OVERTIME"),
    legacyRoutes: ["/attendance-leave/overtime-approvals", "/attendance/overtime-approvals"],
    scope: "module-adapter",
    status: "active",
  },
  {
    workflowKey: "LEAVE_APPROVAL",
    workflowName: "Duyệt nghỉ phép",
    module: "LEAVE",
    moduleLabel: "Nghỉ phép",
    ownerArea: "Chấm công và nghỉ phép",
    creators: ["Employee"],
    approvers: ["Manager", "Director"],
    pendingStatuses: ["PendingDept", "PendingDirector"],
    approveRoute: getApprovalRedirect("LEAVE"),
    legacyRoutes: [],
    scope: "module-adapter",
    status: "active",
  },
  {
    workflowKey: "PAYROLL_FORMULA_APPROVAL",
    workflowName: "Duyệt công thức lương",
    module: "PAYROLL",
    moduleLabel: "Lương",
    ownerArea: "Lương",
    creators: ["HR"],
    approvers: ["Director"],
    pendingStatuses: ["PendingDirectorApproval"],
    approveRoute: getApprovalRedirect("PAYROLL"),
    legacyRoutes: [],
    scope: "module-adapter",
    status: "active",
    note: "Chưa có endpoint duyệt công thức dùng chung; vẫn theo dõi trong kế hoạch chuẩn hóa payroll.",
  },
  {
    workflowKey: "PAYROLL_RUN_APPROVAL",
    workflowName: "Duyệt bảng lương",
    module: "PAYROLL",
    moduleLabel: "Lương",
    ownerArea: "Lương",
    creators: ["HR"],
    approvers: ["Director"],
    pendingStatuses: ["PendingApproval"],
    approveRoute: getApprovalRedirect("PAYROLL"),
    legacyRoutes: [],
    scope: "module-adapter",
    status: "active",
    note: "Workspace đã gom bảng lương chờ xử lý; thao tác chốt chi tiết vẫn ở trang Lương.",
  },
  {
    workflowKey: "PROJECT_BONUS_IMPORT",
    workflowName: "Duyệt thưởng dự án",
    module: "PAYROLL",
    moduleLabel: "Lương",
    ownerArea: "Lương",
    creators: ["HR", "Accountant"],
    approvers: ["Director"],
    pendingStatuses: ["PendingReview"],
    approveRoute: getApprovalRedirect("PAYROLL"),
    legacyRoutes: [],
    scope: "module-adapter",
    status: "active",
    note: "Batch thưởng dự án được duyệt trước khi đưa vào payroll.",
  },
  {
    workflowKey: "EXTERNAL_TIMESHEET_IMPORT",
    workflowName: "Duyệt giờ công cộng tác viên",
    module: "PAYROLL",
    moduleLabel: "Lương",
    ownerArea: "Lương",
    creators: ["HR", "Accountant"],
    approvers: ["Director"],
    pendingStatuses: ["Validated"],
    approveRoute: getApprovalRedirect("PAYROLL"),
    legacyRoutes: [],
    scope: "module-adapter",
    status: "active",
    note: "Batch giờ công cộng tác viên được duyệt trước khi đưa vào payroll.",
  },
  {
    workflowKey: "PERSONNEL_CHANGE_APPROVAL",
    workflowName: "Duyệt biến động nhân sự",
    module: "PERSONNEL_CHANGE",
    moduleLabel: "Biến động nhân sự",
    ownerArea: "Biến động nhân sự",
    creators: ["HR", "Manager", "Employee"],
    approvers: ["HR", "Manager", "Director", "Employee"],
    pendingStatuses: [
      "PendingHRReview",
      "PendingManagerReview",
      "PendingCurrentManagerOpinion",
      "PendingEmployeeConsent",
      "PendingDirectorApproval",
    ],
    approveRoute: getApprovalRedirect("PERSONNEL_CHANGE"),
    legacyRoutes: [],
    scope: "module-adapter",
    status: "active",
    note: "Workspace đã gom các bước xem xét, xác nhận và duyệt chính của biến động nhân sự.",
  },
  {
    workflowKey: "PERFORMANCE_APPROVAL",
    workflowName: "Chấm điểm KPI",
    module: "PERFORMANCE",
    moduleLabel: "Hiệu suất",
    ownerArea: "Hiệu suất và đào tạo",
    creators: ["Employee"],
    approvers: ["Manager", "HR", "Director"],
    pendingStatuses: ["PendingEvaluation", "ReworkRequired"],
    approveRoute: getApprovalRedirect("PERFORMANCE"),
    legacyRoutes: [],
    scope: "module-adapter",
    status: "partial",
    note: "Workspace hiển thị KPI chờ chấm; form chấm điểm chi tiết vẫn nằm ở trang Đánh giá KPI.",
  },
];
