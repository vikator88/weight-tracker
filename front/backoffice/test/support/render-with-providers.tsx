import type { ReactElement } from "react";
import { ThemeProvider } from "@mui/material/styles";
import { render } from "@testing-library/react";
import { I18nextProvider } from "react-i18next";
import type { Dependencies } from "@weight-tracker/core/application";
import { ok, type UserProfile } from "@weight-tracker/core/domain";
import { Container, type Registrations } from "@weight-tracker/core/infrastructure";
import i18n from "@/infrastructure/i18n";
import CurrentUserProvider from "@/presentation/common/components/current-user-provider";
import ContainerProvider from "@/presentation/common/context/container-provider";
import theme from "@/presentation/common/theme";

/** The profile a signed-in view reads unless a test chooses another one. */
export const completeProfile: UserProfile = {
  id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  email: "trainer@example.com",
  name: "Ana",
  surname: "García",
  role: "TRAINER",
};

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
  getSessionUseCase: () => ({ execute: async () => undefined as never }),
  logoutUseCase: () => ({ execute: async () => undefined as never }),
  getPreferredLanguageUseCase: () => ({ execute: async () => ok(null) }),
  setPreferredLanguageUseCase: () => ({ execute: async () => ok(undefined) }),
  getMyProfileUseCase: () => ({ execute: async () => ok(completeProfile) }),
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
    <ThemeProvider theme={theme}>
      <I18nextProvider i18n={i18n}>
        <ContainerProvider container={container}>
          <CurrentUserProvider>{ui}</CurrentUserProvider>
        </ContainerProvider>
      </I18nextProvider>
    </ThemeProvider>,
  );
}
