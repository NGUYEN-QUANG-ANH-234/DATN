import { matchPath } from "react-router-dom";
import {
  APP_ROLES,
  ROLE_GROUPS,
  hasAnyRole,
  normalizeRole,
  type AppRole,
  type RoleList,
} from "../core/auth/roleAccess";
import { MENU_ITEMS, type MenuItem } from "../core/types/menu";

export type AppRouteMeta = {
  path: string;
  label: string;
  module: string;
  roles: RoleList;
  hidden?: boolean;
};

const flattenMenuRoutes = (items: MenuItem[], parentModule?: string): AppRouteMeta[] =>
  items.flatMap((item) => {
    const module = item.module || parentModule || item.label;
    const current = item.path
      ? [
          {
            path: item.path,
            label: item.label,
            module,
            roles: item.roles,
          },
        ]
      : [];

    return [...current, ...flattenMenuRoutes(item.children || [], module)];
  });

const hiddenRoutes: AppRouteMeta[] = [
  {
    path: "/mfa-setup",
    label: "Thiết lập MFA",
    module: "Tài khoản",
    roles: APP_ROLES,
    hidden: true,
  },
  {
    path: "/account/security",
    label: "Bảo mật tài khoản",
    module: "Tài khoản",
    roles: APP_ROLES,
    hidden: true,
  },
  {
    path: "/recruitment/demands/create",
    label: "Tạo nhu cầu tuyển dụng",
    module: "Tuyển dụng",
    roles: ROLE_GROUPS.recruitmentDemandCreators,
    hidden: true,
  },
  {
    path: "/employee-contract/profile-setup/:candidateId",
    label: "Thiết lập hồ sơ",
    module: "Hồ sơ & hợp đồng",
    roles: ROLE_GROUPS.employeeAdminDirector,
    hidden: true,
  },
  {
    path: "/employees/onboarding/:candidateId",
    label: "Thiết lập hồ sơ",
    module: "Hồ sơ & hợp đồng",
    roles: ROLE_GROUPS.employeeAdminDirector,
    hidden: true,
  },
];

export const appRoutes: AppRouteMeta[] = [...flattenMenuRoutes(MENU_ITEMS), ...hiddenRoutes];

export const findRouteMeta = (pathname: string) =>
  [...appRoutes]
    .sort((left, right) => right.path.length - left.path.length)
    .find((route) => matchPath({ path: route.path, end: true }, pathname));

export const canAccessRoute = (route: AppRouteMeta, role?: string | null) =>
  hasAnyRole(route.roles, role);

export const canAccessPath = (
  pathname: string,
  role?: string | null,
  allowUnknownRoute = true,
) => {
  const route = findRouteMeta(pathname);
  if (!route) return allowUnknownRoute;

  return canAccessRoute(route, role);
};

export const getFirstAccessiblePath = (
  role: string | null | undefined,
  candidates: string[],
  fallback = "/dashboard",
) => candidates.find((path) => canAccessPath(path, role, false)) || fallback;

export const getAccessibleRoutesByRole = (role: AppRole | string) => {
  const normalized = normalizeRole(role);
  if (!normalized) return [];

  const seen = new Set<string>();

  return appRoutes.filter((route) => {
    if (route.hidden || seen.has(route.path) || !canAccessRoute(route, normalized)) {
      return false;
    }

    seen.add(route.path);
    return true;
  });
};
