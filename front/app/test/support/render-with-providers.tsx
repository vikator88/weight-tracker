import type { ReactElement } from "react";
import { render } from "@testing-library/react-native";
import { PaperProvider } from "react-native-paper";
import { I18nextProvider } from "react-i18next";
import type { Dependencies } from "@weight-tracker/core/application";
import { ok } from "@weight-tracker/core/domain";
import { Container, type Registrations } from "@weight-tracker/core/infrastructure";
import i18n from "@/infrastructure/i18n";
import SessionProvider from "@/presentation/auth/context/session-provider";
import ContainerProvider from "@/presentation/common/context/container-provider";
import theme from "@/presentation/common/theme";

/** Registrations that do nothing; tests override the keys the view under test uses. */
const unusedDependencies: Registrations<Dependencies> = {
  httpClient: () => ({ request: async () => undefined as never }),
  keyValueStore: () => ({ get: async () => null, set: async () => {}, remove: async () => {} }),
  notificationService: () => ({ requestPermission: async () => false, notify: async () => {} }),
  authRepository: () => ({
    signup: async () => undefined as never,
    login: async () => undefined as never,
  }),
  userRepository: () => ({
    getMe: async () => undefined as never,
    updateMe: async () => undefined as never,
  }),
  signupUseCase: () => ({ execute: async () => undefined as never }),
  loginUseCase: () => ({ execute: async () => undefined as never }),
  getSessionUseCase: () => ({ execute: async () => ok(null) }),
  logoutUseCase: () => ({ execute: async () => undefined as never }),
  getPreferredLanguageUseCase: () => ({ execute: async () => ok(null) }),
  setPreferredLanguageUseCase: () => ({ execute: async () => ok(undefined) }),
  getMyProfileUseCase: () => ({ execute: async () => undefined as never }),
  updateMyProfileUseCase: () => ({ execute: async () => undefined as never }),
};

/** Renders a view in Spanish, with the real theme and a container of fakes. */
export default async function renderWithProviders(
  ui: ReactElement,
  dependencies: Partial<Registrations<Dependencies>> = {},
) {
  await i18n.changeLanguage("es");
  const container = new Container<Dependencies>({ ...unusedDependencies, ...dependencies });

  return render(
    <PaperProvider theme={theme}>
      <I18nextProvider i18n={i18n}>
        <ContainerProvider container={container}>
          <SessionProvider>{ui}</SessionProvider>
        </ContainerProvider>
      </I18nextProvider>
    </PaperProvider>,
  );
}
