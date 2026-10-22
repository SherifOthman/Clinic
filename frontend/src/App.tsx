import { useEffect, useState, type ReactNode } from "react";
import {
  BrowserRouter,
  Navigate,
  Route,
  Routes,
} from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import { Loader2 } from "lucide-react";
import { queryClient } from "@/lib/queryClient";
import { useAuthStore } from "@/features/auth/auth-store";
import { authApi } from "@/features/auth/api";
import AppLayout from "@/components/layout/AppLayout";
import LoginPage from "@/features/auth/LoginPage";
import Dashboard from "@/pages/Dashboard";
import Placeholder from "@/pages/Placeholder";

/** Verifies a stored JWT on load via /api/auth/me so reloads keep a valid session and
 *  drop an expired one before the app renders. */
function Bootstrap({ children }: { children: ReactNode }) {
  const token = useAuthStore((s) => s.token);
  const setSession = useAuthStore((s) => s.setSession);
  const clear = useAuthStore((s) => s.clear);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    let active = true;
    (async () => {
      if (!token) {
        setReady(true);
        return;
      }
      try {
        const res = await authApi.me();
        if (active) setSession(res.token, res.user);
      } catch {
        if (active) clear();
      } finally {
        if (active) setReady(true);
      }
    })();
    return () => {
      active = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (!ready) {
    return (
      <div className="flex min-h-svh items-center justify-center">
        <Loader2 className="text-muted-foreground size-6 animate-spin" />
      </div>
    );
  }
  return <>{children}</>;
}

function RequireAuth({ children }: { children: ReactNode }) {
  const token = useAuthStore((s) => s.token);
  if (!token) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

function RedirectIfAuthed({ children }: { children: ReactNode }) {
  const token = useAuthStore((s) => s.token);
  if (token) return <Navigate to="/" replace />;
  return <>{children}</>;
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Bootstrap>
          <Routes>
            <Route
              path="/login"
              element={
                <RedirectIfAuthed>
                  <LoginPage />
                </RedirectIfAuthed>
              }
            />
            <Route
              element={
                <RequireAuth>
                  <AppLayout />
                </RequireAuth>
              }
            >
              <Route index element={<Dashboard />} />
              <Route path="patients" element={<Placeholder title="Patients" />} />
              <Route path="appointments" element={<Placeholder title="Appointments" />} />
              <Route path="queue" element={<Placeholder title="Queue" />} />
              <Route path="consultations" element={<Placeholder title="Consultations" />} />
              <Route path="billing" element={<Placeholder title="Billing" />} />
              <Route path="users" element={<Placeholder title="Users" />} />
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Bootstrap>
      </BrowserRouter>
    </QueryClientProvider>
  );
}
