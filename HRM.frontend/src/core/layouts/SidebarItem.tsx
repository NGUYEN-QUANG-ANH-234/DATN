import { useState, useEffect } from "react";
import { Link, useLocation } from "react-router-dom";
import type { MenuItem } from "../types/menu";

interface SidebarItemProps {
  item: MenuItem;
  userRole: string;
}

export const SidebarItem = ({ item, userRole }: SidebarItemProps) => {
  const location = useLocation();

  const isChildActive = item.children?.some((child) =>
    location.pathname.startsWith(child.path || ""),
  );
  const isExactActive = location.pathname === item.path;

  const [isOpen, setIsOpen] = useState(isChildActive || false);

  const visibleChildren = item.children?.filter((child) =>
    child.roles.includes(userRole),
  );

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    if (isChildActive) setIsOpen(true);
  }, [isChildActive]);

  // NẾU LÀ ITEM THƯỜNG
  if (!item.children || !visibleChildren || visibleChildren.length === 0) {
    return (
      <Link
        to={item.path || "#"}
        className={`block rounded-lg px-4 py-2.5 text-sm font-medium transition-all duration-300 ease-in-out ${
          isExactActive
            ? "bg-blue-600 text-white shadow-md shadow-blue-900/20"
            : "text-slate-300 hover:bg-white/[0.04] hover:text-white"
        }`}
      >
        {item.label}
      </Link>
    );
  }

  // NẾU LÀ KHỐI CHA (Có Dropdown)
  return (
    <div className="space-y-1 overflow-hidden">
      {/* Nút Khối Cha */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className={`flex w-full items-center justify-between rounded-lg px-4 py-2.5 text-sm font-medium transition-all duration-300 ease-in-out ${
          isChildActive && !isOpen
            ? "font-semibold text-blue-400 bg-white/[0.02]"
            : "text-slate-300 hover:bg-white/[0.04] hover:text-white"
        }`}
      >
        <span>{item.label}</span>
        <svg
          className={`h-4 w-4 transform transition-transform duration-300 ease-in-out ${
            isOpen ? "rotate-90 text-blue-400" : "rotate-0 text-slate-500"
          }`}
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={1.5}
            d="M9 5l7 7-7 7"
          />
        </svg>
      </button>

      {/* Danh sách Menu Con */}
      <div
        className={`grid transition-all duration-300 ease-in-out ${
          isOpen
            ? "grid-rows-[1fr] opacity-100 mt-1"
            : "grid-rows-[0fr] opacity-0"
        }`}
      >
        <div className="overflow-hidden">
          {/* CẬP NHẬT: Đường viền mỏng, nhạt và lùi sâu hơn để tạo cảm giác chìm */}
          <div className="ml-5 space-y-1 border-l border-slate-700/40 pl-3 py-1">
            {visibleChildren.map((child) => {
              const isChildExactActive = location.pathname === child.path;
              return (
                <Link
                  key={child.path}
                  to={child.path || "#"}
                  className={`block rounded-lg px-4 py-2 text-sm transition-all duration-300 ease-in-out ${
                    isChildExactActive
                      ? // CẬP NHẬT: Trạng thái Active dùng màu nền siêu mờ tiệp với màu sidebar
                        "font-medium text-blue-400 bg-white/[0.03]"
                      : // CẬP NHẬT: Trạng thái Inactive có chữ mờ đi (slate-400), hover làm sáng lên nhẹ nhàng
                        "text-slate-400 hover:bg-white/[0.02] hover:text-slate-200 hover:translate-x-1"
                  }`}
                >
                  {child.label}
                </Link>
              );
            })}
          </div>
        </div>
      </div>
    </div>
  );
};
