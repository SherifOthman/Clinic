import { useState } from "react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  CalendarClock,
  LayoutDashboard,
  ListChecks,
  LogOut,
  Menu,
  Receipt,
  Stethoscope,
  UserRound,
  Users,
  X,
} from "lucide-react";
import { useAuthStore } from "@/features/auth/auth-store";
import { LanguageSelector } from "@/components/common/LanguageSelector";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface NavItem {
  to: string;
  labelKey: string;
  icon: typeof LayoutDashboard;
}

const NAV_ITEMS: NavItem[] = [
  { to: "/", labelKey: "nav.dashboard", icon: LayoutDashboard },
  { to: "/patients", labelKey: "nav.patients", icon: UserRound },
  { to: "/appointments", labelKey: "nav.appointments", icon: CalendarClock },
  { to: "/queue", labelKey: "nav.queue", icon: ListChecks },
  { to: "/consultations", labelKey: "nav.consultations", icon: Stethoscope },
  { to: "/billing", labelKey: "nav.billing", icon: Receipt },
  { to: "/users", labelKey: "nav.users", icon: Users },
];

export default function AppLayout() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const clear = useAuthStore((s) => s.clear);
  const [mobileOpen, setMobileOpen] = useState(false);

  const logout = () => {
    clear();
    navigate("/login", { replace: true });
  };

  const SidebarContent = (
    <div className="flex h-full flex-col">
      <div className="border-b px-5 py-4">
        <span className="text-lg font-semibold">{t("app.name")}</span>
        <p className="text-muted-foreground text-xs">{t("app.tagline")}</p>
      </div>
      <nav className="flex-1 space-y-1 p-3">
        {NAV_ITEMS.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === "/"}
            onClick={() => setMobileOpen(false)}
            className={({ isActive }) =>
              cn(
                "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                isActive
                  ? "bg-primary text-primary-foreground"
                  : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
              )
            }
          >
            <item.icon className="size-4" />
            {t(item.labelKey)}
          </NavLink>
        ))}
      </nav>
    </div>
  );

  return (
    <div className="flex min-h-svh">
      {/* Desktop sidebar */}
      <aside className="bg-sidebar text-sidebar-foreground border-sidebar-border hidden w-64 shrink-0 border-e md:block">
        {SidebarContent}
      </aside>

      {/* Mobile sidebar */}
      {mobileOpen && (
        <div className="fixed inset-0 z-40 md:hidden">
          <div className="bg-black/40 absolute inset-0" onClick={() => setMobileOpen(false)} />
          <aside className="bg-sidebar text-sidebar-foreground border-sidebar-border absolute inset-y-0 start-0 w-64 border-e">
            <button
              className="absolute end-2 top-2 rounded-md p-1 hover:bg-accent"
              onClick={() => setMobileOpen(false)}
              aria-label={t("common.close")}
            >
              <X className="size-5" />
            </button>
            {SidebarContent}
          </aside>
        </div>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="border-b bg-background">
          <div className="flex h-14 items-center gap-3 px-4">
            <Button variant="ghost" size="icon" className="md:hidden" onClick={() => setMobileOpen(true)}>
              <Menu />
            </Button>
            <div className="me-auto" />
            <LanguageSelector />
            <div className="text-sm">
              <span className="text-muted-foreground">{user?.fullName}</span>
            </div>
            <Button variant="ghost" size="sm" onClick={logout}>
              <LogOut />
              <span className="hidden sm:inline">{t("auth.logout")}</span>
            </Button>
          </div>
        </header>
        <main className="flex-1 overflow-auto p-4 md:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
