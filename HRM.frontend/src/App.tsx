import { BrowserRouter, Routes, Route } from "react-router-dom";
import { LoginPage, MfaSetup } from "./core/auth";
import DashboardPage from "./features/dashboard/DashboardPage";
import ProtectedRoute from "./routes/ProtectedRoute";
import { MainLayout } from "./core/layouts/MainLayout";
import {
  AttendanceConfigManager,
  SalaryVariableManager,
  SlaManager,
  TemplateManager,
  RbacManager,
  AuditLogViewer,
  ScheduleConfiguration,
} from "./features/system";
import { AccountManagement } from "./features/system/components/AccountManagement";

import { NotificationProvider } from "./core/context/NotificationContext";
import { DepartmentManagement } from "./features/organization";
import { AttendanceLogPage } from "./features/attendance";
import {
  HRProfileReviewList,
  MyContracts,
  MyProfile,
  OnboardingForm,
  ProfileUpdateForm,
  ContractManagement,
  HRContractManagement,
  DirectorContractApproval,
  ContractAddendumManagement,
  EmployeeHistoryTimeline,
} from "./features/employees";
import {
  CreateRecruitmentForm,
  RecruitmentApprovalInbox,
  PublicCareersPage,
  CandidateHistory,
  CandidateManagement,
} from "./features/recruitment";

function App() {
  return (
    <BrowserRouter>
      <NotificationProvider>
        <Routes>
          <Route path="/" element={<LoginPage />} />
          <Route path="/careers" element={<PublicCareersPage />} />

          {/* Lớp 1: Kiểm tra đăng nhập */}
          <Route element={<ProtectedRoute />}>
            {/* Lớp 2: Khung giao diện (Header + Sidebar) */}
            <Route element={<MainLayout />}>
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/mfa-setup" element={<MfaSetup />} />

              {/* Module 1: Quản lý hệ thống */}
              <Route
                path="/system/salary-variables"
                element={<SalaryVariableManager />}
              />
              <Route path="/system/templates" element={<TemplateManager />} />
              <Route path="/system/sla" element={<SlaManager />} />
              <Route
                path="/system/attendance-config"
                element={<AttendanceConfigManager />}
              />
              <Route path="/system/rbac" element={<RbacManager />} />
              <Route path="/system/audit-logs" element={<AuditLogViewer />} />
              <Route
                path="/system/account-management"
                element={<AccountManagement />}
              />
              <Route
                path="/system/schedule-configuration"
                element={<ScheduleConfiguration />}
              />

              {/* Module 2: Quản lý tổ chức */}
              <Route
                path="/organization/department-management"
                element={<DepartmentManagement />}
              />
              <Route path="/attendance" element={<AttendanceLogPage />} />

              {/* Module 3: Hồ sơ nhân sự */}
              <Route
                path="/employees/profile-update"
                element={<ProfileUpdateForm />}
              />
              <Route
                path="/employees/hr-profile-review"
                element={<HRProfileReviewList />}
              />
              <Route path="/employees/my-profile" element={<MyProfile />} />
              <Route path="/employees/my-contracts" element={<MyContracts />} />
              <Route path="/employees/history" element={<EmployeeHistoryTimeline />} />
              <Route path="/employees/contract-management" element={<ContractManagement />} />
              <Route path="/employees/hr-contract-management" element={<HRContractManagement />} />
              <Route path="/employees/director-contract-approval" element={<DirectorContractApproval />} />
              <Route path="/employees/contract-addendums" element={<ContractAddendumManagement />} />
              <Route path="/employees/onboarding" element={<OnboardingForm />} />
              <Route
                path="/employees/onboarding/:candidateId"
                element={<OnboardingForm />}
              />

              {/* Module 4: Quản lý tuyển dụng */}
              <Route
                path="/recruitment/create"
                element={<CreateRecruitmentForm />}
              />
              <Route
                path="/recruitment/approval-inbox"
                element={<RecruitmentApprovalInbox />}
              />
              <Route path="/recruitment/all" element={<PublicCareersPage />} />
              <Route
                path="/recruitment/history"
                element={<CandidateHistory />}
              />
              <Route
                path="/recruitment/candidates-management"
                element={<CandidateManagement />}
              />
            </Route>
          </Route>
        </Routes>
      </NotificationProvider>
    </BrowserRouter>
  );
}

export default App;
