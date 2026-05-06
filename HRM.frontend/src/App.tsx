import { BrowserRouter, Routes, Route } from "react-router-dom";
import { LoginPage, MfaSetup } from "./core/auth"; // Gọn gàng, không dính líu bên trong
import DashboardPage from "./features/dashboard/DashboardPage";
import ProtectedRoute from "./routes/ProtectedRoute";
import { MainLayout } from "./core/layouts/MainLayout";

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

            {/* Các trang sau này (nhân viên, chấm công...) cũng sẽ nhét hết vào đây */}
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
