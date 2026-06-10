import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AccountSecurityPage, LoginPage, MfaSetup } from "./core/auth";
import { useCurrentUser } from "./core/auth/hooks/useCurrentUser";
import DashboardPage from "./features/dashboard/DashboardPage";
import ProtectedRoute from "./routes/ProtectedRoute";
import { getFirstAccessiblePath } from "./routes/appRoutes";
import { MainLayout } from "./core/layouts/MainLayout";
import {
  AttendanceConfigManager,
  PayrollPolicyManager,
  SalaryVariableManager,
  SlaManager,
  TemplateManager,
  DocumentTemplateManager,
  RbacManager,
  AuditLogViewer,
  ScheduleConfiguration,
  CompanyCalendarManager,
} from "./features/system";
import { AccountManagement } from "./features/system/components/AccountManagement";
import { NotificationProvider } from "./core/context/NotificationContext";
import { DepartmentManagement } from "./features/organization";
import {
  ApprovalTrackingPage,
  ApprovalWorkspacePage,
  getApprovalRedirect,
} from "./features/approvals";
import {
  AttendanceLogPage,
  AttendanceSummaryPage,
  LeaveRequestPage,
  OvertimeRequestPage,
} from "./features/attendance";
import {
  MyContracts,
  MyProfile,
  OnboardingForm,
  ProfileUpdateForm,
  ContractManagement,
  HRContractManagement,
  ContractAddendumManagement,
  EmployeeHistoryTimeline,
} from "./features/employees";
import {
  CreateRecruitmentForm,
  RecruitmentDemandListPage,
  PublicCareersPage,
  CandidateHistory,
  CandidateManagement,
} from "./features/recruitment";
import {
  KpiImportPage,
  PerformanceEvaluationPage,
  TaskWorkspacePage,
  TrainingEvaluationPage,
} from "./features/tasks";
import {
  ExternalTimesheetImportPage,
  PayrollAdjustmentPage,
  PayrollAggregationPage,
  PayslipLookupPage,
  ProjectBonusImportPage,
  SalaryFormulaPage,
} from "./features/payroll";
import { PenaltyRecordPage } from "./features/penalties";
import { PersonnelChangePage } from "./features/personnel-change";
import { DocumentFormsPage } from "./features/document-forms";

const Redirect = ({ to }: { to: string }) => <Navigate to={to} replace />;

const RoleRedirect = ({ candidates }: { candidates: string[] }) => {
  const { user } = useCurrentUser();
  return <Navigate to={getFirstAccessiblePath(user?.role, candidates)} replace />;
};

const SYSTEM_CONFIG_ROUTES = [
  "/system-config/positions-departments",
  "/system-config/salary-variables",
  "/system-config/sla",
  "/system-config/notification-templates",
  "/system-config/document-templates",
  "/system-config/attendance-parameters",
  "/system-config/work-schedules",
  "/system-config/company-calendar",
  "/system-config/payroll-policies",
];

const ADMIN_ROUTES = [
  "/admin/roles-permissions",
  "/admin/audit-logs",
  "/admin/identity-auth",
  "/admin/accounts-access",
];

const RECRUITMENT_ROUTES = [
  "/recruitment/jobs",
  "/recruitment/demands",
  "/recruitment/candidates",
  "/recruitment/history",
];

const EMPLOYEE_CONTRACT_ROUTES = [
  "/employee-contract/my-profile",
  "/employee-contract/profile-change",
  "/employee-contract/contracts",
  "/employee-contract/contract-requests",
  "/employee-contract/profile-setup",
  "/employee-contract/hr-contracts",
  "/employee-contract/appendices",
  "/employee-contract/history",
];

const ATTENDANCE_ROUTES = [
  "/attendance-leave/attendance",
  "/attendance-leave/overtime",
  "/attendance-leave/timesheet-summary",
  "/attendance-leave/leave",
];

const PERFORMANCE_ROUTES = [
  "/performance-training/criteria",
  "/performance-training/result-update",
  "/performance-training/penalties",
  "/performance-training/review-finalize",
  "/performance-training/development-training",
];

