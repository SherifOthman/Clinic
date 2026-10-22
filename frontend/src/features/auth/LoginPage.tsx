import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useTranslation } from "react-i18next";
import { Loader2, LockKeyhole, LogIn } from "lucide-react";
import { authApi } from "./api";
import { useAuthStore } from "./auth-store";
import { ApiError } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { LanguageSelector } from "@/components/common/LanguageSelector";

const schema = z.object({
  username: z.string().min(1),
  password: z.string().min(1),
});
type FormValues = z.infer<typeof schema>;

export default function LoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const setSession = useAuthStore((s) => s.setSession);
  const [submitting, setSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onSubmit = async (values: FormValues) => {
    setSubmitting(true);
    try {
      const res = await authApi.login(values.username, values.password);
      setSession(res.token, res.user);
      navigate("/", { replace: true });
    } catch (e) {
      const msg = e instanceof ApiError && e.status === 400 ? t("auth.invalidCredentials") : t("auth.invalidCredentials");
      setError("username", { message: msg });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="bg-muted/40 flex min-h-svh items-center justify-center p-4">
      <div className="absolute top-4 end-4">
        <LanguageSelector />
      </div>
      <Card className="w-full max-w-sm">
        <CardHeader className="text-center">
          <div className="bg-primary text-primary-foreground mx-auto mb-2 flex size-12 items-center justify-center rounded-xl">
            <LockKeyhole className="size-6" />
          </div>
          <CardTitle className="text-xl">{t("auth.welcomeBack")}</CardTitle>
          <CardDescription>{t("auth.signInToContinue")}</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4" noValidate>
            <div className="flex flex-col gap-2">
              <Label htmlFor="username">{t("auth.username")}</Label>
              <Input
                id="username"
                autoComplete="username"
                autoCapitalize="none"
                {...register("username")}
              />
              {errors.username && (
                <p className="text-destructive text-sm">{errors.username.message}</p>
              )}
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="password">{t("auth.password")}</Label>
              <Input id="password" type="password" autoComplete="current-password" {...register("password")} />
              {errors.password && (
                <p className="text-destructive text-sm">{errors.password.message}</p>
              )}
            </div>

            <Button type="submit" disabled={submitting} className="w-full">
              {submitting ? <Loader2 className="animate-spin" /> : <LogIn />}
              {submitting ? t("auth.signingIn") : t("auth.login")}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
