import type { ReactNode } from "react";
import { PaperProvider } from "react-native-paper";
import { I18nextProvider } from "react-i18next";
import container from "@/infrastructure/container";
import i18n from "@/infrastructure/i18n";
import ContainerProvider from "../context/container-provider";
import theme from "../theme";

interface AppProvidersProps {
  children: ReactNode;
}

export default function AppProviders({ children }: AppProvidersProps) {
  return (
    <PaperProvider theme={theme}>
      <I18nextProvider i18n={i18n}>
        <ContainerProvider container={container}>{children}</ContainerProvider>
      </I18nextProvider>
    </PaperProvider>
  );
}
