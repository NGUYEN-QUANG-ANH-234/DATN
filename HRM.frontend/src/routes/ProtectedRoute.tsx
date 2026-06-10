import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useCurrentUser } from "../core/auth/hooks/useCurrentUser";
import { normalizeRole } from "../core/auth/roleAccess";
import { canAccessPath } from "./appRoutes";

const ProtectedRoute = () => {
  const location = useLocation();
  const { user } = useCurrentUser();
  const token = localStorage.getItem("accessToken");

  const isAuthenticated = token && token !== "undefined" && token !== "null";

  if (!isAuthenticated || !user || !normalizeRole(user.role)) {
    localStorage.removeItem("accessToken");
    localStorage.removeItem("refreshToken");
    return <Navigate to="/" replace />;
  }

  if (!canAccessPath(location.pathname, user.role)) {
    return <Navigate to="/dashboard" replace state={{ blockedPath: location.pathname }} />;
  }

  return <Outlet />;
};

export default ProtectedRoute;
