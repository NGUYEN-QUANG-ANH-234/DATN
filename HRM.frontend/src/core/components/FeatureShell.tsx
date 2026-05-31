import type { ReactNode } from "react";

type FeaturePageProps = {
  title: string;
  description?: string;
  actions?: ReactNode;
  children: ReactNode;
  width?: "normal" | "wide";
};

type FeatureCardProps = {
  title?: string;
  description?: string;
  actions?: ReactNode;
  children: ReactNode;
  className?: string;
};

export const fieldClass =
  "hicas-input w-full disabled:cursor-not-allowed disabled:bg-[var(--hicas-bg-soft)] disabled:text-[var(--hicas-text-muted)]";

export const textareaClass = `${fieldClass} min-h-24 resize-y`;

export const primaryButtonClass =
  "hicas-btn-primary inline-flex min-h-[42px] items-center justify-center gap-2 px-[18px] text-sm disabled:cursor-not-allowed disabled:opacity-60";

export const secondaryButtonClass =
  "hicas-btn-secondary inline-flex min-h-[42px] items-center justify-center gap-2 px-[18px] text-sm disabled:cursor-not-allowed disabled:opacity-60";

export const dangerButtonClass =
  "inline-flex min-h-[42px] items-center justify-center gap-2 rounded-[var(--radius-md)] border border-[var(--hicas-danger)] bg-[var(--hicas-danger-soft)] px-[18px] text-sm font-semibold text-[var(--hicas-danger)] transition hover:bg-red-100 disabled:cursor-not-allowed disabled:opacity-60";

export const FeaturePage = ({
  title,
  description,
  actions,
  children,
  width = "wide",
}: FeaturePageProps) => {
  const maxWidth = width === "wide" ? "max-w-none" : "max-w-5xl";

  return (
    <div className="min-h-full">
      <div className={`mx-auto w-full ${maxWidth} space-y-6`}>
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h1 className="text-2xl font-bold text-[var(--hicas-text-main)]">{title}</h1>
            {description && (
              <p className="mt-1 max-w-3xl text-sm leading-6 text-[var(--hicas-text-secondary)]">
                {description}
              </p>
            )}
          </div>
          {actions && <div className="shrink-0">{actions}</div>}
        </div>
        {children}
      </div>
    </div>
  );
};

export const FeatureCard = ({
  title,
  description,
  actions,
  children,
  className = "",
}: FeatureCardProps) => (
  <section
    className={`hicas-card hicas-card-padded ${className}`}
  >
    {(title || description || actions) && (
      <div className="mb-5 flex flex-col gap-3 border-b border-[var(--hicas-border-soft)] pb-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          {title && (
            <h2 className="text-lg font-semibold text-[var(--hicas-text-main)]">{title}</h2>
          )}
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

export const EmptyState = ({
  title,
  description,
}: {
  title: string;
  description?: string;
}) => (
  <div className="rounded-lg border border-dashed border-gray-300 bg-gray-50 px-6 py-10 text-center">
    <p className="font-medium text-[var(--hicas-text-main)]">{title}</p>
    {description && (
      <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">{description}</p>
    )}
  </div>
);
