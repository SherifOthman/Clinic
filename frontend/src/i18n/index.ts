import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import LanguageDetector from "i18next-browser-languagedetector";
import en from "./locales/en.json";
import ar from "./locales/ar.json";

export const LANGUAGES = {
  en: { label: "English", dir: "ltr" as const },
  ar: { label: "العربية", dir: "rtl" as const },
};

export type LanguageCode = keyof typeof LANGUAGES;
export const SUPPORTED_LANGUAGES = Object.keys(LANGUAGES) as LanguageCode[];
export const DEFAULT_LANGUAGE: LanguageCode = "en";
export const LANG_STORAGE_KEY = "clinic.lang";

function applyDirection(lang: string) {
  const code = (lang in LANGUAGES ? lang : DEFAULT_LANGUAGE) as LanguageCode;
  const dir = LANGUAGES[code].dir;
  const html = document.documentElement;
  html.lang = code;
  html.dir = dir;
}

void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: en },
      ar: { translation: ar },
    },
    fallbackLng: DEFAULT_LANGUAGE,
    supportedLngs: SUPPORTED_LANGUAGES,
    nonExplicitSupportedLngs: true,
    interpolation: { escapeValue: false },
    detection: {
      order: ["localStorage", "navigator", "htmlTag"],
      lookupLocalStorage: LANG_STORAGE_KEY,
      caches: ["localStorage"],
    },
  });

// Keep <html dir/lang> in sync whenever the language changes (and on first load).
i18n.on("languageChanged", applyDirection);
applyDirection(i18n.language || DEFAULT_LANGUAGE);

export function changeLanguage(lang: LanguageCode) {
  return i18n.changeLanguage(lang);
}

export function isRTL(lang?: string) {
  const code = (lang ?? i18n.language) as LanguageCode;
  return LANGUAGES[code]?.dir === "rtl";
}

export default i18n;