const PAYROLL_ROUTES = [
  "/payroll/salary-formula",
  "/payroll/payroll-aggregation",
  "/payroll/payslip",
  "/payroll/adjustments",
  "/payroll/project-bonuses",
  "/payroll/external-timesheets",
];

const PERSONNEL_CHANGE_ROUTES = [
  "/personnel-change/promotion",
  "/personnel-change/senior-appointment",
  "/personnel-change/termination",
  "/personnel-change/dismissal",
  "/personnel-change/internal-transfer",
];

function App() {
  return (
    <BrowserRouter>
      <NotificationProvider>
        <Routes>
          <Route path="/" element={<LoginPage />} />
          <Route path="/careers" element={<PublicCareersPage />} />

          <Route element={<ProtectedRoute />}>
            <Route element={<MainLayout />}>
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/mfa-setup" element={<MfaSetup />} />
              <Route path="/account/security" element={<AccountSecurityPage />} />

              <Route
                path="/system-config"
                element={<RoleRedirect candidates={SYSTEM_CONFIG_ROUTES} />}
              />
              <Route
                path="/system-config/positions-departments"
                element={<DepartmentManagement />}
              />
              <Route
                path="/system-config/salary-variables"
                element={<SalaryVariableManager />}
              />
              <Route
                path="/system-config/payroll-policies"
                element={<PayrollPolicyManager />}
              />
              <Route path="/system-config/sla" element={<SlaManager />} />
              <Route
                path="/system-config/notification-templates"
                element={<TemplateManager />}
              />
              <Route
                path="/system-config/document-templates"
                element={<DocumentTemplateManager />}
              />
              <Route
                path="/system-config/attendance-parameters"
                element={<AttendanceConfigManager />}
              />
              <Route
                path="/system-config/work-schedules"
                element={<ScheduleConfiguration />}
              />
              <Route
                path="/system-config/company-calendar"
                element={<CompanyCalendarManager />}
              />

              <Route path="/admin" element={<RoleRedirect candidates={ADMIN_ROUTES} />} />
              <Route path="/admin/roles-permissions" element={<RbacManager />} />
              <Route path="/admin/audit-logs" element={<AuditLogViewer />} />
              <Route path="/admin/identity-auth" element={<MfaSetup />} />
              <Route path="/admin/accounts-access" element={<AccountManagement />} />

              <Route path="/recruitment" element={<RoleRedirect candidates={RECRUITMENT_ROUTES} />} />
              <Route path="/recruitment/jobs" element={<PublicCareersPage />} />
              <Route
                path="/recruitment/demands"
                element={<RecruitmentDemandListPage />}
              />
              <Route path="/recruitment/demands/create" element={<CreateRecruitmentForm />} />
              <Route path="/recruitment/apply-cv" element={<PublicCareersPage />} />
              <Route path="/recruitment/candidates" element={<CandidateManagement />} />
              <Route
                path="/recruitment/candidate-review"
                element={<Redirect to="/recruitment/candidates" />}
              />
              <Route path="/recruitment/history" element={<CandidateHistory />} />

              <Route
                path="/employee-contract"
                element={<RoleRedirect candidates={EMPLOYEE_CONTRACT_ROUTES} />}
              />
              <Route path="/employee-contract/my-profile" element={<MyProfile />} />
              <Route path="/employee-contract/profile-setup" element={<OnboardingForm />} />
              <Route
                path="/employee-contract/profile-setup/:candidateId"
                element={<OnboardingForm />}
              />
              <Route path="/employee-contract/profile-change" element={<ProfileUpdateForm />} />
              <Route
                path="/employee-contract/profile-review"
                element={<Redirect to={getApprovalRedirect("PROFILE")} />}
              />
              <Route path="/employee-contract/contracts" element={<MyContracts />} />
              <Route
                path="/employee-contract/contract-requests"
                element={<ContractManagement />}
              />
              <Route path="/employee-contract/hr-contracts" element={<HRContractManagement />} />
              <Route
                path="/employee-contract/director-contract-approval"
                element={<Redirect to={getApprovalRedirect("CONTRACT")} />}
              />
              <Route
                path="/employee-contract/appendices"
                element={<ContractAddendumManagement />}
              />
              <Route path="/employee-contract/history" element={<EmployeeHistoryTimeline />} />

              <Route
                path="/attendance-leave"
                element={<RoleRedirect candidates={ATTENDANCE_ROUTES} />}
              />
              <Route path="/attendance-leave/attendance" element={<AttendanceLogPage />} />
              <Route path="/attendance-leave/overtime" element={<OvertimeRequestPage />} />
              <Route
                path="/attendance-leave/overtime-approvals"
                element={<Redirect to={getApprovalRedirect("OVERTIME")} />}
              />
              <Route
                path="/attendance-leave/timesheet-summary"
                element={<AttendanceSummaryPage />}
              />
              <Route path="/attendance-leave/leave" element={<LeaveRequestPage />} />

              <Route
                path="/performance-training"
                element={<RoleRedirect candidates={PERFORMANCE_ROUTES} />}
              />
              <Route path="/performance-training/criteria" element={<KpiImportPage />} />
              <Route
                path="/performance-training/result-update"
                element={<TaskWorkspacePage />}
              />
              <Route
                path="/performance-training/penalties"
                element={<PenaltyRecordPage />}
              />
              <Route
                path="/performance-training/review-finalize"
                element={<PerformanceEvaluationPage />}
              />
              <Route
                path="/performance-training/development-training"
                element={<TrainingEvaluationPage />}
              />

              <Route path="/payroll" element={<RoleRedirect candidates={PAYROLL_ROUTES} />} />
              <Route path="/payroll/salary-formula" element={<SalaryFormulaPage />} />
              <Route path="/payroll/payroll-aggregation" element={<PayrollAggregationPage />} />
              <Route path="/payroll/payslip" element={<PayslipLookupPage />} />
              <Route path="/payroll/adjustments" element={<PayrollAdjustmentPage />} />
              <Route path="/payroll/project-bonuses" element={<ProjectBonusImportPage />} />
              <Route
                path="/payroll/external-timesheets"
                element={<ExternalTimesheetImportPage />}
              />

              <Route
                path="/personnel-change"
                element={<RoleRedirect candidates={PERSONNEL_CHANGE_ROUTES} />}
              />
              <Route
                path="/personnel-change/promotion"
                element={<PersonnelChangePage kind="promotion" />}
              />
              <Route
                path="/personnel-change/senior-appointment"
                element={<PersonnelChangePage kind="senior-appointment" />}
              />
              <Route
                path="/personnel-change/termination"
                element={<PersonnelChangePage kind="termination" />}
              />
              <Route
                path="/personnel-change/dismissal"
                element={<PersonnelChangePage kind="dismissal" />}
              />
              <Route
                path="/personnel-change/internal-transfer"
                element={<PersonnelChangePage kind="internal-transfer" />}
              />

              <Route path="/approvals" element={<ApprovalWorkspacePage />} />
              <Route path="/approvals/tracking" element={<ApprovalTrackingPage />} />

              <Route path="/document-forms" element={<RoleRedirect candidates={["/document-forms/create"]} />} />
              <Route path="/document-forms/create" element={<DocumentFormsPage />} />

              <Route path="/system" element={<RoleRedirect candidates={SYSTEM_CONFIG_ROUTES} />} />
              <Route
                path="/system/salary-variables"
                element={<Redirect to="/system-config/salary-variables" />}
              />
              <Route
                path="/system/payroll-policies"
                element={<Redirect to="/system-config/payroll-policies" />}
              />
              <Route path="/system/sla" element={<Redirect to="/system-config/sla" />} />
              <Route
                path="/system/templates"
                element={<Redirect to="/system-config/notification-templates" />}
              />
              <Route
                path="/system/document-templates"
                element={<Redirect to="/system-config/document-templates" />}
              />
              <Route
                path="/system/attendance-config"
                element={<Redirect to="/system-config/attendance-parameters" />}
              />
              <Route
                path="/system/schedule-configuration"
                element={<Redirect to="/system-config/work-schedules" />}
              />
              <Route
                path="/system/company-calendar"
                element={<Redirect to="/system-config/company-calendar" />}
              />
              <Route path="/system/rbac" element={<Redirect to="/admin/roles-permissions" />} />
              <Route path="/system/audit-logs" element={<Redirect to="/admin/audit-logs" />} />
              <Route
                path="/system/account-management"
                element={<Redirect to="/admin/accounts-access" />}
              />
              <Route
                path="/organization/department-management"
                element={<Redirect to="/system-config/positions-departments" />}
              />

              <Route
                path="/recruitment/all"
                element={<Redirect to="/recruitment/jobs" />}
              />
              <Route
                path="/recruitment/create"
                element={<Redirect to="/recruitment/demands/create" />}
              />
              <Route
                path="/recruitment/approval-inbox"
                element={<Redirect to={getApprovalRedirect("RECRUITMENT")} />}
              />
              <Route
                path="/recruitment/candidates-management"
                element={<Redirect to="/recruitment/candidates" />}
              />

              <Route
                path="/employees/my-profile"
                element={<Redirect to="/employee-contract/my-profile" />}
              />
              <Route
                path="/employees/profile-update"
                element={<Redirect to="/employee-contract/profile-change" />}
              />
              <Route
                path="/employees/hr-profile-review"
                element={<Redirect to={getApprovalRedirect("PROFILE")} />}
              />
              <Route
                path="/employees/my-contracts"
                element={<Redirect to="/employee-contract/contracts" />}
              />
              <Route
                path="/employees/history"
                element={<Redirect to="/employee-contract/history" />}
              />
              <Route
                path="/employees/contract-management"
                element={<Redirect to="/employee-contract/contract-requests" />}
              />
              <Route
                path="/employees/hr-contract-management"
                element={<Redirect to="/employee-contract/hr-contracts" />}
              />
              <Route
                path="/employees/director-contract-approval"
                element={<Redirect to={getApprovalRedirect("CONTRACT")} />}
              />
              <Route
                path="/employees/contract-addendums"
                element={<Redirect to="/employee-contract/appendices" />}
              />
              <Route
                path="/employees/onboarding"
                element={<Redirect to="/employee-contract/profile-setup" />}
              />
              <Route path="/employees/onboarding/:candidateId" element={<OnboardingForm />} />

              <Route
                path="/attendance"
                element={<Redirect to="/attendance-leave/attendance" />}
              />
              <Route
                path="/attendance/overtime"
                element={<Redirect to="/attendance-leave/overtime" />}
              />
              <Route
                path="/attendance/overtime-approvals"
                element={<Redirect to={getApprovalRedirect("OVERTIME")} />}
              />
              <Route
                path="/attendance/leaves"
                element={<Redirect to="/attendance-leave/leave" />}
              />
              <Route
                path="/attendance/summary"
                element={<Redirect to="/attendance-leave/timesheet-summary" />}
              />

              <Route
                path="/tasks/kpi-import"
                element={<Redirect to="/performance-training/criteria" />}
              />
              <Route
                path="/tasks/workspace"
                element={<Redirect to="/performance-training/result-update" />}
              />
              <Route
                path="/tasks/penalties"
                element={<Redirect to="/performance-training/penalties" />}
              />
              <Route
                path="/tasks/performance-evaluation"
                element={<Redirect to="/performance-training/review-finalize" />}
              />
              <Route
                path="/tasks/training-evaluation"
                element={<Redirect to="/performance-training/development-training" />}
              />

              <Route
                path="/payroll/aggregation"
                element={<Redirect to="/payroll/payroll-aggregation" />}
              />
              <Route
                path="/payroll/payslips"
                element={<Redirect to="/payroll/payslip" />}
              />
              <Route
                path="/payroll/formulas"
                element={<Redirect to="/payroll/salary-formula" />}
              />
            </Route>
          </Route>
        </Routes>
      </NotificationProvider>
    </BrowserRouter>
  );
}

export default App;
