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
  "w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-800 outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-100 disabled:cursor-not-allowed disabled:bg-gray-100 disabled:text-gray-500";

export const textareaClass = `${fieldClass} min-h-24 resize-y`;

export const primaryButtonClass =
  "inline-flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60";

export const secondaryButtonClass =
  "inline-flex items-center justify-center gap-2 rounded-lg border border-gray-300 bg-white px-4 py-2 text-sm font-semibold text-gray-700 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60";

export const dangerButtonClass =
  "inline-flex items-center justify-center gap-2 rounded-lg border border-red-200 bg-red-50 px-4 py-2 text-sm font-semibold text-red-700 transition hover:bg-red-100 disabled:cursor-not-allowed disabled:opacity-60";

export const FeaturePage = ({
  title,
  description,
  actions,
  children,
  width = "wide",
}: FeaturePageProps) => {
  const maxWidth = width === "wide" ? "max-w-6xl" : "max-w-4xl";

  return (
    <div className="min-h-full bg-gray-50 px-4 py-6 sm:px-6">
      <div className={`mx-auto w-full ${maxWidth} space-y-6`}>
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">{title}</h1>
            {description && (
              <p className="mt-1 max-w-3xl text-sm leading-6 text-gray-500">
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
    className={`rounded-lg border border-gray-200 bg-white p-5 shadow-sm ${className}`}
  >
    {(title || description || actions) && (
      <div className="mb-5 flex flex-col gap-3 border-b border-gray-100 pb-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          {title && <h2 className="text-lg font-semibold text-gray-900">{title}</h2>}
          {description && (
            <p className="mt-1 text-sm leading-6 text-gray-500">{description}</p>
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
    <p className="font-medium text-gray-700">{title}</p>
    {description && <p className="mt-1 text-sm text-gray-500">{description}</p>}
  </div>
);
