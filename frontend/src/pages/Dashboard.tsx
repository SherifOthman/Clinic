import { useTranslation } from "react-i18next";
import { useAuthStore } from "@/features/auth/auth-store";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export default function Dashboard() {
  const { t } = useTranslation();
  const user = useAuthStore((s) => s.user);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">{t("nav.dashboard")}</h1>
        <p className="text-muted-foreground">
          {t("auth.welcomeBack")}, {user?.fullName} ({user?.roles.join(", ")})
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {[
          { label: t("nav.patients"), value: "—" },
          { label: t("nav.appointments"), value: "—" },
          { label: t("nav.consultations"), value: "—" },
          { label: t("nav.billing"), value: "—" },
        ].map((s) => (
          <Card key={s.label}>
            <CardHeader>
              <CardDescription>{s.label}</CardDescription>
              <CardTitle className="text-3xl">{s.value}</CardTitle>
            </CardHeader>
            <CardContent />
          </Card>
        ))}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t("app.name")}</CardTitle>
          <CardDescription>Foundation milestone — core features arrive one at a time.</CardDescription>
        </CardHeader>
        <CardContent className="text-muted-foreground text-sm">
          The architecture, auth, i18n (English + Arabic / RTL), and UI foundation are in place.
          See <code>docs/ROADMAP.md</code> for the feature-by-feature plan.
        </CardContent>
      </Card>
    </div>
  );
}
