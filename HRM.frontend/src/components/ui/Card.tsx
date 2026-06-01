import type { HTMLAttributes, ReactNode } from "react";
import { cn } from "./classNames";

export type CardProps = HTMLAttributes<HTMLElement> & {
  title?: ReactNode;
  description?: ReactNode;
  actions?: ReactNode;
  padded?: boolean;
  hoverable?: boolean;
};

export const Card = ({
  title,
  description,
  actions,
  padded = true,
  hoverable = false,
  className,
  children,
  ...props
}: CardProps) => (
  <section
    className={cn(
      "hicas-card",
      padded && "hicas-card-padded",
      hoverable && "hicas-card-hover",
      className,
    )}
    {...props}
  >
    {(title || description || actions) && (
      <div className="mb-5 flex flex-col gap-3 border-b border-[var(--hicas-border-soft)] pb-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          {title && <h2 className="text-lg font-semibold text-[var(--hicas-text-main)]">{title}</h2>}
          {description && (
            <p className="mt-1 text-sm leading-6 text-[var(--hicas-text-secondary)]">
              {description}
            </p>
          )}
        </div>
        {actions && <div className="shrink-0">{actions}</div>}
      </div>
    )}
    {children}
  </section>
);
