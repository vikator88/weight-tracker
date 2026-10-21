"use client";

import { useEffect, type ReactNode } from "react";
import CssBaseline from "@mui/material/CssBaseline";
import { ThemeProvider } from "@mui/material/styles";
import { AppRouterCacheProvider } from "@mui/material-nextjs/v16-appRouter";
import { I18nextProvider } from "react-i18next";
import container from "@/infrastructure/container";
import i18n, { detectLanguage } from "@/infrastructure/i18n";
import ContainerProvider from "../context/container-provider";
import theme from "../theme";

interface AppProvidersProps {
  children: ReactNode;
}

export default function AppProviders({ children }: AppProvidersProps) {
  useEffect(() => {
    void i18n.changeLanguage(detectLanguage());
    document.documentElement.lang = i18n.language;
  }, []);

  return (
    <AppRouterCacheProvider>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <I18nextProvider i18n={i18n}>
          <ContainerProvider container={container}>{children}</ContainerProvider>
        </I18nextProvider>
      </ThemeProvider>
    </AppRouterCacheProvider>
  );
}
