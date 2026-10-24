"use client";

import { useEffect, type ReactNode } from "react";
import CssBaseline from "@mui/material/CssBaseline";
import { ThemeProvider } from "@mui/material/styles";
import { AppRouterCacheProvider } from "@mui/material-nextjs/v16-appRouter";
import { I18nextProvider } from "react-i18next";
import container from "@/infrastructure/container";
import i18n, { detectLanguage, supportedLanguages } from "@/infrastructure/i18n";
import ContainerProvider from "../context/container-provider";
import CurrentUserProvider from "./current-user-provider";
import theme from "../theme";

interface AppProvidersProps {
  children: ReactNode;
}

export default function AppProviders({ children }: AppProvidersProps) {
  useEffect(() => {
    let active = true;
    const syncDocumentLanguage = (language: string) => {
      document.documentElement.lang = language;
    };
    i18n.on("languageChanged", syncDocumentLanguage);

    // A language the user chose on this device wins; otherwise follow the browser.
    void container
      .resolve("getPreferredLanguageUseCase")
      .execute()
      .then((result) => {
        if (!active) {
          return;
        }
        const preferred = result.ok ? result.value : null;
        const supported = supportedLanguages.find((language) => language === preferred);
        void i18n.changeLanguage(supported ?? detectLanguage());
      });

    return () => {
      active = false;
      i18n.off("languageChanged", syncDocumentLanguage);
    };
  }, []);

  return (
    <AppRouterCacheProvider>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <I18nextProvider i18n={i18n}>
          <ContainerProvider container={container}>
            <CurrentUserProvider>{children}</CurrentUserProvider>
          </ContainerProvider>
        </I18nextProvider>
      </ThemeProvider>
    </AppRouterCacheProvider>
  );
}
