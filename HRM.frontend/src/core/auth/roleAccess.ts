export const APP_ROLES = [
  "Admin",
  "HR",
  "Manager",
  "Director",
  "Employee",
  "Intern",
  "Candidate",
  "Collaborator",
] as const;

export type AppRole = (typeof APP_ROLES)[number];
export type RoleList = readonly AppRole[];

export const ROLE_LABELS: Record<AppRole, string> = {
  Admin: "Quản trị",
  HR: "Nhân sự",
  Manager: "Quản lý",
  Director: "Giám đốc",
  Employee: "Nhân viên",
  Intern: "Thực tập sinh",
  Candidate: "Ứng viên",
  Collaborator: "Cộng tác viên",
};

export const ROLE_GROUPS = {
  all: APP_ROLES,
  systemConfig: ["Admin", "HR", "Director"],
  systemAdmin: ["Admin"],
  hrAdmin: ["Admin", "HR"],
  organization: ["Admin", "HR", "Director"],
  recruitmentPublic: ["Admin", "HR", "Manager", "Director", "Employee", "Intern", "Candidate"],
  recruitmentOps: ["Admin", "HR", "Manager", "Director"],
  recruitmentDemandCreators: ["Admin", "HR", "Manager"],
  employeeSelf: ["HR", "Manager", "Director", "Employee", "Intern"],
  employeeAdmin: ["Admin", "HR"],
  employeeAdminDirector: ["Admin", "HR", "Director"],
  attendanceSelf: ["Admin", "HR", "Manager", "Employee", "Intern"],
  attendanceSummary: ["Admin", "HR"],
  leave: ["Admin", "HR", "Manager", "Director", "Employee", "Intern"],
  performanceContributors: ["Admin", "HR", "Manager", "Employee", "Intern"],
  performanceManagers: ["Admin", "HR", "Manager"],
  performanceDiscipline: ["Admin", "HR", "Manager", "Director"],
  payrollSensitive: ["Admin", "HR", "Director"],
  payrollAdjustments: ["Admin", "HR"],
  payrollSlips: ["Admin", "HR", "Manager", "Director", "Employee", "Intern", "Collaborator"],
  personnelChange: ["Admin", "HR", "Manager", "Director"],
  personnelChangeExecutive: ["Admin", "HR", "Director"],
  documentForms: ["Admin", "HR", "Manager", "Director", "Employee", "Intern", "Collaborator"],
  approvalInbox: ["Admin", "HR", "Manager", "Director"],
  approvalTracking: ["Admin", "HR", "Manager", "Director", "Employee", "Intern", "Candidate"],
} as const satisfies Record<string, RoleList>;

const ROLE_ALIASES: Record<string, AppRole> = {
  admin: "Admin",
  "quản trị": "Admin",
  "quan tri": "Admin",
  hr: "HR",
  "nhân sự": "HR",
  "nhan su": "HR",
  manager: "Manager",
  "quản lý": "Manager",
  "quan ly": "Manager",
  "trưởng phòng": "Manager",
  "truong phong": "Manager",
  director: "Director",
  "giám đốc": "Director",
  "giam doc": "Director",
  employee: "Employee",
  "nhân viên": "Employee",
  "nhan vien": "Employee",
  intern: "Intern",
  "thực tập sinh": "Intern",
  "thuc tap sinh": "Intern",
  candidate: "Candidate",
  "ứng viên": "Candidate",
  "ung vien": "Candidate",
  collaborator: "Collaborator",
  "cộng tác viên": "Collaborator",
  "cong tac vien": "Collaborator",
};

export const normalizeRole = (role?: string | null): AppRole | undefined => {
  const raw = String(role || "").trim();
  if (!raw) return undefined;

  const exact = APP_ROLES.find((item) => item.toLowerCase() === raw.toLowerCase());
  if (exact) return exact;

  return ROLE_ALIASES[raw.toLowerCase()];
};

export const hasAnyRole = (allowedRoles: readonly string[], role?: string | null) => {
  if (allowedRoles.length === 0) return true;

  const normalized = normalizeRole(role);
  if (!normalized) return false;

  return allowedRoles.some((item) => item.toLowerCase() === normalized.toLowerCase());
};

export const getRoleLabel = (role?: string | null) => {
  const normalized = normalizeRole(role);
  return normalized ? ROLE_LABELS[normalized] : String(role || "Người dùng");
};
