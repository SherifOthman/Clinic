import { api } from "@/lib/api";
import type { AuthResult } from "./types";

export const authApi = {
  login: (username: string, password: string) =>
    api.post<AuthResult>("/api/auth/login", { username, password }),
  me: () => api.get<AuthResult>("/api/auth/me"),
};
