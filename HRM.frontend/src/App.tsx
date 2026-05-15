import { BrowserRouter, Routes, Route } from "react-router-dom";
import { LoginPage, MfaSetup } from "./core/auth"; // Gọn gàng, không dính líu bên trong
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
} from "./features/system";
import { AccountManagement } from "./features/system/components/AccountManagement";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<LoginPage />} />

        {/* Lớp 1: Kiểm tra đăng nhập */}
        <Route element={<ProtectedRoute />}>
          {/* Lớp 2: Khung giao diện (Header + Sidebar) */}
          <Route element={<MainLayout />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/mfa-setup" element={<MfaSetup />} />

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

            {/* Sau này sẽ thêm các trang quản lý khác vào đây, ví dụ: */}

            {/* Các trang sau này (nhân viên, chấm công...) cũng sẽ nhét hết vào đây */}
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
