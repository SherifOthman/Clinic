import i18n from "@/i18n";

const TOKEN_KEY = "clinic.token";

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string | null): void {
  if (token) localStorage.setItem(TOKEN_KEY, token);
  else localStorage.removeItem(TOKEN_KEY);
}

/** Typed error thrown for non-2xx responses, carrying status + parsed body. */
export class ApiError extends Error {
  status: number;
  data: unknown;
  constructor(status: number, data: unknown, message?: string) {
    super(message ?? `Request failed (${status})`);
    this.status = status;
    this.data = data;
  }
}

function parse(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = { Accept: "application/json" };
  if (body !== undefined) headers["Content-Type"] = "application/json";
  const token = getToken();
  if (token) headers["Authorization"] = `Bearer ${token}`;
  headers["Accept-Language"] = i18n.language || "en";

  const res = await fetch(path, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
    credentials: "same-origin",
  });

  const text = await res.text();
  const data = text ? parse(text) : null;
  if (!res.ok) {
    const msg =
      data && typeof data === "object" && "message" in data && typeof (data as { message: unknown }).message === "string"
        ? (data as { message: string }).message
        : undefined;
    throw new ApiError(res.status, data, msg);
  }
  return data as T;
}

/** Centralized, type-safe HTTP client. Relative paths rely on the Vite proxy in dev
 *  and same-origin serving in the single-deploy build. */
export const api = {
  get: <T>(path: string) => request<T>("GET", path),
  post: <T>(path: string, body?: unknown) => request<T>("POST", path, body),
  put: <T>(path: string, body?: unknown) => request<T>("PUT", path, body),
  patch: <T>(path: string, body?: unknown) => request<T>("PATCH", path, body),
  del: <T>(path: string) => request<T>("DELETE", path),
};
