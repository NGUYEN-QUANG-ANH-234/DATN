import {
  Bell,
  ChevronDown,
  ClipboardCheck,
  ChevronRight,
  KeyRound,
  LogOut,
  Menu,
  Search,
  ShieldCheck,
  UserRound,
  X,
} from "lucide-react";
import { useMemo, useState } from "react";
import { Link, useLocation } from "react-router-dom";
import { useAuth } from "../../core/auth/hooks/useAuth";
import { useCurrentUser } from "../../core/auth/hooks/useCurrentUser";
import { getRoleLabel } from "../../core/auth/roleAccess";
import { canAccessPath } from "../../routes/appRoutes";
import { cn } from "../ui/classNames";
import { filterMenuByRole, findMenuTrail, getHicasNavigation } from "./navigation";

interface TopbarProps {
  onToggleSidebar: () => void;
}

const getInitials = (name: string) =>
  name
    .split(" ")
    .filter(Boolean)
    .slice(-2)
    .map((part) => part[0])
    .join("")
    .toUpperCase() || "HC";

export const Topbar = ({ onToggleSidebar }: TopbarProps) => {
  const location = useLocation();
  const { user } = useCurrentUser();
  const { logout } = useAuth();
  const [menuOpen, setMenuOpen] = useState(false);
  const [notificationOpen, setNotificationOpen] = useState(false);

  const displayName = user?.name?.trim() || "Người dùng";
  const displayRole = getRoleLabel(user?.role);
  const navigation = useMemo(
    () => filterMenuByRole(getHicasNavigation(), user?.role),
    [user?.role],
  );
  const menuTrail = useMemo(
    () => findMenuTrail(navigation, location.pathname),
    [location.pathname, navigation],
  );
  const fallbackTrail = getFallbackTrail(location.pathname);
  const breadcrumbTrail = menuTrail.length ? menuTrail : fallbackTrail;
  const canOpenApprovalInbox = canAccessPath("/approvals", user?.role, false);
  const canOpenApprovalTracking = canAccessPath("/approvals/tracking", user?.role, false);
  const canOpenProfile = canAccessPath("/employee-contract/my-profile", user?.role, false);
  const avatarUrl =
    user?.avatar?.trim() ||
    `https://ui-avatars.com/api/?name=${encodeURIComponent(
      getInitials(displayName),
    )}&background=FF7A00&color=fff&bold=true`;

  const handleLogout = () => {
    void logout();
  };

  return (
    <header className="shrink-0 border-b border-[var(--hicas-border)] bg-white px-4 py-3 sm:px-6">
      <div className="flex min-h-[52px] items-center justify-between gap-4">
        <div className="flex min-w-0 flex-1 items-center gap-3">
          <button
            type="button"
            onClick={onToggleSidebar}
            className="inline-flex h-11 w-11 items-center justify-center rounded-xl border border-[var(--hicas-border)] text-[var(--hicas-text-secondary)] transition hover:border-[var(--hicas-orange)] hover:bg-[var(--hicas-orange-soft)] hover:text-[var(--hicas-orange)] lg:hidden"
            aria-label="Mở menu"
          >
            <Menu size={19} />
          </button>

          {breadcrumbTrail.length > 0 && (
            <nav
              className="hidden min-w-0 max-w-[560px] items-center gap-2 overflow-hidden rounded-full border border-[var(--hicas-border)] bg-[var(--hicas-bg)] px-4 py-2.5 text-[15px] font-semibold text-[var(--hicas-text-secondary)] md:flex"
              aria-label="Đường dẫn"
            >
              {breadcrumbTrail.map((item, index) => {
                const isLast = index === breadcrumbTrail.length - 1;
                return (
                  <span key={`${item.label}-${index}`} className="flex min-w-0 items-center gap-1.5">
                    {index > 0 && <ChevronRight size={15} className="shrink-0 text-[var(--hicas-text-muted)]" />}
                    {item.path && !isLast ? (
                      <Link
                        to={item.path}
                        className="truncate transition hover:text-[var(--hicas-orange-dark)]"
                      >
                        {item.label}
                      </Link>
                    ) : (
                      <span
                        className={cn(
                          "truncate",
                          isLast && "font-semibold text-[var(--hicas-text-main)]",
                        )}
                      >
                        {item.label}
                      </span>
                    )}
                  </span>
                );
              })}
            </nav>
          )}
        </div>

        <div className="flex min-w-0 items-center justify-end gap-2 sm:gap-3">
        <label className="relative hidden w-[390px] xl:block">
          <Search
            size={18}
            className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-[var(--hicas-text-muted)]"
          />
          <input
            className="hicas-input hicas-input-icon-left h-12 w-full rounded-xl text-[15px] font-semibold"
            placeholder="Tìm nhân viên hoặc chức năng"
            type="search"
          />
        </label>

        <div className="relative">
          <button
            type="button"
            onClick={() => {
              setNotificationOpen((value) => !value);
              setMenuOpen(false);
            }}
            className={cn(
              "inline-flex h-11 w-11 items-center justify-center rounded-xl border bg-white transition",
              notificationOpen
                ? "border-[var(--hicas-orange)] bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange)]"
                : "border-[var(--hicas-border)] text-[var(--hicas-text-secondary)] hover:border-[var(--hicas-orange)] hover:bg-[var(--hicas-orange-soft)] hover:text-[var(--hicas-orange)]",
            )}
            aria-label="Mở thông báo"
            aria-expanded={notificationOpen}
          >
            <Bell size={19} />
          </button>

          {notificationOpen && (
            <div className="absolute right-0 top-full z-50 mt-2 w-[320px] overflow-hidden rounded-2xl border border-[var(--hicas-border)] bg-white shadow-[var(--shadow-hover)]">
              <div className="flex items-start justify-between gap-3 border-b border-[var(--hicas-border-soft)] px-4 py-3">
                <div>
                  <p className="text-base font-bold text-[var(--hicas-text-main)]">
                    Thông báo
                  </p>
                  <p className="mt-1 text-[15px] font-medium text-[var(--hicas-text-secondary)]">
                    Các việc cần bạn chú ý sẽ xuất hiện tại đây.
                  </p>
                </div>
                <button
                  type="button"
                  onClick={() => setNotificationOpen(false)}
                  className="rounded-lg p-1 text-[var(--hicas-text-muted)] transition hover:bg-[var(--hicas-bg-soft)] hover:text-[var(--hicas-text-main)]"
                  aria-label="Đóng thông báo"
                >
                  <X size={17} />
                </button>
              </div>

              <div className="px-4 py-5 text-center">
                <div className="mx-auto flex h-11 w-11 items-center justify-center rounded-[var(--radius-md)] bg-[var(--hicas-bg-soft)] text-[var(--hicas-text-secondary)]">
                  <Bell size={20} />
                </div>
                <p className="mt-3 text-base font-bold text-[var(--hicas-text-main)]">
                  Chưa có thông báo mới
                </p>
                <p className="mt-1 text-[15px] font-medium leading-6 text-[var(--hicas-text-secondary)]">
                  Bạn chưa có cập nhật nào cần xem.
                </p>
              </div>

              {(canOpenApprovalInbox || canOpenApprovalTracking) && (
              <div className="grid gap-1 border-t border-[var(--hicas-border-soft)] p-2">
                {canOpenApprovalInbox && (
                <Link
                  to="/approvals"
                  onClick={() => setNotificationOpen(false)}
                  className="flex items-center gap-3 rounded-xl px-3 py-2.5 text-[15px] font-semibold text-[var(--hicas-text-main)] transition hover:bg-[var(--hicas-orange-lighter)] hover:text-[var(--hicas-orange)]"
                >
                  <ClipboardCheck size={17} />
                  Mở phê duyệt
                </Link>
                )}
                {canOpenApprovalTracking && (
                <Link
                  to="/approvals/tracking"
                  onClick={() => setNotificationOpen(false)}
                  className="flex items-center gap-3 rounded-xl px-3 py-2.5 text-[15px] font-semibold text-[var(--hicas-text-main)] transition hover:bg-[var(--hicas-orange-lighter)] hover:text-[var(--hicas-orange)]"
                >
                  <Bell size={17} />
                  Theo dõi yêu cầu
                </Link>
                )}
              </div>
              )}
            </div>
          )}
        </div>

        <div className="relative">
          <button
            type="button"
            onClick={() => {
              setMenuOpen((value) => !value);
              setNotificationOpen(false);
            }}
            className="flex h-12 items-center gap-3 rounded-2xl border border-[var(--hicas-border)] bg-white px-2.5 pr-3 transition hover:border-[var(--hicas-orange)] hover:bg-[var(--hicas-orange-lighter)]"
          >
            <img src={avatarUrl} alt={displayName} className="h-9 w-9 rounded-xl object-cover" />
            <div className="hidden min-w-0 text-left md:block">
              <p className="max-w-[170px] truncate text-[15px] font-bold text-[var(--hicas-text-main)]">
                {displayName}
              </p>
              <p className="max-w-[170px] truncate text-[15px] font-medium text-[var(--hicas-text-secondary)]">
                {displayRole}
              </p>
            </div>
            <ChevronDown
              size={17}
              className={cn(
                "hidden text-[var(--hicas-text-muted)] transition-transform md:block",
                menuOpen && "rotate-180",
              )}
            />
          </button>

          {menuOpen && (
            <div className="absolute right-0 top-full z-50 mt-2 w-60 overflow-hidden rounded-2xl border border-[var(--hicas-border)] bg-white shadow-[var(--shadow-hover)]">
              <div className="border-b border-[var(--hicas-border-soft)] px-4 py-3">
                <p className="truncate text-base font-bold text-[var(--hicas-text-main)]">
                  {displayName}
                </p>
                <p className="truncate text-[15px] font-medium text-[var(--hicas-text-secondary)]">
                  {displayRole}
                </p>
              </div>

              {canOpenProfile && (
              <Link
                to="/employee-contract/my-profile"
                onClick={() => setMenuOpen(false)}
                className="flex min-h-12 items-center gap-3 px-4 py-3 text-[15px] font-semibold text-[var(--hicas-text-main)] transition hover:bg-[var(--hicas-orange-lighter)] hover:text-[var(--hicas-orange)]"
              >
                <UserRound size={17} />
                Hồ sơ cá nhân
              </Link>
              )}
              <Link
                to="/account/security"
                onClick={() => setMenuOpen(false)}
                className="flex min-h-12 items-center gap-3 px-4 py-3 text-[15px] font-semibold text-[var(--hicas-text-main)] transition hover:bg-[var(--hicas-orange-lighter)] hover:text-[var(--hicas-orange)]"
              >
                <KeyRound size={17} />
                Bảo mật tài khoản
              </Link>
              <Link
                to="/mfa-setup"
                onClick={() => setMenuOpen(false)}
                className="flex min-h-12 items-center gap-3 px-4 py-3 text-[15px] font-semibold text-[var(--hicas-text-main)] transition hover:bg-[var(--hicas-orange-lighter)] hover:text-[var(--hicas-orange)]"
              >
                <ShieldCheck size={17} />
                Thiết lập MFA
              </Link>
              <button
                type="button"
                onClick={handleLogout}
                className="flex min-h-12 w-full items-center gap-3 border-t border-[var(--hicas-border-soft)] px-4 py-3 text-left text-[15px] font-bold text-[var(--hicas-danger)] transition hover:bg-[var(--hicas-danger-soft)]"
              >
                <LogOut size={17} />
                Đăng xuất
              </button>
            </div>
          )}
        </div>
      </div>
      </div>

    </header>
  );
};

const getFallbackTrail = (pathname: string) => {
  if (pathname === "/account/security") {
    return [
      { label: "Tài khoản", path: "/account/security", roles: [] },
      { label: "Bảo mật", path: "/account/security", roles: [] },
    ];
  }

  if (pathname === "/mfa-setup") {
    return [
      { label: "Tài khoản", path: "/account/security", roles: [] },
      { label: "MFA", path: "/mfa-setup", roles: [] },
    ];
  }

  return [];
};
