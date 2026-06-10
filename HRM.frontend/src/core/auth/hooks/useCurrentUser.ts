import { useEffect, useState } from "react";
import { jwtDecode } from "jwt-decode";
import type { UserState, JwtPayload } from "../types";
import { API_BASE_URL } from "../../api/config";
import { normalizeRole } from "../roleAccess";

interface MyProfileHeaderResponse {
  success?: boolean;
  data?: {
    fullName?: string | null;
    avatarUrl?: string | null;
  };
}

export const useCurrentUser = () => {
  const [user, setUser] = useState<UserState | null>(() => {
    const token = localStorage.getItem("accessToken");
    if (!token) return null;

    try {
      const decoded = jwtDecode<JwtPayload>(token);

      const nameClaim =
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
      const emailClaim =
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
      const role = normalizeRole(decoded.role) || String(decoded.role || "");

      return {
        name:
          decoded.name ||
          decoded[nameClaim] ||
          decoded.email ||
          decoded[emailClaim] ||
          "Người dùng",
        role,
        avatar: decoded.avatar || "",
      } as UserState;
    } catch (error) {
      console.error("Token không hợp lệ hoặc đã hết hạn:", error);
      localStorage.removeItem("accessToken");
      return null;
    }
  });

  useEffect(() => {
    const token = localStorage.getItem("accessToken");
    if (!token) return;

    let isMounted = true;

    const syncEmployeeProfileName = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/employees/me/profile`, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
          credentials: "include",
        });

        if (!response.ok) return;

        const res = (await response.json()) as MyProfileHeaderResponse;
        const profile = res.data;

        if (!isMounted || !profile) return;

        setUser((current) => {
          if (!current) return current;

          const name = profile.fullName?.trim() || current.name;
          const avatar = profile.avatarUrl?.trim() || current.avatar;

          if (name === current.name && avatar === current.avatar) {
            return current;
          }

          return {
            ...current,
            name,
            avatar,
          };
        });
      } catch {
        // Candidate/admin-only accounts may not have an employee profile yet.
      }
    };

    void syncEmployeeProfileName();

    return () => {
      isMounted = false;
    };
  }, []);

  return { user, setUser };
};
