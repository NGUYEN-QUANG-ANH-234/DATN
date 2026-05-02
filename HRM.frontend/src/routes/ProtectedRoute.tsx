import { Navigate, Outlet } from "react-router-dom";

const ProtectedRoute = () => {
  const token = localStorage.getItem("accessToken");

  // Kiểm tra token tồn tại và không phải chuỗi rỗng/undefined
  const isAuthenticated = token && token !== "undefined" && token !== "null";

  return isAuthenticated ? <Outlet /> : <Navigate to="/" replace />;
};

export default ProtectedRoute;
