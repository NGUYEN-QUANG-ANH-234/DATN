import { useCurrentUser } from "../auth/hooks/useCurrentUser";
import { MENU_ITEMS } from "../types/menu";
import { SidebarItem } from "./SidebarItem";

export const MainSidebar = () => {
  const { user } = useCurrentUser();
  const userRole = user?.role || "";

  // Lọc menu cấp 1: Nếu role của user nằm trong mảng roles của item đó thì mới hiển thị
  const visibleMenu = MENU_ITEMS.filter((item) =>
    item.roles.includes(userRole),
  );

  return (
    <aside className="flex w-64 flex-col bg-slate-900 text-gray-300 transition-all duration-300">
      <div className="flex h-16 items-center justify-center border-b border-slate-700">
        <span className="text-lg font-bold tracking-wider text-white">
          HRM HICAS
        </span>
      </div>

      <nav className="scrollbar-sidebar flex-1 space-y-1 overflow-y-auto p-3">
        {visibleMenu.map((item, index) => (
          <SidebarItem key={index} item={item} userRole={userRole} />
        ))}
      </nav>
    </aside>
  );
};
