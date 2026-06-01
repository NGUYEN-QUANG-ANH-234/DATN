import {
  Bell,
  ChevronDown,
  KeyRound,
  LogOut,
  Menu,
  Search,
  ShieldCheck,
  UserRound,
} from "lucide-react";
import { useMemo, useState } from "react";
import { Link, useLocation } from "react-router-dom";
import { useAuth } from "../../core/auth/hooks/useAuth";
import { useCurrentUser } from "../../core/auth/hooks/useCurrentUser";
import { cn } from "../ui/classNames";
import { findMenuTrail, getHicasNavigation } from "./navigation";

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

  const navigation = useMemo(() => getHicasNavigation(), []);
  const trail = useMemo(
    () => findMenuTrail(navigation, location.pathname),
    [location.pathname, navigation],
  );

  const accountSecurityTrail =
    location.pathname === "/account/security"
      ? [
          { label: "Tài khoản", path: "/account/security", roles: [] },
          { label: "Bảo mật", path: "/account/security", roles: [] },
        ]
      : [];
  const activeTrail = accountSecurityTrail.length ? accountSecurityTrail : trail;
  const pageTitle = activeTrail.at(-1)?.label ?? "HICAS HR Portal";
  const breadcrumbs = activeTrail.length
    ? activeTrail
    : [{ label: "HICAS HR Portal", path: "/dashboard", roles: [] }];

  const displayName = user?.name?.trim() || "Người dùng";
  const displayRole = user?.role?.trim() || "Nhân viên";
  const avatarUrl =
    user?.avatar?.trim() ||
    `https://ui-avatars.com/api/?name=${encodeURIComponent(
      getInitials(displayName),
    )}&background=FF7A00&color=fff&bold=true`;

  const handleLogout = () => {
    void logout();
  };

  return (
    <header className="flex min-h-[var(--topbar-height)] shrink-0 items-center justify-between gap-3 border-b border-[var(--hicas-border)] bg-white px-4 py-3 sm:px-6">
      <div className="flex min-w-0 items-center gap-3 sm:gap-4">
        <button
          type="button"
          onClick={onToggleSidebar}
          className="inline-flex h-11 w-11 items-center justify-center rounded-xl border border-[var(--hicas-border)] text-[var(--hicas-text-secondary)] transition hover:border-[var(--hicas-orange)] hover:bg-[var(--hicas-orange-soft)] hover:text-[var(--hicas-orange)] lg:hidden"
          aria-label="Mở menu"
        >
          <Menu size={19} />
        </button>

        <div className="min-w-0">
          <h2 className="truncate text-lg font-bold text-[var(--hicas-text-main)] sm:text-xl">
            {pageTitle}
          </h2>
          <div className="mt-1 hidden min-w-0 items-center gap-2 text-xs text-[var(--hicas-text-secondary)] sm:flex">
            {breadcrumbs.map((item, index) => {
              const isLast = index === breadcrumbs.length - 1;

              return (
                <span key={`${item.label}-${index}`} className="flex min-w-0 items-center gap-2">
                  {index > 0 && <span className="text-[var(--hicas-text-muted)]">/</span>}
                  {item.path && !isLast ? (
                    <Link
                      to={item.path}
                      className="truncate transition hover:text-[var(--hicas-orange)]"
                    >
                      {item.label}
                    </Link>
                  ) : (
                    <span
                      className={cn(
                        "truncate",
                        isLast && "font-medium text-[var(--hicas-text-main)]",
                      )}
                    >
                      {item.label}
                    </span>
                  )}
                </span>
              );
            })}
          </div>
        </div>
      </div>

      <div className="flex min-w-0 items-center gap-2 sm:gap-3">
        <label className="relative hidden min-w-[260px] lg:block">
          <Search
            size={18}
            className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-[var(--hicas-text-muted)]"
          />
          <input
            className="hicas-input hicas-input-icon-left h-11 w-full rounded-xl"
            placeholder="Tìm nhân viên, module, báo cáo..."
            type="search"
          />
        </label>

        <button
          type="button"
          className="relative inline-flex h-11 w-11 items-center justify-center rounded-xl border border-[var(--hicas-border)] bg-white text-[var(--hicas-text-secondary)] transition hover:border-[var(--hicas-orange)] hover:bg-[var(--hicas-orange-soft)] hover:text-[var(--hicas-orange)]"
          aria-label="Thông báo"
        >
          <Bell size={19} />
          <span className="absolute right-2 top-2 h-2.5 w-2.5 rounded-full border-2 border-white bg-[var(--hicas-orange)]" />
        </button>

        <div className="relative">
          <button
            type="button"
            onClick={() => setMenuOpen((value) => !value)}
            className="flex h-12 items-center gap-3 rounded-2xl border border-[var(--hicas-border)] bg-white px-2.5 pr-3 transition hover:border-[var(--hicas-orange)] hover:bg-[var(--hicas-orange-lighter)]"
          >
            <img src={avatarUrl} alt={displayName} className="h-9 w-9 rounded-xl object-cover" />
            <div className="hidden min-w-0 text-left md:block">
              <p className="max-w-[160px] truncate text-sm font-semibold text-[var(--hicas-text-main)]">
                {displayName}
              </p>
              <p className="max-w-[160px] truncate text-xs text-[var(--hicas-text-secondary)]">
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
                <p className="truncate text-sm font-semibold text-[var(--hicas-text-main)]">
                  {displayName}
                </p>
                <p className="truncate text-xs text-[var(--hicas-text-secondary)]">
                  {displayRole}
                </p>
              </div>

              <Link
                to="/employee-contract/my-profile"
                onClick={() => setMenuOpen(false)}
                className="flex min-h-11 items-center gap-3 px-4 py-3 text-sm font-medium text-[var(--hicas-text-main)] transition hover:bg-[var(--hicas-orange-lighter)] hover:text-[var(--hicas-orange)]"
              >
                <UserRound size={17} />
                Hồ sơ cá nhân
              </Link>
              <Link
                to="/account/security"
                onClick={() => setMenuOpen(false)}
                className="flex min-h-11 items-center gap-3 px-4 py-3 text-sm font-medium text-[var(--hicas-text-main)] transition hover:bg-[var(--hicas-orange-lighter)] hover:text-[var(--hicas-orange)]"
              >
                <KeyRound size={17} />
                Đổi mật khẩu
              </Link>
              <Link
                to="/mfa-setup"
                onClick={() => setMenuOpen(false)}
                className="flex min-h-11 items-center gap-3 px-4 py-3 text-sm font-medium text-[var(--hicas-text-main)] transition hover:bg-[var(--hicas-orange-lighter)] hover:text-[var(--hicas-orange)]"
              >
                <ShieldCheck size={17} />
                Thiết lập MFA
              </Link>
              <button
                type="button"
                onClick={handleLogout}
                className="flex min-h-11 w-full items-center gap-3 border-t border-[var(--hicas-border-soft)] px-4 py-3 text-left text-sm font-semibold text-[var(--hicas-danger)] transition hover:bg-[var(--hicas-danger-soft)]"
              >
                <LogOut size={17} />
                Đăng xuất
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};
