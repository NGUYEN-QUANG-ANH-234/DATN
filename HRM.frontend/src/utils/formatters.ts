const viDateTimeFormat = new Intl.DateTimeFormat("vi-VN", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

const viDateFormat = new Intl.DateTimeFormat("vi-VN", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
});

const viNumberFormat = (digits = 2) =>
  new Intl.NumberFormat("vi-VN", {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits,
  });

export const formatCurrencyVnd = (value?: number | null) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(value || 0);

export const formatMoney = formatCurrencyVnd;

export const formatOptionalMoney = (value?: number | null, fallback = "-") =>
  value === null || value === undefined ? fallback : formatCurrencyVnd(value);

export const formatNumber = (value?: number | null, digits = 2) =>
  viNumberFormat(digits).format(value || 0);

export const formatPercent = (value?: number | null, digits = 2) =>
  `${formatNumber(value, digits)}%`;

const toValidDate = (value?: string | Date | null) => {
  if (!value) return null;
  const date = value instanceof Date ? value : new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
};

export const formatDate = (value?: string | Date | null, fallback = "-") => {
  const date = toValidDate(value);
  return date ? viDateFormat.format(date) : fallback;
};

export const formatDateTime = (value?: string | Date | null, fallback = "-") => {
  const date = toValidDate(value);
  return date ? viDateTimeFormat.format(date) : fallback;
};

export const formatMonthPeriod = (month: number, year: number) =>
  `${String(month).padStart(2, "0")}/${year}`;

export const formatBoolean = (value?: boolean | null, trueLabel = "Có", falseLabel = "Không") =>
  value ? trueLabel : falseLabel;

export const formatNullable = (value?: string | number | null, fallback = "-") =>
  value === null || value === undefined || value === "" ? fallback : String(value);

export const formatMinutesAsHours = (minutes?: number | null, digits = 2) =>
  `${formatNumber((minutes || 0) / 60, digits)} giờ`;

export const formatBackendEnumLabel = (value?: string | null, fallback = "Không xác định") => {
  if (!value) return fallback;
  return value
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/[_-]+/g, " ")
    .trim();
};
