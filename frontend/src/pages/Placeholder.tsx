import { useTranslation } from "react-i18next";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export default function Placeholder({ title }: { title: string }) {
  const { t } = useTranslation();
  return (
    <Card className="max-w-xl">
      <CardHeader>
        <CardTitle>{title}</CardTitle>
        <CardDescription>{t("common.loading")}</CardDescription>
      </CardHeader>
      <CardContent className="text-muted-foreground text-sm">
        This module is part of a later milestone. See <code>docs/ROADMAP.md</code>.
      </CardContent>
    </Card>
  );
}
