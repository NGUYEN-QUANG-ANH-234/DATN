const trimTrailingSlash = (value: string) => value.replace(/\/+$/, "");

const defaultBackendUrl = import.meta.env.DEV
  ? "https://localhost:7003"
  : window.location.origin;

export const BACKEND_URL = trimTrailingSlash(
  import.meta.env.VITE_BACKEND_URL || defaultBackendUrl,
);

export const API_BASE_URL = trimTrailingSlash(
  import.meta.env.VITE_API_BASE_URL || `${BACKEND_URL}/api/v1`,
);
