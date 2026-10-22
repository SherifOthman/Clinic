import { useTranslation } from "react-i18next";
import { LANGUAGES, SUPPORTED_LANGUAGES, changeLanguage, type LanguageCode } from "@/i18n";
import { cn } from "@/lib/utils";

/** Compact segmented switch between the supported languages (English / العربية). */
export function LanguageSelector({ className }: { className?: string }) {
  const { i18n } = useTranslation();
  const current = ((i18n.language?.split("-")[0] || "en") as LanguageCode) in LANGUAGES
    ? (i18n.language?.split("-")[0] as LanguageCode)
    : "en";

  return (
    <div className={cn("border-border inline-flex rounded-md border p-0.5", className)}>
      {SUPPORTED_LANGUAGES.map((code) => (
        <button
          key={code}
          type="button"
          onClick={() => void changeLanguage(code)}
          className={cn(
            "rounded px-2.5 py-1 text-xs font-medium transition-colors",
            current === code
              ? "bg-primary text-primary-foreground"
              : "text-muted-foreground hover:text-foreground"
          )}
        >
          {LANGUAGES[code].label}
        </button>
      ))}
    </div>
  );
}
