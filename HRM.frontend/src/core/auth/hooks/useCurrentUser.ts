import { useState } from "react";
import { jwtDecode } from "jwt-decode";
import type { UserState, JwtPayload } from "../types";

export const useCurrentUser = () => {
  // Đưa logic vào hàm khởi tạo của useState (chỉ chạy 1 lần khi mount)
  const [user, setUser] = useState<UserState | null>(() => {
    const token = localStorage.getItem("accessToken");
    if (!token) return null;

    try {
      const decoded = jwtDecode<JwtPayload>(token);

      const nameClaim =
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
      const emailClaim =
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";

      return {
        name:
          decoded.name ||
          decoded[nameClaim] ||
          decoded.email ||
          decoded[emailClaim] ||
          "Người dùng",
        role: decoded.role || "Nhân viên",
        avatar: decoded.avatar || "",
      } as UserState;
    } catch (error) {
      console.error("Token không hợp lệ hoặc đã hết hạn:", error);
      localStorage.removeItem("accessToken");
      return null;
    }
  });

  // Export thêm setUser đề phòng trường hợp bạn muốn update user sau khi edit profile
  return { user, setUser };
};
