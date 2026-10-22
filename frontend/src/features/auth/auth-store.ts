import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { UserBrief } from "./types";
import { setToken } from "@/lib/api";

interface AuthState {
  token: string | null;
  user: UserBrief | null;
  setSession: (token: string, user: UserBrief) => void;
  clear: () => void;
}

/**
 * Auth session. The JWT is mirrored to localStorage by the API client (for the
 * Authorization header) and the user object is persisted here so a reload keeps
 * the signed-in state. The token is re-verified via /api/auth/me on app load.
 */
export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      user: null,
      setSession: (token, user) => {
        setToken(token);
        set({ token, user });
      },
      clear: () => {
        setToken(null);
        set({ token: null, user: null });
      },
    }),
    { name: "clinic.auth", partialize: (s) => ({ token: s.token, user: s.user }) }
  )
);
