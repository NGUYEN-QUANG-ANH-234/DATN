import { Outlet } from "react-router-dom";
import { MainHeader } from "./MainHeader";
import { MainSidebar } from "./MainSidebar";

export const MainLayout = () => {
  return (
    <div className="flex h-screen w-full bg-gray-50 overflow-hidden text-gray-900">
      {/* Cột trái: Sidebar cố định */}
      <MainSidebar />

      {/* Cột phải: Vùng nội dung chính */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* Header ở trên cùng cột phải */}
        <MainHeader />

        {/* Nội dung trang thay đổi theo URL */}
        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
};
