import { useState, useEffect } from "react";
import { jwtDecode } from "jwt-decode";
import type { UserState, JwtPayload } from "../types";

export const useCurrentUser = () => {
  const [user, setUser] = useState<UserState | null>(null);

  useEffect(() => {
    const token = localStorage.getItem("accessToken");

    if (token) {
      try {
        // Ép kiểu để Typescript gợi ý code (IntelliSense)
        const decoded = jwtDecode<JwtPayload>(token);

        // eslint-disable-next-line react-hooks/set-state-in-effect
        setUser({
          name: decoded.email || "Người dùng",
          // Bạn có thể map RoleId thành text hiển thị
          role: decoded.RoleId === "1" ? "Admin" : "Nhân viên",
          avatar: "", // Mặc định trống, hoặc thêm link ảnh placeholder
          isMfaEnabled: String(decoded.IsMfaEnabled).toLowerCase() === "true",
        } as UserState);
      } catch (error) {
        console.error("Token không hợp lệ hoặc đã hết hạn:", error);
        // Tùy chọn: Xóa token lỗi đi
        localStorage.removeItem("accessToken");
      }
    }
  }, []); // Cặp ngoặc [] giúp hook chỉ chạy 1 lần khi Component được mount

  return { user };
};
