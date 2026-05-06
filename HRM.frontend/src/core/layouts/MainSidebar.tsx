import { Link, useLocation } from "react-router-dom";
import { useCurrentUser } from "../auth/hooks/useCurrentUser";

// Cấu hình danh sách các module của hệ thống kèm theo Role được phép xem
const MENU_ITEMS = [
  {
    path: "/dashboard",
    label: "Tổng quan",
    roles: ["Admin", "HR", "Manager", "Employee", "Candidate"],
  },
  {
    path: "/organization",
    label: "Cơ cấu tổ chức",
    roles: ["Admin", "HR", "Manager", "Candidate"],
  },
  { path: "/recruitment", label: "Tuyển dụng", roles: ["Admin", "HR"] },
  {
    path: "/employees",
    label: "Hồ sơ nhân viên",
    roles: ["Admin", "HR", "Manager"],
  },
  {
    path: "/attendance",
    label: "Chấm công",
    roles: ["Admin", "HR", "Manager", "Employee"],
  },
  {
    path: "/tasks",
    label: "Công việc & Đào tạo",
    roles: ["Admin", "Manager", "Employee"],
  },
  { path: "/payroll", label: "Lương & Phụ cấp", roles: ["Admin", "HR"] },
  {
    path: "/requests",
    label: "Yêu cầu & Bàn giao",
    roles: ["Admin", "HR", "Manager", "Employee"],
  },
  { path: "/system", label: "Hệ thống", roles: ["Admin"] },
];

export const MainSidebar = () => {
  const { user } = useCurrentUser();
  const location = useLocation();

  // Lọc menu: Nếu role của user nằm trong mảng roles của item đó thì mới hiển thị
  const visibleMenu = MENU_ITEMS.filter((item) =>
    item.roles.includes(user?.role || ""),
  );

  return (
    <aside className="flex w-64 flex-col bg-slate-900 text-gray-300 transition-all duration-300">
      <div className="flex h-16 items-center justify-center border-b border-slate-700">
        <span className="text-lg font-bold tracking-wider text-white">
          MENU
        </span>
      </div>

      <nav className="flex-1 space-y-1 overflow-y-auto p-3">
        {visibleMenu.map((item) => {
          const isActive = location.pathname.startsWith(item.path);
          return (
            <Link
              key={item.path}
              to={item.path}
              className={`block rounded-lg px-4 py-2.5 text-sm font-medium transition-colors ${
                isActive
                  ? "bg-blue-600 text-white shadow-md"
                  : "hover:bg-slate-800 hover:text-white"
              }`}
            >
              {item.label}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
};
