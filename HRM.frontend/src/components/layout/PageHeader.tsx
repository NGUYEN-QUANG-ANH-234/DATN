import type { ReactNode } from "react";

export type BreadcrumbItem = {
  label: string;
  href?: string;
};

export type PageHeaderProps = {
  title: string;
  description?: string;
  breadcrumb?: BreadcrumbItem[];
  actions?: ReactNode;
};

export const PageHeader = ({
  title,
  description,
  breadcrumb,
  actions,
}: PageHeaderProps) => (
  <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
    <div>
      {breadcrumb && breadcrumb.length > 0 && (
        <nav className="mb-2 flex flex-wrap items-center gap-2 text-xs font-medium text-[var(--hicas-text-secondary)]">
          {breadcrumb.map((item, index) => (
            <span key={`${item.label}-${index}`} className="flex items-center gap-2">
              {index > 0 && <span>/</span>}
              {item.href ? (
                <a className="hover:text-[var(--hicas-orange-dark)]" href={item.href}>
                  {item.label}
                </a>
              ) : (
                <span>{item.label}</span>
              )}
            </span>
          ))}
        </nav>
      )}
      <h1 className="text-2xl font-bold text-[var(--hicas-text-main)]">{title}</h1>
      {description && (
        <p className="mt-1 max-w-3xl text-sm leading-6 text-[var(--hicas-text-secondary)]">
          {description}
        </p>
      )}
    </div>
    {actions && <div className="shrink-0">{actions}</div>}
  </div>
);
