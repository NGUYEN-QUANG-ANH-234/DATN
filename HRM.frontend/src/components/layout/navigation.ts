import type { MenuItem } from "../../core/types/menu";
import { MENU_ITEMS } from "../../core/types/menu";
import { hasAnyRole } from "../../core/auth/roleAccess";

export const getHicasNavigation = (): MenuItem[] => MENU_ITEMS;

export const filterMenuByRole = (items: MenuItem[], role?: string | null) =>
  items.reduce<MenuItem[]>((result, item) => {
    const children = item.children ? filterMenuByRole(item.children, role) : undefined;
    const canViewSelf = hasAnyRole(item.roles, role);

    if (!canViewSelf && !children?.length) {
      return result;
    }

    result.push({
      ...item,
      children,
    });

    return result;
  }, []);

export const isActiveRoute = (pathname: string, targetPath?: string) => {
  if (!targetPath) return false;
  if (targetPath === "/") return pathname === targetPath;
  return pathname === targetPath || pathname.startsWith(`${targetPath}/`);
};

export const findMenuTrail = (items: MenuItem[], pathname: string) => {
  let bestTrail: MenuItem[] = [];
  let bestLength = -1;

  const visit = (itemList: MenuItem[], parents: MenuItem[]) => {
    itemList.forEach((item) => {
      const trail = [...parents, item];

      if (isActiveRoute(pathname, item.path) && item.path && item.path.length > bestLength) {
        bestTrail = trail;
        bestLength = item.path.length;
      }

      if (item.children?.length) {
        visit(item.children, trail);
      }
    });
  };

  visit(items, []);
  return bestTrail;
};
