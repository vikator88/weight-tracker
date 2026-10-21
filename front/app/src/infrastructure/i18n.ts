import { getLocales } from "expo-localization";
import { createInstance } from "i18next";
import { initReactI18next } from "react-i18next";
import resources from "@/i18n/resources";

export const supportedLanguages = ["es", "en"] as const;

/** Device languages, in the order set in the device settings, reduced to the first supported one. */
export function detectLanguage(): string {
  const preferred = getLocales().map((locale) => locale.languageCode);

  return preferred.find((language) => supportedLanguages.some((s) => s === language)) ?? "es";
}

const i18n = createInstance();

void i18n.use(initReactI18next).init({
  resources,
  lng: detectLanguage(),
  fallbackLng: "en",
  supportedLngs: supportedLanguages,
  defaultNS: "common",
  interpolation: { escapeValue: false },
});

export default i18n;
