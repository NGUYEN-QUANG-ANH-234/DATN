import { useEffect, useState } from "react";
import { Outlet } from "react-router-dom";
import { Sidebar } from "./Sidebar";
import { Topbar } from "./Topbar";

export const AppLayout = () => {
  const [collapsed, setCollapsed] = useState(false);
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);

  useEffect(() => {
    const mediaQuery = window.matchMedia("(min-width: 768px) and (max-width: 1023px)");
    const syncTabletState = () => {
      if (mediaQuery.matches) {
        setCollapsed(true);
      }
    };

    syncTabletState();
    mediaQuery.addEventListener("change", syncTabletState);
    return () => mediaQuery.removeEventListener("change", syncTabletState);
  }, []);

  const handleSidebarToggle = () => {
    if (window.innerWidth < 768) {
      setMobileSidebarOpen((value) => !value);
      return;
    }

    setCollapsed((value) => !value);
  };

  return (
    <div className="hicas-app-shell flex h-screen w-full overflow-hidden">
      {mobileSidebarOpen && (
        <button
          type="button"
          className="fixed inset-0 z-40 bg-black/40 backdrop-blur-[1px] md:hidden"
          onClick={() => setMobileSidebarOpen(false)}
          aria-label="Đóng menu"
        />
      )}

      <Sidebar
        collapsed={collapsed}
        mobileOpen={mobileSidebarOpen}
        onToggle={handleSidebarToggle}
        onNavigate={() => setMobileSidebarOpen(false)}
      />

      <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
        <Topbar onToggleSidebar={handleSidebarToggle} />

        <main className="flex-1 overflow-y-auto bg-[var(--hicas-bg)]">
          <div className="min-h-full p-4 sm:p-5 lg:p-8">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
};
