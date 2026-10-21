import { createInstance } from "i18next";
import { initReactI18next } from "react-i18next";
import resources from "@/i18n/resources";

export const supportedLanguages = ["es", "en"] as const;

const i18n = createInstance();

// Starts in Spanish so the static build and the first client render match; `detectLanguage` switches after mount.
void i18n.use(initReactI18next).init({
  resources,
  lng: "es",
  fallbackLng: "en",
  supportedLngs: supportedLanguages,
  defaultNS: "common",
  interpolation: { escapeValue: false },
});

/** Browser languages, in preference order, reduced to the first supported one. */
export function detectLanguage(): string {
  const preferred = navigator.languages.map((language) => language.split("-")[0]);

  return preferred.find((language) => supportedLanguages.some((s) => s === language)) ?? "es";
}

export default i18n;
