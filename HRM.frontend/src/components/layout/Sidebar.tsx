import {
  BriefcaseBusiness,
  CalendarClock,
  CheckCircle2,
  ChevronDown,
  ClipboardList,
  FileUser,
  FolderKanban,
  GraduationCap,
  LayoutDashboard,
  Menu,
  Network,
  PanelLeftClose,
  Settings,
  WalletCards,
  type LucideIcon,
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { Link, useLocation } from "react-router-dom";
import hicasLogo from "../../assets/images/hicas-logo.jpg";
import { useCurrentUser } from "../../core/auth/hooks/useCurrentUser";
import type { MenuItem } from "../../core/types/menu";
import { cn } from "../ui/classNames";
import { filterMenuByRole, getHicasNavigation, isActiveRoute } from "./navigation";

interface SidebarProps {
  collapsed: boolean;
  mobileOpen?: boolean;
  onToggle: () => void;
  onNavigate?: () => void;
}

const getMenuIcon = (item: MenuItem): LucideIcon => {
  switch (item.icon) {
    case "dashboard":
      return LayoutDashboard;
    case "settings":
      return Settings;
    case "access":
      return CheckCircle2;
    case "recruitment":
      return BriefcaseBusiness;
    case "profile":
      return FileUser;
    case "attendance":
      return CalendarClock;
    case "training":
      return GraduationCap;
    case "payroll":
      return WalletCards;
    case "personnel":
      return Network;
    case "forms":
    case "approvals":
      return ClipboardList;
    default:
      break;
  }

  const label = item.label;
  if (label.includes("Tổng quan")) return LayoutDashboard;
  if (label.includes("Cấu hình")) return Settings;
  if (label.includes("Quản trị")) return CheckCircle2;
  if (label.includes("Tuyển dụng")) return BriefcaseBusiness;
  if (label.includes("Hồ sơ")) return FileUser;
  if (label.includes("Chấm công")) return CalendarClock;
  if (label.includes("Hiệu suất")) return GraduationCap;
  if (label.includes("Lương")) return WalletCards;
  if (label.includes("Biến động")) return Network;
  if (label.includes("Biểu mẫu")) return ClipboardList;
  if (label.includes("Phê duyệt")) return ClipboardList;
  return FolderKanban;
};

const hasActiveChild = (item: MenuItem, pathname: string) =>
  Boolean(item.children?.some((child) => isActiveRoute(pathname, child.path)));

const isItemActive = (item: MenuItem, pathname: string) =>
  isActiveRoute(pathname, item.path) || hasActiveChild(item, pathname);

export const Sidebar = ({
  collapsed,
  mobileOpen = false,
  onToggle,
  onNavigate,
}: SidebarProps) => {
  const location = useLocation();
  const { user } = useCurrentUser();
  const navigation = useMemo(() => getHicasNavigation(), []);
  const visibleNavigation = useMemo(
    () => filterMenuByRole(navigation, user?.role),
    [navigation, user?.role],
  );
  const [openGroups, setOpenGroups] = useState<Record<string, boolean>>({});

  useEffect(() => {
    setOpenGroups((current) => {
      let changed = false;
      const next = { ...current };

      visibleNavigation.forEach((item) => {
        if (item.children?.length && isItemActive(item, location.pathname) && !next[item.label]) {
          next[item.label] = true;
          changed = true;
        }
      });

      return changed ? next : current;
    });
  }, [location.pathname, visibleNavigation]);

  const toggleGroup = (label: string) => {
    setOpenGroups((current) => ({
      ...current,
      [label]: !current[label],
    }));
  };
  const compact = collapsed && !mobileOpen;

  return (
    <aside
      className={cn(
        "hicas-sidebar fixed inset-y-0 left-0 z-50 flex shrink-0 flex-col transition-[transform,width] duration-200 md:static md:z-auto md:translate-x-0",
        mobileOpen ? "translate-x-0" : "-translate-x-full",
      )}
      style={{
        width: compact ? "var(--sidebar-collapsed-width)" : "var(--sidebar-width)",
      }}
    >
      <div className="flex h-[88px] items-center gap-3 px-5">
        <div className="flex h-11 w-11 shrink-0 items-center justify-center overflow-hidden rounded-xl border border-white/10 bg-white">
          <img src={hicasLogo} alt="HICAS" className="h-full w-full object-contain" />
        </div>

        {!compact && (
          <div className="min-w-0">
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-[var(--hicas-orange)]">
              HICAS
            </p>
            <h1 className="truncate text-lg font-bold text-white">Nhân sự</h1>
          </div>
        )}

        <button
          type="button"
          onClick={onToggle}
          className="ml-auto inline-flex h-10 w-10 items-center justify-center rounded-lg text-white/70 transition hover:bg-white/10 hover:text-white"
          aria-label={compact ? "Mở rộng menu" : "Thu gọn menu"}
        >
          {compact ? <Menu size={19} /> : <PanelLeftClose size={19} />}
        </button>
      </div>

      <nav className="scrollbar-sidebar flex-1 overflow-y-auto px-4 pb-4">
        <div className="space-y-1.5">
          {visibleNavigation.map((item) => {
            const Icon = getMenuIcon(item);
            const active = isItemActive(item, location.pathname);
            const expanded = Boolean(openGroups[item.label]);
            const hasChildren = Boolean(item.children?.length);

            if (!hasChildren && item.path) {
              return (
                <Link
                  key={item.path}
                  to={item.path}
                  onClick={onNavigate}
                  title={compact ? item.label : undefined}
                  className={cn(
                    "hicas-sidebar-item",
                    compact && "justify-center px-0",
                    active && "hicas-sidebar-item-active",
                  )}
                >
                  <Icon size={20} strokeWidth={1.9} />
                  {!compact && <span className="truncate">{item.label}</span>}
                </Link>
              );
            }

            return (
              <div key={item.label} className="space-y-1">
                <button
                  type="button"
                  title={compact ? item.label : undefined}
                  onClick={() => toggleGroup(item.label)}
                  className={cn(
                    "hicas-sidebar-item w-full",
                    compact && "justify-center px-0",
                    active && "hicas-sidebar-item-active",
                  )}
                >
                  <Icon size={20} strokeWidth={1.9} />
                  {!compact && (
                    <>
                      <span className="min-w-0 flex-1 truncate text-left">{item.label}</span>
                      <ChevronDown
                        size={17}
                        className={cn("transition-transform", expanded && "rotate-180")}
                      />
                    </>
                  )}
                </button>

                {!compact && expanded && (
                  <div className="ml-5 space-y-1 border-l border-white/10 pl-3">
                    {item.children?.map((child) => {
                      const childActive = isActiveRoute(location.pathname, child.path);

                      return (
                        <Link
                          key={child.path ?? child.label}
                          to={child.path ?? "#"}
                          onClick={onNavigate}
                          className={cn(
                            "flex min-h-10 items-center rounded-lg px-3 py-2 text-sm font-medium text-white/70 transition hover:bg-white/10 hover:text-white",
                            childActive &&
                              "bg-[rgba(255,122,0,0.14)] text-[var(--hicas-orange-hover)]",
                          )}
                        >
                          <span className="truncate">{child.label}</span>
                        </Link>
                      );
                    })}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </nav>

    </aside>
  );
};
