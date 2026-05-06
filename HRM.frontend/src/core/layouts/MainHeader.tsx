import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/hooks/useAuth";
import { useCurrentUser } from "../auth/hooks/useCurrentUser";

export const MainHeader = () => {
  const { user } = useCurrentUser();
  const { logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/");
  };

  return (
    <header className="flex h-16 items-center justify-between bg-white px-6 shadow-sm border-b">
      <div className="text-xl font-bold text-blue-700">HRM HICAS</div>

      {/* Vùng Avatar có chứa Dropdown */}
      <div className="relative group cursor-pointer">
        <div className="flex items-center gap-3">
          <div className="text-right hidden md:block">
            <p className="text-sm font-semibold text-gray-700">
              {user?.name || "Người dùng"}
            </p>
            <p className="text-xs text-gray-500">{user?.role || "Nhân viên"}</p>
          </div>
          <img
            src={
              user?.avatar ||
              "https://ui-avatars.com/api/?name=User&background=random"
            }
            alt="Avatar"
            className="h-10 w-10 rounded-full object-cover border-2 border-gray-200"
          />
        </div>

        {/* Menu Dropdown - Chỉ hiện ra khi hover */}
        <div className="invisible absolute right-0 top-full mt-2 w-48 rounded-md bg-white py-2 shadow-lg opacity-0 transition-all duration-200 group-hover:visible group-hover:opacity-100 z-50 border border-gray-100">
          <Link
            to="/profile"
            className="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
          >
            Cấu hình tài khoản
          </Link>
          <Link
            to="/mfa-setup"
            className="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
          >
            Thiết lập MFA
          </Link>
          <div className="my-1 border-t border-gray-100"></div>
          <button
            onClick={handleLogout}
            className="block w-full text-left px-4 py-2 text-sm text-red-600 hover:bg-red-50"
          >
            Đăng xuất
          </button>
        </div>
      </div>
    </header>
  );
};
